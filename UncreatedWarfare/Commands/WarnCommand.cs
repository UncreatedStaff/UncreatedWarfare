﻿using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Commands
{
    class WarnCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "warn";

        public string Help => "Warn players who are misbehaving.";

        public string Syntax => "/warn <player> [reason]";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> { "uc.warn" };

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
                            F.LogError(F.Translate("warn_NoPlayerErrorText_Console", 0, command[0]));
                        else
                        {
                            if (command.Length == 1)
                                F.LogError(F.Translate("warn_ErrorNoReasonProvided_Console", 0));
                            else if (command.Length > 1)
                            {
                                string reason = command.MakeRemainder(1);
                                FPlayerName name = F.GetPlayerOriginalNames(player);
                                F.Log(F.Translate("warn_WarnedPlayerFromConsole_Console", 0, name.PlayerName, player.playerID.steamID.m_SteamID.ToString(), reason), ConsoleColor.Cyan);
                                if (UCWarfare.Config.AdminLoggerSettings.LogWarning)
                                    Data.WebInterface?.LogWarning(player.playerID.steamID.m_SteamID, Provider.server.m_SteamID, name.PlayerName, "Console", F.GetTeamByte(player), reason);
                                F.SendChat(player.playerID.steamID, "warn_WarnedPlayerFromConsole_DM", UCWarfare.GetColor("warn_message"), reason);
                                F.BroadcastToAllExcept(new List<CSteamID> { player.playerID.steamID }, "warn_WarnedPlayerFromConsole_Broadcast", UCWarfare.GetColor("warn_broadcast"), name.CharacterName);
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
                            F.SendChat(player, "warn_NoPlayerErrorText", UCWarfare.GetColor("defaulterror"), command[0]);
                        else
                        {
                            if (command.Length == 1)
                                F.SendChat(player, "warn_ErrorNoReasonProvided", UCWarfare.GetColor("defaulterror"));
                            else if (command.Length > 1)
                            {
                                string reason = command.MakeRemainder(1);
                                FPlayerName name = F.GetPlayerOriginalNames(steamplayer);
                                FPlayerName callerName = F.GetPlayerOriginalNames(player.Player);
                                F.Log(F.Translate("warn_WarnedPlayer_Console", 0, name.PlayerName, steamplayer.playerID.steamID.m_SteamID.ToString(), callerName.PlayerName, player.CSteamID.m_SteamID.ToString(), reason), 
                                    ConsoleColor.Cyan);
                                if (UCWarfare.Config.AdminLoggerSettings.LogWarning)
                                    Data.WebInterface?.LogWarning(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, name.PlayerName, callerName.PlayerName, F.GetTeamByte(steamplayer), reason);
                                F.SendChat(player, "warn_WarnedPlayer_Feedback", UCWarfare.GetColor("warn_feedback"), name.CharacterName);
                                F.SendChat(steamplayer.playerID.steamID, "warn_WarnedPlayer_DM", UCWarfare.GetColor("warn_message"), callerName.CharacterName, reason);
                                F.BroadcastToAllExcept(new List<CSteamID> { steamplayer.playerID.steamID, player.CSteamID }, "warn_WarnedPlayer_Broadcast", UCWarfare.GetColor("warn_broadcast"), name.CharacterName, callerName.CharacterName);
                            }
                        }
                    }
                }
            }
        }
    }
}