using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Web.Administration;
using TomLabs.IISBlitz.App.Models;

namespace TomLabs.IISBlitz.App.Services;

/// <summary>
/// Samples CPU and memory of every IIS worker process on a background timer and feeds the
/// <see cref="PoolStats"/> objects the UI binds to. The pool → PID mapping is refreshed from IIS
/// every few rounds because worker processes come and go with recycles.
/// </summary>
public sealed class ProcessMonitor : IDisposable
{
    public static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(2);
    private const int MappingRefreshEveryRounds = 5;

    private readonly ConcurrentDictionary<string, PoolStats> _pools = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, WorkerProcessInfo> _workers = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Action<string> _log;
    private int _round;

    public ProcessMonitor(Action<string>? log = null)
    {
        _log = log ?? (_ => { });
    }

    /// <summary>The stats object for a pool; created on first use so a site can bind before the first sample.</summary>
    public PoolStats GetOrCreate(string poolName) => _pools.GetOrAdd(poolName, name => new PoolStats(name));

    public void Start() => _ = RunAsync(_cts.Token);

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_round++ % MappingRefreshEveryRounds == 0)
                        RefreshMapping();
                    Sample();
                }
                catch (Exception ex)
                {
                    _log($"Process monitor: {ex.Message}");
                }

                await Task.Delay(SampleInterval, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed.
        }
    }

    /// <summary>Reads the worker processes of every pool from IIS and adds/removes entries accordingly.</summary>
    private void RefreshMapping()
    {
        using var manager = new ServerManager();
        var seen = new HashSet<int>();
        var changes = new List<Action>();

        foreach (var pool in manager.ApplicationPools)
        {
            var stats = GetOrCreate(pool.Name);
            foreach (var wp in pool.WorkerProcesses)
            {
                seen.Add(wp.ProcessId);
                var state = wp.State.ToString();
                if (_workers.TryGetValue(wp.ProcessId, out var existing))
                {
                    changes.Add(() => existing.State = state);
                    continue;
                }

                var worker = new WorkerProcessInfo(wp.ProcessId, pool.Name) { State = state };
                try
                {
                    using var process = Process.GetProcessById(wp.ProcessId);
                    worker.StartedAt = process.StartTime;
                    worker.LastProcessorTime = process.TotalProcessorTime;
                    worker.LastSampleAt = DateTime.UtcNow;
                }
                catch
                {
                    // The process may have exited between the IIS query and now; it is dropped on the next refresh.
                }

                _workers[wp.ProcessId] = worker;
                changes.Add(() => stats.Workers.Add(worker));
            }
        }

        foreach (var gone in _workers.Keys.Where(pid => !seen.Contains(pid)).ToList())
        {
            var worker = _workers[gone];
            _workers.Remove(gone);
            if (_pools.TryGetValue(worker.AppPoolName, out var stats))
                changes.Add(() => stats.Workers.Remove(worker));
        }

        if (changes.Count > 0)
            Dispatcher.UIThread.Post(() => { foreach (var change in changes) change(); });
    }

    /// <summary>One CPU/memory reading per worker; CPU is the processor-time delta over the wall-clock delta, across all cores.</summary>
    private void Sample()
    {
        var updates = new List<(WorkerProcessInfo worker, double cpu, double memoryMb)>();
        var now = DateTime.UtcNow;

        foreach (var worker in _workers.Values)
        {
            try
            {
                using var process = Process.GetProcessById(worker.Pid);
                process.Refresh();
                var processorTime = process.TotalProcessorTime;
                var elapsed = (now - worker.LastSampleAt).TotalSeconds;
                var cpu = elapsed > 0 && worker.LastSampleAt != default
                    ? (processorTime - worker.LastProcessorTime).TotalSeconds / elapsed / Environment.ProcessorCount * 100
                    : 0;
                worker.LastProcessorTime = processorTime;
                worker.LastSampleAt = now;
                updates.Add((worker, Math.Clamp(cpu, 0, 100), process.WorkingSet64 / 1048576.0));
            }
            catch
            {
                updates.Add((worker, 0, 0));
            }
        }

        Dispatcher.UIThread.Post(() =>
        {
            foreach (var (worker, cpu, memoryMb) in updates)
            {
                worker.CpuPercent = cpu;
                worker.MemoryMb = memoryMb;
            }
            foreach (var stats in _pools.Values)
                stats.Recalculate();
        });
    }

    public void Dispose() => _cts.Cancel();
}
