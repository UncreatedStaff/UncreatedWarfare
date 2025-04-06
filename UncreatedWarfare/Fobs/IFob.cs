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
    /// The display color of this FOB on the FOB list.
    /// </summary>
    Color32 Color { get; }

    /// <summary>
    /// The team that owns the FOB.
    /// </summary>
    Team Team { get; }

    /// <summary>
    /// Whether the specified player should be able to see this fob on their FOB HUD.
    /// </summary>
    bool IsVibileToPlayer(WarfarePlayer player);

    /// <summary>
    /// Called when the <see cref="FobConfiguration"/> is updated.
    /// </summary>
    void UpdateConfiguration(FobConfiguration configuration);
}