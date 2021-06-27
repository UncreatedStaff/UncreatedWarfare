using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Flags;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.XP;
using UnityEngine;
using Flag = Uncreated.Warfare.Flags.Flag;

namespace Uncreated.Warfare
{
    public static class EventFunctions
    {
        public delegate void GroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup);
        public static event GroupChanged OnGroupChanged;
        internal static void OnGroupChangedInvoke(SteamPlayer player, ulong oldGroup, ulong newGroup) => OnGroupChanged?.Invoke(player, oldGroup, newGroup);
        internal static async void GroupChangedAction(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            ulong newteam = newGroup.GetTeam();

            PlayerManager.VerifyTeam(player.player);

            Data.FlagManager.ClearListUI(player.transportConnection);
            if (Data.FlagManager.OnFlag.ContainsKey(player.playerID.steamID.m_SteamID))
                Data.FlagManager.RefreshStaticUI(newteam, Data.FlagManager.FlagRotation.FirstOrDefault(x => x.ID == Data.FlagManager.OnFlag[player.playerID.steamID.m_SteamID])
                    ?? Data.FlagManager.FlagRotation[0]).SendToPlayer(player, player.transportConnection);
            Data.FlagManager.SendFlagListUI(player.transportConnection, player.playerID.steamID.m_SteamID, newteam);

            SquadManager.ClearUIsquad(player.player);
            SquadManager.UpdateUIMemberCount(newGroup);

            await XPManager.OnGroupChanged(player, oldGroup, newGroup);
            await OfficerManager.OnGroupChanged(player, oldGroup, newGroup);
            TicketManager.OnGroupChanged(player, oldGroup, newGroup);
        }
        internal static void OnBarricadeDestroyed(BarricadeRegion region, BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant, ushort index)
        {
            if (Data.OwnerComponents != null)
            {
                int c = Data.OwnerComponents.FindIndex(x => x != null && x.transform != default && data != default && x.transform.position == data.point);
                if (c != -1)
                {
                    UnityEngine.Object.Destroy(Data.OwnerComponents[c]);
                    Data.OwnerComponents.RemoveAt(c);
                }
            }
            FOBManager.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
            RallyManager.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
        }
        internal static void StopCosmeticsToggleEvent(ref EVisualToggleType type, SteamPlayer player, ref bool allow) 
        {
            if (!UCWarfare.Config.AllowCosmetics) allow = UnturnedPlayer.FromSteamPlayer(player).OnDuty();
        }
        internal static void StopCosmeticsSetStateEvent(ref EVisualToggleType type, SteamPlayer player, ref bool state, ref bool allow)
        {
            if (!UCWarfare.Config.AllowCosmetics && UnturnedPlayer.FromSteamPlayer(player).OffDuty()) state = false;
        }
        internal static void OnBarricadePlaced(BarricadeRegion region, BarricadeData data, ref Transform location)
        {
            F.Log("Placed barricade: " + data.barricade.asset.itemName + ", " + location.position.ToString());
            BarricadeOwnerDataComponent c = location.gameObject.AddComponent<BarricadeOwnerDataComponent>();
            c.SetData(data, region, location);
            Data.OwnerComponents.Add(c);
            RallyManager.OnBarricadePlaced(region, data, ref location);
        }
        internal static async void OnLandmineExploded(InteractableTrap trap, Collider collider, BarricadeOwnerDataComponent owner)
        {
            if (owner == default || owner.owner == default)
            {
                if (owner == default || owner.ownerID == 0) return;
                FPlayerName usernames = await Data.DatabaseManager.GetUsernames(owner.ownerID);
                F.Log(usernames.PlayerName + "'s landmine exploded");
                return;
            }
            if (F.TryGetPlaytimeComponent(owner.owner.player, out PlaytimeComponent c))
                c.LastLandmineExploded = new LandmineDataForPostAccess(trap, owner);
            F.Log(F.GetPlayerOriginalNames(owner.owner).PlayerName + "'s landmine exploded");
        }
        internal static void ThrowableSpawned(UseableThrowable useable, GameObject throwable)
        {
            ThrowableOwnerDataComponent t = throwable.AddComponent<ThrowableOwnerDataComponent>();
            PlaytimeComponent c = F.GetPlaytimeComponent(useable.player, out bool success);
            t.Set(useable, throwable, c);
            if (success)
                c.thrown.Add(t);
        }
        internal static void ProjectileSpawned(UseableGun gun, GameObject projectile)
        {
            PlaytimeComponent c = F.GetPlaytimeComponent(gun.player, out bool success);
            if (success)
            {
                c.lastProjected = gun.equippedGunAsset.id;
            }
        }
        internal static void BulletSpawned(UseableGun gun, BulletInfo bullet)
        {
            PlaytimeComponent c = F.GetPlaytimeComponent(gun.player, out bool success);
            if (success)
            {
                c.lastShot = gun.equippedGunAsset.id;
            }
        }
        internal static void ReloadCommand_onTranslationsReloaded(object sender, EventArgs e)
        {
            foreach (SteamPlayer player in Provider.clients)
                UCWarfare.I.UpdateLangs(player);
        }
        internal static void OnBarricadeTryPlaced(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angle_x,
            ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
        {
            if (hit != null && hit.transform.CompareTag("Vehicle"))
            {
                if (!UCWarfare.Config.AdminLoggerSettings.AllowedBarricadesOnVehicles.Contains(asset.id))
                {
                    UnturnedPlayer player = UnturnedPlayer.FromCSteamID(new CSteamID(owner));
                    if (player != null && player.OffDuty())
                    {
                        shouldAllow = false;
                        player.SendChat("no_placement_on_vehicle", UCWarfare.GetColor("defaulterror"), asset.itemName, asset.itemName.An());
                    }
                }
            }
            if (shouldAllow)
                RallyManager.OnBarricadePlaceRequested(barricade, asset, hit, ref point, ref angle_x, ref angle_y, ref angle_z, ref owner, ref group, ref shouldAllow);
        }
        internal static void OnPostHealedPlayer(Player instigator, Player target)
        {
            Data.ReviveManager.OnPlayerHealed(instigator, target);
        }
        internal static async void OnPostPlayerConnected(UnturnedPlayer player)
        {
            PlayerManager.InvokePlayerConnected(player); // must always be first

            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);

            F.Broadcast("player_connected", UCWarfare.GetColor("join_message_background"), player.Player.channel.owner.playerID.playerName, UCWarfare.GetColorHex("join_message_name"));
            FPlayerName names = F.GetPlayerOriginalNames(player);
            Client.SendPlayerJoined(names);
            await XPManager.OnPlayerJoined(ucplayer);
            await OfficerManager.OnPlayerJoined(ucplayer);
            //Data.DatabaseManager?.UpdateUsernameAsync(player.Player.channel.owner.playerID.steamID.m_SteamID, names);
            Data.GameStats.AddPlayer(player.Player);
            if (Data.PlaytimeComponents.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
            {
                UnityEngine.Object.DestroyImmediate(Data.PlaytimeComponents[player.Player.channel.owner.playerID.steamID.m_SteamID]);
                Data.PlaytimeComponents.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
            PlaytimeComponent pt = player.Player.transform.gameObject.AddComponent<PlaytimeComponent>();
            pt.StartTracking(player.Player);
            Data.PlaytimeComponents.Add(player.Player.channel.owner.playerID.steamID.m_SteamID, pt);
            pt.UCPlayer?.LogIn(player.Player.channel.owner, names);

            if (!UCWarfare.Config.AllowCosmetics)
            {
                player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.COSMETIC, false);
                player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.MYTHIC, false);
                player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.SKIN, false);
            }
            Data.ReviveManager.OnPlayerConnected(player);

            SquadManager.InvokePlayerJoined(ucplayer);
            
            TicketManager.OnPlayerJoined(ucplayer);

            Data.FlagManager?.PlayerJoined(player.Player.channel.owner); // needs to happen last
        }
        internal static void BatteryStolen(SteamPlayer theif, ref bool allow)
        {
            if (!UCWarfare.Config.AllowBatteryStealing)
            {
                allow = false;
                theif.SendChat("cant_steal_batteries", UCWarfare.GetColor("cant_steal_batteries"));
            }
        }
        internal static void OnCalculateSpawnDuringRevive(PlayerLife sender, bool wantsToSpawnAtHome, ref Vector3 position, ref float yaw)
        {
            ulong team = sender.player.GetTeam();
            position = team.GetBaseSpawnFromTeam();
            yaw = team.GetBaseAngle();
        }
        internal static async void OnPlayerDisconnected(UnturnedPlayer player)
        {
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);

            if (Data.OriginalNames.TryGetValue(player.Player.channel.owner.playerID.steamID.m_SteamID, out FPlayerName names))
            {
                Client.SendPlayerLeft(names);
                if (player.OnDuty())
                {
                    if (player.IsAdmin())
                        Commands.DutyCommand.AdminOnToOff(player, names);
                    else if (player.IsIntern())
                        Commands.DutyCommand.InternOnToOff(player, names);
                }
                Data.OriginalNames.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
            if (Data.OriginalNames.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
            F.Broadcast("player_disconnected", UCWarfare.GetColor("leave_message_background"), player.Player.channel.owner.playerID.playerName, UCWarfare.GetColorHex("leave_message_name"));
            if(UCWarfare.Config.RemoveLandminesOnDisconnect)
            {
                IEnumerable<BarricadeOwnerDataComponent> ownedTraps = Data.OwnerComponents.Where(x => x != null && x.ownerID == player.CSteamID.m_SteamID
               && x.barricade?.asset?.type == EItemType.TRAP);
                foreach (BarricadeOwnerDataComponent comp in ownedTraps.ToList())
                {
                    if (comp == null) continue;
                    if (BarricadeManager.tryGetInfo(comp.barricadeTransform, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion region))
                    {
                        BarricadeManager.destroyBarricade(region, x, y, plant, index);
                        F.Log($"Removed {player.DisplayName}'s {comp.barricade.asset.itemName} at {x}, {y}", ConsoleColor.Green);
                    }
                    UnityEngine.Object.Destroy(comp);
                    Data.OwnerComponents.Remove(comp);
                }
            }
            if (F.TryGetPlaytimeComponent(player.Player, out PlaytimeComponent c))
            {
                UnityEngine.Object.Destroy(c);
                Data.PlaytimeComponents.Remove(player.CSteamID.m_SteamID);
            }
            PlayerManager.InvokePlayerDisconnected(player);
            Data.ReviveManager.OnPlayerDisconnected(player);
            SquadManager.InvokePlayerLeft(ucplayer);
            Data.FlagManager?.PlayerLeft(player.Player.channel.owner); // needs to happen last
            await XPManager.OnPlayerLeft(ucplayer);
            await OfficerManager.OnPlayerLeft(ucplayer);
            TicketManager.OnPlayerLeft(ucplayer);
        }
        internal static void LangCommand_OnPlayerChangedLanguage(object sender, Commands.PlayerChangedLanguageEventArgs e) 
            => UCWarfare.I.UpdateLangs(e.player.Player.channel.owner);

        internal static void OnPrePlayerConnect(ValidateAuthTicketResponse_t ticket, ref bool isValid, ref string explanation)
        {
            SteamPending player = Provider.pending.FirstOrDefault(x => x.playerID.steamID.m_SteamID == ticket.m_SteamID.m_SteamID);
            if (player == default(SteamPending)) return;
            F.Log(player.playerID.playerName);
            if (Data.OriginalNames.ContainsKey(player.playerID.steamID.m_SteamID))
                Data.OriginalNames[player.playerID.steamID.m_SteamID] = new FPlayerName(player.playerID);
            else
                Data.OriginalNames.Add(player.playerID.steamID.m_SteamID, new FPlayerName(player.playerID));
            ulong team = F.GetTeamFromPlayerSteam64ID(player.playerID.steamID.m_SteamID);
            string prefix;
            if (team == 1) prefix = $"[{TeamManager.Team1Code.ToUpper()}]";
            else if (team == 2) prefix = $"[{TeamManager.Team2Code.ToUpper()}]";
            else prefix = "";
            if (team < 3 && team > 0 && !player.playerID.characterName.StartsWith(prefix))
                player.playerID.characterName = prefix + player.playerID.characterName;
            if (team < 3 && team > 0 && !player.playerID.nickName.StartsWith(prefix))
                player.playerID.nickName = prefix + player.playerID.nickName;
        }

        internal static async void OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            await XPManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
            await OfficerManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
        }
        internal static async void OnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            await XPManager.OnFlagNeutralized(flag, capturedTeam, lostTeam);
            await OfficerManager.OnFlagNeutralized(flag, capturedTeam, lostTeam);
        }
    }
}
