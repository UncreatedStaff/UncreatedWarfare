using Cysharp.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.NewQuests.Parameters;

/// <summary>
/// Quest paramater template representing a set of possible values for randomly generated quests, or a set of allowed values for conditions.
/// </summary>
[TypeConverter(typeof(StringParameterTemplateTypeConverter))]
public class KitNameParameterTemplate : StringParameterTemplate
{
    /// <summary>
    /// Create a template of it's string representation.
    /// </summary>
    public KitNameParameterTemplate(ReadOnlySpan<char> str) : base(str) { }

    /// <summary>
    /// Create a template of a wildcard set of values.
    /// </summary>
    public KitNameParameterTemplate(ParameterSelectionType wildcardType) : base(wildcardType) { }

    /// <summary>
    /// Create a template of a constant value.
    /// </summary>
    /// <remarks>Use <see cref="MemoryExtensions.AsSpan(string)"/> to parse instead of passing as a constant.</remarks>
    public KitNameParameterTemplate(string constant) : base(constant) { }

    /// <summary>
    /// Create a template of a set of values.
    /// </summary>
    public KitNameParameterTemplate(string[] values, ParameterSelectionType selectionType)
        : base(values, selectionType) { }

    private KitNameParameterTemplate() { }

    /// <inheritdoc />
    public override UniTask<QuestParameterValue<string>> CreateValue(IServiceProvider serviceProvider)
    {
        ParameterValueType valType = ValueType;
        ParameterSelectionType selType = SelectionType;

        string? value = null;
        if (selType != ParameterSelectionType.Inclusive)
        {
            switch (valType)
            {
                case ParameterValueType.Constant:
                    value = ((ConstantSet?)Set!).Value;
                    break;

                case ParameterValueType.List:
                    ListSet list = (ListSet?)Set!;
                    value = list.Values.Length == 0 ? null : list.Values[RandomUtility.GetIndex((ICollection)list.Values)];
                    break;

                default:
                    KitManager kitManager = serviceProvider.GetRequiredService<KitManager>();
                    Kit? kit = kitManager.GetRandomPublicKit();
                    value = kit?.InternalName ?? "default";
                    break;
            }
        }

        return UniTask.FromResult<QuestParameterValue<string>>(new StringParameterValue(value, this));
    }

    /// <summary>
    /// Read a <see cref="KitNameParameterTemplate"/> from a string.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> str, [MaybeNullWhen(false)] out KitNameParameterTemplate template)
    {
        KitNameParameterTemplate val = new KitNameParameterTemplate();
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

        if (valType == ParameterValueType.Range)
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
    public new static SingleParameterTemplate? ReadJson(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.String:
                string str = reader.GetString()!;
                return new SingleParameterTemplate(str.AsSpan());

            default:
                throw new JsonException($"Unexpected token while reading kit name for a quest parameter.");
        }
    }
}