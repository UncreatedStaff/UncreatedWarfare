using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
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
using UnityEngine;

namespace Uncreated.Warfare.Revives;

public class ReviveManager : BaseSingleton, IPlayerConnectListener
{
    public readonly Dictionary<ulong, DownedPlayerData> DownedPlayers;
    public readonly List<UCPlayer> Medics = new List<UCPlayer>();
    private static ReviveManager Singleton;
    private Coroutine? Updater;
    const float SIM_TIME = 0.08f;
    const bool CAN_HEAL_ENEMIES = true;
    public static bool Loaded => Singleton.IsLoaded();
    public ReviveManager()
    {
        DownedPlayers = new Dictionary<ulong, DownedPlayerData>(Provider.maxPlayers);
        Medics = new List<UCPlayer>(Provider.maxPlayers);
    }

    public bool CanPlayerInjure(ref DamagePlayerParameters parameters)
    {
        return parameters.player.life.isDead &&
               parameters.damage > parameters.player.life.health &&
               (parameters.cause is EDeathCause.LANDMINE or EDeathCause.VEHICLE) &&
               parameters.cause < DeathTracker.MAIN_CAMP_OFFSET && // main campers can't get downed, makes death messages easier
               parameters.damage < 300;
    }
    public override void Load()
    {
        Medics.AddRange(PlayerManager.OnlinePlayers.Where(x => x.KitClass == EClass.MEDIC).ToList());
        EventDispatcher.OnPlayerDied += OnPlayerDeath;
        PlayerLife.OnRevived_Global += OnPlayerRespawned;
        UseableConsumeable.onPerformingAid += OnHealPlayer;
        Singleton = this;
        Updater = UCWarfare.I.StartCoroutine(UpdatePositions());
    }
    public override void Unload()
    {
        Singleton = null!;
        UseableConsumeable.onPerformingAid -= OnHealPlayer;
        PlayerLife.OnRevived_Global -= OnPlayerRespawned;
        EventDispatcher.OnPlayerDied -= OnPlayerDeath;
        if (Updater is not null)
        {
            UCWarfare.I.StopCoroutine(Updater);
            Updater = null;
        }
        Medics.Clear();
        foreach (DownedPlayerData downedPlayer in DownedPlayers.Values.ToList())
        {
            if (downedPlayer.parameters.player.TryGetComponent(out Reviver reviver))
                reviver.FinishKillingPlayer();
        }
        DownedPlayers.Clear();
        DeathTracker.ReviveManagerUnloading();
    }
    void IPlayerConnectListener.OnPlayerConnecting(UCPlayer player)
    {
        if (player.KitClass == EClass.MEDIC)
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
            medic.Message("heal_e_enemy");
            shouldAllow = false;
            return;
        }
        if (!DownedPlayers.ContainsKey(downed.channel.owner.playerID.steamID.m_SteamID)) // if not injured
            return;
        if (medic.KitClass != EClass.MEDIC)
        {
            medic.Message("heal_e_notmedic");
            shouldAllow = false;
            return;
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
    /// <summary>Pre-destroy</summary>
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
            player.player.life.askDamage(byte.MaxValue, Vector3.up, p.parameters.cause, p.parameters.limb, p.parameters.killer, out _, p.parameters.trackKill, p.parameters.ragdollEffect, false, true);
            // player will be removed from list in OnDeath
        }
    }
    internal void SetStanceBetter(Player player, EPlayerStance stance)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.PrivateStance == null || Data.ReplicateStance == null)
        {
            player.stance.checkStance(stance);
            L.LogWarning("Unable to set stance properly, fell back to checkStance.");
        }
        Data.PrivateStance?.SetValue(player.stance, stance);
        Data.ReplicateStance?.Invoke(player.stance, new object[] { false });
    }
    internal void OnPlayerHealed(Player medic, Player target)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (target.TryGetComponent(out Reviver r) && DownedPlayers.ContainsKey(target.channel.owner.playerID.steamID.m_SteamID))
        {
            ActionLog.Add(EActionLogType.REVIVED_PLAYER, target.channel.owner.playerID.steamID.m_SteamID.ToString(Data.Locale), medic.channel.owner.playerID.steamID.m_SteamID);
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
                    Points.AwardXP(ucmedic, Points.XPConfig.FriendlyRevivedXP, Translation.Translate("xp_healed_teammate", medic.channel.owner.playerID.steamID.m_SteamID, F.GetPlayerOriginalNames(target).CharacterName));
                    if (uctarget != null)
                        QuestManager.OnRevive(ucmedic, uctarget);
                }


                if (medic.TryGetPlayerData(out UCPlayerData c) && c.stats is IRevivesStats r2)
                    r2.AddRevive();

                Stats.StatsManager.ModifyTeam(team, t => t.Revives++, false);
                if (KitManager.HasKit(medic, out Kit kit))
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
            EffectManager.askEffectClearByID(UCWarfare.Config.GiveUpUI, target.channel.owner.transportConnection);
            EffectManager.askEffectClearByID(Squads.SquadManager.Config.MedicMarker, target.channel.owner.transportConnection);
            ClearInjuredMarker(target.channel.owner.playerID.steamID.m_SteamID, tteam);
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
        if (Data.Gamemode.State != EState.ACTIVE)
        {
            shouldAllow = false;
            return;
        }
        if (Data.Gamemode is ITeams)
        {
            if (Teams.TeamManager.LobbyZone != null && Teams.TeamManager.LobbyZone.IsInside(parameters.player.transform.position))
                return;
        }
        if (!DownedPlayers.TryGetValue(parameters.player.channel.owner.playerID.steamID.m_SteamID, out DownedPlayerData p))
        {
            SteamPlayer? killer = PlayerTool.getSteamPlayer(parameters.killer);
            if (killer != null && DownedPlayers.ContainsKey(killer.playerID.steamID.m_SteamID))
            {
                shouldAllow = false;
                return;
            }
            UCPlayer? player = UCPlayer.FromPlayer(parameters.player);
            if (player != null && player.OnDuty())
            {
                if (parameters.player.TryGetComponent(out UnturnedPlayerFeatures features) && features.GodMode)
                {
                    shouldAllow = false;
                    return;
                }
            }

            if (CanPlayerInjure(ref parameters))
            {
                InjurePlayer(ref shouldAllow, ref parameters, killer);
            }
        }
        else
        {
            if ((DateTime.Now - p.start).TotalSeconds >= 0.4)
            {
                float bleedsPerSecond = Time.timeScale / SIM_TIME / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
                parameters = p.parameters;
                parameters.damage *= UCWarfare.Config.InjuredDamageMultiplier / 10 * bleedsPerSecond * UCWarfare.Config.InjuredLifeTimeSeconds;
            }
            else
            {
                shouldAllow = false;
            }
        }
    }
    private void InjurePlayer(ref bool shouldAllow, ref DamagePlayerParameters parameters, SteamPlayer? killer)
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
        short key = unchecked((short)UCWarfare.Config.GiveUpUI);
        EffectManager.sendUIEffect(UCWarfare.Config.GiveUpUI, key, parameters.player.channel.owner.transportConnection, true, Translation.Translate("injured_ui_header", parameters.player), string.Empty);
        EffectManager.sendUIEffectText(key, parameters.player.channel.owner.transportConnection, true, "GiveUpText", Translation.Translate("injured_ui_give_up", parameters.player));
        parameters.player.SendChat("injured_chat");

        ActionLog.Add(EActionLogType.INJURED, "by " + (killer == null ? "self" : killer.playerID.steamID.m_SteamID.ToString(Data.Locale)), parameters.player.channel.owner.playerID.steamID.m_SteamID);

        DownedPlayers.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID, new DownedPlayerData(parameters));
        SpawnInjuredMarker(parameters.player.transform.position, team);
        UpdateMedicMarkers(parameters.player.channel.owner.transportConnection, team, parameters.player.transform.position, false);
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
                    ToastMessage.QueueMessage(killer, new ToastMessage(Translation.Translate("xp_enemy_downed", killer), EToastMessageSeverity.MINI));
                    if (parameters.player.transform.TryGetComponent(out UCPlayerData p))
                    {
                        if ((DateTime.Now - p.secondLastAttacker.Value).TotalSeconds < 30 && p.secondLastAttacker.Key != parameters.killer.m_SteamID)
                        {
                            //ToastMessage.QueueMessage(killer, new ToastMessage(Translation.Translate("xp_assist_enemy_downed", killer), EToastMessageSeverity.MINI));
                        }
                    }

                    Stats.StatsManager.ModifyTeam(kteam, t => t.Downs++, false);
                    if (KitManager.HasKit(killer, out Kit kit))
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
                    ToastMessage.QueueMessage(killer, new ToastMessage("", Translation.Translate("xp_friendly_downed", killer), EToastMessageSeverity.MINI));
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

            EffectManager.askEffectClearByID(UCWarfare.Config.GiveUpUI, e.Player.Player.channel.owner.transportConnection);
            EffectManager.askEffectClearByID(Squads.SquadManager.Config.MedicMarker, e.Player.Player.channel.owner.transportConnection);
        }
        ClearInjuredMarker(e.Steam64, e.Player.GetTeam());
    }
    public void RegisterMedic(UCPlayer player)
    {
        Medics.Add(player);
        Vector3[] newpositions = GetPositionsOfTeam(player.GetTeam());
        SpawnInjuredMarkers(player.Player.channel.owner.transportConnection, newpositions, true, player.Position);
    }
    public void DeregisterMedic(UCPlayer player)
    {
        Medics.RemoveAll(x => x == null || x.Steam64 == player.Steam64);
        ClearInjuredMarkers(player);
    }
    public void SpawnInjuredMarker(Vector3 Position, ulong Team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        IEnumerator<UCPlayer> player = Medics
            .Where(x => x.GetTeam() == Team)
            .GetEnumerator();
        while (player.MoveNext())
        {
            var sqrDistance = (player.Current.Position - Position).sqrMagnitude;
            var sqrmLimit = Math.Pow(Squads.SquadManager.Config.MedicRange, 2);
            if (sqrDistance >= 1 && sqrDistance <= sqrmLimit)
                EffectManager.sendEffectReliable(Squads.SquadManager.Config.InjuredMarker, player.Current.Player.channel.owner.transportConnection, Position);
        }
        player.Dispose();
    }
    internal void GiveUp(Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (DownedPlayers.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out DownedPlayerData p))
        {
            player.life.askDamage(byte.MaxValue, Vector3.down, p.parameters.cause, p.parameters.limb, p.parameters.killer, out _, p.parameters.trackKill, p.parameters.ragdollEffect, false, true);
            // player and Revive UI will be removed from list in OnDeath
        }
    }
    public void SpawnInjuredMarkers(IEnumerator<UCPlayer> players, Vector3[] positions, bool dispose, bool clearAll)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        while (players.MoveNext())
        {
            if (clearAll)
                EffectManager.askEffectClearByID(Squads.SquadManager.Config.InjuredMarker, players.Current.Player.channel.owner.transportConnection);
            for (int i = 0; i < positions.Length; i++)
            {
                var sqrDistance = (players.Current.Position - positions[i]).sqrMagnitude;
                var sqrmLimit = Math.Pow(Squads.SquadManager.Config.MedicRange, 2);
                if (sqrDistance >= 1 && sqrDistance <= sqrmLimit)
                    EffectManager.sendEffectReliable(Squads.SquadManager.Config.InjuredMarker, players.Current.Player.channel.owner.transportConnection, positions[i]);
            }
        }
        if (dispose) players.Dispose();
    }
    public void SpawnMedicMarkers(IEnumerator<ITransportConnection> players, Vector3[] positions, bool dispose, bool clearAll)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        while (players.MoveNext())
        {
            if (clearAll)
                EffectManager.askEffectClearByID(Squads.SquadManager.Config.MedicMarker, players.Current);
            for (int i = 0; i < positions.Length; i++)
                EffectManager.sendEffectReliable(Squads.SquadManager.Config.MedicMarker, players.Current, positions[i]);
        }
        if (dispose) players.Dispose();
    }
    public void SpawnInjuredMarkers(ITransportConnection player, Vector3[] positions, bool clearAll, Vector3 center)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (clearAll)
            EffectManager.askEffectClearByID(Squads.SquadManager.Config.InjuredMarker, player);
        for (int i = 0; i < positions.Length; i++)
            if ((center - positions[i]).sqrMagnitude <= Math.Pow(Squads.SquadManager.Config.MedicRange, 2))
                EffectManager.sendEffectReliable(Squads.SquadManager.Config.InjuredMarker, player, positions[i]);
    }
    public void SpawnMedicMarkers(ITransportConnection player, Vector3[] positions, bool clearAll)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (clearAll)
            EffectManager.askEffectClearByID(Squads.SquadManager.Config.MedicMarker, player);
        for (int i = 0; i < positions.Length; i++)
            EffectManager.sendEffectReliable(Squads.SquadManager.Config.MedicMarker, player, positions[i]);
    }
    public void ClearInjuredMarker(ulong clearedPlayer, ulong Team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        IEnumerator<UCPlayer> medics = Medics
            .Where(x => x.GetTeam() == Team)
            .GetEnumerator();
        ulong[] downed = DownedPlayers.Keys.ToArray();
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < downed.Length; i++)
        {
            if (downed[i] == clearedPlayer) continue;
            UCPlayer? player = UCPlayer.FromID(downed[i]);
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
    public Vector3[] GetPositionsOfTeam(ulong Team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong[] downed = DownedPlayers.Keys.ToArray();
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < downed.Length; i++)
        {
            UCPlayer? player = UCPlayer.FromID(downed[i]);
            if (player == null || player.GetTeam() != Team) continue;
            positions.Add(player.Position);
        }
        return positions.ToArray();
    }
    public void UpdateInjuredMarkers()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        IEnumerator<UCPlayer> medics = Medics.
            Where(x => x.GetTeam() == 1)
            .GetEnumerator();
        Vector3[] newpositions = GetPositionsOfTeam(1);
        SpawnInjuredMarkers(medics, newpositions, true, true);
        medics = Medics
            .Where(x => x.GetTeam() == 2)
            .GetEnumerator();
        newpositions = GetPositionsOfTeam(2);
        SpawnInjuredMarkers(medics, newpositions, true, true);
    }
    public void UpdateMedicMarkers()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < DownedPlayers.Keys.Count; i++)
        {
            UCPlayer? downed = UCPlayer.FromID(DownedPlayers.Keys.ElementAt(i));
            if (downed == null) continue;
            ulong team = downed.GetTeam();
            Vector3[] medics = Medics
                .Where(x => x.GetTeam() == team &&
                    (x.Position - downed.Position).sqrMagnitude <
                    Math.Pow(Squads.SquadManager.Config.MedicRange, 2) &&
                    x.Connection != downed.Connection)
                .Select(x => x.Position)
                .ToArray();
            SpawnMedicMarkers(downed.Player.channel.owner.transportConnection, medics, true);
        }
    }
    public void UpdateMedicMarkers(ITransportConnection player, ulong team, Vector3 origin, bool clearOld)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (team > 0 && team < 3) return;
        Vector3[] medics = Medics
            .Where(x => x.GetTeam() == team &&
                (x.Position - origin).sqrMagnitude <
                Math.Pow(Squads.SquadManager.Config.MedicRange, 2) &&
                    x.Connection != player)
            .Select(x => x.Position)
            .ToArray();
        SpawnMedicMarkers(player, medics, clearOld);
    }


    private class Reviver : UnturnedPlayerComponent
    {
        private Coroutine? stance;
#pragma warning disable IDE0051
        void Start()
        {
            Player.Player.life.onHurt += OnPlayerPostDamage;
            Player.Player.inventory.onDropItemRequested += EventFunctions.OnDropItemTry;
            Player.Player.stance.onStanceUpdated += StanceUpdatedLocal;
            Player.Player.equipment.onEquipRequested += OnEquipRequested;
        }
        void OnDestroy()
        {
            Player.Player.stance.onStanceUpdated -= StanceUpdatedLocal;
            Player.Player.equipment.onEquipRequested -= OnEquipRequested;
            Player.Player.life.onHurt -= OnPlayerPostDamage;
            Player.Player.inventory.onDropItemRequested -= EventFunctions.OnDropItemTry;
        }
        private void OnEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
        {
            if (shouldAllow && Loaded)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (Singleton.DownedPlayers.ContainsKey(equipment.player.channel.owner.playerID.steamID.m_SteamID))
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
                if (Singleton.DownedPlayers.ContainsKey(Player.Player.channel.owner.playerID.steamID.m_SteamID))
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
            if (killerid.TryGetPlayerData(out Components.UCPlayerData killer) && killer.stats != null && killer.stats is IPVPModeStats pvp)
            {
                pvp.AddDamage(damage);
            }
            if (player.TryGetPlayerData(out Components.UCPlayerData victim))
            {
                victim.TryUpdateAttackers(killerid.m_SteamID);
            }
        }

        public void TellProneDelayed(float time = 0.5f)
        {
            stance = StartCoroutine(WaitToChangeStance(EPlayerStance.PRONE, time));
        }
        public void TellStanceNoDelay(EPlayerStance stance)
        {
            Player.Player.stance.checkStance(stance, true);
        }
        private IEnumerator<WaitForSeconds> WaitToChangeStance(EPlayerStance stance, float time = 0.5f)
        {
            yield return new WaitForSeconds(time);
            TellStanceNoDelay(stance);
            //L.Log("Checked stance of " + Player.Player.channel.owner.playerID.playerName + " to " + stance.ToString() + ".", ConsoleColor.DarkRed);
            this.stance = null;
        }
        public void TellStandDelayed(float time = 0.5f)
        {
            stance = StartCoroutine(WaitToChangeStance(EPlayerStance.STAND, time));
        }
        public void CancelStance()
        {
            if (stance != null)
            {
                StopCoroutine(stance);
                stance = null;
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
                Player.Player.movement.sendPluginSpeedMultiplier(1.0f);
                Player.Player.movement.sendPluginJumpMultiplier(1.0f);
                Player.Player.life.serverSetBleeding(false);
                CancelStance();
                if (remove)
                {
                    g.ReviveManager.DownedPlayers.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
                    DeathTracker.RemovePlayerInfo(Player.Player.channel.owner.playerID.steamID.m_SteamID);
                }
            }
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
                    DamagePlayerParameters parameters = g.ReviveManager.DownedPlayers[Player.Player.channel.owner.playerID.steamID.m_SteamID].parameters;
                    parameters.damage = 100.0f;
                    parameters.respectArmor = false;
                    parameters.applyGlobalArmorMultiplier = false;
                    DamageTool.damagePlayer(parameters, out _);
                }
                g.ReviveManager.DownedPlayers.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
                DeathTracker.RemovePlayerInfo(Player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
        }
    }
    public struct DownedPlayerData
    {
        public DamagePlayerParameters parameters;
        public DateTime start;

        public DownedPlayerData(DamagePlayerParameters parameters)
        {
            this.parameters = parameters;
            start = DateTime.Now;
        }
    }
}
