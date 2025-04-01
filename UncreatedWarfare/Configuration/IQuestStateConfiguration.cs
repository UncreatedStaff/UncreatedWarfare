using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Quests.Parameters;

namespace Uncreated.Warfare.Configuration;

public interface IQuestStateConfiguration
{
    Type TemplateType { get; }

    string? GetValue(string key);

    [return: NotNullIfNotNull(nameof(@default))]
    public QuestParameterValue<Guid>? ParseAssetValue<TAsset>(string key, QuestParameterValue<Guid>? @default = null) where TAsset : Asset
    {
        string? str = GetValue(key);

        if (string.IsNullOrWhiteSpace(str))
            return @default;

        if (!AssetParameterTemplate<TAsset>.TryParseValue(str, out QuestParameterValue<Guid>? value))
        {
            throw new QuestConfigurationException(TemplateType, $"Failed to parse {Accessor.ExceptionFormatter.Format(typeof(TAsset))} parameter for \"{key}\".");
        }

        return value;
    }

    [return: NotNullIfNotNull(nameof(@default))]
    public QuestParameterValue<TEnum>? ParseEnumValue<TEnum>(string key, QuestParameterValue<TEnum>? @default = null) where TEnum : unmanaged, Enum
    {
        string? str = GetValue(key);

        if (string.IsNullOrWhiteSpace(str))
            return @default;

        if (!EnumParameterTemplate<TEnum>.TryParseValue(str, out QuestParameterValue<TEnum>? value))
        {
            throw new QuestConfigurationException(TemplateType, $"Failed to parse {Accessor.ExceptionFormatter.Format(typeof(TEnum))} parameter for \"{key}\".");
        }

        return value;
    }

    [return: NotNullIfNotNull(nameof(@default))]
    public QuestParameterValue<int>? ParseInt32Value(string key, QuestParameterValue<int>? @default = null)
    {
        string? str = GetValue(key);

        if (string.IsNullOrWhiteSpace(str))
            return @default;

        if (!Int32ParameterTemplate.TryParseValue(str, out QuestParameterValue<int>? value))
        {
            throw new QuestConfigurationException(TemplateType, $"Failed to parse integer parameter for \"{key}\".");
        }

        return value;
    }

    [return: NotNullIfNotNull(nameof(@default))]
    public QuestParameterValue<float>? ParseSingleValue(string key, QuestParameterValue<float>? @default = null)
    {
        string? str = GetValue(key);

        if (string.IsNullOrWhiteSpace(str))
            return @default;

        if (!SingleParameterTemplate.TryParseValue(str, out QuestParameterValue<float>? value))
        {
            throw new QuestConfigurationException(TemplateType, $"Failed to parse float parameter for \"{key}\".");
        }

        return value;
    }

    [return: NotNullIfNotNull(nameof(@default))]
    public QuestParameterValue<string>? ParseStringValue(string key, QuestParameterValue<string>? @default = null)
    {
        string? str = GetValue(key);

        if (string.IsNullOrWhiteSpace(str))
            return @default;

        if (!StringParameterTemplate.TryParseValue(str, out QuestParameterValue<string>? value))
        {
            throw new QuestConfigurationException(TemplateType, $"Failed to parse string parameter for \"{key}\".");
        }

        return value;
    }

    [return: NotNullIfNotNull(nameof(@default))]
    public async UniTask<QuestParameterValue<string>?> ParseKitNameValue(string key, IServiceProvider serviceProvider, QuestParameterValue<string>? @default = null)
    {
        string? str = GetValue(key);

        if (string.IsNullOrWhiteSpace(str))
            return @default;

        if (await KitNameParameterTemplate.TryParseValue(str, serviceProvider) is not { } value)
        {
            throw new QuestConfigurationException(TemplateType, $"Failed to parse kit name parameter for \"{key}\".");
        }

        return value;
    }

    [return: NotNullIfNotNull(nameof(@default))]
    public bool ParseBooleanValue(string key, bool @default = false)
    {
        string? str = GetValue(key);
        if (string.IsNullOrWhiteSpace(str))
            return @default;

        if (!bool.TryParse(str, out bool val))
            throw new QuestConfigurationException(TemplateType, $"Failed to parse boolean parameter for \"{key}\".");

        return val;
    }
}

public class QuestIConfigurationStateConfiguration : IQuestStateConfiguration
{
    private readonly IConfiguration _configuration;

    public Type TemplateType { get; }

    public QuestIConfigurationStateConfiguration(IConfiguration configuration, Type templateType)
    {
        _configuration = configuration;
        TemplateType = templateType;
    }

    public string? GetValue(string key)
    {
        return _configuration[key];
    }
}

public class QuestJsonElementStateConfiguration : IQuestStateConfiguration
{
    private readonly JsonElement _element;

    public Type TemplateType { get; }

    public QuestJsonElementStateConfiguration(JsonElement element, Type templateType)
    {
        _element = element;
        TemplateType = templateType;
    }

    public string? GetValue(string key)
    {
        return _element.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    /// <inheritdoc />
    public bool ParseBooleanValue(string key, bool @default = false)
    {
        if (!_element.TryGetProperty(key, out JsonElement value))
            return @default;

        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            throw new QuestConfigurationException(TemplateType, $"Failed to parse boolean parameter for \"{key}\".");

        return value.ValueKind == JsonValueKind.True;
    }
}