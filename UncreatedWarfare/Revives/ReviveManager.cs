using JetBrains.Annotations;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Traits.Buffs;
using UnityEngine;
using static SDG.Unturned.WeatherAsset;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.Revives;

[SingletonDependency(typeof(KitManager))]
public class ReviveManager : BaseSingleton, IPlayerConnectListener, IDeclareWinListener, IPlayerDisconnectListener, IGameTickListener
{
    private readonly Dictionary<ulong, DownedPlayerData> _injuredPlayers;
    private static ReviveManager _singleton;
    private Coroutine? _updater;
    // the estimated amount of time between simulate() calls.
    const float EstSimulateTimeSec = 0.08f;
    const float MarkerDistance = 150;
    const float MarkerUpdateFrequency = 0.33f;
    const bool CanHealEnemies = true;
    public static bool Loaded => _singleton.IsLoaded();
    private IEnumerable<UCPlayer> GetAvailableMedics(UCPlayer injured) => PlayerManager.OnlinePlayers.Where(x => 
    x != injured &&
    x.GetTeam() == injured.GetTeam() && 
    x.KitClass == Class.Medic &&
    !x.Player.life.isDead &&
    !IsInjured(x) &&
    (x.Position - injured.Position).sqrMagnitude < Math.Pow(MarkerDistance, 2));
    public ReviveManager()
    {
        _injuredPlayers = new Dictionary<ulong, DownedPlayerData>(Provider.maxPlayers);
    }
    public bool IsInjured(ulong player) => _injuredPlayers.ContainsKey(player);
    public bool IsInjured(IPlayer player) => _injuredPlayers.ContainsKey(player.Steam64);
    public DamagePlayerParameters GetParameters(ulong player) => _injuredPlayers.TryGetValue(player, out DownedPlayerData data) ? data.Parameters : default; 
    public bool CanPlayerInjure(in DamagePlayerParameters parameters)
    {
        return parameters.player != null &&
               parameters.player.movement.getVehicle() == null && parameters.cause != EDeathCause.VEHICLE &&
               SafezoneManager.checkPointValid(parameters.player.transform.position) && 
              !parameters.player.life.isDead &&
               parameters.damage > parameters.player.life.health &&
               parameters.cause != EDeathCause.LANDMINE &&
               parameters.cause < DeathTracker.MainCampDeathCauseOffset && // main campers can't get downed, makes death messages easier
               parameters.damage < 300;
    }
    public override void Load()
    {
        EventDispatcher.PlayerDied += OnPlayerDeath;
        PlayerLife.OnRevived_Global += OnPlayerRespawned;
        UseableConsumeable.onPerformingAid += OnHealPlayer;
        _singleton = this;
        UCPlayerKeys.SubscribeKeyUp(GiveUpPressed, Data.Keys.GiveUp);
        UCPlayerKeys.SubscribeKeyUp(SelfRevivePressed, Data.Keys.SelfRevive);
        KitManager.OnKitChanged += OnKitChanged;
    }
    public override void Unload()
    {
        KitManager.OnKitChanged -= OnKitChanged;
        UCPlayerKeys.UnsubscribeKeyUp(SelfRevivePressed, Data.Keys.SelfRevive);
        UCPlayerKeys.UnsubscribeKeyUp(GiveUpPressed, Data.Keys.GiveUp);
        _singleton = null!;
        UseableConsumeable.onPerformingAid -= OnHealPlayer;
        PlayerLife.OnRevived_Global -= OnPlayerRespawned;
        EventDispatcher.PlayerDied -= OnPlayerDeath;
        if (_updater is not null)
        {
            UCWarfare.I.StopCoroutine(_updater);
            _updater = null;
        }
        foreach (DownedPlayerData downedPlayer in _injuredPlayers.Values.ToList())
        {
            if (downedPlayer.Parameters.player != null && downedPlayer.Parameters.player.TryGetComponent(out Reviver reviver))
                reviver.FinishKillingPlayer();
        }
        _injuredPlayers.Clear();
        DeathTracker.ReviveManagerUnloading();
    }
    private void GiveUpPressed(UCPlayer player, float timeDown, ref bool handled)
    {
        if (_injuredPlayers.TryGetValue(player.Steam64, out DownedPlayerData p))
        {
            player.Player.life.askDamage(byte.MaxValue, Vector3.down, p.Parameters.cause, p.Parameters.limb, p.Parameters.killer, out _,
                p.Parameters.trackKill, p.Parameters.ragdollEffect, false, true);
            handled = true;
        }
    }
    private void SelfRevivePressed(UCPlayer player, float timeDown, ref bool handled)
    {
        if (_injuredPlayers.TryGetValue(player.Steam64, out DownedPlayerData data) &&
            TraitManager.Loaded &&
            SelfRevive.HasSelfRevive(player, out SelfRevive sr))
        {
            float tl = Time.realtimeSinceStartup - data.Start;
            if (tl >= sr.Cooldown)
            {
                if (player.Player.TryGetComponent(out Reviver r))
                {
                    sr.Consume();
                    r.SelfRevive();
                }
                else player.SendChat(T.UnknownError);
            }
            else
            {
                player.SendChat(T.TraitSelfReviveCooldown, sr.Data, Localization.GetTimeFromSeconds(Mathf.CeilToInt(tl), player));
            }
        }
    }
    void IPlayerConnectListener.OnPlayerConnecting(UCPlayer player)
    {
        if (player?.Player == null) return;
        if (!player.Player.transform.gameObject.TryGetComponent<Reviver>(out _))
            player.Player.transform.gameObject.AddComponent<Reviver>();
        _injuredPlayers.Remove(player.Steam64);
        DeathTracker.RemovePlayerInfo(player.Steam64);
    }
    void IPlayerDisconnectListener.OnPlayerDisconnecting(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        DeathTracker.RemovePlayerInfo(player.Steam64);
        if (_injuredPlayers.TryGetValue(player.Steam64, out DownedPlayerData p))
        {
            player.Save.ShouldRespawnOnJoin = true;
            PlayerSave.WriteToSaveFile(player.Save);
            player.Player.life.askDamage(byte.MaxValue, Vector3.up, p.Parameters.cause, p.Parameters.limb, p.Parameters.killer, out _, p.Parameters.trackKill, p.Parameters.ragdollEffect, false, true);
            // player will be removed from list in OnDeath
        }
    }
    private void OnHealPlayer(Player healer, Player downed, ItemConsumeableAsset asset, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? medic = UCPlayer.FromPlayer(healer);
        if (medic == null)
        {
            shouldAllow = false;
            return;
        }
        if (!CanHealEnemies && medic.GetTeam() != downed.GetTeam())
        {
            medic.SendChat(T.ReviveHealEnemies);
            shouldAllow = false;
            return;
        }
        if (!_injuredPlayers.ContainsKey(downed.channel.owner.playerID.steamID.m_SteamID)) // if not injured
            return;
        if (medic.KitClass != Class.Medic)
        {
            medic.SendChat(T.ReviveNotMedic);
            shouldAllow = false;
        }
    }
    private void OnPlayerRespawned(PlayerLife obj)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (obj.player.TryGetComponent(out Reviver r))
            r.TellStandDelayed(1.5f);
        obj.player.movement.sendPluginSpeedMultiplier(1.0f);
        obj.player.movement.sendPluginJumpMultiplier(1.0f);
    }
    internal void SetStanceBetter(Player player, EPlayerStance stance)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.SetPrivateStance == null || Data.ReplicateStance == null)
        {
            player.stance.checkStance(stance);
            L.LogWarning("Unable to set stance properly, fell back to checkStance.");
            return;
        }
        Data.SetPrivateStance(player.stance, stance);
        Data.ReplicateStance.Invoke(player.stance, new object[] { false });
    }
    internal void OnPlayerHealed(UCPlayer medic, UCPlayer target)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (target.Player.TryGetComponent(out Reviver r) && _injuredPlayers.ContainsKey(target.Steam64))
        {
            ActionLog.Add(ActionLogType.RevivedPlayer, target.Steam64.ToString(Data.AdminLocale), medic.Steam64);
            r.RevivePlayer(null);
            byte team = medic.Player.GetTeamByte();
            byte tteam = target.Player.GetTeamByte();
            if (team == tteam)
            {
                UCPlayer? ucmedic = UCPlayer.FromPlayer(medic);
                UCPlayer? uctarget = UCPlayer.FromPlayer(target);
                if (ucmedic != null)
                {
                    if (CooldownManager.Config.ReviveXPCooldown <= 0 || (uctarget != null &&
                      !(CooldownManager.HasCooldown(ucmedic, CooldownType.Revive, out Cooldown cooldown) &&
                        cooldown.data.Length > 0 &&
                        cooldown.data[0] is ulong id &&
                        id == uctarget.Steam64)))
                    {
                        Points.AwardXP(ucmedic, XPReward.Revive);
                        if (uctarget != null)
                        {
                            QuestManager.OnRevive(ucmedic, uctarget);
                            CooldownManager.StartCooldown(ucmedic, CooldownType.Revive, CooldownManager.Config.ReviveXPCooldown, uctarget.Steam64);
                        }
                    }
                    else
                    {
                        ToastMessage.QueueMessage(ucmedic, new ToastMessage(ToastMessageStyle.Mini,
                            T.XPToastGainXP.Translate(ucmedic, false, 0) + "\n" +
                            T.XPToastHealedTeammate.Translate(ucmedic).Colorize("adadad")));
                    }
                }


                if (medic.Player.TryGetPlayerData(out UCPlayerData c) && c.Stats is IRevivesStats r2)
                    r2.AddRevive();

                Stats.StatsManager.ModifyTeam(team, t => t.Revives++, false);

                if (medic.ActiveKit?.Item is { } mkit)
                {
                    Stats.StatsManager.ModifyStats(medic.Steam64, s =>
                    {
                        s.Revives++;
                        Stats.WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == mkit.Id && k.Team == team);
                        if (kitData == default)
                        {
                            kitData = new Stats.WarfareStats.KitData { KitID = mkit.Id, Team = team, Revives = 1 };
                            s.Kits.Add(kitData);
                        }
                        else
                        {
                            kitData.Revives++;
                        }
                    }, false);
                }
                else
                    Stats.StatsManager.ModifyStats(medic.Steam64, s => s.Revives++, false);
            }
        }
    }
    public void RevivePlayer(Player target)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_injuredPlayers.ContainsKey(target.channel.owner.playerID.steamID.m_SteamID))
        {
            if (target.TryGetComponent(out Reviver r))
                r.RevivePlayer();
        }
    }
    internal void OnPlayerDamagedRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!_injuredPlayers.TryGetValue(parameters.player.channel.owner.playerID.steamID.m_SteamID, out DownedPlayerData p))
        {
            UCPlayer? killer = UCPlayer.FromCSteamID(parameters.killer);
            if (killer != null && _injuredPlayers.ContainsKey(killer.Steam64))
            {
                shouldAllow = false;
                return;
            }
            if (CanPlayerInjure(in parameters) && UCPlayer.FromPlayer(parameters.player) is { } pl)
            {
                PlayerInjuring injuring = new PlayerInjuring(pl, in parameters);
                EventDispatcher.InvokeOnInjuringPlayer(injuring);
                if (injuring.CanContinue)
                {
                    shouldAllow = false;
                    InjurePlayer(in parameters, killer);
                }
            }
        }
        else if (Time.realtimeSinceStartup - p.Start >= 0.4f)
        {
            float bleedsPerSecond = Time.timeScale / EstSimulateTimeSec / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
            parameters = p.Parameters;
            parameters.damage *= UCWarfare.Config.InjuredDamageMultiplier / 10 * bleedsPerSecond * UCWarfare.Config.InjuredLifeTimeSeconds;
        }
        else
        {
            shouldAllow = false;
        }
    }
    internal void InjurePlayer(in DamagePlayerParameters parameters, UCPlayer? killer)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        parameters.player.equipment.dequip();

        // times per second FixedUpdate() is ran times bleed damage ticks = how many seconds it will take to lose 1 hp
        float bleedsPerSecond = (Time.timeScale / EstSimulateTimeSec) / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
        //L.Log(bleedsPerSecond + " bleed times per second");
        parameters.player.life.serverModifyHealth(UCWarfare.Config.InjuredLifeTimeSeconds * bleedsPerSecond - parameters.player.life.health);
        parameters.player.life.serverSetBleeding(true);
        ulong team = parameters.player.GetTeam();
        parameters.player.movement.sendPluginSpeedMultiplier(0.35f);
        parameters.player.movement.sendPluginJumpMultiplier(0);

        UCPlayer? player = UCPlayer.FromPlayer(parameters.player);

        short key = unchecked((short)Gamemode.Config.UIInjured.Value.Id);
        if (key != 0)
        {
            EffectManager.sendUIEffect(Gamemode.Config.UIInjured.Value, key, parameters.player.channel.owner.transportConnection, true, T.InjuredUIHeader.Translate(player), string.Empty);
            EffectManager.sendUIEffectText(key, parameters.player.channel.owner.transportConnection, true, "GiveUpText", T.InjuredUIGiveUp.Translate(player));
        }
        parameters.player.SendChat(T.InjuredUIGiveUpChat);

        ActionLog.Add(ActionLogType.Injured, "by " + (killer == null ? "self" : killer.Steam64.ToString(Data.AdminLocale)), parameters.player.channel.owner.playerID.steamID.m_SteamID);

        _injuredPlayers.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID, new DownedPlayerData(parameters));
        PlayerDied? died = DeathTracker.OnInjured(in parameters);
        Guid item = died != null ? died.PrimaryAsset : Guid.Empty;

        if (parameters.player.transform.TryGetComponent(out Reviver reviver))
            reviver.TellProneDelayed();

        if (killer != null)
        {
            if (killer.Player.TryGetPlayerData(out UCPlayerData c))
                c.TryUpdateAttackers(killer.Steam64);

            if (killer.Steam64 != parameters.player.channel.owner.playerID.steamID.m_SteamID) // suicide
            {
                byte kteam = killer.SteamPlayer.GetTeamByte();
                if (kteam != team)
                {
                    ToastMessage.QueueMessage(killer, new ToastMessage(ToastMessageStyle.Mini, T.XPToastEnemyInjured.Translate(killer)));

                    Stats.StatsManager.ModifyTeam(kteam, t => t.Downs++, false);
                    if (UCPlayer.FromSteamPlayer(killer) is { ActiveKit.Item: { } kit })
                    {
                        Stats.StatsManager.ModifyStats(killer.Steam64, s =>
                        {
                            s.Downs++;
                            Stats.WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Id && k.Team == kteam);
                            if (kitData == default)
                            {
                                kitData = new Stats.WarfareStats.KitData { KitID = kit.Id, Team = kteam, Downs = 1 };
                                s.Kits.Add(kitData);
                            }
                            else
                            {
                                kitData.Downs++;
                            }
                        }, false);
                        if (Assets.find(item) is ItemAsset asset)
                        {
                            Stats.StatsManager.ModifyWeapon(asset.id, kit.Id, w => w.Downs++, true);
                        }
                    }
                    else
                        Stats.StatsManager.ModifyStats(killer.Steam64, s => s.Downs++, false);
                }
                else
                    ToastMessage.QueueMessage(killer, new ToastMessage(ToastMessageStyle.Mini, T.XPToastFriendlyInjured.Translate(killer)));
            }
        }
    }
    private void OnPlayerDeath(PlayerDied e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        //L.Log(player.Player.channel.owner.playerID.playerName + " died in ReviveManager.", ConsoleColor.DarkRed);
        SetStanceBetter(e.Player, EPlayerStance.STAND);
        if (_injuredPlayers.ContainsKey(e.Steam64))
        {
            if (e.Player.Player.transform.TryGetComponent(out Reviver reviver))
            {
                reviver.FinishKillingPlayer(true);
            }
            else
            {
                _injuredPlayers.Remove(e.Steam64);
                DeathTracker.RemovePlayerInfo(e.Steam64);
                e.Player.Player.movement.sendPluginSpeedMultiplier(1.0f);
                e.Player.Player.movement.sendPluginJumpMultiplier(1.0f);
                e.Player.Player.life.serverSetBleeding(false);
            }

            EffectManager.askEffectClearByID(Gamemode.Config.UIInjured.Value, e.Player.Player.channel.owner.transportConnection);
            EffectManager.askEffectClearByID(Squads.SquadManager.Config.MedicMarker, e.Player.Player.channel.owner.transportConnection);
        }
    }
    private void OnKitChanged(UCPlayer player, SqlItem<Kit>? newKit, SqlItem<Kit>? oldKit)
    {
        
    }
    public IEnumerable<Vector3> GetPositionsOfTeam(ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong[] downed = _injuredPlayers.Keys.ToArray();
        List<Vector3> positions = new List<Vector3>(downed.Length / 2 + 4);
        for (int i = 0; i < downed.Length; i++)
        {
            UCPlayer? player = UCPlayer.FromID(downed[i]);
            if (player == null || player.GetTeam() != team) continue;
            positions.Add(player.Position);
        }
        return positions;
    }
    public void OnWinnerDeclared(ulong winner)
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            if (_injuredPlayers.TryGetValue(PlayerManager.OnlinePlayers[i].Steam64, out _))
            {
                if (PlayerManager.OnlinePlayers[i].Player.TryGetComponent(out Reviver r))
                    r.RevivePlayer(null, true);
            }
        }
    }



    private float _timeSinceLastMarkers = 0;
    public void Tick()
    {
        if (Time.time - _timeSinceLastMarkers > MarkerUpdateFrequency)
        {
            _timeSinceLastMarkers = Time.time;

            foreach (var player in PlayerManager.OnlinePlayers)
            {
                if (IsInjured(player))
                {
                    var medics = GetAvailableMedics(player);

                    // for all nearby medics, spawn an injured marker at this player's position 
                    if (SquadManager.Config.InjuredMarker.ValidReference(out EffectAsset injuredMarker))
                        F.TriggerEffectReliable(injuredMarker, Data.GetPooledTransportConnectionList(medics.Select(x => x.Connection), medics.Count()), player.Position);

                    // for this player only, spawn a medic marker at every nearby medic's position
                    if (SquadManager.Config.InjuredMarker.ValidReference(out EffectAsset medicMarker))
                    {
                        foreach (var medic in medics)
                            F.TriggerEffectReliable(medicMarker, player.Connection, medic.Position);
                    }                        
                }
            }
        }
    }

    [UsedImplicitly]
    private class Reviver : MonoBehaviour
    {
        private UCPlayer _player;
        
        [UsedImplicitly]
        void Start()
        {
            _player = UCPlayer.FromPlayer(gameObject.GetComponent<Player>())!;
            if (_player is null)
            {
                Destroy(this);
                L.Log("Failed to set up reviver for " + gameObject.name);
            }
            else
            {
                _player.Player.life.onHurt += OnPlayerPostDamage;
                _player.Player.inventory.onDropItemRequested += EventFunctions.OnDropItemTry;
                _player.Player.stance.onStanceUpdated += StanceUpdatedLocal;
                _player.Player.equipment.onEquipRequested += OnEquipRequested;
            }
        }
        private Coroutine? _stance;
#pragma warning disable IDE0051
        [UsedImplicitly]
        void OnDestroy()
        {
            if (_player is not null && _player.Player != null)
            {
                _player.Player.stance.onStanceUpdated -= StanceUpdatedLocal;
                _player.Player.equipment.onEquipRequested -= OnEquipRequested;
                _player.Player.life.onHurt -= OnPlayerPostDamage;
                _player.Player.inventory.onDropItemRequested -= EventFunctions.OnDropItemTry;
            }
        }
        private void OnEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
        {
            if (shouldAllow && Loaded)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (_singleton._injuredPlayers.ContainsKey(equipment.player.channel.owner.playerID.steamID.m_SteamID))
                {
                    shouldAllow = false;
                }
            }
        }
        private void StanceUpdatedLocal()
        {
            if (Loaded)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (_singleton._injuredPlayers.ContainsKey(_player.Player.channel.owner.playerID.steamID.m_SteamID))
                {
                    TellStanceNoDelay(EPlayerStance.PRONE);
                }
            }
        }
#pragma warning restore IDE0051
        private void OnPlayerPostDamage(Player player, byte damage, Vector3 force, EDeathCause cause, ELimb limb, CSteamID killerid)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (killerid.TryGetPlayerData(out UCPlayerData killer) && killer.Stats != null && killer.Stats is IPVPModeStats pvp)
            {
                pvp.AddDamage(damage);
            }
            if (player.TryGetPlayerData(out UCPlayerData victim))
            {
                victim.TryUpdateAttackers(killerid.m_SteamID);
            }

            if (player.life.health < 50 && !player.life.isDead && UCPlayer.FromPlayer(player) is { } pl)
                Tips.TryGiveTip(pl, 3600, T.TipCallMedic);
        }
        public void TellProneDelayed(float time = 0.5f)
        {
            _stance = StartCoroutine(WaitToChangeStance(EPlayerStance.PRONE, time));
        }
        private void TellStanceNoDelay(EPlayerStance stance)
        {
            _player.Player.stance.checkStance(stance, true);
        }
        private IEnumerator<WaitForSeconds> WaitToChangeStance(EPlayerStance stance, float time = 0.5f)
        {
            yield return new WaitForSeconds(time);
            TellStanceNoDelay(stance);
            //L.Log("Checked stance of " + Player.Player.channel.owner.playerID.playerName + " to " + stance.ToString() + ".", ConsoleColor.DarkRed);
            _stance = null;
        }
        public void TellStandDelayed(float time = 0.5f)
        {
            _stance = StartCoroutine(WaitToChangeStance(EPlayerStance.STAND, time));
        }

        private void CancelStance()
        {
            if (_stance != null)
            {
                StopCoroutine(_stance);
                _stance = null;
            }
        }
        public void RevivePlayer(IRevives? g = default, bool remove = true)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (g == default) Data.Is<IRevives>(out g);
            if (g != default)
            {
                if (Gamemode.Config.UIInjured.ValidReference(out Guid id))
                    EffectManager.ClearEffectByGuid(id, _player.Player.channel.owner.transportConnection);

                if (Squads.SquadManager.Config.MedicMarker.ValidReference(out id))
                    EffectManager.ClearEffectByGuid(id, _player.Player.channel.owner.transportConnection);

                _player.Player.movement.sendPluginSpeedMultiplier(1.0f);
                _player.Player.movement.sendPluginJumpMultiplier(1.0f);
                _player.Player.life.serverSetBleeding(false);

                CancelStance();
                if (remove)
                {
                    g.ReviveManager._injuredPlayers.Remove(_player.Player.channel.owner.playerID.steamID.m_SteamID);
                    DeathTracker.RemovePlayerInfo(_player.Player.channel.owner.playerID.steamID.m_SteamID);
                }
            }
        }
        public void SelfRevive()
        {
            RevivePlayer();
            _player.Player.stance.checkStance(EPlayerStance.CROUCH);
        }
        public void FinishKillingPlayer(bool isDead = false)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (Data.Is(out IRevives g))
            {
                RevivePlayer(g, false);
                if (!isDead)
                {
                    DamagePlayerParameters parameters = g.ReviveManager._injuredPlayers[_player.Player.channel.owner.playerID.steamID.m_SteamID].Parameters;
                    parameters.damage = 100.0f;
                    parameters.respectArmor = false;
                    parameters.applyGlobalArmorMultiplier = false;
                    DamageTool.damagePlayer(parameters, out _);
                }
                g.ReviveManager._injuredPlayers.Remove(_player.Player.channel.owner.playerID.steamID.m_SteamID);
                DeathTracker.RemovePlayerInfo(_player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
        }
    }
    private class DownedPlayerData
    {
        public readonly DamagePlayerParameters Parameters;
        public readonly float Start;
        public DownedPlayerData(DamagePlayerParameters parameters)
        {
            Parameters = parameters;
            Start = Time.realtimeSinceStartup;
        }
    }
}
