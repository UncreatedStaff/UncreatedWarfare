using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Networking.Invocations;
using Uncreated.Players;

namespace Uncreated.Networking
{

    public static class Client
    {
        public static SendTask Send { get => _send; }
        public static Action BeginRead { get => _beginRead; }
        public static Func<CancellationToken, ConfiguredTaskAwaitable<bool>> BeginReadCancellableAwaitable { get => _beginReadCancellableAwaitable; }
        public static SendTask _send;
        public static Action _beginRead;
        public static Func<CancellationToken, ConfiguredTaskAwaitable<bool>> _beginReadCancellableAwaitable;
        public static Dictionary<byte, KeyValuePair<bool, CancellationTokenSource>> Waits =
            new Dictionary<byte, KeyValuePair<bool, CancellationTokenSource>>();
        public static Dictionary<byte, SynchronizationContext> WaitLoopbacks = 
            new Dictionary<byte, SynchronizationContext>();
        #region INVOCATIONS
        // Confirm Received
        public static NetworkInvocation<byte> ReceivedInvoc =
            new NetworkInvocation<byte>(ECall.CONFIRM_RECEIVED);
        // Failed to Receive
        public static NetworkInvocation<byte> FailedToReceiveInvoc =
            new NetworkInvocation<byte>(ECall.TELL_FAILED_TO_READ);
        // Identifying
        public static NetworkInvocation<string> IdentifyInvoc =
            new NetworkInvocation<string>(ECall.IDENTIFY_TO_SERVER);
        // Shutting down
        public static NetworkInvocation<ulong, string> ShuttingDownInvoc =
            new NetworkInvocation<ulong, string>(ECall.SERVER_SHUTTING_DOWN);
        // Starting up
        public static NetworkInvocation<EStartupStep> StartingUpInvoc =
            new NetworkInvocation<EStartupStep>(ECall.SERVER_STARTING_UP);
        // Player List
        public static NetworkInvocationRaw<List<FPlayerName>> PlayerListInvoc =
            new NetworkInvocationRaw<List<FPlayerName>>(ECall.PLAYER_LIST,
                (byte[] arr, int index, out int size) =>
            {
                if (ByteMath.ReadUInt8(out byte player_count, 0, arr))
                {
                    size = index + 1;
                    List<FPlayerName> players = new List<Players.FPlayerName>();
                    for (int i = 0; i < player_count; i++)
                    {
                        try
                        {
                            players.Add(FPlayerName.FromBytes(arr, out int length, size));
                            size += length;
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine($"Couldn't read FPlayerName: " + ex.Message);
                            size = 1;
                            return new List<FPlayerName>();
                        }
                    }
                    return players;
                }
                else
                {
                    size = 0;
                    return new List<FPlayerName>();
                }
            },
                (players) =>
                {
                    if (players.Count > byte.MaxValue) return new byte[0];
                    List<byte> bytes = new List<byte> { (byte)players.Count };
                    for (int i = 0; i < players.Count; i++)
                        bytes.AddRange(players[i].GetBytes());
                    return bytes.ToArray();
                });
        // Player Joined
        public static NetworkInvocationRaw<FPlayerName> PlayerJoinedInvoc =
            new NetworkInvocationRaw<FPlayerName>(ECall.PLAYER_JOINED,
                (byte[] arr, int index, out int size) => FPlayerName.FromBytes(arr, out size, index),
                (player) => player.GetBytes());
        // Player Left
        public static NetworkInvocationRaw<FPlayerName> PlayerLeftInvoc =
            new NetworkInvocationRaw<FPlayerName>(ECall.PLAYER_LEFT,
                (byte[] arr, int index, out int size) => FPlayerName.FromBytes(arr, out size, index),
                (player) => player.GetBytes());
        // Username Updated
        public static NetworkInvocationRaw<FPlayerName> PlayerUpdatedUsernameInvoc =
            new NetworkInvocationRaw<FPlayerName>(ECall.USERNAME_UPDATED,
                (byte[] arr, int index, out int size) => FPlayerName.FromBytes(arr, out size, index),
                (player) => player.GetBytes());
        // Banned Log
        public static NetworkInvocation<ulong, ulong, string, uint, DateTime> PlayerBannedInvoc =
            new NetworkInvocation<ulong, ulong, string, uint, DateTime>(ECall.LOG_BAN);
        // Kicked Log
        public static NetworkInvocation<ulong, ulong, string, DateTime> PlayerKickedInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.LOG_KICK);
        // BE Kicked Log
        public static NetworkInvocation<ulong, string, DateTime> PlayerBEKickedInvoc =
            new NetworkInvocation<ulong, string, DateTime>(ECall.LOG_BATTLEYEKICK);
        // Teamkilled Log
        public static NetworkInvocation<ulong, ulong, ulong, string, DateTime> PlayerTeamkilledInvoc =
            new NetworkInvocation<ulong, ulong, ulong, string, DateTime>(ECall.LOG_TEAMKILL);
        // Unbanned Log
        public static NetworkInvocation<ulong, ulong, DateTime> PlayerUnbannedInvoc =
            new NetworkInvocation<ulong, ulong, DateTime>(ECall.LOG_UNBAN);
        // Warned Log
        public static NetworkInvocation<ulong, ulong, string, DateTime> PlayerWarnedInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.LOG_WARNING);
        // On Duty
        public static NetworkInvocation<ulong, bool, DateTime> PlayerOnDutyInvoc =
            new NetworkInvocation<ulong, bool, DateTime>(ECall.ON_DUTY);
        // Off Duty
        public static NetworkInvocation<ulong, bool, DateTime> PlayerOffDutyInvoc =
            new NetworkInvocation<ulong, bool, DateTime>(ECall.OFF_DUTY);
        // Invoke Ban
        public static NetworkInvocation<ulong, ulong, string, uint, DateTime> InvokeBanInvoc =
            new NetworkInvocation<ulong, ulong, string, uint, DateTime>(ECall.INVOKE_BAN);
        // Invoke Kick
        public static NetworkInvocation<ulong, ulong, string, DateTime> InvokeKickInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.INVOKE_KICK);
        // Invoke Warning
        public static NetworkInvocation<ulong, ulong, string, DateTime> InvokeWarnInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.INVOKE_WARN);
        // Invoke Unban
        public static NetworkInvocation<ulong, ulong, DateTime> InvokeUnbanInvoc =
            new NetworkInvocation<ulong, ulong, DateTime>(ECall.INVOKE_UNBAN);
        // Invoke Give Kit
        public static NetworkInvocation<ulong, ulong, string, DateTime> InvokeGiveKitInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.INVOKE_GIVE_KIT);
        // Invoke Revoke Kit
        public static NetworkInvocation<ulong, ulong, string, DateTime> InvokeRevokeKitInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.INVOKE_REVOKE_KIT);
        // Invoke Instant Shutdown
        public static NetworkInvocation<ulong, string> InvokeShutdownInvoc =
            new NetworkInvocation<ulong, string>(ECall.INVOKE_SHUTDOWN);
        // Invoke Shutdown After Current Game
        public static NetworkInvocation<ulong, string> InvokeShutdownAfterGameInvoc =
            new NetworkInvocation<ulong, string>(ECall.INVOKE_SHUTDOWN_AFTER_GAME);
        // Invoke Set Officer Level
        public static NetworkInvocation<ulong, ulong, Warfare.Kits.EBranch, int, byte, DateTime> InvokeSetOfficerLevelInvoc =
            new NetworkInvocation<ulong, ulong, Warfare.Kits.EBranch, int, byte, DateTime>(ECall.INVOKE_SET_OFFICER_LEVEL);
        // Invoke Server Reloading
        public static NetworkInvocation<ulong, string> InvokeServerReloadingInvoc =
            new NetworkInvocation<ulong, string>(ECall.SERVER_RELOADING);
        // Invoke Give Admin
        public static NetworkInvocation<ulong, ulong> InvokeGiveAdminInvoc =
            new NetworkInvocation<ulong, ulong>(ECall.GIVE_ADMIN);
        // Invoke Give Intern
        public static NetworkInvocation<ulong, ulong> InvokeGiveInternInvoc =
            new NetworkInvocation<ulong, ulong>(ECall.GIVE_INTERN);
        // Invoke Give Helper
        public static NetworkInvocation<ulong, ulong> InvokeGiveHelperInvoc =
            new NetworkInvocation<ulong, ulong>(ECall.GIVE_HELPER);
        // Invoke Revoke Admin
        public static NetworkInvocation<ulong, ulong> InvokeRevokeAdminInvoc =
            new NetworkInvocation<ulong, ulong>(ECall.REVOKE_ADMIN);
        // Invoke Revoke Intern
        public static NetworkInvocation<ulong, ulong> InvokeRevokeInternInvoc =
            new NetworkInvocation<ulong, ulong>(ECall.REVOKE_INTERN);
        // Invoke Revoke Helper
        public static NetworkInvocation<ulong, ulong> InvokeRevokeHelperInvoc =
            new NetworkInvocation<ulong, ulong>(ECall.REVOKE_HELPER);
        #endregion
        public static void ConfirmReceived(byte message_id) =>
            ReceivedInvoc.Invoke(message_id);
        public static void FailureToReceive(byte message_id) =>
            FailedToReceiveInvoc.Invoke(message_id);
        public static void Identify() =>
            IdentifyInvoc.Invoke("warfare");
        public static void SendShuttingDown(ulong admin, string reason) =>
            ShuttingDownInvoc.Invoke(admin, reason);
        public static void SendStartingUp(EStartupStep step) =>
            StartingUpInvoc.Invoke(step);
        public static void SendPlayerList(List<FPlayerName> players) =>
            PlayerListInvoc.Invoke(players);
        public static void SendPlayerJoined(FPlayerName player) =>
            PlayerJoinedInvoc?.Invoke(player);
        public static void SendPlayerLeft(FPlayerName player) =>
            PlayerLeftInvoc.Invoke(player);
        public static void SendPlayerUpdatedUsername(FPlayerName player) =>
            PlayerUpdatedUsernameInvoc.Invoke(player);
        public static void LogPlayerBanned(ulong violator, ulong admin_id, string reason, uint duration, DateTime time) =>
            PlayerBannedInvoc.Invoke(violator, admin_id, reason, duration, time);
        public static void LogPlayerKicked(ulong violator, ulong admin_id, string reason, DateTime time) =>
            PlayerKickedInvoc.Invoke(violator, admin_id, reason, time);
        public static void LogPlayerBattleyeKicked(ulong violator, string reason, DateTime time) =>
            PlayerBEKickedInvoc.Invoke(violator, reason, time);
        public static void LogPlayerTeamkilled(ulong violator, ulong dead, ulong landmine_assoc, string death_cause, DateTime time) =>
            PlayerTeamkilledInvoc.Invoke(violator, dead, landmine_assoc, death_cause, time);
        public static void LogPlayerUnbanned(ulong pardoned, ulong admin_id, DateTime time) =>
            PlayerUnbannedInvoc.Invoke(pardoned, admin_id, time);
        public static void LogPlayerWarned(ulong violator, ulong admin_id, string reason, DateTime time) =>
            PlayerWarnedInvoc.Invoke(violator, admin_id, reason, time);
        public static void SendPlayerOnDuty(ulong player, bool intern) =>
            PlayerOnDutyInvoc.Invoke(player, intern, DateTime.Now);
        public static void SendPlayerOffDuty(ulong player, bool intern) =>
            PlayerOffDutyInvoc.Invoke(player, intern, DateTime.Now);
        public static void SendServerReloading(ulong admin, string reason) =>
            InvokeServerReloadingInvoc.Invoke(admin, reason);
        public static void SendReloading(ulong admin, string reason) =>
            InvokeServerReloadingInvoc.Invoke(admin, reason);
    }
    public enum ECall : ushort
    {
        CONFIRM_RECEIVED = 0,
        TELL_FAILED_TO_READ = 1,
        IDENTIFY_TO_SERVER = 2,
        SERVER_SHUTTING_DOWN = 3,
        SERVER_STARTING_UP = 4,
        PLAYER_LIST = 5,
        PLAYER_JOINED = 6,
        PLAYER_LEFT = 7,
        USERNAME_UPDATED = 8,
        LOG_BAN = 9,
        LOG_KICK = 10,
        LOG_BATTLEYEKICK = 11,
        LOG_TEAMKILL = 12,
        LOG_UNBAN = 13,
        LOG_WARNING = 14,
        ON_DUTY = 15,
        OFF_DUTY = 16,
        INVOKE_BAN = 17,
        INVOKE_KICK = 18,
        INVOKE_WARN = 19,
        INVOKE_UNBAN = 20,
        INVOKE_GIVE_KIT = 21,
        INVOKE_REVOKE_KIT = 22,
        INVOKE_SHUTDOWN = 23,
        INVOKE_SHUTDOWN_AFTER_GAME = 24,
        INVOKE_SET_OFFICER_LEVEL = 25,
        SERVER_RELOADING = 26,
        GIVE_ADMIN = 27,
        GIVE_INTERN = 28,
        GIVE_HELPER = 29,
        REVOKE_ADMIN = 30,
        REVOKE_INTERN = 31,
        REVOKE_HELPER = 32,
        MEMBER_COUNT_TEST = 33,
        GET_MAP_NAME = 34
    }
    public enum EStartupStep : byte
    {
        LOADING_PLUGIN = 0,
        PLUGINS_LOADED = 1,
        LEVEL_LOADED = 2
    }
}
