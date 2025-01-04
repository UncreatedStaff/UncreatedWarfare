using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.UI.Leaderboards;

public class LeaderboardSet
{
    public delegate void CreateRow(in LeaderboardRow row, LeaderboardPhaseStatInfo[] stats, Span<double> data);
    public readonly int ColumnCount;
    private readonly LeaderboardPhaseStatInfo[] _stats;

    private readonly LeaderboardPlayer[] _players;
    private readonly double[,] _data;
    private readonly Dictionary<int, string[,]> _formattedData;
    private readonly int[] _sortMapBuffer;

    // reverse look-up player to row index
    private readonly Dictionary<ulong, int> _indexCache;

    // maps indices from their original index to their sorted position. index 0 is ascending #0, 1 is descending #0, etc.
    private readonly int[]?[] _inverseSortMaps;

    public Team Team { get; }

    public LeaderboardRow[] Rows { get; }

    public LeaderboardPhaseStatInfo[] Stats { get; }

    public LeaderboardSet(CreateRow callback, LeaderboardPhaseStatInfo[] stats, IEnumerable<LeaderboardPlayer> players, Team team)
    {
        _formattedData = new Dictionary<int, string[,]>();


        _players = players.ToArray();
        LeaderboardRow[] rows = new LeaderboardRow[_players.Length];

        int visibleColumns = 0;
        for (int i = 0; i < stats.Length; ++i)
        {
            if (stats[i].Visible)
                ++visibleColumns;
        }

        LeaderboardPhaseStatInfo[] visibleStats = new LeaderboardPhaseStatInfo[visibleColumns];
        visibleColumns = -1;
        for (int i = 0; i < stats.Length; ++i)
        {
            LeaderboardPhaseStatInfo stat = stats[i];
            if (stat.Visible)
                visibleStats[++visibleColumns] = stat;
        }

        Stats = visibleStats;

        _data = new double[rows.Length, visibleStats.Length];
        ColumnCount = visibleStats.Length;
        _stats = stats;
        Team = team;

        _indexCache = new Dictionary<ulong, int>(rows.Length);
        for (int i = 0; i < rows.Length; ++i)
        {
            LeaderboardRow row = new LeaderboardRow(i, this);
            rows[i] = row;
            LeaderboardPlayer player = _players[i];
            callback(in row, visibleStats, row.Data);
            _indexCache[player.Player.Steam64.m_SteamID] = i;
        }

        Rows = rows;

        _sortMapBuffer = new int[rows.Length];
        _inverseSortMaps = new int[visibleStats.Length * 2][];
    }

    public double GetStatisticValue(string statName, CSteamID player, double value)
    {
        int statIndex = -1;
        for (int i = 0; i < Stats.Length; ++i)
        {
            if (!Stats[i].Name.Equals(statName, StringComparison.Ordinal))
                continue;

            statIndex = i;
            break;
        }

        if (statIndex == -1)
            return 0;

        int rowIndex = GetRowIndex(player);
        if (rowIndex == -1)
            return 0;

        ref LeaderboardRow row = ref Rows[rowIndex];
        return row.Data[statIndex];
    }

    public int GetRowIndex(CSteamID player)
    {
        return _indexCache.GetValueOrDefault(player.m_SteamID, -1);
    }

    /// <summary>
    /// Maps row indices to which row they would show as for a player's UI if sorted by the given column.
    /// </summary>
    /// <remarks>Use like this <c>for i in rows</c>: <c>ui = ui.players[invSortMap[rowIndex]];</c></remarks>
    public int[] GetSortMap(int columnIndex, bool descending)
    {
        int index = columnIndex * 2 + (descending ? 1 : 0);

        ref int[]? targetArray = ref _inverseSortMaps[index];
        if (targetArray != null)
            return targetArray;

        LeaderboardRow[] rows = Rows;
        for (int i = 0; i < rows.Length; ++i)
            _sortMapBuffer[i] = i;

        IComparer<int> comparer = descending ? new DescendingRowComparer(columnIndex, this) : new AscendingRowComparer(columnIndex, this);
        Array.Sort(_sortMapBuffer, comparer);

        int[] invSortMap = new int[rows.Length];
        for (int i = 0; i < rows.Length; ++i)
            invSortMap[_sortMapBuffer[i]] = i;

        targetArray = invSortMap;
        return invSortMap;
    }

    private sealed class AscendingRowComparer : IComparer<int>
    {
        private readonly int _index;
        private readonly LeaderboardSet _set;

        public AscendingRowComparer(int index, LeaderboardSet set)
        {
            _index = index;
            _set = set;
        }

        public int Compare(int x, int y)
        {
            return _set._data[x, _index].CompareTo(_set._data[y, _index]);
        }
    }
    private sealed class DescendingRowComparer : IComparer<int>
    {
        private readonly int _index;
        private readonly LeaderboardSet _set;

        public DescendingRowComparer(int index, LeaderboardSet set)
        {
            _index = index;
            _set = set;
        }

        public int Compare(int x, int y)
        {
            return _set._data[y, _index].CompareTo(_set._data[x, _index]);
        }
    }

    public readonly struct LeaderboardRow(int index, LeaderboardSet set)
    {
        private readonly LeaderboardSet _set = set;
        private readonly int _index = index;

        public LeaderboardPlayer Player { get => _set._players[_index]; }
        public Span<double> Data { get => MemoryMarshal.CreateSpan(ref _set._data[_index, 0], _set.ColumnCount); }
        public Span<string> FormatData(CultureInfo culture)
        {
            int lcid = culture.LCID;
            if (_set._formattedData.TryGetValue(lcid, out string[,] formats))
            {
                return MemoryMarshal.CreateSpan(ref formats[_index, 0], _set.ColumnCount);
            }

            formats = new string[_set._players.Length, _set.ColumnCount];

            Span<string> allStrings = MemoryMarshal.CreateSpan(ref formats[0, 0], formats.Length);
            Span<double> allData = MemoryMarshal.CreateSpan(ref _set._data[0, 0], _set._data.Length);

            for (int i = 0; i < allStrings.Length; ++i)
            {
                LeaderboardPhaseStatInfo stat = _set._stats[i % _set.ColumnCount];
                allStrings[i] = allData[i].ToString(stat.NumberFormat ?? "0.##", culture);
            }

            _set._formattedData.Add(lcid, formats);
            return allStrings.Slice(_index * _set.ColumnCount, _set.ColumnCount);
        }
    }
}