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
using Uncreated.Warfare.Teams;
using UnityEngine;
using Cache = Uncreated.Warfare.Components.Cache;

namespace Uncreated.Warfare.FOBs
{
    [Flags]
    public enum EFOBStatus : byte
    {
        RADIO = 0,
        AMMO_CRATE = 1,
        REPAIR_STATION = 2,
        HAB = 4
    }

    public class FOBManager
    {
        public static Config<FOBConfig> config;
        public static readonly List<FOB> Team1FOBs = new List<FOB>();
        public static readonly List<FOB> Team2FOBs = new List<FOB>();
        public static List<FOB> AllFOBs { get => Team1FOBs.Concat(Team2FOBs).ToList(); }
        public static readonly List<SpecialFOB> SpecialFOBs = new List<SpecialFOB>();
        public static readonly List<Cache> Caches = new List<Cache>();
        public static ushort fobListId;
        public const short fobListKey = 12008;
        public static ushort nearbyResourceId;
        public const short nearbyResourceKey = 12009;

        public static ushort GridSquareCount { get; private set; }
        public static ushort GridBorder { get; private set; }
        public static float GridSquareWidth { get; private set; }
        public static float GridScalingFactor { get; private set; }

        public static char[] GridLetters { get; private set; }

        public static void TempCacheEffectIDs()
        {
            if (Assets.find(Gamemode.Config.UI.FOBListGUID) is EffectAsset fobList)
                fobListId = fobList.id;
            if (Assets.find(Gamemode.Config.UI.NearbyResourcesGUID) is EffectAsset nbyrs)
                nearbyResourceId = nbyrs.id;
        }

        public FOBManager()
        {
            config = new Config<FOBConfig>(Data.FOBStorage, "config.json");

            if (Level.size == Level.MEDIUM_SIZE)
            {
                GridSquareCount = 12;
                GridBorder = 64;
            }
            else
            {
                GridSquareCount = 12;
                GridBorder = 64;
            }
        }
#if false
        public static string GetGridCoordsFromTexture(float textureX, float textureY, bool includeSubKey = false)
        {

            int xKey;
            int xSubIndex = 0;
            float upper = GridBorder;
            float lower = 0;

            for (xKey = 0; xKey < GridSquareCount; xKey++)
            {
                upper += GridSquareWidth;
                if (lower <= upper && textureX < upper)
                {
                    upper = upper - GridSquareWidth;
                    lower = upper;
                    for (xSubIndex = 0; xSubIndex < 3; xSubIndex++)
                    {
                        upper += GridSquareWidth / 3;
                        if (lower <= upper && textureX < upper)
                            break;
                        lower = upper;
                    }

                    break;
                }
                lower = upper;
            }

            int yKey;
            int ySubIndex = 0;
            upper = GridBorder;
            lower = 0;
            for (yKey = 1; yKey < GridSquareCount; yKey++)
            {
                upper += GridSquareWidth;
                if (lower <= upper && textureY < upper)
                {
                    upper = upper - GridSquareWidth;
                    lower = upper;
                    for (ySubIndex = 0; ySubIndex < 3; ySubIndex++)
                    {
                        upper += GridSquareWidth / 3;
                        if (lower <= upper && textureY < upper)
                            break;
                        lower = upper;
                    }

                    break;
                }
                lower = upper;
            }

            int subKey = (3 - ySubIndex) * 3 + (xSubIndex - 2);

            string gridCoord = GridLetters[xKey] + yKey.ToString();
            if (includeSubKey && 
                textureX >= GridBorder && 
                textureX <= Level.size - GridBorder &&
                textureY >= GridBorder &&
                textureY <= Level.size - GridBorder)
                gridCoord += "-" + subKey;

            return gridCoord;
        }

        public static string GetGridCoords(float xPos, float yPos, bool includeSubKey = false)
        {
            return GetGridCoordsFromTexture(xPos * GridScalingFactor + Level.size / 2, yPos * -GridScalingFactor + Level.size / 2, includeSubKey);
        }
#endif
        public static void Reset()
        {
            Team1FOBs.Clear();
            Team2FOBs.Clear();
            SpecialFOBs.Clear();
            Caches.Clear();
            SendFOBListToTeam(1);
            SendFOBListToTeam(2);
        }
        public static void OnLevelLoaded()
        {
            L.Log($"level size: {Level.size}");
            L.Log($"level border: {Level.border}");
            GridScalingFactor = Level.size / (Level.size - Level.border * 2);
            GridSquareWidth = (Level.size - Level.border * 2) / (float)GridSquareCount;
            L.Log($"grid square width: {GridSquareWidth}");
            GridLetters = new char[12] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L' };
        }
        public static void OnNewGameStarting()
        {
            Team1FOBs.Clear();
            Team2FOBs.Clear();
            SpecialFOBs.Clear();
            Caches.Clear();

            SendFOBListToTeam(1);
            SendFOBListToTeam(2);
        }
        /*
        public static void OnGameTick()
        {
            if (Data.Gamemode.EveryXSeconds(50f))
            {

            }
        }*/
        public static void OnPlayerDisconnect(UCPlayer player)
        {
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

        public static void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {
            if (Gamemode.Config.Barricades.FOBRadioGUIDs == null) return;
            if (data.barricade.asset.GUID == Gamemode.Config.Barricades.FOBGUID)
            {
                FOB fob = FOB.GetNearestFOB(data.point, EFOBRadius.SHORT, data.group);

                if (fob != null)
                {
                    fob.UpdateBunker(null);
                }

                SendFOBListToTeam(data.group);
            }
            if (drop.model.TryGetComponent(out FOBComponent f))
            {
                if (Gamemode.Config.Barricades.FOBRadioGUIDs.Any(g => g == data.barricade.asset.GUID))
                {
                    if (f.parent.IsWipedByAuthority)
                        f.parent.Destroy();
                    else
                        f.parent.StartBleed();
                }
                else if (data.barricade.asset.GUID == Gamemode.Config.Barricades.FOBRadioDamagedGUID)
                {
                    f.parent.Destroy();
                }

                SendFOBListToTeam(f.parent.Team);
            }
            else if (data.barricade.asset.GUID == Gamemode.Config.Barricades.InsurgencyCacheGUID)
            {
                DeleteCache(drop);
            }
            else if (data.barricade.asset.GUID == Gamemode.Config.Barricades.AmmoCrateGUID)
            {
                FOB fob = FOB.GetNearestFOB(data.point, EFOBRadius.SHORT, data.group);
                if (fob != null)
                {
                    fob.Status &= ~EFOBStatus.AMMO_CRATE;
                }
            }
            else if (data.barricade.asset.GUID == Gamemode.Config.Barricades.RepairStationGUID)
            {
                FOB fob = FOB.GetNearestFOB(data.point, EFOBRadius.SHORT, data.group);
                if (fob != null)
                {
                    fob.Status &= ~EFOBStatus.REPAIR_STATION;
                }
            }
        }

        public static void PrepareFOBsForWipe()
        {
            foreach (var fob in AllFOBs)
            {
                fob.IsWipedByAuthority = true;
            }
        }
        
        public static FOB RegisterNewFOB(BarricadeDrop drop)
        {

            FOB fob = new FOB(drop);
            
            if (fob.Owner != 0 && Data.Is(out IGameStats ws) && ws.GameStats is IFobsTracker ft)
            {
                if (fob.Owner.TryGetPlaytimeComponent(out PlaytimeComponent c) && c.stats is IFOBStats f)
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
                byte[] state = Convert.FromBase64String(config.data.T1RadioState);
                fob.Radio.GetServersideData().barricade.state = state;
                fob.Radio.ReceiveUpdateState(state);

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
                byte[] state = Convert.FromBase64String(config.data.T2RadioState);
                fob.Radio.GetServersideData().barricade.state = state;
                fob.Radio.ReceiveUpdateState(state);

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
            SendFOBListToTeam(fob.Team);
            return fob;
        }
        public static SpecialFOB RegisterNewSpecialFOB(string name, Vector3 point, ulong team, string UIcolor, bool disappearAroundEnemies)
        {
            SpecialFOB f = new SpecialFOB(name, point, team, UIcolor, disappearAroundEnemies);
            SpecialFOBs.Add(f);

            SendFOBListToTeam(team);
            return f;
        }
        public static Cache RegisterNewCache(BarricadeDrop drop)
        {
            if (Data.Is(out Insurgency insurgency))
            {
                Cache cache = drop.model.gameObject.AddComponent<Cache>();

                int number;
                List<Insurgency.CacheData> caches = insurgency.ActiveCaches;
                if (caches.Count == 0)
                    number = insurgency.CachesDestroyed + 1;
                else
                    number = caches.Last().Number + 1;

                cache.Number = number;
                cache.Name = "CACHE" + number;

                Caches.Add(cache);

                SendFOBListToTeam(cache.Team);

                return cache;
            }
            else
                return null;
        }
        public static void DeleteFOB(FOB fob)
        {
            ulong team = fob.Team;

            UCPlayer killer = UCPlayer.FromID(fob.Killer);
            ulong killerteam = 0;
            if (killer != null)
                killerteam = (ulong)(killer.GetTeam());

            ulong instanceID = fob.Radio.instanceID;

            FOB removed;
            if (team == 1)
            {
                removed = Team1FOBs.FirstOrDefault(x => x.Radio.instanceID == instanceID);
                Team1FOBs.RemoveAll(f => f.Radio.instanceID == instanceID);
            }
            else if (team == 2)
            {
                removed = Team2FOBs.FirstOrDefault(x => x.Radio.instanceID == instanceID);
                Team2FOBs.RemoveAll(f => f.Radio.instanceID == instanceID);
            }
            else removed = null;

            if (removed != null)
            {
                IEnumerator<PlaytimeComponent> pts = Data.PlaytimeComponents.Values.GetEnumerator();
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
                    if (killer.Player.TryGetPlaytimeComponent(out PlaytimeComponent c) && c.stats is IFOBStats f)
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
                        Points.AwardXP(killer, Points.XPConfig.FOBTeamkilledXP, Translation.Translate("xp_fob_teamkilled", killer));
                    }
                    else
                    {
                        Points.AwardXP(killer, Points.XPConfig.FOBKilledXP, Translation.Translate("xp_fob_killed", killer));

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
        public static void DeleteCache(BarricadeDrop cache)
        {
            if (!Data.Is(out Insurgency ins)) return;

            ulong team = cache.GetServersideData().group;

            UCPlayer killer = null;
            if (cache.model.TryGetComponent(out BarricadeComponent component))
                killer = UCPlayer.FromID(component.LastDamager);

            ulong instanceID = cache.instanceID;

            Cache removed = Caches.FirstOrDefault(x => x.Structure.instanceID == instanceID);
            Caches.RemoveAll(f => f.Structure.instanceID == instanceID);

            if (removed != null)
            {
                removed.Destroy();

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
                    Points.AwardXP(killer, Points.XPConfig.FOBTeamkilledXP, Translation.Translate("xp_fob_teamkilled", killer));
                }
                else
                {
                    Points.AwardXP(killer, Points.XPConfig.FOBKilledXP, Translation.Translate("xp_fob_killed", killer));
                    Stats.StatsManager.ModifyStats(killer.Steam64, x => x.FobsDestroyed++, false);
                    Stats.StatsManager.ModifyTeam(team, t => t.FobsDestroyed++, false);
                }
            }

            SendFOBListToTeam(team);
        }
        public static bool FindFOBByName(string name, ulong team, out object fob)
        {
            fob = SpecialFOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && f.Team == team);
            if (fob != null)
                return true;

            fob = Caches.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && f.Team == team);
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
            if (!Data.Is(out TeamGamemode gm)) return;
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                if (PlayerManager.OnlinePlayers[i].GetTeam() == team && !gm.JoinManager.IsInLobby(PlayerManager.OnlinePlayers[i]))
                    UpdateFOBList(PlayerManager.OnlinePlayers[i], fob);
            }
        }
        public static void UpdateFOBListForTeam(ulong team, FOB fob = null)
        {
            if (!Data.Is(out TeamGamemode gm)) return;
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                if (PlayerManager.OnlinePlayers[i].GetTeam() == team && !gm.JoinManager.IsInLobby(PlayerManager.OnlinePlayers[i]))
                    UpdateFOBList(PlayerManager.OnlinePlayers[i], fob);
            }
        }
        public static void UpdateFOBListForTeam(ulong team, Cache fob = null)
        {
            if (!Data.Is(out TeamGamemode gm)) return;
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                if (PlayerManager.OnlinePlayers[i].GetTeam() == team && !gm.JoinManager.IsInLobby(PlayerManager.OnlinePlayers[i]))
                    UpdateFOBList(PlayerManager.OnlinePlayers[i], fob);
            }
        }
        public static void SendFOBListToTeam(ulong team)
        {
            if (!Data.Is(out TeamGamemode gm)) return;
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                if (PlayerManager.OnlinePlayers[i].GetTeam() == team && !gm.JoinManager.IsInLobby(PlayerManager.OnlinePlayers[i]))
                    SendFOBList(PlayerManager.OnlinePlayers[i]);
            }
        }

        public static void HideFOBList(UCPlayer player)
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

            UpdateUIList(team, player.connection, FOBList, player);
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

            if (fob == null)
            {
                UpdateUIList(team, player.connection, FOBList, player);
            }
            else
            {
                int offset = SpecialFOBs.Count + Caches.Count;
                int i = FOBList.IndexOf(fob) + offset;
                if (i == -1)
                {
                    UpdateUIList(team, player.connection, FOBList, player);
                    return;
                }
                EffectManager.sendUIEffectText(fobListKey, player.connection, true, "N" + i.ToString(),
                    Translation.Translate("fob_ui", player.Steam64, FOBList[i].Name.Colorize(FOBList[i].NearbyEnemies.Count == 0 ? FOBList[i].UIColor : UCWarfare.GetColorHex("enemy_nearby_fob_color")), FOBList[i].GridCoordinates));
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
                EffectManager.sendUIEffectText(fobListKey, c, true, "N" + i.ToString(), Translation.Translate("fob_ui", player.Steam64, SpecialFOBs[i].Name.Colorize(SpecialFOBs[i].UIColor), SpecialFOBs[i].ClosestLocation));
            }
        }
        public static void UpdateFOBList(UCPlayer player, Cache cache = null)
        {
            List<FOB> FOBList;
            ulong team = player.GetTeam();
            if (team == 1)
                FOBList = Team1FOBs;
            else if (team == 2)
                FOBList = Team2FOBs;
            else return;
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            if (cache == null)
            {
                UpdateUIList(team, c, FOBList, player);
            }
            else
            {
                int offset = SpecialFOBs.Count;
                int i = Caches.IndexOf(cache) + offset;
                if (i == -1)
                {
                    UpdateUIList(team, c, FOBList, player);
                    return;
                }
                EffectManager.sendUIEffectText(fobListKey, c, true, "N" + i.ToString(), Translation.Translate("fob_ui", player.Steam64, Caches[i].Name.Colorize(Caches[i].UIColor), Caches[i].ClosestLocation));
            }
        }
        private static void UpdateUIList(ulong team, ITransportConnection c, List<FOB> FOBList, UCPlayer player)
        {
            HideFOBList(player); // TODO: remove this line and make the FOB list UI have all gameobjects disabled by default
            EffectManager.sendUIEffect(fobListId, fobListKey, true);

            int i2 = 0;
            int min = Math.Min(SpecialFOBs.Count, config.data.FobLimit);
            for (int i = 0; i < min; i++)
            {
                //L.LogDebug($"    s: {i}");
                if (SpecialFOBs[i].IsActive && SpecialFOBs[i].Team == team)
                {
                    string i22 = i2.ToString();
                    EffectManager.sendUIEffectVisibility(fobListKey, c, true, i22, true);
                    EffectManager.sendUIEffectText(fobListKey, c, true, "N" + i22, Translation.Translate("fob_ui", player.Steam64, SpecialFOBs[i].Name.Colorize(SpecialFOBs[i].UIColor), SpecialFOBs[i].ClosestLocation));
                    i2++;
                }
            }

            if (Data.Is<Insurgency>(out _))
            {
                min = Math.Min(Caches.Count, config.data.FobLimit);
                for (int i = 0; i < min; i++)
                {
                    //L.LogDebug($"    i: {i}");
                    string i22 = i2.ToString();
                    EffectManager.sendUIEffectVisibility(fobListKey, c, true, i22, true);
                    EffectManager.sendUIEffectText(fobListKey, c, true, "N" + i22, Translation.Translate("fob_ui", player.Steam64, Caches[i].Name.Colorize(Caches[i].UIColor), Caches[i].ClosestLocation));
                    i2++;
                }
            }

            min = Math.Min(FOBList.Count, config.data.FobLimit - i2);
            for (int i = 0; i < min; i++)
            {
                //L.LogDebug($"    f: {i}");
                string i22 = i2.ToString();

                EffectManager.sendUIEffectVisibility(fobListKey, c, true, i22, true);
                EffectManager.sendUIEffectText(fobListKey, c, true, "N" + i22,
                    Translation.Translate("fob_ui", player.Steam64, FOBList[i].Name.Colorize(FOBList[i].UIColor), FOBList[i].GridCoordinates));
                i2++;
            }
            for (; i2 < config.data.FobLimit; i2++)
            {
                //L.LogDebug($"    c: {i2}");
                EffectManager.sendUIEffectVisibility(fobListKey, c, true, i2.ToString(), false);
            }
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
            T2RadioState = "8yh8NQEAEAEAAAAAAAAAACIAAACmlQFkAAQAAKyVAWQAAAIArpUBZAAABQDGlQFkAAMFAMmVAWQABgUAyZUBZAAACACmZQFkAAMIAMCVAWQABggAwJUBZAAHAgDWlQFkAAoCANaVAWQABwMA1pUBZAAKAwDWlQFkAAcEANaVAWQACgQA1pUBZAAJBQDYlQFkAAkHANiVAWQACQkA2JUBZAAACwDOlQFkAAAMAM6VAWQAAA0AzpUBZAADCwDOlQFkAAcAAKyVAWQACgAA1pUBZAAKAQDWlQFkAAMMAM6VAWQAAw0AzpUBZAAGDQDQlQFkAAQCANqVAWQACQsA0JUBZAAJDADQlQFkAAkNANCVAWQABgsAzpUBZAAGDADOlQFkAA==";

            LogiTruckIDs = new Guid[4]
            { 
                new Guid("58d6410084f04e43ba4462a1c9a6b8c0"), // Logistics_Woodlands
                new Guid("fe1a85aeb8e34c2fbeca3e485300a61c"), // Logistics_Forest
                new Guid("6082d95b5fcb4805a7a2120e3e3c6f68"), // UH60_Blackhawk
                new Guid("18a6b283dbd245d0a13e0daa09b84aed")  // Mi8
            };
            AmmoBagMaxUses = 3;

            MortarBase = new Guid("6ff4826eaeb14c7cac1cf25a55d24bd3");

            Buildables = new List<BuildableData>()
            {
                new BuildableData
                {
                    structureID = new Guid("61c349f10000498fa2b92c029d38e523"),
                    foundationID = new Guid("1bb17277dd8148df9f4c53d1a19b2503"),
                    type = EBuildableType.FOB_BUNKER,
                    requiredHits = 40,
                    requiredBuild = 20,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    structureID = new Guid("6fe208519d7c45b0be38273118eea7fd"),
                    foundationID = new Guid("eccfe06e53d041d5b83c614ffa62ee59"),
                    type = EBuildableType.AMMO_CRATE,
                    requiredHits = 10,
                    requiredBuild = 2,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    structureID = new Guid("c0d11e0666694ddea667377b4c0580be"),
                    foundationID = new Guid("26a6b91cd1944730a0f28e5f299cebf9"),
                    type = EBuildableType.REPAIR_STATION,
                    requiredHits = 30,
                    requiredBuild = 15,
                    team = 0,
                    emplacementData = null
                },
                new BuildableData
                {
                    // sandbag line
                    structureID = new Guid("ab702192eab4456ebb9f6d7cc74d4ba2"),
                    foundationID = new Guid("15f674dcaf3f44e19a124c8bf7e19ca2"),
                    type = EBuildableType.FORTIFICATION,
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
                    type = EBuildableType.FORTIFICATION,
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
                    type = EBuildableType.FORTIFICATION,
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
                    type = EBuildableType.FORTIFICATION,
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
                    type = EBuildableType.FORTIFICATION,
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
                    type = EBuildableType.FORTIFICATION,
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
                    type = EBuildableType.FORTIFICATION,
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
                    type = EBuildableType.EMPLACEMENT,
                    requiredHits = 12,
                    requiredBuild = 10,
                    team = 1,
                    emplacementData = new EmplacementData
                    {
                        vehicleID = new Guid("aa3c6af4911243b5b5c9dc95ca1263bf"),
                        baseID = new Guid("80396c361d3040d7beb3921964ec2997"),
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
                    type = EBuildableType.EMPLACEMENT,
                    requiredHits = 12,
                    requiredBuild = 10,
                    team = 2,
                    emplacementData = new EmplacementData
                    {
                        vehicleID = new Guid("86cfe1eb8be144aeae7659c9c74ff11a"),
                        baseID = new Guid("e44ba62f763c432e882ddc7eabaa9c77"),
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
                    type = EBuildableType.EMPLACEMENT,
                    requiredHits = 20,
                    requiredBuild = 15,
                    team = 1,
                    emplacementData = new EmplacementData
                    {
                        vehicleID = new Guid("9d305050a6a142349376d6c49fb38362"),
                        baseID = new Guid("a68ae466fb804829a0eb0d4556071801"),
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
                    type = EBuildableType.EMPLACEMENT,
                    requiredHits = 20,
                    requiredBuild = 15,
                    team = 2,
                    emplacementData = new EmplacementData
                    {
                        vehicleID = new Guid("677b1084-dffa-4633-84d2-9167a3fae25b"),
                        baseID = new Guid("37811b1847744c958fcb30a0b759874b"),
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
                    type = EBuildableType.EMPLACEMENT,
                    requiredHits = 20,
                    requiredBuild = 10,
                    team = 0,
                    emplacementData = new EmplacementData
                    {
                        vehicleID = new Guid("94bf8feb05bc4680ac26464bc175460c"),
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
        public EBuildableType type;
        public int requiredHits;
        public int requiredBuild;
        public int team;
        public EmplacementData emplacementData;

        public static implicit operator Guid(BuildableData data) => data.structureID;
    }

    public class EmplacementData
    {
        public Guid vehicleID;
        public Guid baseID;
        public Guid ammoID;
        public int ammoAmount;
        public int allowedPerFob;
    }

    public enum EBuildableType
    {
        FOB_BUNKER,
        AMMO_CRATE,
        REPAIR_STATION,
        FORTIFICATION,
        EMPLACEMENT
    }
}
