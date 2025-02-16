using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Deployment;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.Deployment;

[PlayerComponent]
public class DeploymentComponent : MonoBehaviour, IPlayerComponent, IEventListener<PlayerDied>
{
    private const float TickSpeedSeconds = 0.25f;

    private DeploymentTranslations _translations = null!;
    
    private float _deploymentTimeStarted;
    private Coroutine? _deploymentCoroutine;
    private CooldownManager? _cooldownManager;
    private FobManager? _fobManager;

#nullable disable

    private ZoneStore _zoneStore;
    private IPlayerService _playerService;
    private ChatService _chatService;

    public WarfarePlayer Player { get; private set; }

#nullable restore

    // TODO: this value should also be set on exiting main base
    private DateTime _lastDeployedFromSafeZone;

    public IDeployable? CurrentDeployment { get; private set; }
    public TimeSpan DeploymentTimeLeft => _deploymentTimeStarted == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(Time.realtimeSinceStartup - _deploymentTimeStarted);
    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    public void Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _translations = serviceProvider.GetRequiredService<TranslationInjection<DeploymentTranslations>>().Value;
        _zoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _cooldownManager = serviceProvider.GetService<CooldownManager>();
        _fobManager = serviceProvider.GetService<FobManager>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _lastDeployedFromSafeZone = DateTime.UtcNow;
    }

    [EventListener(Priority = -100)]
    void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        _lastDeployedFromSafeZone = DateTime.UtcNow;
    }

    public TimeSpan GetTimeDeployed()
    {
        return DateTime.UtcNow - _lastDeployedFromSafeZone;
    }

    public void CancelDeployment(bool chat)
    {
        GameThread.AssertCurrent();

        if (CurrentDeployment == null)
            return;

        CurrentDeployment = null;
        if (_deploymentCoroutine != null)
        {
            StopCoroutine(_deploymentCoroutine);
            _deploymentCoroutine = null;
        }

        _deploymentTimeStarted = 0f;
        if (chat)
        {
            _chatService.Send(Player, _translations.DeployCancelled);
        }
    }

    public bool TryStartDeployment(IDeployable location, DeploySettings settings = default)
    {
        GameThread.AssertCurrent();
        if (CurrentDeployment != null)
        {
            CancelDeployment(true);
        }

        if (!settings.AllowInjured && Player.IsInjured())
        {
            if (!settings.DisableInitialChatUpdates)
                _chatService.Send(Player, _translations.DeployInjured);

            return false;
        }

        if (!settings.DisableCheckingForCooldown && _cooldownManager != null && _cooldownManager.HasCooldown(Player, settings.CooldownType ?? KnownCooldowns.Deploy, out Cooldown cooldown))
        {
            if (!settings.DisableInitialChatUpdates)
                _chatService.Send(Player, _translations.DeployCooldown, cooldown);

            return false;
        }

        if (!settings.AllowCombat && _cooldownManager != null && _cooldownManager.HasCooldown(Player, KnownCooldowns.Combat, out cooldown))
        {
            if (!settings.DisableInitialChatUpdates)
                _chatService.Send(Player, _translations.DeployInCombat, cooldown);

            return false;
        }

        if (!settings.AllowNearbyEnemies && AreEnemiesNearby(settings.NearbyEnemyRange ?? DeploymentService.DefaultNearbyEnemyRange))
        {
            if (!settings.DisableInitialChatUpdates)
                _chatService.Send(Player, _translations.DeployEnemiesNearby, location);

            return false;
        }

        if (!location.CheckDeployableTo(Player, _chatService, _translations, in settings))
        {
            return false;
        }

        bool isInMain = _zoneStore.IsInMainBase(Player);

        IFob? deployFrom = null;

        if (!isInMain)
        {
            //if (_fobManager == null || !_fobManager.IsOnFob(Player, out deployFrom))
            //{
            //    if (!settings.DisableInitialChatUpdates)
            //        _chatService.Send(Player, _translations.DeployNotNearFOB); // todo add Insurgency special message
            //
            //    return false;
            //}
        }

        if (deployFrom != null && !deployFrom.CheckDeployableFrom(Player, _chatService, _translations, in settings, location))
        {
            return false;
        }

        if (location.Equals(deployFrom))
        {
            if (!settings.DisableInitialChatUpdates)
                _chatService.Send(Player, _translations.DeployableAlreadyOnFOB);

            return false;
        }

        TimeSpan delay = settings.Delay ?? location.GetDelay(Player);
        if (delay < TimeSpan.Zero)
            delay = default;

        settings.Delay = delay;

        if (delay == TimeSpan.Zero)
        {
            CurrentDeployment = location;
            InstantDeploy(location, settings, deployFrom);
        }
        else
        {
            _chatService.Send(Player, _translations.DeployStandby, location, delay.Seconds);
            _deploymentCoroutine = StartCoroutine(DeployCoroutine(location, settings, deployFrom));
        }

        return true;
    }

    private void InstantDeploy(IDeployable deployable, DeploySettings settings, IDeployable? deployFrom)
    {
        Player.UnturnedPlayer.teleportToLocationUnsafe(deployable.SpawnPosition, deployable.Yaw);
        if (deployFrom is { IsSafeZone: true })
        {
            _lastDeployedFromSafeZone = DateTime.UtcNow;
        }

        deployFrom?.OnDeployFrom(Player, in settings);
        deployable.OnDeployTo(Player, in settings);
        ActionLog.Add(ActionLogType.DeployToLocation, deployable.ToString(), Player);

        PlayerDeployed args = new PlayerDeployed
        {
            Player = Player,
            Deployable = deployable
        };
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);

        if (!settings.DisableInitialChatUpdates)
            _chatService.Send(Player, _translations.DeploySuccess, deployable);
        if (!settings.DisableStartingCooldown && _cooldownManager != null)
        {
            _cooldownManager.StartCooldown(Player, settings.CooldownType ?? KnownCooldowns.Deploy, 30f /* todo RapidDeployment.GetDeployTime(Player) */);
        }

        CurrentDeployment = null;
    }

    private IEnumerator DeployCoroutine(IDeployable deployable, DeploySettings settings, IDeployable? deployFrom)
    {
        CurrentDeployment = deployable;

                             // set by calling function
        float delay = (float)settings.Delay.GetValueOrDefault().TotalSeconds;

        DeploymentPlayerState startState = default;
        startState.Position = Player.Position;
        startState.Health = Player.UnturnedPlayer.life.health;
        startState.DeployFrom = deployFrom;

        do
        {
            float waitTime = Math.Min(delay, TickSpeedSeconds);
            delay -= waitTime;
            yield return new WaitForSecondsRealtime(waitTime);

            if (!VerifyDeploymentTick(deployable, in settings, ref startState))
            {
                CancelDeployment(false);
                yield break;
            }
        }
        while (delay > 0);

        InstantDeploy(deployable, settings, deployFrom);
        _deploymentCoroutine = null;
    }

    private struct DeploymentPlayerState
    {
        public Vector3 Position;
        public byte Health;
        public IDeployable? DeployFrom;
    }

    private bool VerifyDeploymentTick(IDeployable deployable, in DeploySettings settings, ref DeploymentPlayerState startState)
    {
        byte currentHealth = Player.UnturnedPlayer.life.health;
        if (!settings.AllowDamage && startState.Health > currentHealth)
        {
            if (!settings.DisableTickingChatUpdates)
                _chatService.Send(Player, _translations.DeployDamaged);

            return false;
        }

        // reset this so damage after heals can be detected
        startState.Health = currentHealth;

        if (!settings.AllowMovement && !startState.Position.IsNearlyEqual(Player.Position, 1f))
        {
            if (!settings.DisableTickingChatUpdates)
                _chatService.Send(Player, _translations.DeployMoved);

            return false;
        }

        if (!settings.AllowInjured && Player.IsInjured())
        {
            if (!settings.DisableTickingChatUpdates)
                _chatService.Send(Player, _translations.DeployInjured);

            return false;
        }

        if (!settings.DisableCheckingForCooldown && _cooldownManager != null && _cooldownManager.HasCooldown(Player, settings.CooldownType ?? KnownCooldowns.Deploy, out Cooldown cooldown))
        {
            if (!settings.DisableTickingChatUpdates)
                _chatService.Send(Player, _translations.DeployCooldown, cooldown);

            return false;
        }

        if (!settings.AllowCombat && _cooldownManager != null && _cooldownManager.HasCooldown(Player, KnownCooldowns.Combat, out cooldown))
        {
            if (!settings.DisableTickingChatUpdates)
                _chatService.Send(Player, _translations.DeployInCombat, cooldown);

            return false;
        }

        if (!settings.AllowNearbyEnemies && AreEnemiesNearby(settings.NearbyEnemyRange ?? DeploymentService.DefaultNearbyEnemyRange))
        {
            if (!settings.DisableTickingChatUpdates)
                _chatService.Send(Player, _translations.DeployEnemiesNearbyTick, deployable);

            return false;
        }

        if (startState.DeployFrom != null && !startState.DeployFrom.CheckDeployableFromTick(Player, _translations, in settings, deployable))
        {
            return false;
        }

        if (!deployable.CheckDeployableToTick(Player, _chatService, _translations, in settings))
        {
            return false;
        }

        return true;
    }

    private bool AreEnemiesNearby(float range)
    {
        if (range <= 0)
            return false;

        float sqrRange = range * range;
        Vector3 pos = Player.Position;
        Team team = Player.Team;

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            if (!player.Equals(Player) && (player.Position - pos).sqrMagnitude < sqrRange && team.IsOpponent(player.Team))
            {
                return true;
            }
        }

        return false;
    }
}