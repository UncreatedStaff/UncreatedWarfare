namespace Uncreated.Warfare.Events.Players;
public class GroupChanged : PlayerEvent
{
    public ulong OldGroup { get; }
    public ulong NewGroup { get; }
    public EPlayerGroupRank OldRank { get; }
    public EPlayerGroupRank NewRank { get; }
    public ulong NewTeam { get; }
    public ulong OldTeam { get; }
    public GroupChanged(UCPlayer player, ulong oldGroup, EPlayerGroupRank oldRank, ulong newGroup, EPlayerGroupRank newRank) : base(player)
    {
        OldGroup = oldGroup;
        OldRank = oldRank;
        NewGroup = newGroup;
        NewRank = newRank;
        NewTeam = NewGroup.GetTeam();
        OldTeam = OldGroup.GetTeam();
    }
}
