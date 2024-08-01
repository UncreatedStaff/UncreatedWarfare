using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Uncreated.Warfare.Util.List;

/// <summary>
/// Dictionary of <see cref="CSteamID"/> to a generic value.
/// </summary>
public class PlayerDictionary<TValue> : IDictionary<ulong, TValue>, IReadOnlyDictionary<ulong, TValue>
{
    private readonly Dictionary<ulong, TValue> _dictionary;
    private IReadOnlyDictionary<ulong, TValue>? _readOnlyDictionary;

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    public int Count => _dictionary.Count;

    public PlayerDictionary() : this(0) { }
    public PlayerDictionary(int capacity)
    {
        _dictionary = new Dictionary<ulong, TValue>(capacity);
    }

    /// <summary>
    /// Check if this dictionary has a value for the given player.
    /// </summary>
    public bool ContainsPlayer(Player player)
    {
        return _dictionary.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Check if this dictionary has a value for the given player.
    /// </summary>
    public bool ContainsPlayer(SteamPlayer player)
    {
        return _dictionary.ContainsKey(player.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Check if this dictionary has a value for the given player.
    /// </summary>
    public bool ContainsPlayer(IPlayer player)
    {
        return _dictionary.ContainsKey(player.Steam64);
    }
    
    /// <summary>
    /// Check if this dictionary has a value for the given player.
    /// </summary>
    public bool ContainsPlayer(CSteamID steam64)
    {
        return _dictionary.ContainsKey(steam64.m_SteamID);
    }

    /// <summary>
    /// Attempt to get the value for the given player if it exists.
    /// </summary>
    public bool TryGetValue(Player player, [MaybeNullWhen(false)] out TValue value)
    {
        return _dictionary.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out value);
    }

    /// <summary>
    /// Attempt to get the value for the given player if it exists.
    /// </summary>
    public bool TryGetValue(SteamPlayer player, [MaybeNullWhen(false)] out TValue value)
    {
        return _dictionary.TryGetValue(player.playerID.steamID.m_SteamID, out value);
    }

    /// <summary>
    /// Attempt to get the value for the given player if it exists.
    /// </summary>
    public bool TryGetValue(IPlayer player, [MaybeNullWhen(false)] out TValue value)
    {
        return _dictionary.TryGetValue(player.Steam64, out value);
    }

    /// <summary>
    /// Attempt to get the value for the given player if it exists.
    /// </summary>
    public bool TryGetValue(CSteamID steam64, [MaybeNullWhen(false)] out TValue value)
    {
        return _dictionary.TryGetValue(steam64.m_SteamID, out value);
    }

    /// <summary>
    /// Remove the value for the given player if it exists.
    /// </summary>
    public bool Remove(Player player)
    {
        return _dictionary.Remove(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Remove the value for the given player if it exists.
    /// </summary>
    public bool Remove(SteamPlayer player)
    {
        return _dictionary.Remove(player.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Remove the value for the given player if it exists.
    /// </summary>
    public bool Remove(IPlayer player)
    {
        return _dictionary.Remove(player.Steam64);
    }

    /// <summary>
    /// Remove the value for the given player if it exists.
    /// </summary>
    public bool Remove(CSteamID steam64)
    {
        return _dictionary.Remove(steam64.m_SteamID);
    }

    /// <summary>
    /// Add the value for the given player.
    /// </summary>
    /// <exception cref="ArgumentException">The key already exists in the dictionary.</exception>
    public void Add(Player player, TValue value)
    {
        _dictionary.Add(player.channel.owner.playerID.steamID.m_SteamID, value);
    }

    /// <summary>
    /// Add the value for the given player.
    /// </summary>
    /// <exception cref="ArgumentException">The key already exists in the dictionary.</exception>
    public void Add(SteamPlayer player, TValue value)
    {
        _dictionary.Add(player.playerID.steamID.m_SteamID, value);
    }

    /// <summary>
    /// Add the value for the given player.
    /// </summary>
    /// <exception cref="ArgumentException">The key already exists in the dictionary.</exception>
    public void Add(IPlayer player, TValue value)
    {
        _dictionary.Add(player.Steam64, value);
    }

    /// <summary>
    /// Add the value for the given player.
    /// </summary>
    /// <exception cref="ArgumentException">The key already exists in the dictionary.</exception>
    public void Add(CSteamID steam64, TValue value)
    {
        _dictionary.Add(steam64.m_SteamID, value);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _dictionary.Clear();
    }

    /// <summary>
    /// Returns a cached read-only dictionary referencing this dictionary.
    /// </summary>
    public IReadOnlyDictionary<ulong, TValue> AsReadOnly() => _readOnlyDictionary ??= new ReadOnlyDictionary<ulong, TValue>(this);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<ulong, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    bool IReadOnlyDictionary<ulong, TValue>.TryGetValue(ulong key, out TValue value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    bool IDictionary<ulong, TValue>.TryGetValue(ulong key, out TValue value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    bool IDictionary<ulong, TValue>.Remove(ulong key)
    {
        return _dictionary.Remove(key);
    }

    /// <inheritdoc />
    void IDictionary<ulong, TValue>.Add(ulong key, TValue value)
    {
        _dictionary.Add(key, value);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<ulong, TValue>>.Add(KeyValuePair<ulong, TValue> item)
    {
        _dictionary.Add(item.Key, item.Value);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<ulong, TValue>>.Contains(KeyValuePair<ulong, TValue> item)
    {
        return _dictionary.TryGetValue(item.Key, out TValue value) && Equals(value, item.Value);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<ulong, TValue>>.CopyTo(KeyValuePair<ulong, TValue>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<ulong, TValue>>)_dictionary).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<ulong, TValue>>.Remove(KeyValuePair<ulong, TValue> item)
    {
        if (_dictionary.TryGetValue(item.Key, out TValue value) && Equals(value, item.Value))
            return _dictionary.Remove(item.Key);

        return false;
    }

    /// <inheritdoc />
    bool IReadOnlyDictionary<ulong, TValue>.ContainsKey(ulong key)
    {
        return _dictionary.ContainsKey(key);
    }

    /// <inheritdoc />
    bool IDictionary<ulong, TValue>.ContainsKey(ulong key)
    {
        return _dictionary.ContainsKey(key);
    }

    /// <inheritdoc />
    TValue IReadOnlyDictionary<ulong, TValue>.this[ulong key]
    {
        get => _dictionary[key];
    }

    /// <inheritdoc />
    TValue IDictionary<ulong, TValue>.this[ulong key]
    {
        get => _dictionary[key];
        set => _dictionary[key] = value;
    }

    /// <inheritdoc />
    IEnumerable<ulong> IReadOnlyDictionary<ulong, TValue>.Keys => _dictionary.Keys;

    /// <inheritdoc />
    ICollection<ulong> IDictionary<ulong, TValue>.Keys => _dictionary.Keys;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<ulong, TValue>.Values => _dictionary.Values;

    /// <inheritdoc />
    ICollection<TValue> IDictionary<ulong, TValue>.Values => _dictionary.Values;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<ulong, TValue>>.IsReadOnly => false;
}