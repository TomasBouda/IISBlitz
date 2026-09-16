using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TomLabs.IISBlitz.App.Models;

/// <summary>One w3wp.exe of an application pool with its live CPU and memory figures.</summary>
public partial class WorkerProcessInfo : ObservableObject
{
    public WorkerProcessInfo(int pid, string appPoolName)
    {
        Pid = pid;
        AppPoolName = appPoolName;
    }

    public int Pid { get; }

    public string AppPoolName { get; }

    [ObservableProperty]
    private string _state = string.Empty;

    /// <summary>Share of all cores, 0–100.</summary>
    [ObservableProperty]
    private double _cpuPercent;

    /// <summary>Working set in MB.</summary>
    [ObservableProperty]
    private double _memoryMb;

    [ObservableProperty]
    private DateTime? _startedAt;

    /// <summary>Last processor time seen, used to compute the CPU delta between samples.</summary>
    internal TimeSpan LastProcessorTime { get; set; }

    internal DateTime LastSampleAt { get; set; }

    public string CpuText => string.Create(CultureInfo.InvariantCulture, $"{CpuPercent:0.0} %");

    public string MemoryText => string.Create(CultureInfo.InvariantCulture, $"{MemoryMb:0} MB");

    partial void OnCpuPercentChanged(double value) => OnPropertyChanged(nameof(CpuText));

    partial void OnMemoryMbChanged(double value) => OnPropertyChanged(nameof(MemoryText));
}
