using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Traits.Buffs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.Deployment;

internal class DeploymentComponent : MonoBehaviour, IPlayerComponent
{
    private const float TickSpeedSeconds = 0.25f;

    private DeploymentTranslations _translations = null!;
    
    private float _deploymentTimeStarted;
    private Coroutine? _deploymentCoroutine;
    private CooldownManager? _cooldownManager;
    private FOBManager? _fobManager;
    private ZoneStore _zoneStore;
    private IPlayerService _playerService;
    public WarfarePlayer Player { get; private set; }
    public IDeployable? CurrentDeployment { get; private set; }
    public TimeSpan DeploymentTimeLeft => _deploymentTimeStarted == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(Time.realtimeSinceStartup - _deploymentTimeStarted);
    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    public void Init(IServiceProvider serviceProvider)
    {
        _translations = serviceProvider.GetRequiredService<TranslationInjection<DeploymentTranslations>>().Value;
        _zoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _cooldownManager = serviceProvider.GetService<CooldownManager>();
        _fobManager = serviceProvider.GetService<FOBManager>();
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
            Player.SendChat(_translations.DeployCancelled);
        }
    }

    public bool TryStartDeployment(IDeployable location, DeploySettings settings = default)
    {
        GameThread.AssertCurrent();
        if (CurrentDeployment != null)
        {
            CancelDeployment(true);
        }

        if (!settings.AllowInjured && _reviveManager != null && _reviveManager.IsInjured(Player))
        {
            if (!settings.DisableInitialChatUpdates)
                Player.SendChat(_translations.DeployInjured);

            return false;
        }

        if (!settings.DisableCheckingForCooldown && _cooldownManager != null && _cooldownManager.HasCooldown(Player, settings.CooldownType ?? CooldownType.Deploy, out Cooldown cooldown))
        {
            if (!settings.DisableInitialChatUpdates)
                Player.SendChat(_translations.DeployCooldown, cooldown);

            return false;
        }

        if (!settings.AllowCombat && _cooldownManager != null && _cooldownManager.HasCooldown(Player, CooldownType.Combat, out cooldown))
        {
            if (!settings.DisableInitialChatUpdates)
                Player.SendChat(_translations.DeployInCombat, cooldown);

            return false;
        }

        if (!settings.AllowNearbyEnemies && AreEnemiesNearby(settings.NearbyEnemyRange ?? DeploymentService.DefaultNearbyEnemyRange))
        {
            if (!settings.DisableInitialChatUpdates)
                Player.SendChat(_translations.DeployEnemiesNearby, location);

            return false;
        }

        if (!location.CheckDeployableTo(Player, _translations, in settings))
        {
            return false;
        }

        bool isInMain = _zoneStore.IsInMainBase(Player);

        IFOB? deployFrom = null;

        if (!isInMain)
        {
            if (_fobManager == null || !_fobManager.IsOnFOB(Player, out deployFrom))
            {
                if (!settings.DisableInitialChatUpdates)
                    Player.SendChat(_translations.DeployNotNearFOB); // todo add Insurgency special message

                return false;
            }
        }

        if (deployFrom != null && !deployFrom.CheckDeployableFrom(Player, _translations, in settings, location))
        {
            return false;
        }

        if (location.Equals(deployFrom))
        {
            if (!settings.DisableInitialChatUpdates)
                Player.SendChat(_translations.DeployableAlreadyOnFOB);

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
            _deploymentCoroutine = StartCoroutine(DeployCoroutine(location, settings, deployFrom));
        }

        return true;
    }

    private void InstantDeploy(IDeployable deployable, DeploySettings settings, IDeployable? deployFrom)
    {
        Player.UnturnedPlayer.teleportToLocationUnsafe(deployable.SpawnPosition, deployable.Yaw);
        deployFrom?.OnDeployFrom(Player, in settings);
        deployable.OnDeployTo(Player, in settings);
        if (!settings.DisableStartingCooldown && _cooldownManager != null)
        {
            _cooldownManager.StartCooldown(Player, settings.CooldownType ?? CooldownType.Deploy, RapidDeployment.GetDeployTime(Player));
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
                Player.SendChat(_translations.DeployDamaged);

            return false;
        }

        // reset this so damage after heals can be detected
        startState.Health = currentHealth;

        if (!settings.AllowMovement && !startState.Position.AlmostEquals(Player.Position, 1f))
        {
            if (!settings.DisableTickingChatUpdates)
                Player.SendChat(_translations.DeployMoved);

            return false;
        }

        if (!settings.AllowInjured && _reviveManager != null && _reviveManager.IsInjured(Player))
        {
            if (!settings.DisableTickingChatUpdates)
                Player.SendChat(_translations.DeployInjured);

            return false;
        }

        if (!settings.DisableCheckingForCooldown && _cooldownManager != null && _cooldownManager.HasCooldown(Player, settings.CooldownType ?? CooldownType.Deploy, out Cooldown cooldown))
        {
            if (!settings.DisableTickingChatUpdates)
                Player.SendChat(_translations.DeployCooldown, cooldown);

            return false;
        }

        if (!settings.AllowCombat && _cooldownManager != null && _cooldownManager.HasCooldown(Player, CooldownType.Combat, out cooldown))
        {
            if (!settings.DisableTickingChatUpdates)
                Player.SendChat(_translations.DeployInCombat, cooldown);

            return false;
        }

        if (!settings.AllowNearbyEnemies && AreEnemiesNearby(settings.NearbyEnemyRange ?? DeploymentService.DefaultNearbyEnemyRange))
        {
            if (!settings.DisableTickingChatUpdates)
                Player.SendChat(_translations.DeployEnemiesNearbyTick, deployable);

            return false;
        }

        if (startState.DeployFrom != null && !startState.DeployFrom.CheckDeployableFromTick(Player, _translations, in settings, deployable))
        {
            return false;
        }

        if (!deployable.CheckDeployableToTick(Player, _translations, in settings))
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