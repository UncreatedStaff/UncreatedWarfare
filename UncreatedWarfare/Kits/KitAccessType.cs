using Uncreated.Warfare.Models.Kits;

namespace Uncreated.Warfare.Kits;

/// <summary>
/// Defines the reason behind a <see cref="KitAccess"/> entry.
/// </summary>
public enum KitAccessType : byte
{
    /// <summary>
    /// Access was granted before this enum was added.
    /// </summary>
    Unknown,

    /// <summary>
    /// Bought using credits. Shouldn't be used after season 4.
    /// </summary>
    Credits,

    /// <summary>
    /// Rewarded due to winning some event or for a similar reason.
    /// </summary>
    Event,

    /// <summary>
    /// Bought with real money.
    /// </summary>
    Purchase,

    /// <summary>
    /// Rewarded after completing a quest.
    /// </summary>
    QuestReward
}