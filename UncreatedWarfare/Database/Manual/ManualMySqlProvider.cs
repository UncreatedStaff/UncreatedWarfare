using DanielWillett.ReflectionTools;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;

namespace Uncreated.Warfare.Database.Manual;

/// <summary>
/// Simplified replacement for the old manual SQL provider. Uses connection pooling. Supports moderation API since EFCore 3.1 doesn't support complicated type hierarchies.
/// </summary>
public interface IManualMySqlProvider
{
    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row.
    /// </summary>
    /// <exception cref="DbException"/>
    /// <exception cref="ArgumentNullException"/>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, ReadLoopAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row.
    /// </summary>
    /// <exception cref="DbException"/>
    /// <exception cref="ArgumentNullException"/>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, CancellationToken token, ReadLoopAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row.
    /// </summary>
    /// <exception cref="DbException"/>
    /// <exception cref="ArgumentNullException"/>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, ReadLoopAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row.
    /// </summary>
    /// <exception cref="DbException"/>
    /// <exception cref="ArgumentNullException"/>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, CancellationToken token, ReadLoopAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row while it returns <see langword="true"/>.
    /// </summary>
    /// <exception cref="DbException"/>
    /// <exception cref="ArgumentNullException"/>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, ReadLoopWhileAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row while it returns <see langword="true"/>.
    /// </summary>
    /// <exception cref="DbException"/>
    /// <exception cref="ArgumentNullException"/>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, CancellationToken token, ReadLoopWhileAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row while it returns <see langword="true"/>.
    /// </summary>
    /// <exception cref="DbException"/>
    /// <exception cref="ArgumentNullException"/>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, ReadLoopWhileAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row while it returns <see langword="true"/>.
    /// </summary>
    /// <exception cref="DbException"/>
    /// <exception cref="ArgumentNullException"/>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, CancellationToken token, ReadLoopWhileAction callback);

    /// <summary>
    /// Execute a command against a MySql database.
    /// </summary>
    /// <exception cref="DbException"/>
    /// <exception cref="ArgumentNullException"/>
    Task<int> NonQueryAsync(string query, IReadOnlyList<object?>? parameters, CancellationToken token = default);

    /// <summary>
    /// Execute a command against a MySql database.
    /// </summary>
    /// <exception cref="DbException"/>
    /// <exception cref="ArgumentNullException"/>
    Task<int> NonQueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, CancellationToken token = default);
}

/// <summary>
/// Called per row while reading a query response.
/// </summary>
public delegate void ReadLoopAction(MySqlDataReader reader);

/// <summary>
/// Called per row while reading a query response. Can return <see langword="true"/> to break from the loop.
/// </summary>
public delegate bool ReadLoopWhileAction(MySqlDataReader reader);


/// <summary>
/// Simplified replacement for the old manual SQL provider. Uses connection pooling.
/// </summary>
public class ManualMySqlProvider : IManualMySqlProvider
{
    private readonly string _connectionString;
    private readonly ILogger<ManualMySqlProvider> _logger;

    public ManualMySqlProvider(string connectionString, ILogger<ManualMySqlProvider> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, ReadLoopAction callback)
        => QueryAsync(query, parameters, 0, -1, CancellationToken.None, callback);

    /// <inheritdoc />
    public Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, CancellationToken token, ReadLoopAction callback)
        => QueryAsync(query, parameters, 0, -1, token, callback);

    /// <inheritdoc />
    public Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, ReadLoopAction callback)
        => QueryAsync(query, parameters, index, length, CancellationToken.None, callback);

    /// <inheritdoc />
    public async Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, CancellationToken token, ReadLoopAction callback)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));
                
        token.ThrowIfCancellationRequested();

        CheckLengthIndex(parameters, ref index, ref length);
        await using MySqlConnection connection = new MySqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            if (ex is DbException)
                throw;

            throw new ManualMySqlProviderDbException("Error opening database connection.", ex);
        }

        await using MySqlCommand command = new MySqlCommand(query, connection);

        AppendParameters(command, parameters, index, length);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Executing query: {0}.", QueryToString(query, parameters, index, length));
        }

        int row = 0;
        try
        {
            await using MySqlDataReader dataReader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            while (await dataReader.ReadAsync(token).ConfigureAwait(false))
            {
                ++row;
                token.ThrowIfCancellationRequested();
                callback?.Invoke(dataReader);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning("Query failed to execute: {0}.", QueryToString(query, parameters, index, length));

            if (ex is DbException)
                throw;

            throw new ManualMySqlProviderDbException("Error executing query.", ex);
        }

        return row;
    }

    /// <inheritdoc />
    public Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, ReadLoopWhileAction callback)
        => QueryAsync(query, parameters, 0, -1, CancellationToken.None, callback);

    /// <inheritdoc />
    public Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, CancellationToken token, ReadLoopWhileAction callback)
        => QueryAsync(query, parameters, 0, -1, token, callback);

    /// <inheritdoc />
    public Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, ReadLoopWhileAction callback)
        => QueryAsync(query, parameters, index, length, CancellationToken.None, callback);

    /// <inheritdoc />
    public async Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, CancellationToken token, ReadLoopWhileAction callback)
    {
        token.ThrowIfCancellationRequested();

        CheckLengthIndex(parameters, ref index, ref length);
        await using MySqlConnection connection = new MySqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            if (ex is DbException)
                throw;

            throw new ManualMySqlProviderDbException("Error opening database connection.", ex);
        }

        await using MySqlCommand command = new MySqlCommand(query, connection);

        AppendParameters(command, parameters, index, length);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Executing query: {0}.", QueryToString(query, parameters, index, length));
        }

        int row = 0;
        try
        {
            await using MySqlDataReader dataReader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            while (await dataReader.ReadAsync(token).ConfigureAwait(false))
            {
                ++row;
                token.ThrowIfCancellationRequested();
                if (callback != null && callback(dataReader))
                    break;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning("Query failed to execute: {0}.", QueryToString(query, parameters, index, length));

            if (ex is DbException)
                throw;

            throw new ManualMySqlProviderDbException("Error executing query.", ex);
        }

        return row;
    }

    /// <inheritdoc />
    public Task<int> NonQueryAsync(string query, IReadOnlyList<object?>? parameters, CancellationToken token = default)
        => NonQueryAsync(query, parameters, 0, -1, token);

    /// <inheritdoc />
    public async Task<int> NonQueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        CheckLengthIndex(parameters, ref index, ref length);
        await using MySqlConnection connection = new MySqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            if (ex is DbException)
                throw;

            throw new ManualMySqlProviderDbException("Error opening database connection.", ex);
        }

        await using MySqlCommand command = new MySqlCommand(query, connection);

        AppendParameters(command, parameters, index, length);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Executing command: {0}.", QueryToString(query, parameters, index, length));
        }

        try
        {
            return await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning("Command failed to execute: {0}.", QueryToString(query, parameters, index, length));

            if (ex is DbException)
                throw;

            throw new ManualMySqlProviderDbException("Error executing command.", ex);
        }
    }

    private static void AppendParameters(MySqlCommand cmd, IReadOnlyList<object?>? parameters, int index, int length)
    {
        if (parameters is not { Count: > 0 })
            return;

        for (int i = 0; i < length; ++i)
        {
            object? obj = parameters[i + index];
            cmd.Parameters.AddWithValue("@" + i.ToString(CultureInfo.InvariantCulture), obj ?? DBNull.Value);
        }
    }

    private static string QueryToString(string query, IReadOnlyList<object?>? parameters, int index, int length)
    {
        if (parameters is not { Count: > 0 } || length <= 0)
            return query;

        StringBuilder sb = new StringBuilder(query);

        for (int i = 0; i < length; ++i)
        {
            object? obj = parameters[i + index];
            sb.AppendLine();
            sb.Append('@').Append(i).Append(" - ");

            if (obj is null || ReferenceEquals(obj, DBNull.Value))
            {
                sb.Append("null");
            }
            else
            {
                sb.Append(Accessor.Formatter.Format(obj.GetType())).Append(" - ").Append('"').Append(obj is byte[] b ? Convert.ToBase64String(b) : obj.ToString()).Append('"');
            }
        }

        return sb.ToString();
    }

    private static void CheckLengthIndex(IReadOnlyList<object?>? list, ref int index, ref int length)
    {
        if (list == null)
            return;
        if (index < 0)
            index = 0;
        if (index > list.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (length < 0)
            length = list.Count - index;
        else if (index + length > list.Count)
            throw new ArgumentOutOfRangeException(nameof(length));
    }
}

/// <summary>
/// Wraps non-<see cref="DbException"/> errors thrown by <see cref="IManualMySqlProvider"/> implementations.
/// </summary>
public class ManualMySqlProviderDbException : DbException
{
    public ManualMySqlProviderDbException(string message, Exception inner) : base(message, inner) { }
}