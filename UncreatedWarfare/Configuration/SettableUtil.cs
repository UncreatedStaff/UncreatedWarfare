using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    public delegate SetPropertyResult CustomSettableHandler<in TValueType>(TItem item, TValueType? value, IServiceProvider serviceProvider);

    private static readonly object Sync = new object();

    private static readonly FieldInfo[] Fields = typeof(TItem).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly PropertyInfo[] Properties = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static List<CustomSettablePropertyInfo>? _customHandlers;

    /// <summary>
    /// Allows you to add custom set methods to types.
    /// </summary>
    /// <remarks>The first alias will be the display name for the property.</remarks>
    /// <exception cref="ArgumentException">Must have at least one alias.</exception>
    public static void AddCustomHandler<TValueType>(CustomSettableHandler<TValueType> handler, params string[] aliases)
    {
        if (aliases.Length == 0)
            throw new ArgumentException("Must have at least one alias.", nameof(aliases));

        CustomSettablePropertyInfo props = new CustomSettablePropertyInfo(aliases, typeof(TValueType), handler);
        props.Callback = props.InvokeCallback<TValueType>;

        lock (Sync)
        {
            (_customHandlers ??= [ ]).Add(props);
        }
    }

    /// <summary>
    /// Parse <paramref name="value"/> into the type of <paramref name="property"/> (a field or property in <see cref="TItem"/>) and set the property if it has the <see cref="CommandSettableAttribute"/>.
    /// </summary>
    /// <param name="instance">Object to set the property for.</param>
    /// <param name="property">Name of a property, field, custom handler, or an alias defined in <see cref="CommandSettableAttribute"/>.</param>
    /// <param name="value">The value to set in string format.</param>
    /// <param name="provider">Locale to use for parsing.</param>
    /// <param name="actualPropertyName">Actual name of the discovered member. Never <see langword="null"/> unless <paramref name="property"/> is <see langword="null"/>.</param>
    /// <param name="propertyType">Actual type of the discovered member. Never <see langword="null"/>, defaults to <see cref="string"/> if a property isn't found..</param>
    /// <returns>A success/error code.</returns>
    public static SetPropertyResult SetProperty(TItem instance, string property, string value, IFormatProvider provider, IServiceProvider serviceProvider, [NotNullIfNotNull(nameof(property))] out string? actualPropertyName, out Type propertyType)
    {
        if (string.IsNullOrWhiteSpace(property))
        {
            actualPropertyName = property;
            propertyType = typeof(string);
            return SetPropertyResult.PropertyNotFound;
        }

        object? memberRtn = GetMember(property, out SetPropertyResult reason);

        Type? memberType;
        if (memberRtn is MemberInfo member)
        {
            actualPropertyName = member.Name;
            propertyType = member.GetMemberType() ?? typeof(string);

            if (instance is null)
                return SetPropertyResult.ObjectNotFound;

            if (reason != SetPropertyResult.Success)
                return reason;

            memberType = member.GetMemberType();
        }
        else if (memberRtn is not CustomSettablePropertyInfo custom)
        {
            actualPropertyName = property;
            propertyType = typeof(string);
            return SetPropertyResult.PropertyNotFound;
        }
        else
        {
            memberType = custom.ValueType;
            actualPropertyName = custom.Aliases[0];
            propertyType = memberType;
        }


        if (memberType == null || !FormattingUtility.TryParseAny(value, provider, memberType, out object? val) || val != null && !memberType.IsInstanceOfType(val) || val == null && memberType.IsValueType)
        {
            return SetPropertyResult.ParseFailure;
        }

        if (memberRtn is CustomSettablePropertyInfo custom2)
        {
            try
            {
                return custom2.Callback(instance, val, serviceProvider);
            }
            catch (Exception ex)
            {
                L.LogError($"Failed to set value of \"{instance}.{custom2.Aliases[0]}\" (custom settable).");
                L.LogError(ex);
                return SetPropertyResult.TypeNotSettable;
            }
        }

        IVariable variable = Variables.AsVariable((MemberInfo)memberRtn);

        try
        {
            variable.SetValue(instance, val);
            return SetPropertyResult.Success;
        }
        catch (Exception ex)
        {
            L.LogError($"Failed to set value of \"{variable.Format(Accessor.Formatter, includeDefinitionKeywords: true)}\".");
            L.LogError(ex);
            return SetPropertyResult.TypeNotSettable;
        }
    }

    private static object? GetMember(string property, out SetPropertyResult reason)
    {
        lock (Sync)
        {
            if (_customHandlers is not null)
            {
                for (int i = 0; i < _customHandlers.Count; ++i)
                {
                    CustomSettablePropertyInfo handler = _customHandlers[i];
                    for (int j = 0; j < handler.Aliases.Length; ++j)
                    {
                        if (!handler.Aliases[j].Equals(property, StringComparison.OrdinalIgnoreCase))
                            continue;

                        reason = SetPropertyResult.Success;
                        return handler;
                    }
                }
            }
        }

        return (MemberInfo?)GetField(property, out reason) ?? GetProperty(property, out reason);
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


    private class CustomSettablePropertyInfo
    {
        public readonly string[] Aliases;
        public readonly Type ValueType;
        public readonly Delegate Handler;
        public Func<TItem, object?, IServiceProvider, SetPropertyResult> Callback;
        public CustomSettablePropertyInfo(string[] aliases, Type valueType, Delegate handler)
        {
            Aliases = aliases;
            ValueType = valueType;
            Handler = handler;
        }

        public SetPropertyResult InvokeCallback<TValueType>(TItem instance, object? value, IServiceProvider serviceProvider)
        {
            return ((CustomSettableHandler<TValueType>)Handler)(instance, (TValueType?)value, serviceProvider);
        }
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