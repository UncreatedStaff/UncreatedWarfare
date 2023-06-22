using System;
using System.Reflection;
using Uncreated.Framework;

namespace Uncreated.Warfare.Moderation;
public static class PunishmentReflection
{
    private static bool _hasReflected;
    public static Map<ModerationEntryType, Type> Types = new Map<ModerationEntryType, Type>(16);
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
        for (int i = 0; i < types.Length; ++i)
        {
            Type type = types[i];
            if (Types.Contains(type))
                continue;
            if (typeof(ModerationEntry).IsAssignableFrom(type) &&
                Attribute.GetCustomAttribute(type, typeof(ModerationEntryAttribute)) is ModerationEntryAttribute attr)
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