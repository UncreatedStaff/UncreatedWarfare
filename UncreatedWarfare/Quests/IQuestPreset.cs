using System;

namespace Uncreated.Warfare.Quests;

/// <summary>
/// Represents a preset for a quest template that can be used to load exact values for the random variables.
/// It is identified by a unqiue <see cref="Guid"/> and can contain overridden or precalculated rewards.
/// </summary>
public interface IQuestPreset
{
    /// <summary>
    /// Unique ID used to identify the preset.
    /// </summary>
    Guid Key { get; }

    /// <summary>
    /// The data that overrides the random variables.
    /// </summary>
    IQuestState State { get; }

    /// <summary>
    /// Optionally override the default reward expressions.
    /// </summary>
    IQuestReward[]? RewardOverrides { get; }

    /// <summary>
    /// Used to set the state during deserialization.
    /// </summary>
    void UpdateState(IQuestState state);

    /// <summary>
    /// Unturned Quest Flag ID, just needs to be a unique UInt16 value amongst all other flags.
    /// </summary>
    ushort Flag { get; }
}

/// <summary>
/// An <see cref="IQuestPreset"/> that has a <see cref="QuestAsset"/> configured for it.
/// </summary>
public interface IAssetQuestPreset : IQuestPreset
{
    /// <summary>
    /// GUID for a <see cref="QuestAsset"/>.
    /// </summary>
    /// <remarks>Note that multiple presets may have the same asset.</remarks>
    Guid Asset { get; }
}