﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uncreated.Framework;

namespace Uncreated.Warfare.Moderation;
public static class ModerationReflection
{
    private static bool _hasReflected;
    public static Map<ModerationEntryType, Type> Types = new Map<ModerationEntryType, Type>(16);
    public static Dictionary<Type, ModerationEntryType[]> TypeInheritance = new Dictionary<Type, ModerationEntryType[]>(20);
    public static Type? GetType(ModerationEntryType type)
    {
        CheckReflect();
        return Types.TryGetValue(type, out Type type2) ? type2 : null;
    }
    public static ModerationEntryType? GetType(Type type)
    {
        CheckReflect();
        return Types.TryGetValue(type, out ModerationEntryType type2) ? type2 : null;
    }
    private static void CheckReflect()
    {
        if (_hasReflected) return;
        _hasReflected = true;
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type[] types = assembly.GetTypes();
        List<Type> types2 = new List<Type>();
        for (int i = 0; i < types.Length; ++i)
        {
            Type type = types[i];
            if (Types.Contains(type))
                continue;
            if (typeof(ModerationEntry).IsAssignableFrom(type))
            {
                types2.Add(type);
                if (Attribute.GetCustomAttribute(type, typeof(ModerationEntryAttribute)) is ModerationEntryAttribute attr)
                {
                    if (Types.Contains(attr.Type))
                    {
                        L.LogWarning($"Multiple moderation types defined with {attr.Type}.");
                        continue;
                    }

                    Types.Add(attr.Type, type);
                }
            }
        }

        ICollection<Type> leafTypes = (Types as IDictionary<ModerationEntryType, Type>).Values;
        foreach (Type type in types2)
            TypeInheritance.Add(type, leafTypes.Where(type.IsAssignableFrom).Select(x => Types[x]).ToArray());
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class ModerationEntryAttribute : Attribute
{
    public ModerationEntryType Type { get; set; }
    public ModerationEntryAttribute(ModerationEntryType type)
    {
        Type = type;
    }
}