using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TomLabs.IISBlitz.App.Services;

/// <summary>
/// Writes a full memory dump of a process with dbghelp's MiniDumpWriteDump — the same file
/// Task Manager, ProcDump and Visual Studio produce, so it opens in VS, WinDbg or dotnet-dump.
/// </summary>
public static class MemoryDumpService
{
    public static string DumpDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IISBlitz", "dumps");

    [Flags]
    private enum MiniDumpType : uint
    {
        WithFullMemory = 0x00000002,
        WithHandleData = 0x00000004,
        WithUnloadedModules = 0x00000020,
        WithFullMemoryInfo = 0x00000800,
        WithThreadInfo = 0x00001000,
        IgnoreInaccessibleMemory = 0x00020000,
    }

    [DllImport("dbghelp.dll", SetLastError = true)]
    private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeHandle hFile, MiniDumpType dumpType,
        IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);

    /// <summary>Writes the dump on a background thread and returns the file path.</summary>
    public static Task<string> DumpAsync(int pid, string poolName) => Task.Run(() =>
    {
        Directory.CreateDirectory(DumpDirectory);
        var safePool = string.Concat(poolName.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(DumpDirectory, $"w3wp_{safePool}_{pid}_{DateTime.Now:yyyyMMdd-HHmmss}.dmp");

        using var process = Process.GetProcessById(pid);
        using var file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        const MiniDumpType type = MiniDumpType.WithFullMemory | MiniDumpType.WithHandleData | MiniDumpType.WithUnloadedModules
                                  | MiniDumpType.WithFullMemoryInfo | MiniDumpType.WithThreadInfo | MiniDumpType.IgnoreInaccessibleMemory;

        if (!MiniDumpWriteDump(process.Handle, (uint)pid, file.SafeFileHandle, type, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
        {
            var error = Marshal.GetLastWin32Error();
            file.Close();
            File.Delete(path);
            throw new InvalidOperationException($"MiniDumpWriteDump failed (Win32 error {error}).");
        }

        return path;
    });

    /// <summary>Opens Explorer with the dump selected.</summary>
    public static void RevealInExplorer(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }
        catch
        {
            // Explorer missing is not worth an error dialog.
        }
    }
}
