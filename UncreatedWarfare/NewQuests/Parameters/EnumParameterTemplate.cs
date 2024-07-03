using Cysharp.Threading.Tasks;
using DanielWillett.ReflectionTools;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.NewQuests.Parameters;

/// <summary>
/// Quest paramater template representing a set of possible values for randomly generated quests, or a set of allowed values for conditions.
/// </summary>
[TypeConverter(typeof(EnumParameterTemplateTypeConverter))]
public class EnumParameterTemplate<TEnum> : QuestParameterTemplate<TEnum>, IEquatable<EnumParameterTemplate<TEnum>> where TEnum : unmanaged, Enum
{
    /// <summary>
    /// A parameter value that matches any enum of type <typeparamref name="TEnum"/>.
    /// </summary>
    public static QuestParameterValue<TEnum> WildcardInclusive { get; } = new EnumParameterValue(new EnumParameterTemplate<TEnum>(ParameterSelectionType.Inclusive));

    /// <summary>
    /// Create a template of it's string representation.
    /// </summary>
    public EnumParameterTemplate(ReadOnlySpan<char> str) : base(str) { }

    /// <summary>
    /// Create a template of it's string representation.
    /// </summary>
    [UsedImplicitly]
    public EnumParameterTemplate(string str) : base(str.AsSpan()) { }

    /// <summary>
    /// Create a template of a wildcard set of values.
    /// </summary>
    public EnumParameterTemplate(ParameterSelectionType wildcardType) : base(null, wildcardType) { }

    /// <summary>
    /// Create a template of a constant value.
    /// </summary>
    public EnumParameterTemplate(TEnum constant) : base(new ConstantSet(constant), ParameterSelectionType.Selective) { }

    /// <summary>
    /// Create a template of a range of values.
    /// </summary>
    public EnumParameterTemplate(TEnum? minValue, TEnum? maxValue, ParameterSelectionType selectionType)
        : base(minValue.HasValue && maxValue.HasValue && minValue.Value.CompareTo(maxValue.Value) > 0
            ? new RangeSet(maxValue.GetValueOrDefault(), minValue.GetValueOrDefault(), false, false)
            : new RangeSet(minValue.GetValueOrDefault(), maxValue.GetValueOrDefault(), !minValue.HasValue, !maxValue.HasValue), selectionType) { }

    /// <summary>
    /// Create a template of a set of values.
    /// </summary>
    public EnumParameterTemplate(TEnum[] values, ParameterSelectionType selectionType) : base(new ListSet(values), selectionType) { }

    protected EnumParameterTemplate() { }

    /// <inheritdoc />
    public override UniTask<QuestParameterValue<TEnum>> CreateValue(IServiceProvider serviceProvider)
    {
        return UniTask.FromResult<QuestParameterValue<TEnum>>(new EnumParameterValue(this));
    }

    /// <summary>
    /// Read a saved value of this <see cref="StringParameterTemplate"/> from a string.
    /// </summary>
    public static bool TryParseValue(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out QuestParameterValue<TEnum> value)
    {
        return EnumParameterValue.TryParse(str, out value);
    }

    /// <summary>
    /// Read a <see cref="StringParameterTemplate"/> from a string.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out EnumParameterTemplate<TEnum> template)
    {
        EnumParameterTemplate<TEnum> val = new EnumParameterTemplate<TEnum>();
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
        if (!TryParseIntl(str, out ParameterSelectionType selType, out ParameterValueType valType, out TEnum constant, out TEnum minValue, out TEnum maxValue, out bool minValInf, out bool maxValInf, out TEnum[]? list))
            return false;

        SelectionType = selType;
        ValueType = valType;
        Set = valType switch
        {
            ParameterValueType.Constant => new ConstantSet(constant),
            ParameterValueType.Range => new RangeSet(minValue, maxValue, minValInf, maxValInf),
            ParameterValueType.List => new ListSet(list!),
            _ => null
        };

        return true;
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

            case JsonTokenType.String:
                string str = reader.GetString()!;
                return new SingleParameterTemplate(str.AsSpan());

            default:
                throw new JsonException($"Unexpected token while reading {Accessor.ExceptionFormatter.Format(typeof(TEnum))} enum value for a quest parameter.");
        }
    }

    /// <summary>
    /// Read a value from a JSON reader.
    /// </summary>
    public static QuestParameterValue<TEnum>? ReadValueJson(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.String:
                string str = reader.GetString()!;
                if (!TryParseValue(str.AsSpan(), out QuestParameterValue<TEnum>? value))
                    throw new FormatException("Failed to parse quest parameter value.");

                return value;

            default:
                throw new JsonException($"Unexpected token while reading {Accessor.ExceptionFormatter.Format(typeof(TEnum))} enum value for a quest parameter.");
        }
    }

    protected static bool TryParseIntl(ReadOnlySpan<char> str, out ParameterSelectionType selType, out ParameterValueType valType, out TEnum constant, out TEnum minValue, out TEnum maxValue, out bool minValInf, out bool maxValInf, out TEnum[]? list)
    {
        constant = default;
        minValInf = false;
        maxValInf = false;
        minValue = default;
        maxValue = default;
        list = null;
        str = str.Trim();
        selType = ParameterSelectionType.Selective;
        valType = ParameterValueType.Wildcard;
        if (str.Length == 2)
        {
            if (str[0] == '$' && str[1] == '*')
            {
                selType = ParameterSelectionType.Selective;
                valType = ParameterValueType.Wildcard;
                return false;
            }

            if (str[0] == '#' && str[1] == '*')
            {
                selType = ParameterSelectionType.Inclusive;
                valType = ParameterValueType.Wildcard;
                return true;
            }
        }

        if (Enum.TryParse(new string(str), true, out TEnum enumVal))
        {
            constant = enumVal;
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
            TEnum lowerBound = default, upperBound = default;

            if (lowerBoundStr.Length > 0 && !Enum.TryParse(new string(lowerBoundStr), true, out lowerBound))
                return false;

            if (upperBoundStr.Length > 0 && !Enum.TryParse(new string(upperBoundStr), true, out upperBound))
                return false;

            minValue = lowerBound;
            maxValue = upperBound;

            if (minValue.CompareTo(maxValue) > 0)
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
            list = Array.Empty<TEnum>();
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

        list = new TEnum[termCount];
        termCount = 0;
        lastIndex = 1;
        while (true)
        {
            int nextComma = str.Slice(lastIndex + 1).IndexOf(',');
            if (nextComma == -1)
                break;

            if (!Enum.TryParse(new string(str.Slice(lastIndex + 1, nextComma).Trim()), true, out list[termCount]))
            {
                return false;
            }

            ++termCount;
            lastIndex = nextComma + lastIndex + 1;
        }

        if (!Enum.TryParse(new string(str.Slice(lastIndex + 1, str.Length - lastIndex - 2).Trim()), true, out list[termCount]))
        {
            return false;
        }

        selType = isInclusive ? ParameterSelectionType.Inclusive : ParameterSelectionType.Selective;
        valType = ParameterValueType.List;
        return true;
    }

    /// <inheritdoc />
    public bool Equals(EnumParameterTemplate<TEnum> other) => Equals((QuestParameterTemplate<TEnum>)other);

    /// <inheritdoc />
    public override string ToString()
    {
        switch (ValueType)
        {
            default: // Wildcard
                return SelectionType == ParameterSelectionType.Inclusive ? "#*" : "$*";

            case ParameterValueType.Constant:
                return ((ConstantSet)Set!).Value.ToString();

            case ParameterValueType.Range:
                RangeSet range = (RangeSet?)Set!;
                return $"{(SelectionType == ParameterSelectionType.Inclusive ? "#" : "$")}({(range.InfinityMinimum ? string.Empty : range.Minimum.ToString())}:{(range.InfinityMaximum ? string.Empty : range.Maximum.ToString())})";

            case ParameterValueType.List:
                ListSet list = (ListSet?)Set!;
                if (list.Values.Length == 0)
                {
                    return SelectionType == ParameterSelectionType.Inclusive ? "#[]" : "$[]";
                }

                StringBuilder sb = new StringBuilder(SelectionType == ParameterSelectionType.Inclusive ? "#[" : "$[", 0, 2, 3 + 14 * (list.Values.Length - 1));

                for (int i = 0; i < list.Values.Length; ++i)
                {
                    TEnum v = list.Values[i];

                    if (i != 0)
                        sb.Append(", ");

                    sb.Append(v.ToString());
                }

                return sb.ToString();
        }
    }

    protected class EnumParameterValue : QuestParameterValue<TEnum>, IEquatable<EnumParameterValue>
    {
        private static TEnum[]? _valueList;
        private TEnum _value;
        private TEnum _minValue;
        private TEnum _maxValue;
        private bool _minValueIsInf;
        private bool _maxValueIsInf;
        private TEnum[]? _values;
        private bool _isEmptySet;
        private EnumParameterValue() { }

        public EnumParameterValue(EnumParameterTemplate<TEnum> template)
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

                case ParameterValueType.Range when selType == ParameterSelectionType.Inclusive:
                    RangeSet range = (RangeSet?)template.Set!;
                    _minValueIsInf = range.InfinityMinimum;
                    _maxValueIsInf = range.InfinityMaximum;
                    _minValue = range.Minimum;
                    _maxValue = range.Maximum;
                    break;

                case ParameterValueType.Range:
                    range = (RangeSet?)template.Set!;
                    CheckValueList();
                    TEnum[] values = _valueList!;
                    int startIndex = 0;
                    int endIndex = values.Length - 1;
                    if (!range.InfinityMinimum)
                    {
                        for (; startIndex <= endIndex; ++startIndex)
                        {
                            if (range.Minimum.CompareTo(values[startIndex]) >= 0)
                                break;
                        }
                    }
                    if (!range.InfinityMaximum)
                    {
                        for (; endIndex >= startIndex; --endIndex)
                        {
                            if (range.Maximum.CompareTo(values[endIndex]) <= 0)
                                break;
                        }
                    }

                    if (endIndex <= startIndex)
                        _isEmptySet = true;
                    else
                    {
                        int ind = RandomUtility.GetInteger(startIndex, endIndex + 1);
                        _value = values[ind];
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

                case ParameterValueType.Wildcard when selType == ParameterSelectionType.Selective:
                    CheckValueList();
                    values = _valueList!;
                    _value = values[RandomUtility.GetIndex((ICollection)values)];
                    break;
            }
        }

        private static void CheckValueList()
        {
            if (_valueList != null)
                return;

            TEnum[] values = (TEnum[])Enum.GetValues(typeof(TEnum));
            Array.Sort(values);
            _valueList = values;
        }

        public static bool TryParse(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out QuestParameterValue<TEnum> value)
        {
            if (!TryParseIntl(str, out ParameterSelectionType selType, out ParameterValueType valType, out TEnum constant, out TEnum minValue, out TEnum maxValue, out bool minValInf, out bool maxValInf, out TEnum[]? list))
            {
                value = null;
                return false;
            }

            EnumParameterValue val = new EnumParameterValue
            {
                SelectionType = selType,
                ValueType = valType
            };

            switch (valType)
            {
                case ParameterValueType.Constant:
                    val._value = constant;
                    break;

                case ParameterValueType.Range when selType == ParameterSelectionType.Inclusive:
                    val._minValueIsInf = minValInf;
                    val._maxValueIsInf = maxValInf;
                    val._minValue = minValue;
                    val._maxValue = maxValue;
                    break;

                case ParameterValueType.Range:
                    CheckValueList();
                    TEnum[] values = _valueList!;
                    int startIndex = 0;
                    int endIndex = values.Length - 1;
                    if (!minValInf)
                    {
                        for (; startIndex <= endIndex; ++startIndex)
                        {
                            if (minValue.CompareTo(values[startIndex]) >= 0)
                                break;
                        }
                    }
                    if (!maxValInf)
                    {
                        for (; endIndex >= startIndex; --endIndex)
                        {
                            if (maxValue.CompareTo(values[endIndex]) <= 0)
                                break;
                        }
                    }

                    if (endIndex <= startIndex)
                        val._isEmptySet = true;
                    else
                    {
                        int ind = RandomUtility.GetInteger(startIndex, endIndex + 1);
                        val._value = values[ind];
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

                case ParameterValueType.Wildcard when selType == ParameterSelectionType.Selective:
                    CheckValueList();
                    values = _valueList!;
                    val._value = values[RandomUtility.GetIndex((ICollection)values)];
                    break;
            }

            value = val;
            return true;
        }

        public override bool IsMatch(TEnum otherValue)
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
                return !_isEmptySet && _value.Equals(otherValue);
            }

            if (valType == ParameterValueType.Range)
            {
                return (_minValueIsInf || _minValue.CompareTo(otherValue) <= 0) && (_maxValueIsInf || _maxValue.CompareTo(otherValue) >= 0);
            }

            // List
            for (int i = 0; i < _values!.Length; ++i)
            {
                if (_values[i].Equals(otherValue))
                    return true;
            }

            return false;
        }

        public override TEnum GetSingleValue()
        {
            if (ValueType != ParameterValueType.Constant && SelectionType != ParameterSelectionType.Selective)
                throw new InvalidOperationException("Not a selective or constant parameter value.");

            return _value;
        }

        /// <inheritdoc />
        public override bool Equals(QuestParameterValue<TEnum>? other)
        {
            return other is EnumParameterValue v && Equals(v);
        }

        /// <inheritdoc />
        public bool Equals(EnumParameterValue? other)
        {
            if (other == null)
                return false;

            if (ValueType == ParameterValueType.Constant || SelectionType == ParameterSelectionType.Selective)
                return _value.Equals(other._value);

            if (ValueType != other.ValueType)
                return false;

            if (ValueType == ParameterValueType.Wildcard && other.ValueType == ParameterValueType.Wildcard)
                return true;

            if (ValueType == ParameterValueType.Range)
            {
                return _maxValueIsInf == other._maxValueIsInf && (other._maxValueIsInf || _maxValue.Equals(other._maxValue))
                    && _minValueIsInf == other._minValueIsInf && (other._minValueIsInf || _minValue.Equals(other._minValue));
            }

            if (_isEmptySet || _values == null || _values.Length == 0)
            {
                return other._isEmptySet || other._values == null || other._values.Length == 0;
            }

            if (other._isEmptySet || other._values == null || other._values.Length == 0)
                return false;

            for (int i = 0; i < _values.Length; ++i)
            {
                if (!_values[i].Equals(other._values[i]))
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            if (SelectionType == ParameterSelectionType.Selective)
            {
                return _isEmptySet ? "$[]" : _value.ToString();
            }

            switch (ValueType)
            {
                default: // Wildcard
                    return "#*";

                case ParameterValueType.Constant:
                    return _value.ToString();

                case ParameterValueType.Range:
                    return $"#({(_minValueIsInf ? string.Empty : _minValue.ToString())}:{(_maxValueIsInf ? string.Empty : _maxValue.ToString())})";

                case ParameterValueType.List:
                    if (_values!.Length == 0)
                    {
                        return "#[]";
                    }

                    StringBuilder sb = new StringBuilder("#[", 0, 2, 3 + 14 * (_values.Length - 1));

                    for (int i = 0; i < _values.Length; ++i)
                    {
                        TEnum v = _values[i];

                        if (i != 0)
                            sb.Append(", ");

                        sb.Append(v.ToString());
                    }

                    return sb.ToString();
            }
        }
    }
}