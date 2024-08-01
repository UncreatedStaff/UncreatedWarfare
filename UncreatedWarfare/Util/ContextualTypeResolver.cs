using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Uncreated.Warfare.Util;
internal static class ContextualTypeResolver
{
    private static readonly Assembly ThisAssembly = typeof(ContextualTypeResolver).Assembly;
    private static List<Type>? _allTypesCache;

    /// <summary>
    /// Attempt to resolve a type based on a type name input.
    /// </summary>
    /// <param name="expectedBaseType">Any found types must be assignable to this type.</param>
    public static Type? ResolveType(string? typeName, Type? expectedBaseType = null)
    {
        return TryResolveType(typeName, out Type? type, expectedBaseType) ? type : null;
    }

    /// <summary>
    /// Attempt to resolve a type based on a type name input.
    /// </summary>
    /// <param name="expectedBaseType">Any found types must be assignable to this type.</param>
    public static bool TryResolveType([NotNullWhen(true)] string? typeName, [MaybeNullWhen(false)] out Type type, Type? expectedBaseType = null)
    {
        type = null;
        if (typeName == null)
            return false;

        // search by assembly-qualified name
        type = Type.GetType(typeName, false, false);
        if (type != null && (expectedBaseType == null || expectedBaseType.IsAssignableFrom(type)))
            return true;

        // search within main assembly
        type = ThisAssembly.GetType(typeName, false, false);
        if (type != null && (expectedBaseType == null || expectedBaseType.IsAssignableFrom(type)))
            return true;

        // search by actual type name
        _allTypesCache ??= Accessor.GetTypesSafe(ThisAssembly);
        
        Interlocked.MemoryBarrier();

        int index = -1;
        for (int i = 0; i < _allTypesCache.Count; ++i)
        {
            Type t = _allTypesCache[i];
            if (!t.Name.Equals(typeName, StringComparison.Ordinal))
                continue;

            if (expectedBaseType != null && !expectedBaseType.IsAssignableFrom(t))
                continue;

            if (index != -1)
                return false;

            index = i;
        }

        if (index == -1)
            return false;

        type = _allTypesCache[index];
        return true;
    }
}
