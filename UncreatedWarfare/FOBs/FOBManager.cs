using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs.UI;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Cache = Uncreated.Warfare.Components.Cache;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.FOBs;
[SingletonDependency(typeof(Whitelister))]
public class FOBManager : BaseSingleton, ILevelStartListener, IGameStartListener, IPlayerDisconnectListener, IGameTickListener, IJoinedTeamListener, IUIListener
{
    private const float InsideFOBRangeSqr = 2f * 2f;

    private static FOBManager _singleton;
    private static readonly FOBConfig ConfigFile = new FOBConfig();
    public static bool Loaded => _singleton.IsLoaded();
    public static readonly FOBListUI ListUI = new FOBListUI();
    public static readonly NearbyResourceUI ResourceUI = new NearbyResourceUI();
    public readonly List<SpecialFOB> SpecialFOBs = new List<SpecialFOB>();
    public readonly List<Cache> Caches = new List<Cache>();
    public readonly List<FOB> Team1FOBs = new List<FOB>();
    public readonly List<FOB> Team2FOBs = new List<FOB>();
    public static FOBConfigData Config => ConfigFile.Data;
    public IEnumerable<FOB> AllFOBs => Team1FOBs.Concat(Team2FOBs);

    public override void Load()
    {
        EventDispatcher.BarricadePlaced += OnBarricadePlaced;
        EventDispatcher.BarricadeDestroyed += OnBarricadeDestroyed;
        EventDispatcher.GroupChanged += OnGroupChanged;
        _singleton = this;
    }
    public override void Unload()
    {
        _singleton = null!;
        EventDispatcher.GroupChanged -= OnGroupChanged;
        EventDispatcher.BarricadeDestroyed -= OnBarricadeDestroyed;
        EventDispatcher.BarricadePlaced -= OnBarricadePlaced;
        Team1FOBs.Clear();
        Team2FOBs.Clear();
        SpecialFOBs.Clear();
        Caches.Clear();
    }
    void IJoinedTeamListener.OnJoinTeam(UCPlayer player, ulong team) => SendFOBList(player);
    void ILevelStartListener.OnLevelReady()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (BuildableData b in Config.Buildables)
        {
            if (b.Foundation.ValidReference(out Guid guid) && !Whitelister.IsWhitelisted(guid, out _))
                Whitelister.AddItem(guid);

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
        ulong team = e.GroupID.GetTeam();
        FactionInfo? info = TeamManager.GetFactionSafe(team);
        bool isRadio = info != null && info.FOBRadio.MatchGuid(guid);
        BarricadeDrop drop = e.Barricade;
        BuildableData? buildable = Config.Buildables.Find(b => b.Foundation.MatchGuid(guid));
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
        BuildableData? repairable = isRadio ? null : Config.Buildables.Find(b => b.BuildableBarricade != null && b.BuildableBarricade.HasValue && b.BuildableBarricade.Value.MatchGuid(guid) ||
            (b.Type == BuildableType.Emplacement && b.Emplacement != null && b.Emplacement.BaseBarricade.MatchGuid(guid)));
        if (repairable != null || isRadio)
        {
            drop.model.gameObject.AddComponent<RepairableComponent>();
        }
        IconManager.OnBarricadePlaced(e.Barricade, isRadio);
    }
    void IGameTickListener.Tick()
    {
        if (Data.Gamemode.EveryXSeconds(5f))
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
    }
    private void OnBarricadeDestroyed(BarricadeDestroyed e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (e.Transform.TryGetComponent(out BuiltBuildableComponent comp))
            UnityEngine.Object.Destroy(comp);
        if (Gamemode.Config.BarricadeFOBBunker.ValidReference(out Guid guid) && guid == e.ServersideData.barricade.asset.GUID)
        {
            FOB.GetNearestFOB(e.ServersideData.point, EfobRadius.SHORT, e.ServersideData.group.GetTeam())?.UpdateBunker(null);
        }
        if (e.Transform.TryGetComponent(out FOBComponent f) && f.Parent != null)
        {
            ulong team = e.ServersideData.group.GetTeam();
            FactionInfo? info = TeamManager.GetFactionSafe(team);
            if (info is not null)
            {
                if (info.FOBRadio.ValidReference(out guid) && guid == e.ServersideData.barricade.asset.GUID)
                {
                    if (f.Parent.IsWipedByAuthority)
                        f.Parent.Destroy();
                    else
                        f.Parent.StartBleed();
                }
                else if (Gamemode.Config.BarricadeFOBRadioDamaged.ValidReference(out guid) && guid == e.ServersideData.barricade.asset.GUID)
                {
                    if (f.Parent.IsBleeding)
                        f.Parent.Destroy();
                }
                else return;

                SendFOBListToTeam(f.Parent.Team);
            }
        }
        else if (Gamemode.Config.BarricadeInsurgencyCache.ValidReference(out guid) && guid == e.ServersideData.barricade.asset.GUID && Data.Is<Insurgency>())
        {
            DeleteCache(e.Barricade);
        }
    }
    public FOB RegisterNewFOB(BarricadeDrop drop)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FOB fob = new FOB(drop);

        if (fob.Owner != 0 && Data.Is(out IGameStats ws) && ws.GameStats is IFobsTracker ft)
        {
            if (fob.Owner.TryGetPlayerData(out UCPlayerData c) && c.Stats is IFOBStats f)
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
            if (Gamemode.Config.BarricadeFOBBunkerBase.ValidReference(out ItemBarricadeAsset fobBase))
                ItemManager.dropItem(new Item(fobBase.id, true), placer.Position, true, true, true);
            if (Gamemode.Config.BarricadeAmmoCrateBase.ValidReference(out ItemBarricadeAsset ammoBase))
                ItemManager.dropItem(new Item(ammoBase.id, true), placer.Position, true, true, true);
            QuestManager.OnFOBBuilt(placer, fob);
            Tips.TryGiveTip(placer, 3, T.TipPlaceBunker);
        }
        SendFOBListToTeam(fob.Team);
        return fob;
    }

    public static bool IsPointInFOB(Vector3 point, out FOB? fob, out SpecialFOB? specialFob)
    {
        _singleton.AssertLoaded();
        fob = null;
        specialFob = null;
        if (_singleton.SpecialFOBs != null)
        {
            for (int i = 0; i < _singleton.SpecialFOBs.Count; ++i)
            {
                if ((_singleton.SpecialFOBs[i].Position - point).sqrMagnitude <= InsideFOBRangeSqr)
                {
                    specialFob = _singleton.SpecialFOBs[i];
                }
            }
        }
        if (_singleton.Team1FOBs != null)
        {
            for (int i = 0; i < _singleton.Team1FOBs.Count; ++i)
            {
                BarricadeDrop? bunker = _singleton.Team1FOBs[i].Bunker;
                if (bunker != null && (bunker.model.position - point).sqrMagnitude <= InsideFOBRangeSqr)
                {
                    fob = _singleton.Team1FOBs[i];
                }
            }
        }
        if (_singleton.Team2FOBs != null)
        {
            for (int i = 0; i < _singleton.Team2FOBs.Count; ++i)
            {
                BarricadeDrop? bunker = _singleton.Team2FOBs[i].Bunker;
                if (bunker != null && (bunker.model.position - point).sqrMagnitude <= InsideFOBRangeSqr)
                {
                    fob = _singleton.Team2FOBs[i];
                }
            }
        }
        return fob is not null || specialFob is not null;
    }
    public static SpecialFOB RegisterNewSpecialFOB(string name, Vector3 point, ulong team, string color, bool disappearAroundEnemies)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
        SpecialFOB f = new SpecialFOB(name, point, team, color, disappearAroundEnemies);
        _singleton.SpecialFOBs.Add(f);

        SendFOBListToTeam(team);
        return f;
    }
    public static Cache RegisterNewCache(BarricadeDrop drop)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
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

            _singleton.Caches.Add(cache);

            SendFOBListToTeam(cache.Team);

            return cache;
        }
        else
            return null!;
    }
    public static void DeleteFOB(FOB fob)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
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
            removed = _singleton.Team1FOBs.FirstOrDefault(x => x.Radio.instanceID == instanceID);
            _singleton.Team1FOBs.RemoveAll(f => f.Radio.instanceID == instanceID);
        }
        else if (team == 2)
        {
            removed = _singleton.Team2FOBs.FirstOrDefault(x => x.Radio.instanceID == instanceID);
            _singleton.Team2FOBs.RemoveAll(f => f.Radio.instanceID == instanceID);
        }
        else removed = null;

        if (removed != null)
            Deployment.CancelDeploymentsTo(removed);

        if (!fob.IsWipedByAuthority)
        {
            if (killer != null && killerteam != 0 && killerteam != team && Data.Gamemode.State == State.Active && Data.Is(out IGameStats w) && w.GameStats is IFobsTracker ft)
            // doesnt count destroying fobs after game ends
            {
                if (killer.Player.TryGetPlayerData(out UCPlayerData c) && c.Stats is IFOBStats f)
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
                if (Data.Is(out ITickets tickets))
                {
                    if (team == 1) tickets.TicketManager.Team1Tickets += Config.TicketsFOBRadioLost;
                    else if (team == 2) tickets.TicketManager.Team2Tickets += Config.TicketsFOBRadioLost;
                }

                if (killer.GetTeam() == team)
                {
                    Points.AwardXP(killer, Points.XPConfig.FOBTeamkilledXP, T.XPToastFriendlyFOBDestroyed);
                }
                else
                {
                    Points.AwardXP(killer, Points.XPConfig.FOBKilledXP, T.XPToastFOBDestroyed);

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
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SpecialFOB? removed = _singleton.SpecialFOBs.FirstOrDefault(x => x.Name == name && x.Team == team);
        if (removed == null)
            return;
        _singleton.SpecialFOBs.Remove(removed);

        Deployment.CancelDeploymentsTo(removed);

        SendFOBListToTeam(team);
    }

    public static void DeleteCache(BarricadeDrop cache)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.Is(out Insurgency ins)) return;

        ulong team = cache.GetServersideData().group.GetTeam();

        UCPlayer? killer = null;
        if (cache.model.TryGetComponent(out BarricadeComponent component))
            killer = UCPlayer.FromID(component.LastDamager);

        ulong instanceID = cache.instanceID;

        Cache? removed = _singleton.Caches.FirstOrDefault(x => x.Structure.instanceID == instanceID);
        _singleton.Caches.RemoveAll(f => f.Structure.instanceID == instanceID);

        if (removed != null)
        {
            removed.Destroy();

            Deployment.CancelDeploymentsTo(removed);
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
                Points.AwardXP(killer, Points.XPConfig.FOBTeamkilledXP, T.XPToastFriendlyFOBDestroyed);
            }
            else
            {
                Points.AwardXP(killer, Points.XPConfig.FOBKilledXP, T.XPToastFOBDestroyed);
                Stats.StatsManager.ModifyStats(killer.Steam64, x => x.FobsDestroyed++, false);
                Stats.StatsManager.ModifyTeam(team, t => t.FobsDestroyed++, false);
            }
        }

        SendFOBListToTeam(team);
    }
    public static bool TryFindFOB(string name, ulong team, out IDeployable fob)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (team is 1 or 2)
        {
            fob = int.TryParse(name, NumberStyles.Number, Data.LocalLocale, out int fobNumber)
                ? (team == 1 ? _singleton.Team1FOBs : _singleton.Team2FOBs).Find(x => x.Number == fobNumber)
                : (team == 1 ? _singleton.Team1FOBs : _singleton.Team2FOBs).Find(f => f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (fob != null)
                return true;
        }

        fob = _singleton.SpecialFOBs.Find(f => f.Team == team && f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        if (fob != null)
            return true;

        fob = _singleton.Caches.Find(f => f.Team == team && f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        if (name.Equals("cache", StringComparison.InvariantCultureIgnoreCase) && _singleton.Caches.Count == 1)
            fob = _singleton.Caches[0];
        return fob != null;
    }

    public static void UpdateFOBListForTeam(ulong team, SpecialFOB? fob = null)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (fob != null)
        {
            int offset = -1, index = -1;
            for (int b = 0; b < _singleton.SpecialFOBs.Count; b++)
            {
                SpecialFOB specFob = _singleton.SpecialFOBs[b];
                if (specFob == fob)
                {
                    ++offset;
                    index = b;
                    break;
                }
                if (specFob.Team == team)
                    ++offset;
            }
            if (index != -1 && ListUI.FOBNames.Length > offset)
            {
                SpecialFOB spf = _singleton.SpecialFOBs[index];
                foreach (LanguageSet set in LanguageSet.OnTeam(team))
                {
                    string txt = T.FOBUI.Translate(set.Language, spf, spf.GridLocation, spf.ClosestLocation, null, set.Team);
                    while (set.MoveNext())
                    {
                        if (set.Next.HasUIHidden) continue;
                        ListUI.FOBNames[offset].SetText(set.Next.Connection, txt);
                    }
                }
                return;
            }
        }
        SendFOBListToTeam(team);
    }
    public static void UpdateFOBListForTeam(ulong team, FOB? fob = null)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<FOB> list;
        if (team == 1)
            list = _singleton.Team1FOBs;
        else if (team == 2)
            list = _singleton.Team2FOBs;
        else return;

        if (fob != null)
        {
            int offset = 0;
            for (int b = 0; b < _singleton.SpecialFOBs.Count; b++)
                if (_singleton.SpecialFOBs[b].Team == team)
                    offset++;
            for (int b = 0; b < _singleton.Caches.Count; b++)
                if (_singleton.Caches[b].Team == team)
                    offset++;
            int i = list.IndexOf(fob);
            int ii = i + offset;
            if (i != -1 && ListUI.FOBNames.Length > ii)
            {
                FOB f = list[i];
                foreach (LanguageSet set in LanguageSet.OnTeam(team))
                {
                    string txt = T.FOBUI.Translate(set.Language, f, f.GridLocation, f.ClosestLocation, null, set.Team);
                    L.LogDebug(txt);
                    while (set.MoveNext())
                    {
                        if (set.Next.HasUIHidden) continue;
                        ListUI.FOBNames[ii].SetText(set.Next.Connection, txt);
                        ListUI.FOBResources[ii].SetText(set.Next.Connection, f.UIResourceString);
                    }
                }
                return;
            }
        }
        SendFOBListToTeam(team);
    }
    public static void UpdateFOBListForTeam(ulong team, Cache? cache = null)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (cache != null)
        {
            int offset = 0;
            for (int b = 0; b < _singleton.SpecialFOBs.Count; b++)
                if (_singleton.SpecialFOBs[b].Team == team)
                    offset++;
            int cacheOffset = -1, index = -1;
            for (int b = 0; b < _singleton.Caches.Count; b++)
            {
                Cache cache2 = _singleton.Caches[b];
                if ((object)cache2 == cache)
                {
                    ++cacheOffset;
                    index = b;
                    break;
                }
                else if (cache2.Team == team)
                    ++cacheOffset;
            }
            int ii = cacheOffset + offset;
            if (index != -1 && ListUI.FOBNames.Length > ii)
            {
                Cache c2 = _singleton.Caches[index];
                foreach (LanguageSet set in LanguageSet.OnTeam(team))
                {
                    string txt = T.FOBUI.Translate(set.Language, c2, c2.GridLocation, c2.ClosestLocation, null, set.Team);
                    while (set.MoveNext())
                    {
                        if (!set.Next.HasUIHidden)
                            ListUI.FOBNames[ii].SetText(set.Next.Connection, txt);
                    }
                }
            }
        }
        SendFOBListToTeam(team);
    }
    public static void SendFOBListToTeam(ulong team)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (team == 0) return;
        if (!Data.Is<TeamGamemode>()) return;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.GetTeam() == team && !pl.HasUIHidden)
                SendFOBList(PlayerManager.OnlinePlayers[i]);
        }
    }

    public static void HideFOBList(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        ListUI.ClearFromPlayer(player.Player.channel.owner.transportConnection);
    }
    public static void SendFOBList(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
        List<FOB> list;
        ulong team = player.GetTeam();
        if (team == 1)
            list = _singleton.Team1FOBs;
        else if (team == 2)
            list = _singleton.Team2FOBs;
        else return;

        UpdateUIList(team, player, list);
    }
    private void OnGroupChanged(GroupChanged e)
    {
        if (e.NewGroup.GetTeam() is > 0 and < 3)
            SendFOBList(e.Player);
    }
    public static void UpdateResourceUIString(FOB fob)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.Is<TeamGamemode>()) return;
        if (fob.IsBleeding) return;

        List<FOB> list;
        ulong team = fob.Team;
        if (team == 1)
            list = _singleton.Team1FOBs;
        else if (team == 2)
            list = _singleton.Team2FOBs;
        else return;

        int offset = 0;
        for (int b = 0; b < _singleton.SpecialFOBs.Count; b++)
            if (_singleton.SpecialFOBs[b].Team == team)
                offset++;
        for (int b = 0; b < _singleton.Caches.Count; b++)
            if (_singleton.Caches[b].Team == team)
                offset++;
        int i = list.IndexOf(fob);
        if (i == -1)
            return;
        i += offset;

        for (int j = 0; j < PlayerManager.OnlinePlayers.Count; j++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[j];
            if (pl.GetTeam() == team && !pl.HasUIHidden)
                ListUI.FOBResources[i].SetText(PlayerManager.OnlinePlayers[j].Connection, fob.UIResourceString);
        }
    }
    public static int GetUIFOBIndex(ulong team, IFOB fob)
    {
        if (fob is FOB fob2)
        {
            List<FOB> list;
            if (team == 1)
                list = _singleton.Team1FOBs;
            else if (team == 2)
                list = _singleton.Team2FOBs;
            else return -1;
            int offset = 0;
            for (int b = 0; b < _singleton.SpecialFOBs.Count; b++)
                if (_singleton.SpecialFOBs[b].Team == team)
                    offset++;
            for (int b = 0; b < _singleton.Caches.Count; b++)
                if (_singleton.Caches[b].Team == team)
                    offset++;
            int i = list.IndexOf(fob2);
            if (i == -1)
                return -1;
            int ii = i + offset;
            if (ListUI.FOBNames.Length > ii)
                return ii;
        }
        else if (fob is SpecialFOB)
        {
            int offset = -1, index = -1;
            for (int b = 0; b < _singleton.SpecialFOBs.Count; b++)
            {
                SpecialFOB specFob = _singleton.SpecialFOBs[b];
                if (specFob == fob)
                {
                    ++offset;
                    index = b;
                    break;
                }
                if (specFob.Team == team)
                    ++offset;
            }
            if (index != -1 && ListUI.FOBNames.Length > offset)
                return offset;
        }
        else if (fob is Cache)
        {
            int offset = 0;
            for (int b = 0; b < _singleton.SpecialFOBs.Count; b++)
                if (_singleton.SpecialFOBs[b].Team == team)
                    offset++;
            int cacheOffset = -1, index = -1;
            for (int b = 0; b < _singleton.Caches.Count; b++)
            {
                Cache cache2 = _singleton.Caches[b];
                if (cache2 == fob)
                {
                    ++cacheOffset;
                    index = b;
                    break;
                }
                if (cache2.Team == team)
                    ++cacheOffset;
            }
            int ii = cacheOffset + offset;
            if (index != -1 && ListUI.FOBNames.Length > ii)
            {
                return ii;
            }
        }

        return -1;
    }
    public static void UpdateFOBList(UCPlayer player, IFOB? fob = null)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = player.GetTeam();
        List<FOB> list;
        if (team == 1)
            list = _singleton.Team1FOBs;
        else if (team == 2)
            list = _singleton.Team2FOBs;
        else return;
        if (!player.HasFOBUI)
        {
            UpdateUIList(team, player, list);
            return;
        }

        int index;
        if (fob == null || (index = GetUIFOBIndex(team, fob)) == -1)
        {
            UpdateUIList(team, player, list);
        }
        else
        {
            ListUI.FOBNames[index].SetText(player.Connection, T.FOBUI.Translate(player, (IDeployable)fob, fob.GridLocation, fob.ClosestLocation));
            if (fob is IResourceFOB rs)
                ListUI.FOBResources[index].SetText(player.Connection, rs.UIResourceString);
        }
    }
    private static void UpdateUIList(ulong team, UCPlayer player, List<FOB> fobs)
    {
        _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ITransportConnection connection = player.Connection;
        if (!player.HasFOBUI)
        {
            ListUI.SendToPlayer(connection);
            player.HasFOBUI = true;
        }

        int i2 = 0;
        int min = Math.Min(_singleton.SpecialFOBs.Count, ListUI.FOBParents.Length);
        for (int i = 0; i < min; i++)
        {
            if (_singleton.SpecialFOBs[i].IsActive && _singleton.SpecialFOBs[i].Team == team)
            {
                SpecialFOB spf = _singleton.SpecialFOBs[i];
                ListUI.FOBParents[i2].SetVisibility(connection, true);
                ListUI.FOBNames[i2].SetText(connection, T.FOBUI.Translate(player, spf, spf.GridLocation, spf.ClosestLocation));
                i2++;
            }
        }

        if (Data.Is(out Insurgency ins) && team == ins.DefendingTeam)
        {
            min = Math.Min(_singleton.Caches.Count, ListUI.FOBParents.Length);
            for (int i = 0; i < min; i++)
            {
                Cache cache = _singleton.Caches[i];
                ListUI.FOBParents[i2].SetVisibility(connection, true);
                ListUI.FOBNames[i2].SetText(connection, T.FOBUI.Translate(player, cache, cache.GridLocation, cache.ClosestLocation));
                ListUI.FOBResources[i2].SetText(connection, string.Empty);
                i2++;
            }
        }

        min = Math.Min(fobs.Count, ListUI.FOBParents.Length - i2);
        for (int i = 0; i < min; i++)
        {
            ListUI.FOBParents[i2].SetVisibility(connection, true);
            ListUI.FOBNames[i2].SetText(connection, T.FOBUI.Translate(player, fobs[i], fobs[i].GridLocation, fobs[i].ClosestLocation));
            ListUI.FOBResources[i2].SetText(connection, fobs[i].UIResourceString);
            i2++;
        }
        for (; i2 < ListUI.FOBParents.Length; i2++)
        {
            ListUI.FOBParents[i2].SetVisibility(connection, false);
        }
    }
    void IUIListener.HideUI(UCPlayer player)
    {
        // todo
    }
    void IUIListener.ShowUI(UCPlayer player)
    {
        SendFOBList(player);
    }
    void IUIListener.UpdateUI(UCPlayer player)
    {
        SendFOBList(player);
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
        _cl = F.GetClosestLocationName(point);

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

    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(FOB.COLORED_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(UIColor ?? TeamManager.GetTeamHexColor(Team), Name, flags);
            else if (format.Equals(FOB.CLOSEST_LOCATION_FORMAT, StringComparison.Ordinal))
                return ClosestLocation;
            else if (format.Equals(FOB.GRID_LOCATION_FORMAT, StringComparison.Ordinal))
                return GridLocation.ToString();
        }
        return Name;
    }
    bool IDeployable.CheckDeployable(UCPlayer player, CommandInteraction? ctx)
    {
        if (IsActive)
            return true;
        if (ctx is not null)
            throw ctx.Reply(T.DeployNotSpawnable, this);
        return false;
    }
    bool IDeployable.CheckDeployableTick(UCPlayer player, bool chat)
    {
        if (IsActive)
            return true;
        if (chat)
            player.SendChat(T.DeployNotSpawnableTick, this);
        return false;
    }
    void IDeployable.OnDeploy(UCPlayer player, bool chat)
    {
        ActionLog.Add(ActionLogType.DeployToLocation, "SPECIAL FOB " + Name + " TEAM " + TeamManager.TranslateName(Team, 0), player);
        if (chat)
            player.SendChat(T.DeploySuccess, this);
    }

    float IDeployable.GetDelay() => FOBManager.Config.DeployFOBDelay;
}

public class FOBConfigData : JSONConfigData
{
    public JsonAssetReference<VehicleAsset> MortarBase;
    public float FOBMaxHeightAboveTerrain;
    public bool RestrictFOBPlacement;
    public ushort FOBID;
    public ushort FOBRequiredBuild;
    public int FOBBuildPickupRadius;
    public byte FobLimit;
    public int TicketsFOBRadioLost;

    public float AmmoCommandCooldown;
    public ushort AmmoCrateRequiredBuild;
    public ushort RepairStationRequiredBuild;

    public List<BuildableData> Buildables;
    public JsonAssetReference<VehicleAsset>[] LogiTruckIDs;
    public int AmmoBagMaxUses;

    public float DeployMainDelay;
    public float DeployFOBDelay;

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
        TicketsFOBRadioLost = -40;

        AmmoCrateRequiredBuild = 2;
        AmmoCommandCooldown = 120f;

        RepairStationRequiredBuild = 6;

        T1RadioState = "8yh8NQEAEAEAAAAAAAAAACIAAACmlQFkAAQAAKyVAWQAAAIArpUBZAAABQDGlQFkAAMFAMmVAWQABgUAyZUBZAAACACmZQFkAAMIAMCVAWQABggAwJUBZAAHAgDWlQFkAAoCANaVAWQABwMA1pUBZAAKAwDWlQFkAAcEANaVAWQACgQA1pUBZAAJBQDYlQFkAAkHANiVAWQACQkA2JUBZAAACwDOlQFkAAAMAM6VAWQAAA0AzpUBZAADCwDOlQFkAAcAAKyVAWQACgAA1pUBZAAKAQDWlQFkAAMMAM6VAWQAAw0AzpUBZAAGDQDQlQFkAAQCANqVAWQACQsA0JUBZAAJDADQlQFkAAkNANCVAWQABgsAzpUBZAAGDADOlQFkAA==";
        T2RadioState = "8yh8NQEAEAEAAAAAAAAAACIAAACmlQFkAAQAAKyVAWQAAAIArpUBZAADCADAlQFkAAYIAMCVAWQABwIA1pUBZAAKAgDWlQFkAAcDANaVAWQACgMA1pUBZAAHBADWlQFkAAoEANaVAWQACQUA2JUBZAAJBwDYlQFkAAkJANiVAWQAAAsAzpUBZAAADADOlQFkAAANAM6VAWQAAwsAzpUBZAAHAACslQFkAAoAANaVAWQACgEA1pUBZAADDADOlQFkAAMNAM6VAWQABg0A0JUBZAAEAgDalQFkAAkLANCVAWQACQwA0JUBZAAJDQDQlQFkAAYLAM6VAWQABgwAzpUBZAAABQDDlQFkAAMFAMqVAWQABgUAypUBZAAACAC6ZQFkAA==";

        LogiTruckIDs = new JsonAssetReference<VehicleAsset>[]
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
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>("61c349f10000498fa2b92c029d38e523"),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("1bb17277dd8148df9f4c53d1a19b2503"),
                Type = BuildableType.Bunker,
                RequiredHits = 30,
                RequiredBuild = 15,
                Team = 0,
                Limit = 1,
                Emplacement = null
            },
            new BuildableData
            {
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>("6fe208519d7c45b0be38273118eea7fd"),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("eccfe06e53d041d5b83c614ffa62ee59"),
                Type = BuildableType.AmmoCrate,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Limit = 6,
                Emplacement = null
            },
            new BuildableData
            {
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>("c0d11e0666694ddea667377b4c0580be"),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("26a6b91cd1944730a0f28e5f299cebf9"),
                Type = BuildableType.RepairStation,
                RequiredHits = 25,
                RequiredBuild = 15,
                Team = 0,
                Limit = 1,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag line
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>("ab702192eab4456ebb9f6d7cc74d4ba2"),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("15f674dcaf3f44e19a124c8bf7e19ca2"),
                Type = BuildableType.Fortification,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Limit = 8,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag pillbox
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>("f3bd9ee2fa334faabc8fd9d5a3b84424"),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("a9294335d8e84b76b1cbcb7d70f66aaa"),
                Type = BuildableType.Fortification,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Limit = 6,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag crescent
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>("eefee76f077349e58359f5fd03cf311d"),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("920f8b30ae314406ab032a0c2efa753d"),
                Type = BuildableType.Fortification,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Limit = 4,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag foxhole
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>("a71e3e3d6bb54a36b7bd8bf5f25160aa"),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("12ea830dd9ab4f949893bbbbc5e9a5f6"),
                Type = BuildableType.Fortification,
                RequiredHits = 12,
                RequiredBuild = 2,
                Team = 0,
                Limit = 3,
                Emplacement = null
            },
            new BuildableData
            {
                // razorwire
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>("bc24bd85ff714ff7bb2f8b2dd5056395"),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("a2a8a01a58454816a6c9a047df0558ad"),
                Type = BuildableType.Fortification,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Limit = 16,
                Emplacement = null
            },
            new BuildableData
            {
                // hesco wall
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>("e1af3a3af31e4996bc5d6ffd9a0773ec"),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("baf23a8b514441ee8db891a3ddf32ef4"),
                Type = BuildableType.Fortification,
                RequiredHits = 25,
                RequiredBuild = 1,
                Team = 0,
                Limit = 4,
                Emplacement = null
            },
            new BuildableData
            {
                // hesco tower
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>("857c85161f254964a921700a69e215a9"),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("827d0ca8bfff43a39f750f191e16ea71"),
                Type = BuildableType.Fortification,
                RequiredHits = 20,
                RequiredBuild = 1,
                Team = 0,
                Limit = 4,
                Emplacement = null
            },
            new BuildableData
            {
                // M2A1
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("80396c361d3040d7beb3921964ec2997"),
                Type = BuildableType.Emplacement,
                RequiredHits = 16,
                RequiredBuild = 6,
                Team = 1,
                Limit = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("aa3c6af4911243b5b5c9dc95ca1263bf"),
                    BaseBarricade =  new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("523c49ce4df44d46ba37be0dd6b4504b"),
                    AmmoCount = 2
                }
            },
            new BuildableData
            {
                // Kord
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("e44ba62f763c432e882ddc7eabaa9c77"),
                Type = BuildableType.Emplacement,
                RequiredHits = 16,
                RequiredBuild = 6,
                Team = 2,
                Limit = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("86cfe1eb8be144aeae7659c9c74ff11a"),
                    BaseBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("6e9bc2083a1246b49b1656c2ec6f535a"),
                    AmmoCount = 2,
                }
            },
            new BuildableData
            {
                // TOW
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("a68ae466fb804829a0eb0d4556071801"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 1,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("9d305050a6a142349376d6c49fb38362"),
                    BaseBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("3128a69d06ac4bbbbfddc992aa7185a6"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Kornet
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("37811b1847744c958fcb30a0b759874b"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 2,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("677b1084-dffa-4633-84d2-9167a3fae25b"),
                    BaseBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("d7774b017c404adbb0a0fe8e902b9689"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Stinger
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("3c2dd7febc854b7f8859852b8c736c8e"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 2,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("1883345cbdad40aa81e49c84e6c872ef"),
                    BaseBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("3c0a94af5af24901a9e3207f3e9ed0ba"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Igla
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("b50cb548734946ffa5f88d6691a2c7ce"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("8add59a2e2b94f93ab0d6b727d310097"),
                    BaseBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("a54d571983c2432a9624eec39d602997"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Mortar
                BuildableBarricade = new JsonAssetReference<ItemBarricadeAsset>(),
                Foundation = new JsonAssetReference<ItemBarricadeAsset>("6ff4826eaeb14c7cac1cf25a55d24bd3"),
                Type = BuildableType.Emplacement,
                RequiredHits = 22,
                RequiredBuild = 10,
                Team = 0,
                Limit = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("94bf8feb05bc4680ac26464bc175460c"),
                    BaseBarricade = new JsonAssetReference<ItemBarricadeAsset>("c3eb4dd3fd1d463993ec69c4c3de50d7"), // Mortar
                    Ammo = new JsonAssetReference<ItemAsset>("66f4c76a119e4d6ca9d0b1a866c4d901"),
                    AmmoCount = 3,
                    ShouldWarnFriendlies = true,
                    ShouldWarnEnemies = true
                }
            },
        };

        DeployMainDelay = 3;
        DeployFOBDelay = 5;

        DeployCancelOnMove = true;
        DeployCancelOnDamage = true;

        ShouldRespawnAtMain = true;
        ShouldSendPlayersBackToMainOnRoundEnded = true;
        ShouldWipeAllFOBsOnRoundedEnded = true;
        ShouldKillMaincampers = true;
    }
}

[JsonSerializable(typeof(BuildableData))]
public class BuildableData : ITranslationArgument
{
    [JsonPropertyName("foundationID")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> Foundation;
    [JsonPropertyName("structureID")]
    public RotatableConfig<JsonAssetReference<ItemBarricadeAsset>> BuildableBarricade;
    [JsonPropertyName("type")]
    public BuildableType Type;
    [JsonPropertyName("requiredHits")]
    public RotatableConfig<int> RequiredHits;
    [JsonPropertyName("requiredBuild")]
    public RotatableConfig<int> RequiredBuild;
    [JsonPropertyName("team")]
    public int Team;
    [JsonPropertyName("limit")]
    public int Limit;
    [JsonPropertyName("disabled")]
    public RotatableConfig<bool> Disabled;
    [JsonPropertyName("emplacementData")]
    public EmplacementData? Emplacement;

    public string Translate(string language, string? format, UCPlayer? target, CultureInfo? culture, ref TranslationFlags flags)
    {
        ItemBarricadeAsset asset;
        if (Emplacement is not null)
        {
            if (Emplacement.EmplacementVehicle.ValidReference(out VehicleAsset vasset))
            {
                string plural = Translation.Pluralize(language, culture, vasset.vehicleName, flags);
                if (format is not null && format.Equals(T.FormatRarityColor))
                    return Localization.Colorize(ItemTool.getRarityColorUI(vasset.rarity).Hex(), plural, flags);
                else
                    return plural;
            }
            if (Emplacement.BaseBarricade.ValidReference(out asset))
            {
                string plural = Translation.Pluralize(language, culture, asset.itemName, flags);
                if (format is not null && format.Equals(T.FormatRarityColor))
                    return Localization.Colorize(ItemTool.getRarityColorUI(asset.rarity).Hex(), plural, flags);
                else
                    return plural;
            }
            if (Emplacement.Ammo.ValidReference(out ItemAsset iasset))
            {
                string plural = Translation.Pluralize(language, culture, iasset.itemName, flags);
                if (format is not null && format.Equals(T.FormatRarityColor))
                    return Localization.Colorize(ItemTool.getRarityColorUI(iasset.rarity).Hex(), plural, flags);
                else
                    return plural;
            }
        }

        if (BuildableBarricade.ValidReference(out asset) || Foundation.ValidReference(out asset))
        {
            string plural = Translation.Pluralize(language, culture, asset.itemName, flags);
            if (format is not null && format.Equals(T.FormatRarityColor))
                return Localization.Colorize(ItemTool.getRarityColorUI(asset.rarity).Hex(), plural, flags);
            else
                return plural;
        }

        return Localization.TranslateEnum(Type, language);
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
    [JsonPropertyName("warnFriendlyProjectiles")]
    public bool ShouldWarnFriendlies;
    [JsonPropertyName("warnEnemyProjectiles")]
    public bool ShouldWarnEnemies;
}

[Translatable("Buildable Type")]
public enum BuildableType
{
    Bunker,
    AmmoCrate,
    RepairStation,
    Fortification,
    Emplacement,
    Radio
}