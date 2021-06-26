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
        public static Action<byte[]> Send;
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
                (arr) =>
            {
                if (ByteMath.ReadUInt8(out byte player_count, 0, arr))
                {
                    int index = 1;
                    List<Players.FPlayerName> players = new List<Players.FPlayerName>();
                    for (int i = 0; i < player_count; i++)
                    {
                        try
                        {
                            players.Add(FPlayerName.FromBytes(arr, out int length, index));
                            index += length;
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine($"Couldn't read FPlayerName: " + ex.Message);
                            return new List<FPlayerName>();
                        }
                    }
                    return players;
                }
                else return new List<FPlayerName>();
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
                (arr) => FPlayerName.FromBytes(arr, out _),
                (player) => player.GetBytes());
        // Player Left
        public static NetworkInvocationRaw<FPlayerName> PlayerLeftInvoc =
            new NetworkInvocationRaw<FPlayerName>(ECall.PLAYER_LEFT, Send, 
                (arr) => FPlayerName.FromBytes(arr, out _),
                (player) => player.GetBytes());
        // Username Updated
        public static NetworkInvocationRaw<FPlayerName> PlayerUpdatedUsernameInvoc =
            new NetworkInvocationRaw<FPlayerName>(ECall.USERNAME_UPDATED, Send, 
                (arr) => FPlayerName.FromBytes(arr, out _),
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
        public static void Identify() => 
            IdentifyInvoc.Invoke(TCPClient.I.Identity);
        public static void SendShuttingDown(ulong admin, string reason) =>
            ShuttingDownInvoc.Invoke(admin, reason);
        public static void SendStartingUp(EStartupStep step) =>
            StartingUpInvoc.Invoke(step);
        public static void SendPlayerList(List<FPlayerName> players) =>
            PlayerListInvoc.Invoke(players);
        public static void SendPlayerJoined(FPlayerName player) => 
            PlayerJoinedInvoc.Invoke(player);
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
        private static void InvokeBan()
        {

        }
        public static void SendReloading() =>
            TCPClient.I.SendMessageAsync(new byte[1] { (byte)ECall.SERVER_RELOADING });


        internal static void ProcessResponse(object sender, ReceivedServerMessageArgs e)
        {
            if (e.message.Length <= 0) return;
            ECall call;
            if (ByteMath.ReadUInt16(out ushort callid, 0, e.message))
            {
                call = (ECall)callid;
            }
            else
            {
                Warfare.F.LogError("Incorrect call enumerator given in response: " + e.message[0].ToString());
                return;
            }
            byte[] data = new byte[e.message.Length - sizeof(ushort)];
            Array.Copy(e.message, sizeof(ushort), data, 0, data.Length);
            switch (call)
            {
                case ECall.INVOKE_BAN:
                    InvokeBan();
                    break;
            }
        }
    }
    public class ReceivedServerMessageArgs : EventArgs
    {
        public byte[] message;
        public ReceivedServerMessageArgs(byte[] message)
        {
            this.message = message;
        }
    }
    public class TCPClient : IDisposable
    {
        public static readonly Action<byte[]> Send = (arr) => { SendMessageAsyncStatic(arr); };
        public static TCPClient I;
        public const int BufferSize = 4096;
        public string IP = "127.0.0.1";
        public int LocalID = 0;
        public ushort Port = 31902;
        public string Identity = "ucwarfare";
        public event EventHandler<ReceivedServerMessageArgs> OnReceivedData;
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
            Connect();
            Client.Send = Send;
        }
        public bool Connect()
        {
            if (connection != null)
            {
                connection.Connect(true);
                return true;
            }
            else return false;
        }
        public void Shutdown()
        {
            Warfare.F.Log("Shutting down", ConsoleColor.Magenta);
            connection.socket.Close();
            connection.socket.Dispose();
        }
        public void SendMessageAsync(byte[] message) => connection?.SendMessage(message);
        public static void SendMessageAsyncStatic(byte[] message) => I?.SendMessageAsync(message);
        internal void ReceiveData(byte[] data) => OnReceivedData?.Invoke(this, new ReceivedServerMessageArgs(data));
        public class ClientConnection
        {
            public TcpClient socket;
            private TCPClient _owner;
            private NetworkStream stream;
            private byte[] _buffer;
            public ClientConnection(TCPClient owner)
            {
                this._owner = owner;
            }
            int connection_tries = 0;
            const int max_connection_tries = 10;
            public void Connect(bool first = true)
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
                socket.BeginConnect(_owner.IP, _owner.Port, Connected, socket);
            }

            private void Connected(IAsyncResult ar)
            {
                connection_tries++;
                try
                {
                    socket.EndConnect(ar);
                    Warfare.F.Log($"Connected to {socket.Client.RemoteEndPoint}.", ConsoleColor.DarkYellow);
                    Client.Identify();
                }
                catch (SocketException)
                {
                    if (connection_tries <= max_connection_tries)
                    {
                        Warfare.F.LogWarning($"Unable to connect, retrying ({connection_tries}/{max_connection_tries})", ConsoleColor.DarkYellow);
                        Connect(false);
                    }
                    else Warfare.F.LogError($"Unable to connect after {max_connection_tries} tries.", ConsoleColor.Red);

                }
                if (!socket.Connected) return;
                stream = socket.GetStream();
                stream.BeginRead(_buffer, 0, BufferSize, AsyncReceivedData, socket);
            }
            public void SendMessage(byte[] message)
            {
                try
                {
                    if (socket != null && socket.Connected)
                    {
                        if (stream == null) stream = socket.GetStream();
                        stream.BeginWrite(message, 0, message.Length, WriteComplete, socket);
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
            protected virtual void WriteComplete(IAsyncResult ar)
            {
                try
                {
                    stream.EndWrite(ar);
                }
                catch (SocketException ex)
                {
                    Warfare.F.LogError(ex, ConsoleColor.Red);
                }
                ar.AsyncWaitHandle.Dispose();
            }

            private void AsyncReceivedData(IAsyncResult ar)
            {
                try
                {
                    int received_bytes_count = stream.EndRead(ar);
                    if (received_bytes_count <= 0) _owner.Shutdown();
                    byte[] received = new byte[received_bytes_count];
                    Array.Copy(_buffer, 0, received, 0, received_bytes_count);
                    _owner.ReceiveData(received);
                    stream.BeginRead(_buffer, 0, BufferSize, AsyncReceivedData, socket);
                }
                catch (SocketException)
                {
                    _owner.Shutdown();
                }
                catch (IOException)
                {
                    _owner.Shutdown();
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
