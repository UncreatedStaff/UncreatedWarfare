using DanielWillett.ReflectionTools;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Uncreated.Warfare.Util.List;

/// <summary>
/// Fast type-to-object dictionary that can only be used once per type and type list.
/// </summary>
public class SingleUseTypeDictionary<TType> where TType : notnull
{
    private readonly TType[] _values;

    /// <summary>
    /// List of all values in order of their types.
    /// </summary>
    public TType[] Values => _values;
    public SingleUseTypeDictionary(Type[] types, TType[] values)
    {
        _values = values;
        Type[] typeArgs = [ typeof(TType), null! ];
        for (int i = 0; i < types.Length; ++i)
        {
            typeArgs[1] = types[i];
            typeof(IndexCache<>)
                .MakeGenericType(typeArgs)
                .GetField(nameof(IndexCache<object>.Index), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!
                .SetValue(null, i);
        }
    }

    /// <summary>
    /// Retreives the component using a given type.
    /// </summary>
    /// <exception cref="ComponentNotFoundException">Thrown when the component isn't found.</exception>
    public TValueType Get<TValueType, TContext>(TContext context) where TValueType : TType where TContext : notnull
    {
        int index = IndexCache<TValueType>.Index;
        return index < 0 || index >= _values.Length
            ? throw new ComponentNotFoundException(typeof(TValueType), context)
            : (TValueType)_values[index];
    }

    /// <summary>
    /// Retreives the component using a given type.
    /// </summary>
    public bool TryGet<TValueType>([NotNullWhen(true)] out TValueType? value) where TValueType : TType
    {
        int index = IndexCache<TValueType>.Index;
        if (index < 0 || index >= _values.Length)
        {
            value = default;
            return false;
        }

        value = (TValueType)_values[index];
        return true;
    }

    // ReSharper disable once UnusedTypeParameter
    private static class IndexCache<TValueType>
    {
        public static int Index = -1;
    }
}

public class ComponentNotFoundException : Exception
{
    public ComponentNotFoundException(Type type, object context)
        : base($"The component {Accessor.ExceptionFormatter.Format(type)} could not be found on {context}.")
    { }
}