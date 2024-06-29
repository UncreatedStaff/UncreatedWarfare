using Cysharp.Threading.Tasks;
using System;
using System.Text.Json;

namespace Uncreated.Warfare.NewQuests.Parameters;
public abstract class QuestParameterTemplate<TValue> : IFormattable, IEquatable<QuestParameterTemplate<TValue>>
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

    /// <inheritdoc />
    public bool Equals(QuestParameterTemplate<TValue> other)
    {
        if (other.SelectionType != SelectionType || other.ValueType != ValueType)
            return false;

        if (Set == null)
        {
            return other.Set == null;
        }

        return other.Set != null && Set.Equals(other.Set);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is QuestParameterTemplate<TValue> && Equals(obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return HashCode.Combine(Set, SelectionType, ValueType);
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }

    protected interface IValueSet
    {
        ParameterValueType ValueType { get; }
    }

    protected class ConstantSet(TValue value) : IValueSet, IEquatable<ConstantSet>, IEquatable<IValueSet>
    {
        public readonly TValue Value = value;
        public ParameterValueType ValueType => ParameterValueType.Constant;
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override bool Equals(object? obj) => obj is ConstantSet set && Equals(set);
        public bool Equals(IValueSet other) => other is ConstantSet set && Equals(set);
        public bool Equals(ConstantSet other)
        {
            if (Value == null)
            {
                return other.Value == null;
            }

            return other.Value != null && (Value is IComparable<TValue> comparable ? comparable.Equals(other.Value) : Value.Equals(other.Value));
        }
    }

    protected class RangeSet(TValue minimum, TValue maximum, bool infinityMinimum, bool infinityMaximum) : IValueSet, IEquatable<RangeSet>, IEquatable<IValueSet>
    {
        public readonly TValue Minimum = minimum;
        public readonly TValue Maximum = maximum;
        public readonly bool InfinityMinimum = infinityMinimum;
        public readonly bool InfinityMaximum = infinityMaximum;
        public ParameterValueType ValueType => ParameterValueType.Range;
        public override int GetHashCode()
        {
            return HashCode.Combine(InfinityMinimum, InfinityMaximum, InfinityMinimum ? default : Minimum, InfinityMaximum ? default : Maximum);
        }
        public override bool Equals(object? obj) => obj is RangeSet set && Equals(set);
        public virtual bool Equals(IValueSet other) => other is RangeSet set && Equals(set);
        public virtual bool Equals(RangeSet other)
        {
            return InfinityMaximum == other.InfinityMaximum && (InfinityMaximum || Maximum is not IEquatable<TValue> max ? Equals(Maximum, other.Maximum) : max.Equals(other.Maximum))
                && InfinityMinimum == other.InfinityMinimum && (InfinityMinimum || Minimum is not IEquatable<TValue> min ? Equals(Minimum, other.Minimum) : min.Equals(other.Minimum));
        }
    }

    protected class ListSet(TValue[] values) : IValueSet
    {
        public readonly TValue[] Values = values;
        public ParameterValueType ValueType => ParameterValueType.List;
        public override int GetHashCode()
        {
            if (Values == null)
                return 0;

            HashCode hc = new HashCode();
            hc.Add(Values.Length);
            for (int i = 0; i < Values.Length; ++i)
            {
                hc.Add(Values[i]);
            }

            return hc.ToHashCode();
        }
        public override bool Equals(object? obj) => obj is ListSet set && Equals(set);
        public virtual bool Equals(IValueSet other) => other is ListSet set && Equals(set);
        public virtual bool Equals(ListSet other)
        {
            if (Values == null || Values.Length == 0)
            {
                return other.Values == null || other.Values.Length == 0;
            }

            if (other.Values == null || other.Values.Length != Values.Length)
                return false;

            bool equatable = Values[0] is IEquatable<TValue>;

            for (int i = 0; i < Values.Length; ++i)
            {
                TValue val1 = Values[i];
                TValue val2 = other.Values[i];
                if (val1 == null)
                {
                    if (val2 != null)
                        return false;

                    continue;
                }

                if (val2 == null)
                    return false;

                if (equatable ? !((IEquatable<TValue>)val1).Equals(val2) : !val1.Equals(val2))
                    return false;
            }

            return true;
        }
    }
}
