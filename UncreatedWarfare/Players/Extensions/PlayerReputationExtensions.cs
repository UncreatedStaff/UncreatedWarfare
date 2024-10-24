using Uncreated.Warfare.Players.Components;

namespace Uncreated.Warfare.Players.Extensions;
public static class PlayerReputationExtensions
{
    /// <summary>
    /// Adds (or subtracts) a certain reputation value to a player.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public static void AddReputation(this WarfarePlayer player, int reputation)
    {
        player.Component<PlayerReputationComponent>().AddReputation(reputation);
    }
}
