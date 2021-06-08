using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static class EventFunctions
    {
        internal static void OnBarricadeDestroyed(BarricadeRegion region, BarricadeData data, BarricadeDrop drop, uint instanceID)
        {
            if (Data.OwnerComponents != null)
            {
                int c = Data.OwnerComponents.FindIndex(x => x.transform.position == data.point);
                if (c != -1)
                {
                    UnityEngine.Object.Destroy(Data.OwnerComponents[c]);
                    Data.OwnerComponents.RemoveAt(c);
                }
            }
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
        }
        internal static void OnLandmineExploded(InteractableTrap trap, Collider collider, BarricadeOwnerDataComponent owner, ref bool allow)
        {
            if (owner == default || owner.owner == default)
            {
                if (owner == default || owner.ownerID == 0) return;
                Data.DatabaseManager.GetUsernameAsync(owner.ownerID, LandmineExplodedUsernameReceived);
                return;
            }
            if (F.TryGetPlaytimeComponent(owner.owner.player, out PlaytimeComponent c))
                c.LastLandmineExploded = new LandmineDataForPostAccess(trap, owner);
            F.Log(owner.owner.playerID.playerName + "'s landmine exploded");
        }
        internal static void LandmineExplodedUsernameReceived(FPlayerName usernames, bool success)
        {
            if(success)
            {
                F.Log(usernames.PlayerName + "'s landmine exploded");
            }
        }
        internal static void ThrowableSpawned(UseableThrowable useable, GameObject throwable)
        {
            F.Log(useable == null ? "null" : useable.player.name + " - " + useable.equippedThrowableAsset.itemName);
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
        }
        internal static void OnPostHealedPlayer(Player instigator, Player target)
        {

        }
        internal static void OnPostPlayerConnected(UnturnedPlayer player)
        {
            F.Broadcast("player_connected", UCWarfare.GetColor("join_message_background"), player.Player.channel.owner.playerID.playerName, UCWarfare.GetColorHex("join_message_name"));
            Data.WebInterface?.SendPlayerJoinedAsync(player.Player.channel.owner);
            FPlayerName names;
            if (Data.OriginalNames.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                names = Data.OriginalNames[player.Player.channel.owner.playerID.steamID.m_SteamID];
            else names = new FPlayerName(player);
            Data.DatabaseManager?.UpdateUsernameAsync(player.Player.channel.owner.playerID.steamID.m_SteamID, names);
            Data.GameStats.AddPlayer(player.Player);
            if (Data.PlaytimeComponents.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
            {
                UnityEngine.Object.DestroyImmediate(Data.PlaytimeComponents[player.Player.channel.owner.playerID.steamID.m_SteamID]);
                Data.PlaytimeComponents.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
            player.Player.transform.gameObject.AddComponent<PlaytimeComponent>().StartTracking(player.Player);
            if (F.TryGetPlaytimeComponent(player.Player, out PlaytimeComponent c))
                Data.PlaytimeComponents.Add(player.Player.channel.owner.playerID.steamID.m_SteamID, c);
            F.AddPlayerStatsAndLogIn(player.Player.channel.owner.playerID.steamID.m_SteamID);

            player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.COSMETIC, false);
            player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.MYTHIC, false);
            player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.SKIN, false);

            LogoutSaver.InvokePlayerConnected(player);
            LogoutSave save = LogoutSaver.GetSave(player.CSteamID);
            if(save != default)
            {
                if (KitManager.KitExists(save.KitName, out Kit kit))
                    KitManager.GiveKit(player, kit);
                else
                {
                    byte team = F.GetTeamByte(player);
                    string unarmedKitName = team == 1 ? TeamManager.Team1UnarmedKit : (team == 2 ? TeamManager.Team2UnarmedKit : TeamManager.DefaultKit);
                    if (KitManager.KitExists(unarmedKitName, out Kit defKit))
                        KitManager.GiveKit(player, defKit);
                }
            }

            Data.FlagManager?.PlayerJoined(player.Player.channel.owner); // needs to happen last
        }
        internal static void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (Data.OriginalNames.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                Data.OriginalNames.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            F.Broadcast("player_disconnected", UCWarfare.GetColor("leave_message_background"), player.Player.channel.owner.playerID.playerName, UCWarfare.GetColorHex("leave_message_name"));
            Data.WebInterface?.SendPlayerLeftAsync(player.Player.channel.owner);
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
            LogoutSaver.InvokePlayerDisconnected(player);




            Data.FlagManager?.PlayerLeft(player.Player.channel.owner); // needs to happen last
        }
        internal static void LangCommand_OnPlayerChangedLanguage(object sender, Commands.PlayerChangedLanguageEventArgs e) => UCWarfare.I.UpdateLangs(e.player.Player.channel.owner);

        internal static void OnPrePlayerConnect(ValidateAuthTicketResponse_t callback, ref bool isValid, ref string explanation)
        {
            SteamPending player = Provider.pending.FirstOrDefault(x => x.playerID.steamID.m_SteamID == callback.m_SteamID.m_SteamID);
            if (player == default(SteamPending)) return;
            F.Log(player.playerID.playerName);
            if (Data.OriginalNames.ContainsKey(player.playerID.steamID.m_SteamID))
                Data.OriginalNames[player.playerID.steamID.m_SteamID] = new FPlayerName(player.playerID);
            else
                Data.OriginalNames.Add(player.playerID.steamID.m_SteamID, new FPlayerName(player.playerID));
            const string prefix = "[TEAM] ";
            if (!player.playerID.characterName.StartsWith(prefix))
                player.playerID.characterName = prefix + player.playerID.characterName;
            if (!player.playerID.nickName.StartsWith(prefix))
                player.playerID.nickName = prefix + player.playerID.nickName;
            // remove any "staff" from player's names.
            player.playerID.characterName = player.playerID.characterName.ReplaceCaseInsensitive("staff");
            player.playerID.nickName = player.playerID.nickName.ReplaceCaseInsensitive("staff");
        }
    }
}
