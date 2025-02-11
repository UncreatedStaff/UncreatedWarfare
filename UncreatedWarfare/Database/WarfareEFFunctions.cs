using System;

namespace Uncreated.Warfare.Database;

internal static class WarfareEFFunctions
{
    /// <summary>
    /// Maps to the RAND() function in MySQL. This is available starting in EFCore 6+ but needs to be added for use in EFCore 5.
    /// </summary>
    // needs uncommented in WarfareDbContext if used
    public static double Random()
    {
        throw new NotSupportedException("Expected invocation in EF query context only.");
    }
}