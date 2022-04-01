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
    /// <summary>
    /// Overridable MySQL wrapper.
    /// </summary>
    public abstract class MySqlDatabase : IDisposable
    {
        /// <summary>
        /// Underlying MySQL Connection
        /// </summary>
        public MySqlConnection SQL;
        /// <summary>
        /// Should log debug messages.
        /// </summary>
        public bool DebugLogging = false;
        /// <summary>
        /// Data used to log into the database.
        /// </summary>
        protected MySqlData _login;
        private DbDataReader CurrentReader;
        private bool _openSuccess;
        private readonly SemaphoreSlim _threadLocker = new SemaphoreSlim(1, 1);
        /// <summary>
        /// Create a <see cref="MySqlDatabase"/> instance using a <see cref="MySqlData"/> object for the connection string.
        /// </summary>
        public MySqlDatabase(MySqlData data)
        {
            _login = data;
            SQL = new MySqlConnection(_login.ConnectionString);
        }
        /// <inheritdoc />
        public void Dispose()
        {
            _threadLocker.Wait();
            Close();
            SQL.Dispose();
            _threadLocker.Release();
            _threadLocker.Dispose();
        }
        /// <summary>
        /// Override to customize how logging is done.
        /// </summary>
        /// <param name="message">A message message</param>
        /// <param name="color">The color of the message, may not be respected by all overrides.</param>
        protected abstract void Log(string message, ConsoleColor color = ConsoleColor.Gray);
        /// <summary>
        /// Override to customize how logging is done.
        /// </summary>
        /// <param name="message">A warning message</param>
        /// <param name="color">The color of the message, may not be respected by all overrides.</param>
        protected abstract void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow);
        /// <summary>
        /// Override to customize how logging is done.
        /// </summary>
        /// <param name="message">An error message</param>
        /// <param name="color">The color of the message, may not be respected by all overrides.</param>
        protected abstract void LogError(string message, ConsoleColor color = ConsoleColor.Red);
        /// <summary>
        /// Override to customize how logging is done.
        /// </summary>
        /// <param name="ex">An exception</param>
        /// <param name="color">The color of the message, may not be respected by all overrides.</param>
        protected abstract void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red);
        /// <summary>
        /// Open the MySQL connection.
        /// </summary>
        /// <returns>A <see cref="bool"/> meaning whether the operation was successful or not.</returns>
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
        /// <summary>
        /// Close the MySQL connection.
        /// </summary>
        /// <returns>A <see cref="bool"/> meaning whether the operation was successful or not.</returns>
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
        /// <summary>
        /// Open the MySQL connection asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing an asynchronous operation that returns a <see cref="bool"/> meaning whether the operation was successful or not.</returns>
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
        /// <summary>
        /// Close the MySQL connection asynchronously.
        /// </summary>
        /// <returns>A <see cref="bool"/> meaning whether the operation was successful or not.</returns>
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
        /// <summary>
        /// Call a query, such as a select, etc, and run code for each row.
        /// </summary>
        /// <param name="query">MySQL query to call.</param>
        /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
        /// <param name="readLoopAction">Callback to call for each row, with signature: <code><see cref="void"/> <paramref name="readLoopAction"/>(<see cref="MySqlDataReader"/> reader)</code>
        /// To break the loop use the overload: <see cref="Query(string, object[], BreakableReadLoopAction, byte)"/>.</param>
        /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> == <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
        /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
        public void Query(string query, object[] parameters, ReadLoopAction readLoopAction, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess && !Open()) throw new Exception("Not connected");
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
                            int row = 0;
                            while (R.Read())
                            {
                                try
                                {
                                    readLoopAction.Invoke(R);
                                }
                                catch (Exception ex)
                                {
                                    LogError("Error in defined reader loop on row " + row + ": ");
                                    LogError(ex);
                                    LogError(Environment.StackTrace);
                                }
                                ++row;
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
                        Query(query, parameters, readLoopAction, 1);
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        throw;
                    }
                    return;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                    _threadLocker.Release();
                    throw;
                }
            }
            _threadLocker.Release();
        }
        /// <summary>
        /// Call a query, such as a select, etc, and run code for each row.
        /// </summary>
        /// <param name="query">MySQL query to call.</param>
        /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
        /// <param name="readLoopAction">Callback to call for each row, with signature: <code><see cref="void"/> <paramref name="readLoopAction"/>(<see cref="MySqlDataReader"/> reader)</code>
        /// To break the loop use the overload: <see cref="QueryAsync(string, object[], BreakableReadLoopAction, byte)"/>.</param>
        /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous Query operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> == <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
        /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
        public async Task QueryAsync(string query, object[] parameters, ReadLoopAction readLoopAction, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess && !Open()) throw new Exception("Not connected");
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
                            int row = 0;
                            while (await R.ReadAsync())
                            {
                                try
                                {
                                    readLoopAction.Invoke(R);
                                }
                                catch (Exception ex)
                                {
                                    LogError("Error in defined reader loop on row " + row + ": ");
                                    LogError(ex);
                                    LogError(Environment.StackTrace);
                                }
                                ++row;
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
                        await QueryAsync(query, parameters, readLoopAction, 1);
                        return;
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                    _threadLocker.Release();
                    throw;
                }
            }
            _threadLocker.Release();
        }
        /// <summary>
        /// Called per row while reading a query response.
        /// </summary>
        public delegate void ReadLoopAction(MySqlDataReader R);
        /// <summary>
        /// Called per row while reading a query response. Can return <see langword="true"/> to break from the loop.
        /// </summary>
        public delegate bool BreakableReadLoopAction(MySqlDataReader R);
        /// <summary>
        /// Call a query, such as a select, etc, and run code for each row.
        /// </summary>
        /// <param name="query">MySQL query to call.</param>
        /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
        /// <param name="readLoopAction">Callback to call for each row, with signature: <code><see cref="bool"/> <paramref name="readLoopAction"/>(<see cref="MySqlDataReader"/> reader)</code>
        /// Return <see langword="true"/> to break the loop.</param>
        /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> == <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
        /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
        public void Query(string query, object[] parameters, BreakableReadLoopAction readLoopAction, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess && !Open()) throw new Exception("Not connected");
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
                            int row = 0;
                            while (R.Read())
                            {
                                try
                                {
                                    if (!readLoopAction.Invoke(R)) break;
                                }
                                catch (Exception ex)
                                {
                                    LogError("Error in defined reader loop on row " + row + ": ");
                                    LogError(ex);
                                    LogError(Environment.StackTrace);
                                }
                            }
                            ++row;
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
                        Query(query, parameters, readLoopAction, 1);
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        throw;
                    }
                    return;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                    _threadLocker.Release();
                    throw;
                }
            }
            _threadLocker.Release();
        }
        /// <summary>
        /// Call a query, such as a select, etc, and run code for each row.
        /// </summary>
        /// <param name="query">MySQL query to call.</param>
        /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
        /// <param name="readLoopAction">Callback to call for each row, with signature: <code><see cref="bool"/> <paramref name="readLoopAction"/>(<see cref="MySqlDataReader"/> reader)</code>
        /// Return <see langword="true"/> to break the loop.</param>
        /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous Query operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> == <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
        /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
        public async Task QueryAsync(string query, object[] parameters, BreakableReadLoopAction readLoopAction, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess && !Open()) throw new Exception("Not connected");
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
                            int row = 0;
                            while (await R.ReadAsync())
                            {
                                try
                                {
                                    if (!readLoopAction.Invoke(R)) break;
                                }
                                catch (Exception ex)
                                {
                                    LogError("Error in defined reader loop on row " + row + ": ");
                                    LogError(ex);
                                    LogError(Environment.StackTrace);
                                }
                            }
                            ++row;
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
                        await QueryAsync(query, parameters, readLoopAction, 1);
                        return;
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                    _threadLocker.Release();
                    throw;
                }
            }
            _threadLocker.Release();
        }
        /// <summary>
        /// Call a query, such as a select, etc, and get 1 cell's result (gets 0,0 in the table).
        /// </summary>
        /// <param name="query">MySQL query to call.</param>
        /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
        /// <param name="converter">Convert from <see cref="object"/> to <typeparamref name="T"/>, usually just a C-cast.</param>
        /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> == <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
        /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
        /// <typeparam name="T">Output type</typeparam>
        public T Scalar<T>(string query, object[] parameters, Func<object, T> converter, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess && !Open()) throw new Exception("Not connected");
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
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                    _threadLocker.Release();
                    throw;
                }
            }
        }
        /// <summary>
        /// Call a query, such as an insert, delete, etc, and get 1 cell's result (gets 0,0 in the table).
        /// </summary>
        /// <param name="query">MySQL query to call.</param>
        /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
        /// <param name="converter">Convert from <see cref="object"/> to <typeparamref name="T"/>, usually just a C-cast.</param>
        /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
        /// <returns>A <see cref="Task{T}"/> representing the Non-Query asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> == <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
        /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
        /// <typeparam name="T">Output type</typeparam>
        public async Task<T> ScalarAsync<T>(string query, object[] parameters, Func<object, T> converter, byte t = 0)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!_openSuccess && !Open()) throw new Exception("Not connected");
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
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                    _threadLocker.Release();
                    throw;
                }
            }
        }
        /// <summary>
        /// Call a non-query, such as an insert, delete, etc.
        /// </summary>
        /// <param name="command">MySQL non-query to call.</param>
        /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
        /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
        /// <returns>The number of rows modified.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="command"/> == <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
        /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
        public int NonQuery(string command, object[] parameters, byte t = 0)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!_openSuccess && !Open()) throw new Exception("Not connected");
            _threadLocker.Wait();
            using (MySqlCommand Q = new MySqlCommand(command, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                if (DebugLogging) Log(nameof(NonQuery) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                try
                {
                    int lc = Q.ExecuteNonQuery();
                    _threadLocker.Release();
                    return lc;
                }
                catch (InvalidOperationException ex) when (t == 0)
                {
                    _threadLocker.Release();
                    Close();
                    if (Open())
                    {
                        return NonQuery(command, parameters, 1);
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                    _threadLocker.Release();
                    throw;
                }
            }
        }
        /// <summary>
        /// Call a non-query, such as an insert, delete, etc.
        /// </summary>
        /// <param name="command">MySQL non-query to call.</param>
        /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
        /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
        /// <returns>The number of rows modified.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="command"/> == <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
        /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
        public async Task<int> NonQueryAsync(string command, object[] parameters, byte t = 0)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!_openSuccess && !Open()) throw new Exception("Not connected");
            await _threadLocker.WaitAsync();
            using (MySqlCommand Q = new MySqlCommand(command, SQL))
            {
                for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                if (DebugLogging) Log(nameof(NonQueryAsync) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
                try
                {
                    int lc = await Q.ExecuteNonQueryAsync();
                    _threadLocker.Release();
                    return lc;
                }
                catch (InvalidOperationException ex) when (t == 0)
                {
                    _threadLocker.Release();
                    await CloseAsync();
                    if (await OpenAsync())
                    {
                        return await NonQueryAsync(command, parameters, 1);
                    }
                    else
                    {
                        LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {string.Join(",", parameters)}");
                        LogError(ex);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to execute command: {Q.CommandText}: {string.Join(",", parameters)}");
                    LogError(ex);
                    _threadLocker.Release();
                    throw;
                }
            }
        }
    }
    /// <summary>Stores information needed to connect to a MySQL connection.</summary>
    [System.Text.Json.Serialization.JsonSerializable(typeof(MySqlData))]
    public class MySqlData : IReadWrite
    {
        /// <summary>IP/Host Address</summary>
        public string Host;
        /// <summary>Database Name</summary>
        public string Database;
        /// <summary>User Password</summary>
        public string Password;
        /// <summary>User Name</summary>
        public string Username;
        /// <summary>Port, default is 3306</summary>
        public ushort Port;
        /// <summary>Character set to use when connecting</summary>
        public string CharSet;
        /// <summary>Generated on get, connection string for mysql connectors.</summary>
        /// <remarks><code>server={Host};port={Port};database={Database};uid={Username};password={Password};charset={CharSet};</code></remarks>
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public string ConnectionString { get => $"server={Host};port={Port};database={Database};uid={Username};password={Password};charset={CharSet};Allow User Variables=True;"; }

        /// <summary>Read by <see cref="ByteReader"/></summary>
        public static MySqlData Read(ByteReader R)
        {
            MySqlData data = new MySqlData();
            (data as IReadWrite).Read(R);
            return data;
        }
        /// <summary>Write by <see cref="ByteWriter"/></summary>
        public static void Write(ByteWriter W, MySqlData o)
        {
            W.Write(o.Host);
            W.Write(o.Database);
            W.Write(o.Password);
            W.Write(o.Username);
            W.Write(o.CharSet);
            W.Write(o.Port);
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void IReadWrite.Write(ByteWriter W) => Write(W, this);
        void IReadWrite.Read(ByteReader R)
        {
            Host = R.ReadString();
            Database = R.ReadString();
            Password = R.ReadString();
            Username = R.ReadString();
            CharSet = R.ReadString();
            Port = R.ReadUInt16();
        }
    }
}
