using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Uncreated.Warfare.Stats;
using Uncreated.Networking.Invocations;
using Uncreated.Players;

namespace Uncreated.Networking
{
    public static class Client
    {
        public static SendTask Send;
        #region INVOCATIONS
        // Identifying
        public static NetworkInvocation<string> IdentifyInvoc = 
            new NetworkInvocation<string>(ECall.IDENTIFY_TO_SERVER, Send);
        // Shutting down
        public static NetworkInvocation<ulong, string> ShuttingDownInvoc =
            new NetworkInvocation<ulong, string>(ECall.SERVER_SHUTTING_DOWN, Send);
        // Starting up
        public static NetworkInvocation<EStartupStep> StartingUpInvoc = 
            new NetworkInvocation<EStartupStep>(ECall.SERVER_STARTING_UP, Send);
        // Player List
        public static NetworkInvocationRaw<List<FPlayerName>> PlayerListInvoc =
            new NetworkInvocationRaw<List<FPlayerName>>(ECall.PLAYER_LIST, Send, 
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
            new NetworkInvocationRaw<FPlayerName>(ECall.PLAYER_JOINED, Send, 
                (byte[] arr, int index, out int size) => FPlayerName.FromBytes(arr, out size, index),
                (player) => player.GetBytes());
        // Player Left
        public static NetworkInvocationRaw<FPlayerName> PlayerLeftInvoc =
            new NetworkInvocationRaw<FPlayerName>(ECall.PLAYER_LEFT, Send,
                (byte[] arr, int index, out int size) => FPlayerName.FromBytes(arr, out size, index),
                (player) => player.GetBytes());
        // Username Updated
        public static NetworkInvocationRaw<FPlayerName> PlayerUpdatedUsernameInvoc =
            new NetworkInvocationRaw<FPlayerName>(ECall.USERNAME_UPDATED, Send,
                (byte[] arr, int index, out int size) => FPlayerName.FromBytes(arr, out size, index),
                (player) => player.GetBytes());
        // Banned Log
        public static NetworkInvocation<ulong, ulong, string, uint, DateTime> PlayerBannedInvoc =
            new NetworkInvocation<ulong, ulong, string, uint, DateTime>(ECall.LOG_BAN, Send);
        // Kicked Log
        public static NetworkInvocation<ulong, ulong, string, DateTime> PlayerKickedInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.LOG_KICK, Send);
        // BE Kicked Log
        public static NetworkInvocation<ulong, string, DateTime> PlayerBEKickedInvoc =
            new NetworkInvocation<ulong, string, DateTime>(ECall.LOG_BATTLEYEKICK, Send);
        // Teamkilled Log
        public static NetworkInvocation<ulong, ulong, ulong, string, DateTime> PlayerTeamkilledInvoc =
            new NetworkInvocation<ulong, ulong, ulong, string, DateTime>(ECall.LOG_TEAMKILL, Send);
        // Unbanned Log
        public static NetworkInvocation<ulong, ulong, DateTime> PlayerUnbannedInvoc =
            new NetworkInvocation<ulong, ulong, DateTime>(ECall.LOG_UNBAN, Send);
        // Warned Log
        public static NetworkInvocation<ulong, ulong, string, DateTime> PlayerWarnedInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.LOG_WARNING, Send);
        // On Duty
        public static NetworkInvocation<ulong, bool, DateTime> PlayerOnDutyInvoc = 
            new NetworkInvocation<ulong, bool, DateTime>(ECall.ON_DUTY, Send);
        // Off Duty
        public static NetworkInvocation<ulong, bool, DateTime> PlayerOffDutyInvoc = 
            new NetworkInvocation<ulong, bool, DateTime>(ECall.OFF_DUTY, Send);
        // Invoke Ban
        public static NetworkInvocation<ulong, ulong, string, uint, DateTime> InvokeBanInvoc =
            new NetworkInvocation<ulong, ulong, string, uint, DateTime>(ECall.INVOKE_BAN, Send);
        // Invoke Kick
        public static NetworkInvocation<ulong, ulong, string, DateTime> InvokeKickInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.INVOKE_KICK, Send);
        // Invoke Warning
        public static NetworkInvocation<ulong, ulong, string, DateTime> InvokeWarnInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.INVOKE_WARN, Send);
        // Invoke Unban
        public static NetworkInvocation<ulong, ulong, DateTime> InvokeUnbanInvoc =
            new NetworkInvocation<ulong, ulong, DateTime>(ECall.INVOKE_UNBAN, Send);
        // Invoke Give Kit
        public static NetworkInvocation<ulong, ulong, string, DateTime> InvokeGiveKitInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.INVOKE_GIVE_KIT, Send);
        // Invoke Revoke Kit
        public static NetworkInvocation<ulong, ulong, string, DateTime> InvokeRevokeKitInvoc =
            new NetworkInvocation<ulong, ulong, string, DateTime>(ECall.INVOKE_REVOKE_KIT, Send);
        // Invoke Instant Shutdown
        public static NetworkInvocation<ulong, string> InvokeShutdownInvoc =
            new NetworkInvocation<ulong, string>(ECall.INVOKE_SHUTDOWN, Send);
        // Invoke Shutdown After Current Game
        public static NetworkInvocation<ulong, string> InvokeShutdownAfterGameInvoc =
            new NetworkInvocation<ulong, string>(ECall.INVOKE_SHUTDOWN_AFTER_GAME, Send);
        // Invoke Set Officer Level
        public static NetworkInvocation<ulong, ulong, Warfare.Kits.EBranch, int, byte, DateTime> InvokeSetOfficerLevelInvoc =
            new NetworkInvocation<ulong, ulong, Warfare.Kits.EBranch, int, byte, DateTime>(ECall.INVOKE_SET_OFFICER_LEVEL, Send);
        // Invoke Server Reloading
        public static NetworkInvocation<ulong, string> InvokeServerReloadingInvoc =
            new NetworkInvocation<ulong, string>(ECall.SERVER_RELOADING, Send);
        #endregion
        public static async Task Identify() =>
            await IdentifyInvoc.Invoke(TCPClient.I.Identity);
        public static async Task SendShuttingDown(ulong admin, string reason) =>
            await ShuttingDownInvoc.Invoke(admin, reason);
        public static async Task SendStartingUp(EStartupStep step) =>
            await StartingUpInvoc.Invoke(step);
        public static async Task SendPlayerList(List<FPlayerName> players) =>
            await PlayerListInvoc.Invoke(players);
        public static async Task SendPlayerJoined(FPlayerName player) =>
            await PlayerJoinedInvoc.Invoke(player);
        public static async Task SendPlayerLeft(FPlayerName player) =>
            await PlayerLeftInvoc.Invoke(player);
        public static async Task SendPlayerUpdatedUsername(FPlayerName player) =>
            await PlayerUpdatedUsernameInvoc.Invoke(player);
        public static async Task LogPlayerBanned(ulong violator, ulong admin_id, string reason, uint duration, DateTime time) =>
            await PlayerBannedInvoc.Invoke(violator, admin_id, reason, duration, time);
        public static async Task LogPlayerKicked(ulong violator, ulong admin_id, string reason, DateTime time) =>
            await PlayerKickedInvoc.Invoke(violator, admin_id, reason, time);
        public static async Task LogPlayerBattleyeKicked(ulong violator, string reason, DateTime time) =>
            await PlayerBEKickedInvoc.Invoke(violator, reason, time);
        public static async Task LogPlayerTeamkilled(ulong violator, ulong dead, ulong landmine_assoc, string death_cause, DateTime time) =>
            await PlayerTeamkilledInvoc.Invoke(violator, dead, landmine_assoc, death_cause, time);
        public static async Task LogPlayerUnbanned(ulong pardoned, ulong admin_id, DateTime time) =>
            await PlayerUnbannedInvoc.Invoke(pardoned, admin_id, time);
        public static async Task LogPlayerWarned(ulong violator, ulong admin_id, string reason, DateTime time) =>
            await PlayerWarnedInvoc.Invoke(violator, admin_id, reason, time);
        public static async Task SendPlayerOnDuty(ulong player, bool intern) =>
            await PlayerOnDutyInvoc.Invoke(player, intern, DateTime.Now);
        public static async Task SendPlayerOffDuty(ulong player, bool intern) =>
            await PlayerOffDutyInvoc.Invoke(player, intern, DateTime.Now);
        public static async Task SendServerReloading(ulong admin, string reason) =>
            await InvokeServerReloadingInvoc.Invoke(admin, reason);
        public static async Task SendReloading(ulong admin, string reason) =>
            await InvokeServerReloadingInvoc.Invoke(admin, reason);


        internal static async Task ProcessResponse(byte[] message)
        {
            if (message.Length <= 0) return;
            ECall call;
            if (ByteMath.ReadUInt16(out ushort callid, 0, message))
            {
                call = (ECall)callid;
            }
            else
            {
                Warfare.F.LogError("Incorrect call enumerator given in response: " + message[0].ToString(Warfare.Data.Locale));
                return;
            }
            byte[] data = new byte[message.Length - sizeof(ushort)];
            Array.Copy(message, sizeof(ushort), data, 0, data.Length);
            switch (call)
            {
                case ECall.INVOKE_BAN:
                    InvokeBanInvoc.Read(data, out ulong banned, out ulong admin, out string reason, out uint duration, out DateTime timestamp);
                    await ReceiveInvokeBan(banned, admin, reason, duration, timestamp);
                    break;
            }
        }

        private static async Task ReceiveInvokeBan(ulong banned, ulong admin, string reason, uint duration, DateTime timestamp)
        {
            await Task.Yield(); // TODO
        }
    }
    public delegate Task EmptyTaskDelegate();
    public class TCPClient : IDisposable
    {
        public static readonly SendTask Send = async (arr) => { await SendMessageAsyncStatic(arr); };
        public static TCPClient I;
        public const int BufferSize = 4096;
        public string IP = "127.0.0.1";
        public int LocalID = 0;
        public ushort Port = 31902;
        public string Identity = "ucwarfare";
        public event SendTask OnReceivedData;
        public ClientConnection connection;
        public TCPClient(string ip, ushort port, string identitiy)
        {
            if (I == null)
            {
                I = this;
            }
            else if (I != this)
            {
                Warfare.F.LogWarning("Connection already established, resetting.", ConsoleColor.DarkYellow);
                I.Shutdown();
                GC.SuppressFinalize(I);
                I = this;
            }
            this.IP = ip;
            this.Port = port;
            this.connection = new ClientConnection(this);
            this.Identity = identitiy;
            Client.Send = Send;
        }
        public async Task Connect() => await connection?.Connect(true);
        public void Shutdown()
        {
            Warfare.F.Log("Shutting down", ConsoleColor.Magenta);
            connection.socket.Close();
            connection.socket.Dispose();
        }
        public async Task SendMessageAsync(byte[] message) => await connection?.SendMessage(message);
        public static async Task SendMessageAsyncStatic(byte[] message) => await I?.SendMessageAsync(message);
        internal async Task ReceiveData(byte[] data) => await OnReceivedData?.Invoke(data);
        public class ClientConnection
        {
            public TcpClient socket;
            private readonly TCPClient _owner;
            private NetworkStream stream;
            private byte[] _buffer;
            public ClientConnection(TCPClient owner)
            {
                this._owner = owner;
            }
            int connection_tries = 0;
            const int max_connection_tries = 10;
            public async Task Connect(bool first = true)
            {
                if (first) connection_tries = 0;
                if (socket != null)
                {
                    try
                    {
                        socket.Close();
                        socket.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Warfare.F.LogError(ex);
                    }
                }
                socket = new TcpClient()
                {
                    ReceiveBufferSize = BufferSize,
                    SendBufferSize = BufferSize
                };
                _buffer = new byte[BufferSize];

                connection_tries++;
                try
                {
                    await socket.ConnectAsync(_owner.IP, _owner.Port);
                    Warfare.F.Log($"Connected to {socket.Client.RemoteEndPoint}.", ConsoleColor.DarkYellow);
                    await Client.Identify();
                }
                catch (SocketException)
                {
                    if (connection_tries <= max_connection_tries)
                    {
                        Warfare.F.LogWarning($"Unable to connect, retrying ({connection_tries}/{max_connection_tries})", ConsoleColor.DarkYellow);
                        await Connect(false);
                    }
                    else Warfare.F.LogError($"Unable to connect after {max_connection_tries} tries.", ConsoleColor.Red);

                }
                if (!socket.Connected) return;
                stream = socket.GetStream();
                while (true)
                {
                    int received_bytes_count = await stream.ReadAsync(_buffer, 0, BufferSize);
                    if (received_bytes_count <= 0) _owner.Shutdown();
                    byte[] received = new byte[received_bytes_count];
                    Array.Copy(_buffer, 0, received, 0, received_bytes_count);
                    await _owner.ReceiveData(received);
                }
            }
            public async Task SendMessage(byte[] message)
            {
                try
                {
                    if (socket != null && socket.Connected)
                    {
                        if (stream == null) stream = socket.GetStream();
                        await stream.WriteAsync(message, 0, message.Length);
                    }
                }
                catch (SocketException)
                {
                    Warfare.F.LogError("Unable to write message.", ConsoleColor.Red);
                }
                catch (InvalidOperationException)
                {
                    Warfare.F.LogError("Unable to write message.", ConsoleColor.Red);
                }
            }
        }
        public void Dispose()
        {
            this.Shutdown();
            I = null;
            GC.SuppressFinalize(this);
        }
    }
    public enum ECall : ushort
    {
        IDENTIFY_TO_SERVER = 1,
        SERVER_SHUTTING_DOWN = 2,
        SERVER_STARTING_UP = 3,
        PLAYER_LIST = 4,
        PLAYER_JOINED = 5,
        PLAYER_LEFT = 6,
        USERNAME_UPDATED = 7,
        LOG_BAN = 8,
        LOG_KICK = 9,
        LOG_BATTLEYEKICK = 10,
        LOG_TEAMKILL = 11,
        LOG_UNBAN = 12,
        LOG_WARNING = 13,
        ON_DUTY = 14,
        OFF_DUTY = 15,
        INVOKE_BAN = 16,
        INVOKE_KICK = 17,
        INVOKE_WARN = 18,
        INVOKE_UNBAN = 19,
        INVOKE_GIVE_KIT = 20,
        INVOKE_REVOKE_KIT = 21,
        INVOKE_SHUTDOWN = 22,
        INVOKE_SHUTDOWN_AFTER_GAME = 23,
        INVOKE_SET_OFFICER_LEVEL = 24,
        SERVER_RELOADING = 25
    }
    public enum EStartupStep : byte
    {
        LOADING_PLUGIN = 0,
        PLUGINS_LOADED = 1,
        LEVEL_LOADED = 2
    }
}
