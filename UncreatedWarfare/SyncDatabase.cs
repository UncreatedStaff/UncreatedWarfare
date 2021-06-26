using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.SQL;

namespace Uncreated.Warfare
{
    public class SyncDatabase
    {
        private readonly MySqlConnection Connection;

        public bool IsConnected;

        public SyncDatabase(MySqlConnection connection)
        {
            IsConnected = true; // assume the connection has already been opened.
            try
            {
                this.Connection = connection;
                F.Log($"DATABASE CONNECTION: Connection created to {UCWarfare.I.SQL.Host}:{UCWarfare.I.SQL.Port} under user: {UCWarfare.I.SQL.Username}", ConsoleColor.DarkYellow);
                IsConnected = true;
            }
            catch
            {
                F.Log($"DATABASE CONNECTION FAILED: Could not create connection to {UCWarfare.I.SQL.Host}:{UCWarfare.I.SQL.Port} under user: {UCWarfare.I.SQL.Username}", ConsoleColor.Yellow);
                IsConnected = false;
            }
        }
        public bool Open()
        {
            try
            {
                Connection.Open();
                IsConnected = true;
                F.Log($"DATABASE CONNECTION: Successfully connected to {UCWarfare.I.SQL.Host}:{UCWarfare.I.SQL.Port} under user: {UCWarfare.I.SQL.Username}", ConsoleColor.Magenta);
                return true;
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        F.LogWarning($"DATABASE CONNECTION FAILED: Could not find a host called '{UCWarfare.I.SQL.Host}'", ConsoleColor.Yellow);
                        break;

                    case 1045:
                        F.LogWarning($"DATABASE CONNECTION FAILED: Host was found, but password was incorrect.", ConsoleColor.Yellow);
                        break;
                    default:
                        F.LogWarning($"DATABASE CONNECTION FAILED: An unknown error occured...", ConsoleColor.Yellow);
                        break;
                }
                F.LogError($"DATABASE CONNECTION ERROR CODE: {ex.Number} - {ex.Message}", ConsoleColor.Yellow);
                F.LogError(ex);
                return false;
            }
        }
        public bool Close()
        {
            try
            {
                Connection.Close();
                return true;
            }
            catch (MySqlException)
            {
                return false;
            }
        }
        public int GetXP(ulong playerID, ulong team)
        {
            int balance = 0;

            if (IsConnected)
            {
                string query = $"SELECT XP FROM levels WHERE Steam64 = @playerID AND TEAM = @team;";

                MySqlCommand command = new MySqlCommand(query, Connection);
                command.Parameters.AddWithValue("@playerID", playerID);
                command.Parameters.AddWithValue("@team", team);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                    balance = reader.GetInt32("XP");

                reader.Close();
                reader.Dispose();
                command.Dispose();
            }
            return balance;
        }
        public int AddXP(ulong playerID, ulong team, int amount)
        {
            int balance = GetXP(playerID, team);
            if (balance + amount < 0)
                return 0;

            if (IsConnected)
            {
                string query = $"INSERT INTO levels (Steam64, Team, OfficerPoints, XP) VALUES (@playerID, @team, 0, @absxp) ON DUPLICATE KEY UPDATE XP = XP + @xp;";

                MySqlCommand command = new MySqlCommand(query, Connection);
                command.Parameters.AddWithValue("@playerID", playerID);
                command.Parameters.AddWithValue("@team", team);
                command.Parameters.AddWithValue("@absxp", Math.Abs(amount));
                command.Parameters.AddWithValue("@xp", amount);
                command.ExecuteNonQuery();
                command.Dispose();
            }
            return balance + amount;
        }
        public int GetOfficerPoints(ulong playerID, ulong team)
        {
            int balance = 0;

            if (IsConnected)
            {
                string query = $"SELECT OfficerPoints FROM levels WHERE Steam64 = @playerID AND TEAM = @team;";

                MySqlCommand command = new MySqlCommand(query, Connection);
                command.Parameters.AddWithValue("@playerID", playerID);
                command.Parameters.AddWithValue("@team", team);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                    balance = reader.GetInt32("OfficerPoints");

                reader.Close();
                reader.Dispose();
                command.Dispose();
            }
            return balance;
        }
        public int AddOfficerPoints(ulong playerID, ulong team, int amount)
        {
            int balance = GetOfficerPoints(playerID, team);
            if (balance - amount < 0)
                return 0;

            if (IsConnected)
            {
                string query = $"INSERT INTO levels (Steam64, Team, OfficerPoints, XP) VALUES (@playerID, @team, @abspoints, 0) ON DUPLICATE KEY UPDATE OfficerPoints = OfficerPoints + @points;";

                MySqlCommand command = new MySqlCommand(query, Connection);
                command.Parameters.AddWithValue("@playerID", playerID);
                command.Parameters.AddWithValue("@team", team);
                command.Parameters.AddWithValue("@abspoints", Math.Abs(amount));
                command.Parameters.AddWithValue("@points", amount);
                command.ExecuteNonQuery();
                command.Dispose();
            }
            return balance + amount;
        }
    }
}
