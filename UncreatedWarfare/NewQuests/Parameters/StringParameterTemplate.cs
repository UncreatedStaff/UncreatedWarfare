using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.NewQuests.Parameters;

/// <summary>
/// Quest paramater template representing a set of possible values for randomly generated quests, or a set of allowed values for conditions.
/// </summary>
/// <remarks>For kit names, use <see cref="KitNameParameterTemplate"/>.</remarks>
[TypeConverter(typeof(StringParameterTemplateTypeConverter))]
public class StringParameterTemplate : QuestParameterTemplate<string>
{
    /// <summary>
    /// A parameter value that matches any integer.
    /// </summary>
    public static QuestParameterValue<string> WildcardInclusive { get; } = new StringParameterValue(new StringParameterTemplate(ParameterSelectionType.Inclusive));

    /// <summary>
    /// Create a template of it's string representation.
    /// </summary>
    public StringParameterTemplate(ReadOnlySpan<char> str) : base(str) { }

    /// <summary>
    /// Create a template of a wildcard set of values.
    /// </summary>
    /// <remarks>Only Inclusive wildcards are supported.</remarks>
    /// <exception cref="ArgumentException">Selective wildcard was used.</exception>
    public StringParameterTemplate(ParameterSelectionType wildcardType) : base(null, wildcardType)
    {
        if (GetType() == typeof(StringParameterTemplate) && wildcardType != ParameterSelectionType.Inclusive)
            throw new ArgumentException("Only inclusive wildcards are supported by string parameters.", nameof(wildcardType));
    }

    /// <summary>
    /// Create a template of a constant value.
    /// </summary>
    /// <remarks>Use <see cref="MemoryExtensions.AsSpan(string)"/> to parse instead of passing as a constant.</remarks>
    public StringParameterTemplate(string constant) : base(new ConstantSet(constant), ParameterSelectionType.Selective) { }

    /// <summary>
    /// Create a template of a set of values.
    /// </summary>
    public StringParameterTemplate(string[] values, ParameterSelectionType selectionType)
        : base(new ListSet(values), selectionType) { }

    protected StringParameterTemplate() { }

    /// <inheritdoc />
    public override UniTask<QuestParameterValue<string>> CreateValue(IServiceProvider serviceProvider)
    {
        return UniTask.FromResult<QuestParameterValue<string>>(new StringParameterValue(this));
    }

    /// <summary>
    /// Read a saved value of this <see cref="StringParameterTemplate"/> from a string.
    /// </summary>
    public static bool TryParseValue(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out QuestParameterValue<string> value)
    {
        return StringParameterValue.TryParse(str, out value);
    }

    /// <summary>
    /// Read a <see cref="StringParameterTemplate"/> from a string.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out StringParameterTemplate template)
    {
        StringParameterTemplate val = new StringParameterTemplate();
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
        if (!TryParseIntl(str, out ParameterSelectionType selType, out ParameterValueType valType, out string? constant, out string[]? list))
            return false;

        if (valType == ParameterValueType.Range || selType == ParameterSelectionType.Selective && valType is not ParameterValueType.Constant and not ParameterValueType.List)
            return false;

        SelectionType = selType;
        ValueType = valType;
        Set = valType switch
        {
            ParameterValueType.Constant => new ConstantSet(constant!),
            ParameterValueType.List => new ListSet(list!),
            _ => null
        };

        return true;
    }

    /// <summary>
    /// Read from a JSON reader.
    /// </summary>
    public static StringParameterTemplate? ReadJson(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.String:
                string str = reader.GetString()!;
                return new StringParameterTemplate(str.AsSpan());

            default:
                throw new JsonException("Unexpected token while reading string value for a quest parameter.");
        }
    }

    /// <summary>
    /// Read a value from a JSON reader.
    /// </summary>
    public static QuestParameterValue<string>? ReadValueJson(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.String:
                string str = reader.GetString()!;
                if (!TryParseValue(str.AsSpan(), out QuestParameterValue<string>? value))
                    throw new FormatException("Failed to parse quest parameter value.");

                return value;

            default:
                throw new JsonException("Unexpected token while reading string value for a quest parameter.");
        }
    }

    protected static bool TryParseIntl(ReadOnlySpan<char> str, out ParameterSelectionType selType, out ParameterValueType valType, out string? constant, out string[]? list)
    {
        constant = null;
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

        if (str.Length == 0 || str[0] is not '$' and not '#')
        {
            if (str.Length > 1 && str[0] == '\\' && str[1] is '$' or '#')
                str = str[1..];

            constant = str.Length == 0 ? string.Empty : new string(str);
            selType = ParameterSelectionType.Selective;
            valType = ParameterValueType.Constant;
            return true;
        }

        if (str.Length < 3)
            return false;

        bool isInclusive = str[0] == '#';
        if (!isInclusive && str[0] != '$')
            return false;

        if (str[1] == '(')
            return false;

        if (str[1] != '[')
            return false;

        int endInd = 2;
        while (char.IsWhiteSpace(str[endInd]) && endInd + 1 > str.Length)
            ++endInd;

        if (endInd == str.Length - 1 || str[endInd] == ']')
        {
            list = Array.Empty<string>();
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

            nextComma += lastIndex + 1;
            if (nextComma > 0 && str[nextComma - 1] == '\\')
            {
                if (nextComma == 1 || str[nextComma - 2] != '\\')
                    continue;
            }

            lastIndex = nextComma;
            ++termCount;
        }

        list = new string[termCount];
        termCount = 0;
        lastIndex = 1;
        while (true)
        {
            int nextComma = str.Slice(lastIndex + 1).IndexOf(',');
            if (nextComma == -1)
                break;

            nextComma += lastIndex + 1;
            if (nextComma > 0 && str[nextComma - 1] == '\\')
            {
                if (nextComma == 1 || str[nextComma - 2] != '\\')
                    continue;
            }

            int len = nextComma - lastIndex - 1;
            list[termCount] = len == 0 ? string.Empty : new string(str.Slice(lastIndex + 1, len).Trim());

            ++termCount;
            lastIndex = nextComma;
        }

        list[termCount] = str.Length - lastIndex - 2 == 0 ? string.Empty : new string(str.Slice(lastIndex + 1, str.Length - lastIndex - 2).Trim());

        selType = isInclusive ? ParameterSelectionType.Inclusive : ParameterSelectionType.Selective;
        valType = ParameterValueType.List;
        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        switch (ValueType)
        {
            default: // Wildcard
                return SelectionType == ParameterSelectionType.Inclusive ? "#*" : "$*";

            case ParameterValueType.Constant:
                return ((ConstantSet)Set!).Value.ToString(CultureInfo.InvariantCulture);
                
            case ParameterValueType.List:
                ListSet list = (ListSet?)Set!;
                if (list.Values.Length == 0)
                {
                    return SelectionType == ParameterSelectionType.Inclusive ? "#[]" : "$[]";
                }

                int ttlLength = 3 + 2 * (list.Values.Length - 1);
                for (int i = 0; i < list.Values.Length; ++i)
                {
                    string v = list.Values[i];
                    if (string.IsNullOrEmpty(v))
                        continue;

                    if (v.IndexOf(',') != -1)
                        v = v.Replace(",", @"\,");

                    ttlLength += v?.Length ?? 0;
                }

                return string.Create(ttlLength, this, (span, state) =>
                {
                    span[0] = state.SelectionType == ParameterSelectionType.Inclusive ? '#' : '$';
                    span[1] = '[';

                    ListSet list = (ListSet?)Set!;

                    int index = 2;
                    for (int i = 0; i < list.Values.Length; ++i)
                    {
                        string v = list.Values[i];

                        if (i != 0)
                        {
                            span[index] = ',';
                            span[index + 2] = ' ';
                            index += 2;
                        }

                        if (string.IsNullOrEmpty(v))
                            continue;

                        if (v.IndexOf(',') != -1)
                            v = v.Replace(",", @"\,");

                        v.AsSpan().CopyTo(span[index..]);
                        index += v.Length;
                    }

                    span[index] = ']';
                });
        }
    }

    protected class StringParameterValue : QuestParameterValue<string>, IEquatable<StringParameterValue>
    {
        private string? _value;
        private string[]? _values;
        private bool _isEmptySet;

        private StringParameterValue() { }

        public StringParameterValue(StringParameterTemplate template)
        {
            ParameterValueType valType = template.ValueType;
            ParameterSelectionType selType = template.SelectionType;

            ValueType = valType;
            SelectionType = selType;

            switch (valType)
            {
                case ParameterValueType.Wildcard when template.SelectionType == ParameterSelectionType.Selective:
                    throw new ArgumentException("String parameters do not support selective wildcards.");

                case ParameterValueType.Constant:
                    _value = ((ConstantSet?)template.Set!).Value;
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

        public StringParameterValue(string? value, ParameterValueType valType)
        {
            ValueType = valType;
            SelectionType = ParameterSelectionType.Selective;

            _value = value;
        }

        public StringParameterValue(string? value, string[]? list, ParameterValueType valType)
        {
            ValueType = valType;
            SelectionType = ParameterSelectionType.Inclusive;

            _value = value;
            _values = list;
            _isEmptySet = valType == ParameterValueType.List && list is not { Length: > 0 };
        }

        public StringParameterValue(string? value, StringParameterTemplate template)
        {
            ParameterValueType valType = template.ValueType;
            ParameterSelectionType selType = template.SelectionType;

            ValueType = valType;
            SelectionType = selType;

            _value = value;

            switch (valType)
            {
                case ParameterValueType.Constant when selType == ParameterSelectionType.Inclusive:
                    _value = ((ConstantSet?)template.Set!).Value;
                    break;

                case ParameterValueType.List:
                    ListSet list = (ListSet?)template.Set!;
                    if (selType == ParameterSelectionType.Inclusive)
                    {
                        _values = list.Values;
                    }
                    else if (list.Values.Length == 0)
                    {
                        _isEmptySet = true;
                    }

                    break;
            }
        }

        public static bool TryParse(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out QuestParameterValue<string> value)
        {
            if (!TryParseIntl(str, out ParameterSelectionType selType, out ParameterValueType valType, out string? constant,
                    out string[]? list))
            {
                value = null;
                return false;
            }

            if (valType == ParameterValueType.Range || selType == ParameterSelectionType.Selective && valType is not ParameterValueType.Constant and not ParameterValueType.List)
            {
                value = null;
                return false;
            }

            StringParameterValue val = new StringParameterValue
            {
                SelectionType = selType,
                ValueType = valType
            };

            switch (valType)
            {
                case ParameterValueType.Constant:
                    val._value = constant!;
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

        public override bool IsMatch(string otherValue)
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
                return !_isEmptySet && string.Equals(_value, otherValue, StringComparison.OrdinalIgnoreCase);
            }

            if (valType == ParameterValueType.Range)
            {
                return false;
            }

            // List
            for (int i = 0; i < _values!.Length; ++i)
            {
                if (string.Equals(_values[i], otherValue, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public override string GetSingleValue()
        {
            if (ValueType != ParameterValueType.Constant && SelectionType != ParameterSelectionType.Selective)
                throw new InvalidOperationException("Not a selective or constant parameter value.");

            return _value!;
        }

        /// <inheritdoc />
        public override bool Equals(QuestParameterValue<string>? other)
        {
            return other is StringParameterValue v && Equals(v);
        }

        /// <inheritdoc />
        public bool Equals(StringParameterValue other)
        {
            if (ValueType == ParameterValueType.Constant || SelectionType == ParameterSelectionType.Selective)
                return string.Equals(_value, other._value, StringComparison.Ordinal);

            if (ValueType != other.ValueType)
                return false;

            if (ValueType == ParameterValueType.Wildcard && other.ValueType == ParameterValueType.Wildcard)
                return true;

            if (ValueType == ParameterValueType.Range)
                return false;

            if (_isEmptySet || _values == null || _values.Length == 0)
            {
                return other._isEmptySet || other._values == null || other._values.Length == 0;
            }

            if (other._isEmptySet || other._values == null || other._values.Length == 0)
                return false;

            for (int i = 0; i < _values.Length; ++i)
            {
                if (!string.Equals(_values[i], other._values[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            if (SelectionType == ParameterSelectionType.Selective)
            {
                return !_isEmptySet ? _value!.Length > 0 && _value![0] is '#' or '$' ? @"\" + _value : _value : "$[]";
            }

            switch (ValueType)
            {
                default: // Wildcard
                    return "#*";

                case ParameterValueType.Constant:
                    return _value!.Length > 0 && _value![0] is '#' or '$' ? @"\" + _value : _value;

                case ParameterValueType.List:
                    if (_values!.Length == 0)
                    {
                        return "#[]";
                    }

                    int ttlLength = 3 + 2 * (_values!.Length - 1);
                    for (int i = 0; i < _values.Length; ++i)
                    {
                        string v = _values[i];
                        if (string.IsNullOrEmpty(v))
                            continue;

                        if (v.IndexOf(',') != -1)
                            v = v.Replace(",", @"\,");

                        ttlLength += v?.Length ?? 0;
                    }

                    return string.Create(ttlLength, this, (span, state) =>
                    {
                        span[0] = '#';
                        span[1] = '[';

                        int index = 2;
                        for (int i = 0; i < state._values!.Length; ++i)
                        {
                            string v = state._values[i];

                            if (i != 0)
                            {
                                span[index] = ',';
                                span[index + 2] = ' ';
                                index += 2;
                            }

                            if (string.IsNullOrEmpty(v))
                                continue;

                            if (v.IndexOf(',') != -1)
                                v = v.Replace(",", @"\,");

                            v.AsSpan().CopyTo(span[index..]);
                            index += v.Length;
                        }

                        span[index] = ']';
                    });
            }
        }
    }
}