using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.NewQuests.Parameters;

/// <summary>
/// Quest paramater template representing a set of possible values for randomly generated quests, or a set of allowed values for conditions.
/// </summary>
[TypeConverter(typeof(SingleParameterTemplateTypeConverter))]
public class SingleParameterTemplate : QuestParameterTemplate<float>, IEquatable<SingleParameterTemplate>
{
    /// <summary>
    /// A parameter value that matches any integer.
    /// </summary>
    public static QuestParameterValue<float> WildcardInclusive { get; } = new SingleParameterValue(new SingleParameterTemplate(ParameterSelectionType.Inclusive));

    /// <summary>
    /// Create a template of it's string representation.
    /// </summary>
    public SingleParameterTemplate(ReadOnlySpan<char> str) : base(str) { }

    /// <summary>
    /// Create a template of a wildcard set of values.
    /// </summary>
    public SingleParameterTemplate(ParameterSelectionType wildcardType) : base(null, wildcardType) { }

    /// <summary>
    /// Create a template of a constant value.
    /// </summary>
    public SingleParameterTemplate(float constant) : base(new ConstantSet(constant), ParameterSelectionType.Selective) { }

    /// <summary>
    /// Create a template of a range of values.
    /// </summary>
    public SingleParameterTemplate(float? minValue, float? maxValue, ParameterSelectionType selectionType, int round = 0)
        : base(minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value
            ? new SingleRangeSet(maxValue.GetValueOrDefault(), minValue.GetValueOrDefault(), false, false, round)
            : new SingleRangeSet(minValue.GetValueOrDefault(), maxValue.GetValueOrDefault(), !minValue.HasValue, !maxValue.HasValue, round), selectionType) { }
    
    /// <summary>
    /// Create a template of a set of values.
    /// </summary>
    public SingleParameterTemplate(float[] values, ParameterSelectionType selectionType)
        : base(new ListSet(values), selectionType) { }

    protected SingleParameterTemplate() { }

    protected class SingleRangeSet(float minimum, float maximum, bool infinityMinimum, bool infinityMaximum, int round) : RangeSet(minimum, maximum, infinityMinimum, infinityMaximum)
    {
        public readonly int Round = round;
        public override bool Equals(RangeSet other)
        {
            return other is SingleRangeSet r && r.Round == Round && base.Equals(other);
        }
    }

    /// <inheritdoc />
    public override UniTask<QuestParameterValue<float>> CreateValue(IServiceProvider serviceProvider)
    {
        return UniTask.FromResult<QuestParameterValue<float>>(new SingleParameterValue(this));
    }

    /// <summary>
    /// Read a saved value of this <see cref="SingleParameterTemplate"/> from a string.
    /// </summary>
    public static bool TryParseValue(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out QuestParameterValue<float> value)
    {
        return SingleParameterValue.TryParse(str, out value);
    }

    /// <summary>
    /// Read a <see cref="SingleParameterTemplate"/> from a string.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out SingleParameterTemplate template)
    {
        SingleParameterTemplate val = new SingleParameterTemplate();
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
        if (!TryParseIntl(str, out ParameterSelectionType selType, out ParameterValueType valType, out float constant, out float minValue, out float maxValue, out bool minValInf, out bool maxValInf, out int round, out float[]? list))
            return false;

        SelectionType = selType;
        ValueType = valType;
        Set = valType switch
        {
            ParameterValueType.Constant => new ConstantSet(constant),
            ParameterValueType.Range => new SingleRangeSet(minValue, maxValue, minValInf, maxValInf, round),
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
    public static SingleParameterTemplate? ReadJson(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.Number:
                if (!reader.TryGetSingle(out float constant))
                    throw new JsonException("Failed to get decimal value from number value for a quest parameter.");

                return new SingleParameterTemplate(constant);

            case JsonTokenType.String:
                string str = reader.GetString()!;
                return new SingleParameterTemplate(str.AsSpan());

            default:
                throw new JsonException("Unexpected token while reading decimal value for a quest parameter.");
        }
    }

    /// <summary>
    /// Read a value from a JSON reader.
    /// </summary>
    public static QuestParameterValue<float>? ReadValueJson(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.Number:
                if (!reader.TryGetInt32(out int constant))
                    throw new JsonException("Failed to get decimal value from number value for a quest parameter.");

                return new SingleParameterValue(constant);

            case JsonTokenType.String:
                string str = reader.GetString()!;
                if (!TryParseValue(str.AsSpan(), out QuestParameterValue<float>? value))
                    throw new FormatException("Failed to parse quest parameter value.");

                return value;

            default:
                throw new JsonException("Unexpected token while reading decimal value for a quest parameter.");
        }
    }

    protected static bool TryParseIntl(ReadOnlySpan<char> str, out ParameterSelectionType selType, out ParameterValueType valType, out float constant, out float minValue, out float maxValue, out bool minValInf, out bool maxValInf, out int round, out float[]? list)
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

        if (float.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out float result))
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

            ReadOnlySpan<char> lowerBoundStr = str.Slice(2, separatorIndex - 3).Trim();
            ReadOnlySpan<char> upperBoundStr = str.Slice(separatorIndex + 1, endIndex - separatorIndex - 1).Trim();
            float lowerBound = 0, upperBound = 0;

            if (lowerBoundStr.Length > 0 && !float.TryParse(lowerBoundStr, NumberStyles.Number, CultureInfo.InvariantCulture, out lowerBound))
                return false;

            if (upperBoundStr.Length > 0 && !float.TryParse(upperBoundStr, NumberStyles.Number, CultureInfo.InvariantCulture, out upperBound))
                return false;

            int startRoundRange = endIndex + 1 >= str.Length ? -1 : str.Slice(endIndex + 1).IndexOf('{');
            int endRoundRange = startRoundRange + 1 >= str.Length ? -1 : str.Slice(startRoundRange + 1).IndexOf('}');

            if (startRoundRange != -1 && endRoundRange != -1)
            {
                ReadOnlySpan<char> roundStr = str.Slice(startRoundRange + endIndex + 2, endRoundRange).Trim();
                if (!int.TryParse(roundStr, NumberStyles.Number, CultureInfo.InvariantCulture, out round))
                    return false;
            }

            minValue = lowerBound;
            maxValue = upperBound;

            if (minValue > maxValue)
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
            list = Array.Empty<float>();
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

        list = new float[termCount];
        termCount = 0;
        lastIndex = 1;
        while (true)
        {
            int nextComma = str.Slice(lastIndex + 1).IndexOf(',');
            if (nextComma == -1)
                break;

            if (!float.TryParse(str.Slice(lastIndex + 1, nextComma).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out list[termCount]))
            {
                return false;
            }

            ++termCount;
            lastIndex = nextComma + lastIndex + 1;
        }

        if (!float.TryParse(str.Slice(lastIndex + 1, str.Length - lastIndex - 2).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out list[termCount]))
        {
            return false;
        }

        selType = isInclusive ? ParameterSelectionType.Inclusive : ParameterSelectionType.Selective;
        valType = ParameterValueType.List;
        return true;
    }

    /// <inheritdoc />
    public bool Equals(SingleParameterTemplate other) => Equals((QuestParameterTemplate<float>)other);


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
                if (range is SingleRangeSet { Round: not 0 } round)
                    return $"{(SelectionType == ParameterSelectionType.Inclusive ? "#" : "$")}({(range.InfinityMinimum ? string.Empty : range.Minimum.ToString(CultureInfo.InvariantCulture))}:{(range.InfinityMaximum ? string.Empty : range.Maximum.ToString(CultureInfo.InvariantCulture))}){{{round.Round.ToString(CultureInfo.InvariantCulture)}}}";
                
                return $"{(SelectionType == ParameterSelectionType.Inclusive ? "#" : "$")}({(range.InfinityMinimum ? string.Empty : range.Minimum.ToString(CultureInfo.InvariantCulture))}:{(range.InfinityMaximum ? string.Empty : range.Maximum.ToString(CultureInfo.InvariantCulture))})";

            case ParameterValueType.List:
                ListSet list = (ListSet?)Set!;
                if (list.Values.Length == 0)
                {
                    return SelectionType == ParameterSelectionType.Inclusive ? "#[]" : "$[]";
                }

                StringBuilder sb = new StringBuilder(SelectionType == ParameterSelectionType.Inclusive ? "#[" : "$[", 0, 2, 3 + 8 * (list.Values.Length - 1));

                Span<char> formatBuffer = stackalloc char[12];

                for (int i = 0; i < list.Values.Length; ++i)
                {
                    float v = list.Values[i];

                    if (i != 0)
                        sb.Append(", ");

                    if (!v.TryFormat(formatBuffer, out int charsWritten, provider: CultureInfo.InvariantCulture))
                    {
                        sb.Append(v.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(formatBuffer.Slice(0, charsWritten));
                    }
                }

                return sb.ToString();
        }
    }

    protected class SingleParameterValue : QuestParameterValue<float>, IEquatable<SingleParameterValue>
    {
        private float _value;
        private float[]? _values;
        private float _minValue;
        private float _maxValue;
        private int _round;
        private bool _isEmptySet;

        private SingleParameterValue() { }

        public SingleParameterValue(float constant)
        {
            _value = constant;
            SelectionType = ParameterSelectionType.Selective;
            ValueType = ParameterValueType.Constant;
        }

        public SingleParameterValue(SingleParameterTemplate template)
        {
            ParameterValueType valType = template.ValueType;
            ParameterSelectionType selType = template.SelectionType;

            ValueType = valType;
            SelectionType = selType;

            switch (valType)
            {
                case ParameterValueType.Constant:
                    _value = ((ConstantSet?)template.Set!).Value;
                    break;

                case ParameterValueType.Wildcard when selType == ParameterSelectionType.Selective:
                    _value = RandomUtility.GetFloat();
                    break;

                case ParameterValueType.Range:
                    RangeSet range = (RangeSet)template.Set!;

                    float min = range.InfinityMinimum ? float.NaN : range.Minimum,
                          max = range.InfinityMaximum ? float.NaN : range.Maximum;

                    if (selType == ParameterSelectionType.Inclusive)
                    {
                        _minValue = min;
                        _maxValue = max;
                        _round = range is SingleRangeSet round ? round.Round : 0;
                    }
                    else
                    {
                        if (float.IsNaN(min))
                            min = float.MinValue;
                        if (float.IsNaN(max))
                            max = float.MaxValue;
                        _value = RandomUtility.GetFloat(min, max);
                        if (range is SingleRangeSet round)
                        {
                            _value = (float)MathUtility.RoundNumber(_value, round.Round, min, max);
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

        public static bool TryParse(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out QuestParameterValue<float> value)
        {
            if (!TryParseIntl(str, out ParameterSelectionType selType, out ParameterValueType valType, out float constant,
                    out float minValue, out float maxValue, out bool minValInf, out bool maxValInf, out int round,
                    out float[]? list))
            {
                value = null;
                return false;
            }

            SingleParameterValue val = new SingleParameterValue
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
                    val._value = RandomUtility.GetFloat();
                    break;

                case ParameterValueType.Range:
                    float min = minValInf ? float.NaN : minValue,
                          max = maxValInf ? float.NaN : maxValue;
                    val._round = round;
                    if (selType == ParameterSelectionType.Inclusive)
                    {
                        val._minValue = min;
                        val._maxValue = max;
                    }
                    else
                    {
                        if (float.IsNaN(min))
                            min = float.MinValue;
                        if (float.IsNaN(max))
                            max = float.MaxValue;
                        val._value = RandomUtility.GetFloat(min, max);
                        if (round != 0)
                        {
                            val._value = (float)MathUtility.RoundNumber(val._value, round, min, max);
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

        public override bool IsMatch(float otherValue)
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
                return !_isEmptySet && Math.Abs(_value - otherValue) < 0.001f;
            }

            if (valType == ParameterValueType.Range)
            {
                return (float.IsNaN(_minValue) || otherValue >= _minValue) && (float.IsNaN(_maxValue) || otherValue <= _maxValue);
            }

            // List
            for (int i = 0; i < _values!.Length; ++i)
            {
                if (Math.Abs(_values[i] - otherValue) < 0.001f)
                    return true;
            }

            return false;
        }

        public override float GetSingleValue()
        {
            if (ValueType != ParameterValueType.Constant && SelectionType != ParameterSelectionType.Selective)
                throw new InvalidOperationException("Not a selective or constant parameter value.");

            return _value;
        }

        public override float GetSingleValueOrMaximum()
        {
            if (SelectionType == ParameterSelectionType.Selective || ValueType == ParameterValueType.Constant)
                return _value;

            if (ValueType == ParameterValueType.Range)
                return _maxValue;

            if (ValueType != ParameterValueType.List || _isEmptySet)
                return float.MaxValue;

            float max = float.MinValue;
            for (int i = 0; i < _values!.Length; ++i)
            {
                if (i == 0 || max < _values[i])
                    max = _values[i];
            }

            return max;
        }

        public override float GetSingleValueOrMinimum()
        {
            if (SelectionType == ParameterSelectionType.Selective || ValueType == ParameterValueType.Constant)
                return _value;

            if (ValueType == ParameterValueType.Range)
                return _maxValue;

            if (ValueType != ParameterValueType.List || _isEmptySet)
                return float.MinValue;

            float min = float.MaxValue;
            for (int i = 0; i < _values!.Length; ++i)
            {
                if (i == 0 || min > _values[i])
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
        public override bool Equals(QuestParameterValue<float> other)
        {
            return other is SingleParameterValue v && Equals(v);
        }

        /// <inheritdoc />
        public bool Equals(SingleParameterValue other)
        {
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
                    if (_round != 0)
                        return $"#({(float.IsNaN(_minValue) ? string.Empty : _minValue.ToString(CultureInfo.InvariantCulture))}:{(float.IsNaN(_maxValue) ? string.Empty : _maxValue.ToString(CultureInfo.InvariantCulture))}){{{_round.ToString(CultureInfo.InvariantCulture)}}}";

                    return $"#({(float.IsNaN(_minValue) ? string.Empty : _minValue.ToString(CultureInfo.InvariantCulture))}:{(float.IsNaN(_maxValue) ? string.Empty : _maxValue.ToString(CultureInfo.InvariantCulture))})";

                case ParameterValueType.List:
                    if (_values!.Length == 0)
                    {
                        return "#[]";
                    }

                    StringBuilder sb = new StringBuilder("#[", 0, 2, 3 + 8 * (_values.Length - 1));

                    Span<char> formatBuffer = stackalloc char[12];

                    for (int i = 0; i < _values.Length; ++i)
                    {
                        float v = _values[i];

                        if (i != 0)
                            sb.Append(", ");

                        if (!v.TryFormat(formatBuffer, out int charsWritten, provider: CultureInfo.InvariantCulture))
                        {
                            sb.Append(v.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(formatBuffer.Slice(0, charsWritten));
                        }
                    }

                    return sb.ToString();
            }
        }
    }
}