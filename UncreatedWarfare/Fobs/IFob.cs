using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Fobs;

/// <summary>
/// A FOB that can show up on the FOB list.
/// </summary>
public interface IFob : IDeployable
{
    /// <summary>
    /// The display name of the FOB on the FOB list.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The team that owns the FOB.
    /// </summary>
    Team Team { get; }

    /// <summary>
    /// Gets the text displayed on the UI for a specific team, defaulting to <see cref="Name"/>.
    /// </summary>
    string GetUIDisplay(Team viewingTeam)
    {
        return string.Empty;
    }

    /// <summary>
    /// Gets the display color of this FOB on the FOB list.
    /// </summary>
    Color32 GetColor(Team viewingTeam);

    /// <summary>
    /// Whether the specified player should be able to see this fob on their FOB HUD.
    /// </summary>
    bool IsVisibleToPlayer(WarfarePlayer player);

    /// <summary>
    /// Called when the <see cref="FobConfiguration"/> is updated.
    /// </summary>
    void UpdateConfiguration(FobConfiguration configuration);
}