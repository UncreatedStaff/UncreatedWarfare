using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Configuration;

/// <summary>
/// Allows for setting properties and fields via string input (such as a command).
/// </summary>
/// <typeparam name="TItem">The declaring type of the variables being set.</typeparam>
public static class SettableUtil<TItem> where TItem : class
{
    private static readonly object Sync = new object();
    public delegate SetPropertyResult CustomSettableHandler(TItem item);
    private static readonly FieldInfo[] Fields = typeof(TItem).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly PropertyInfo[] Properties = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static List<KeyValuePair<string[], CustomSettableHandler>>? _customHandlers;

    /// <summary>
    /// Allows you to add custom set methods to types.
    /// </summary>
    /// <remarks>The first alias will be the display name for the property.</remarks>
    /// <exception cref="ArgumentException">Must have at least one alias.</exception>
    public static void AddCustomHandler(CustomSettableHandler handler, params string[] aliases)
    {
        if (aliases.Length == 0)
            throw new ArgumentException("Must have at least one alias.", nameof(aliases));

        lock (Sync)
        {
            (_customHandlers ??= [ ]).Add(new KeyValuePair<string[], CustomSettableHandler>(aliases, handler));
        }
    }

    /// <summary>
    /// Parse <paramref name="value"/> into the type of <paramref name="property"/> (a field or property in <see cref="TItem"/>) and set the property if it has the <see cref="CommandSettableAttribute"/>.
    /// </summary>
    /// <param name="instance">Object to set the property for.</param>
    /// <param name="property">Name of a property, field, custom handler, or an alias defined in <see cref="CommandSettableAttribute"/>.</param>
    /// <param name="value">The value to set in string format.</param>
    /// <param name="culture">Locale to use for parsing.</param>
    /// <param name="actualPropertyName">Actual name of the discovered member. Never <see langword="null"/> unless <paramref name="property"/> is <see langword="null"/>.</param>
    /// <param name="propertyType">Actual type of the discovered member. Never <see langword="null"/>, defaults to <see cref="string"/> if a property isn't found..</param>
    /// <returns>A success/error code.</returns>
    public static SetPropertyResult SetProperty(TItem instance, string property, string value, CultureInfo culture, [NotNullIfNotNull(nameof(property))] out string? actualPropertyName, out Type propertyType)
    {
        if (string.IsNullOrWhiteSpace(property))
        {
            actualPropertyName = property;
            propertyType = typeof(string);
            return SetPropertyResult.PropertyNotFound;
        }

        object? memberRtn = GetMember(instance, property, out SetPropertyResult reason);

        // custom settable field is already set
        if (memberRtn is not MemberInfo member)
        {
            actualPropertyName = memberRtn as string ?? property;
            propertyType = typeof(string);
            return reason;
        }

        actualPropertyName = member.Name;
        propertyType = member.GetMemberType() ?? typeof(string);

        if (instance is null)
            return SetPropertyResult.ObjectNotFound;

        if (reason != SetPropertyResult.Success)
            return reason;

        Type? memberType = member.GetMemberType();

        if (memberType == null || !FormattingUtility.TryParseAny(value, culture, memberType, out object? val) || val == null || !memberType.IsInstanceOfType(val))
        {
            return SetPropertyResult.ParseFailure;
        }

        try
        {
            Variables.AsVariable(member).SetValue(instance, val);
            return SetPropertyResult.Success;
        }
        catch (Exception ex)
        {
            L.LogError($"Failed to set value of {member switch
            {
                FieldInfo f => Accessor.Formatter.Format(f),
                PropertyInfo p => Accessor.Formatter.Format(p),
                _ => member.Name
            }}.");
            L.LogError(ex);
            return SetPropertyResult.TypeNotSettable;
        }
    }
    private static object? GetMember(TItem instance, string property, out SetPropertyResult reason)
    {
        lock (Sync)
        {
            if (_customHandlers is not null)
            {
                for (int i = 0; i < _customHandlers.Count; ++i)
                {
                    KeyValuePair<string[], CustomSettableHandler> handler = _customHandlers[i];
                    for (int j = 0; j < handler.Key.Length; ++j)
                    {
                        if (!handler.Key[j].Equals(property, StringComparison.OrdinalIgnoreCase))
                            continue;

                        reason = handler.Value(instance);
                        return handler.Key[0];
                    }
                }
            }
        }

        MemberInfo? member = GetField(property, out reason);
        if (member is not null)
        {
            return member;
        }

        member = GetProperty(property, out reason);
        if (member is not null)
        {
            return member;
        }

        lock (Sync)
        {
            if (_customHandlers is null)
            {
                reason = SetPropertyResult.PropertyNotFound;
                return null;
            }

            for (int i = 0; i < _customHandlers.Count; ++i)
            {
                KeyValuePair<string[], CustomSettableHandler> handler = _customHandlers[i];
                for (int j = 0; j < handler.Key.Length; ++j)
                {
                    if (handler.Key[j].IndexOf(property, StringComparison.OrdinalIgnoreCase) == -1)
                        continue;

                    reason = handler.Value(instance);
                    return handler.Key[0];
                }
            }

            reason = SetPropertyResult.PropertyNotFound;
            return null;
        }
    }
    private static FieldInfo? GetField(string property, out SetPropertyResult reason)
    {
        for (int i = 0; i < Fields.Length; i++)
        {
            FieldInfo fi = Fields[i];
            if (!fi.Name.Equals(property, StringComparison.Ordinal))
                continue;

            ValidateField(fi, out reason);
            return fi;
        }
        for (int i = 0; i < Fields.Length; i++)
        {
            FieldInfo fi = Fields[i];
            if (!fi.Name.Equals(property, StringComparison.OrdinalIgnoreCase))
                continue;

            ValidateField(fi, out reason);
            return fi;
        }
        for (int i = 0; i < Fields.Length; i++)
        {
            FieldInfo fi = Fields[i];
            string? n = (Attribute.GetCustomAttribute(fi, typeof(CommandSettableAttribute)) as CommandSettableAttribute)?.Alias;
            if (n == null || !n.Equals(property, StringComparison.OrdinalIgnoreCase))
                continue;

            ValidateField(fi, out reason);
            return fi;
        }
        reason = SetPropertyResult.PropertyNotFound;
        return default;
    }
    private static PropertyInfo? GetProperty(string property, out SetPropertyResult reason)
    {
        for (int i = 0; i < Properties.Length; i++)
        {
            PropertyInfo pi = Properties[i];
            if (!pi.Name.Equals(property, StringComparison.Ordinal))
                continue;

            ValidateProperty(pi, out reason);
            return pi;
        }
        for (int i = 0; i < Properties.Length; i++)
        {
            PropertyInfo pi = Properties[i];
            if (!pi.Name.Equals(property, StringComparison.OrdinalIgnoreCase))
                continue;

            ValidateProperty(pi, out reason);
            return pi;
        }
        for (int i = 0; i < Properties.Length; i++)
        {
            PropertyInfo pi = Properties[i];
            string? n = (Attribute.GetCustomAttribute(pi, typeof(CommandSettableAttribute)) as CommandSettableAttribute)?.Alias;
            if (n == null || !n.Equals(property, StringComparison.OrdinalIgnoreCase))
                continue;

            ValidateProperty(pi, out reason);
            return pi;
        }
        reason = SetPropertyResult.PropertyNotFound;
        return default;
    }
    private static void ValidateField(FieldInfo field, out SetPropertyResult reason)
    {
        if (field == null || field.IsStatic || field.IsInitOnly)
        {
            reason = SetPropertyResult.PropertyNotFound;
            return;
        }

        Attribute atr = Attribute.GetCustomAttribute(field, typeof(CommandSettableAttribute));
        reason = atr is not null ? SetPropertyResult.Success : SetPropertyResult.PropertyProtected;
    }
    private static void ValidateProperty(PropertyInfo property, out SetPropertyResult reason)
    {
        MethodInfo? setter = property?.SetMethod;
        if (property is null || setter is null || setter.IsStatic)
        {
            reason = SetPropertyResult.PropertyNotFound;
            return;
        }

        Attribute atr = Attribute.GetCustomAttribute(property, typeof(CommandSettableAttribute));
        reason = atr is not null ? SetPropertyResult.Success : SetPropertyResult.PropertyProtected;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class CommandSettableAttribute : Attribute
{
    public CommandSettableAttribute() { }
    public CommandSettableAttribute(string alias) => Alias = alias;
    public string? Alias { get; set; }
}

public enum SetPropertyResult
{
    Success,
    ParseFailure,
    PropertyProtected,
    PropertyNotFound,
    TypeNotSettable,
    ObjectNotFound
}