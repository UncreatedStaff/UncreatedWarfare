using System;
using System.Text.Json;

namespace Uncreated.Warfare.NewQuests.Parameters;
public abstract class QuestParameterValue<TValue> : IFormattable, IEquatable<QuestParameterValue<TValue>>
{
    /// <summary>
    /// Type of value set. Constant, list, wildcard, and sometimes range.
    /// </summary>
    public ParameterValueType ValueType { get; protected set; }

    /// <summary>
    /// Selection style of the parameter. Inclusive allows *any* value in the set, where selective selects *one* value in the set.
    /// </summary>
    public ParameterSelectionType SelectionType { get; protected set; }

    /// <summary>
    /// Compare a value against the current value of this parameter. Best used with inclusive selection.
    /// </summary>
    public abstract bool IsMatch(TValue otherValue);

    /// <summary>
    /// Get one value to use. Must either be a constant or selective.
    /// </summary>
    /// <exception cref="InvalidOperationException">Inclusive selection is not supported when getting a single value.</exception>
    public abstract TValue GetSingleValue();

    /// <summary>
    /// Get one value to use. If this is a range or list it'll return the maximum of the set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Inclusive selection is not supported when getting a single value for this type.</exception>
    public virtual TValue GetSingleValueOrMaximum()
    {
        return GetSingleValue();
    }

    /// <summary>
    /// Get one value to use. If this is a range or list it'll return the minimum of the set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Inclusive selection is not supported when getting a single value for this type.</exception>
    public virtual TValue GetSingleValueOrMinimum()
    {
        return GetSingleValue();
    }

    /// <inheritdoc />
    public abstract bool Equals(QuestParameterValue<TValue>? other);

    /// <summary>
    /// Convert a parameter value back to a string.
    /// </summary>
    public abstract override string ToString();

    /// <summary>
    /// Write to a JSON writer.
    /// </summary>
    public virtual void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStringValue(ToString());
    }

    string IFormattable.ToString(string format, IFormatProvider formatProvider) => ToString();
}