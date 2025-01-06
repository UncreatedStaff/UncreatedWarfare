using Microsoft.Extensions.DependencyInjection;
using StackCleaner;
using System;
using System.Globalization;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Injures;

/// <summary>
/// Replacement for ReviveManager. Injures no longer have a centralized 'manager', only a player component.
/// </summary>
[PlayerComponent]
public class PlayerInjureComponent : MonoBehaviour,
    IPlayerComponent,
    IDisposable,
    IEventListener<PlayerDied>,
    IEventListener<DamagePlayerRequested>,
    IEventListener<AidPlayerRequested>,
    IEventListener<PlayerAided>,
    IEventListener<EnterVehicleRequested>
{

    /// <summary>
    /// Number of seconds after a player is injured before they can take damage.
    /// </summary>
    private const float InjureCooldownBeforeDamageAllowed = 0.4f;

    /// <summary>
    /// If players can revive enemies.
    /// </summary>
    private const bool CanReviveEnemies = true;

    /// <summary>
    /// Number of seconds between each marker update.
    /// </summary>
    private const float MarkerUpdateFrequency = 0.31f;

    /// <summary>
    /// Maximum distance from which a medic or injured marker will render.
    /// </summary>
    private const float MarkerRenderDistance = 150;

    /// <summary>
    /// The amount of time it takes to bleed out with no intervention.
    /// </summary>
    private const float InjureStandardBleedoutDuration = 30;

    /// <summary>
    /// A damage multiplier of incoming damage while injured.
    /// </summary>
    private const float InjuredDamageMultiplier = 0.25f;

    /// <summary>
    /// Defines which players can revive other players.
    /// </summary>
    private static bool CanRevive(WarfarePlayer medic)
    {
        return !medic.IsInjured()
               && medic.Component<KitPlayerComponent>().ActiveClass == Class.Medic;
    }

    /// <summary>
    /// Defines the conditions under which a player will be injured.
    /// </summary>
    private static bool ShouldDamageInjurePlayer(in DamagePlayerParameters parameters)
    {
        int unclampedDamage = Mathf.FloorToInt(parameters.damage * parameters.times);
        int actualDamage = Math.Min(byte.MaxValue, unclampedDamage);

               // player online
        return parameters.player != null &&
               // player not in vehicle
               parameters.player.movement.getVehicle() == null &&
               // player not hit by vehicle
               parameters.cause is not EDeathCause.VEHICLE and not EDeathCause.KILL &&
               // player alive
               !parameters.player.life.isDead &&
               // player will die from hit
               actualDamage >= parameters.player.life.health &&
               // not killed by landmine
               parameters.cause != EDeathCause.LANDMINE &&
               // not dying from main-camping reverse damage (makes death messages significantly harder)
               parameters.cause < DeathTracker.MainCampDeathCauseOffset &&
               // not more than total 300 damage (unclamped)
               unclampedDamage < 300;
    }

    private static readonly short GiveUpUiKey = UnturnedUIKeyPool.Claim();

    private Coroutine? _markerCoroutine;
    private bool _isInjured;
    private bool _isReviving;
    private WarfarePlayer? _reviver;

    private DamagePlayerParameters _injureParameters;
    private float _injureStart;

#nullable disable
    private DeathTracker _deathTracker;
    private ChatService _chatService;
    private IPlayerService _playerService;
    private EventDispatcher _eventDispatcher;
    private AssetConfiguration _assetConfiguration;
    private PointsTranslations _xpTranslations;
    private CooldownManager _cooldownManager;
    private PlayersTranslations _playerTranslations;
    public WarfarePlayer Player { get; private set; }
#nullable restore

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    /// <summary>
    /// The player's state (injured/not injured).
    /// </summary>
    public PlayerHealthState State
    {
        get
        {
            if (!Player.IsOnline)
                return PlayerHealthState.Offline;

            if (Player.UnturnedPlayer.life.isDead)
                return PlayerHealthState.Dead;

            return _isInjured ? PlayerHealthState.Injured : PlayerHealthState.Healthy;
        }
    }

    /// <summary>
    /// The death info for if the player bleeds out.
    /// </summary>
    public PlayerDied? PendingDeathInfo { get; internal set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _deathTracker = serviceProvider.GetRequiredService<DeathTracker>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _eventDispatcher = serviceProvider.GetRequiredService<EventDispatcher>();
        _cooldownManager = serviceProvider.GetRequiredService<CooldownManager>();
        _xpTranslations = serviceProvider.GetRequiredService<TranslationInjection<PointsTranslations>>().Value;
        _playerTranslations = serviceProvider.GetRequiredService<TranslationInjection<PlayersTranslations>>().Value;

        if (!isOnJoin)
            return;


        PlayerKeys.PressedPluginKey3 += OnPressedGiveUp;
        PlayerKeys.PressedPluginKey2 += OnPressedReviveSelf;

        _isInjured = false;
        _injureStart = 0f;

        PendingDeathInfo = null;

        Player.UnturnedPlayer.stance.onStanceUpdated += OnStanceUpdated;
        Player.UnturnedPlayer.equipment.onEquipRequested += OnEquipRequested;

        if (_markerCoroutine != null)
            StopCoroutine(_markerCoroutine);
    }

    void IDisposable.Dispose()
    {
        PendingDeathInfo = null;

        Player.UnturnedPlayer.stance.onStanceUpdated -= OnStanceUpdated;
        Player.UnturnedPlayer.equipment.onEquipRequested -= OnEquipRequested;

        PlayerKeys.PressedPluginKey3 -= OnPressedGiveUp;
        PlayerKeys.PressedPluginKey2 -= OnPressedReviveSelf;

        if (!_isInjured)
            return;
        
        // make sure player will be dead if they rejoin if the left while injured
        Player.Save.ShouldRespawnOnJoin = true;
        Player.Save.Save();

        ref DamagePlayerParameters p = ref _injureParameters;
        Player.UnturnedPlayer.life.askDamage(byte.MaxValue, Vector3.up, p.cause, p.limb, p.killer, out _, p.trackKill, p.ragdollEffect, false, true);
        _isInjured = false;
        _injureStart = 0f;
    }

    private void OnPressedGiveUp(WarfarePlayer player, ref bool handled)
    {
        if (!player.Equals(Player) || !_isInjured)
            return;

        GiveUp();
        handled = true;
    }

    private void OnPressedReviveSelf(WarfarePlayer player, ref bool handled)
    {
#if false // todo
        if (!player.Equals(Player) || !_isInjured) 
            return;

        if (!TraitManager.Loaded || !SelfRevive.HasSelfRevive(Player, out SelfRevive selfRevive))
        {
            handled = false;
            return;
        }

        float timeSinceInjure = Time.realtimeSinceStartup - _injureStart;
        if (timeSinceInjure >= selfRevive.Cooldown)
        {
            selfRevive.Consume();
            Revive();
        }
        else
        {
            _chatService.Send(Player, T.TraitSelfReviveCooldown, selfRevive.Data, TimeSpan.FromSeconds(timeSinceInjure));
        }

        handled = true;
#endif
    }

    /// <summary>
    /// Injure the current player if they're not injured or dead.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public void Injure(ref DamagePlayerParameters parameters)
    {
        GameThread.AssertCurrent();
        WarfarePlayer player = _playerService.GetOnlinePlayer(parameters.player);
        CSteamID killerId = parameters.killer;

        WarfarePlayer? killer = _playerService.GetOnlinePlayerOrNull(killerId);

        if (State is PlayerHealthState.Injured or PlayerHealthState.Dead)
            return;

        AddInjureModifiers(ref parameters);
        _injureParameters = parameters;

        ActionLog.Add(ActionLogType.Injured, "by " + (killer == null ? "self" : killer.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture)), parameters.player.channel.owner.playerID.steamID.m_SteamID);

        _deathTracker.OnInjured(in parameters);

        if (killerId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            PlayerDeathTrackingComponent comp = PlayerDeathTrackingComponent.GetOrAdd(parameters.player);
            comp.TryUpdateLastAttacker(killerId);
        }

        if (killer != null && !killer.Equals(parameters.player) /* suicide */)
        {
            // send Injured toast to killer
            Team killerTeam = killer.Team;
            killer.SendToast(new ToastMessage(ToastMessageStyle.Mini, (killerTeam != player.Team
                ? _xpTranslations.XPToastEnemyInjured
                : _xpTranslations.XPToastFriendlyInjured)
                .Translate(killer)));
        }

        PlayerInjured args = new PlayerInjured(in parameters)
        {
            Player = Player,
            Instigator = killer
        };

        _ = _eventDispatcher.DispatchEventAsync(args, CancellationToken.None);
    }

    /// <summary>
    /// Revives the current player if they're injured. Dead players will not be respawned.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public void Revive(WarfarePlayer? medic = null)
    {
        GameThread.AssertCurrent();

        if (State != PlayerHealthState.Injured)
            return;

        RemoveInjureModifiers();
        PendingDeathInfo = null;


        PlayerRevived args = new PlayerRevived
        {
            Player = Player,
            Medic = medic
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }

    /// <summary>
    /// Skips the player's injure timer and kills them.
    /// </summary>
    public void GiveUp()
    {
        ref DamagePlayerParameters p = ref _injureParameters;
        Player.UnturnedPlayer.life.askDamage(byte.MaxValue, Vector3.down, p.cause, p.limb, p.killer, out _, p.trackKill, p.ragdollEffect, false, true);
    }

    private void AddInjureModifiers(ref DamagePlayerParameters parameters)
    {
        Player player = Player.UnturnedPlayer;
        player.equipment.dequip();

        // times per second simulate() is ran times bleed damage ticks = how many seconds it will take to lose 1 hp
        float bleedsPerSecond = Time.timeScale / PlayerInput.RATE / Provider.modeConfigData.Players.Bleed_Damage_Ticks;

        float startingHp = InjureStandardBleedoutDuration * bleedsPerSecond - player.life.health;

        parameters.respectArmor = false;
        parameters.applyGlobalArmorMultiplier = false;
        if (startingHp > 0.0)
        {
            parameters.damage = 0;
            parameters.times = 1;
            player.life.askHeal(MathfEx.RoundAndClampToByte(startingHp), false, false);
        }
        else
        {
            parameters.damage = MathfEx.RoundAndClampToByte(-startingHp);
            parameters.times = 1;
        }
        
        player.life.serverSetBleeding(true);
        player.movement.sendPluginSpeedMultiplier(0.35f);
        player.movement.sendPluginJumpMultiplier(0);

        _chatService.Send(Player, _playerTranslations.InjuredUIGiveUpChat);

        SendGiveUpUI();

        Player.TrySetStance(EPlayerStance.PRONE);

        _isInjured = true;
        _injureStart = Time.realtimeSinceStartup;
        if (_markerCoroutine != null)
        {
            StopCoroutine(_markerCoroutine);
        }

        _markerCoroutine = StartCoroutine(SpawnMedicMarkersCoroutine());
    }

    private void RemoveInjureModifiers()
    {
        _isInjured = false;
        _injureStart = 0f;
        if (_markerCoroutine != null)
        {
            StopCoroutine(_markerCoroutine);
            _markerCoroutine = null;
        }

        if (Player.UnturnedPlayer.stance.stance == EPlayerStance.PRONE)
            Player.TrySetStance(EPlayerStance.CROUCH);

        Player.UnturnedPlayer.movement.sendPluginSpeedMultiplier(1.0f);
        Player.UnturnedPlayer.movement.sendPluginJumpMultiplier(1.0f);
        Player.UnturnedPlayer.life.serverSetBleeding(false);
        ClearMedicIcons();
        ClearGiveUpUI();
    }

    private void SendGiveUpUI()
    {
        if (!_assetConfiguration.GetAssetLink<EffectAsset>("UI:GiveUp").TryGetId(out ushort uiInjuredId))
            return;

        // GiveUpText has to be resent because initial values don't get their hotkeys filled but resent values do
        EffectManager.sendUIEffect(uiInjuredId, GiveUpUiKey, Player.Connection, true, _playerTranslations.InjuredUIHeader.Translate(Player), string.Empty);
        EffectManager.sendUIEffectText(GiveUpUiKey, Player.Connection, true, "Canvas/GameObject/GiveUpText", _playerTranslations.InjuredUIGiveUp.Translate(Player));
    }

    private void ClearGiveUpUI()
    {
        if (_assetConfiguration.GetAssetLink<EffectAsset>("UI:GiveUp").TryGetGuid(out Guid uiInjuredId))
            EffectManager.ClearEffectByGuid(uiInjuredId, Player.Connection);
    }

    private void ClearMedicIcons()
    {
        if (_assetConfiguration.GetAssetLink<EffectAsset>("Effect:Medic").TryGetGuid(out Guid medicUi))
            EffectManager.ClearEffectByGuid(medicUi, Player.Connection);
    }

    private IEnumerator SpawnMedicMarkersCoroutine()
    {
        _assetConfiguration.GetAssetLink<EffectAsset>("Effect:Medic").TryGetAsset(out EffectAsset? medicAsset);
        _assetConfiguration.GetAssetLink<EffectAsset>("Effect:Injured").TryGetAsset(out EffectAsset? injuredAsset);

        while (_isInjured)
        {
            Vector3 position = Player.Position;

            PooledTransportConnectionList medicList = TransportConnectionPoolHelper.Claim();
            foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                if (player.IsInjured()
                    || (player.Position - position).sqrMagnitude > MarkerRenderDistance * MarkerRenderDistance
                    || player.Component<KitPlayerComponent>().ActiveClass != Class.Medic
                    || player.UnturnedPlayer.life.isDead
                    || player.Team != Player.Team)
                {
                    continue;
                }

                if (medicAsset != null)
                    EffectUtility.TriggerEffect(medicAsset, Player.Connection, player.Position, true);

                medicList.Add(player.Connection);
            }

            if (injuredAsset != null)
                EffectUtility.TriggerEffect(injuredAsset, medicList, position, true);

            yield return new WaitForSeconds(MarkerUpdateFrequency);
        }
    }

    // specially invoked after the AidPlayerRequested event to setup
    // for the PlayerAided event which will run instantly after this
    internal void PrepAidRevive(AidPlayerRequested args)
    {
        _isReviving = true;
        _reviver = args.Medic;
    }

    private void OnStanceUpdated()
    {
        if (!_isInjured)
            return;

        Player.TrySetStance(EPlayerStance.PRONE);
    }

    private void OnEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldallow)
    {
        if (!_isInjured)
            return;

        shouldallow = false;
    }

    [EventListener(Priority = 100, MustRunInstantly = true)]
    void IEventListener<AidPlayerRequested>.HandleEvent(AidPlayerRequested e, IServiceProvider serviceProvider)
    {
        if (!_isInjured)
            return;

#pragma warning disable CS0162

        if (!CanReviveEnemies && e.Medic.Team == e.Player.Team)
        {
            _chatService.Send(e.Medic, _playerTranslations.ReviveHealEnemies);
            e.Cancel();
            return;
        }

#pragma warning restore CS0162

        if (!CanRevive(e.Medic))
        {
            _chatService.Send(e.Medic, _playerTranslations.ReviveNotMedic);
            e.Cancel();
            return;
        }

        e.IsRevive = true;
    }

    [EventListener(Priority = 100, MustRunInstantly = true)]
    void IEventListener<PlayerAided>.HandleEvent(PlayerAided e, IServiceProvider serviceProvider)
    {
        bool canRevive = _isReviving && _isInjured && _reviver != null;

        _isReviving = false;
        _reviver = null;

        if (!canRevive)
            return;

        ActionLog.Add(
            ActionLogType.RevivedPlayer,
            e.Player.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture),
            e.Medic.Steam64
        );

        Revive(e.Medic);

        if (e.Player.Team != e.Medic.Team)
            return;

        // prevent injure/revive spamming to farm XP
        if (_cooldownManager.Config.ReviveXPCooldown <= 0
            || !_cooldownManager.HasCooldown(e.Medic, CooldownType.Revive, out _, e.Steam64.m_SteamID))
        {
            // todo Points.AwardXP(e.Medic, XPReward.Revive);
            // QuestManager.OnRevive(e.Medic, e.Player);
            _cooldownManager.StartCooldown(e.Medic, CooldownType.Revive, _cooldownManager.Config.ReviveXPCooldown, e.Steam64.m_SteamID);
        }
        else
        {
            e.Medic.SendToast(new ToastMessage(ToastMessageStyle.Mini,
                _xpTranslations.XPToastGainXP.Translate(0, e.Medic)
                +"\n"
                + TranslationFormattingUtility.Colorize(_xpTranslations.XPToastHealedTeammate.Translate(e.Medic), new Color32(173, 173, 173, 255), TranslationOptions.TMProUI, StackColorFormatType.None) ));
        }
    }

    [EventListener(Priority = -100)]
    void IEventListener<DamagePlayerRequested>.HandleEvent(DamagePlayerRequested e, IServiceProvider serviceProvider)
    {
        ref DamagePlayerParameters parameters = ref e.Parameters;
        if (_isInjured)
        {
            if (Time.realtimeSinceStartup - _injureStart < InjureCooldownBeforeDamageAllowed)
            {
                e.Cancel();
                return;
            }

            if (parameters.cause == EDeathCause.KILL)
            {
                return;
            }

            parameters.limb = _injureParameters.limb;
            parameters.killer = _injureParameters.killer;
            parameters.bleedingModifier = DamagePlayerParameters.Bleeding.Never;
            parameters.bonesModifier = DamagePlayerParameters.Bones.None;
            parameters.times = 1f;
            parameters.applyGlobalArmorMultiplier = false;
            parameters.respectArmor = false;
            parameters.direction = e.Instigator != null ? (Player.Position - e.Instigator.Position).normalized : _injureParameters.direction;
            parameters.waterModifier = 0;
            parameters.foodModifier = 0;
            parameters.hallucinationModifier = 0;
            parameters.trackKill = _injureParameters.trackKill;

            if (parameters.cause != EDeathCause.BLEEDING)
            {
                parameters.cause = _injureParameters.cause;
                // times per second simulate() is ran times bleed damage ticks = how many seconds it will take to lose 1 hp
                float bleedsPerSecond = Time.timeScale / PlayerInput.RATE / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
                parameters.damage *= /* todo UCWarfare.Config.InjuredDamageMultiplier */ 0.4f / 10 * bleedsPerSecond * /* todo UCWarfare.Config.InjuredLifeTimeSeconds */ 30;
            }
            else
            {
                parameters.damage = 1f;
                if (Player.UnturnedPlayer.life.health <= 1)
                    parameters.cause = _injureParameters.cause;
            }
            return;
        }

        WarfarePlayer? killer = e.Instigator;
        if (killer != null && killer.IsInjured())
        {
            e.Cancel();
            return;
        }

        if (!ShouldDamageInjurePlayer(in parameters))
        {
            return;
        }

        InjurePlayerRequested args = new InjurePlayerRequested(in parameters, _playerService)
        {
            Player = e.Player
        };

        bool shouldContinue = _eventDispatcher.DispatchEventAsync(args, allowAsync: false).GetAwaiter().GetResult();
        if (!shouldContinue)
        {
            e.Parameters = args.Parameters;
            return;
        }

        e.IsInjure = true;
        Injure(ref parameters);
    }

    void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        if (!_isInjured)
            return;

        Player.TrySetStance(EPlayerStance.STAND);
        RemoveInjureModifiers();
    }

    [EventListener(Priority = 100)]
    void IEventListener<EnterVehicleRequested>.HandleEvent(EnterVehicleRequested e, IServiceProvider serviceProvider)
    {
        if (!_isInjured)
            return;

        e.Cancel();
        _chatService.Send(Player, _playerTranslations.InjuredUIGiveUpChat);
    }
}