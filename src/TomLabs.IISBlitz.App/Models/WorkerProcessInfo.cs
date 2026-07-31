namespace TomLabs.IISBlitz.App.Models;

public record WorkerProcessInfo(int Pid, string AppPoolName, string State, long MemoryKb);
