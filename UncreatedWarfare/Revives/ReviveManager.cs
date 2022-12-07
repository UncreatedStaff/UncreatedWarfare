﻿using JetBrains.Annotations;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Traits.Buffs;
using UnityEngine;

namespace Uncreated.Warfare.Revives;

[SingletonDependency(typeof(KitManager))]
public class ReviveManager : BaseSingleton, IPlayerConnectListener, IDeclareWinListener
{
    private readonly Dictionary<ulong, DownedPlayerData> DownedPlayers;
    public readonly List<UCPlayer> Medics;
    private static ReviveManager _singleton;
    private Coroutine? _updater;
    const float SIM_TIME = 0.08f;
    const bool CAN_HEAL_ENEMIES = true;
    public static bool Loaded => _singleton.IsLoaded();
    public ReviveManager()
    {
        DownedPlayers = new Dictionary<ulong, DownedPlayerData>(Provider.maxPlayers);
        Medics = new List<UCPlayer>(Provider.maxPlayers);
    }
    public bool IsInjured(ulong player) => DownedPlayers.ContainsKey(player);
    public bool IsInjured(IPlayer player) => DownedPlayers.ContainsKey(player.Steam64);
    public DamagePlayerParameters GetParameters(ulong player) => DownedPlayers.TryGetValue(player, out DownedPlayerData data) ? data.Parameters : default; 
    public bool CanPlayerInjure(ref DamagePlayerParameters parameters)
    {
        return parameters.player != null &&
               SafezoneManager.checkPointValid(parameters.player.transform.position) &&
               !parameters.player.life.isDead &&
               parameters.damage > parameters.player.life.health &&
               (parameters.cause is not EDeathCause.LANDMINE or EDeathCause.VEHICLE) &&
               parameters.cause < DeathTracker.MAIN_CAMP_OFFSET && // main campers can't get downed, makes death messages easier
               parameters.damage < 300;
    }
    public override void Load()
    {
        Medics.AddRange(PlayerManager.OnlinePlayers.Where(x => x.KitClass == Class.Medic).ToList());
        EventDispatcher.PlayerDied += OnPlayerDeath;
        PlayerLife.OnRevived_Global += OnPlayerRespawned;
        UseableConsumeable.onPerformingAid += OnHealPlayer;
        _singleton = this;
        _updater = UCWarfare.I.StartCoroutine(UpdatePositions());
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
        Medics.Clear();
        foreach (DownedPlayerData downedPlayer in DownedPlayers.Values.ToList())
        {
            if (downedPlayer.Parameters.player.TryGetComponent(out Reviver reviver))
                reviver.FinishKillingPlayer();
        }
        DownedPlayers.Clear();
        DeathTracker.ReviveManagerUnloading();
    }
    private void GiveUpPressed(UCPlayer player, float timeDown, ref bool handled)
    {
        if (DownedPlayers.TryGetValue(player.Steam64, out DownedPlayerData p))
        {
            player.Player.life.askDamage(byte.MaxValue, Vector3.down, p.Parameters.cause, p.Parameters.limb, p.Parameters.killer, out _,
                p.Parameters.trackKill, p.Parameters.ragdollEffect, false, true);
            handled = true;
        }
    }
    private void SelfRevivePressed(UCPlayer player, float timeDown, ref bool handled)
    {
        if (DownedPlayers.TryGetValue(player.Steam64, out DownedPlayerData data) &&
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
                player.SendChat(T.TraitSelfReviveCooldown, sr.Data, Mathf.CeilToInt(tl).GetTimeFromSeconds(player));
            }
        }
    }
    void IPlayerConnectListener.OnPlayerConnecting(UCPlayer player)
    {
        if (!player.Player.transform.gameObject.TryGetComponent<Reviver>(out _))
            player.Player.transform.gameObject.AddComponent<Reviver>();
        if (player.KitClass == Class.Medic)
            Medics.Add(player);
        DownedPlayers.Remove(player.Steam64);
        DeathTracker.RemovePlayerInfo(player.Steam64);
    }
    private IEnumerator<WaitForSeconds> UpdatePositions()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (DownedPlayers.Count == 0) continue;
            UpdateInjuredMarkers();
            UpdateMedicMarkers();
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
        if (!CAN_HEAL_ENEMIES || medic.GetTeam() != downed.GetTeam())
        {
            medic.SendChat(T.ReviveHealEnemies);
            shouldAllow = false;
            return;
        }
        if (!DownedPlayers.ContainsKey(downed.channel.owner.playerID.steamID.m_SteamID)) // if not injured
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
    internal void OnPlayerDisconnected(SteamPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Medics.RemoveAll(x => x == null || !x.IsOnline || x.Steam64 == player.playerID.steamID.m_SteamID);
        DeathTracker.RemovePlayerInfo(player.playerID.steamID.m_SteamID);
        if (DownedPlayers.TryGetValue(player.playerID.steamID.m_SteamID, out DownedPlayerData p))
        {
            if (PlayerManager.HasSave(player.playerID.steamID.m_SteamID, out PlayerSave save))
            {
                save.ShouldRespawnOnJoin = true;
                PlayerManager.ApplyToOnline();
            }
            player.player.life.askDamage(byte.MaxValue, Vector3.up, p.Parameters.cause, p.Parameters.limb, p.Parameters.killer, out _, p.Parameters.trackKill, p.Parameters.ragdollEffect, false, true);
            // player will be removed from list in OnDeath
        }
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
    internal void OnPlayerHealed(Player medic, Player target)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (target.TryGetComponent(out Reviver r) && DownedPlayers.ContainsKey(target.channel.owner.playerID.steamID.m_SteamID))
        {
            ActionLogger.Add(EActionLogType.REVIVED_PLAYER, target.channel.owner.playerID.steamID.m_SteamID.ToString(Data.Locale), medic.channel.owner.playerID.steamID.m_SteamID);
            r.RevivePlayer(null);
            byte team = medic.GetTeamByte();
            byte tteam = target.GetTeamByte();
            if (team == tteam)
            {
                // TODO: better points calculations
                UCPlayer? ucmedic = UCPlayer.FromPlayer(medic);
                UCPlayer? uctarget = UCPlayer.FromPlayer(target);
                if (ucmedic != null)
                {
                    if (CooldownManager.Config.ReviveXPCooldown <= 0 || (uctarget != null &&
                      !(CooldownManager.HasCooldown(ucmedic, ECooldownType.REVIVE, out Cooldown cooldown) &&
                        cooldown.data.Length > 0 &&
                        cooldown.data[0] is ulong id &&
                        id == uctarget.Steam64)))
                    {
                        Points.AwardXP(ucmedic, Points.XPConfig.FriendlyRevivedXP, T.XPToastHealedTeammate);
                        if (uctarget != null)
                        {
                            QuestManager.OnRevive(ucmedic, uctarget);
                            CooldownManager.StartCooldown(ucmedic, ECooldownType.REVIVE, CooldownManager.Config.ReviveXPCooldown, uctarget.Steam64);
                        }
                    }
                    else
                    {
                        ToastMessage.QueueMessage(ucmedic, new ToastMessage(
                            T.XPToastGainXP.Translate(ucmedic, 0) + "\n" +
                            T.XPToastHealedTeammate.Translate(ucmedic).Colorize("adadad"), EToastMessageSeverity.MINI));
                    }
                }


                if (medic.TryGetPlayerData(out UCPlayerData c) && c.stats is IRevivesStats r2)
                    r2.AddRevive();

                Stats.StatsManager.ModifyTeam(team, t => t.Revives++, false);
                if (KitManager.HasKit(medic, out KitOld kit))
                {
                    Stats.StatsManager.ModifyStats(medic.channel.owner.playerID.steamID.m_SteamID, s =>
                    {
                        s.Revives++;
                        Stats.WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == team);
                        if (kitData == default)
                        {
                            kitData = new Stats.WarfareStats.KitData() { KitID = kit.Name, Team = team, Revives = 1 };
                            s.Kits.Add(kitData);
                        }
                        else
                        {
                            kitData.Revives++;
                        }
                    }, false);
                }
                else
                    Stats.StatsManager.ModifyStats(medic.channel.owner.playerID.steamID.m_SteamID, s => s.Revives++, false);
            }
        }
    }
    public void RevivePlayer(Player target)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (DownedPlayers.ContainsKey(target.channel.owner.playerID.steamID.m_SteamID))
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
        if (!DownedPlayers.TryGetValue(parameters.player.channel.owner.playerID.steamID.m_SteamID, out DownedPlayerData p))
        {
            SteamPlayer? killer = PlayerTool.getSteamPlayer(parameters.killer);
            if (killer != null && DownedPlayers.ContainsKey(killer.playerID.steamID.m_SteamID))
            {
                shouldAllow = false;
                return;
            }
            if (CanPlayerInjure(ref parameters))
            {
                InjurePlayer(ref shouldAllow, ref parameters, killer);
            }
        }
        else if (Time.realtimeSinceStartup - p.Start >= 0.4f)
        {
            float bleedsPerSecond = Time.timeScale / SIM_TIME / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
            parameters = p.Parameters;
            parameters.damage *= UCWarfare.Config.InjuredDamageMultiplier / 10 * bleedsPerSecond * UCWarfare.Config.InjuredLifeTimeSeconds;
        }
        else
        {
            shouldAllow = false;
        }
    }
    internal void InjurePlayer(ref bool shouldAllow, ref DamagePlayerParameters parameters, SteamPlayer? killer)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!shouldAllow)
            return;
        if (parameters.player.movement.getVehicle() != null || parameters.cause == EDeathCause.VEHICLE)
            return;
        shouldAllow = false;
        parameters.player.equipment.dequip();

        // times per second FixedUpdate() is ran times bleed damage ticks = how many seconds it will take to lose 1 hp
        float bleedsPerSecond = (Time.timeScale / SIM_TIME) / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
        //L.Log(bleedsPerSecond + " bleed times per second");
        parameters.player.life.serverModifyHealth(UCWarfare.Config.InjuredLifeTimeSeconds * bleedsPerSecond - parameters.player.life.health);
        parameters.player.life.serverSetBleeding(true);
        ulong team = parameters.player.GetTeam();
        parameters.player.movement.sendPluginSpeedMultiplier(0.35f);
        parameters.player.movement.sendPluginJumpMultiplier(0);
        short key = unchecked((short)Gamemode.Config.UIInjured.Value.Id);
        if (key != 0)
        {
            EffectManager.sendUIEffect(Gamemode.Config.UIInjured.Value, key, parameters.player.channel.owner.transportConnection, true, T.InjuredUIHeader.Translate(parameters.player.channel.owner.playerID.steamID.m_SteamID), string.Empty);
            EffectManager.sendUIEffectText(key, parameters.player.channel.owner.transportConnection, true, "GiveUpText", T.InjuredUIGiveUp.Translate(parameters.player.channel.owner.playerID.steamID.m_SteamID));
        }
        parameters.player.SendChat(T.InjuredUIGiveUpChat);

        ActionLogger.Add(EActionLogType.INJURED, "by " + (killer == null ? "self" : killer.playerID.steamID.m_SteamID.ToString(Data.Locale)), parameters.player.channel.owner.playerID.steamID.m_SteamID);

        DownedPlayers.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID, new DownedPlayerData(parameters));
        SpawnInjuredMarker(parameters.player.transform.position, team);
        UpdateMedicMarkers(parameters.player, team, parameters.player.transform.position, false);
        Guid item = Guid.Empty;
        DeathTracker.OnInjured(ref parameters);
        if (killer != default)
        {
            if (killer.player.TryGetPlayerData(out UCPlayerData c))
            {
                c.TryUpdateAttackers(killer.playerID.steamID.m_SteamID);
            }
            if (killer.playerID.steamID.m_SteamID != parameters.player.channel.owner.playerID.steamID.m_SteamID) // suicide
            {
                byte kteam = killer.GetTeamByte();
                if (kteam != team)
                {
                    ToastMessage.QueueMessage(killer, new ToastMessage(T.XPToastEnemyInjured.Translate(killer.playerID.steamID.m_SteamID), EToastMessageSeverity.MINI));
                    if (parameters.player.transform.TryGetComponent(out UCPlayerData p))
                    {
                        if ((DateTime.Now - p.secondLastAttacker.Value).TotalSeconds < 30 && p.secondLastAttacker.Key != parameters.killer.m_SteamID)
                        {
                            //ToastMessage.QueueMessage(killer, new ToastMessage(Translation.Translate("xp_assist_enemy_downed", killer), EToastMessageSeverity.MINI));
                        }
                    }

                    Stats.StatsManager.ModifyTeam(kteam, t => t.Downs++, false);
                    if (KitManager.HasKit(killer, out KitOld kit))
                    {
                        Stats.StatsManager.ModifyStats(killer.playerID.steamID.m_SteamID, s =>
                        {
                            s.Downs++;
                            Stats.WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == kteam);
                            if (kitData == default)
                            {
                                kitData = new Stats.WarfareStats.KitData() { KitID = kit.Name, Team = kteam, Downs = 1 };
                                s.Kits.Add(kitData);
                            }
                            else
                            {
                                kitData.Downs++;
                            }
                        }, false);
                        if (Assets.find(item) is ItemAsset asset && asset != null)
                        {
                            Stats.StatsManager.ModifyWeapon(asset.id, kit.Name, w => w.Downs++, true);
                        }
                    }
                    else
                        Stats.StatsManager.ModifyStats(killer.playerID.steamID.m_SteamID, s => s.Downs++, false);
                }
                else
                    ToastMessage.QueueMessage(killer, new ToastMessage(T.XPToastFriendlyInjured.Translate(Localization.GetLang(killer.playerID.steamID.m_SteamID)), EToastMessageSeverity.MINI));
            }
        }
        if (parameters.player.transform.TryGetComponent(out Reviver reviver))
        {
            reviver.TellProneDelayed();
            //reviver.StartBleedout();
        }
    }
    private void OnPlayerDeath(PlayerDied e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        //L.Log(player.Player.channel.owner.playerID.playerName + " died in ReviveManager.", ConsoleColor.DarkRed);
        SetStanceBetter(e.Player, EPlayerStance.STAND);
        if (DownedPlayers.ContainsKey(e.Steam64))
        {
            if (e.Player.Player.transform.TryGetComponent(out Reviver reviver))
            {
                reviver.FinishKillingPlayer(true);
            }
            else
            {
                DownedPlayers.Remove(e.Steam64);
                DeathTracker.RemovePlayerInfo(e.Steam64);
                e.Player.Player.movement.sendPluginSpeedMultiplier(1.0f);
                e.Player.Player.movement.sendPluginJumpMultiplier(1.0f);
                e.Player.Player.life.serverSetBleeding(false);
            }

            EffectManager.askEffectClearByID(Gamemode.Config.UIInjured.Value, e.Player.Player.channel.owner.transportConnection);
            EffectManager.askEffectClearByID(Squads.SquadManager.Config.MedicMarker, e.Player.Player.channel.owner.transportConnection);
        }
        ClearInjuredMarker(e.Steam64, e.Player.GetTeam());
    }
    private void OnKitChanged(UCPlayer player, SqlItem<Kit>? oldkit, SqlItem<Kit>? newKit)
    {
        bool oldIsMedic = oldkit?.Item != null && oldkit.Item.Class == Class.Medic;
        if (newKit?.Item != null && newKit.Item.Class == Class.Medic)
        {
            if (!oldIsMedic)
                RegisterMedic(player);
            return;
        }
        if (oldIsMedic)
            DeregisterMedic(player);
    }
    private void RegisterMedic(UCPlayer player)
    {
        Medics.Add(player);
        IEnumerable<Vector3> newpositions = GetPositionsOfTeam(player.GetTeam());
        SpawnInjuredMarkers(player.Player.channel.owner.transportConnection, newpositions, true, player.Position);
    }
    private void DeregisterMedic(UCPlayer player)
    {
        Medics.RemoveAll(x => x == null || x.Steam64 == player.Steam64);
        ClearInjuredMarkers(player);
    }
    public void SpawnInjuredMarker(Vector3 position, ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Squads.SquadManager.Config.InjuredMarker.ValidReference(out EffectAsset effect))
        {
            double sqrmLimit = Math.Pow(Squads.SquadManager.Config.MedicRange, 2);
            F.TriggerEffectReliable(effect, Medics.Where(x =>
            {
                if (x.GetTeam() != team)
                    return false;
                float sqr = (x.Position - position).sqrMagnitude;
                return sqr > 1 && sqr <= sqrmLimit;
            }).Select(x => x.Player.channel.owner.transportConnection), position);
        }
    }
    public void SpawnInjuredMarkers(IEnumerable<UCPlayer> players, IEnumerable<Vector3> positions, bool dispose, bool clearAll)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float range = Mathf.Pow(Squads.SquadManager.Config.MedicRange, 2);
        if (Squads.SquadManager.Config.InjuredMarker.ValidReference(out EffectAsset asset))
        {
            foreach (UCPlayer player in players)
            {
                ITransportConnection c = player.Player.channel.owner.transportConnection;
                if (clearAll)
                    EffectManager.ClearEffectByGuid(asset.GUID, c);
                foreach (Vector3 position in positions)
                {
                    float sqrDistance = (player.Position - position).sqrMagnitude;
                    if (sqrDistance >= 1 && sqrDistance <= range)
                        F.TriggerEffectReliable(asset, c, position);
                }
            }
        }
    }
    public void SpawnMedicMarkers(IEnumerable<ITransportConnection> players, IEnumerable<Vector3> positions, bool dispose, bool clearAll)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Squads.SquadManager.Config.InjuredMarker.ValidReference(out EffectAsset asset))
        {
            foreach (ITransportConnection c in players)
            {
                if (clearAll)
                    EffectManager.ClearEffectByGuid(asset.GUID, c);
                foreach (Vector3 position in positions)
                    F.TriggerEffectReliable(asset, c, position);
            }
        }
    }

    public void SpawnInjuredMarkers(ITransportConnection player, IEnumerable<Vector3> positions, bool clearAll, Vector3 center)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float range = Mathf.Pow(Squads.SquadManager.Config.MedicRange, 2);
        if (Squads.SquadManager.Config.InjuredMarker.ValidReference(out EffectAsset asset))
        {
            if (clearAll)
                EffectManager.ClearEffectByGuid(asset.GUID, player);
            foreach (Vector3 position in positions)
            {
                float sqr = (center - position).sqrMagnitude;
                if (sqr > 1 && sqr <= range)
                    F.TriggerEffectReliable(asset, player, position);
            }
        }
    }
    public void SpawnMedicMarkers(ITransportConnection player, IEnumerable<Vector3> positions, bool clearAll)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Squads.SquadManager.Config.InjuredMarker.ValidReference(out EffectAsset asset))
        {
            if (clearAll)
                EffectManager.ClearEffectByGuid(asset.GUID, player);
            foreach (Vector3 position in positions)
                F.TriggerEffectReliable(asset, player, position);
        }
    }
    public void ClearInjuredMarker(ulong clearedPlayer, ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        IEnumerable<UCPlayer> medics = Medics
            .Where(x => x.GetTeam() == team);
        List<Vector3> positions = new List<Vector3>();
        foreach (ulong down in DownedPlayers.Keys)
        {
            if (down == clearedPlayer) continue;
            UCPlayer? player = UCPlayer.FromID(down);
            if (player == null) continue;
            positions.Add(player.Position);
        }
        Vector3[] newpositions = positions.ToArray();
        SpawnInjuredMarkers(medics, newpositions, true, true);
    }
    public void ClearInjuredMarkers(UCPlayer medic)
    {
        EffectManager.askEffectClearByID(Squads.SquadManager.Config.InjuredMarker, medic.Player.channel.owner.transportConnection);
    }
    public IEnumerable<Vector3> GetPositionsOfTeam(ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong[] downed = DownedPlayers.Keys.ToArray();
        List<Vector3> positions = new List<Vector3>(downed.Length / 2 + 4);
        for (int i = 0; i < downed.Length; i++)
        {
            UCPlayer? player = UCPlayer.FromID(downed[i]);
            if (player == null || player.GetTeam() != team) continue;
            positions.Add(player.Position);
        }
        return positions;
    }
    public void UpdateInjuredMarkers()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        IEnumerable<UCPlayer> medics = Medics.Where(x => x.GetTeam() == 1);
        IEnumerable<Vector3> newpositions = GetPositionsOfTeam(1);
        SpawnInjuredMarkers(medics, newpositions, true, true);
        medics = Medics.Where(x => x.GetTeam() == 2);
        newpositions = GetPositionsOfTeam(2);
        SpawnInjuredMarkers(medics, newpositions, true, true);
    }
    public void UpdateMedicMarkers()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float range = Mathf.Pow(Squads.SquadManager.Config.MedicRange, 2);
        foreach (ulong s64 in DownedPlayers.Keys)
        {
            UCPlayer? downed = UCPlayer.FromID(s64);
            if (downed == null) continue;
            ulong team = downed.GetTeam();
            SpawnMedicMarkers(downed.Player.channel.owner.transportConnection, Medics
                .Where(x => x.GetTeam() == team && (x.Position - downed.Position).sqrMagnitude < range && x.Steam64 != downed.Steam64)
                .Select(x => x.Position), true);
        }
    }
    public void UpdateMedicMarkers(Player player, ulong team, Vector3 origin, bool clearOld)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong s = player.channel.owner.playerID.steamID.m_SteamID;
        float range = Mathf.Pow(Squads.SquadManager.Config.MedicRange, 2);
        if (team is > 0 and < 3) return;
        IEnumerable<Vector3> medics = Medics
            .Where(x => x.GetTeam() == team &&
                (x.Position - origin).sqrMagnitude <
                range && x.Steam64 != s)
            .Select(x => x.Position);
        SpawnMedicMarkers(player.channel.owner.transportConnection, medics, clearOld);
    }
    public void OnWinnerDeclared(ulong winner)
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            if (DownedPlayers.TryGetValue(PlayerManager.OnlinePlayers[i].Steam64, out _))
            {
                if (PlayerManager.OnlinePlayers[i].Player.TryGetComponent(out Reviver r))
                    r.RevivePlayer(null, true);
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
                if (_singleton.DownedPlayers.ContainsKey(equipment.player.channel.owner.playerID.steamID.m_SteamID))
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
                if (_singleton.DownedPlayers.ContainsKey(_player.Player.channel.owner.playerID.steamID.m_SteamID))
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
            if (killerid.TryGetPlayerData(out UCPlayerData killer) && killer.stats != null && killer.stats is IPVPModeStats pvp)
            {
                pvp.AddDamage(damage);
            }
            if (player.TryGetPlayerData(out UCPlayerData victim))
            {
                victim.TryUpdateAttackers(killerid.m_SteamID);
            }
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
            this._stance = null;
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

                g.ReviveManager.ClearInjuredMarker(_player.Player.channel.owner.playerID.steamID.m_SteamID, _player.GetTeam());

                _player.Player.movement.sendPluginSpeedMultiplier(1.0f);
                _player.Player.movement.sendPluginJumpMultiplier(1.0f);
                _player.Player.life.serverSetBleeding(false);

                CancelStance();
                if (remove)
                {
                    g.ReviveManager.DownedPlayers.Remove(_player.Player.channel.owner.playerID.steamID.m_SteamID);
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
                this.RevivePlayer(g, false);
                if (!isDead)
                {
                    DamagePlayerParameters parameters = g.ReviveManager.DownedPlayers[_player.Player.channel.owner.playerID.steamID.m_SteamID].Parameters;
                    parameters.damage = 100.0f;
                    parameters.respectArmor = false;
                    parameters.applyGlobalArmorMultiplier = false;
                    DamageTool.damagePlayer(parameters, out _);
                }
                g.ReviveManager.DownedPlayers.Remove(_player.Player.channel.owner.playerID.steamID.m_SteamID);
                DeathTracker.RemovePlayerInfo(_player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
        }
    }
    private readonly struct DownedPlayerData
    {
        public readonly DamagePlayerParameters Parameters;
        public readonly float Start;
        public DownedPlayerData(DamagePlayerParameters parameters)
        {
            this.Parameters = parameters;
            Start = Time.realtimeSinceStartup;
        }
    }
}
