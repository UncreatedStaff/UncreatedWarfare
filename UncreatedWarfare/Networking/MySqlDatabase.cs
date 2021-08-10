using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.SQL
{
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
            Close();
            SQL.Dispose();
        }
        public virtual void Log(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = temp;
        }
        public virtual void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = temp;
        }
        public virtual void LogError(string message, ConsoleColor color = ConsoleColor.Red)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = temp;
        }
        public virtual void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(ex);
            Console.ForegroundColor = temp;
        }
        public bool Open()
        {
            try
            {
                SQL.Open();
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
                        LogWarning($"DATABASE CONNECTION FAILED: Could not find a host called '{_login.Host}'", ConsoleColor.Yellow);
                        return false;
                    case 1045:
                        LogWarning($"DATABASE CONNECTION FAILED: Host was found, but login was incorrect.", ConsoleColor.Yellow);
                        return false;
                    default:
                        LogError($"DATABASE CONNECTION ERROR CODE: {ex.Number} - {ex.Message}", ConsoleColor.Yellow);
                        LogError(ex);
                        return false;
                }
            }
        }
        public bool Close()
        {
            _openSuccess = false;
            try
            {
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    System.Threading.Thread.Sleep(1);
                }
                SQL.Close();
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
        public void Query(string query, object[] parameters, Action<MySqlDataReader> ReadLoopAction)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                try
                {
                    for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                    using (CurrentReader = Q.ExecuteReader())
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
        public T Scalar<T>(string query, object[] parameters, Func<object, T> converter)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                try
                {
                    for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                    if (DebugLogging) Log(nameof(Scalar) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                    object res = Q.ExecuteScalar();
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
        public void NonQuery(string command, object[] parameters)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(command, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(NonQuery) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                try
                {
                    Q.ExecuteNonQuery();
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
        [Newtonsoft.Json.JsonIgnore]
        public string ConnectionString { get => $"server={Host};port={Port};database={Database};uid={Username};password={Password};charset={CharSet};"; }
    }
}
