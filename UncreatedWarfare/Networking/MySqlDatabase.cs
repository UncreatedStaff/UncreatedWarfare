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
                if (DebugLogging) Log(nameof(OpenSync) + ": Opened Connection.", ConsoleColor.DarkGray);
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
        public async Task<bool> OpenAsync()
        {
            try
            {
                await SQL.OpenAsync();
                if (DebugLogging) Log(nameof(OpenAsync) + ": Opened Connection.", ConsoleColor.DarkGray);
                _openSuccess = true;
                return true;
            }
            catch (DbException ex)
            {
                _openSuccess = false;
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
            _openSuccess = false;
            try
            {
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    System.Threading.Thread.Sleep(1);
                }
                SQL.Close();
                if (DebugLogging) Log(nameof(CloseSync) + ": Closed Connection.", ConsoleColor.DarkGray);
                return true;
            }
            catch (MySqlException ex)
            {
                LogError("Failed to close MySql Connection synchronously: ");
                LogError(ex);
                return false;
            }
        }
        public async Task<bool> CloseAsync()
        {
            _openSuccess = false;
            try
            {
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    await Task.Delay(1);
                }
                await SQL.CloseAsync();
                if (DebugLogging) Log(nameof(CloseAsync) + ": Closed Connection.", ConsoleColor.DarkGray);
                return true;
            }
            catch (MySqlException ex)
            {
                LogError("Failed to close MySqlConnection asynchronously: ");
                LogError(ex);
                return false;
            }
        }
        public async Task Query(string query, object[] parameters, Action<MySqlDataReader> ReadLoopAction)
        {
            if(query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(Query) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    await Task.Delay(1);
                }
                if (!await InternalQuery(Q, ReadLoopAction, true))
                {
                    await Task.Delay(10);
                    int counter = 0;
                    while (counter < 10 && !await InternalQuery(Q, ReadLoopAction, counter != 9))
                    {
                        counter++;
                    }
                }
                Q.Dispose();
            }
        }
        private async Task<bool> InternalQuery(MySqlCommand Q, Action<MySqlDataReader> ReadLoopAction, bool @catch)
        {
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
                }
                return true;
            }
            catch (Exception ex)
            {
                if (!@catch)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", Q.Parameters)}");
                    LogError(ex);
                }
                return false;
            }
        }
        public async Task<T> Scalar<T>(string query, object[] parameters, Func<object, T> converter)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {

                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(Scalar) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    await Task.Delay(1);
                }
                ScalarResponse<T> response = await InternalScalar(Q, converter, true);
                if (!response.success)
                {
                    int counter = 0;
                    while (counter < 10)
                    {
                        await Task.Delay(10);
                        response = await InternalScalar(Q, converter, counter != 9);
                        if (response.success)
                            break;
                        counter++;
                    }
                }
                Q.Dispose();
                if (!response.success) return default;
                else return response.v;
            }
        }
        private async Task<ScalarResponse<T>> InternalScalar<T>(MySqlCommand Q, Func<object, T> converter, bool @catch)
        {
            try
            {
                object res = await Q.ExecuteScalarAsync();
                if (res == null) return new ScalarResponse<T>(default, false);
                else return new ScalarResponse<T>(converter.Invoke(res), true);
            }
            catch (Exception ex)
            {
                if (!@catch)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", Q.Parameters)}");
                    LogError(ex);
                }
                return new ScalarResponse<T>(default, false);
            }
        }
        private struct ScalarResponse<T>
        {
            public T v;
            public bool success;
            public ScalarResponse(T v, bool success)
            {
                this.v = v;
                this.success = success;
            }
        }
        public async Task NonQuery(string command, object[] parameters)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(command, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(NonQuery) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    await Task.Delay(1);
                }
                if (!await InternalNonQuery(Q, false))
                {
                    int counter = 0;
                    while (counter < 10 && !await InternalNonQuery(Q, counter != 9))
                    {
                        await Task.Delay(10);
                        counter++;
                    }
                }
            }
        }
        private async Task<bool> InternalNonQuery(MySqlCommand Q, bool @catch)
        {
            try
            {
                await Q.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                if (!@catch)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", Q.Parameters)}");
                    LogError(ex);
                }
                return false;
            }
        }
        public void QuerySync(string query, object[] parameters, Action<MySqlDataReader> ReadLoopAction)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(QuerySync) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    System.Threading.Thread.Sleep(1);
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
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {

                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(ScalarSync) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    System.Threading.Thread.Sleep(1);
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
            if (!_openSuccess) throw new Exception("Not connected");
            using (MySqlCommand Q = new MySqlCommand(command, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(Warfare.Data.Locale), parameters[i]);
                if (DebugLogging) Log(nameof(NonQuerySync) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                while (CurrentReader != null && !CurrentReader.IsClosed)
                {
                    System.Threading.Thread.Sleep(1);
                }
                try
                {
                    Q.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
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
