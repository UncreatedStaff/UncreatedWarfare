using Cysharp.Threading.Tasks;
using System;
using System.Text.Json;

namespace Uncreated.Warfare.NewQuests.Parameters;
public abstract class QuestParameterTemplate<TValue> : IFormattable
{
    protected IValueSet? Set;

    /// <summary>
    /// Type of value set. Constant, list, wildcard, and sometimes range.
    /// </summary>
    public ParameterValueType ValueType { get; protected set; }

    /// <summary>
    /// Selection style of the parameter. Inclusive allows *any* value in the set, where selective selects *one* value in the set.
    /// </summary>
    public ParameterSelectionType SelectionType { get; protected set; }

    protected QuestParameterTemplate() { }
    protected QuestParameterTemplate(ReadOnlySpan<char> str)
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        if (!TryParseFrom(str))
            throw new FormatException("Failed to parse quest parameter template.");
    }

    protected QuestParameterTemplate(IValueSet? set, ParameterSelectionType selectionType)
    {
        Set = set;
        ValueType = set?.ValueType ?? ParameterValueType.Wildcard;
        SelectionType = selectionType;
    }

    /// <summary>
    /// Creates a random value from this selective set, or a set of values if this is an inclusive set.
    /// </summary>
    public abstract UniTask<QuestParameterValue<TValue>> CreateValue(IServiceProvider serviceProvider);

    /// <summary>
    /// Read a parameter template from a string.
    /// </summary>
    public abstract bool TryParseFrom(ReadOnlySpan<char> str);

    /// <summary>
    /// Convert a parameter template back to a string.
    /// </summary>
    public abstract override string ToString();

    /// <summary>
    /// Write to a JSON writer.
    /// </summary>
    public virtual void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStringValue(ToString());
    }

    /// <inheritdoc />
    string IFormattable.ToString(string format, IFormatProvider formatProvider) => ToString();

    protected interface IValueSet
    {
        ParameterValueType ValueType { get; }
    }

    protected class ConstantSet(TValue value) : IValueSet
    {
        public readonly TValue Value = value;
        public ParameterValueType ValueType => ParameterValueType.Constant;
    }

    protected class RangeSet(TValue minimum, TValue maximum, bool infinityMinimum, bool infinityMaximum) : IValueSet
    {
        public readonly TValue Minimum = minimum;
        public readonly TValue Maximum = maximum;
        public readonly bool InfinityMinimum = infinityMinimum;
        public readonly bool InfinityMaximum = infinityMaximum;
        public ParameterValueType ValueType => ParameterValueType.Range;
    }

    protected class ListSet(TValue[] values) : IValueSet
    {
        public readonly TValue[] Values = values;
        public ParameterValueType ValueType => ParameterValueType.List;
    }
}
