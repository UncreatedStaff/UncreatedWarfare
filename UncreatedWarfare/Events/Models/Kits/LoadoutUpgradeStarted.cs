namespace Uncreated.Warfare.Events.Models.Kits;

/// <summary>
/// Invoked after a loadout upgrade is started.
/// </summary>
public class LoadoutUpgradeStarted : KitUpdated
{
    /// <summary>
    /// The admin that started the upgrade.
    /// </summary>
    public required CSteamID Admin { get; init; }

    /// <summary>
    /// The player the loadout was created for.
    /// </summary>
    public required CSteamID Player { get; init; }
}
