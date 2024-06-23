using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Database.Manual;

/// <summary>
/// Simplified replacement for the old manual SQL provider. Uses connection pooling. Supports moderation API since EFCore 3.1 doesn't support complicated type hierarchies.
/// </summary>
public interface IManualMySqlProvider
{
    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row.
    /// </summary>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, ReadLoopAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row.
    /// </summary>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, CancellationToken token, ReadLoopAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row.
    /// </summary>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, ReadLoopAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row.
    /// </summary>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, CancellationToken token, ReadLoopAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row until it returns <see langword="true"/>.
    /// </summary>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, ReadLoopUntilAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row until it returns <see langword="true"/>.
    /// </summary>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, CancellationToken token, ReadLoopUntilAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row until it returns <see langword="true"/>.
    /// </summary>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, ReadLoopUntilAction callback);

    /// <summary>
    /// Query a MySql database and run <paramref name="callback"/> for each returned row until it returns <see langword="true"/>.
    /// </summary>
    Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, CancellationToken token, ReadLoopUntilAction callback);

    /// <summary>
    /// Execute a command against a MySql database.
    /// </summary>
    Task<int> NonQueryAsync(string query, IReadOnlyList<object?>? parameters, CancellationToken token = default);

    /// <summary>
    /// Execute a command against a MySql database.
    /// </summary>
    Task<int> NonQueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, CancellationToken token = default);
}

/// <summary>
/// Called per row while reading a query response.
/// </summary>
public delegate void ReadLoopAction(MySqlDataReader reader);

/// <summary>
/// Called per row while reading a query response. Can return <see langword="true"/> to break from the loop.
/// </summary>
public delegate bool ReadLoopUntilAction(MySqlDataReader reader);


/// <summary>
/// Simplified replacement for the old manual SQL provider. Uses connection pooling.
/// </summary>
public class ManualMySqlProvider : IManualMySqlProvider
{
    private readonly string _connectionString;
    public ManualMySqlProvider(string connectionString)
    {
        _connectionString = connectionString;
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
        token.ThrowIfCancellationRequested();

        CheckLengthIndex(parameters, ref index, ref length);
        await using MySqlConnection connection = new MySqlConnection(_connectionString);
        await using MySqlCommand command = new MySqlCommand(query, connection);

        AppendParameters(command, parameters, index, length);

        await using MySqlDataReader dataReader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();

        int row = 0;
        while (await dataReader.ReadAsync(token).ConfigureAwait(false))
        {
            ++row;
            token.ThrowIfCancellationRequested();
            callback(dataReader);
        }

        return row;
    }

    /// <inheritdoc />
    public Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, ReadLoopUntilAction callback)
        => QueryAsync(query, parameters, 0, -1, CancellationToken.None, callback);

    /// <inheritdoc />
    public Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, CancellationToken token, ReadLoopUntilAction callback)
        => QueryAsync(query, parameters, 0, -1, token, callback);

    /// <inheritdoc />
    public Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, ReadLoopUntilAction callback)
        => QueryAsync(query, parameters, index, length, CancellationToken.None, callback);

    /// <inheritdoc />
    public async Task<int> QueryAsync(string query, IReadOnlyList<object?>? parameters, int index, int length, CancellationToken token, ReadLoopUntilAction callback)
    {
        token.ThrowIfCancellationRequested();

        CheckLengthIndex(parameters, ref index, ref length);
        await using MySqlConnection connection = new MySqlConnection(_connectionString);
        await using MySqlCommand command = new MySqlCommand(query, connection);

        AppendParameters(command, parameters, index, length);

        await using MySqlDataReader dataReader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();

        int row = 0;
        while (await dataReader.ReadAsync(token).ConfigureAwait(false))
        {
            ++row;
            token.ThrowIfCancellationRequested();
            if (callback(dataReader))
                break;
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
        await using MySqlCommand command = new MySqlCommand(query, connection);

        AppendParameters(command, parameters, index, length);

        return await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
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