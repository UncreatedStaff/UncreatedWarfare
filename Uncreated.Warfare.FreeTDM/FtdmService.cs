using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SDG.Framework.Utilities;
using Steamworks;
using System;
using Uncreated.Warfare.Configuration.JsonConverters;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Lobby;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FreeTeamDeathmatch;

internal class FtdmService : ILayoutPhaseListener<ActionPhase>
{
    private const float OutOfBoundsWarningTime = 7.5f;

    private readonly Layout _layout;
    private readonly ZoneStore _zoneStore;
    private readonly ILogger<FtdmService> _logger;
    private readonly TimeTranslations _timeTranslations;
    private readonly IPlayerService _playerService;
    private readonly LobbyZoneManager _lobbyManager;
    private readonly IConfiguration _configuration;
    private readonly ChatService _chatService;
    private readonly KitRequestService _kitRequestService;
    private readonly IKitDataStore _kitDataStore;
    private readonly FtdmTranslations _translations;
    private readonly FtdmDualSidedTeamManager _teamManager;
    private ITrackingProximity<WarfarePlayer>? _playAreaCollider;
    private IDisposable? _onChange;

    private LinearDictionary<Team, string[]>? _allowedKits;

    private LinearDictionary<Team, IEventBasedProximity<WarfarePlayer>>? _friendlyZoneColliders;

    public bool IsInActionPhase { get; private set; }

    public FtdmService(
        Layout layout,
        ZoneStore zoneStore,
        ILogger<FtdmService> logger,
        TranslationInjection<FtdmTranslations> translations,
        TranslationInjection<TimeTranslations> timeTranslations,
        IPlayerService playerService,
        LobbyZoneManager lobbyManager,
        IConfiguration configuration,
        ChatService chatService,
        KitRequestService kitRequestService,
        IKitDataStore kitDataStore)
    {
        _layout = layout;
        _zoneStore = zoneStore;
        _logger = logger;
        _timeTranslations = timeTranslations.Value;
        _playerService = playerService;
        _lobbyManager = lobbyManager;
        _configuration = configuration;
        _chatService = chatService;
        _kitRequestService = kitRequestService;
        _kitDataStore = kitDataStore;
        _translations = translations.Value;
        _teamManager = layout.TeamManager as FtdmDualSidedTeamManager
                       ?? throw new GameConfigurationException("Expected FtdmDualSidedTeamManager.");

        _onChange = ChangeToken.OnChange(
            () => _configuration.GetReloadToken(),
            me =>
            {
                me.OnConfigUpdate();
            },
            this);

        OnConfigUpdate();
    }

    private void OnConfigUpdate()
    {
        LinearDictionary<Team, string[]> dict = new LinearDictionary<Team, string[]>(_teamManager.AllTeams.Count);
        foreach (IConfigurationSection section in _configuration.GetSection("Kits").GetChildren())
        {
            Team? team = _teamManager.FindTeam(section.Key);
            if (team == null)
            {
                _logger.LogWarning($"Invalid team in 'Kits': {section.Key}.");
                continue;
            }

            string[] kits = section.Get<string[]>() ?? Array.Empty<string>();
            dict[team] = kits;

            if (kits.Length == 0)
            {
                _logger.LogWarning($"Team {team} does not define any default kits.");
            }
        }

        _allowedKits = dict;
    }

    private void OnUpdate()
    {
        if (!IsInActionPhase || _playAreaCollider == null || _friendlyZoneColliders == null)
            return;

        float time = Time.realtimeSinceStartup;
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            if (player.IsOnDuty || player.IsDisconnecting || player.UnturnedPlayer.life.isDead)
                continue;

            FtdmPlayerComponent comp = player.Component<FtdmPlayerComponent>();
            if (_playAreaCollider.Contains(player) || !_lobbyManager.Disabled && _zoneStore.IsInLobby(player))
            {
                if (comp.HasExitedSpawnSinceRespawned
                    && _friendlyZoneColliders.TryGetValue(player.Team, out IEventBasedProximity<WarfarePlayer>? prox)
                    && ((ITrackingProximity<WarfarePlayer>)prox).Contains(player))
                {
                    if (float.IsNaN(comp.LastInFriendlySpawn))
                        comp.LastInFriendlySpawn = time;
                    else if (time - comp.LastInFriendlySpawn > 1f)
                    {
                        // this shouldn't really happen
                        player.UnturnedPlayer.life.askDamage(101, Vector3.up, EDeathCause.ARENA, ELimb.SPINE, CSteamID.Nil, out _);
                    }
                    else if (time - comp.LastInFriendlySpawn > 0.25f)
                    {
                        HandlePlayerEntersEnemySpawnOrTriesToReenterSpawn(player, player.Team, prox);
                    }
                }
                else
                    comp.LastInFriendlySpawn = float.NaN;

                comp.LastInPlayArea = time;
                continue;
            }

            float timeLeft = time - comp.LastInPlayArea;
            if (float.IsNaN(comp.LastInPlayArea))
            {
                comp.LastInPlayArea = time;
                comp.LastOutOfBoundsUIUpdate = float.NaN;
            }
            else
            {
                if (timeLeft > OutOfBoundsWarningTime)
                {
                    player.UnturnedPlayer.life.askDamage(101, Vector3.up, EDeathCause.ARENA, ELimb.SPINE, CSteamID.Nil, out _);
                }
                else if (float.IsNaN(comp.LastOutOfBoundsUIUpdate) || time - comp.LastOutOfBoundsUIUpdate > 1f)
                {
                    comp.LastOutOfBoundsUIUpdate = time;
                    ToastMessage toast = new ToastMessage(
                        ToastMessageStyle.FlashingWarning,
                        _translations.EnteredEnemyTerritory.Translate(
                            TimeAddon.ToLongTimeString(_timeTranslations, Mathf.RoundToInt(OutOfBoundsWarningTime), player.Locale.LanguageInfo),
                            player
                        )
                    )
                    {
                        OverrideDuration = OutOfBoundsWarningTime
                    };

                    player.Component<ToastManager>().SkipExpiration(ToastMessageStyle.FlashingWarning);
                    player.SendToast(toast);
                }
            }
        }
    }

    private void HandlePlayerEntersEnemySpawnOrTriesToReenterSpawn(WarfarePlayer player, Team enemySpawnTeam, IProximity spawn)
    {
        // turns the player around and teleports them a small distance away from the zone in the direction they came from.
        Vector3 playerPos = player.Position;
        Vector3 closestPoint = spawn.GetNearestPointOnBorder(in playerPos);
        Vector3 vector = closestPoint - playerPos;

        Vector3 newPosition = closestPoint + vector.normalized;

        newPosition.y = TerrainUtility.GetHighestPoint(in newPosition, float.NaN) + 0.125f;
        player.UnturnedPlayer.teleportToLocation(newPosition, Quaternion.LookRotation(vector).eulerAngles.y);

        if (enemySpawnTeam.IsFriendly(player.Team))
        {
            _chatService.Send(player, _translations.ReenteredSpawn);
        }
        else
        {
            _chatService.Send(player, _translations.EnteredTeamSpawn, enemySpawnTeam.Faction);
        }
    }

    private void HandlePlayerExitsFriendlySpawn(WarfarePlayer player)
    {
        FtdmPlayerComponent comp = player.Component<FtdmPlayerComponent>();
        comp.HasExitedSpawnSinceRespawned = true;
    }

    private static void HandlePlayerEntersPlayArea(IEventBasedProximity<WarfarePlayer> prox, WarfarePlayer obj)
    {
        float lastUI = obj.Component<FtdmPlayerComponent>().LastOutOfBoundsUIUpdate;
        if (!float.IsNaN(lastUI) && Time.realtimeSinceStartup - lastUI <= OutOfBoundsWarningTime)
        {
            obj.Component<ToastManager>().SkipExpiration(ToastMessageStyle.FlashingWarning);
        }
    }

    private void PlayerEnteredSpawnZone(IEventBasedProximity<WarfarePlayer> prox, WarfarePlayer obj)
    {
        if (_friendlyZoneColliders == null || obj.IsOnDuty || obj.IsDisconnecting)
            return;

        if (!_friendlyZoneColliders.TryGetKey(prox, out Team? team))
            return;

        if (!team.IsFriendly(obj.Team) || obj.Component<FtdmPlayerComponent>().HasExitedSpawnSinceRespawned)
        {
            HandlePlayerEntersEnemySpawnOrTriesToReenterSpawn(obj, team, prox);
        }
        else if (_allowedKits?.TryGetValue(obj.Team, out string[]? allowedKits) is true)
        {
            if (allowedKits is not { Length: > 0 })
                return;

            // give the player a random kit when they spawn
            string kitId = allowedKits[RandomUtility.GetIndex(allowedKits)];
            WarfarePlayer player = obj;
            Task.Run(async () =>
            {
                try
                {
                    Kit? kit = await _kitDataStore.QueryKitAsync(kitId, KitInclude.Giveable);
                    if (kit == null)
                    {
                        _logger.LogWarning($"Unknown kit: {kitId}.");
                    }
                    else
                    {
                        await _kitRequestService.GiveKitAsync(player, new KitBestowData(kit) { Silent = true }, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error giving kit {kitId}.");
                }
            });
        }
    }

    private void PlayerExitedSpawnZone(IEventBasedProximity<WarfarePlayer> prox, WarfarePlayer obj)
    {
        if (_friendlyZoneColliders == null)
            return;

        if (_friendlyZoneColliders.TryGetKey(prox, out Team? team) && team.IsFriendly(obj.Team))
        {
            HandlePlayerExitsFriendlySpawn(obj);
        }
    }

    UniTask ILayoutPhaseListener<ActionPhase>.OnPhaseStarted(ActionPhase phase, CancellationToken token)
    {
        Zone? playArea = _zoneStore.SearchZone(_teamManager.Location.PlayArea);
        if (playArea == null)
        {
            _logger.LogWarning($"Play area zone not found: {_teamManager.Location.PlayArea}.");
            _playAreaCollider = null;
        }
        else
        {
            _playAreaCollider = _zoneStore.CreateColliderForZone(playArea);
            //_playAreaCollider.OnObjectExited += HandlePlayerExitsPlayArea;
            _playAreaCollider.OnObjectEntered += HandlePlayerEntersPlayArea;
        }

        _friendlyZoneColliders = new LinearDictionary<Team, IEventBasedProximity<WarfarePlayer>>(_teamManager.Spawns.Count);
        foreach (KeyValuePair<Team, FtdmLocationSpawn> spawn in _teamManager.Spawns)
        {
            Zone? zone = _zoneStore.SearchZone(spawn.Value.Zone);
            if (zone == null)
            {
                _logger.LogWarning($"Spawn zone not found: {spawn.Value.Zone}.");
                continue;
            }

            ITrackingProximity<WarfarePlayer> collider = _zoneStore.CreateColliderForZone(zone);

            _friendlyZoneColliders[spawn.Key] = collider;

            collider.OnObjectEntered += PlayerEnteredSpawnZone;
            collider.OnObjectExited += PlayerExitedSpawnZone;
        }

        if (!IsInActionPhase)
        {
            TimeUtility.updated += OnUpdate;
            IsInActionPhase = true;
        }

        _logger.LogInformation($"Play area: '{playArea?.Name}', {_friendlyZoneColliders.Count} spawns.");
        return UniTask.CompletedTask;
    }

    UniTask ILayoutPhaseListener<ActionPhase>.OnPhaseEnded(ActionPhase phase, CancellationToken token)
    {
        if (_friendlyZoneColliders != null)
        {
            foreach (IEventBasedProximity<WarfarePlayer> prox in _friendlyZoneColliders.Values)
            {
                prox.OnObjectEntered -= PlayerEnteredSpawnZone;
                prox.OnObjectExited -= PlayerExitedSpawnZone;
                if (prox is IDisposable disp)
                    disp.Dispose();
            }

            _friendlyZoneColliders.Clear();
        }

        if (_playAreaCollider != null)
        {
            _playAreaCollider.OnObjectEntered -= HandlePlayerEntersPlayArea;
            //_playAreaCollider.OnObjectExited -= HandlePlayerExitsPlayArea;
            if (_playAreaCollider is IDisposable playAreaDisposable)
                playAreaDisposable.Dispose();
            _playAreaCollider = null;
        }

        if (IsInActionPhase)
        {
            TimeUtility.updated -= OnUpdate;
            IsInActionPhase = false;
        }

        Interlocked.Exchange(ref _onChange, null)?.Dispose();

        return UniTask.CompletedTask;
    }
}