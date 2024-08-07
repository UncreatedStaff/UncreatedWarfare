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

        if (type == null)
        {
            if (typeName.Equals("bool", StringComparison.Ordinal))
            {
                type = typeof(bool);
            }
            else if (typeName.Equals("byte", StringComparison.Ordinal))
            {
                type = typeof(byte);
            }
            else if (typeName.Equals("char", StringComparison.Ordinal))
            {
                type = typeof(char);
            }
            else if (typeName.Equals("decimal", StringComparison.Ordinal))
            {
                type = typeof(decimal);
            }
            else if (typeName.Equals("delegate", StringComparison.Ordinal))
            {
                type = typeof(Delegate);
            }
            else if (typeName.Equals("double", StringComparison.Ordinal))
            {
                type = typeof(double);
            }
            else if (typeName.Equals("enum", StringComparison.Ordinal))
            {
                type = typeof(Enum);
            }
            else if (typeName.Equals("float", StringComparison.Ordinal))
            {
                type = typeof(float);
            }
            else if (typeName.Equals("int", StringComparison.Ordinal))
            {
                type = typeof(int);
            }
            else if (typeName.Equals("long", StringComparison.Ordinal))
            {
                type = typeof(long);
            }
            else if (typeName.Equals("object", StringComparison.Ordinal))
            {
                type = typeof(object);
            }
            else if (typeName.Equals("sbyte", StringComparison.Ordinal))
            {
                type = typeof(sbyte);
            }
            else if (typeName.Equals("short", StringComparison.Ordinal))
            {
                type = typeof(short);
            }
            else if (typeName.Equals("string", StringComparison.Ordinal))
            {
                type = typeof(string);
            }
            else if (typeName.Equals("uint", StringComparison.Ordinal))
            {
                type = typeof(uint);
            }
            else if (typeName.Equals("ulong", StringComparison.Ordinal))
            {
                type = typeof(ulong);
            }
            else if (typeName.Equals("ushort", StringComparison.Ordinal))
            {
                type = typeof(ushort);
            }
            else if (typeName.Equals("void", StringComparison.Ordinal))
            {
                type = typeof(void);
            }

            if (type != null && (expectedBaseType == null || expectedBaseType.IsAssignableFrom(type)))
                return true;
        }

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
            {
                index = -1;
                break;
            }

            index = i;
        }

        type = _allTypesCache[index];
        return true;
    }
}
