using Cysharp.Threading.Tasks;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.NewQuests.Parameters;

/// <summary>
/// Quest paramater template representing a set of possible values for randomly generated quests, or a set of allowed values for conditions.
/// </summary>
/// <remarks>For kit names, use <see cref="KitNameParameterTemplate"/>.</remarks>
[TypeConverter(typeof(AssetParameterTemplateTypeConverter))]
public class AssetParameterTemplate<TAsset> : QuestParameterTemplate<Guid>, IEquatable<AssetParameterTemplate<TAsset>> where TAsset : Asset
{
    private static List<TAsset>? _mainWorkingThreadList;

    /// <summary>
    /// A parameter value that matches any asset that is assignable to <typeparamref name="TAsset"/>.
    /// </summary>
    public static QuestParameterValue<Guid> WildcardInclusive { get; } = new AssetParameterValue(new AssetParameterTemplate<TAsset>(ParameterSelectionType.Inclusive));

    /// <summary>
    /// Create a template of it's string representation.
    /// </summary>
    public AssetParameterTemplate(ReadOnlySpan<char> str) : base(str) { }

    /// <summary>
    /// Create a template of it's string representation.
    /// </summary>
    [UsedImplicitly]
    public AssetParameterTemplate(string str) : base(str.AsSpan()) { }

    /// <summary>
    /// Create a template of a wildcard set of values.
    /// </summary>
    public AssetParameterTemplate(ParameterSelectionType wildcardType) : base(null, wildcardType) { }

    /// <summary>
    /// Create a template of a constant value.
    /// </summary>
    public AssetParameterTemplate(Guid constant) : base(new ConstantSet(constant), ParameterSelectionType.Selective) { }

    /// <summary>
    /// Create a template of a set of values.
    /// </summary>
    public AssetParameterTemplate(Guid[] values, ParameterSelectionType selectionType)
        : base(new ListSet(values), selectionType) { }

    protected AssetParameterTemplate() { }

    /// <inheritdoc />
    public override async UniTask<QuestParameterValue<Guid>> CreateValue(IServiceProvider serviceProvider)
    {
        await UniTask.SwitchToMainThread();

        return new AssetParameterValue(this);
    }

    /// <summary>
    /// Creates a random value from this selective set, or a set of values if this is an inclusive set.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on the game thread.</exception>
    public QuestParameterValue<Guid> CreateValueOnGameThread()
    {
        ThreadUtil.assertIsGameThread();

        return new AssetParameterValue(this);
    }

    /// <summary>
    /// Read a saved value of this <see cref="AssetParameterTemplate"/> from a string.
    /// </summary>
    public static bool TryParseValue(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out QuestParameterValue<Guid> value)
    {
        return AssetParameterValue.TryParse(str, out value);
    }

    /// <summary>
    /// Read a <see cref="AssetParameterTemplate"/> from a string.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out AssetParameterTemplate<TAsset> template)
    {
        AssetParameterTemplate<TAsset> val = new AssetParameterTemplate<TAsset>();
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
        if (!TryParseIntl(str, out ParameterSelectionType selType, out ParameterValueType valType, out Guid constant, out Guid[]? list))
            return false;

        if (valType == ParameterValueType.Range)
            return false;

        SelectionType = selType;
        ValueType = valType;
        Set = valType switch
        {
            ParameterValueType.Constant => new ConstantSet(constant),
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
                throw new JsonException($"Unexpected token while reading {Accessor.ExceptionFormatter.Format(typeof(TAsset))} GUID value for a quest parameter.");
        }
    }

    /// <summary>
    /// Read a value from a JSON reader.
    /// </summary>
    public static QuestParameterValue<Guid>? ReadValueJson(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.String:
                string str = reader.GetString()!;
                if (!TryParseValue(str.AsSpan(), out QuestParameterValue<Guid>? value))
                    throw new FormatException("Failed to parse quest parameter value.");

                return value;

            default:
                throw new JsonException($"Unexpected token while reading {Accessor.ExceptionFormatter.Format(typeof(TAsset))} GUID value for a quest parameter.");
        }
    }

    protected static bool TryParseIntl(ReadOnlySpan<char> str, out ParameterSelectionType selType, out ParameterValueType valType, out Guid constant, out Guid[]? list)
    {
        constant = default;
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

        if (Guid.TryParse(str, out Guid guid))
        {
            constant = guid;
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
            list = Array.Empty<Guid>();
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

        list = new Guid[termCount];
        termCount = 0;
        lastIndex = 1;
        while (true)
        {
            int nextComma = str.Slice(lastIndex + 1).IndexOf(',');
            if (nextComma == -1)
                break;

            if (!Guid.TryParse(str.Slice(lastIndex + 1, nextComma).Trim(), out list[termCount]))
            {
                return false;
            }

            ++termCount;
            lastIndex = nextComma + lastIndex + 1;
        }

        if (!Guid.TryParse(str.Slice(lastIndex + 1, str.Length - lastIndex - 2).Trim(), out list[termCount]))
        {
            return false;
        }

        selType = isInclusive ? ParameterSelectionType.Inclusive : ParameterSelectionType.Selective;
        valType = ParameterValueType.List;
        return true;
    }

    /// <inheritdoc />
    public bool Equals(AssetParameterTemplate<TAsset> other) => Equals((QuestParameterTemplate<Guid>)other);


    /// <inheritdoc />
    public override string ToString()
    {
        switch (ValueType)
        {
            default: // Wildcard
                return SelectionType == ParameterSelectionType.Inclusive ? "#*" : "$*";

            case ParameterValueType.Constant:
                return ((ConstantSet)Set!).Value.ToString("N");
                
            case ParameterValueType.List:
                ListSet list = (ListSet?)Set!;
                if (list.Values.Length == 0)
                {
                    return SelectionType == ParameterSelectionType.Inclusive ? "#[]" : "$[]";
                }

                int ttlLength = 1 + 34 * list.Values.Length;

                return string.Create(ttlLength, this, (span, state) =>
                {
                    span[0] = state.SelectionType == ParameterSelectionType.Inclusive ? '#' : '$';
                    span[1] = '[';

                    ListSet list = (ListSet?)Set!;

                    int index = 2;
                    for (int i = 0; i < list.Values.Length; ++i)
                    {
                        Guid v = list.Values[i];

                        if (i != 0)
                        {
                            span[index] = ',';
                            span[index + 2] = ' ';
                            index += 2;
                        }

                        v.TryFormat(span, out _, "N");
                        index += 32;
                    }

                    span[index] = ']';
                });
        }
    }

    internal class AssetParameterValue : QuestParameterValue<Guid>, IEquatable<AssetParameterValue>
    {
        private Guid _value;
        private Guid[]? _values;
        private bool _isEmptySet;

        private AssetParameterValue() { }

        public AssetParameterValue(AssetParameterTemplate<TAsset> template)
        {
            ParameterValueType valType = template.ValueType;
            ParameterSelectionType selType = template.SelectionType;

            ValueType = valType;
            SelectionType = selType;

            switch (valType)
            {
                case ParameterValueType.Wildcard when template.SelectionType == ParameterSelectionType.Selective:
                    _mainWorkingThreadList ??= new List<TAsset>(16);
                    try
                    {
                        Assets.find(_mainWorkingThreadList);
                        _value = _mainWorkingThreadList[RandomUtility.GetIndex((ICollection)_mainWorkingThreadList)].GUID;
                    }
                    finally
                    {
                        _mainWorkingThreadList.Clear();
                    }
                    break;

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

        public static bool TryParse(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out QuestParameterValue<Guid> value)
        {
            if (!TryParseIntl(str, out ParameterSelectionType selType, out ParameterValueType valType, out Guid constant,
                    out Guid[]? list))
            {
                value = null;
                return false;
            }

            AssetParameterValue val = new AssetParameterValue
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
                    if (Thread.CurrentThread.IsGameThread())
                    {
                        _mainWorkingThreadList ??= new List<TAsset>(16);
                        try
                        {
                            Assets.find(_mainWorkingThreadList);
                            val._value = _mainWorkingThreadList[RandomUtility.GetIndex((ICollection)_mainWorkingThreadList)].GUID;
                        }
                        finally
                        {
                            _mainWorkingThreadList.Clear();
                        }
                    }
                    else
                    {
                        List<TAsset> assetList = new List<TAsset>(16);
                        Assets.find(assetList);
                        val._value = assetList[RandomUtility.GetIndex((ICollection)assetList)].GUID;
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

        public bool IsMatch(Asset otherValue)
        {
            ParameterValueType valType = ValueType;

            if (otherValue is not TAsset)
                return false;

            // Wildcard
            if (valType is < ParameterValueType.Constant or > ParameterValueType.List)
            {
                return true;
            }

            ParameterSelectionType selType = SelectionType;

            if (valType == ParameterValueType.Constant || selType == ParameterSelectionType.Selective)
            {
                return !_isEmptySet && _value == otherValue.GUID;
            }

            if (valType == ParameterValueType.Range)
            {
                return false;
            }

            // List
            for (int i = 0; i < _values!.Length; ++i)
            {
                if (_values[i] == otherValue.GUID)
                    return true;
            }

            return false;
        }
        public override bool IsMatch(Guid otherValue)
        {
            ParameterValueType valType = ValueType;

            if (Assets.find(otherValue) is not TAsset)
                return false;

            // Wildcard
            if (valType is < ParameterValueType.Constant or > ParameterValueType.List)
            {
                return Assets.find(otherValue) is TAsset;
            }

            ParameterSelectionType selType = SelectionType;

            if (valType == ParameterValueType.Constant || selType == ParameterSelectionType.Selective)
            {
                return !_isEmptySet && _value == otherValue;
            }

            if (valType == ParameterValueType.Range)
            {
                return false;
            }

            // List
            for (int i = 0; i < _values!.Length; ++i)
            {
                if (_values[i] == otherValue)
                    return true;
            }

            return false;
        }

        public override Guid GetSingleValue()
        {
            if (ValueType != ParameterValueType.Constant && SelectionType != ParameterSelectionType.Selective)
                throw new InvalidOperationException("Not a selective or constant parameter value.");

            return _value;
        }

        /// <inheritdoc />
        public override bool Equals(QuestParameterValue<Guid>? other)
        {
            return other is AssetParameterValue v && Equals(v);
        }

        /// <inheritdoc />
        public bool Equals(AssetParameterValue? other)
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
                return false;

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

        /// <inheritdoc />
        public override string ToString()
        {
            if (SelectionType == ParameterSelectionType.Selective)
            {
                return _isEmptySet ? "$[]" : _value.ToString("N");
            }

            switch (ValueType)
            {
                default: // Wildcard
                    return "#*";

                case ParameterValueType.Constant:
                    return _value.ToString("N");

                case ParameterValueType.List:
                    if (_values!.Length == 0)
                    {
                        return "#[]";
                    }

                    int ttlLength = 1 + 34 * _values.Length;

                    return string.Create(ttlLength, this, (span, state) =>
                    {
                        span[0] = state.SelectionType == ParameterSelectionType.Inclusive ? '#' : '$';
                        span[1] = '[';

                        int index = 2;
                        for (int i = 0; i < state._values!.Length; ++i)
                        {
                            Guid v = state._values[i];

                            if (i != 0)
                            {
                                span[index] = ',';
                                span[index + 2] = ' ';
                                index += 2;
                            }

                            v.TryFormat(span, out _, "N");
                            index += 32;
                        }

                        span[index] = ']';
                    });
            }
        }
    }
}

public static class AssetParameterValueExtensions
{
    /// <summary>
    /// Compare a value against the current value of this parameter. Best used with inclusive selection.
    /// </summary>
    public static bool IsMatch<TAssetType>(this QuestParameterValue<Guid> assetType, Asset asset) where TAssetType : Asset
    {
        return asset != null && (assetType is AssetParameterTemplate<TAssetType>.AssetParameterValue a
            ? a.IsMatch(asset)
            : assetType.IsMatch(asset.GUID));
    }

    /// <summary>
    /// Compare a value against the current value of this parameter. Best used with inclusive selection.
    /// </summary>
    public static bool IsMatch<TAssetType>(this QuestParameterValue<Guid> assetType, IAssetLink<Asset>? asset) where TAssetType : Asset
    {
        return asset != null && (asset.GetAsset() is { } asset2 && assetType is AssetParameterTemplate<TAssetType>.AssetParameterValue a
            ? a.IsMatch(asset2)
            : assetType.IsMatch(asset.Guid));
    }
}