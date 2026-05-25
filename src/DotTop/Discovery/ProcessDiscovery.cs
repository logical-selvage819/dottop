using System.Diagnostics;
using Microsoft.Diagnostics.NETCore.Client;

namespace DotTop.Discovery;

public static class ProcessDiscovery
{
    public static IReadOnlyList<DotNetProcessInfo> Enumerate()
    {
        var self = Environment.ProcessId;
        var result = new List<DotNetProcessInfo>();

        IEnumerable<int> pids;
        try
        {
            pids = DiagnosticsClient.GetPublishedProcesses();
        }
        catch
        {
            return result;
        }

        foreach (var pid in pids)
        {
            if (pid == self) continue;

            var info = TryDescribe(pid);
            if (info is not null) result.Add(info);
        }

        return result
            .OrderByDescending(p => p.DiagnosticsReachable)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static DotNetProcessInfo? FindByPid(int pid) => TryDescribe(pid);

    public static IReadOnlyList<DotNetProcessInfo> FindByName(string needle)
    {
        var all = Enumerate();
        return all
            .Where(p => p.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static DotNetProcessInfo? TryDescribe(int pid)
    {
        Process? proc = null;
        try { proc = Process.GetProcessById(pid); }
        catch { return null; }

        string name;
        DateTime startTime;
        string? runtime = null;
        string? cmd = null;
        try
        {
            name = proc.ProcessName;
            startTime = proc.StartTime;
            try { runtime = proc.MainModule?.FileVersionInfo.ProductVersion; }
            catch { runtime = null; }
        }
        catch
        {
            return null;
        }
        finally
        {
            proc.Dispose();
        }

        // GetPublishedProcesses already scans the diagnostics port directory, so the
        // socket file exists. Actual connect succeeds at session start.
        return new DotNetProcessInfo(pid, name, cmd, runtime, OperatingSystem: null, startTime, DiagnosticsReachable: true);
    }
}
