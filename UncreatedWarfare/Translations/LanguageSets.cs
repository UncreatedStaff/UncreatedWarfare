using System.ComponentModel;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;

namespace Uncreated.Warfare.Translations;

/// <summary>
/// Service responsible for grouping players into groups so that translations can be done in bulk.
/// Players can be grouped by team, language, culture, time zone, and IMGUI setting.
/// </summary>
public class LanguageSets
{
    /// <summary>
    /// Signature for <see cref="LanguageSets.PlayersWhere{TState}(ref TState,WhereStateRef{TState},TranslationOptions)"/>
    /// </summary>
    public delegate bool WhereStateRef<TState>(WarfarePlayer player, ref TState state);

    /// <summary>
    /// Signature for <see cref="LanguageSets.PlayersWhere{TState}(TState,WhereState{TState},TranslationOptions)"/>
    /// </summary>
    public delegate bool WhereState<in TState>(WarfarePlayer player, TState state);

    /// <summary>
    /// Default translation options for groupings.
    /// </summary>
    public const TranslationOptions DefaultTranslationOptions = TranslationOptions.PerLanguageTranslation | TranslationOptions.PerCultureTranslation;

    private readonly IPlayerService _playerService;

    private readonly Stack<LanguageGroup> _languageGroupPoolMainThread;

    public LanguageSets(IPlayerService playerService)
    {
        _playerService = playerService;
        _languageGroupPoolMainThread = new Stack<LanguageGroup>(16);
    }

    /// <summary>
    /// Groups all online players by their locale.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator AllPlayers(TranslationOptions options = DefaultTranslationOptions)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        LanguageSetBuilder builder = new LanguageSetBuilder(_playerService.Count, this, options);

        if (GameThread.IsCurrent)
        {
            foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                builder.AddPlayer(player);
            }
        }
        else
        {
            foreach (WarfarePlayer player in _playerService.GetThreadsafePlayerList())
            {
                builder.AddPlayer(player);
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// Groups all online players besides <paramref name="steam64"/> by their locale.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator AllPlayersExcept(ulong steam64, TranslationOptions options = DefaultTranslationOptions)
    {
        return PlayersWhere(steam64, static (pl, steam64) => pl.Steam64.m_SteamID != steam64, options);
    }

    /// <summary>
    /// Groups all online players besides <paramref name="steam64"/> by their locale.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator AllPlayersExcept(CSteamID steam64, TranslationOptions options = DefaultTranslationOptions)
    {
        return PlayersWhere(steam64, static (pl, steam64) => pl.Steam64.m_SteamID != steam64.m_SteamID, options);
    }

    /// <summary>
    /// Groups all online players besides <paramref name="player"/> by their locale.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator AllPlayersExcept(IPlayer player, TranslationOptions options = DefaultTranslationOptions)
    {
        return PlayersWhere(player, static (pl, player) => pl.Steam64.m_SteamID != player.Steam64.m_SteamID, options);
    }

    // ReSharper disable InconsistentNaming
    /// <inheritdoc cref="AllPlayersExcept(TranslationOptions,ulong[])"/>
    public LanguageSetEnumerator AllPlayersExcept(params ulong[] steam64s)
    {
        return AllPlayersExcept(DefaultTranslationOptions, steam64s);
    }
    /// <summary>
    /// Groups all online players besides those in <paramref name="steam64s"/> by their locale.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator AllPlayersExcept(TranslationOptions options, params ulong[] steam64s)
    {
        return PlayersWhere(steam64s, static (pl, steam64s) => Array.IndexOf(steam64s, pl.Steam64.m_SteamID) == -1, options);
    }

    /// <inheritdoc cref="AllPlayersExcept(TranslationOptions,CSteamID[])"/>
    public LanguageSetEnumerator AllPlayersExcept(params CSteamID[] steam64s)
    {
        return AllPlayersExcept(DefaultTranslationOptions, steam64s);
    }
    /// <summary>
    /// Groups all online players besides those in <paramref name="steam64s"/> by their locale.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator AllPlayersExcept(TranslationOptions options, params CSteamID[] steam64s)
    {
        return PlayersWhere(steam64s, static (pl, steam64s) => Array.IndexOf(steam64s, pl.Steam64) == -1, options);
    }

    /// <inheritdoc cref="AllPlayersExcept(TranslationOptions,IPlayer[])"/>
    public LanguageSetEnumerator AllPlayersExcept(params IPlayer[] players)
    {
        return AllPlayersExcept(DefaultTranslationOptions, players);
    }
    /// <summary>
    /// Groups all online players besides those in <paramref name="players"/> by their locale.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator AllPlayersExcept(TranslationOptions options, params IPlayer[] players)
    {
        return PlayersWhere(players, static (pl, players) => Array.FindIndex(players, pl2 => pl.Steam64.m_SteamID == pl2.Steam64.m_SteamID) == -1, options);
    }
    // ReSharper restore InconsistentNaming

    /// <summary>
    /// Groups all online players on a team friendly to <paramref name="team"/>.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator Friendlies(Team team, TranslationOptions options = DefaultTranslationOptions)
    {
        return PlayersWhere(team, static (pl, team) => pl.Team.IsFriendly(team), options);
    }

    /// <summary>
    /// Groups all online players on a team opposing <paramref name="team"/>.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator Opponents(Team team, TranslationOptions options = DefaultTranslationOptions)
    {
        return PlayersWhere(team, static (pl, team) => pl.Team.IsOpponent(team), options);
    }

    /// <summary>
    /// Groups all online players on a <paramref name="team"/>.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator PlayersOnTeam(Team team, TranslationOptions options = DefaultTranslationOptions)
    {
        return PlayersWhere(team, static (pl, team) => pl.Team == team, options);
    }

    /// <summary>
    /// Groups all online players on any playable team.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator PlayersOnTeam(TranslationOptions options = DefaultTranslationOptions)
    {
        return PlayersWhere(static pl => pl.Team.IsValid, options);
    }

    /// <summary>
    /// Groups all online players within a region area, using <see cref="Regions.checkArea"/>.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator PlayersInArea(byte x, byte y, byte area, TranslationOptions options = DefaultTranslationOptions)
    {
        AreaState state;
        state.X = x;
        state.Y = y;
        state.Area = area;
        return PlayersWhere(ref state, static (player, ref state) =>
        {
            PlayerMovement movement = player.UnturnedPlayer.movement;
            return Regions.checkArea(movement.region_x, movement.region_y, state.X, state.Y, state.Area);
        }, options);
    }

    private struct AreaState
    {
        public byte X, Y, Area;
    }

    /// <summary>
    /// Groups all online players matching a predicate.
    /// </summary>
    /// <param name="selector">A lambda expression determining whether or not to select a player.</param>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator PlayersWhere(Func<WarfarePlayer, bool> selector, TranslationOptions options = DefaultTranslationOptions)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        LanguageSetBuilder builder = new LanguageSetBuilder(_playerService.Count / 2, this, options);

        if (GameThread.IsCurrent)
        {
            foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                if (!selector(player))
                    continue;

                builder.AddPlayer(player);
            }
        }
        else
        {
            foreach (WarfarePlayer player in _playerService.GetThreadsafePlayerList())
            {
                if (!selector(player))
                    continue;

                builder.AddPlayer(player);
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// Groups all online players matching a predicate.
    /// </summary>
    /// <param name="state">Parameter passed to <paramref name="selector"/>.</param>
    /// <param name="selector">A lambda expression determining whether or not to select a player.</param>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator PlayersWhere<TState>(TState state, WhereState<TState> selector, TranslationOptions options = DefaultTranslationOptions)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        LanguageSetBuilder builder = new LanguageSetBuilder(_playerService.Count / 2, this, options);

        if (GameThread.IsCurrent)
        {
            foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                if (!selector(player, state))
                    continue;

                builder.AddPlayer(player);
            }
        }
        else
        {
            foreach (WarfarePlayer player in _playerService.GetThreadsafePlayerList())
            {
                if (!selector(player, state))
                    continue;

                builder.AddPlayer(player);
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// Groups all online players matching a predicate.
    /// </summary>
    /// <param name="state">Parameter passed to <paramref name="selector"/> by reference.</param>
    /// <param name="selector">A lambda expression determining whether or not to select a player.</param>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator PlayersWhere<TState>(ref TState state, WhereStateRef<TState> selector, TranslationOptions options = DefaultTranslationOptions)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        LanguageSetBuilder builder = new LanguageSetBuilder(_playerService.Count / 2, this, options);

        if (GameThread.IsCurrent)
        {
            foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                if (!selector(player, ref state))
                    continue;

                builder.AddPlayer(player);
            }
        }
        else
        {
            foreach (WarfarePlayer player in _playerService.GetThreadsafePlayerList())
            {
                if (!selector(player, ref state))
                    continue;

                builder.AddPlayer(player);
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// Groups all online players within a <paramref name="set"/>.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator PlayersIn(IEnumerable<WarfarePlayer> set, TranslationOptions options = DefaultTranslationOptions)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        LanguageSetBuilder builder = new LanguageSetBuilder(set is ICollection<WarfarePlayer> c ? c.Count : 16, this, options);

        foreach (WarfarePlayer player in set)
        {
            if (player.IsOnline)
                builder.AddPlayer(player);
        }

        return builder.Build();
    }

    /// <summary>
    /// Groups all online players whose Steam IDs are within a <paramref name="set"/>.
    /// </summary>
    /// <param name="options">Modifiers for how players should be grouped during translation.</param>
    public LanguageSetEnumerator PlayersIn(ISet<ulong> set, TranslationOptions options = DefaultTranslationOptions)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        LanguageSetBuilder builder = new LanguageSetBuilder(set.Count, this, options);

        if (GameThread.IsCurrent)
        {
            foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                if (!set.Contains(player.Steam64.m_SteamID))
                    continue;

                builder.AddPlayer(player);
            }
        }
        else
        {
            foreach (ulong id in set)
            {
                if (_playerService.GetOnlinePlayerOrNullThreadSafe(id) is not { } player)
                    continue;

                builder.AddPlayer(player);
            }
        }

        return builder.Build();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal LanguageGroup RentLanguageGroup()
    {
        if (!GameThread.IsCurrent || _languageGroupPoolMainThread.Count == 0)
            return new LanguageGroup(this);

        return _languageGroupPoolMainThread.Pop();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal void ReturnLanguageGroup(LanguageGroup group)
    {
        if (GameThread.IsCurrent)
            _languageGroupPoolMainThread.Push(group);
    }

    private struct LanguageSetBuilder
    {
        private readonly LanguageSets _inst;
        private readonly TranslationOptions _options;
        private ValuePooledList<LanguageSet> _sets;
        private ValuePooledList<WarfarePlayer> _players;

        public LanguageSetBuilder(int playerCount, LanguageSets inst, TranslationOptions options)
        {
            _inst = inst;
            _options = options;
            _sets = new ValuePooledList<LanguageSet>(4);
            _players = new ValuePooledList<WarfarePlayer>(playerCount);
        }

        public LanguageSetEnumerator Build()
        {
            if (_players.Count == 0)
            {
                _players.Dispose();
                _sets.Dispose();
                return LanguageSetEnumerator.Empty;
            }

            WarfarePlayer[] playersArray = _players.GetUnderlyingArray();
            for (int i = 0; i < _sets.Count; ++i)
            {
                ref LanguageSet langSet = ref _sets[i];
                langSet.Group!.Players = new ArraySegment<WarfarePlayer>(playersArray, langSet.Group!.StartIndex, langSet.Group.Count);
            }


            return new LanguageSetEnumerator(_sets.GetUnderlyingArray(), _sets.Count, playersArray);
        }

        public void AddPlayer(WarfarePlayer player)
        {
            if ((_options & TranslationOptions.PerPlayerTranslation) == 0)
            {
                int ct = _sets.Count;
                for (int i = 0; i < ct; ++i)
                {
                    LanguageGroup languageSet = _sets[i].Group!;
                    if (!languageSet.Includes(player, _options))
                    {
                        continue;
                    }

                    _players.AllocateAt(languageSet.StartIndex + languageSet.Count) = player;
                    ++languageSet.Count;
                    for (int j = i + 1; j < _sets.Count; ++j)
                    {
                        ++_sets[j].Group!.StartIndex;
                    }

                    return;
                }

                ref LanguageSet newSet = ref _sets.Allocate();
                LanguageGroup group = _inst.RentLanguageGroup();
                group.Reset(player.Locale.LanguageInfo, player.Locale.CultureInfo, player.Locale.TimeZone, player.Save.IMGUI, player.Team);
                group.StartIndex = _players.Count;
                group.Count = 1;

                newSet = new LanguageSet(group);
                _players.Allocate() = player;
            }
            else
            {
                _sets.Allocate() = new LanguageSet(player);
            }
        }
    }
}