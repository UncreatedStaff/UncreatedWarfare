using Uncreated.Warfare.Injures;

namespace Uncreated.Warfare.Players.Extensions;
public static class PlayerInjureExtensions
{
    /// <summary>
    /// Check if this player is currently injured.
    /// </summary>
    public static bool IsInjured(this WarfarePlayer player)
    {
        PlayerInjureComponent? comp = player.ComponentOrNull<PlayerInjureComponent>();
        return comp is not null && comp.State == PlayerHealthState.Injured;
    }
}
