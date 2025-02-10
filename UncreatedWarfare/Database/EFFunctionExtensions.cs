using Microsoft.EntityFrameworkCore;
using System;

namespace Uncreated.Warfare.Database;

internal static class EFFunctionExtensions
{
    /// <summary>
    /// Maps to the RAND() function in MySQL. This is available starting in EFCore 6+ but needs to be added for use in EFCore 5.
    /// </summary>
    public static double Random(this DbFunctions _)
    {
        throw new NotSupportedException("Expected invocation in EF query context only.");
    }
}