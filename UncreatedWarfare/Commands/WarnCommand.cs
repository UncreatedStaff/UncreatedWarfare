using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Commands
{
    public class WarnCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "warn";

        public string Help => "Warn players who are misbehaving.";

        public string Syntax => "/warn <player> <reason>";

        private readonly List<string> _aliases = new List<string>(0);
        public List<string> Aliases => _aliases;

        private readonly List<string> _permissions = new List<string>(1) { "uc.warn" };
		public List<string> Permissions => _permissions;

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (caller.DisplayName == "Console")
            {
                if (!Provider.isServer)
                    L.LogError(Translation.Translate("server_not_running", 0, out _));
                else
                {
                    if (command.Length < 1)
                        L.LogError(Translation.Translate("warn_syntax", 0, out _));
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer player))
                            L.LogError(Translation.Translate("warn_no_player_found_console", 0, out _, command[0]));
                        else
                        {
                            if (command.Length == 1)
                                L.LogError(Translation.Translate("warn_no_reason_provided", 0, out _));
                            else if (command.Length > 1)
                            {
                                string reason = command.MakeRemainder(1);
                                FPlayerName name = F.GetPlayerOriginalNames(player);
                                L.Log(Translation.Translate("warn_warned_console_operator", 0, out _,
                                    name.PlayerName, player.playerID.steamID.m_SteamID.ToString(), reason), ConsoleColor.Cyan);
                                if (UCWarfare.Config.AdminLoggerSettings.LogWarning)
                                {
                                    Data.DatabaseManager.AddWarning(player.playerID.steamID.m_SteamID, 0, reason);
                                    Invocations.Shared.LogWarned.NetInvoke(player.playerID.steamID.m_SteamID, 0UL, reason, DateTime.Now);
                                }
                                player.playerID.steamID.SendChat("warn_warned_private_operator", reason);
                                ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("warn_warned_private_operator", player, out _, reason), EToastMessageSeverity.WARNING));
                                Chat.BroadcastToAllExcept(new ulong[1] { player.playerID.steamID.m_SteamID }, "warn_warned_broadcast_operator", name.CharacterName);
                                ActionLog.Add(EActionLogType.WARN_PLAYER, $"WARNED {player.playerID.steamID.m_SteamID.ToString(Data.Locale)} FOR \"{reason}\"");
                            }
                        }
                    }
                }
            }
            else
            {
                UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
                if (player == null) return;
                if (!Provider.isServer)
                    player.SendChat("server_not_running");
                else
                {
                    if (command.Length < 1)
                        player.SendChat("warn_syntax");
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer steamplayer))
                            player.SendChat("warn_no_player_found", command[0]);
                        else
                        {
                            if (command.Length == 1)
                                player.SendChat("warn_no_reason_provided");
                            else if (command.Length > 1)
                            {
                                string reason = command.MakeRemainder(1);
                                FPlayerName name = F.GetPlayerOriginalNames(steamplayer);
                                FPlayerName callerName = F.GetPlayerOriginalNames(player.Player);
                                L.Log(Translation.Translate("warn_warned_console", 0, out _, name.PlayerName,
                                    steamplayer.playerID.steamID.m_SteamID.ToString(), callerName.PlayerName,
                                    player.CSteamID.m_SteamID.ToString(), reason),
                                    ConsoleColor.Cyan);
                                if (UCWarfare.Config.AdminLoggerSettings.LogWarning)
                                {
                                    Data.DatabaseManager.AddWarning(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason);
                                    Invocations.Shared.LogWarned.NetInvoke(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason, DateTime.Now);
                                }
                                player.SendChat("warn_warned_feedback", name.CharacterName);
                                ToastMessage.QueueMessage(steamplayer,
                                    new ToastMessage(Translation.Translate("warn_warned_private", player, out _, callerName.CharacterName, reason),
                                    EToastMessageSeverity.WARNING));
                                steamplayer.playerID.steamID.SendChat("warn_warned_private", callerName.CharacterName, reason);
                                ActionLog.Add(EActionLogType.WARN_PLAYER, $"WARNED {steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale)} FOR \"{reason}\"", player);
                                Chat.BroadcastToAllExcept(new ulong[2] { steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID }, "warn_warned_broadcast", name.CharacterName, callerName.CharacterName);
                            }
                        }
                    }
                }
            }
        }
        public static void WarnPlayer(ulong Violator, ulong Admin, string Reason)
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
                names = F.GetPlayerOriginalNames(Violator);
            if (violator != null)
            {
                L.Log(Translation.Translate("warn_warned_console" + (admin == null ? "_operator" : string.Empty), 0, out _, names.PlayerName,
                Violator.ToString(), callerName.PlayerName,
                Admin.ToString(), Reason),
                ConsoleColor.Cyan);
                if (UCWarfare.Config.AdminLoggerSettings.LogWarning)
                {
                    Data.DatabaseManager.AddWarning(Violator, Admin, Reason);
                    Invocations.Shared.LogWarned.NetInvoke(Violator, Admin, Reason, DateTime.Now);
                }
                if (admin != null)
                    admin.SendChat("warn_warned_feedback", names.CharacterName);
                ToastMessage.QueueMessage(violator,
                    new ToastMessage(Translation.Translate("warn_warned_private" + (admin == null ? "_operator" : string.Empty), Admin, out _, callerName.CharacterName, Reason),
                    EToastMessageSeverity.WARNING));
                violator.SendChat("warn_warned_private" + (admin == null ? "_operator" : string.Empty), callerName.CharacterName, Reason);
                Chat.BroadcastToAllExcept(new ulong[2] { violator.playerID.steamID.m_SteamID, Admin }, "warn_warned_broadcast" + (admin == null ? "_operator" : string.Empty), names.CharacterName, callerName.CharacterName);
            }
        }
    }
}