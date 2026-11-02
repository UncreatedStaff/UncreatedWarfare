#if PROFILING
using System;

namespace Uncreated.Warfare.Profiling;

internal readonly struct UncreatedProfilerKey : IEquatable<UncreatedProfilerKey>
{
    public readonly string? Description;
    public readonly string FilePath;
    public readonly string MethodName;
    public readonly int LineNumber;

    public UncreatedProfilerKey(string? description, string filePath, string methodName, int lineNumber)
    {
        Description = description;
        FilePath = filePath;
        MethodName = methodName;
        LineNumber = lineNumber;
    }

    private bool EqualsHelper(in UncreatedProfilerKey other)
    {
        return LineNumber == other.LineNumber
               && string.Equals(FilePath, other.FilePath, StringComparison.Ordinal)
               && string.Equals(MethodName, other.MethodName, StringComparison.Ordinal)
               && string.Equals(Description, other.Description, StringComparison.Ordinal);
    }

    public bool Equals(UncreatedProfilerKey other)
    {
        return EqualsHelper(in other);
    }

    public override bool Equals(object? obj)
    {
        return obj is UncreatedProfilerKey k && EqualsHelper(in k);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LineNumber, FilePath, MethodName, Description);
    }

    public override string ToString()
    {
        return $"{{ Profiler Key: Line#: {LineNumber}, Path: \"{FilePath}\", MethodName: \"{MethodName}\", Desc: \"{Description}\" }}";
    }
}
#endif