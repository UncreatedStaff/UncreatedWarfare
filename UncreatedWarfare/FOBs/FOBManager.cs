using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Uncreated.Warfare.Singletons;
using Cache = Uncreated.Warfare.Components.Cache;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;
using Uncreated.Warfare.FOBs.UI;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Barricades;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Commands.CommandSystem;

namespace Uncreated.Warfare.FOBs;
[SingletonDependency(typeof(Whitelister))]
public class FOBManager : BaseSingleton, ILevelStartListener, IGameStartListener, IPlayerDisconnectListener
{
    private static FOBManager Singleton;
    private static readonly FOBConfig _config = new FOBConfig();
    public static bool Loaded => Singleton.IsLoaded();
    public static readonly FOBListUI ListUI = new FOBListUI();
    public static readonly NearbyResourceUI ResourceUI = new NearbyResourceUI();
    public readonly List<SpecialFOB> SpecialFOBs = new List<SpecialFOB>();
    public readonly List<Cache> Caches  = new List<Cache>();
    public readonly List<FOB> Team1FOBs = new List<FOB>();
    public readonly List<FOB> Team2FOBs = new List<FOB>();
    public static FOBConfigData Config => _config.Data;
    public IEnumerable<FOB> AllFOBs => Team1FOBs.Concat(Team2FOBs);
    public FOBManager() { }
    public override void Load()
    {
        EventDispatcher.OnBarricadePlaced += OnBarricadePlaced;
        EventDispatcher.OnBarricadeDestroyed += OnBarricadeDestroyed;
        EventDispatcher.OnGroupChanged += OnGroupChanged;
        Singleton = this;
    }
    public override void Unload()
    {
        Singleton = null!;
        EventDispatcher.OnGroupChanged -= OnGroupChanged;
        EventDispatcher.OnBarricadeDestroyed -= OnBarricadeDestroyed;
        EventDispatcher.OnBarricadePlaced -= OnBarricadePlaced;
        Team1FOBs.Clear();
        Team2FOBs.Clear();
        SpecialFOBs.Clear();
        Caches.Clear();
    }
    void ILevelStartListener.OnLevelReady()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (BuildableData b in Config.Buildables)
        {
            if (!Whitelister.IsWhitelisted(b.Foundation, out _))
                Whitelister.AddItem(b.Foundation);

            if (b.Emplacement != null)
            {
                if (!Whitelister.IsWhitelisted(b.Emplacement.Ammo, out _))
                    Whitelister.AddItem(b.Emplacement.Ammo);
            }
        }
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
        Team1FOBs.Clear();
        Team2FOBs.Clear();
        SpecialFOBs.Clear();
        Caches.Clear();

        SendFOBListToTeam(1);
        SendFOBListToTeam(2);
    }
    void IPlayerDisconnectListener.OnPlayerDisconnecting(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (FOB f in Team1FOBs)
        {
            if (f.FriendliesOnFOB.Remove(player))
                f.OnPlayerLeftFOB(player);
            if (f.NearbyEnemies.Remove(player))
                f.OnEnemyLeftFOB(player);
        }

        foreach (FOB f in Team2FOBs)
        {
            if (f.FriendliesOnFOB.Remove(player))
                f.OnPlayerLeftFOB(player);
            if (f.NearbyEnemies.Remove(player))
                f.OnEnemyLeftFOB(player);
        }

        foreach (Cache f in Caches)
        {
            if (f.NearbyDefenders.Remove(player))
                f.OnDefenderLeft(player);
            if (f.NearbyAttackers.Remove(player))
                f.OnAttackerLeft(player);
        }
    }
    private void OnBarricadePlaced(BarricadePlaced e)
    {
        Guid guid = e.ServersideData.barricade.asset.GUID;
        bool isRadio = Gamemode.Config.Barricades.FOBRadioGUIDs.HasValue && Gamemode.Config.Barricades.FOBRadioGUIDs.Value.Any(g => g == guid);
        BarricadeDrop drop = e.Barricade;
        BuildableData? buildable = Config.Buildables.Find(b => b.Foundation == drop.asset.GUID);
        if (buildable != null)
        {
            drop.model.gameObject.AddComponent<BuildableComponent>().Initialize(drop, buildable);
        }
        if (isRadio)
        {
            Vector3 pos = e.Transform.position;
            if (Team1FOBs.FirstOrDefault(f => f.Position == pos) is null && Team2FOBs.FirstOrDefault(f => f.Position == pos) is null)
                RegisterNewFOB(e.Barricade);
        }
        BuildableData? repairable = isRadio ? null : Config.Buildables.Find(b => b.BuildableBarricade == drop.asset.GUID || 
            (b.Type == EBuildableType.EMPLACEMENT && b.Emplacement != null && b.Emplacement.BaseBarricade == drop.asset.GUID));
        if (repairable != null || isRadio)
        {
            drop.model.gameObject.AddComponent<RepairableComponent>();
        }
        IconManager.OnBarricadePlaced(e.Barricade, isRadio);
    }
    public void Tick()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = SpecialFOBs.Count - 1; i >= 0; i--)
        {
            SpecialFOB special = SpecialFOBs[i];
            if (special.DisappearAroundEnemies)
            {
                if (Provider.clients.Count(p => p.GetTeam() != special.Team && (p.player.transform.position - special.Position).sqrMagnitude < Math.Pow(70, 2)) > 0)
                {
                    DeleteSpecialFOB(special.Name, special.Team);
                }
            }
        }
    }
    private void OnBarricadeDestroyed(BarricadeDestroyed e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (e.Transform.TryGetComponent(out BuiltBuildableComponent comp))
            UnityEngine.Object.Destroy(comp);
        if (Gamemode.Config.Barricades.FOBRadioGUIDs == null) return;
        if (Gamemode.Config.Barricades.FOBGUID.ValidReference(out Guid guid) && guid == e.ServersideData.barricade.asset.GUID)
        {
            FOB? fob = FOB.GetNearestFOB(e.ServersideData.point, EFOBRadius.SHORT, e.ServersideData.group);

            if (fob != null)
            {
                fob.UpdateBunker(null);
            }

            SendFOBListToTeam(e.ServersideData.group);
        }
        if (e.Transform.TryGetComponent(out FOBComponent f))
        {
            if (Gamemode.Config.Barricades.FOBRadioGUIDs.Value.Any(g => g == e.ServersideData.barricade.asset.GUID))
            {
                if (f.parent.IsWipedByAuthority)
                    f.parent.Destroy();
                else
                    f.parent.StartBleed();
            }
            else if (Gamemode.Config.Barricades.FOBRadioDamagedGUID.ValidReference(out guid) && guid == e.ServersideData.barricade.asset.GUID)
            {
                if (f.parent.IsBleeding)
                    f.parent.Destroy();
            }

            SendFOBListToTeam(f.parent.Team);
        }
        else if (Gamemode.Config.Barricades.InsurgencyCacheGUID.ValidReference(out guid) && guid == e.ServersideData.barricade.asset.GUID)
        {
            DeleteCache(e.Barricade);
        }
    }    
    public FOB RegisterNewFOB(BarricadeDrop drop)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FOB fob = new FOB(drop);
        
        if (fob.Owner != 0 && Data.Is(out IGameStats ws) && ws.GameStats is IFobsTracker ft)
        {
            if (fob.Owner.TryGetPlayerData(out UCPlayerData c) && c.stats is IFOBStats f)
                f.AddFOBPlaced();
            if (fob.Team == 1)
            {
                ft.FOBsPlacedT1++;
            }
            else if (fob.Team == 2)
            {
                ft.FOBsPlacedT2++;
            }
        }

        if (fob.Team == 1)
        {
            int number = 1;
            bool placed = false;
            for (int i = 0; i < Team1FOBs.Count; i++)
            {
                if (Team1FOBs[i].Number != number)
                {
                    fob.Number = number;
                    fob.Name = "FOB" + number;
                    Team1FOBs.Insert(i, fob);
                    placed = true;
                    break;
                }

                number++;
            }

            if (!placed)
            {
                fob.Number = number;
                fob.Name = "FOB" + number;
                Team1FOBs.Add(fob);
            }
        }
        else if (fob.Team == 2)
        {
            int number = 1;
            bool placed = false;
            for (int i = 0; i < Team2FOBs.Count; i++)
            {
                if (Team2FOBs[i].Number != number)
                {
                    fob.Number = number;
                    fob.Name = "FOB" + number;
                    Team2FOBs.Insert(i, fob);
                    placed = true;
                    break;
                }

                number++;
            }

            if (!placed)
            {
                fob.Number = number;
                fob.Name = "FOB" + number;
                Team2FOBs.Add(fob);
            }
        }
        UCPlayer? placer = UCPlayer.FromID(drop.GetServersideData().owner);
        if (placer != null)
        {
            if (Gamemode.Config.Barricades.FOBBaseGUID.ValidReference(out ItemBarricadeAsset fobBase))
                ItemManager.dropItem(new Item(fobBase.id, true), placer.Position, true, true, true);
            if (Gamemode.Config.Barricades.AmmoCrateBaseGUID.ValidReference(out ItemBarricadeAsset ammoBase))
                ItemManager.dropItem(new Item(ammoBase.id, true), placer.Position, true, true, true);
            QuestManager.OnFOBBuilt(placer, fob);
            Tips.TryGiveTip(placer, ETip.PLACE_BUNKER);
        }
        SendFOBListToTeam(fob.Team);
        return fob;
    }

    private const float INSIDE_FOB_RANGE_SQR = 2f * 2f;
    public static bool IsPointInFOB(Vector3 point, out FOB? fob, out SpecialFOB? specialFob)
    {
        Singleton.AssertLoaded();
        fob = null;
        specialFob = null;
        if (Singleton.SpecialFOBs != null)
        {
            for (int i = 0; i < Singleton.SpecialFOBs.Count; ++i)
            {
                if ((Singleton.SpecialFOBs[i].Position - point).sqrMagnitude <= INSIDE_FOB_RANGE_SQR)
                {
                    specialFob = Singleton.SpecialFOBs[i];
                }
            }
        }
        if (Singleton.Team1FOBs != null)
        {
            for (int i = 0; i < Singleton.Team1FOBs.Count; ++i)
            {
                BarricadeDrop? bunker = Singleton.Team1FOBs[i].Bunker;
                if (bunker != null && (bunker.model.position - point).sqrMagnitude <= INSIDE_FOB_RANGE_SQR)
                {
                    fob = Singleton.Team1FOBs[i];
                }
            }
        }
        if (Singleton.Team2FOBs != null)
        {
            for (int i = 0; i < Singleton.Team2FOBs.Count; ++i)
            {
                BarricadeDrop? bunker = Singleton.Team2FOBs[i].Bunker;
                if (bunker != null && (bunker.model.position - point).sqrMagnitude <= INSIDE_FOB_RANGE_SQR)
                {
                    fob = Singleton.Team2FOBs[i];
                }
            }
        }
        return fob is not null || specialFob is not null;
    }
    public static SpecialFOB RegisterNewSpecialFOB(string name, Vector3 point, ulong team, string UIcolor, bool disappearAroundEnemies)
    {
        Singleton.AssertLoaded();
        SpecialFOB f = new SpecialFOB(name, point, team, UIcolor, disappearAroundEnemies);
        Singleton.SpecialFOBs.Add(f);

        SendFOBListToTeam(team);
        return f;
    }
    public static Cache RegisterNewCache(BarricadeDrop drop)
    {
        Singleton.AssertLoaded();
        if (Data.Is(out Insurgency insurgency))
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            Cache cache = drop.model.gameObject.AddComponent<Cache>();

            int number;
            List<Insurgency.CacheData> caches = insurgency.ActiveCaches;
            if (caches.Count == 0)
                number = insurgency.CachesDestroyed + 1;
            else
                number = caches.Last().Number + 1;

            cache.Number = number;
            cache.Name = "CACHE" + number;

            Singleton.Caches.Add(cache);

            SendFOBListToTeam(cache.Team);

            return cache;
        }
        else
            return null!;
    }
    public static void DeleteFOB(FOB fob)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = fob.Team;

        UCPlayer? killer = UCPlayer.FromID(fob.Killer);
        ulong killerteam = 0;
        if (killer != null)
            killerteam = killer.GetTeam();

        ulong instanceID = fob.Radio.instanceID;

        FOB? removed;
        if (team == 1)
        {
            removed = Singleton.Team1FOBs.FirstOrDefault(x => x.Radio.instanceID == instanceID);
            Singleton.Team1FOBs.RemoveAll(f => f.Radio.instanceID == instanceID);
        }
        else if (team == 2)
        {
            removed = Singleton.Team2FOBs.FirstOrDefault(x => x.Radio.instanceID == instanceID);
            Singleton.Team2FOBs.RemoveAll(f => f.Radio.instanceID == instanceID);
        }
        else removed = null;

        if (removed != null)
        {
            IEnumerator<UCPlayerData> pts = Data.PlaytimeComponents.Values.GetEnumerator();
            while (pts.MoveNext())
            {
                if (pts.Current.PendingFOB is FOB f && f.Number == removed.Number)
                {
                    pts.Current.CancelTeleport();
                }
            }
            pts.Dispose();
        }

        if (!fob.IsWipedByAuthority)
        {
            if (killer != null && killerteam != 0 && killerteam != team && Data.Gamemode.State == EState.ACTIVE && Data.Is(out IGameStats w) && w.GameStats is IFobsTracker ft)
            // doesnt count destroying fobs after game ends
            {
                if (killer.Player.TryGetPlayerData(out UCPlayerData c) && c.stats is IFOBStats f)
                    f.AddFOBDestroyed();
                if (team == 1)
                {
                    ft.FOBsDestroyedT2++;
                }
                else if (team == 2)
                {
                    ft.FOBsDestroyedT1++;
                }
            }
            if (removed != null && killer != null)
            {
                if (killer.GetTeam() == team)
                {
                    Points.AwardXP(killer, Points.XPConfig.FOBTeamkilledXP, Localization.Translate("xp_fob_teamkilled", killer));
                }
                else
                {
                    Points.AwardXP(killer, Points.XPConfig.FOBKilledXP, Localization.Translate("xp_fob_killed", killer));

                    Points.TryAwardDriverAssist(killer.Player, Points.XPConfig.FOBKilledXP, 5);

                    Stats.StatsManager.ModifyStats(killer.Steam64, x => x.FobsDestroyed++, false);
                    Stats.StatsManager.ModifyTeam(team, t => t.FobsDestroyed++, false);
                }
            }
        }
        SendFOBListToTeam(team);
    }
    public static void DeleteSpecialFOB(string name, ulong team)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SpecialFOB removed = Singleton.SpecialFOBs.FirstOrDefault(x => x.Name == name && x.Team == team);
        Singleton.SpecialFOBs.Remove(removed);

        if (removed != null)
        {
            IEnumerator<UCPlayerData> pts = Data.PlaytimeComponents.Values.GetEnumerator();
            while (pts.MoveNext())
            {
                if (pts.Current.PendingFOB is SpecialFOB special)
                {
                    pts.Current.CancelTeleport();
                }
            }
        }

        SendFOBListToTeam(team);
    }
    public static void DeleteCache(BarricadeDrop cache)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.Is(out Insurgency ins)) return;

        ulong team = cache.GetServersideData().group.GetTeam();

        UCPlayer? killer = null;
        if (cache.model.TryGetComponent(out BarricadeComponent component))
            killer = UCPlayer.FromID(component.LastDamager);

        ulong instanceID = cache.instanceID;

        Cache removed = Singleton.Caches.FirstOrDefault(x => x.Structure.instanceID == instanceID);
        Singleton.Caches.RemoveAll(f => f.Structure.instanceID == instanceID);

        if (removed != null)
        {
            removed.Destroy();

            IEnumerator<UCPlayerData> pts = Data.PlaytimeComponents.Values.GetEnumerator();
            while (pts.MoveNext())
            {
                if (pts.Current.PendingFOB is FOB fob && fob.Number == removed.Number)
                {
                    pts.Current.CancelTeleport();
                }
            }
            pts.Dispose();
        }

        
        if (killer == null)
        {
             if (removed != null)
                ins.OnCacheDestroyed(removed, killer);
            return;
        }

        if (removed != null && killer != null)
        {
            if (killer.GetTeam() == team)
            {
                Points.AwardXP(killer, Points.XPConfig.FOBTeamkilledXP, Localization.Translate("xp_fob_teamkilled", killer));
            }
            else
            {
                Points.AwardXP(killer, Points.XPConfig.FOBKilledXP, Localization.Translate("xp_fob_killed", killer));
                Stats.StatsManager.ModifyStats(killer.Steam64, x => x.FobsDestroyed++, false);
                Stats.StatsManager.ModifyTeam(team, t => t.FobsDestroyed++, false);
            }
        }

        SendFOBListToTeam(team);
    }
    public static bool FindFOBByName(string name, ulong team, out object? fob)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        fob = Singleton.SpecialFOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && f.Team == team);
        if (fob != null)
            return true;

        fob = Singleton.Caches.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && f.Team == team);
        if (fob != null)
            return true;

        if (team == 1)
        {
            fob = Singleton.Team1FOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return fob != null;
        }
        else if (team == 2)
        {
            fob = Singleton.Team2FOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return fob != null;
        }
        fob = null;
        return false;
    }

    public static void UpdateFOBListForTeam(ulong team, SpecialFOB? fob = null)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (team == 0) return;
        if (!Data.Is(out TeamGamemode gm)) return;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            if (PlayerManager.OnlinePlayers[i].GetTeam() == team && !gm.JoinManager.IsInLobby(PlayerManager.OnlinePlayers[i]))
                UpdateFOBList(PlayerManager.OnlinePlayers[i], fob);
        }
    }
    public static void UpdateFOBListForTeam(ulong team, FOB? fob = null)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (team == 0) return;
        if (!Data.Is(out TeamGamemode gm)) return;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            if (PlayerManager.OnlinePlayers[i].GetTeam() == team && !gm.JoinManager.IsInLobby(PlayerManager.OnlinePlayers[i]))
                UpdateFOBList(PlayerManager.OnlinePlayers[i], fob);
        }
    }
    public static void UpdateFOBListForTeam(ulong team, Cache? fob = null)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (team == 0) return;
        if (!Data.Is(out TeamGamemode gm)) return;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            if (PlayerManager.OnlinePlayers[i].GetTeam() == team && !gm.JoinManager.IsInLobby(PlayerManager.OnlinePlayers[i]))
                UpdateFOBList(PlayerManager.OnlinePlayers[i], fob);
        }
    }
    public static void SendFOBListToTeam(ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (team == 0) return;
        if (!Data.Is(out TeamGamemode gm)) return;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            if (PlayerManager.OnlinePlayers[i].GetTeam() == team && !gm.JoinManager.IsInLobby(PlayerManager.OnlinePlayers[i]))
                SendFOBList(PlayerManager.OnlinePlayers[i]);
        }
    }

    public static void HideFOBList(UCPlayer player)
    {
        ListUI.ClearFromPlayer(player.Player.channel.owner.transportConnection);
    }
    public static void SendFOBList(UCPlayer player)
    {
        Singleton.AssertLoaded();
        List<FOB> FOBList;
        ulong team = player.GetTeam();
        if (team == 1)
            FOBList = Singleton.Team1FOBs;
        else if (team == 2)
            FOBList = Singleton.Team2FOBs;
        else return;

        UpdateUIList(team, player.Connection, FOBList, player);
    }
    private void OnGroupChanged(GroupChanged e)
    {
        if (e.NewGroup.GetTeam() is > 0 and < 3)
            SendFOBList(e.Player);
    }
    public static void UpdateResourceUIString(FOB fob)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.Is(out TeamGamemode gm)) return;
        if (fob.IsBleeding) return;

        List<FOB> FOBList;
        ulong team = fob.Team;
        if (team == 1)
            FOBList = Singleton.Team1FOBs;
        else if (team == 2)
            FOBList = Singleton.Team2FOBs;
        else return;

        int offset = 0;
        for (int b = 0; b < Singleton.SpecialFOBs.Count; b++)
            if (Singleton.SpecialFOBs[b].Team == team)
                offset++;
        for (int b = 0; b < Singleton.Caches.Count; b++)
            if (Singleton.Caches[b].Team == team)
                offset++;
        int i = FOBList.IndexOf(fob);
        if (i == -1)
            return;
        i += offset;

        for (int j = 0; j < PlayerManager.OnlinePlayers.Count; j++)
        {
            if (PlayerManager.OnlinePlayers[j].GetTeam() == team && (!JoinManager.Loaded || !gm.JoinManager.IsInLobby(PlayerManager.OnlinePlayers[j])))
            {
                ListUI.FOBResources[i].SetText(PlayerManager.OnlinePlayers[j].Connection, fob.UIResourceString);
            }
        }
    }
    public static void UpdateFOBList(UCPlayer player, FOB? fob = null)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<FOB> FOBList;
        ulong team = player.GetTeam();
        if (team == 1)
            FOBList = Singleton.Team1FOBs;
        else if (team == 2)
            FOBList = Singleton.Team2FOBs;
        else return;

        if (fob == null)
        {
            UpdateUIList(team, player.Connection, FOBList, player);
        }
        else
        {
            int offset = 0;
            for (int b = 0; b < Singleton.SpecialFOBs.Count; b++)
                if (Singleton.SpecialFOBs[b].Team == team)
                    offset++;
            for (int b = 0; b < Singleton.Caches.Count; b++)
                if (Singleton.Caches[b].Team == team)
                    offset++;
            int i = FOBList.IndexOf(fob);
            if (i == -1)
            {
                UpdateUIList(team, player.Connection, FOBList, player);
                return;
            }
            int ii = i + offset;
            if (ListUI.FOBNames.Length > ii)
            {
                ListUI.FOBNames[ii].SetText(player.Connection, Localization.Translate("fob_ui", player.Steam64, FOBList[i].Name.Colorize(FOBList[i].UIColor), FOBList[i].GridLocation.ToString().Colorize("ebe8df"), FOBList[i].ClosestLocation));
                ListUI.FOBResources[ii].SetText(player.Connection, FOBList[i].UIResourceString);
            }
        }
    }
    public static void UpdateFOBList(UCPlayer player, SpecialFOB? fob = null)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<FOB> FOBList;
        ulong team = player.GetTeam();
        if (team == 1)
            FOBList = Singleton.Team1FOBs;
        else if (team == 2)
            FOBList = Singleton.Team2FOBs;
        else return;
        ITransportConnection c = player.Player.channel.owner.transportConnection;
        if (fob == null)
        {
            UpdateUIList(team, c, FOBList, player);
        }
        else
        {
            int i = Singleton.SpecialFOBs.IndexOf(fob);
            if (i == -1)
            {
                UpdateUIList(team, c, FOBList, player);
                return;
            }
            if (ListUI.FOBNames.Length > i)
            {
                ListUI.FOBNames[i].SetText(player.Connection, Localization.Translate("fob_ui", player.Steam64,
                    Singleton.SpecialFOBs[i].Name.Colorize(Singleton.SpecialFOBs[i].UIColor),
                    Singleton.SpecialFOBs[i].GridLocation.ToString(), Singleton.SpecialFOBs[i].ClosestLocation));
            }
        }
    }
    public static void UpdateFOBList(UCPlayer player, Cache? cache = null)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<FOB> FOBList;
        ulong team = player.GetTeam();
        if (team == 1)
            FOBList = Singleton.Team1FOBs;
        else if (team == 2)
            FOBList = Singleton.Team2FOBs;
        else return;
        ITransportConnection c = player.Player.channel.owner.transportConnection;
        if (cache == null)
        {
            UpdateUIList(team, c, FOBList, player);
        }
        else
        {
            int offset = 0;
            for (int b = 0; b < Singleton.SpecialFOBs.Count; b++)
                if (Singleton.SpecialFOBs[b].Team == team)
                    offset++;
            int i = Singleton.Caches.IndexOf(cache);
            if (i == -1)
            {
                UpdateUIList(team, c, FOBList, player);
                return;
            }
            int ii = i + offset;
            if (ListUI.FOBNames.Length > ii)
            {
                ListUI.FOBNames[ii].SetText(player.Connection, Localization.Translate("fob_ui", player.Steam64,
                    Singleton.Caches[i].Name.Colorize(Singleton.Caches[i].UIColor),
                    Singleton.Caches[i].GridLocation.ToString(),
                    Singleton.Caches[i].ClosestLocation));
            }
        }
    }
    private static void UpdateUIList(ulong team, ITransportConnection connection, List<FOB> fobs, UCPlayer player)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ListUI.SendToPlayer(connection);

        int i2 = 0;
        FOBConfigData config = Config;
        int min = Math.Min(Singleton.SpecialFOBs.Count, ListUI.FOBParents.Length);
        for (int i = 0; i < min; i++)
        {
            if (Singleton.SpecialFOBs[i].IsActive && Singleton.SpecialFOBs[i].Team == team)
            {
                ListUI.FOBParents[i2].SetVisibility(connection, true);
                ListUI.FOBNames[i2].SetText(connection, Localization.Translate("fob_ui", player.Steam64,
                    Singleton.SpecialFOBs[i].Name.Colorize(Singleton.SpecialFOBs[i].UIColor),
                    Singleton.SpecialFOBs[i].GridLocation.ToString(),
                    Singleton.SpecialFOBs[i].ClosestLocation));
                i2++;
            }
        }

        if (Data.Is(out Insurgency ins) && team == ins.DefendingTeam)
        {
            min = Math.Min(Singleton.Caches.Count, ListUI.FOBParents.Length);
            for (int i = 0; i < min; i++)
            {
                ListUI.FOBParents[i2].SetVisibility(connection, true);
                ListUI.FOBNames[i2].SetText(connection, Localization.Translate("fob_ui", player.Steam64,
                    Singleton.Caches[i].Name.Colorize(Singleton.Caches[i].UIColor),
                    Singleton.Caches[i].GridLocation.ToString(),
                    Singleton.Caches[i].ClosestLocation));
                ListUI.FOBResources[i2].SetText(connection, string.Empty);
                i2++;
            }
        }

        min = Math.Min(fobs.Count, ListUI.FOBParents.Length - i2);
        for (int i = 0; i < min; i++)
        {
            ListUI.FOBParents[i2].SetVisibility(connection, true);
            ListUI.FOBNames[i2].SetText(connection, Localization.Translate("fob_ui", player.Steam64,
                fobs[i].Name.Colorize(fobs[i].UIColor),
                fobs[i].GridLocation.ToString(),
                fobs[i].ClosestLocation));
            ListUI.FOBResources[i2].SetText(connection, fobs[i].UIResourceString);
            i2++;
        }
        for (; i2 < ListUI.FOBParents.Length; i2++)
        {
            ListUI.FOBParents[i2].SetVisibility(connection, false);
        }
    }
}

public class SpecialFOB : IFOB, IDeployable
{
    private readonly string _name;
    private readonly string _cl;
    private readonly GridLocation _gc;
    private readonly Vector3 _pos;
    public ulong Team;
    public string UIColor;
    public bool IsActive;
    public bool DisappearAroundEnemies;
    public string Name => _name;
    public Vector3 Position => _pos;
    float IDeployable.Yaw => 0f;
    public string ClosestLocation => _cl;
    public GridLocation GridLocation => _gc;

    public SpecialFOB(string name, Vector3 point, ulong team, string color, bool disappearAroundEnemies)
    {
        _name = name;
        _cl = F.GetClosestLocation(point);

        if (Data.Is(out IFlagRotation fg))
        {
            Flag flag = fg.LoadedFlags.Find(f => f.Name.Equals(ClosestLocation, StringComparison.OrdinalIgnoreCase));
            if (flag is not null)
                _cl = flag.ShortName;
        }

        Team = team;
        _pos = point;
        _gc = new GridLocation(in point);
        UIColor = color;
        IsActive = true;
        DisappearAroundEnemies = disappearAroundEnemies;
    }

    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is not null && format.Equals(FOB.COLORED_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(UIColor ?? TeamManager.GetTeamHexColor(Team), Name, flags);
        return Name;
    }
    bool IDeployable.CheckDeployable(UCPlayer player, CommandInteraction? ctx)
    {
        if (IsActive)
            return true;
        if (ctx is not null)
            throw ctx.Reply("deploy_c_notactive");
        return false;
    }
    bool IDeployable.CheckDeployableTick(UCPlayer player, bool chat)
    {
        if (IsActive)
            return true;
        if (chat)
            player.SendChat("deploy_c_notactive");
        return false;
    }
    void IDeployable.OnDeploy(UCPlayer player, bool chat)
    {
        ActionLogger.Add(EActionLogType.DEPLOY_TO_LOCATION, "SPECIAL FOB " + Name + " TEAM " + TeamManager.TranslateName(Team, 0), player);
        if (chat)
            player.Message("deploy_s", UIColor, Name);
    }
}
[JsonSerializable(typeof(FOBConfigData))]
public class FOBConfigData : ConfigData
{
    public JsonAssetReference<VehicleAsset> MortarBase;
    public float FOBMaxHeightAboveTerrain;
    public bool RestrictFOBPlacement;
    public ushort FOBID;
    public ushort FOBRequiredBuild;
    public int FOBBuildPickupRadius;
    public byte FobLimit;

    public float AmmoCommandCooldown;
    public ushort AmmoCrateRequiredBuild;
    public ushort RepairStationRequiredBuild;

    public List<BuildableData> Buildables;
    public JsonAssetReference<VehicleAsset>[] LogiTruckIDs;
    public int AmmoBagMaxUses;

    public float DeloyMainDelay;
    public float DeloyFOBDelay;

    public bool EnableCombatLogger;
    public uint CombatCooldown;

    public bool EnableDeployCooldown;
    public uint DeployCooldown;
    public bool DeployCancelOnMove;
    public bool DeployCancelOnDamage;

    public bool ShouldRespawnAtMain;
    public bool ShouldWipeAllFOBsOnRoundedEnded;
    public bool ShouldSendPlayersBackToMainOnRoundEnded;
    public bool ShouldKillMaincampers;

    public ushort FirstFOBUiId;
    public ushort BuildResourceUI;

    public string T1RadioState;
    public string T2RadioState;

    public override void SetDefaults()
    {
        FOBMaxHeightAboveTerrain = 25f;
        RestrictFOBPlacement = true;
        FOBRequiredBuild = 15;
        FOBBuildPickupRadius = 80;
        FobLimit = 10;

        AmmoCrateRequiredBuild = 2;
        AmmoCommandCooldown = 120f;

        RepairStationRequiredBuild = 6;

        T1RadioState = "8yh8NQEAEAEAAAAAAAAAACIAAACmlQFkAAQAAKyVAWQAAAIArpUBZAAABQDGlQFkAAMFAMmVAWQABgUAyZUBZAAACACmZQFkAAMIAMCVAWQABggAwJUBZAAHAgDWlQFkAAoCANaVAWQABwMA1pUBZAAKAwDWlQFkAAcEANaVAWQACgQA1pUBZAAJBQDYlQFkAAkHANiVAWQACQkA2JUBZAAACwDOlQFkAAAMAM6VAWQAAA0AzpUBZAADCwDOlQFkAAcAAKyVAWQACgAA1pUBZAAKAQDWlQFkAAMMAM6VAWQAAw0AzpUBZAAGDQDQlQFkAAQCANqVAWQACQsA0JUBZAAJDADQlQFkAAkNANCVAWQABgsAzpUBZAAGDADOlQFkAA==";
        T2RadioState = "8yh8NQEAEAEAAAAAAAAAACIAAACmlQFkAAQAAKyVAWQAAAIArpUBZAADCADAlQFkAAYIAMCVAWQABwIA1pUBZAAKAgDWlQFkAAcDANaVAWQACgMA1pUBZAAHBADWlQFkAAoEANaVAWQACQUA2JUBZAAJBwDYlQFkAAkJANiVAWQAAAsAzpUBZAAADADOlQFkAAANAM6VAWQAAwsAzpUBZAAHAACslQFkAAoAANaVAWQACgEA1pUBZAADDADOlQFkAAMNAM6VAWQABg0A0JUBZAAEAgDalQFkAAkLANCVAWQACQwA0JUBZAAJDQDQlQFkAAYLAM6VAWQABgwAzpUBZAAABQDDlQFkAAMFAMqVAWQABgUAypUBZAAACAC6ZQFkAA==";

        LogiTruckIDs = new JsonAssetReference<VehicleAsset>[6]
        { 
            "58d6410084f04e43ba4462a1c9a6b8c0", // Logistics_Woodlands
            "fe1a85aeb8e34c2fbeca3e485300a61c", // Logistics_Forest
            "6082d95b5fcb4805a7a2120e3e3c6f68", // UH60_Blackhawk
            "18a6b283dbd245d0a13e0daa09b84aed", // Mi8
            "855859643f3c49a088a85be7260a5226", // Mi8
            "5613d32e8e194b3caf44aa16c2e19456"  // Mi8
        };
        AmmoBagMaxUses = 3;

        MortarBase = "6ff4826eaeb14c7cac1cf25a55d24bd3";

        Buildables = new List<BuildableData>()
        {
            new BuildableData
            {
                BuildableBarricade = "61c349f10000498fa2b92c029d38e523",
                Foundation = "1bb17277dd8148df9f4c53d1a19b2503",
                Type = EBuildableType.FOB_BUNKER,
                RequiredHits = 30,
                RequiredBuild = 15,
                Team = 0,
                Emplacement = null
            },
            new BuildableData
            {
                BuildableBarricade = "6fe208519d7c45b0be38273118eea7fd",
                Foundation = "eccfe06e53d041d5b83c614ffa62ee59",
                Type = EBuildableType.AMMO_CRATE,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Emplacement = null
            },
            new BuildableData
            {
                BuildableBarricade = "c0d11e0666694ddea667377b4c0580be",
                Foundation = "26a6b91cd1944730a0f28e5f299cebf9",
                Type = EBuildableType.REPAIR_STATION,
                RequiredHits = 25,
                RequiredBuild = 15,
                Team = 0,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag line
                BuildableBarricade = "ab702192eab4456ebb9f6d7cc74d4ba2",
                Foundation = "15f674dcaf3f44e19a124c8bf7e19ca2",
                Type = EBuildableType.FORTIFICATION,
                RequiredHits = 8,
                RequiredBuild = 1,
                Team = 0,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag pillbox
                BuildableBarricade = "f3bd9ee2fa334faabc8fd9d5a3b84424",
                Foundation = "a9294335d8e84b76b1cbcb7d70f66aaa",
                Type = EBuildableType.FORTIFICATION,
                RequiredHits = 8,
                RequiredBuild = 1,
                Team = 0,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag crescent
                BuildableBarricade = "eefee76f077349e58359f5fd03cf311d",
                Foundation = "920f8b30ae314406ab032a0c2efa753d",
                Type = EBuildableType.FORTIFICATION,
                RequiredHits = 8,
                RequiredBuild = 1,
                Team = 0,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag foxhole
                BuildableBarricade = "a71e3e3d6bb54a36b7bd8bf5f25160aa",
                Foundation = "12ea830dd9ab4f949893bbbbc5e9a5f6",
                Type = EBuildableType.FORTIFICATION,
                RequiredHits = 12,
                RequiredBuild = 2,
                Team = 0,
                Emplacement = null
            },
            new BuildableData
            {
                // razorwire
                BuildableBarricade = "bc24bd85ff714ff7bb2f8b2dd5056395",
                Foundation = "a2a8a01a58454816a6c9a047df0558ad",
                Type = EBuildableType.FORTIFICATION,
                RequiredHits = 8,
                RequiredBuild = 1,
                Team = 0,
                Emplacement = null
            },
            new BuildableData
            {
                // hesco wall
                BuildableBarricade = "e1af3a3af31e4996bc5d6ffd9a0773ec",
                Foundation = "baf23a8b514441ee8db891a3ddf32ef4",
                Type = EBuildableType.FORTIFICATION,
                RequiredHits = 20,
                RequiredBuild = 1,
                Team = 0,
                Emplacement = null
            },
            new BuildableData
            {
                // hesco tower
                BuildableBarricade = "857c85161f254964a921700a69e215a9",
                Foundation = "827d0ca8bfff43a39f750f191e16ea71",
                Type = EBuildableType.FORTIFICATION,
                RequiredHits = 15,
                RequiredBuild = 1,
                Team = 0,
                Emplacement = null
            },
            new BuildableData
            {
                // M2A1
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = "80396c361d3040d7beb3921964ec2997",
                Type = EBuildableType.EMPLACEMENT,
                RequiredHits = 10,
                RequiredBuild = 6,
                Team = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = "aa3c6af4911243b5b5c9dc95ca1263bf",
                    BaseBarricade =  new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = "523c49ce4df44d46ba37be0dd6b4504b",
                    AmmoCount = 2,
                    MaxFobCapacity = 2
                }
            },
            new BuildableData
            {
                // Kord
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = "e44ba62f763c432e882ddc7eabaa9c77",
                Type = EBuildableType.EMPLACEMENT,
                RequiredHits = 10,
                RequiredBuild = 6,
                Team = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = "86cfe1eb8be144aeae7659c9c74ff11a",
                    BaseBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = "6e9bc2083a1246b49b1656c2ec6f535a",
                    AmmoCount = 2,
                    MaxFobCapacity = 2
                }
            },
            new BuildableData
            {
                // TOW
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = "a68ae466fb804829a0eb0d4556071801",
                Type = EBuildableType.EMPLACEMENT,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = "9d305050a6a142349376d6c49fb38362",
                    BaseBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = "3128a69d06ac4bbbbfddc992aa7185a6",
                    AmmoCount = 1,
                    MaxFobCapacity = 1
                }
            },
            new BuildableData
            {
                // Kornet
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = "37811b1847744c958fcb30a0b759874b",
                Type = EBuildableType.EMPLACEMENT,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = "677b1084-dffa-4633-84d2-9167a3fae25b",
                    BaseBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = "d7774b017c404adbb0a0fe8e902b9689",
                    AmmoCount = 1,
                    MaxFobCapacity = 1
                }
            },
            new BuildableData
            {
                // Stinger
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = "3c2dd7febc854b7f8859852b8c736c8e",
                Type = EBuildableType.EMPLACEMENT,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = "1883345cbdad40aa81e49c84e6c872ef",
                    BaseBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = "3c0a94af5af24901a9e3207f3e9ed0ba",
                    AmmoCount = 1,
                    MaxFobCapacity = 1
                }
            },
            new BuildableData
            {
                // Igla
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = "b50cb548734946ffa5f88d6691a2c7ce",
                Type = EBuildableType.EMPLACEMENT,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = "8add59a2e2b94f93ab0d6b727d310097",
                    BaseBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = "a54d571983c2432a9624eec39d602997",
                    AmmoCount = 1,
                    MaxFobCapacity = 1
                }
            },
            new BuildableData
            {
                // Mortar
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = "6ff4826eaeb14c7cac1cf25a55d24bd3",
                Type = EBuildableType.EMPLACEMENT,
                RequiredHits = 22,
                RequiredBuild = 10,
                Team = 0,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = "94bf8feb05bc4680ac26464bc175460c",
                    BaseBarricade = "c3eb4dd3fd1d463993ec69c4c3de50d7", // Mortar
                    Ammo = "66f4c76a119e4d6ca9d0b1a866c4d901",
                    AmmoCount = 3,
                    MaxFobCapacity = 2,
                    ShouldWarnFriendliesIncoming = true
                }
            },
        };

        DeloyMainDelay = 3;
        DeloyFOBDelay = 5;

        DeployCancelOnMove = true;
        DeployCancelOnDamage = true;

        ShouldRespawnAtMain = true;
        ShouldSendPlayersBackToMainOnRoundEnded = true;
        ShouldWipeAllFOBsOnRoundedEnded = true;
        ShouldKillMaincampers = true;
    }

    public FOBConfigData() { }
}

[JsonSerializable(typeof(BuildableData))]
public class BuildableData : ITranslationArgument
{
    [JsonPropertyName("foundationID")]
    public JsonAssetReference<ItemBarricadeAsset> Foundation;
    [JsonPropertyName("structureID")]
    public JsonAssetReference<ItemBarricadeAsset> BuildableBarricade;
    [JsonPropertyName("type")]
    public EBuildableType Type;
    [JsonPropertyName("requiredHits")]
    public int RequiredHits;
    [JsonPropertyName("requiredBuild")]
    public int RequiredBuild;
    [JsonPropertyName("team")]
    public int Team;
    [JsonPropertyName("emplacementData")]
    public EmplacementData? Emplacement;

    public string Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        ItemBarricadeAsset asset;
        if (Emplacement is not null)
        {
            if (Emplacement.EmplacementVehicle.ValidReference(out VehicleAsset vasset))
                return vasset.vehicleName;
            if (Emplacement.BaseBarricade.ValidReference(out asset))
                return asset.itemName;
            if (Emplacement.Ammo.ValidReference(out ItemAsset iasset))
                return iasset.itemName;
        }

        if (BuildableBarricade.ValidReference(out asset) || Foundation.ValidReference(out asset))
            return asset.itemName;

        return Type.ToString();
    }
}

[JsonSerializable(typeof(EmplacementData))]
public class EmplacementData
{
    [JsonPropertyName("vehicleID")]
    public JsonAssetReference<VehicleAsset> EmplacementVehicle;
    [JsonPropertyName("baseID")]
    public JsonAssetReference<ItemBarricadeAsset> BaseBarricade;
    [JsonPropertyName("ammoID")]
    public JsonAssetReference<ItemAsset> Ammo;
    [JsonPropertyName("ammoAmount")]
    public int AmmoCount;
    [JsonPropertyName("allowedPerFob")]
    public int MaxFobCapacity;
    [JsonPropertyName("warnFriendlyProjectiles")]
    public bool ShouldWarnFriendliesIncoming = false;
}

[Translatable("Buildable Type")]
public enum EBuildableType
{
    [Translatable("Bunker")]
    FOB_BUNKER,
    AMMO_CRATE,
    REPAIR_STATION,
    FORTIFICATION,
    EMPLACEMENT,
    RADIO
}

[Flags]
public enum EFOBStatus : byte
{
    RADIO = 0,
    AMMO_CRATE = 1,
    REPAIR_STATION = 2,
    HAB = 4
}