using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TomLabs.IISBlitz.App.Models;

/// <summary>
/// Live resource usage of one application pool: the sum over its worker processes plus a short history
/// (one sample every <see cref="Services.ProcessMonitor.SampleInterval"/>) for the sparklines.
/// </summary>
public partial class PoolStats : ObservableObject
{
    public const int HistoryLength = 60;

    private readonly List<double> _cpuHistory = new();
    private readonly List<double> _memoryHistory = new();

    public PoolStats(string poolName)
    {
        PoolName = poolName;
    }

    public string PoolName { get; }

    public ObservableCollection<WorkerProcessInfo> Workers { get; } = new();

    [ObservableProperty]
    private double _cpuPercent;

    [ObservableProperty]
    private double _memoryMb;

    /// <summary>Snapshot copies so the chart re-renders on assignment.</summary>
    [ObservableProperty]
    private List<double> _cpuHistorySnapshot = new();

    [ObservableProperty]
    private List<double> _memoryHistorySnapshot = new();

    public bool HasWorkers => Workers.Count > 0;

    /// <summary>"3.2 % · 184 MB" for the sidebar, or "no worker" when the pool has not spun up.</summary>
    public string Summary => HasWorkers ? string.Create(CultureInfo.InvariantCulture, $"{CpuPercent:0}% · {MemoryMb:0} MB") : "no worker";

    public string CpuText => HasWorkers ? string.Create(CultureInfo.InvariantCulture, $"{CpuPercent:0.0} %") : "—";

    public string MemoryText => HasWorkers ? string.Create(CultureInfo.InvariantCulture, $"{MemoryMb:0} MB") : "—";

    /// <summary>Called by the monitor after every sample round.</summary>
    public void Recalculate()
    {
        CpuPercent = Workers.Sum(w => w.CpuPercent);
        MemoryMb = Workers.Sum(w => w.MemoryMb);

        // Only real readings go into the history; an idle pool shows an empty chart instead of a flat zero line.
        if (Workers.Count > 0 && Workers.Any(w => w.LastSampleAt != default))
        {
            Push(_cpuHistory, CpuPercent);
            Push(_memoryHistory, MemoryMb);
            CpuHistorySnapshot = new List<double>(_cpuHistory);
            MemoryHistorySnapshot = new List<double>(_memoryHistory);
        }

        OnPropertyChanged(nameof(HasWorkers));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(CpuText));
        OnPropertyChanged(nameof(MemoryText));
    }

    private static void Push(List<double> history, double value)
    {
        history.Add(value);
        if (history.Count > HistoryLength) history.RemoveAt(0);
    }
}
