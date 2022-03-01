using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated;

public static class ProfilingUtils
{
    private static readonly List<MethodProfileData> _profiles = new List<MethodProfileData>();
    public static IDisposable StartTracking([CallerMemberName] string callerName = "", [CallerFilePath] string filepath = "", [CallerLineNumber] int linenumber = 0)
    {
        MethodProfileData profile;
        for (int i = _profiles.Count - 1; i >= 0; i--)
        {
            if (_profiles[i].MethodName == callerName)
            {
                profile = _profiles[i];
                profile.stopwatch.Stop();
                goto next;
            }
        }
        profile = new MethodProfileData()
        {
            MethodName = callerName,
            MethodHash = callerName.GetHashCode(),
            FilePath = filepath,
            LineNumber = linenumber,
            stopwatch = new Stopwatch()
        };
        _profiles.Add(profile);
    next:
        profile.stopwatch.Start();
        profile.inProgress = true;
        return profile;
    }
    public static void StopTracking([CallerMemberName] string callerName = "", [CallerFilePath] string filepath = "")
    {
        int callerNameHash = callerName.GetHashCode();
        for (int i = _profiles.Count - 1; i >= 0; i--)
        {
            MethodProfileData data = _profiles[i];
            if (data.inProgress && data.MethodHash.Equals(callerNameHash) && data.MethodName.Equals(callerName, StringComparison.Ordinal) && data.FilePath.Equals(filepath, StringComparison.OrdinalIgnoreCase))
            {
                data.stopwatch.Stop();
                if (data.avgCount == 0)
                {
                    data.ExecutionTime = data.stopwatch.Elapsed.TotalMilliseconds;
                    data.avgCount = 1;
                }
                else
                    data.ExecutionTime = (data.ExecutionTime * data.avgCount++ + data.stopwatch.Elapsed.TotalMilliseconds) / data.avgCount;
                data.inProgress = false;
                return;
            }
        }
    }
    public static void WriteAllDataToCSV(string file)
    {
        using (FileStream writer = new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
        {
            byte[] bytes = Encoding.UTF8.GetBytes("MethodName,ExecutionTimeMS,Executions,FirstExecutionLine#,File");
            writer.Write(bytes, 0, bytes.Length);
            foreach (MethodProfileData data in _profiles)
            {
                if (!data.inProgress)
                {
                    bytes = Encoding.UTF8.GetBytes($"\n{data.MethodName},{data.ExecutionTime},{data.avgCount},{data.LineNumber},{new DirectoryInfo(data.FilePath).Name.Replace(',', '_')}");
                    writer.Write(bytes, 0, bytes.Length);
                }
            }
            writer.Flush();
        }
    }
    public static void Clear()
    {
        for (int i = _profiles.Count - 1; i >= 0; i--)
        {
            if (!_profiles[i].inProgress)
                _profiles.RemoveAt(i);
        }
    }
    private class MethodProfileData : IDisposable
    {
        public int MethodHash;
        public string MethodName;
        public string FilePath;
        public double ExecutionTime;
        public int avgCount;
        public int LineNumber;
        public bool inProgress;
        public Stopwatch stopwatch;
        public void Dispose()
        {
            stopwatch.Stop();
            if (avgCount == 0)
            {
                ExecutionTime = stopwatch.Elapsed.TotalMilliseconds;
                avgCount = 1;
            }
            else
                ExecutionTime = (ExecutionTime * avgCount++ + stopwatch.Elapsed.TotalMilliseconds) / avgCount;
            inProgress = false;
        }
    }
}