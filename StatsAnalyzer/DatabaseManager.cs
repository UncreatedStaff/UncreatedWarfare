using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Networking.Encoding;
using Uncreated.Networking.Encoding.IO;

namespace StatsAnalyzer
{
    public sealed class DatabaseManager : MySqlDatabase
    {
        public DatabaseManager(MySqlData data, bool debug) : base(data) { DebugLogging = debug; }
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
        public async Task<List<FPlayerName>> UsernameSearch(string input, CancellationToken token)
        {
            DateTime start = DateTime.Now;
            string search = $"%{input}%";
            List<FPlayerName> OUT = new List<FPlayerName>();
            await Query($"SELECT * FROM `usernames` WHERE `PlayerName` LIKE @0 ORDER BY LENGTH(`PlayerName`);", new object[1] { search }, R =>
            {
                OUT.Add(new FPlayerName
                {
                    PlayerName = R.GetString("PlayerName"),
                    CharacterName = R.GetString("CharacterName"),
                    NickName = R.GetString("NickName"),
                    Steam64 = R.GetUInt64("Steam64")
                });
            }, token);
            if (OUT.Count == 0 && !token.IsCancellationRequested)
            {
                await Query($"SELECT * FROM `usernames` WHERE `CharacterName` LIKE @0 ORDER BY LENGTH(`CharacterName`);", new object[1] { search }, R =>
                {
                    OUT.Add(new FPlayerName
                    {
                        PlayerName = R.GetString("PlayerName"),
                        CharacterName = R.GetString("CharacterName"),
                        NickName = R.GetString("NickName"),
                        Steam64 = R.GetUInt64("Steam64")
                    });
                }, token);
                if (OUT.Count == 0 && !token.IsCancellationRequested)
                {
                    await Query($"SELECT * FROM `usernames` WHERE `NickName` LIKE @0 ORDER BY LENGTH(`NickName`);", new object[1] { search }, R =>
                    {
                        OUT.Add(new FPlayerName
                        {
                            PlayerName = R.GetString("PlayerName"),
                            CharacterName = R.GetString("CharacterName"),
                            NickName = R.GetString("NickName"),
                            Steam64 = R.GetUInt64("Steam64")
                        });
                    }, token);
                }
            }
            Log($"Username search took {(DateTime.Now - start).TotalMilliseconds} ms on term \"{search}\" and found {OUT.Count} results.");
            return OUT;
        }
        public override void Log(string message, ConsoleColor color = ConsoleColor.Gray) =>
            Debug.WriteLine("INF: " + message);
        public override void LogError(string message, ConsoleColor color = ConsoleColor.Red) =>
            Debug.WriteLine("ERR: " + message);
        public override void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red) =>
            Debug.WriteLine("ERR: " + ex.GetType().Name + "\n" + ex.ToString());
        public override void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow) =>
            Debug.WriteLine("WRN: " + message);
    }

    public class Settings
    {
        public static readonly RawByteIO<Settings> IO = new RawByteIO<Settings>(Read, Write, null, 30);
        public const uint CURRENT_DATA_VERSION = 3;
        public uint DATA_VERSION;
        public MySqlData SQL;
        public ulong LastSteam64;
        public string TCPServerIP;
        public string Identity;
        public ushort TCPServerPort;
        public static void Write(ByteWriter W, Settings S)
        {
            W.Write(S.DATA_VERSION);
            MySqlData.Write(W, S.SQL);
            W.Write(S.LastSteam64);
            W.Write(S.TCPServerIP);
            W.Write(S.TCPServerPort); 
            W.Write(S.Identity); 
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
                    if (S.DATA_VERSION > 2)
                    {
                        S.TCPServerIP = R.ReadString();
                        S.TCPServerPort = R.ReadUInt16();
                        S.Identity = R.ReadString();
                    } 
                    else
                    {
                        S.TCPServerIP = Default.TCPServerIP;
                        S.TCPServerPort = Default.TCPServerPort;
                        S.Identity = Default.Identity;
                    }
                } 
                else
                {
                    S.LastSteam64 = Default.LastSteam64;
                    S.TCPServerIP = Default.TCPServerIP;
                    S.TCPServerPort = Default.TCPServerPort;
                    S.Identity = Default.Identity;
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
            LastSteam64 = 0,
            Identity = string.Empty,
            TCPServerIP = "127.0.0.1",
            TCPServerPort = 31902
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
