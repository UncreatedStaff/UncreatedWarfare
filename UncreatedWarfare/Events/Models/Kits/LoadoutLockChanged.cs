namespace Uncreated.Warfare.Events.Models.Kits;

/// <summary>
/// Invoked after a loadout is locked or unlocked.
/// </summary>
public class LoadoutLockChanged : KitUpdated
{
    /// <summary>
    /// The current lock state.
    /// </summary>
    public bool IsLocked => Kit.IsLocked;

    /// <summary>
    /// The previous lock state.
    /// </summary>
    public bool WasLocked => !Kit.IsLocked;

    /// <summary>
    /// The admin that locked or unlocked the loadout.
    /// </summary>
    public required CSteamID Admin { get; init; }

    /// <summary>
    /// The player the loadout was created for.
    /// </summary>
    public required CSteamID Player { get; init; }
}
