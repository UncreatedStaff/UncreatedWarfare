using Uncreated.Framework.UI;

namespace Uncreated.Warfare.Players.UI;

/// <summary>
/// Implemented by <see cref="UnturnedUI"/> instances to hide and show themselves when the HUD should be hidden.
/// </summary>
public interface IHudUIListener
{
    /// <summary>
    /// Invoked when the HUD needs to be hidden.
    /// </summary>
    /// <param name="player">The player to hide the HUD for, or all players.</param>
    void Hide(WarfarePlayer? player);

    /// <summary>
    /// Invoked when the HUD needs to be shown.
    /// </summary>
    /// <param name="player">The player to show the HUD for, or all players.</param>
    void Restore(WarfarePlayer? player);
}