using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Networking;
using Uncreated.Players;

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
            if (!Dedicator.isDedicated)
                return;
            if (caller.DisplayName == "Console")
            {
                if (!Provider.isServer)
                    F.LogError(F.Translate("NotRunningErrorText", 0));
                else
                {
                    if (command.Length < 1)
                        F.LogError(F.Translate("InvalidParameterErrorText", 0));
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer player))
                            F.LogError(F.Translate("kick_NoPlayerErrorText_Console", 0, command[0]));
                        else
                        {
                            if (command.Length == 1)
                                F.LogError(F.Translate("kick_ErrorNoReasonProvided_Console", 0));
                            else if (command.Length > 1)
                            {
                                string reason = command.MakeRemainder(1);
                                FPlayerName names = F.GetPlayerOriginalNames(player);
                                Provider.kick(player.playerID.steamID, reason);
                                if (UCWarfare.Config.AdminLoggerSettings.LogKicks)
                                    Server.LogPlayerKicked(player.playerID.steamID.m_SteamID, Provider.server.m_SteamID, reason, DateTime.Now);
                                F.Log(F.Translate("kick_KickedPlayerFromConsole_Console", 0, names.PlayerName, player.playerID.steamID.m_SteamID.ToString(), reason), ConsoleColor.Cyan);
                                F.Broadcast("kick_KickedPlayerFromConsole_Broadcast", UCWarfare.GetColor("kick_broadcast"), names.PlayerName);
                            }
                        }
                    }
                }
            }
            else
            {
                UnturnedPlayer player = caller as UnturnedPlayer;
                if (!Provider.isServer)
                    F.SendChat(player, "NotRunningErrorText", UCWarfare.GetColor("defaulterror"));
                else
                {
                    if (command.Length < 1)
                        F.SendChat(player, "InvalidParameterErrorText", UCWarfare.GetColor("defaulterror"));
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer steamplayer))
                            F.SendChat(player, "kick_NoPlayerErrorText", UCWarfare.GetColor("defaulterror"), command[0]);
                        else
                        {
                            if (command.Length == 1)
                                F.SendChat(player, "kick_ErrorNoReasonProvided", UCWarfare.GetColor("defaulterror"));
                            else if (command.Length > 1)
                            {
                                string reason = command.MakeRemainder(1);
                                FPlayerName names = F.GetPlayerOriginalNames(steamplayer);
                                FPlayerName callerNames = F.GetPlayerOriginalNames(player.Player);
                                Provider.kick(steamplayer.playerID.steamID, reason);
                                if (UCWarfare.Config.AdminLoggerSettings.LogKicks)
                                    Server.LogPlayerKicked(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason, DateTime.Now);
                                F.LogWarning(F.Translate("kick_KickedPlayer_Console", 0, 
                                    names.PlayerName, steamplayer.playerID.steamID.m_SteamID.ToString(), callerNames.PlayerName, player.CSteamID.m_SteamID.ToString(), reason), ConsoleColor.Cyan);
                                F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "kick_KickedPlayer_Broadcast", UCWarfare.GetColor("kick_broadcast"), names.CharacterName, callerNames.CharacterName);
                                F.SendChat(player.CSteamID, "kick_KickedPlayer", UCWarfare.GetColor("kick_feedback"), names.CharacterName);
                            }
                        }
                    }
                }
            }
        }
    }
}