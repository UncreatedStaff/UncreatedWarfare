using System.Runtime.CompilerServices;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Players;
public class GroupChanged : PlayerEvent
{
    public ulong OldGroup { get; }
    public ulong NewGroup { get; }
    public EPlayerGroupRank OldRank { get; }
    public EPlayerGroupRank NewRank { get; }
    public ulong NewTeam { get; }
    public ulong OldTeam { get; }

    [SetsRequiredMembers]
    public GroupChanged(WarfarePlayer player, ulong oldGroup, EPlayerGroupRank oldRank, ulong newGroup, EPlayerGroupRank newRank)
    {
        Player = player;

        OldGroup = oldGroup;
        OldRank = oldRank;
        NewGroup = newGroup;
        NewRank = newRank;
        NewTeam = NewGroup;
        OldTeam = OldGroup; // todo teams
    }
}
