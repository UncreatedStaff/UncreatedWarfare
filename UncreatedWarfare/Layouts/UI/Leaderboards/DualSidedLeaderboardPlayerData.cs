using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Layouts.UI.Leaderboards;

internal class DualSidedLeaderboardPlayerData : IUnturnedUIData
{
    public RingBuffer<DualSidedLeaderboardUI.ChatMessageInfo> VisibleChats;
    public LeaderboardSortColumn[] SortColumns;
    public ModalHandle Modal;
    public int Vote;

    public CSteamID Player { get; }

    public UnturnedUI Owner { get; }

    public UnturnedUIElement? Element => null;

    public DualSidedLeaderboardPlayerData(CSteamID player, UnturnedUI owner)
    {
        Player = player;
        Owner = owner;
        VisibleChats = new RingBuffer<DualSidedLeaderboardUI.ChatMessageInfo>(((DualSidedLeaderboardUI)owner).ChatEntries.Length);

        SortColumns = new LeaderboardSortColumn[2];
        ref LeaderboardSortColumn col = ref SortColumns[0];

        col.ColumnIndex = ((DualSidedLeaderboardUI)owner).DefaultSortColumn;
        col.Descending = ((DualSidedLeaderboardUI)owner).DefaultSortMode;
        SortColumns[1] = col;

        Vote = -1;
    }
}

public struct LeaderboardSortColumn
{
    public int ColumnIndex;
    public bool Descending;
    public int[]? RowMap;
}