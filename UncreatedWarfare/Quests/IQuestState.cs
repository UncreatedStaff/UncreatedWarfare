using System;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Quests.Parameters;

namespace Uncreated.Warfare.Quests;

/// <summary>
/// Stores information about the values of random variables for <see cref="QuestTemplate"/> objects.
/// </summary>
public interface IQuestState
{
    /// <summary>
    /// The value of the Unturned Quest Flag. This is formatted into <see cref="QuestAsset"/> translations.
    /// </summary>
    [JsonIgnore]
    QuestParameterValue<int> FlagValue { get; }

    /// <summary>
    /// Creates a string used to auto-generate quests.
    /// </summary>
    /// <remarks>Used by Daily Quests.</remarks>
    string CreateQuestDescriptiveString();
}

/// <inheritdoc/>
/// <typeparam name="TQuestTemplate">Class deriving from <see cref="QuestTemplate"/> used as a template for random variations to be created.</typeparam>
public interface IQuestState<in TQuestTemplate> : IQuestState where TQuestTemplate : QuestTemplate
{
    /// <summary>
    /// Creates a random state from a <see cref="QuestTemplate"/> type.
    /// </summary>
    UniTask CreateFromTemplateAsync(TQuestTemplate template, CancellationToken token = default);
    
    /// <summary>
    /// Reads a pre-configured state from a configuration file.
    /// </summary>
    UniTask CreateFromConfigurationAsync(IQuestStateConfiguration configuration, TQuestTemplate template, IServiceProvider serviceProvider, CancellationToken token = default);
}