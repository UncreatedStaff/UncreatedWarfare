using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking.Encoding;
using Uncreated.Networking.Encoding.IO;

namespace StatsAnalyzer
{
    public sealed class DatabaseManager : MySqlDatabase
    {
        public DatabaseManager(MySqlData data, bool debug) : base(data) { DebugLogging = debug; }
        public override void Log(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            Debug.WriteLine("INF: " + message);
        }
        public override void LogError(string message, ConsoleColor color = ConsoleColor.Red)
        {
            Debug.WriteLine("ERR: " + message);
        }
        public override void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red)
        {
            Debug.WriteLine("ERR: " + ex.GetType().Name + "\n" + ex.ToString());
        }
        public override void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow)
        {
            Debug.WriteLine("WRN: " + message);
        }
        public async Task<FPlayerName> GetUsernames(ulong Steam64)
        {
            FPlayerName? name = null;
            await Query(
                $"SELECT `PlayerName`, `CharacterName`, `NickName` " +
                $"FROM `usernames` " +
                $"WHERE `Steam64` = @0 LIMIT 1;",
                new object[] { Steam64 },
                (R) =>
                {
                    name = new FPlayerName() { Steam64 = Steam64, PlayerName = R.GetString(0), CharacterName = R.GetString(1), NickName = R.GetString(2) };
                });
            if (name.HasValue)
                return name.Value;
            string tname = Steam64.ToString(StatsPage.Locale);
            return new FPlayerName() { Steam64 = Steam64, PlayerName = tname, CharacterName = tname, NickName = tname };
        }
    }
    public abstract class MySqlDatabase : IDisposable
    {
        public MySqlConnection SQL;
        public bool DebugLogging = false;
        protected MySqlData _login;
        protected DbDataReader CurrentReader;
        private bool _openSuccess;
        public MySqlDatabase(MySqlData data)
        {
            _login = data;
            SQL = new MySqlConnection(_login.ConnectionString);
        }
        public void Dispose()
        {
            Close().GetAwaiter().GetResult();
            SQL.Dispose();
            GC.SuppressFinalize(this);
        }
        public async Task DisposeAsync()
        {
            await Close();
            SQL.Dispose();
            GC.SuppressFinalize(this);
        }
        public abstract void Log(string message, ConsoleColor color = ConsoleColor.Gray);
        public abstract void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow);
        public abstract void LogError(string message, ConsoleColor color = ConsoleColor.Red);
        public abstract void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red);
        public async Task<bool> Open()
        {
            try
            {
                await SQL.OpenAsync();
                if (DebugLogging) Log(nameof(Open) + ": Opened Connection.", ConsoleColor.DarkGray);
                _openSuccess = true;
                return true;
            }
            catch (MySqlException ex)
            {
                _openSuccess = false;
                switch (ex.Number)
                {
                    case 0:
                    case 1042:
                        LogWarning($"MySQL Connection Error: Could not find a host called '{_login.Host}'", ConsoleColor.Yellow);
                        return false;
                    case 1045:
                        LogWarning($"MySQL Connection Error: Host was found, but login was incorrect.", ConsoleColor.Yellow);
                        return false;
                    default:
                        LogError($"MySQL Connection Error Code: {ex.Number} - {ex.Message}", ConsoleColor.Yellow);
                        LogError(ex);
                        return false;
                }
            }
        }
        public async Task<bool> Close()
        {
            _openSuccess = false;
            try
            {
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    await Task.Delay(1);
                }
                await SQL.CloseAsync();
                if (DebugLogging) Log(nameof(Close) + ": Closed Connection.", ConsoleColor.DarkGray);
                return true;
            }
            catch (MySqlException ex)
            {
                LogError("Failed to close MySql Connection synchronously: ");
                LogError(ex);
                return false;
            }
        }
        public async Task Query(string query, object[] parameters, Action<MySqlDataReader> ReadLoopAction)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                try
                {
                    for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                    if (DebugLogging) Log(nameof(Query) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                    using (CurrentReader = await Q.ExecuteReaderAsync())
                    {
                        if (CurrentReader is MySqlDataReader R)
                        {
                            while (R.Read())
                            {
                                ReadLoopAction.Invoke(R);
                            }
                        }
                        CurrentReader.Close();
                        CurrentReader.Dispose();
                        Q.Dispose();
                        CurrentReader = null;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", Q.Parameters)}");
                    LogError(ex);
                }
            }
        }
        public async Task<T> Scalar<T>(string query, object[] parameters, Func<object, T> converter)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                try
                {
                    for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                    if (DebugLogging) Log(nameof(Scalar) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                    object res = await Q.ExecuteScalarAsync();
                    Q.Dispose();
                    if (res == null) return default;
                    else return converter.Invoke(res);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", Q.Parameters)}");
                    LogError(ex);
                    return default;
                }
            }
        }
        public async Task NonQuery(string command, object[] parameters)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(command, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                if (DebugLogging) Log(nameof(NonQuery) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                try
                {
                    await Q.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", Q.Parameters)}");
                    LogError(ex);
                }
            }
        }
    }

    public struct MySqlData
    {
        public string Host;
        public string Database;
        public string Password;
        public string Username;
        public ushort Port;
        public string CharSet;
        public string ConnectionString { get => $"server={Host};port={Port};database={Database};uid={Username};password={Password};charset={CharSet};"; }
        public static void Write(ByteWriter W, MySqlData S)
        {
            W.Write(S.Host);
            W.Write(S.Database);
            W.Write(S.Password);
            W.Write(S.Username);
            W.Write(S.Port);
            W.Write(S.CharSet);
        }
        public static MySqlData Read(ByteReader R)
        {
            return new MySqlData()
            {
                Host = R.ReadString(),
                Database = R.ReadString(),
                Password = R.ReadString(),
                Username = R.ReadString(),
                Port = R.ReadUInt16(),
                CharSet = R.ReadString()
            };
        }
    }
    public class Settings
    {
        public static readonly RawByteIO<Settings> IO = new RawByteIO<Settings>(Read, Write, null, 24);
        public const uint CURRENT_DATA_VERSION = 2;
        public uint DATA_VERSION;
        public MySqlData SQL;
        public ulong LastSteam64;
        public static void Write(ByteWriter W, Settings S)
        {
            W.Write(S.DATA_VERSION);
            MySqlData.Write(W, S.SQL);
            W.Write(S.LastSteam64);
        }
        public static Settings Read(ByteReader R)
        {
            Settings S = new Settings() { DATA_VERSION = R.ReadUInt32() };
            if (S.DATA_VERSION > 0)
            {
                S.SQL = MySqlData.Read(R);
                if (S.DATA_VERSION > 1)
                {
                    S.LastSteam64 = R.ReadUInt64();
                }
            }
            return S;
        }
        public static readonly Settings Default = new Settings
        {
            DATA_VERSION = CURRENT_DATA_VERSION,
            SQL = new MySqlData
            {
                CharSet = "utf8mb4",
                Database = string.Empty,
                Host = "127.0.0.1",
                Port = 3306,
                Password = string.Empty,
                Username = string.Empty
            },
            LastSteam64 = 0
        };
    }

    public struct FPlayerName
    {
        public static readonly FPlayerName Nil = new FPlayerName() { CharacterName = string.Empty, NickName = string.Empty, PlayerName = string.Empty, Steam64 = 0 };
        public ulong Steam64;
        public string PlayerName;
        public string CharacterName;
        public string NickName;
        public static void Write(ByteWriter W, FPlayerName N)
        {
            W.Write(N.Steam64);
            W.Write(N.PlayerName);
            W.Write(N.CharacterName);
            W.Write(N.NickName);
        }
        public static FPlayerName Read(ByteReader R)
        {
            return new FPlayerName
            {
                Steam64 = R.ReadUInt64(),
                PlayerName = R.ReadString(),
                CharacterName = R.ReadString(),
                NickName = R.ReadString()
            };
        }
        public override string ToString() => PlayerName;
    }
}
