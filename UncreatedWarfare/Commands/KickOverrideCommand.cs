using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Commands
{
    class KickOverrideCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "kick";
        public string Help => "Kick players who are misbehaving.";
        public string Syntax => "/kick <player> <reason>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "uc.kick" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (caller.DisplayName == "Console")
            {
                if (!Provider.isServer)
                    L.LogError(Translation.Translate("server_not_running", 0, out _));
                else
                {
                    if (command.Length < 1)
                        L.LogError(Translation.Translate("kick_syntax", 0, out _));
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer player))
                            L.LogError(Translation.Translate("kick_no_player_found_console", 0, out _, command[0]));
                        else
                        {
                            if (command.Length == 1)
                                L.LogError(Translation.Translate("kick_no_reason_provided", 0, out _));
                            else if (command.Length > 1)
                            {
                                string reason = command.MakeRemainder(1);
                                FPlayerName names = F.GetPlayerOriginalNames(player);
                                Provider.kick(player.playerID.steamID, reason);
                                if (UCWarfare.Config.AdminLoggerSettings.LogKicks)
                                {
                                    Invocations.Shared.LogKicked.NetInvoke(player.playerID.steamID.m_SteamID, 0UL, reason, DateTime.Now);
                                    Data.DatabaseManager.AddKick(player.playerID.steamID.m_SteamID, 0, reason);
                                }
                                L.Log(Translation.Translate("kick_kicked_console_operator", 0, out _, names.PlayerName,
                                    player.playerID.steamID.m_SteamID.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                Chat.Broadcast("kick_kicked_broadcast_operator", names.PlayerName);
                            }
                        }
                    }
                }
            }
            else
            {
                UnturnedPlayer player = caller as UnturnedPlayer;
                if (!Provider.isServer)
                    Chat.SendChat(player, "server_not_running");
                else
                {
                    if (command.Length < 1)
                        Chat.SendChat(player, "kick_syntax");
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer steamplayer))
                            Chat.SendChat(player, "kick_no_player_found", command[0]);
                        else
                        {
                            if (command.Length == 1)
                                Chat.SendChat(player, "kick_no_reason_provided");
                            else if (command.Length > 1)
                            {
                                string reason = command.MakeRemainder(1);
                                FPlayerName names = F.GetPlayerOriginalNames(steamplayer);
                                FPlayerName callerNames = F.GetPlayerOriginalNames(player.Player);
                                Provider.kick(steamplayer.playerID.steamID, reason);
                                if (UCWarfare.Config.AdminLoggerSettings.LogKicks)
                                {
                                    Invocations.Shared.LogKicked.NetInvoke(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason, DateTime.Now);
                                    Data.DatabaseManager.AddKick(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason);
                                }
                                L.LogWarning(Translation.Translate("kick_kicked_console", 0, out _,
                                    names.PlayerName, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale),
                                    callerNames.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                Chat.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "kick_kicked_broadcast", names.CharacterName, callerNames.CharacterName);
                                Chat.SendChat(player.CSteamID, "kick_kicked_feedback", names.CharacterName);
                            }
                        }
                    }
                }
            }
        }
        public static void KickPlayer(ulong Violator, ulong Admin, string Reason)
        {
            SteamPlayer violator = PlayerTool.getSteamPlayer(Violator);
            SteamPlayer admin = PlayerTool.getSteamPlayer(Admin);
            FPlayerName callerName;
            if (admin == null)
                callerName = Data.DatabaseManager.GetUsernames(Admin);
            else
                callerName = F.GetPlayerOriginalNames(admin);
            FPlayerName names;
            if (violator == null)
                names = Data.DatabaseManager.GetUsernames(Violator);
            else
                names = F.GetPlayerOriginalNames(admin);
            if (violator == null)
            {
                SharedInvocations.PrintText.NetInvoke(DateTime.Now, "KICK: Player not found online", ConsoleColor.Red);
            }
            else
            {
                Provider.kick(violator.playerID.steamID, Reason);
                if (UCWarfare.Config.AdminLoggerSettings.LogKicks)
                {
                    Invocations.Shared.LogKicked.NetInvoke(Violator, Admin, Reason, DateTime.Now);
                    Data.DatabaseManager.AddKick(Violator, Admin, Reason);
                }
                L.LogWarning(Translation.Translate("kick_kicked_console", 0, out _,
                    names.PlayerName, Violator.ToString(Data.Locale),
                    callerName.PlayerName, Admin.ToString(Data.Locale), Reason), ConsoleColor.Cyan);
                Chat.BroadcastToAllExcept(new List<CSteamID> { admin == null ? new CSteamID(Admin) : admin.playerID.steamID }, "kick_kicked_broadcast", names.CharacterName, callerName.CharacterName);
                if (admin != null)
                    Chat.SendChat(admin, "kick_kicked_feedback", names.CharacterName);
            }
        }
    }
}