using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uncreated.Networking.Encoding;
using System.Threading;

namespace Uncreated.SQL
{
    public abstract class MySqlDatabase : IDisposable
    {
        public MySqlConnection SQL;
        public bool DebugLogging = false;
        protected MySqlData _login;
        protected DbDataReader CurrentReader;
        private bool _openSuccess;
        private SemaphoreSlim _threadLocker = new SemaphoreSlim(1, 1);
        public MySqlDatabase(MySqlData data)
        {
            _login = data;
            SQL = new MySqlConnection(_login.ConnectionString);
        }
        public void Dispose()
        {
            _threadLocker.Wait();
            Close();
            SQL.Dispose();
            _threadLocker.Release();
            _threadLocker.Dispose();
        }
        public abstract void Log(string message, ConsoleColor color = ConsoleColor.Gray);
        public abstract void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow);
        public abstract void LogError(string message, ConsoleColor color = ConsoleColor.Red);
        public abstract void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red);
        public bool Open()
        {
            if (!_threadLocker.Wait(10000))
            {
                LogWarning("Failed to wait for the threadlogger and open the MySql connection, all subsequent MySql operations will lock the thread.");
                return false;
            }
            try
            {
                SQL.Open();
                if (DebugLogging) Log(nameof(Open) + ": Opened Connection.", ConsoleColor.DarkGray);
                _openSuccess = true;
                _threadLocker.Release();
                return true;
            }
            catch (MySqlException ex)
            {
                _openSuccess = false;
                _threadLocker.Release();
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
        public bool Close()
        {
            _threadLocker.Wait();
            _openSuccess = false;
            try
            {
                SQL.Close();
                if (DebugLogging) Log(nameof(Close) + ": Closed Connection.", ConsoleColor.DarkGray);
                _threadLocker.Release();
                return true;
            }
            catch (MySqlException ex)
            {
                LogError("Failed to close MySql Connection synchronously: ");
                LogError(ex);
                _threadLocker.Release();
                return false;
            }
        }
        public async Task<bool> OpenAsync()
        {
            await _threadLocker.WaitAsync();
            try
            {
                await SQL.OpenAsync();
                if (DebugLogging) Log(nameof(OpenAsync) + ": Opened Connection.", ConsoleColor.DarkGray);
                _openSuccess = true;
                _threadLocker.Release();
                return true;
            }
            catch (MySqlException ex)
            {
                _openSuccess = false;
                _threadLocker.Release();
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
        public async Task<bool> CloseAsync()
        {
            await _threadLocker.WaitAsync();
            _openSuccess = false;
            try
            {
                await SQL.CloseAsync();
                if (DebugLogging) Log(nameof(CloseAsync) + ": Closed Connection.", ConsoleColor.DarkGray);
                _threadLocker.Release();
                return true;
            }
            catch (MySqlException ex)
            {
                LogError("Failed to close MySql Connection asynchronously: ");
                LogError(ex);
                _threadLocker.Release();
                return false;
            }
        }
        public void Query(string query, object[] parameters, ReadLoopAction ReadLoopAction, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            _threadLocker.Wait();
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                try
                {
                    for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                    if (DebugLogging) Log(nameof(Query) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
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
                catch (InvalidOperationException ex) when (t == 0)
                {
                    _threadLocker.Release();
                    Close();
                    if (Open())
                    {
                        Query(query, parameters, ReadLoopAction, 1);
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                }
            }
            _threadLocker.Release();
        }
        public async Task QueryAsync(string query, object[] parameters, ReadLoopAction ReadLoopAction, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            await _threadLocker.WaitAsync();
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                try
                {
                    for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                    if (DebugLogging) Log(nameof(QueryAsync) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
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
                        Q.Dispose();
                        CurrentReader = null;
                    }
                }
                catch (InvalidOperationException ex) when (t == 0)
                {
                    _threadLocker.Release();
                    await CloseAsync();
                    if (await OpenAsync())
                    {
                        await QueryAsync(query, parameters, ReadLoopAction, 1);
                        return;
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                }
            }
            _threadLocker.Release();
        }

        public delegate void ReadLoopAction(MySqlDataReader R);
        public delegate bool BreakableReadLoopAction(MySqlDataReader R);
        public void Query(string query, object[] parameters, BreakableReadLoopAction ReadLoopAction, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            _threadLocker.Wait();
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                try
                {
                    for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                    if (DebugLogging) Log(nameof(Query) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                    using (CurrentReader = Q.ExecuteReader())
                    {
                        if (CurrentReader is MySqlDataReader R)
                        {
                            while (R.Read() && !ReadLoopAction.Invoke(R)) ;
                        }
                        CurrentReader.Close();
                        CurrentReader.Dispose();
                        Q.Dispose();
                        CurrentReader = null;
                    }
                }
                catch (InvalidOperationException ex) when (t == 0)
                {
                    _threadLocker.Release();
                    Close();
                    if (Open())
                    {
                        Query(query, parameters, ReadLoopAction, 1);
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                }
            }
            _threadLocker.Release();
        }
        public async Task QueryAsync(string query, object[] parameters, BreakableReadLoopAction ReadLoopAction, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            await _threadLocker.WaitAsync();
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                try
                {
                    for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                    if (DebugLogging) Log(nameof(QueryAsync) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                    using (CurrentReader = await Q.ExecuteReaderAsync())
                    {
                        if (CurrentReader is MySqlDataReader R)
                        {
                            while (await R.ReadAsync() && !ReadLoopAction.Invoke(R)) ;
                        }
                        CurrentReader.Close();
                        CurrentReader.Dispose();
                        Q.Dispose();
                        CurrentReader = null;
                    }
                }
                catch (InvalidOperationException ex) when (t == 0)
                {
                    _threadLocker.Release();
                    await CloseAsync();
                    if (await OpenAsync())
                    {
                        await QueryAsync(query, parameters, ReadLoopAction, 1);
                        return;
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                }
            }
            _threadLocker.Release();
        }
        public T Scalar<T>(string query, object[] parameters, Func<object, T> converter, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            _threadLocker.Wait();
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                try
                {
                    for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                    if (DebugLogging) Log(nameof(Scalar) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                    object res = Q.ExecuteScalar();
                    Q.Dispose();
                    _threadLocker.Release();
                    if (res == null) return default;
                    else return converter.Invoke(res);
                }
                catch (InvalidOperationException ex) when (t == 0)
                {
                    _threadLocker.Release();
                    Close();
                    if (Open())
                    {
                        _threadLocker.Release();
                        return Scalar(query, parameters, converter, 1);
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        _threadLocker.Release();
                        return default;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                    _threadLocker.Release();
                    return default;
                }
            }
        }
        public async Task<T> ScalarAsync<T>(string query, object[] parameters, Func<object, T> converter, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess) throw new Exception("Not connected");
            await _threadLocker.WaitAsync();
            using (MySqlCommand Q = new MySqlCommand(query, SQL))
            {
                try
                {
                    for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                    if (DebugLogging) Log(nameof(ScalarAsync) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                    object res = await Q.ExecuteScalarAsync();
                    Q.Dispose();
                    _threadLocker.Release();
                    if (res == null) return default;
                    else return converter.Invoke(res);
                }
                catch (InvalidOperationException ex) when (t == 0)
                {
                    _threadLocker.Release();
                    await CloseAsync();
                    if (await OpenAsync())
                    {
                        return await ScalarAsync(query, parameters, converter, 1);
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        _threadLocker.Release();
                        return default;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                    _threadLocker.Release();
                    return default;
                }
            }
        }
        public void NonQuery(string command, object[] parameters, byte t = 0)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!_openSuccess) throw new Exception("Not connected");
            _threadLocker.Wait();
            using (MySqlCommand Q = new MySqlCommand(command, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                if (DebugLogging) Log(nameof(NonQuery) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                try
                {
                    Q.ExecuteNonQuery();
                }
                catch (InvalidOperationException ex) when (t == 0)
                {
                    _threadLocker.Release();
                    Close();
                    if (Open())
                    {
                        NonQuery(command, parameters, 1);
                        return;
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                }
            }
            _threadLocker.Release();
        }
        public async Task NonQueryAsync(string command, object[] parameters, byte t = 0)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!_openSuccess) throw new Exception("Not connected");
            await _threadLocker.WaitAsync();
            using (MySqlCommand Q = new MySqlCommand(command, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                if (DebugLogging) Log(nameof(NonQueryAsync) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                try
                {
                    await Q.ExecuteNonQueryAsync();
                }
                catch (InvalidOperationException ex) when (t == 0)
                {
                    _threadLocker.Release();
                    await CloseAsync();
                    if (await OpenAsync())
                    {
                        await NonQueryAsync(command, parameters, 1);
                        return;
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                }
            }
            _threadLocker.Release();
        }
    }

    public class MySqlData
    {
        public string Host;
        public string Database;
        public string Password;
        public string Username;
        public ushort Port;
        public string CharSet;
        [JsonIgnore]
        public string ConnectionString { get => $"server={Host};port={Port};database={Database};uid={Username};password={Password};charset={CharSet};"; }

        public static MySqlData Read(ByteReader R) => new MySqlData()
        {
            Host = R.ReadString(),
            Database = R.ReadString(),
            Password = R.ReadString(),
            Username = R.ReadString(),
            CharSet = R.ReadString(),
            Port = R.ReadUInt16()
        };
        public static void Write(ByteWriter W, MySqlData o)
        {
            W.Write(o.Host);
            W.Write(o.Database);
            W.Write(o.Password);
            W.Write(o.Username);
            W.Write(o.CharSet);
            W.Write(o.Port);
        }
    }
}
