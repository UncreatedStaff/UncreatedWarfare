#if PROFILING
using System;
using System.Diagnostics;
using System.IO;

namespace Uncreated.Warfare.Profiling;

internal sealed class UncreatedProfiler : IDisposable
{
    private readonly Stopwatch _stopwatch = new Stopwatch();

    private string? _description, _filePath, _methodName;
    private int _lineNumber;

    private int _state;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _state, 0) != 1)
            return;

        _stopwatch.Stop();
        ProfilerUtil.RecordResults(_stopwatch.ElapsedTicks, _description, _filePath!, _methodName!, _lineNumber);
        _filePath = null;
        _description = null;
        _methodName = null;
        _lineNumber = 0;
        ProfilerUtil.ReturnProfiler(this);
    }

    internal void Load(string? description, string filePath, string methodName, int lineNumber)
    {
        if (Interlocked.Exchange(ref _state, 1) != 0)
            throw new InvalidOperationException("Profiler still running.");

        _description = description;
        _filePath = filePath;
        _methodName = methodName;
        _lineNumber = lineNumber;

        _stopwatch.Restart();
    }

    public override string ToString()
    {
        string? desc = _description, fn = _filePath, mn = _methodName;
        int ln = _lineNumber;
        if (fn == null || mn == null || ln == 0)
        {
            return "<pooled>";
        }

        return desc ?? Path.GetFileName(fn) + "." + mn + ":" + ln;
    }
}
#endif