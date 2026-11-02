using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Kits;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;

public class KitSignService :
    IEventListener<PlayerKitChanged>,
    IEventListener<SquadMemberJoined>,
    IEventListener<SquadMemberLeft>,
    IEventListener<KitAccessUpdated>,
    IEventListener<KitUpdated>
{
    private readonly SignInstancer _signs;
    private readonly IPlayerService _playerService;
    private readonly ITranslationService _translationService;
    private readonly LoadoutService _loadoutService;
    private readonly IKitDataStore _kitDataStore;

    public KitSignService(SignInstancer signs, IPlayerService playerService, ITranslationService translationService, LoadoutService loadoutService, IKitDataStore kitDataStore)
    {
        _signs = signs;
        _playerService = playerService;
        _translationService = translationService;
        _loadoutService = loadoutService;
        _kitDataStore = kitDataStore;
    }

    /// <summary>
    /// Update all kit signs for all players.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns()
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl((WarfarePlayer?)null);
        else
        {
            GameThread.Queue(() =>
            {
                UpdateSignsIntl((WarfarePlayer?)null);
            });
        }
    }

    /// <summary>
    /// Update all kit signs for <paramref name="player"/>.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(WarfarePlayer player)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(player);
        else
        {
            WarfarePlayer p2 = player;
            GameThread.Queue(() =>
            {
                UpdateSignsIntl(p2);
            });
        }
    }

    /// <summary>
    /// Update all kit signs for a <paramref name="team"/>.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(Team team)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(team);
        else
        {
            Team t2 = team;
            GameThread.Queue(() =>
            {
                UpdateSignsIntl(t2);
            });
        }
    }

    /// <summary>
    /// Update all kit signs for all players matching a predicate
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSignsWhere(Func<Kit, bool>? kitSignPredicate, Func<int, bool>? loadoutSignPredicate, Func<int, bool>? favoriteSignPredicate)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(null, null, kitSignPredicate, loadoutSignPredicate, favoriteSignPredicate);
        else
        {
            Func<Kit, bool>? kitSignPredicate2 = kitSignPredicate;
            Func<int, bool>? loadoutSignPredicate2 = loadoutSignPredicate;
            Func<int, bool>? favoriteSignPredicate2 = favoriteSignPredicate;
            GameThread.Queue(() =>
            {
                UpdateSignsIntl(null, null, kitSignPredicate2, loadoutSignPredicate2, favoriteSignPredicate2);
            });
        }
    }

    /// <summary>
    /// Update all kit signs for a player matching a predicate
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSignsWhere(WarfarePlayer player, Func<Kit, bool>? kitSignPredicate, Func<int, bool>? loadoutSignPredicate, Func<int, bool>? favoriteSignPredicate)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(player, null, kitSignPredicate, loadoutSignPredicate, favoriteSignPredicate);
        else
        {
            WarfarePlayer p2 = player;
            Func<Kit, bool>? kitSignPredicate2 = kitSignPredicate;
            Func<int, bool>? loadoutSignPredicate2 = loadoutSignPredicate;
            Func<int, bool>? favoriteSignPredicate2 = favoriteSignPredicate;
            GameThread.Queue(() =>
            {
                UpdateSignsIntl(p2, null, kitSignPredicate2, loadoutSignPredicate2, favoriteSignPredicate2);
            });
        }
    }

    /// <summary>
    /// Update all kit signs for a team matching a predicate
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSignsWhere(Team team, Func<Kit, bool>? kitSignPredicate, Func<int, bool>? loadoutSignPredicate, Func<int, bool>? favoriteSignPredicate)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(null, team, kitSignPredicate, loadoutSignPredicate, favoriteSignPredicate);
        else
        {
            Team t2 = team;
            Func<Kit, bool>? kitSignPredicate2 = kitSignPredicate;
            Func<int, bool>? loadoutSignPredicate2 = loadoutSignPredicate;
            Func<int, bool>? favoriteSignPredicate2 = favoriteSignPredicate;
            GameThread.Queue(() =>
            {
                UpdateSignsIntl(null, t2, kitSignPredicate2, loadoutSignPredicate2, favoriteSignPredicate2);
            });
        }
    }

    /// <summary>
    /// Update only loadout signs for <paramref name="player"/>.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateLoadoutSigns(WarfarePlayer player)
    {
        if (GameThread.IsCurrent)
            UpdateLoadoutSignsIntl(player);
        else
        {
            WarfarePlayer p2 = player;
            GameThread.Queue(() =>
            {
                UpdateLoadoutSignsIntl(p2);
            });
        }
    }

    /// <summary>
    /// Update only loadout signs for all players.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateLoadoutSigns()
    {
        if (GameThread.IsCurrent)
            UpdateLoadoutSignsIntl(null);
        else
        {
            GameThread.Queue(() =>
            {
                UpdateLoadoutSignsIntl(null);
            });
        }
    }

    /// <summary>
    /// Update all favorite signs.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateFavoriteSigns(WarfarePlayer player)
    {
        if (GameThread.IsCurrent)
            UpdateFavoriteSignsIntl(player);
        else
        {
            WarfarePlayer p2 = player;
            GameThread.Queue(() =>
            {
                UpdateFavoriteSignsIntl(p2);
            });
        }
    }

    /// <summary>
    /// Update all signs for a specific <paramref name="kit"/>.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(Kit kit)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(kit, null);
        else
        {
            Kit k2 = kit;
            GameThread.Queue(() =>
            {
                UpdateSignsIntl(k2, null);
            });
        }
    }

    /// <summary>
    /// Update all signs for a specific <paramref name="kit"/> and <paramref name="player"/>.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(Kit kit, WarfarePlayer player)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(kit, player);
        else
        {
            Kit k2 = kit;
            WarfarePlayer p2 = player;
            GameThread.Queue(() =>
            {
                UpdateSignsIntl(k2, p2);
            });
        }
    }

    /// <summary>
    /// Update all signs for a specific kit's ID.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(string kitId)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(kitId, null);
        else
        {
            string k2 = kitId;
            GameThread.Queue(() =>
            {
                UpdateSignsIntl(k2, null);
            });
        }
    }

    /// <summary>
    /// Update all signs for a specific kit's ID and <paramref name="player"/>.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(string kitId, WarfarePlayer player)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(kitId, player);
        else
        {
            string k2 = kitId;
            WarfarePlayer p2 = player;
            GameThread.Queue(() =>
            {
                UpdateSignsIntl(k2, p2);
            });
        }
    }

    /// <summary>
    /// Update all signs for all kits at a specific class and level for all players.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(PublicKitLevel levelInfo)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(levelInfo, null);
        else
        {
            PublicKitLevel k2 = levelInfo;
            GameThread.Queue(() =>
            {
                UpdateSignsIntl(k2, null);
            });
        }
    }

    /// <summary>
    /// Update all signs for all kits at a specific class and level for a <paramref name="player"/>.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public void UpdateSigns(PublicKitLevel levelInfo, WarfarePlayer player)
    {
        if (GameThread.IsCurrent)
            UpdateSignsIntl(levelInfo, player);
        else
        {
            PublicKitLevel k2 = levelInfo;
            WarfarePlayer p2 = player;
            GameThread.Queue(() =>
            {
                UpdateSignsIntl(k2, p2);
            });
        }
    }

    private void UpdateSignsIntl(WarfarePlayer? player)
    {
        if (player is { IsOnline: false })
            return;

        using IDisposable? profiler = ProfilerUtil.Profile();

        // expects game thread
        if (player == null)
        {
            _signs.UpdateSigns<KitSignInstanceProvider>();
        }
        else
        {
            _signs.UpdateSigns<KitSignInstanceProvider>(player);
        }
    }

    private void UpdateSignsIntl(Team team)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        // expects game thread
        _signs.UpdateSigns<KitSignInstanceProvider>(_translationService.SetOf.PlayersOnTeam(team, KitSignInstanceProvider.TranslationOptions));
    }

    private void UpdateSignsIntl(WarfarePlayer? player, Team? team, Func<Kit, bool>? kitSignPredicate, Func<int, bool>? loadoutSignPredicate, Func<int, bool>? favoriteSignPredicate)
    {
        if (player is { IsOnline: false })
            return;

        using IDisposable? profiler = ProfilerUtil.Profile();

        // expects game thread
        UpdateKitSignsWithPredicatesState state;
        state.KitSignPredicate = kitSignPredicate;
        state.LoadoutSignPredicate = loadoutSignPredicate;
        state.FavoriteSignPredicate = favoriteSignPredicate;
        state.KitDataStore = _kitDataStore;

        SignInstancer.WhereStateRef<KitSignInstanceProvider, UpdateKitSignsWithPredicatesState> predicate
            = static (_, provider, ref state) =>
            {
                int index = provider.LoadoutNumber;
                if (index >= 0)
                {
                    if (index == 0 || state.LoadoutSignPredicate == null)
                        return false;

                    return state.LoadoutSignPredicate(index);
                }

                index = provider.FavoriteIndex;
                if (index >= 0)
                {
                    return state.FavoriteSignPredicate != null && state.FavoriteSignPredicate(index);
                }

                return state.KitSignPredicate != null
                       && provider.KitId != null
                       && state.KitDataStore.CachedKitsById.TryGetValue(provider.KitId, out Kit? cachedKit)
                       && state.KitSignPredicate(cachedKit);
            };

        if (team is not null)
        {
            _signs.UpdateSigns(_translationService.SetOf.PlayersOnTeam(team), ref state, predicate);
        }
        else if (player == null)
        {
            _signs.UpdateSigns(ref state, predicate);
        }
        else
        {
            _signs.UpdateSigns(player, ref state, predicate);
        }
    }

    private struct UpdateKitSignsWithPredicatesState
    {
        internal Func<Kit, bool>? KitSignPredicate;
        internal Func<int, bool>? LoadoutSignPredicate;
        internal Func<int, bool>? FavoriteSignPredicate;
        internal IKitDataStore KitDataStore;
    }

    private void UpdateLoadoutSignsIntl(WarfarePlayer? player)
    {
        if (player is { IsOnline: false })
            return;

        using IDisposable? profiler = ProfilerUtil.Profile();

        // expects game thread
        if (player == null)
        {
            _signs.UpdateSigns<KitSignInstanceProvider>((_, provider) => provider.LoadoutNumber >= 0);
        }
        else
        {
            _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.LoadoutNumber >= 0);
        }
    }

    private void UpdateFavoriteSignsIntl(WarfarePlayer player)
    {
        if (!player.IsOnline)
            return;

        using IDisposable? profiler = ProfilerUtil.Profile();

        // expects game thread
        _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.FavoriteIndex >= 0);
    }

    private void UpdateSignsIntl(Kit kit, WarfarePlayer? player)
    {
        if (player is { IsOnline: false })
            return;

        using IDisposable? profiler = ProfilerUtil.Profile();

        // expects game thread
        if (kit.Type == KitType.Loadout)
        {
            int loadoutId = LoadoutIdHelper.Parse(kit.Id, out CSteamID s64);
            if (loadoutId == -1)
                return;

            if (player == null)
            {
                player = _playerService.GetOnlinePlayerOrNull(s64);
                if (player == null)
                    return;
            }
            else if (player.Steam64.m_SteamID != s64.m_SteamID)
                return;

            _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.LoadoutNumber >= 0);
        }
        else
        {
            string capturedKitId = kit.Id;
            if (player == null)
            {
                _signs.UpdateSigns<KitSignInstanceProvider>((_, provider) => string.Equals(provider.KitId, capturedKitId, StringComparison.Ordinal));
            }
            else
            {
                _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => string.Equals(provider.KitId, capturedKitId, StringComparison.Ordinal));
            }
        }
    }

    private void UpdateSignsIntl(string kitId, WarfarePlayer? player)
    {
        if (player is { IsOnline: false })
            return;

        using IDisposable? profiler = ProfilerUtil.Profile();

        // expects game thread
        int loadoutId = LoadoutIdHelper.Parse(kitId, out CSteamID s64);
        if (loadoutId != -1)
        {
            if (player == null)
            {
                player = _playerService.GetOnlinePlayerOrNull(s64);
                if (player == null)
                    return;
            }
            else if (player.Steam64.m_SteamID != s64.m_SteamID)
                return;

            _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.LoadoutNumber >= 0);
        }
        else
        {
            string capturedKitId = kitId;
            if (player == null)
            {
                _signs.UpdateSigns<KitSignInstanceProvider>((_, provider) => string.Equals(provider.KitId, capturedKitId, StringComparison.Ordinal));
            }
            else
            {
                _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => string.Equals(provider.KitId, capturedKitId, StringComparison.Ordinal));
            }
        }
    }

    private void UpdateSignsIntl(PublicKitLevel levelInfo, WarfarePlayer? player)
    {
        if (player is { IsOnline: false })
            return;

        using IDisposable? profiler = ProfilerUtil.Profile();

        // expects game thread
        if (player == null)
        {
            _signs.UpdateSigns<KitSignInstanceProvider>((_, provider) => provider.LoadoutNumber < 0 && levelInfo.AppliesTo(provider.KitId));
        }
        else
        {
            _signs.UpdateSigns<KitSignInstanceProvider>(player, (_, provider) => provider.LoadoutNumber < 0 && levelInfo.AppliesTo(provider.KitId));
        }
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(PlayerKitChanged e, IServiceProvider serviceProvider)
    {
        UpdateSignsWhere(e.Player.Team, k => k.Id == e.State?.Id || k.MinRequiredSquadMembers.HasValue, _ => true, _ => true);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadMemberJoined e, IServiceProvider serviceProvider)
    {
        UpdateSignsWhere(e.Player.Team, k => k.RequiresSquad, _ => true, _ => true);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(SquadMemberLeft e, IServiceProvider serviceProvider)
    {
        UpdateSignsWhere(e.Player.Team, k => k.RequiresSquad, _ => true, _ => true);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(KitUpdated e, IServiceProvider serviceProvider)
    {
        UpdateSigns(e.Kit);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(KitCreated e, IServiceProvider serviceProvider)
    {
        UpdateSigns(e.Kit);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(KitAccessUpdated e, IServiceProvider serviceProvider)
    {
        UpdateSigns(e.Kit);
        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(e.PlayerId);
        if (player == null)
            return;

        KitPlayerComponent component = player.Component<KitPlayerComponent>();

        if (e.HasAccess)
            component.AddAccessibleKit(e.Kit.Key);
        else
            component.RemoveAccessibleKit(e.Kit.Key);

        if (e.Kit.Type != KitType.Loadout)
        {
            UpdateSignsIntl(e.Kit, player);
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                // recache loadouts
                _ = await _loadoutService.GetLoadouts(e.PlayerId, KitInclude.Cached, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                WarfareModule.Singleton.GlobalLogger.LogError(ex, "Error updating loadouts after access was changed.");
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (player.IsOnline)
                    UpdateLoadoutSignsIntl(player);
            }
        });
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    public void HandleEvent(KitLevelAccessUpdated e, IServiceProvider serviceProvider)
    {
        UpdateSigns(e.KitLevel);
    }
}
