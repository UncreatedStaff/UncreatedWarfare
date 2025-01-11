using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Quests.Parameters;

/// <summary>
/// Quest paramater template representing a set of possible values for randomly generated quests, or a set of allowed values for conditions.
/// </summary>
[TypeConverter(typeof(Int32ParameterTemplateTypeConverter))]
public class Int32ParameterTemplate : QuestParameterTemplate<int>, IEquatable<Int32ParameterTemplate>
{
    /// <summary>
    /// A parameter value that matches any integer.
    /// </summary>
    public static QuestParameterValue<int> WildcardInclusive { get; } = new Int32ParameterValue(new Int32ParameterTemplate(ParameterSelectionType.Inclusive));

    /// <summary>
    /// Create a template of it's string representation.
    /// </summary>
    public Int32ParameterTemplate(ReadOnlySpan<char> str) : base(str) { }

    /// <summary>
    /// Create a template of a wildcard set of values.
    /// </summary>
    public Int32ParameterTemplate(ParameterSelectionType wildcardType) : base(null, wildcardType) { }

    /// <summary>
    /// Create a template of a constant value.
    /// </summary>
    public Int32ParameterTemplate(int constant) : base(new ConstantSet(constant), ParameterSelectionType.Selective) { }

    /// <summary>
    /// Create a template of a range of values.
    /// </summary>
    public Int32ParameterTemplate(int? minValue, int? maxValue, ParameterSelectionType selectionType, int round = 0)
        : base(minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value
            ? new Int32RangeSet(maxValue.GetValueOrDefault(), minValue.GetValueOrDefault(), false, false, round)
            : new Int32RangeSet(minValue.GetValueOrDefault(), maxValue.GetValueOrDefault(), !minValue.HasValue, !maxValue.HasValue, round), selectionType)
    { }

    /// <summary>
    /// Create a template of a set of values.
    /// </summary>
    public Int32ParameterTemplate(int[] values, ParameterSelectionType selectionType)
        : base(new ListSet(values), selectionType) { }

    protected Int32ParameterTemplate() { }

    protected class Int32RangeSet(int minimum, int maximum, bool infinityMinimum, bool infinityMaximum, int round) : RangeSet(minimum, maximum, infinityMinimum, infinityMaximum)
    {
        public readonly int Round = round;
        public override bool Equals(RangeSet other)
        {
            return other is Int32RangeSet r && r.Round == Round && base.Equals(other);
        }
    }

    /// <inheritdoc />
    public override UniTask<QuestParameterValue<int>> CreateValue(IServiceProvider serviceProvider)
    {
        return UniTask.FromResult<QuestParameterValue<int>>(new Int32ParameterValue(this));
    }

    /// <summary>
    /// Read a saved value of this <see cref="Int32ParameterTemplate"/> from a string.
    /// </summary>
    public static bool TryParseValue(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out QuestParameterValue<int> value)
    {
        return Int32ParameterValue.TryParse(str, out value);
    }

    /// <summary>
    /// Read an <see cref="Int32ParameterTemplate"/> from a string.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out Int32ParameterTemplate template)
    {
        Int32ParameterTemplate val = new Int32ParameterTemplate();
        if (val.TryParseFrom(str))
        {
            template = val;
            return true;
        }

        template = null;
        return false;
    }

    /// <inheritdoc />
    public override bool TryParseFrom(ReadOnlySpan<char> str)
    {
        if (!TryParseIntl(str, out ParameterSelectionType selType, out ParameterValueType valType, out int constant, out int minValue, out int maxValue, out bool minValInf, out bool maxValInf, out int round, out int[]? list))
            return false;

        SelectionType = selType;
        ValueType = valType;
        Set = valType switch
        {
            ParameterValueType.Constant => new ConstantSet(constant),
            ParameterValueType.Range => new Int32RangeSet(minValue, maxValue, minValInf, maxValInf, round),
            ParameterValueType.List => new ListSet(list!),
            _ => null
        };

        return true;
    }

    /// <inheritdoc />
    public override void WriteJson(Utf8JsonWriter writer)
    {
        if (ValueType == ParameterValueType.Constant)
        {
            writer.WriteNumberValue(((ConstantSet?)Set!).Value);
        }
        else
        {
            writer.WriteStringValue(ToString());
        }
    }

    /// <summary>
    /// Read from a JSON reader.
    /// </summary>
    public static Int32ParameterTemplate? ReadJson(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.Number:
                if (!reader.TryGetInt32(out int constant))
                    throw new JsonException("Failed to get integer value from number value for a quest parameter.");

                return new Int32ParameterTemplate(constant);

            case JsonTokenType.String:
                string str = reader.GetString()!;
                return new Int32ParameterTemplate(str.AsSpan());

            default:
                throw new JsonException("Unexpected token while reading integer value for a quest parameter.");
        }
    }

    /// <summary>
    /// Read a value from a JSON reader.
    /// </summary>
    public static QuestParameterValue<int>? ReadValueJson(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.Number:
                if (!reader.TryGetInt32(out int constant))
                    throw new JsonException("Failed to get integer value from number value for a quest parameter.");

                return new Int32ParameterValue(constant);

            case JsonTokenType.String:
                string str = reader.GetString()!;
                if (!TryParseValue(str.AsSpan(), out QuestParameterValue<int>? value))
                    throw new FormatException("Failed to parse quest parameter value.");

                return value;

            default:
                throw new JsonException("Unexpected token while reading integer value for a quest parameter.");
        }
    }

    protected static bool TryParseIntl(ReadOnlySpan<char> str, out ParameterSelectionType selType, out ParameterValueType valType, out int constant, out int minValue, out int maxValue, out bool minValInf, out bool maxValInf, out int round, out int[]? list)
    {
        constant = 0;
        minValInf = false;
        maxValInf = false;
        minValue = 0;
        maxValue = 0;
        list = null;
        round = 0;
        str = str.Trim();
        selType = ParameterSelectionType.Selective;
        valType = ParameterValueType.Wildcard;
        if (str.Length == 2)
        {
            if (str[0] == '$' && str[1] == '*')
            {
                selType = ParameterSelectionType.Selective;
                valType = ParameterValueType.Wildcard;
                return true;
            }

            if (str[0] == '#' && str[1] == '*')
            {
                selType = ParameterSelectionType.Inclusive;
                valType = ParameterValueType.Wildcard;
                return true;
            }
        }

        if (int.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out int result))
        {
            constant = result;
            selType = ParameterSelectionType.Selective;
            valType = ParameterValueType.Constant;
            return true;
        }

        if (str.Length < 3)
            return false;

        bool isInclusive = str[0] == '#';
        if (!isInclusive && str[0] != '$')
            return false;

        bool isRange = str[1] == '(';
        if (isRange)
        {
            int separatorIndex = str.Slice(2).IndexOf(':') + 2;
            int endIndex = separatorIndex + 1 >= str.Length ? -1 : str.Slice(separatorIndex + 1).IndexOf(')') + separatorIndex + 1;
            if (separatorIndex < 2 || endIndex < separatorIndex + 1)
                return false;

            ReadOnlySpan<char> lowerBoundStr = str.Slice(2, separatorIndex - 2).Trim();
            ReadOnlySpan<char> upperBoundStr = str.Slice(separatorIndex + 1, endIndex - separatorIndex - 1).Trim();
            int lowerBound = 0, upperBound = 0;

            if (lowerBoundStr.Length > 0 && !int.TryParse(lowerBoundStr, NumberStyles.Number, CultureInfo.InvariantCulture, out lowerBound))
                return false;

            if (upperBoundStr.Length > 0 && !int.TryParse(upperBoundStr, NumberStyles.Number, CultureInfo.InvariantCulture, out upperBound))
                return false;

            int startRoundRange = endIndex + 1 >= str.Length ? -1 : str.Slice(endIndex + 1).IndexOf('{');
            int endRoundRange = startRoundRange + 1 >= str.Length ? -1 : str.Slice(endIndex + startRoundRange + 2).IndexOf('}');

            if (startRoundRange != -1 && endRoundRange != -1)
            {
                ReadOnlySpan<char> roundStr = str.Slice(startRoundRange + endIndex + 2, endRoundRange).Trim();
                if (!int.TryParse(roundStr, NumberStyles.Number, CultureInfo.InvariantCulture, out round) || round < 0)
                    return false;
            }

            minValue = lowerBound;
            maxValue = upperBound;

            if (minValue > maxValue && upperBoundStr.Length != 0 && lowerBoundStr.Length == 0)
            {
                (minValue, maxValue) = (maxValue, minValue);
                minValInf = upperBoundStr.Length == 0;
                maxValInf = lowerBoundStr.Length == 0;
            }
            else
            {
                minValInf = lowerBoundStr.Length == 0;
                maxValInf = upperBoundStr.Length == 0;
            }

            selType = isInclusive ? ParameterSelectionType.Inclusive : ParameterSelectionType.Selective;
            valType = ParameterValueType.Range;
            return true;
        }

        if (str[1] != '[')
            return false;

        int endInd = 2;
        while (char.IsWhiteSpace(str[endInd]) && endInd + 1 > str.Length)
            ++endInd;

        if (endInd == str.Length - 1 || str[endInd] == ']')
        {
            list = Array.Empty<int>();
            selType = isInclusive ? ParameterSelectionType.Inclusive : ParameterSelectionType.Selective;
            valType = ParameterValueType.List;
            return true;
        }

        int termCount = 1;
        int lastIndex = 1;
        while (true)
        {
            int nextComma = str.Slice(lastIndex + 1).IndexOf(',');
            if (nextComma == -1)
                break;

            lastIndex = nextComma + lastIndex + 1;
            ++termCount;
        }

        list = new int[termCount];
        termCount = 0;
        lastIndex = 1;
        while (true)
        {
            int nextComma = str.Slice(lastIndex + 1).IndexOf(',');
            if (nextComma == -1)
                break;

            if (!int.TryParse(str.Slice(lastIndex + 1, nextComma).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out list[termCount]))
            {
                return false;
            }

            ++termCount;
            lastIndex = nextComma + lastIndex + 1;
        }

        if (!int.TryParse(str.Slice(lastIndex + 1, str.Length - lastIndex - 2).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out list[termCount]))
        {
            return false;
        }

        selType = isInclusive ? ParameterSelectionType.Inclusive : ParameterSelectionType.Selective;
        valType = ParameterValueType.List;
        return true;
    }

    /// <inheritdoc />
    public bool Equals(Int32ParameterTemplate other) => Equals((QuestParameterTemplate<int>)other);

    /// <inheritdoc />
    public override string ToString()
    {
        switch (ValueType)
        {
            default: // Wildcard
                return SelectionType == ParameterSelectionType.Inclusive ? "#*" : "$*";

            case ParameterValueType.Constant:
                return ((ConstantSet)Set!).Value.ToString(CultureInfo.InvariantCulture);

            case ParameterValueType.Range:
                RangeSet range = (RangeSet?)Set!;
                int size = 4;
                if (!range.InfinityMinimum)
                {
                    size += MathUtility.CountDigits(range.Minimum);
                    if (range.Minimum < 0)
                        ++size;
                }
                if (!range.InfinityMaximum)
                {
                    size += MathUtility.CountDigits(range.Maximum);
                    if (range.Maximum < 0)
                        ++size;
                }

                if (range is Int32RangeSet { Round: not 0 } round)
                {
                    size += 2 + MathUtility.CountDigits(round.Round);
                }

                return string.Create(size, this, (span, state) =>
                {
                    span[0] = state.SelectionType == ParameterSelectionType.Inclusive ? '#' : '$';
                    span[1] = '(';

                    int index = 2;
                    RangeSet range = (RangeSet?)state.Set!;
                    if (!range.InfinityMinimum)
                    {
                        range.Minimum.TryFormat(span[index..], out int charsWritten, "F0", CultureInfo.InvariantCulture);
                        index += charsWritten;
                    }

                    span[index] = ':';
                    ++index;
                    if (!range.InfinityMaximum)
                    {
                        range.Maximum.TryFormat(span[index..], out int charsWritten, "F0", CultureInfo.InvariantCulture);
                        index += charsWritten;
                    }

                    span[index] = ')';
                    ++index;

                    if (range is Int32RangeSet { Round: not 0 } round)
                    {
                        span[index] = '{';
                        ++index;
                        round.Round.TryFormat(span[index..], out int charsWritten, "F0", CultureInfo.InvariantCulture);
                        index += charsWritten;
                        span[index] = '}';
                    }
                });

            case ParameterValueType.List:
                ListSet list = (ListSet?)Set!;
                if (list.Values.Length == 0)
                {
                    return SelectionType == ParameterSelectionType.Inclusive ? "#[]" : "$[]";
                }

                size = 3 + (list.Values.Length - 1);
                for (int i = 0; i < list.Values.Length; ++i)
                {
                    int v = list.Values[i];
                    size += MathUtility.CountDigits(v);
                    if (v < 0)
                        ++size;
                }

                return string.Create(size, this, (span, state) =>
                {
                    span[0] = state.SelectionType == ParameterSelectionType.Inclusive ? '#' : '$';
                    span[1] = '[';

                    ListSet list = (ListSet?)Set!;

                    int index = 2;
                    for (int i = 0; i < list.Values.Length; ++i)
                    {
                        int v = list.Values[i];

                        if (i != 0)
                        {
                            span[index] = ',';
                            ++index;
                        }

                        v.TryFormat(span[index..], out int charsWritten, "F0", CultureInfo.InvariantCulture);
                        index += charsWritten;
                    }

                    span[index] = ']';
                });

        }
    }

    [JsonConverter(typeof(QuestParameterConverter))]
    protected class Int32ParameterValue : QuestParameterValue<int>, IEquatable<Int32ParameterValue>
    {
        private int _value;
        private int[]? _values;
        private int _minValue;
        private int _maxValue;
        private int _round;
        private bool _isEmptySet;

        private Int32ParameterValue() { }

        public Int32ParameterValue(int constant)
        {
            _value = constant;
            SelectionType = ParameterSelectionType.Selective;
            ValueType = ParameterValueType.Constant;
        }

        public Int32ParameterValue(Int32ParameterTemplate template)
        {
            ParameterValueType valType = template.ValueType;
            ParameterSelectionType selType = template.SelectionType;

            ValueType = valType;
            SelectionType = selType;

            switch (valType)
            {
                case ParameterValueType.Wildcard when selType == ParameterSelectionType.Selective:
                    _value = RandomUtility.GetInteger();
                    break;

                case ParameterValueType.Constant:
                    _value = ((ConstantSet?)template.Set!).Value;
                    break;

                case ParameterValueType.Range:
                    RangeSet range = (RangeSet)template.Set!;
                    int min = range.InfinityMinimum ? int.MinValue : range.Minimum,
                        max = range.InfinityMaximum ? int.MaxValue : range.Maximum;
                    if (selType == ParameterSelectionType.Inclusive)
                    {
                        _minValue = min;
                        _maxValue = max;
                        _round = range is Int32RangeSet round ? round.Round : 0;
                    }
                    else
                    {
                        _value = RandomUtility.GetInteger(min, max + 1);
                        if (range is Int32RangeSet round)
                        {
                            _value = MathUtility.RoundNumber(_value, round.Round, min, max);
                            _round = round.Round;
                        }
                        else
                            _round = 0;
                    }

                    break;

                case ParameterValueType.List:
                    ListSet list = (ListSet?)template.Set!;
                    if (selType == ParameterSelectionType.Inclusive)
                    {
                        _values = list.Values;
                    }
                    else if (list.Values.Length > 0)
                    {
                        _value = list.Values[RandomUtility.GetIndex((ICollection)list.Values)];
                    }
                    else
                    {
                        _isEmptySet = true;
                    }

                    break;
            }
        }
        public static bool TryParse(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out QuestParameterValue<int> value)
        {
            if (!TryParseIntl(str, out ParameterSelectionType selType, out ParameterValueType valType, out int constant,
                    out int minValue, out int maxValue, out bool minValInf, out bool maxValInf, out int round,
                    out int[]? list))
            {
                value = null;
                return false;
            }

            Int32ParameterValue val = new Int32ParameterValue
            {
                SelectionType = selType,
                ValueType = valType
            };

            switch (valType)
            {
                case ParameterValueType.Constant:
                    val._value = constant;
                    break;

                case ParameterValueType.Wildcard when selType == ParameterSelectionType.Selective:
                    val._value = RandomUtility.GetInteger();
                    break;

                case ParameterValueType.Range:
                    int min = minValInf ? int.MinValue : minValue,
                        max = maxValInf ? int.MaxValue : maxValue;

                    val._round = round;

                    if (selType == ParameterSelectionType.Inclusive)
                    {
                        val._minValue = min;
                        val._maxValue = max;
                    }
                    else
                    {
                        val._value = RandomUtility.GetInteger(min, max + 1);
                        if (round != 0)
                        {
                            val._value = MathUtility.RoundNumber(val._value, round, min, max);
                        }
                    }

                    break;

                case ParameterValueType.List:
                    if (selType == ParameterSelectionType.Inclusive)
                    {
                        val._values = list;
                    }
                    else if (list!.Length > 0)
                    {
                        val._value = list[RandomUtility.GetIndex((ICollection)list)];
                    }
                    else
                    {
                        val._isEmptySet = true;
                    }

                    break;
            }

            value = val;
            return true;
        }
        public override bool IsMatch(int otherValue)
        {
            ParameterValueType valType = ValueType;

            // Wildcard
            if (valType is < ParameterValueType.Constant or > ParameterValueType.List)
            {
                return true;
            }

            ParameterSelectionType selType = SelectionType;

            if (valType == ParameterValueType.Constant || selType == ParameterSelectionType.Selective)
            {
                return !_isEmptySet && _value == otherValue;
            }

            if (valType == ParameterValueType.Range)
            {
                return otherValue >= _minValue && otherValue <= _maxValue;
            }

            // List
            for (int i = 0; i < _values!.Length; ++i)
            {
                if (_values[i] == otherValue)
                    return true;
            }

            return false;
        }

        public override int GetSingleValue()
        {
            if (ValueType != ParameterValueType.Constant && SelectionType != ParameterSelectionType.Selective)
                throw new InvalidOperationException("Not a selective or constant parameter value.");

            return _value;
        }

        public override int GetSingleValueOrMaximum()
        {
            if (SelectionType == ParameterSelectionType.Selective || ValueType == ParameterValueType.Constant)
                return _value;

            if (ValueType == ParameterValueType.Range)
                return _maxValue;

            if (ValueType != ParameterValueType.List || _isEmptySet)
                return int.MaxValue;

            int max = int.MinValue;
            for (int i = 0; i < _values!.Length; ++i)
            {
                if (max < _values[i])
                    max = _values[i];
            }

            return max;
        }

        public override int GetSingleValueOrMinimum()
        {
            if (SelectionType == ParameterSelectionType.Selective || ValueType == ParameterValueType.Constant)
                return _value;

            if (ValueType == ParameterValueType.Range)
                return _maxValue;

            if (ValueType != ParameterValueType.List || _isEmptySet)
                return int.MinValue;

            int min = int.MaxValue;
            for (int i = 0; i < _values!.Length; ++i)
            {
                if (min > _values[i])
                    min = _values[i];
            }

            return min;
        }

        public override void WriteJson(Utf8JsonWriter writer)
        {
            if (!_isEmptySet && (SelectionType == ParameterSelectionType.Selective || ValueType == ParameterValueType.Constant))
            {
                writer.WriteNumberValue(_value);
            }
            else
            {
                writer.WriteStringValue(ToString());
            }
        }

        /// <inheritdoc />
        public override object GetDisplayString(ITranslationValueFormatter formatter)
        {
            if (ValueType == ParameterValueType.Constant || SelectionType == ParameterSelectionType.Selective)
            {
                return _value;
            }

            return ToString();
        }

        /// <inheritdoc />
        public override bool Equals(QuestParameterValue<int>? other)
        {
            return other is Int32ParameterValue v && Equals(v);
        }

        /// <inheritdoc />
        public bool Equals(Int32ParameterValue? other)
        {
            if (other == null)
                return false;

            if (ValueType == ParameterValueType.Constant || SelectionType == ParameterSelectionType.Selective)
                return _value == other._value;

            if (ValueType != other.ValueType)
                return false;

            if (ValueType == ParameterValueType.Wildcard && other.ValueType == ParameterValueType.Wildcard)
                return true;

            if (ValueType == ParameterValueType.Range)
            {
                return _maxValue == other._maxValue && _minValue == other._minValue;
            }

            if (_isEmptySet || _values == null || _values.Length == 0)
            {
                return other._isEmptySet || other._values == null || other._values.Length == 0;
            }

            if (other._isEmptySet || other._values == null || other._values.Length == 0)
                return false;

            for (int i = 0; i < _values.Length; ++i)
            {
                if (_values[i] != other._values[i])
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            if (SelectionType == ParameterSelectionType.Selective)
            {
                return _isEmptySet ? "$[]" : _value.ToString(CultureInfo.InvariantCulture);
            }

            switch (ValueType)
            {
                default: // Wildcard
                    return "#*";

                case ParameterValueType.Constant:
                    return _value.ToString(CultureInfo.InvariantCulture);

                case ParameterValueType.Range:
                    int size = 4;
                    if (_minValue != int.MinValue)
                    {
                        size += MathUtility.CountDigits(_minValue);
                        if (_minValue < 0)
                            ++size;
                    }
                    if (_maxValue != int.MaxValue)
                    {
                        size += MathUtility.CountDigits(_maxValue);
                        if (_maxValue < 0)
                            ++size;
                    }

                    if (_round != 0)
                    {
                        size += 2 + MathUtility.CountDigits(_round);
                    }

                    return string.Create(size, this, (span, state) =>
                    {
                        span[0] = '#';
                        span[1] = '(';

                        int index = 2;
                        if (state._minValue != int.MinValue)
                        {
                            state._minValue.TryFormat(span[index..], out int charsWritten, "F0", CultureInfo.InvariantCulture);
                            index += charsWritten;
                        }

                        span[index] = ':';
                        ++index;
                        if (state._maxValue != int.MaxValue)
                        {
                            state._maxValue.TryFormat(span[index..], out int charsWritten, "F0", CultureInfo.InvariantCulture);
                            index += charsWritten;
                        }

                        span[index] = ')';
                        ++index;

                        if (state._round != 0)
                        {
                            span[index] = '{';
                            ++index;
                            state._round.TryFormat(span[index..], out int charsWritten, "F0", CultureInfo.InvariantCulture);
                            index += charsWritten;
                            span[index] = '}';
                        }
                    });

                case ParameterValueType.List:
                    if (_values!.Length == 0)
                    {
                        return "#[]";
                    }

                    size = 3 + (_values.Length - 1);
                    for (int i = 0; i < _values.Length; ++i)
                    {
                        int v = _values[i];
                        size += MathUtility.CountDigits(v);
                        if (v < 0)
                            ++size;
                    }

                    return string.Create(size, this, (span, state) =>
                    {
                        span[0] = '#';
                        span[1] = '[';
                        
                        int index = 2;
                        for (int i = 0; i < state._values!.Length; ++i)
                        {
                            int v = state._values[i];

                            if (i != 0)
                            {
                                span[index] = ',';
                                ++index;
                            }

                            v.TryFormat(span[index..], out int charsWritten, "F0", CultureInfo.InvariantCulture);
                            index += charsWritten;
                        }

                        span[index] = ']';
                    });

            }
        }
    }
}