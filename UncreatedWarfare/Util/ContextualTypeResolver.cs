using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Util;
internal static class ContextualTypeResolver
{
    private static readonly Assembly ThisAssembly = typeof(ContextualTypeResolver).Assembly;
    private static Type[]? _allTypesFullNameCache;
    private static Type[]? _allTypesNameCache;
    private static string[]? _allTypesFullNamesCache;
    private static string[]? _allTypesNamesCache;

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

        // search by assembly-qualified name
        type = Type.GetType(typeName, false, false);
        if (type != null && (expectedBaseType == null || expectedBaseType.IsAssignableFrom(type)))
            return true;

        // search within main assembly
        type = ThisAssembly.GetType(typeName, false, false);
        if (type != null && (expectedBaseType == null || expectedBaseType.IsAssignableFrom(type)))
            return true;

        // search by actual type name
        if (_allTypesFullNameCache == null)
        {
            LoadAllTypes();
        }
        
        Interlocked.MemoryBarrier();

        int index = Array.BinarySearch(_allTypesFullNamesCache!, typeName, StringComparer.Ordinal);
        if (index >= 0 && (expectedBaseType == null || expectedBaseType.IsAssignableFrom(_allTypesFullNameCache![index])))
        {
            type = _allTypesFullNameCache![index];
            return true;
        }

        index = Array.BinarySearch(_allTypesNamesCache!, typeName, StringComparer.Ordinal);
        if (index >= 0 && (expectedBaseType == null || expectedBaseType.IsAssignableFrom(_allTypesNameCache![index])))
        {
            // check for duplicates
            for (int i = 0; i < _allTypesNameCache!.Length; ++i)
            {
                if (i == index || !_allTypesNamesCache![i].Equals(typeName, StringComparison.Ordinal) || expectedBaseType != null && !expectedBaseType.IsAssignableFrom(type))
                {
                    continue;
                }

                type = null;
                return false;
            }

            type = _allTypesNameCache[index];
            return true;
        }

        type = null;
        return false;
    }

    private static void LoadAllTypes()
    {
        Assembly warfareAssembly = Assembly.GetExecutingAssembly();

        List<Assembly> assemblies = [ warfareAssembly ];

        foreach (AssemblyName referencedAssembly in warfareAssembly.GetReferencedAssemblies())
        {
            try
            {
                assemblies.Add(Assembly.Load(referencedAssembly));
            }
            catch { /* ignored */ }
        }

        List<Type> allTypesCache = Accessor.GetTypesSafe(assemblies);

        allTypesCache.RemoveAll(x => x.IsNested || x.IsDefinedSafe<CompilerGeneratedAttribute>());

        int ct = allTypesCache.Count;
        _allTypesNameCache = new Type[ct];
        _allTypesFullNameCache = new Type[ct];

        allTypesCache.CopyTo(_allTypesNameCache);
        Array.Copy(_allTypesNameCache, _allTypesFullNameCache, ct);

        // yes this is necessary, its very slow without binary search
        _allTypesNamesCache = new string[ct];
        _allTypesFullNamesCache = new string[ct];
        for (int i = 0; i < _allTypesNameCache.Length; ++i)
        {
            _allTypesNamesCache[i] = _allTypesNameCache[i].Name;
            _allTypesFullNamesCache[i] = _allTypesFullNameCache[i].FullName ?? _allTypesFullNameCache[i].Name;
        }

        Array.Sort(_allTypesNamesCache, _allTypesNameCache, StringComparer.Ordinal);
        Array.Sort(_allTypesFullNamesCache, _allTypesFullNameCache, StringComparer.Ordinal);
    }
}
