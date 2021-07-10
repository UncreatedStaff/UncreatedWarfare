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
using System.Runtime.CompilerServices;

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
        public static async Task ConfirmReceived(byte message_id) =>
            await ReceivedInvoc.Invoke(message_id);
        public static async Task FailureToReceive(byte message_id) =>
            await FailedToReceiveInvoc.Invoke(message_id);
        public static async Task Identify() =>
            await IdentifyInvoc.InvokeAndWaitAsync(TCPClient.I.Identity);
        public static async Task SendShuttingDown(ulong admin, string reason) =>
            await ShuttingDownInvoc.InvokeAndWaitAsync(admin, reason);
        public static async Task SendStartingUp(EStartupStep step) =>
            await StartingUpInvoc.InvokeAndWaitAsync(step);
        public static async Task SendPlayerList(List<FPlayerName> players) =>
            await PlayerListInvoc.InvokeAndWaitAsync(players);
        public static async Task SendPlayerJoined(FPlayerName player) =>
            await PlayerJoinedInvoc?.InvokeAndWaitAsync(player);
        public static async Task SendPlayerLeft(FPlayerName player) =>
            await PlayerLeftInvoc.InvokeAndWaitAsync(player);
        public static async Task SendPlayerUpdatedUsername(FPlayerName player) =>
            await PlayerUpdatedUsernameInvoc.InvokeAndWaitAsync(player);
        public static async Task LogPlayerBanned(ulong violator, ulong admin_id, string reason, uint duration, DateTime time) =>
            await PlayerBannedInvoc.InvokeAndWaitAsync(violator, admin_id, reason, duration, time);
        public static async Task LogPlayerKicked(ulong violator, ulong admin_id, string reason, DateTime time) =>
            await PlayerKickedInvoc.InvokeAndWaitAsync(violator, admin_id, reason, time);
        public static async Task LogPlayerBattleyeKicked(ulong violator, string reason, DateTime time) =>
            await PlayerBEKickedInvoc.InvokeAndWaitAsync(violator, reason, time);
        public static async Task LogPlayerTeamkilled(ulong violator, ulong dead, ulong landmine_assoc, string death_cause, DateTime time) =>
            await PlayerTeamkilledInvoc.InvokeAndWaitAsync(violator, dead, landmine_assoc, death_cause, time);
        public static async Task LogPlayerUnbanned(ulong pardoned, ulong admin_id, DateTime time) =>
            await PlayerUnbannedInvoc.InvokeAndWaitAsync(pardoned, admin_id, time);
        public static async Task LogPlayerWarned(ulong violator, ulong admin_id, string reason, DateTime time) =>
            await PlayerWarnedInvoc.InvokeAndWaitAsync(violator, admin_id, reason, time);
        public static async Task SendPlayerOnDuty(ulong player, bool intern) =>
            await PlayerOnDutyInvoc.InvokeAndWaitAsync(player, intern, DateTime.Now);
        public static async Task SendPlayerOffDuty(ulong player, bool intern) =>
            await PlayerOffDutyInvoc.InvokeAndWaitAsync(player, intern, DateTime.Now);
        public static async Task SendServerReloading(ulong admin, string reason) =>
            await InvokeServerReloadingInvoc.Invoke(admin, reason);
        public static async Task SendReloading(ulong admin, string reason) =>
            await InvokeServerReloadingInvoc.InvokeAndWaitAsync(admin, reason);

        private static void ReceiveConfirmation(byte id)
        {
            //Warfare.F.Log("Confirmed message " + id.ToString(), ConsoleColor.Cyan);
            NetworkCall.RemovePending(id, false, true);
        }
        private static void ReceiveFailure(byte id)
        {
            //Warfare.F.Log("Failure to read message " + id.ToString(), ConsoleColor.Cyan);
            if (Waits.TryGetValue(id, out KeyValuePair<bool, CancellationTokenSource> result))
            {
                result = new KeyValuePair<bool, CancellationTokenSource>(false, result.Value);
                NetworkCall.RemovePending(id, result.Value, false, true);
            }
        }
        internal static async Task ProcessResponse(byte[] message)
        {
            try
            {
                if (message.Length <= 0) return;
                if (message.Length < sizeof(ushort) + 1)
                {
                    Warfare.F.LogError("Received a message under the minimum size of 3");
                    BeginRead.Invoke();
                    return;
                }
                ECall call;
                byte id;
                if (ByteMath.ReadUInt16(out ushort callid, 0, message))
                {
                    call = (ECall)callid;
                    id = message[2];
                }
                else
                {
                    Warfare.F.LogError("Incorrect call enumerator given in response: " + message[0].ToString(Warfare.Data.Locale));
                    BeginRead.Invoke();
                    return;
                }
                byte[] data = new byte[message.Length - sizeof(ushort) - 1];
                Array.Copy(message, sizeof(ushort) + 1, data, 0, data.Length);
                bool success = false;
                switch (call)
                {
                    case ECall.CONFIRM_RECEIVED:
                        if (ReceivedInvoc.Read(data, out byte receive_id))
                        {
                            ReceiveConfirmation(receive_id);
                            success = true;
                        }
                        break;
                    case ECall.TELL_FAILED_TO_READ:
                        if (FailedToReceiveInvoc.Read(data, out byte fail_id))
                        {
                            ReceiveFailure(fail_id);
                            success = true;
                        }
                        break;
                    case ECall.INVOKE_BAN:
                        if (InvokeBanInvoc.Read(data, out ulong banned, out ulong admin, out string reason, out uint duration, out DateTime timestamp))
                        {
                            await ReceiveInvokeBan(banned, admin, reason, duration, timestamp);
                            success = true;
                        } else
                        {
                            Warfare.F.Log("FAILED TO READ BAN: " + string.Join(", ", data));
                        }
                        break;
                }
                if (id > 0)
                {
                    if (success) await ConfirmReceived(id);
                    else await FailureToReceive(id);
                }
                BeginRead.Invoke();
            }
            catch (Exception ex)
            {
                Warfare.F.LogError("Caught exception while processing response.");
                Warfare.F.LogError(ex);
                BeginRead.Invoke();
            }
        }
        private static async Task ReceiveInvokeBan(ulong banned, ulong admin, string reason, uint duration, DateTime timestamp)
        {
            Warfare.F.Log($"Received ban request for {banned} from {admin} because {reason} for {Warfare.F.GetTimeFromMinutes(duration)} at {timestamp:G}");
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
            Client._send = Send;
            Client._beginRead = () => _ = I?.connection?.BeginRead(false);
            Client._beginReadCancellableAwaitable = (token) => I.connection.BeginRead(token, true);
        }
        public async Task Connect(CancellationTokenSource token) => await connection?.Connect(token, true);
        public void Shutdown()
        {
            Warfare.F.Log("Shutting down", ConsoleColor.Magenta);
            connection.connected = false;
            if (connection == null || connection.socket == null) return;
            connection.socket.Close();
            try
            {
                connection.socket.Dispose();
            }
            catch (ObjectDisposedException) { }
        }
        public bool IsConnected() => connection.connected;
        public async Task SendMessageAsync(byte[] message) => await connection?.SendMessage(message);
        public static async Task SendMessageAsyncStatic(byte[] message) => await I?.SendMessageAsync(message);
        internal async Task ReceiveData(byte[] data) => await OnReceivedData?.Invoke(data);
        public class ClientConnection
        {
            public TcpClient socket;
            private readonly TCPClient _owner;
            private NetworkStream stream;
            private byte[] _buffer;
            private CancellationTokenSource _waiterToken;
            private bool listening = false;
            public bool connected = false;
            public ClientConnection(TCPClient owner)
            {
                this._owner = owner;
            }
            int connection_tries = 0;
            const int max_connection_tries = 10;
            public async Task Connect(CancellationTokenSource token, bool first = true)
            {
                this._waiterToken = token;
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
                    connected = true;
                    Warfare.F.Log($"Connected to {socket.Client.RemoteEndPoint}.", ConsoleColor.DarkYellow);
                    DateTime start = DateTime.Now;
                    await Client.Identify();
                }
                catch (SocketException)
                {
                    if (connection_tries <= max_connection_tries)
                    {
                        Warfare.F.LogWarning($"Unable to connect, retrying ({connection_tries}/{max_connection_tries})", ConsoleColor.DarkYellow);
                        await Connect(token, false);
                    }
                    else Warfare.F.LogError($"Unable to connect after {max_connection_tries} tries.", ConsoleColor.Red);
                    return;
                }
                if (!socket.Connected) return;
                stream = socket.GetStream();
                _ = BeginRead(_waiterToken.Token, false);
            }
            public ConfiguredTaskAwaitable<bool> BeginRead() => BeginRead(_waiterToken.Token, true);
            public ConfiguredTaskAwaitable<bool> BeginRead(bool wait) => BeginRead(_waiterToken.Token, wait);
            public ConfiguredTaskAwaitable<bool> BeginRead(CancellationToken token, bool wait)
            {
                if (listening) return Task.FromResult(true).ConfigureAwait(true);
                return Task.Run(async () =>
                {
                    Warfare.F.Log("LISTENING", ConsoleColor.Cyan);
                    try
                    {
                        if (stream == default)
                        {
                            Warfare.F.LogError("Disconnected from TCP host.");
                            return false;
                        }
                        listening = true;
                        int received_bytes_count = await stream.ReadAsync(_buffer, 0, BufferSize, token);
                        listening = false;
                        if (token.IsCancellationRequested) return false;
                        if (received_bytes_count <= 0) _owner.Shutdown();
                        byte[] received = new byte[received_bytes_count];
                        Array.Copy(_buffer, 0, received, 0, received_bytes_count);
                        await _owner.ReceiveData(received);
                    }
                    catch (SocketException)
                    {
                        goto Error;
                    }
                    catch (InvalidOperationException)
                    {
                        goto Error;
                    }
                    catch (IOException)
                    {
                        goto Error;
                    }
                    return false;
                Error:
                    Warfare.F.LogError("Disconnected from TCP host.");
                    I.Dispose();
                    return false;
                }).ConfigureAwait(wait);
            }
            public async Task SendMessage(byte[] message)
            {
                if (!connected) return;
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
                    goto Error;
                }
                catch (InvalidOperationException)
                {
                    goto Error;
                }
                catch (IOException)
                {
                    goto Error;
                }
                catch (Exception ex)
                {
                    Warfare.F.LogError("Unknown error in ClientConnection.SendMessage(byte[]):");
                    Warfare.F.LogError(ex);
                }
                return;

                Error:
                    Warfare.F.LogError("Unable to write message.", ConsoleColor.Red);
            }
            public void Dispose()
            {
                this._waiterToken?.Cancel();
            }
        }
        public void Dispose()
        {
            this.Shutdown();
            this.connection?.Dispose();
            I = null;
            GC.SuppressFinalize(this);
        }
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
        REVOKE_HELPER = 32
    }
    public enum EStartupStep : byte
    {
        LOADING_PLUGIN = 0,
        PLUGINS_LOADED = 1,
        LEVEL_LOADED = 2
    }
}
