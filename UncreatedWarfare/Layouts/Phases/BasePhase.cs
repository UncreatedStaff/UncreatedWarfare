using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tweaks;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Layouts.Phases;

public abstract class BasePhase<TTeamSettings> : ILayoutPhase,
    IEventListener<PlayerExitedZone>,
    IEventListener<PlayerTeamChanged>,
    IEventListener<PlayerJoined>
    where TTeamSettings : PhaseTeamSettings
{
    private bool _anyInvincible;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPlayerService _playerService;
    private readonly ZoneStore _zoneStore;
    protected readonly ITeamManager<Team> TeamManager;
    protected readonly Layout Layout;

    [field: MaybeNull, AllowNull]
    protected ILogger Logger => field ??= _loggerFactory.CreateLogger(GetType());

    public bool IsActive { get; private set; }

    /// <summary>
    /// If all players can't take damage. Overrides <see cref="PhaseTeamSettings.Invincible"/> if <see langword="true"/>.
    /// </summary>
    [UsedImplicitly]
    public bool Invincible { get; set; }

    [UsedImplicitly]
    public TimeSpan Duration { get; set; } = TimeSpan.MinValue;

    [UsedImplicitly]
    public TimeSpan TimeElapsedSinceActive => IsActive ? DateTime.Now - TimeStarted : TimeSpan.Zero;

    public DateTime TimeStarted { get; private set; }

    /// <summary>
    /// Display name of the phase on the popup toast for all teams.
    /// </summary>
    [UsedImplicitly]
    public TranslationList? Name { get; set; }

    /// <summary>
    /// Per-team behavior of the phase.
    /// </summary>
    [UsedImplicitly]
    public IReadOnlyList<TTeamSettings>? Teams { get; set; }

    /// <inheritdoc />
    public IConfiguration Configuration { get; }

    protected BasePhase(IServiceProvider serviceProvider, IConfiguration config)
    {
        Configuration = config;
        TeamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();
        _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _zoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        Layout = serviceProvider.GetRequiredService<Layout>();
    }

    public virtual UniTask InitializePhaseAsync(CancellationToken token = default)
    {
        _anyInvincible = Invincible;
        if (Teams is not { Count: > 0 })
            return UniTask.CompletedTask;

        int i = 0;
        foreach (TTeamSettings settings in Teams)
        {
            settings.TeamInfo = TeamManager.FindTeam(settings.Team);
            settings.Configuration = Configuration.GetSection($"Teams:{i.ToString(CultureInfo.InvariantCulture)}");
            ++i;

            _anyInvincible |= settings.Invincible;
        }

        return UniTask.CompletedTask;
    }

    public virtual async UniTask BeginPhaseAsync(CancellationToken token = default)
    {
        TimeStarted = DateTime.Now;
        IsActive = true;

        await UniTask.SwitchToMainThread(token);

        foreach (WarfarePlayer player in _playerService.GetThreadsafePlayerList())
        {
            UpdatePlayerInvinciblity(player);
        }
    }

    public virtual UniTask EndPhaseAsync(CancellationToken token = default)
    {
        IsActive = false;
        return UniTask.CompletedTask;
    }

    void IEventListener<PlayerTeamChanged>.HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        // make sure Invincible is respected if the player changes to that team during the phase
        if (!IsActive || e.Player.IsDisconnecting || !e.Player.IsOnline)
            return;

        UpdatePlayerInvinciblity(e.Player);
    }

    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        // make sure Invincible is respected if the player joins the server during the phase
        if (!IsActive || e.Player.IsDisconnecting || !e.Player.IsOnline)
            return;

        UpdatePlayerInvinciblity(e.Player);
    }

    /// <summary>
    /// Determines if a player should currently be invincible.
    /// </summary>
    protected virtual bool ShouldBeInvincible(WarfarePlayer player)
    {
        if (Invincible)
            return true;

        if (!player.Team.IsValid)
            return false;

        Team closureTeam = player.Team;
        TTeamSettings? teamSettings = Teams?.FirstOrDefault(x => x.TeamInfo == closureTeam);

        return teamSettings is { Invincible: true };
    }

    /// <summary>
    /// Double-check whether the player should be currently invincible.
    /// </summary>
    public void UpdatePlayerInvinciblity(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        if (!_anyInvincible)
            return;

        if (ShouldBeInvincible(player))
        {
            // Re-initialization of GodPlayerComponent will reset this if this is the last phase
            PlayerInjureComponent? injure = player.ComponentOrNull<PlayerInjureComponent>();
            if (injure is { State: PlayerHealthState.Injured })
                injure.Revive();
            player.UnturnedPlayer.life.sendRevive();
            player.Component<GodPlayerComponent>().SetGameplayActive(true);
        }
        else
        {
            player.Component<GodPlayerComponent>().SetGameplayActive(false);
        }
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerExitedZone>.HandleEvent(PlayerExitedZone e, IServiceProvider serviceProvider)
    {
        // code for PhaseTeamSettings.Grounded
        if (!IsActive || e.Player.IsDisconnecting || !e.Player.IsOnline)
            return;

        if (e.Player.IsOnDuty || !e.Player.Team.IsValid)
            return;

        TTeamSettings? settings = Teams?.FirstOrDefault(x => x.TeamInfo == e.Player.Team);
        if (settings is not { Grounded: true })
            return;

        FactionInfo faction = e.Player.Team.Faction;
        if (_zoneStore.IsInMainBase(e.Player, faction))
        {
            return;
        }

        // find closest point of all friendly main bases
        Vector3 position = e.Player.Position;

        Vector3 nearestLocation = default;
        float nearestLocationDist = -1;
        foreach (ZoneProximity zoneProxy in _zoneStore.ProximityZones!)
        {
            Zone zone = zoneProxy.Zone;
            if (zone.Type != ZoneType.MainBase || !string.Equals(zone.Faction, faction.FactionId, StringComparison.Ordinal))
                continue;

            Vector3 pos = zoneProxy.Proximity.GetNearestPointOnBorder(in position);

            float dist = (pos - position).sqrMagnitude;
            if (nearestLocationDist >= 0 && dist >= nearestLocationDist)
                continue;

            nearestLocation = pos;
            nearestLocationDist = dist;
        }

        if (nearestLocationDist < 0)
            return;

        // vector facing back into the zone from the player
        Vector3 vector = nearestLocation - position;

        Vector3 newPosition = nearestLocation + vector.normalized;

        newPosition.y = TerrainUtility.GetHighestPoint(in newPosition, float.NaN);

        // teleport player back a meter and turn them around
        e.Player.UnturnedPlayer.teleportToLocationUnsafe(newPosition, Quaternion.LookRotation(vector).eulerAngles.y);
    }
}