using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.FOBs
{
    public delegate void PlayerEnteredFOBRadiusHandler(FOB fob, UCPlayer player);
    public delegate void PlayerLeftFOBRadiusHandler(FOB fob, UCPlayer player);

    public class FOBManager
    {
        public static Config<FOBConfig> config;
        public static readonly List<FOB> Team1FOBs = new List<FOB>();
        public static readonly List<FOB> Team2FOBs = new List<FOB>();
        public static readonly List<SpecialFOB> SpecialFOBs = new List<SpecialFOB>();


        public static event PlayerEnteredFOBRadiusHandler OnPlayerEnteredFOBRadius;
        public static event PlayerLeftFOBRadiusHandler OnPlayerLeftFOBRadius;
        public static event PlayerEnteredFOBRadiusHandler OnEnemyEnteredFOBRadius;
        public static event PlayerLeftFOBRadiusHandler OnEnemyLeftFOBRadius;

        public FOBManager()
        {
            config = new Config<FOBConfig>(Data.FOBStorage, "config.json");

            OnPlayerEnteredFOBRadius += OnEnteredFOBRadius;
            OnPlayerLeftFOBRadius += OnLeftFOBRadius;
            OnEnemyEnteredFOBRadius += OnEnemyEnteredFOB;
            OnEnemyLeftFOBRadius += OnEnemyLeftFOB;
            
        }

        public static void Reset()
        {
            Team1FOBs.Clear();
            Team2FOBs.Clear();
            SpecialFOBs.Clear();
            UpdateUIAll();
            OnPlayerEnteredFOBRadius -= OnEnteredFOBRadius;
            OnPlayerLeftFOBRadius -= OnLeftFOBRadius;
            OnEnemyEnteredFOBRadius -= OnEnemyEnteredFOB;
            OnEnemyLeftFOBRadius -= OnEnemyLeftFOB;
        }
        public static void OnNewGameStarting()
        {
            Team1FOBs.Clear();
            Team2FOBs.Clear();
            SpecialFOBs.Clear();

            UpdateUIAll();
        }
        public static void OnItemDropped(Item item, Vector3 point)
        {
            ulong team;
            if (item.id == config.Data.Team1BuildID)
                team = 1;
            else if (item.id == config.Data.Team2BuildID)
                team = 2;
            else
                return;

            var fobs = GetFriendlyFOBs(team).Where(f => (f.Structure.model.position - point).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2));

            var alreadyCounted = new List<UCPlayer>();

            foreach (var fob in fobs)
            {
                foreach (var player in fob.nearbyPlayers)
                {
                    if (!alreadyCounted.Contains(player))
                    {
                        alreadyCounted.Add(player);
                        UpdateBuildUI(player);
                    }

                }
            }

            var foundations = UCBarricadeManager.GetNearbyBarricades(config.Data.FOBBaseID, 30, point, false);
            foreach (var foundation in foundations)
            {
                if (foundation.model.TryGetComponent<FOBBaseComponent>(out var component))
                {
                    foreach (var player in component.nearbyPlayers)
                    {
                        if (!alreadyCounted.Contains(player))
                        {
                            alreadyCounted.Add(player);
                            UpdateBuildUI(player);
                        }
                    }
                }
            }
        }
        public static void OnItemRemoved(SDG.Unturned.ItemData itemData)
        {
            ulong team;
            if (itemData.item.id == config.Data.Team1BuildID)
                team = 1;
            else if (itemData.item.id == config.Data.Team2BuildID)
                team = 2;
            else
                return;

            var fobs = GetFriendlyFOBs(team).Where(f => (f.Structure.model.position - itemData.point).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2));

            var alreadyCounted = new List<UCPlayer>();

            foreach (var fob in fobs)
            {
                foreach (var player in fob.nearbyPlayers)
                {
                    if (!alreadyCounted.Contains(player))
                    {
                        alreadyCounted.Add(player);
                        UpdateBuildUI(player);
                    }
                    
                }
            }

            var foundations = UCBarricadeManager.GetNearbyBarricades(config.Data.FOBBaseID, 30, itemData.point, false);
            foreach (var foundation in foundations)
            {
                if (foundation.model.TryGetComponent<FOBBaseComponent>(out var component))
                {
                    foreach (var player in component.nearbyPlayers)
                    {
                        if (!alreadyCounted.Contains(player))
                        {
                            alreadyCounted.Add(player);
                            UpdateBuildUI(player);
                        }
                    }
                }
            }
        }
        public static void RefillMainStorages()
        {
            var repairStations = UCBarricadeManager.GetBarricadesByID(config.Data.RepairStationID).ToList();
            var ammoCrates = UCBarricadeManager.GetBarricadesByID(config.Data.AmmoCrateID).ToList();

            for (int i = 0; i < repairStations.Count; i++)
            {
                if (F.IsInMain(repairStations[i].model.transform.position))
                {
                    ushort BuildID = 0;
                    if (repairStations[i].GetServersideData().group == 1)
                        BuildID = config.Data.Team1BuildID;
                    else if (repairStations[i].GetServersideData().group == 2)
                        BuildID = config.Data.Team2BuildID;

                    UCBarricadeManager.TryAddItemToStorage(repairStations[i], BuildID);
                }
            }
            for (int i = 0; i < ammoCrates.Count; i++)
            {
                if (F.IsInMain(ammoCrates[i].model.transform.position))
                {
                    ushort AmmoID = 0;
                    if (ammoCrates[i].GetServersideData().group == 1)
                        AmmoID = config.Data.Team1AmmoID;
                    else if (ammoCrates[i].GetServersideData().group == 2)
                        AmmoID = config.Data.Team2AmmoID;

                    UCBarricadeManager.TryAddItemToStorage(ammoCrates[i], AmmoID);
                }
            }
        }
        public static void UpdateBuildUI(UCPlayer player)
        {
            EffectManager.sendUIEffectText((short)unchecked(config.Data.BuildResourceUI), player.connection, true,
                    "Build",
                    GetNearbyBuildForPlayer(player).Count.ToString()
                    );
        }
        public static void OnAmmoCrateUpdated(InteractableStorage storage, BarricadeDrop ammoCrate)
        {
            var data = ammoCrate.GetServersideData();
            var fobs = GetFriendlyFOBs(data.group).Where(f => (f.Structure.model.position - data.point).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2));

            var alreadyCounted = new List<UCPlayer>();

            foreach (var fob in fobs)
            {
                foreach (var player in fob.nearbyPlayers)
                {
                    if (!alreadyCounted.Contains(player))
                    {
                        alreadyCounted.Add(player);
                        UpdateAmmoUI(player);
                    }
                }
            }
        }

        public static List<SDG.Unturned.ItemData> GetNearbyBuildForPlayer(UCPlayer player)
        {
            var counted = new List<SDG.Unturned.ItemData>();

            ushort BuildID = 0;
            if (player.GetTeam() == 1)
                BuildID = config.Data.Team1BuildID;
            else if (player.GetTeam() == 2)
                BuildID = config.Data.Team2BuildID;
            else
                return counted;

            var nearbyFOBs = GetFriendlyFOBs(player.GetTeam()).Where(f => f.nearbyPlayers.Contains(player));
            var nearbyFoundations = GetNearbyFriendlyFoundations(player.Position, player.GetTeam());

            foreach (var foundation in nearbyFoundations)
            {
                foreach (var item in UCBarricadeManager.GetNearbyItems(BuildID, 30, foundation.model.position))
                {
                    if (!counted.Contains(item))
                    {
                        counted.Add(item);
                    }
                }
            }
            foreach (var fob in nearbyFOBs)
            {
                foreach (var item in UCBarricadeManager.GetNearbyItems(BuildID, config.Data.FOBBuildPickupRadius, fob.Structure.model.position))
                {
                    if (!counted.Contains(item))
                    {
                        counted.Add(item);
                    }
                }
            }

            return counted;
        }

        public static List<FOB> GetFriendlyFOBs(ulong team)
        {
            List<FOB> FOBList = null;

            if (team == 1)
                FOBList = Team1FOBs;
            else if (team == 2)
                FOBList = Team2FOBs;
            else
                FOBList = new List<FOB>();

            return FOBList;
        }
        public static IEnumerable<BarricadeDrop> GetNearbyFriendlyFoundations(Vector3 origin, ulong team)
        {
            var foundations = UCBarricadeManager.GetBarricadesByID(config.Data.FOBBaseID);
            return UCBarricadeManager.GetNearbyBarricades(foundations, 30, origin, true).Where(d => d.GetServersideData().group == team);
        }
        public static void UpdateAmmoUI(UCPlayer player)
        {
            int ammoCount = 0;

            var alreadyCounted = new List<BarricadeDrop>();

            var nearbyFOBs = GetFriendlyFOBs(player.GetTeam()).Where(f => f.nearbyPlayers.Contains(player));

            foreach (var fob in nearbyFOBs)
            {
                if (fob.IsCache)
                {
                    if (fob.Structure.interactable is InteractableStorage cache)
                        ammoCount += CountAmmo(cache, player.GetTeam());
                }

                var ammoCrates = UCBarricadeManager.GetNearbyBarricades(config.Data.AmmoCrateID, config.Data.FOBBuildPickupRadius, fob.Structure.model.position, true);

                foreach (var ammoCrate in ammoCrates)
                {
                    if (ammoCrate.interactable is InteractableStorage crate)
                    {
                        if (!alreadyCounted.Contains(ammoCrate))
                        {
                            ammoCount += CountAmmo(crate, player.GetTeam());
                            alreadyCounted.Add(ammoCrate);
                        }
                    }

                }
            }

            EffectManager.sendUIEffectText((short)unchecked(config.Data.BuildResourceUI), player.connection, true,
                   "Ammo",
                   ammoCount.ToString()
                   );
        }
        public static int CountAmmo(InteractableStorage storage, ulong team)
        {
            int ammoCount = 0;
            
            for (int i = 0; i < storage.items.items.Count; i++)
            {
                var jar = storage.items.items[i];

                if ((TeamManager.IsTeam1(team) && jar.item.id == config.Data.Team1AmmoID) || (TeamManager.IsTeam2(team) && jar.item.id == config.Data.Team2AmmoID))
                {
                    ammoCount++;
                }
            }
            return ammoCount;
        }
        public static void OnGameTick(uint counter)
        {
            for (int i = 0; i < Team1FOBs.Count; i++)
            {
                Tick(Team1FOBs[i], (int)counter);
            }
            for (int i = 0; i < Team2FOBs.Count; i++)
            {
                Tick(Team2FOBs[i], (int)counter);
            }
            for (int i = 0; i < SpecialFOBs.Count; i++)
            {
                Tick(SpecialFOBs[i], (int)counter);
            }

            if (counter % 60 == 0)
                RefillMainStorages();
        }
        public static void Tick(FOB fob, int counter = -1)
        {
            for (int j = 0; j < PlayerManager.OnlinePlayers.Count; j++)
            {
                if (PlayerManager.OnlinePlayers[j].GetTeam() == fob.Structure.GetServersideData().group)
                {
                    if ((fob.Structure.model.position - PlayerManager.OnlinePlayers[j].Position).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2))
                    {
                        if (!fob.nearbyPlayers.Contains(PlayerManager.OnlinePlayers[j]))
                        {
                            fob.nearbyPlayers.Add(PlayerManager.OnlinePlayers[j]);
                            OnPlayerEnteredFOBRadius?.Invoke(fob, PlayerManager.OnlinePlayers[j]);
                        }
                    }
                    else if (fob.nearbyPlayers.Contains(PlayerManager.OnlinePlayers[j]))
                    {
                        fob.nearbyPlayers.Remove(PlayerManager.OnlinePlayers[j]);
                        OnPlayerLeftFOBRadius?.Invoke(fob, PlayerManager.OnlinePlayers[j]);
                    }
                }
                else
                {
                    if ((fob.Structure.model.position - PlayerManager.OnlinePlayers[j].Position).sqrMagnitude < Math.Pow(9, 2))
                    {
                        if (!PlayerManager.OnlinePlayers[j].Player.life.isDead)
                        {
                            if (!fob.nearbyEnemies.Contains(PlayerManager.OnlinePlayers[j]))
                            {
                                fob.nearbyEnemies.Add(PlayerManager.OnlinePlayers[j]);
                                OnEnemyEnteredFOBRadius?.Invoke(fob, PlayerManager.OnlinePlayers[j]);
                            }
                        }
                        
                    }
                    else if (fob.nearbyEnemies.Contains(PlayerManager.OnlinePlayers[j]))
                    {
                        fob.nearbyEnemies.Remove(PlayerManager.OnlinePlayers[j]);
                        OnEnemyLeftFOBRadius?.Invoke(fob, PlayerManager.OnlinePlayers[j]);
                    }
                    //if (
                    //    Data.Is(out Insurgency insurgency) &&
                    //    fob.IsCache &&
                    //    !fob.isDiscovered &&
                    //    !PlayerManager.OnlinePlayers[j].Player.life.isDead &&
                    //    (fob.Structure.model.position - PlayerManager.OnlinePlayers[j].Position).sqrMagnitude < Math.Pow(insurgency.Config.CacheDiscoverRange, 2))
                    //{
                    //    insurgency.OnCacheDiscovered(fob);
                    //}
                }

                //if (counter % 4 == 0)
                //{
                //    UpdateBuildUI(PlayerManager.OnlinePlayers[j]);
                //    UpdateAmmoUI(PlayerManager.OnlinePlayers[j]);
                //}
            }
        }
        public static void Tick(SpecialFOB special, int counter = -1)
        {
            if (special.DisappearAroundEnemies && counter % 4 == 0)
            {
                if (Provider.clients.Where(p => p.GetTeam() != special.Team && (p.player.transform.position - special.Point).sqrMagnitude < Math.Pow(20, 2)).Count() > 0)
                {
                    DeleteSpecialFOB(special.Name, special.Team);
                }
            }
        }

        public static void OnEnteredFOBRadius(FOB fob, UCPlayer player)
        {
            EffectManager.sendUIEffect(config.Data.BuildResourceUI, (short)unchecked(config.Data.BuildResourceUI), player.Player.channel.owner.transportConnection, true);

            UpdateBuildUI(player);
            UpdateAmmoUI(player);
        }
        public static void OnLeftFOBRadius(FOB fob, UCPlayer player)
        {
            List<FOB> FOBList = GetFriendlyFOBs(player.GetTeam());

            bool isAroundotherFOBs = false;
            foreach (var f in FOBList)
            {
                if (f.nearbyPlayers.Contains(player))
                {
                    isAroundotherFOBs = true;
                    break;
                }
            }

            if (!isAroundotherFOBs)
            {
                EffectManager.askEffectClearByID(config.Data.BuildResourceUI, player.Player.channel.owner.transportConnection);
            }
            else
            {
                UpdateBuildUI(player);
                UpdateAmmoUI(player);
            }
        }

        public static void OnEnemyEnteredFOB(FOB fob, UCPlayer enemy)
        {
            UpdateUIForTeam(fob.Structure.GetServersideData().group);
        }
        public static void OnEnemyLeftFOB(FOB fob, UCPlayer enemy)
        {
            UpdateUIForTeam(fob.Structure.GetServersideData().group);
        }

        public static void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {

            if (data.barricade.id == config.Data.AmmoCrateID)
            {
                IEnumerable<BarricadeDrop> TotalFOBs = UCBarricadeManager.GetAllFobs().Where(f => f.GetServersideData().group == data.group);
                IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyBarricades(TotalFOBs, config.Data.FOBBuildPickupRadius, drop.model.position, true);

                if (NearbyFOBs.Count() != 0)
                {
                    List<UCPlayer> nearbyPlayers = PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == data.group && !p.Player.life.isDead && (p.Position - NearbyFOBs.FirstOrDefault().model.position).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2)).ToList();

                    for (int i = 0; i < nearbyPlayers.Count; i++)
                    {
                        UpdateAmmoUI(nearbyPlayers[i]);
                    }
                }
            }
            else if (data.barricade.id == config.Data.FOBBaseID)
            {
                if (drop.model.TryGetComponent<FOBBaseComponent>(out var component))
                {
                    component.OnDestroyed();
                }
            }
            else
            {
                DeleteFOB(instanceID, data.group.GetTeam(), drop.model.TryGetComponent(out BarricadeComponent o) ? o.LastDamager : 0);
            }
        }

        public static void LoadFobsFromMap()
        {
            GetRegionBarricadeLists(
                out List<BarricadeDrop> Team1FOBBarricades,
                out List<BarricadeDrop> Team2FOBBarricades
                );

            Team1FOBs.Clear();
            Team2FOBs.Clear();
            SpecialFOBs.Clear();

            for (int i = 0; i < Team1FOBs.Count; i++)
            {
                Team1FOBs.Add(new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Team1FOBBarricades[i], "54e3ff"));
            }
            for (int i = 0; i < Team2FOBs.Count; i++)
            {
                Team2FOBs.Add(new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Team2FOBBarricades[i], "54e3ff"));
            }
            UpdateUIAll();
        }
        public static FOB RegisterNewFOB(BarricadeDrop Structure, string color, bool isCache = false)
        {
            FOB fob = null;
            SDG.Unturned.BarricadeData data = Structure.GetServersideData();
            ulong team = data.group.GetTeam();
            bool isInsurgency = Data.Is(out Insurgency insurgency);

            if (isCache)
            {
                int number;
                List<Insurgency.CacheData> caches = insurgency.ActiveCaches;
                if (caches.Count == 0)
                    number = insurgency.CachesDestroyed + 1;
                else
                    number = caches.Last().Number + 1;

                fob = new FOB("CACHE" + (number).ToString(Data.Locale), number, Structure, color, isCache);
                
                if (team == 1)
                {
                    Team1FOBs.Insert(0, fob);
                }
                else if (team == 2)
                {
                    Team2FOBs.Insert(0, fob);
                }
            }
            else
            {
                if (Data.Is(out IWarstatsGamemode ws) && ws.GameStats != null)
                {
                    if (F.TryGetPlaytimeComponent(Structure.GetServersideData().owner, out PlaytimeComponent c) && c.stats is IFOBStats f)
                        f.AddFOBPlaced();
                    if (team == 1)
                    {
                        ws.GameStats.fobsPlacedT1++;
                    }
                    else if (team == 2)
                    {
                        ws.GameStats.fobsPlacedT2++;
                    }
                }

                if (team == 1)
                {
                    int number = 1;
                    bool placed = false;
                    for (int i = 0; i < Team1FOBs.Count; i++)
                    {
                        if (!Team1FOBs[i].IsCache)
                        {
                            if (Team1FOBs[i].Number != number)
                            {
                                fob = new FOB("FOB" + number.ToString(Data.Locale), number, Structure, color, isCache);
                                Team1FOBs.Insert(i, fob);
                                placed = true;
                                break;
                            }

                            number++;
                        }
                    }

                    if (!placed)
                    {
                        fob = new FOB("FOB" + number.ToString(Data.Locale), number, Structure, color, isCache);
                        Team1FOBs.Add(fob);
                    }
                }
                else if (team == 2)
                {
                    int number = 1;
                    bool placed = false;
                    for (int i = 0; i < Team2FOBs.Count; i++)
                    {
                        if (!Team2FOBs[i].IsCache)
                        {
                            if (Team2FOBs[i].Number != number)
                            {
                                fob = new FOB("FOB" + number.ToString(Data.Locale), number, Structure, color, isCache);
                                Team2FOBs.Insert(i, fob);
                                placed = true;
                                break;
                            }

                            number++;
                        }
                    }

                    if (!placed)
                    {
                        fob = new FOB("FOB" + number.ToString(Data.Locale), number, Structure, color, isCache);
                        Team2FOBs.Add(fob);
                    }
                }
            }

            UpdateUIForTeam(team);
            return fob;
        }
        public static void RegisterNewSpecialFOB(string name, Vector3 point, ulong team, string UIcolor, bool disappearAroundEnemies)
        {
            SpecialFOBs.Add(new SpecialFOB(name, point, team, UIcolor, disappearAroundEnemies));

            UpdateUIForTeam(team);
        }

        public static void DeleteFOB(uint instanceID, ulong team, ulong player)
        {
            FOB removed;
            if (team == 1)
            {
                removed = Team1FOBs.FirstOrDefault(x => x.Structure.instanceID == instanceID);
                Team1FOBs.RemoveAll(f => f.Structure.instanceID == instanceID);
            }
            else if (team == 2)
            {
                removed = Team2FOBs.FirstOrDefault(x => x.Structure.instanceID == instanceID);
                Team2FOBs.RemoveAll(f => f.Structure.instanceID == instanceID);
            }
            else removed = null;

            if (removed != null)
            {
                List<FOB> FOBList = GetFriendlyFOBs(team);

                foreach (UCPlayer p in removed.nearbyPlayers)
                {
                    if (FOBList.Where(f => f.nearbyPlayers.Contains(p)).Count() != 0)
                    {
                        UpdateBuildUI(p);
                        UpdateAmmoUI(p);
                    }
                    else
                        EffectManager.askEffectClearByID(config.Data.BuildResourceUI, p.connection);
                }

                IEnumerator<PlaytimeComponent> pts = Data.PlaytimeComponents.Values.GetEnumerator();
                while (pts.MoveNext())
                {
                    if (pts.Current.PendingFOB is FOB fob && fob.Number == removed.Number)
                    {
                        pts.Current.CancelTeleport();
                    }
                }
                pts.Dispose();
            }
            if (Data.Is(out IWarstatsGamemode w) && w.GameStats != null && w.State == EState.ACTIVE)
            // doesnt count destroying fobs after game ends
            {
                if (F.TryGetPlaytimeComponent(player, out PlaytimeComponent c) && c.stats is IFOBStats f)
                    f.AddFOBDestroyed();
                if (team == 1)
                {
                    w.GameStats.fobsDestroyedT2++;
                }
                else if (team == 2)
                {
                    w.GameStats.fobsDestroyedT1++;
                }
            }
            UCPlayer ucplayer = UCPlayer.FromID(player);
            if (Data.Is(out Insurgency insurgency) && removed != null && removed.IsCache)
            {
                insurgency.OnCacheDestroyed(removed, ucplayer);
            }
            else if (removed != null && !removed.IsCache && ucplayer != null)
            {
                if (ucplayer.GetTeam() == team)
                {
                    XP.XPManager.AddXP(ucplayer.Player, XP.XPManager.config.Data.FOBTeamkilledXP, F.Translate("xp_fob_teamkilled", player));
                }
                else
                {
                    XP.XPManager.AddXP(ucplayer.Player, XP.XPManager.config.Data.FOBKilledXP, F.Translate("xp_fob_killed", player));
                    Stats.StatsManager.ModifyStats(player, x => x.FobsDestroyed++, false);
                    Stats.StatsManager.ModifyTeam(team, t => t.FobsDestroyed++, false);
                }
            }
            UpdateUIForTeam(team);
        }
        public static void DeleteSpecialFOB(string name, ulong team)
        {
            SpecialFOB removed = SpecialFOBs.FirstOrDefault(x => x.Name == name && x.Team == team);
            SpecialFOBs.Remove(removed);

            if (removed != null)
            {
                IEnumerator<PlaytimeComponent> pts = Data.PlaytimeComponents.Values.GetEnumerator();
                while (pts.MoveNext())
                {
                    if (pts.Current.PendingFOB is SpecialFOB special)
                    {
                        pts.Current.CancelTeleport();
                    }
                }
            }

            UpdateUIForTeam(team);
        }

        public static void GetRegionBarricadeLists(
                out List<BarricadeDrop> Team1Barricades,
                out List<BarricadeDrop> Team2Barricades
                )
        {
            IEnumerable<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>();

            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            Team1Barricades = barricadeDrops.Where(b =>
                b.GetServersideData().barricade.id == config.Data.FOBID &&   // All barricades that are FOB Structures
                TeamManager.IsTeam1(b.GetServersideData().group)        // All barricades that are friendly
                ).ToList();
            Team2Barricades = barricadeDrops.Where(b =>
                b.GetServersideData().barricade.id == config.Data.FOBID &&   // All barricades that are FOB Structures
                TeamManager.IsTeam2(b.GetServersideData().group)        // All barricades that are friendly
                ).ToList();
        }

        public static bool FindFOBByName(string name, ulong team, out object fob)
        {
            fob = SpecialFOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && f.Team == team);
            if (fob != null)
                return true;

            if (team == 1)
            {
                fob = Team1FOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return fob != null;
            }
            else if (team == 2)
            {
                fob = Team2FOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return fob != null;
            }
            fob = null;
            return false;
        }

        public static void UpdateUI(UCPlayer player)
        {
            List<FOB> FOBList;
            ulong team = player.GetTeam();
            if (team == 1)
            {
                FOBList = Team1FOBs;
            }
            else if (team == 2)
            {
                FOBList = Team2FOBs;
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    EffectManager.askEffectClearByID(unchecked((ushort)(config.Data.FirstFOBUiId + i)), player.Player.channel.owner.transportConnection);
                }
                return;
            }


            for (int i = 0; i < 10; i++)
            {
                EffectManager.askEffectClearByID(unchecked((ushort)(config.Data.FirstFOBUiId + i)), player.Player.channel.owner.transportConnection);
            }

            int start = 0;
            for (int i = 0; i < Math.Min(SpecialFOBs.Count, config.Data.FobLimit); i++)
            {
                if (SpecialFOBs[i].IsActive && SpecialFOBs[i].Team == team)
                {
                    string name = $"<color={SpecialFOBs[i].UIColor}>{SpecialFOBs[i].Name}</color>";
                    EffectManager.sendUIEffect(unchecked((ushort)(config.Data.FirstFOBUiId + i)), unchecked((short)(config.Data.FirstFOBUiId + i)),
                    player.Player.channel.owner.transportConnection, true, F.Translate("fob_ui", player.Steam64, name, SpecialFOBs[i].ClosestLocation));
                    start++;
                }
            }
            for (int i = 0; i < Math.Min(FOBList.Count, config.Data.FobLimit); i++)
            {
                string name = FOBList[i].nearbyEnemies.Count == 0 ? $"<color={FOBList[i].UIColor}>{FOBList[i].Name}</color>" : $"<color=#ff8754>{FOBList[i].Name}</color>";

                EffectManager.sendUIEffect(unchecked((ushort)(config.Data.FirstFOBUiId + i + start)), unchecked((short)(config.Data.FirstFOBUiId + i + start)),
                player.Player.channel.owner.transportConnection, true, F.Translate("fob_ui", player.Steam64, name, FOBList[i].ClosestLocation));
            }
        }
        public static void UpdateUIAll()
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                UpdateUI(player);
            }
        }
        public static void UpdateUIForTeam(ulong team)
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == team))
            {
                UpdateUI(player);
            }
        }
    }

    public class FOB
    {
        public string Name;
        public int Number;
        public BarricadeDrop Structure;
        public string ClosestLocation;
        public List<UCPlayer> nearbyPlayers;
        public List<UCPlayer> nearbyEnemies;
        public bool IsCache;
        public string UIColor;
        public bool isDiscovered;
        public FOB(string Name, int number, BarricadeDrop Structure, string color, bool isCache = false)
        {
            this.Name = Name;
            Number = number;
            this.Structure = Structure;
            ClosestLocation =
                (LevelNodes.nodes
                .Where(n => n.type == ENodeType.LOCATION)
                .Aggregate((n1, n2) =>
                    (n1.point - Structure.model.position).sqrMagnitude <= (n2.point - Structure.model.position).sqrMagnitude ? n1 : n2) as LocationNode)
                .name;
            nearbyPlayers = new List<UCPlayer>();
            nearbyEnemies = new List<UCPlayer>();

            IsCache = isCache;
            UIColor = color;
            isDiscovered = false;
        }
    }

    public class SpecialFOB
    {
        public string Name;
        public Vector3 Point;
        public string ClosestLocation;
        public ulong Team;
        public string UIColor;
        public bool IsActive;
        public bool DisappearAroundEnemies;

        public SpecialFOB(string name, Vector3 point, ulong team, string color, bool disappearAroundEnemies)
        {
            Name = name;
            ClosestLocation =
                (LevelNodes.nodes
                .Where(n => n.type == ENodeType.LOCATION)
                .Aggregate((n1, n2) =>
                    (n1.point - point).sqrMagnitude <= (n2.point - point).sqrMagnitude ? n1 : n2) as LocationNode)
                .name;
            Team = team;
            Point = point;
            UIColor = color;
            IsActive = true;
            DisappearAroundEnemies = disappearAroundEnemies;
        }
    }

    public class FOBConfig : ConfigData
    {
        public ushort Team1BuildID;
        public ushort Team2BuildID;
        public ushort Team1AmmoID;
        public ushort Team2AmmoID;
        public ushort FOBBaseID;
        public float FOBMaxHeightAboveTerrain;
        public bool RestrictFOBPlacement;
        public ushort FOBID;
        public ushort FOBRequiredBuild;
        public int FOBBuildPickupRadius;
        public byte FobLimit;

        public float AmmoCommandCooldown;
        public ushort AmmoCrateBaseID;
        public ushort AmmoCrateID;
        public ushort AmmoCrateRequiredBuild;
        public ushort RepairStationBaseID;
        public ushort RepairStationID;
        public ushort RepairStationRequiredBuild;
        public ushort MortarID;
        public ushort MortarBaseID;
        public ushort MortarRequiredBuild;
        public ushort MortarShellID;

        public List<Emplacement> Emplacements;
        public List<Fortification> Fortifications;
        public List<ushort> LogiTruckIDs;
        public List<ushort> AmmoBagIDs;
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

        public override void SetDefaults()
        {
            Team1BuildID = 38312;
            Team2BuildID = 38313;
            Team1AmmoID = 38314;
            Team2AmmoID = 38315;
            FOBBaseID = 38310;
            FOBMaxHeightAboveTerrain = 25f;
            RestrictFOBPlacement = true;
            FOBID = 38311;
            FOBRequiredBuild = 15;
            FOBBuildPickupRadius = 80;
            FobLimit = 10;

            AmmoCrateBaseID = 38316;
            AmmoCrateID = 38317;
            AmmoCrateRequiredBuild = 2;
            AmmoCommandCooldown = 0f;

            RepairStationBaseID = 38318;
            RepairStationID = 38319;
            RepairStationRequiredBuild = 6;

            LogiTruckIDs = new List<ushort>() { 38305, 38306, 38311, 38312 };
            AmmoBagIDs = new List<ushort>() { 38398 };
            AmmoBagMaxUses = 3;

            Fortifications = new List<Fortification>() {
                new Fortification
                {
                    base_id = 38350,
                    barricade_id = 38351,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38352,
                    barricade_id = 38353,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38354,
                    barricade_id = 38355,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38356,
                    barricade_id = 38357,
                    required_build = 2
                },
                new Fortification
                {
                    base_id = 38358,
                    barricade_id = 38359,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38360,
                    barricade_id = 38361,
                    required_build = 3
                },
                new Fortification
                {
                    base_id = 38362,
                    barricade_id = 38363,
                    required_build = 3
                }
            };

            Emplacements = new List<Emplacement>() {
                new Emplacement
                {
                    baseID = 38345,
                    vehicleID = 38316,
                    ammoID = 38302,
                    ammoAmount = 2,
                    requiredBuild = 4
                },
                new Emplacement
                {
                    baseID = 38346,
                    vehicleID = 38317,
                    ammoID = 38305,
                    ammoAmount = 2,
                    requiredBuild = 4
                },
                new Emplacement
                {
                    baseID = 38342,
                    vehicleID = 38315,
                    ammoID = 38341,
                    ammoAmount = 1,
                    requiredBuild = 8
                },
                new Emplacement
                {
                    baseID = 38339,
                    vehicleID = 38314,
                    ammoID = 38338,
                    ammoAmount = 1,
                    requiredBuild = 8
                },
                new Emplacement
                {
                    baseID = 38336,
                    vehicleID = 38313,
                    ammoID = 38330,
                    ammoAmount = 3,
                    requiredBuild = 6
                },
            };

            DeloyMainDelay = 3;
            DeloyFOBDelay = 10;

            DeployCancelOnMove = true;
            DeployCancelOnDamage = true;

            ShouldRespawnAtMain = true;
            ShouldSendPlayersBackToMainOnRoundEnded = true;
            ShouldWipeAllFOBsOnRoundedEnded = true;
            ShouldKillMaincampers = true;

            FirstFOBUiId = 36020;
            BuildResourceUI = 36090;
        }

        public FOBConfig() { }
    }

    public class Emplacement
    {
        public ushort vehicleID;
        public ushort baseID;
        public ushort ammoID;
        public ushort ammoAmount;
        public ushort requiredBuild;
    }

    public class Fortification
    {
        public ushort barricade_id;
        public ushort base_id;
        public ushort required_build;
    }
}
