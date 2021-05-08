using MySql.Data.MySqlClient;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare
{
    public class DatabaseManager
    {
        private Dictionary<string, MySqlTableLang> Tables { get => UCWarfare.I?.TableData; }
        public MySqlConnection SQL;
        public DatabaseManager()
        {
            SQL = new MySqlConnection(UCWarfare.I.Configuration.Instance.SQL.ConnectionString);
            SQL.OpenAsync();
            CommandWindow.LogWarning("MySQL instance created.");
        }
        public async void Open()
        {
            try
            {
                await SQL.OpenAsync();
            }
            catch (Exception ex)
            {
                CommandWindow.LogError("MySQL Error while opening connection...\n" + ex.ToString());
            }
        }
        public async void Close()
        {
            try
            {
                await SQL.CloseAsync();
            }
            catch (MySqlException ex)
            {
                CommandWindow.LogError("MySQL Error while closing connection...\n" + ex.ToString());
            }
        }
        public void CheckDatabase()
        {
            if (SQL.State != System.Data.ConnectionState.Open)
            {
                Open();
            }
        }
        public async Task<ulong> GetDiscordID(ulong Steam64)
        {
            MySqlTableLang table = Tables["discord_accounts"];
            using (MySqlCommand Q = new MySqlCommand($"SELECT `{table.Columns["DiscordID"]}` FROM `{table.TableName}` WHERE `{table.Columns["Steam64"]}` = @0 LIMIT 1;", SQL))
            {
                Q.Parameters.AddWithValue("@0", Steam64);
                using(DbDataReader R = await Q.ExecuteReaderAsync())
                {
                    CommandWindow.LogError(R.GetType());
                    while(await R.ReadAsync())
                    {
                        return (ulong)R.GetInt64(0);
                    }
                }
            }
            return 0;
        }
        public async Task<ulong> GetSteamID(ulong DiscordID)
        {
            MySqlTableLang table = Tables["discord_accounts"];
            using (MySqlCommand Q = new MySqlCommand($"SELECT `{table.Columns["Steam64"]}` FROM `{table.TableName}` WHERE `{table.Columns["DiscordID"]}` = @0 LIMIT 1;", SQL))
            {
                Q.Parameters.AddWithValue("@0", DiscordID);
                using (DbDataReader R = await Q.ExecuteReaderAsync())
                {
                    CommandWindow.LogError(R.GetType());
                    while (await R.ReadAsync())
                    {
                        return (ulong)R.GetInt64(0);
                    }
                }
            }
            return 0;
        }
        public void AddXP(EXPGainType type)
        {

        }
    }
    public enum EXPGainType : byte
    {
        CAP_INCREASE,
        WIN,
        KILL,
        DEFENCE_KILL,
        OFFENCE_KILL,
        CAPTURE_KILL,
        CAPTURE,
        HOLDING_POINT
    }
    public enum ECreditsGainType : byte
    {
        CAPTURE,
        WIN
    }
}
