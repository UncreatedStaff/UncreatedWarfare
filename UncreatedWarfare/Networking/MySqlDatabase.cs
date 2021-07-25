using MySql.Data.MySqlClient;
using System;
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
        public MySqlDatabase(MySqlData data)
        {
            _login = data;
            SQL = new MySqlConnection(_login.ConnectionString);
        }
        public void Dispose()
        {
            CloseSync();
            SQL.Dispose();
        }
        public async void DisposeAsync()
        {
            await CloseAsync();
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
        public bool OpenSync()
        {
            try
            {
                SQL.Open();
                if (DebugLogging) Log(nameof(OpenSync) + ": Opened Connection.");
                return true;
            }
            catch (MySqlException ex)
            {
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
        public async Task<bool> OpenAsync()
        {
            try
            {
                await SQL.OpenAsync();
                if (DebugLogging) Log(nameof(OpenAsync) + ": Opened Connection.");
                return true;
            }
            catch (DbException ex)
            {
                switch (ex.ErrorCode)
                {
                    case 0:
                    case 1042:
                        LogWarning($"DATABASE CONNECTION FAILED: Could not find a host called '{_login.Host}'", ConsoleColor.Yellow);
                        return false;
                    case 1045:
                        LogWarning($"DATABASE CONNECTION FAILED: Host was found, but login was incorrect.", ConsoleColor.Yellow);
                        return false;
                    default:
                        LogError($"DATABASE CONNECTION ERROR CODE: {ex.ErrorCode} - {ex.Message}", ConsoleColor.Yellow);
                        LogError(ex);
                        return false;
                }
            }
        }
        public bool CloseSync()
        {
            try
            {
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    System.Threading.Thread.Sleep(1);
                    Log("reader open");
                }
                SQL.Close();
                if (DebugLogging) Log(nameof(CloseSync) + ": Closed Connection.");
                return true;
            }
            catch (MySqlException ex)
            {
                LogError("ERROR CLOSING MYSQL CONNECTION: ");
                LogError(ex);
                return false;
            }
        }
        public async Task<bool> CloseAsync()
        {
            try
            {
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    await Task.Delay(1);
                    Log("reader open");
                }
                await SQL.CloseAsync();
                if (DebugLogging) Log(nameof(CloseAsync) + ": Closed Connection.");
                return true;
            }
            catch (MySqlException ex)
            {
                LogError("ERROR CLOSING MYSQL CONNECTION: ");
                LogError(ex);
                return false;
            }
        }
        public async Task Query(string query, object[] parameters, Action<MySqlDataReader> ReadLoopAction)
        {
            if(query == null) throw new ArgumentNullException(nameof(query));
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(Query) + ": " + Q.CommandText);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    await Task.Delay(1);
                    Log("reader open");
                }
                try
                {
                    using (CurrentReader = await Q.ExecuteReaderAsync())
                    {
                        if (CurrentReader is MySqlDataReader R)
                        {
                            while (await R.ReadAsync())
                            {
                                ReadLoopAction.Invoke(R);
                            }
                        }
                        CurrentReader.Close();
                        CurrentReader.Dispose();
                        CurrentReader = null;
                        Q.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Log($"{query}: {string.Join(",", parameters)}");
                    LogError(ex);
                }
            }
        }
        public async Task<T> Scalar<T>(string query, object[] parameters, Func<object, T> converter)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {

                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(Scalar) + ": " + Q.CommandText);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    await Task.Delay(1);
                    Log("reader open");
                }
                object res = await Q.ExecuteScalarAsync();
                Q.Dispose();
                if (res == null) return default;
                else return converter.Invoke(res);
            }
        }
        public async Task NonQuery(string command, object[] parameters)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            using (MySqlCommand Q = new MySqlCommand(command, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(NonQuery) + ": " + Q.CommandText);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    await Task.Delay(1);
                    Log("reader open");
                }
                try
                {
                    await Q.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    LogError($"FAILURE TO EXECUTE COMMAND:\n{command}");
                    LogError(ex);
                }
            }
        }
        public void QuerySync(string query, object[] parameters, Action<MySqlDataReader> ReadLoopAction)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(QuerySync) + ": " + Q.CommandText);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    System.Threading.Thread.Sleep(1);
                    Log("reader open");
                }
                using (CurrentReader = Q.ExecuteReader())
                {
                    while (CurrentReader.Read())
                    {
                        ReadLoopAction.Invoke(CurrentReader as MySqlDataReader);
                    }
                    CurrentReader.Close();
                    CurrentReader.Dispose();
                    Q.Dispose();
                }
            }
        }
        public T ScalarSync<T>(string query, object[] parameters)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {

                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(ScalarSync) + ": " + Q.CommandText);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    System.Threading.Thread.Sleep(1);
                    Log("reader open");
                }
                object res = Q.ExecuteScalar();
                if (res is T a)
                {
                    Q.Dispose();
                    return a;
                }
                else return default;
            }
        }
        public void NonQuerySync(string command, object[] parameters)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            using (MySqlCommand Q = new MySqlCommand(command, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(NonQuerySync) + ": " + Q.CommandText);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    System.Threading.Thread.Sleep(1);
                    Log("reader open");
                }
                try
                {
                    Q.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    LogError("FAILURE TO EXECUTE COMMAND:\n" + command);
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
