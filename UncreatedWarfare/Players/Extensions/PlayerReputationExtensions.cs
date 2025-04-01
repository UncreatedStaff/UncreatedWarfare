using Uncreated.Warfare.Players.Components;

namespace Uncreated.Warfare.Players.Extensions;
public static class PlayerReputationExtensions
{
    /// <summary>
    /// Adds (or subtracts) a certain displayed reputation value to a player.
    /// </summary>
    /// <remarks>Thread-safe. This DOES NOT update the value in the database.</remarks>
    public static void AddReputation(this WarfarePlayer player, int reputation)
    {
        player.Component<PlayerReputationComponent>().AddReputation(reputation);
    }

    /// <summary>
    /// Sets the displayed reputation value for a player.
    /// </summary>
    /// <remarks>Thread-safe. This DOES NOT update the value in the database.</remarks>
    public static void SetReputation(this WarfarePlayer player, int reputation)
    {
        player.Component<PlayerReputationComponent>().SetReputation(reputation);
    }
}
