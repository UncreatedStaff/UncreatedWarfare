﻿using Newtonsoft.Json;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
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
        public static ushort fobListId;
        public const short fobListKey = 12008;
        public static ushort nearbyResourceId;
        public const short nearbyResourceKey = 12009;
        public static void TempCacheEffectIDs()
        {
            if (Assets.find(Gamemode.Config.UI.FOBListGUID) is EffectAsset fobList)
                fobListId = fobList.id;
            if (Assets.find(Gamemode.Config.UI.NearbyResourcesGUID) is EffectAsset nbyrs)
                nearbyResourceId = nbyrs.id;
            F.Log("Found fobs UIs: " + fobListId + ", " + nearbyResourceId);
        }

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
            SendFOBListToTeam(1);
            SendFOBListToTeam(2);
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

            SendFOBListToTeam(1);
            SendFOBListToTeam(2);
        }
        public static void OnItemDropped(Item item, Vector3 point)
        {
            if (!(Assets.find(EAssetType.ITEM, item.id) is ItemAsset asset)) return;
            ulong team;
            if (asset.GUID == Gamemode.Config.Items.T1Build)
                team = 1;
            else if (asset.GUID == Gamemode.Config.Items.T2Build)
                team = 2;
            else return;

            IEnumerable<FOB> fobs = GetFriendlyFOBs(team).Where(f => (f.Structure.model.position - point).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2));

            List<UCPlayer> alreadyCounted = new List<UCPlayer>();

            foreach (FOB fob in fobs)
            {
                foreach (UCPlayer player in fob.nearbyPlayers)
                {
                    if (!alreadyCounted.Contains(player))
                    {
                        alreadyCounted.Add(player);
                        UpdateBuildUI(player);
                    }

                }
            }

            IEnumerable<BarricadeDrop> foundations = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.FOBBaseGUID, 30, point, false);
            foreach (BarricadeDrop foundation in foundations)
            {
                if (foundation.model.TryGetComponent(out FOBBaseComponent component))
                {
                    foreach (UCPlayer player in component.nearbyPlayers)
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
            if (!(Assets.find(EAssetType.ITEM, itemData.item.id) is ItemAsset asset)) return;
            ulong team;
            if (asset.GUID == Gamemode.Config.Items.T1Build)
                team = 1;
            else if (asset.GUID == Gamemode.Config.Items.T2Build)
                team = 2;
            else return;

            IEnumerable<FOB> fobs = GetFriendlyFOBs(team).Where(f => (f.Structure.model.position - itemData.point).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2));

            List<UCPlayer> alreadyCounted = new List<UCPlayer>();

            foreach (FOB fob in fobs)
            {
                foreach (UCPlayer player in fob.nearbyPlayers)
                {
                    if (!alreadyCounted.Contains(player))
                    {
                        alreadyCounted.Add(player);
                        UpdateBuildUI(player);
                    }
                    
                }
            }

            IEnumerable<BarricadeDrop> foundations = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.FOBBaseGUID, 30, itemData.point, false);
            foreach (BarricadeDrop foundation in foundations)
            {
                if (foundation.model.TryGetComponent<FOBBaseComponent>(out var component))
                {
                    foreach (UCPlayer player in component.nearbyPlayers)
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
            IEnumerator<BarricadeDrop> repairStations = UCBarricadeManager.GetBarricadesByGUID(Gamemode.Config.Barricades.RepairStationGUID).GetEnumerator();
            IEnumerator<BarricadeDrop> ammoCrates = UCBarricadeManager.GetBarricadesByGUID(Gamemode.Config.Barricades.AmmoCrateGUID).GetEnumerator();

            while (repairStations.MoveNext())
            {
                if (F.IsInMain(repairStations.Current.model.transform.position))
                {
                    if (repairStations.Current.GetServersideData().group == 1)
                        UCBarricadeManager.TryAddItemToStorage(repairStations.Current, Gamemode.Config.Items.T1Build);
                    else if (repairStations.Current.GetServersideData().group == 2)
                        UCBarricadeManager.TryAddItemToStorage(repairStations.Current, Gamemode.Config.Items.T2Build);
                }
            }
            while (ammoCrates.MoveNext())
            {
                if (F.IsInMain(ammoCrates.Current.model.transform.position))
                {
                    if (ammoCrates.Current.GetServersideData().group == 1)
                        UCBarricadeManager.TryAddItemToStorage(ammoCrates.Current, Gamemode.Config.Items.T1Ammo);
                    else if (ammoCrates.Current.GetServersideData().group == 2)
                        UCBarricadeManager.TryAddItemToStorage(ammoCrates.Current, Gamemode.Config.Items.T2Ammo);
                }
            }
        }
        public static void UpdateBuildUI(UCPlayer player)
        {
            EffectManager.sendUIEffectText(nearbyResourceKey, player.connection, true,
                    "Build",
                    GetNearbyBuildForPlayer(player).Count.ToString()
                    );
        }
        public static void OnAmmoCrateUpdated(InteractableStorage storage, BarricadeDrop ammoCrate)
        {
            SDG.Unturned.BarricadeData data = ammoCrate.GetServersideData();
            IEnumerable<FOB> fobs = GetFriendlyFOBs(data.group).Where(f => (f.Structure.model.position - data.point).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2));

            List<UCPlayer> alreadyCounted = new List<UCPlayer>();

            foreach (FOB fob in fobs)
            {
                foreach (UCPlayer player in fob.nearbyPlayers)
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
            List<SDG.Unturned.ItemData> counted = new List<SDG.Unturned.ItemData>();
            ulong team = player.GetTeam();
            Guid BuildID;
            if (team == 1)
                BuildID = Gamemode.Config.Items.T1Build;
            else if (team == 2)
                BuildID = Gamemode.Config.Items.T2Build;
            else
                return counted;

            IEnumerable<FOB> nearbyFOBs = GetFriendlyFOBs(player.GetTeam()).Where(f => f.nearbyPlayers.Contains(player));
            IEnumerable<BarricadeDrop> nearbyFoundations = GetNearbyFriendlyFoundations(player.Position, player.GetTeam());

            foreach (BarricadeDrop foundation in nearbyFoundations)
            {
                foreach (SDG.Unturned.ItemData item in UCBarricadeManager.GetNearbyItems(BuildID, 30, foundation.model.position))
                {
                    if (!counted.Contains(item))
                    {
                        counted.Add(item);
                    }
                }
            }
            foreach (FOB fob in nearbyFOBs)
            {
                foreach (SDG.Unturned.ItemData item in UCBarricadeManager.GetNearbyItems(BuildID, config.Data.FOBBuildPickupRadius, fob.Structure.model.position))
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
            if (team == 1)
                return Team1FOBs;
            else if (team == 2)
                return Team2FOBs;
            else
                return new List<FOB>();
        }
        public static IEnumerable<BarricadeDrop> GetNearbyFriendlyFoundations(Vector3 origin, ulong team)
        {
            IEnumerable<BarricadeDrop> foundations = UCBarricadeManager.GetBarricadesByGUID(Gamemode.Config.Barricades.FOBBaseGUID);
            return UCBarricadeManager.GetNearbyBarricades(foundations, 30, origin, true).Where(d => d.GetServersideData().group == team);
        }
        public static void UpdateAmmoUI(UCPlayer player)
        {
            int ammoCount = 0;

            List<BarricadeDrop> alreadyCounted = new List<BarricadeDrop>();

            IEnumerable<FOB> nearbyFOBs = GetFriendlyFOBs(player.GetTeam()).Where(f => f.nearbyPlayers.Contains(player));

            foreach (FOB fob in nearbyFOBs)
            {
                if (fob.IsCache)
                {
                    if (fob.Structure.interactable is InteractableStorage cache)
                        ammoCount += CountAmmo(cache, player.GetTeam());
                }

                IEnumerable<BarricadeDrop> ammoCrates = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.AmmoCrateGUID, config.Data.FOBBuildPickupRadius, fob.Structure.model.position, true);

                foreach (BarricadeDrop ammoCrate in ammoCrates)
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

            EffectManager.sendUIEffectText(nearbyResourceKey, player.connection, true,
                   "Ammo",
                   ammoCount.ToString()
                   );
        }
        public static int CountAmmo(InteractableStorage storage, ulong team)
        {
            int ammoCount = 0;
            
            for (int i = 0; i < storage.items.items.Count; i++)
            {
                ItemJar jar = storage.items.items[i];
                if (!(Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset)) continue;
                if ((TeamManager.IsTeam1(team) && asset.GUID == Gamemode.Config.Items.T1Ammo) || (TeamManager.IsTeam2(team) && asset.GUID == Gamemode.Config.Items.T2Ammo))
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
        public static void OnPlayerDisconnect(UCPlayer player)
        {
            for (int i = 0; i < Team1FOBs.Count; i++)
            {
                for (int p = 0; p < Team1FOBs[i].nearbyPlayers.Count; p++)
                {
                    if (Team1FOBs[i].nearbyPlayers[p] == null)
                    {
                        Team1FOBs[i].nearbyPlayers.RemoveAt(p);
                    } 
                    else if (player.Steam64 == Team1FOBs[i].nearbyPlayers[p].Steam64)
                    {
                        Team1FOBs[i].nearbyPlayers.RemoveAt(p);
                        break;
                    }
                }
            }
            for (int i = 0; i < Team2FOBs.Count; i++)
            {
                for (int p = 0; p < Team2FOBs[i].nearbyPlayers.Count; p++)
                {
                    if (Team2FOBs[i].nearbyPlayers[p] == null)
                    {
                        Team2FOBs[i].nearbyPlayers.RemoveAt(p);
                    }
                    else if (player.Steam64 == Team2FOBs[i].nearbyPlayers[p].Steam64)
                    {
                        Team2FOBs[i].nearbyPlayers.RemoveAt(p);
                        break;
                    }
                }
            }
        }
        public static void Tick(FOB fob, int counter = -1)
        {
            for (int j = 0; j < PlayerManager.OnlinePlayers.Count; j++)
            {
                if (PlayerManager.OnlinePlayers[j].GetTeam() == fob.Structure.GetServersideData().group)
                {
                    if ((fob.Structure.model.position - PlayerManager.OnlinePlayers[j].Position).sqrMagnitude < config.Data.FOBBuildPickupRadius * config.Data.FOBBuildPickupRadius)
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
                    if ((fob.Structure.model.position - PlayerManager.OnlinePlayers[j].Position).sqrMagnitude < 81)
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
            }
        }
        public static void Tick(SpecialFOB special, int counter = -1)
        {
            if (special.DisappearAroundEnemies && counter % 4 == 0)
            {
                if (Provider.clients.Count(p => p.GetTeam() != special.Team && (p.player.transform.position - special.Point).sqrMagnitude < 400) > 0)
                {
                    DeleteSpecialFOB(special.Name, special.Team);
                }
            }
        }

        public static void OnEnteredFOBRadius(FOB fob, UCPlayer player)
        {
            EffectManager.sendUIEffect(nearbyResourceId, nearbyResourceKey, player.Player.channel.owner.transportConnection, true);

            UpdateBuildUI(player);
            UpdateAmmoUI(player);
        }
        public static void OnLeftFOBRadius(FOB fob, UCPlayer player)
        {
            List<FOB> FOBList = GetFriendlyFOBs(player.GetTeam());

            bool isAroundotherFOBs = false;
            foreach (FOB f in FOBList)
            {
                if (f.nearbyPlayers.Contains(player))
                {
                    isAroundotherFOBs = true;
                    break;
                }
            }

            if (!isAroundotherFOBs)
            {
                EffectManager.askEffectClearByID(nearbyResourceId, player.Player.channel.owner.transportConnection);
            }
            else
            {
                UpdateBuildUI(player);
                UpdateAmmoUI(player);
            }
        }

        public static void OnEnemyEnteredFOB(FOB fob, UCPlayer enemy)
        {
            UpdateFOBListForTeam(fob.Structure.GetServersideData().group.GetTeam(), fob);
        }
        public static void OnEnemyLeftFOB(FOB fob, UCPlayer enemy)
        {
            UpdateFOBListForTeam(fob.Structure.GetServersideData().group.GetTeam(), fob);
        }

        public static void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {

            if (data.barricade.asset.GUID == Gamemode.Config.Barricades.AmmoCrateGUID)
            {
                IEnumerable<BarricadeDrop> TotalFOBs = UCBarricadeManager.GetAllFobs().Where(f => f.GetServersideData().group == data.group);
                IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyBarricades(TotalFOBs, config.Data.FOBBuildPickupRadius, drop.model.position, true);

                BarricadeDrop first = NearbyFOBs.FirstOrDefault();
                if (first != null)
                {
                    IEnumerator<UCPlayer> nearbyPlayers = PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == data.group && !p.Player.life.isDead && (p.Position - first.model.position).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2)).GetEnumerator();
                    while (nearbyPlayers.MoveNext())
                    {
                        UpdateAmmoUI(nearbyPlayers.Current);
                    }
                    nearbyPlayers.Dispose();
                }
            }
            else if (data.barricade.asset.GUID == Gamemode.Config.Barricades.FOBGUID)
            {
                if (drop.model.TryGetComponent(out FOBBaseComponent component))
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

            string clr = UCWarfare.GetColorHex("default_fob_color");
            for (int i = 0; i < Team1FOBBarricades.Count; i++)
            {
                Team1FOBs.Add(new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Team1FOBBarricades[i], clr));
            }
            for (int i = 0; i < Team2FOBBarricades.Count; i++)
            {
                Team2FOBs.Add(new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Team2FOBBarricades[i], clr));
            }
            SendFOBListToTeam(1);
            SendFOBListToTeam(2);
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

                fob = new FOB("CACHE" + number.ToString(Data.Locale), number, Structure, color, true);
                
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
                if (Structure.GetServersideData().owner != 0 && Data.Is(out IGameStats ws) && ws.GameStats is IFobsTracker ft)
                {
                    if (F.TryGetPlaytimeComponent(Structure.GetServersideData().owner, out PlaytimeComponent c) && c.stats is IFOBStats f)
                        f.AddFOBPlaced();
                    if (team == 1)
                    {
                        ft.FOBsPlacedT1++;
                    }
                    else if (team == 2)
                    {
                        ft.FOBsPlacedT2++;
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

            SendFOBListToTeam(team);
            return fob;
        }
        public static SpecialFOB RegisterNewSpecialFOB(string name, Vector3 point, ulong team, string UIcolor, bool disappearAroundEnemies)
        {
            SpecialFOB f = new SpecialFOB(name, point, team, UIcolor, disappearAroundEnemies);
            SpecialFOBs.Add(f);

            SendFOBListToTeam(team);
            return f;
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
                        EffectManager.askEffectClearByID(nearbyResourceId, p.connection);
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
            Insurgency ins;
            UCPlayer ucplayer = UCPlayer.FromID(player);
            if (ucplayer == null)
            {
                if (Data.Is(out ins) && removed != null && removed.IsCache)
                    ins.OnCacheDestroyed(removed, ucplayer);
                return;
            }
            ulong killerteam = ucplayer.GetTeam();
            if (killerteam != 0 && killerteam != team && Data.Gamemode.State == EState.ACTIVE && Data.Is(out IGameStats w) && w.GameStats is IFobsTracker ft)
            // doesnt count destroying fobs after game ends
            {
                if (F.TryGetPlaytimeComponent(player, out PlaytimeComponent c) && c.stats is IFOBStats f)
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
            if (Data.Is(out ins) && removed != null && removed.IsCache)
                ins.OnCacheDestroyed(removed, ucplayer);
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
            SendFOBListToTeam(team);
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

            SendFOBListToTeam(team);
        }

        public static void GetRegionBarricadeLists(
                out List<BarricadeDrop> Team1Barricades,
                out List<BarricadeDrop> Team2Barricades
                )
        {
            IEnumerable<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>();

            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            Team1Barricades = barricadeDrops.Where(b =>
                b.GetServersideData().barricade.asset.GUID == Gamemode.Config.Barricades.FOBGUID &&   // All barricades that are FOB Structures
                TeamManager.IsTeam1(b.GetServersideData().group)        // All barricades that are friendly
                ).ToList();
            Team2Barricades = barricadeDrops.Where(b =>
                b.GetServersideData().barricade.asset.GUID == Gamemode.Config.Barricades.FOBGUID &&   // All barricades that are FOB Structures
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

        public static void UpdateFOBListForTeam(ulong team, SpecialFOB fob = null)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                if (PlayerManager.OnlinePlayers[i].GetTeam() == team)
                    UpdateFOBList(PlayerManager.OnlinePlayers[i], fob);
            }
        }
        public static void UpdateFOBListForTeam(ulong team, FOB fob = null)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                if (PlayerManager.OnlinePlayers[i].GetTeam() == team)
                    UpdateFOBList(PlayerManager.OnlinePlayers[i], fob);
            }
        }
        public static void SendFOBListToTeam(ulong team)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                if (PlayerManager.OnlinePlayers[i].GetTeam() == team)
                    SendFOBList(PlayerManager.OnlinePlayers[i]);
            }
        }

        public static void ClearFOBList(UCPlayer player)
        {
            EffectManager.askEffectClearByID(fobListId, player.Player.channel.owner.transportConnection);
        }
        public static void SendFOBList(UCPlayer player)
        {
            List<FOB> FOBList;
            ulong team = player.GetTeam();
            if (team == 1)
                FOBList = Team1FOBs;
            else if (team == 2)
                FOBList = Team2FOBs;
            else return;
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            UpdateUIList(team, c, FOBList, player);
        }
        public static void UpdateFOBList(UCPlayer player, FOB fob = null)
        {
            List<FOB> FOBList;
            ulong team = player.GetTeam();
            if (team == 1)
                FOBList = Team1FOBs;
            else if (team == 2)
                FOBList = Team2FOBs;
            else return;
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            if (fob == null)
            {
                UpdateUIList(team, c, FOBList, player);
            }
            else
            {
                int i = FOBList.IndexOf(fob);
                if (i == -1)
                {
                    UpdateUIList(team, c, FOBList, player);
                    return;
                }
                for (int i2 = 0; i2 < SpecialFOBs.Count; i2++)
                {
                    if (SpecialFOBs[i2].IsActive && SpecialFOBs[i2].Team == team)
                        i++;
                }
                EffectManager.sendUIEffectText(fobListKey, c, true, "N" + i.ToString(),
                    F.Translate("fob_ui", player.Steam64, FOBList[i].Name.Colorize(FOBList[i].nearbyEnemies.Count == 0 ? FOBList[i].UIColor : UCWarfare.GetColorHex("enemy_nearby_fob_color")), FOBList[i].ClosestLocation));
            }
        }
        public static void UpdateFOBList(UCPlayer player, SpecialFOB fob = null)
        {
            List<FOB> FOBList;
            ulong team = player.GetTeam();
            if (team == 1)
                FOBList = Team1FOBs;
            else if (team == 2)
                FOBList = Team2FOBs;
            else return;
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            if (fob == null)
            {
                UpdateUIList(team, c, FOBList, player);
            }
            else
            {
                int i = SpecialFOBs.IndexOf(fob);
                if (i == -1)
                {
                    UpdateUIList(team, c, FOBList, player);
                    return;
                }
                EffectManager.sendUIEffectText(fobListKey, c, true, "N" + i.ToString(), F.Translate("fob_ui", player.Steam64, SpecialFOBs[i].Name.Colorize(SpecialFOBs[i].UIColor), SpecialFOBs[i].ClosestLocation));
            }
        }
        private static void UpdateUIList(ulong team, ITransportConnection c, List<FOB> FOBList, UCPlayer player)
        {
            int i2 = 0;
            int min = Math.Min(SpecialFOBs.Count, config.Data.FobLimit);
            for (int i = 0; i < min; i++)
            {
                if (SpecialFOBs[i].IsActive && SpecialFOBs[i].Team == team)
                {
                    string i22 = i2.ToString();
                    EffectManager.sendUIEffectVisibility(fobListKey, c, true, i22, true);
                    EffectManager.sendUIEffectText(fobListKey, c, true, "N" + i22, F.Translate("fob_ui", player.Steam64, SpecialFOBs[i].Name.Colorize(SpecialFOBs[i].UIColor), SpecialFOBs[i].ClosestLocation));
                    i2++;
                }
            }
            min = Math.Min(FOBList.Count, config.Data.FobLimit - i2);
            for (int i = 0; i < min; i++)
            {
                string i22 = i2.ToString();
                EffectManager.sendUIEffectVisibility(fobListKey, c, true, i22, true);
                EffectManager.sendUIEffectText(fobListKey, c, true, "N" + i22,
                    F.Translate("fob_ui", player.Steam64, FOBList[i].Name.Colorize(FOBList[i].nearbyEnemies.Count == 0 ? FOBList[i].UIColor : UCWarfare.GetColorHex("enemy_nearby_fob_color")), FOBList[i].ClosestLocation));
                i2++;
            }
            for (; i2 < config.Data.FobLimit; i2++)
            {
                EffectManager.sendUIEffectVisibility(fobListKey, c, true, i2.ToString(), false);
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
        public Guid MortarBase;
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
        public Guid[] LogiTruckIDs;
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
            FOBMaxHeightAboveTerrain = 25f;
            RestrictFOBPlacement = true;
            FOBRequiredBuild = 15;
            FOBBuildPickupRadius = 80;
            FobLimit = 10;

            AmmoCrateRequiredBuild = 2;
            AmmoCommandCooldown = 0f;

            RepairStationRequiredBuild = 6;

            LogiTruckIDs = new Guid[4]
            { 
                new Guid("58d6410084f04e43ba4462a1c9a6b8c0"), // Logistics_Woodlands
                new Guid("fe1a85aeb8e34c2fbeca3e485300a61c"), // Logistics_Forest
                new Guid("6082d95b5fcb4805a7a2120e3e3c6f68"), // UH60_Blackhawk
                new Guid("18a6b283dbd245d0a13e0daa09b84aed") // Mi8
            };
            AmmoBagMaxUses = 3;

            MortarBase = new Guid("6ff4826eaeb14c7cac1cf25a55d24bd3");

            Buildables = new List<BuildableData>()
            {
                new BuildableData
                {
                    structureID = new Guid("61c349f10000498fa2b92c029d38e523"),
                    foundationID = new Guid("1bb17277dd8148df9f4c53d1a19b2503"),
                    type = EbuildableType.FOB,
                    requiredHits = 40,
                    requiredBuild = 20,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    structureID = new Guid("6fe208519d7c45b0be38273118eea7fd"),
                    foundationID = new Guid("eccfe06e53d041d5b83c614ffa62ee59"),
                    type = EbuildableType.AMMO_CRATE,
                    requiredHits = 8,
                    requiredBuild = 20,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    structureID = new Guid("c0d11e0666694ddea667377b4c0580be"),
                    foundationID = new Guid("26a6b91cd1944730a0f28e5f299cebf9"),
                    type = EbuildableType.AMMO_CRATE,
                    requiredHits = 30,
                    requiredBuild = 1,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    // sandbag line
                    structureID = new Guid("ab702192eab4456ebb9f6d7cc74d4ba2"),
                    foundationID = new Guid("15f674dcaf3f44e19a124c8bf7e19ca2"),
                    type = EbuildableType.FORTIFICATION,
                    requiredHits = 8,
                    requiredBuild = 1,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    // sandbag pillbox
                    structureID = new Guid("f3bd9ee2fa334faabc8fd9d5a3b84424"),
                    foundationID = new Guid("a9294335d8e84b76b1cbcb7d70f66aaa"),
                    type = EbuildableType.FORTIFICATION,
                    requiredHits = 8,
                    requiredBuild = 1,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    // sandbag crescent
                    structureID = new Guid("eefee76f077349e58359f5fd03cf311d"),
                    foundationID = new Guid("920f8b30ae314406ab032a0c2efa753d"),
                    type = EbuildableType.FORTIFICATION,
                    requiredHits = 8,
                    requiredBuild = 1,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    // sandbag foxhole
                    structureID = new Guid("a71e3e3d6bb54a36b7bd8bf5f25160aa"),
                    foundationID = new Guid("12ea830dd9ab4f949893bbbbc5e9a5f6"),
                    type = EbuildableType.FORTIFICATION,
                    requiredHits = 12,
                    requiredBuild = 2,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    // razorwire
                    structureID = new Guid("bc24bd85ff714ff7bb2f8b2dd5056395"),
                    foundationID = new Guid("a2a8a01a58454816a6c9a047df0558ad"),
                    type = EbuildableType.FORTIFICATION,
                    requiredHits = 8,
                    requiredBuild = 1,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    // hesco wall
                    structureID = new Guid("e1af3a3af31e4996bc5d6ffd9a0773ec"),
                    foundationID = new Guid("baf23a8b514441ee8db891a3ddf32ef4"),
                    type = EbuildableType.FORTIFICATION,
                    requiredHits = 20,
                    requiredBuild = 1,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    // hesco tower
                    structureID = new Guid("857c85161f254964a921700a69e215a9"),
                    foundationID = new Guid("827d0ca8bfff43a39f750f191e16ea71"),
                    type = EbuildableType.FORTIFICATION,
                    requiredHits = 20,
                    requiredBuild = 1,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    // M2A1
                    structureID = new Guid("aa3c6af4911243b5b5c9dc95ca1263bf"),
                    foundationID = new Guid("80396c361d3040d7beb3921964ec2997"),
                    type = EbuildableType.EMPLACEMENT,
                    requiredHits = 12,
                    requiredBuild = 10,
                    team = 1,
                    emplacementData = new EmplacementData
                    {
                        baseID = Guid.Empty,
                        ammoID = new Guid("523c49ce4df44d46ba37be0dd6b4504b"),
                        ammoAmount = 2,
                        allowedPerFob = 2
                    }
                },
                new BuildableData
                {
                    // Kord
                    structureID = new Guid("86cfe1eb8be144aeae7659c9c74ff11a"),
                    foundationID = new Guid("e44ba62f763c432e882ddc7eabaa9c77"),
                    type = EbuildableType.EMPLACEMENT,
                    requiredHits = 12,
                    requiredBuild = 10,
                    team = 2,
                    emplacementData = new EmplacementData
                    {
                        baseID = Guid.Empty,
                        ammoID = new Guid("6e9bc2083a1246b49b1656c2ec6f535a"),
                        ammoAmount = 2,
                        allowedPerFob = 2
                    }
                },
                new BuildableData
                {
                    // TOW
                    structureID = new Guid("9d305050a6a142349376d6c49fb38362"),
                    foundationID = new Guid("a68ae466fb804829a0eb0d4556071801"),
                    type = EbuildableType.EMPLACEMENT,
                    requiredHits = 20,
                    requiredBuild = 15,
                    team = 1,
                    emplacementData = new EmplacementData
                    {
                        baseID = Guid.Empty,
                        ammoID = new Guid("3128a69d06ac4bbbbfddc992aa7185a6"),
                        ammoAmount = 1,
                        allowedPerFob = 1
                    }
                },
                new BuildableData
                {
                    // Kornet
                    structureID = new Guid("677b1084dffa463384d29167a3fae25b"),
                    foundationID = new Guid("37811b1847744c958fcb30a0b759874b"),
                    type = EbuildableType.EMPLACEMENT,
                    requiredHits = 20,
                    requiredBuild = 15,
                    team = 2,
                    emplacementData = new EmplacementData
                    {
                        baseID = Guid.Empty,
                        ammoID = new Guid("d7774b017c404adbb0a0fe8e902b9689"),
                        ammoAmount = 1,
                        allowedPerFob = 1
                    }
                },
                new BuildableData
                {
                    // Mortar
                    structureID = new Guid("94bf8feb05bc4680ac26464bc175460c"),
                    foundationID = new Guid("6ff4826eaeb14c7cac1cf25a55d24bd3"),
                    type = EbuildableType.EMPLACEMENT,
                    requiredHits = 20,
                    requiredBuild = 10,
                    team = 0,
                    emplacementData = new EmplacementData
                    {
                        baseID = new Guid("c3eb4dd3fd1d463993ec69c4c3de50d7"), // Mortar
                        ammoID = new Guid("66f4c76a119e4d6ca9d0b1a866c4d901"),
                        ammoAmount = 3,
                        allowedPerFob = 2
                    }
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
        }

        public FOBConfig() { }
    }

    public class BuildableData
    {
        public Guid foundationID;
        public Guid structureID;
        public EbuildableType type;
        public int requiredHits;
        public int requiredBuild;
        public int team;
        public EmplacementData emplacementData;
    }

    public class EmplacementData
    {
        public Guid vehicleID;
        public Guid baseID;
        public Guid ammoID;
        public int ammoAmount;
        public int allowedPerFob;
    }

    public enum EbuildableType
    {
        FOB,
        AMMO_CRATE,
        REPAIR_STATION,
        FORTIFICATION,
        EMPLACEMENT
    }
}
