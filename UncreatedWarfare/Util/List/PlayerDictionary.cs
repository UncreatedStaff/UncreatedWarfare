using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Util.List;

/// <summary>
/// Dictionary of <see cref="CSteamID"/> to a generic value.
/// </summary>
public class PlayerDictionary<TValue> :
    IDictionary<ulong, TValue>,
    IReadOnlyDictionary<ulong, TValue>,
    IDictionary<CSteamID, TValue>,
    IReadOnlyDictionary<CSteamID, TValue>,
    IDictionary<Player, TValue>,
    IReadOnlyDictionary<Player, TValue>,
    IDictionary<SteamPlayer, TValue>,
    IReadOnlyDictionary<SteamPlayer, TValue>,
    IDictionary<IPlayer, TValue>,
    IReadOnlyDictionary<IPlayer, TValue>,
    IDictionary<WarfarePlayer, TValue>,
    IReadOnlyDictionary<WarfarePlayer, TValue>
{
    private readonly Dictionary<ulong, TValue> _dictionary;
    private IReadOnlyDictionary<ulong, TValue>? _readOnlyDictionary;

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    public int Count => _dictionary.Count;

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    public Dictionary<ulong, TValue>.ValueCollection Values => _dictionary.Values;

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    public Dictionary<ulong, TValue>.KeyCollection Keys => _dictionary.Keys;

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    ICollection<TValue> IDictionary<WarfarePlayer, TValue>.Values => _dictionary.Values;

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    ICollection<TValue> IDictionary<IPlayer, TValue>.Values => _dictionary.Values;

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    ICollection<TValue> IDictionary<SteamPlayer, TValue>.Values => _dictionary.Values;

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    ICollection<TValue> IDictionary<Player, TValue>.Values => _dictionary.Values;

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    ICollection<TValue> IDictionary<CSteamID, TValue>.Values => _dictionary.Values;

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    ICollection<TValue> IDictionary<ulong, TValue>.Values => _dictionary.Values;

    public PlayerDictionary() : this(0) { }
    public PlayerDictionary(int capacity)
    {
        _dictionary = new Dictionary<ulong, TValue>(capacity);
    }

    /// <summary>
    /// Get the value associated with the given player.
    /// </summary>
    public TValue this[Player player]
    {
        get => _dictionary[player.channel.owner.playerID.steamID.m_SteamID];
        set => _dictionary[player.channel.owner.playerID.steamID.m_SteamID] = value;
    }

    /// <summary>
    /// Get the value associated with the given player.
    /// </summary>
    public TValue this[SteamPlayer player]
    {
        get => _dictionary[player.playerID.steamID.m_SteamID];
        set => _dictionary[player.playerID.steamID.m_SteamID] = value;
    }

    /// <summary>
    /// Get the value associated with the given player.
    /// </summary>
    public TValue this[IPlayer player]
    {
        get => _dictionary[player.Steam64.m_SteamID];
        set => _dictionary[player.Steam64.m_SteamID] = value;
    }

    /// <summary>
    /// Get the value associated with the given player.
    /// </summary>
    public TValue this[WarfarePlayer player]
    {
        get => _dictionary[player.Steam64.m_SteamID];
        set => _dictionary[player.Steam64.m_SteamID] = value;
    }

    /// <summary>
    /// Get the value associated with the given player.
    /// </summary>
    public TValue this[CSteamID steam64]
    {
        get => _dictionary[steam64.m_SteamID];
        set => _dictionary[steam64.m_SteamID] = value;
    }

    /// <summary>
    /// Get the value associated with the given player.
    /// </summary>
    public TValue this[ulong steam64]
    {
        get => _dictionary[steam64];
        set => _dictionary[steam64] = value;
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
        return _dictionary.ContainsKey(player.Steam64.m_SteamID);
    }

    /// <summary>
    /// Check if this dictionary has a value for the given player.
    /// </summary>
    public bool ContainsPlayer(WarfarePlayer player)
    {
        return _dictionary.ContainsKey(player.Steam64.m_SteamID);
    }
    
    /// <summary>
    /// Check if this dictionary has a value for the given player.
    /// </summary>
    public bool ContainsPlayer(CSteamID steam64)
    {
        return _dictionary.ContainsKey(steam64.m_SteamID);
    }
    
    /// <summary>
    /// Check if this dictionary has a value for the given player.
    /// </summary>
    public bool ContainsPlayer(ulong steam64)
    {
        return _dictionary.ContainsKey(steam64);
    }

    /// <summary>
    /// Attempt to get the value for the given player if it exists.
    /// </summary>
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
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
        return _dictionary.TryGetValue(player.Steam64.m_SteamID, out value);
    }

    /// <summary>
    /// Attempt to get the value for the given player if it exists.
    /// </summary>
    public bool TryGetValue(WarfarePlayer player, [MaybeNullWhen(false)] out TValue value)
    {
        return _dictionary.TryGetValue(player.Steam64.m_SteamID, out value);
    }

    /// <summary>
    /// Attempt to get the value for the given player if it exists.
    /// </summary>
    public bool TryGetValue(CSteamID steam64, [MaybeNullWhen(false)] out TValue value)
    {
        return _dictionary.TryGetValue(steam64.m_SteamID, out value);
    }

    /// <summary>
    /// Attempt to get the value for the given player if it exists.
    /// </summary>
    public bool TryGetValue(ulong steam64, [MaybeNullWhen(false)] out TValue value)
    {
        return _dictionary.TryGetValue(steam64, out value);
    }

#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).

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
        return _dictionary.Remove(player.Steam64.m_SteamID);
    }

    /// <summary>
    /// Remove the value for the given player if it exists.
    /// </summary>
    public bool Remove(WarfarePlayer player)
    {
        return _dictionary.Remove(player.Steam64.m_SteamID);
    }

    /// <summary>
    /// Remove the value for the given player if it exists.
    /// </summary>
    public bool Remove(CSteamID steam64)
    {
        return _dictionary.Remove(steam64.m_SteamID);
    }

    /// <summary>
    /// Remove the value for the given player if it exists.
    /// </summary>
    public bool Remove(ulong steam64)
    {
        return _dictionary.Remove(steam64);
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
        _dictionary.Add(player.Steam64.m_SteamID, value);
    }

    /// <summary>
    /// Add the value for the given player.
    /// </summary>
    /// <exception cref="ArgumentException">The key already exists in the dictionary.</exception>
    public void Add(WarfarePlayer player, TValue value)
    {
        _dictionary.Add(player.Steam64.m_SteamID, value);
    }

    /// <summary>
    /// Add the value for the given player.
    /// </summary>
    /// <exception cref="ArgumentException">The key already exists in the dictionary.</exception>
    public void Add(CSteamID steam64, TValue value)
    {
        _dictionary.Add(steam64.m_SteamID, value);
    }

    /// <summary>
    /// Add the value for the given player.
    /// </summary>
    /// <exception cref="ArgumentException">The key already exists in the dictionary.</exception>
    public void Add(ulong steam64, TValue value)
    {
        _dictionary.Add(steam64, value);
    }

    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    public void Clear()
    {
        _dictionary.Clear();
    }

    /// <summary>
    /// Returns a cached read-only dictionary referencing this dictionary.
    /// </summary>
    public IReadOnlyDictionary<ulong, TValue> AsReadOnly() => _readOnlyDictionary ??= new ReadOnlyDictionary<ulong, TValue>(this);

    #region IDictionary stuff

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<ulong, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    IEnumerator<KeyValuePair<IPlayer, TValue>> IEnumerable<KeyValuePair<IPlayer, TValue>>.GetEnumerator()
    {
        throw new NotSupportedException();
    }

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    IEnumerator<KeyValuePair<SteamPlayer, TValue>> IEnumerable<KeyValuePair<SteamPlayer, TValue>>.GetEnumerator()
    {
        throw new NotSupportedException();
    }

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    IEnumerator<KeyValuePair<Player, TValue>> IEnumerable<KeyValuePair<Player, TValue>>.GetEnumerator()
    {
        throw new NotSupportedException();
    }

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    IEnumerator<KeyValuePair<WarfarePlayer, TValue>> IEnumerable<KeyValuePair<WarfarePlayer, TValue>>.GetEnumerator()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    IEnumerator<KeyValuePair<CSteamID, TValue>> IEnumerable<KeyValuePair<CSteamID, TValue>>.GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<ulong, TValue>>)this)
            .Select(x => new KeyValuePair<CSteamID, TValue>(new CSteamID(x.Key), x.Value))
            .GetEnumerator();
    }

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
    void ICollection<KeyValuePair<CSteamID, TValue>>.Add(KeyValuePair<CSteamID, TValue> item)
    {
        _dictionary.Add(item.Key.m_SteamID, item.Value);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<Player, TValue>>.Add(KeyValuePair<Player, TValue> item)
    {
        _dictionary.Add(item.Key.channel.owner.playerID.steamID.m_SteamID, item.Value);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<SteamPlayer, TValue>>.Add(KeyValuePair<SteamPlayer, TValue> item)
    {
        _dictionary.Add(item.Key.playerID.steamID.m_SteamID, item.Value);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<IPlayer, TValue>>.Add(KeyValuePair<IPlayer, TValue> item)
    {
        _dictionary.Add(item.Key.Steam64.m_SteamID, item.Value);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<WarfarePlayer, TValue>>.Add(KeyValuePair<WarfarePlayer, TValue> item)
    {
        _dictionary.Add(item.Key.Steam64.m_SteamID, item.Value);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<ulong, TValue>>.Contains(KeyValuePair<ulong, TValue> item)
    {
        return _dictionary.TryGetValue(item.Key, out TValue value) && Equals(value, item.Value);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<SteamPlayer, TValue>>.Contains(KeyValuePair<SteamPlayer, TValue> item)
    {
        return _dictionary.TryGetValue(item.Key.playerID.steamID.m_SteamID, out TValue value) && Equals(value, item);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<IPlayer, TValue>>.Contains(KeyValuePair<IPlayer, TValue> item)
    {
        return _dictionary.TryGetValue(item.Key.Steam64.m_SteamID, out TValue value) && Equals(value, item);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<WarfarePlayer, TValue>>.Contains(KeyValuePair<WarfarePlayer, TValue> item)
    {
        return _dictionary.TryGetValue(item.Key.Steam64.m_SteamID, out TValue value) && Equals(value, item);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<Player, TValue>>.Contains(KeyValuePair<Player, TValue> item)
    {
        return _dictionary.TryGetValue(item.Key.channel.owner.playerID.steamID.m_SteamID, out TValue value) && Equals(value, item);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<CSteamID, TValue>>.Contains(KeyValuePair<CSteamID, TValue> item)
    {
        return _dictionary.TryGetValue(item.Key.m_SteamID, out TValue value) && Equals(value, item);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<ulong, TValue>>.CopyTo(KeyValuePair<ulong, TValue>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<ulong, TValue>>)_dictionary).CopyTo(array, arrayIndex);
    }

    void ICollection<KeyValuePair<CSteamID, TValue>>.CopyTo(KeyValuePair<CSteamID, TValue>[] array, int arrayIndex)
    {
        KeyValuePair<ulong, TValue>[] arr = new KeyValuePair<ulong, TValue>[array.Length];
        for (int i = 0; i < array.Length; ++i)
        {
            ref KeyValuePair<CSteamID, TValue> keyValuePair = ref array[i];
            arr[i] = new KeyValuePair<ulong, TValue>(keyValuePair.Key.m_SteamID, keyValuePair.Value);
        }

        ((ICollection<KeyValuePair<ulong, TValue>>)_dictionary).CopyTo(arr, arrayIndex);
    }

    void ICollection<KeyValuePair<SteamPlayer, TValue>>.CopyTo(KeyValuePair<SteamPlayer, TValue>[] array, int arrayIndex)
    {
        KeyValuePair<ulong, TValue>[] arr = new KeyValuePair<ulong, TValue>[array.Length];
        for (int i = 0; i < array.Length; ++i)
        {
            ref KeyValuePair<SteamPlayer, TValue> keyValuePair = ref array[i];
            arr[i] = new KeyValuePair<ulong, TValue>(keyValuePair.Key.playerID.steamID.m_SteamID, keyValuePair.Value);
        }

        ((ICollection<KeyValuePair<ulong, TValue>>)_dictionary).CopyTo(arr, arrayIndex);
    }

    void ICollection<KeyValuePair<Player, TValue>>.CopyTo(KeyValuePair<Player, TValue>[] array, int arrayIndex)
    {
        KeyValuePair<ulong, TValue>[] arr = new KeyValuePair<ulong, TValue>[array.Length];
        for (int i = 0; i < array.Length; ++i)
        {
            ref KeyValuePair<Player, TValue> keyValuePair = ref array[i];
            arr[i] = new KeyValuePair<ulong, TValue>(keyValuePair.Key.channel.owner.playerID.steamID.m_SteamID, keyValuePair.Value);
        }

        ((ICollection<KeyValuePair<ulong, TValue>>)_dictionary).CopyTo(arr, arrayIndex);
    }

    void ICollection<KeyValuePair<IPlayer, TValue>>.CopyTo(KeyValuePair<IPlayer, TValue>[] array, int arrayIndex)
    {
        KeyValuePair<ulong, TValue>[] arr = new KeyValuePair<ulong, TValue>[array.Length];
        for (int i = 0; i < array.Length; ++i)
        {
            ref KeyValuePair<IPlayer, TValue> keyValuePair = ref array[i];
            arr[i] = new KeyValuePair<ulong, TValue>(keyValuePair.Key.Steam64.m_SteamID, keyValuePair.Value);
        }

        ((ICollection<KeyValuePair<ulong, TValue>>)_dictionary).CopyTo(arr, arrayIndex);
    }

    void ICollection<KeyValuePair<WarfarePlayer, TValue>>.CopyTo(KeyValuePair<WarfarePlayer, TValue>[] array, int arrayIndex)
    {
        KeyValuePair<ulong, TValue>[] arr = new KeyValuePair<ulong, TValue>[array.Length];
        for (int i = 0; i < array.Length; ++i)
        {
            ref KeyValuePair<WarfarePlayer, TValue> keyValuePair = ref array[i];
            arr[i] = new KeyValuePair<ulong, TValue>(keyValuePair.Key.Steam64.m_SteamID, keyValuePair.Value);
        }

        ((ICollection<KeyValuePair<ulong, TValue>>)_dictionary).CopyTo(arr, arrayIndex);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<ulong, TValue>>.Remove(KeyValuePair<ulong, TValue> item)
    {
        if (_dictionary.TryGetValue(item.Key, out TValue value) && Equals(value, item.Value))
            return _dictionary.Remove(item.Key);

        return false;
    }

    bool ICollection<KeyValuePair<CSteamID, TValue>>.Remove(KeyValuePair<CSteamID, TValue> item)
    {
        if (!_dictionary.TryGetValue(item.Key.m_SteamID, out TValue value) || !Equals(value, item))
            return false;

        _dictionary.Remove(item.Key.m_SteamID);
        return true;
    }

    bool ICollection<KeyValuePair<Player, TValue>>.Remove(KeyValuePair<Player, TValue> item)
    {
        if (!_dictionary.TryGetValue(item.Key.channel.owner.playerID.steamID.m_SteamID, out TValue value) || !Equals(value, item))
            return false;

        _dictionary.Remove(item.Key.channel.owner.playerID.steamID.m_SteamID);
        return true;
    }

    bool ICollection<KeyValuePair<SteamPlayer, TValue>>.Remove(KeyValuePair<SteamPlayer, TValue> item)
    {
        if (!_dictionary.TryGetValue(item.Key.playerID.steamID.m_SteamID, out TValue value) || !Equals(value, item))
            return false;

        _dictionary.Remove(item.Key.playerID.steamID.m_SteamID);
        return true;
    }

    bool ICollection<KeyValuePair<IPlayer, TValue>>.Remove(KeyValuePair<IPlayer, TValue> item)
    {
        if (!_dictionary.TryGetValue(item.Key.Steam64.m_SteamID, out TValue value) || !Equals(value, item))
            return false;

        _dictionary.Remove(item.Key.Steam64.m_SteamID);
        return true;
    }

    bool ICollection<KeyValuePair<WarfarePlayer, TValue>>.Remove(KeyValuePair<WarfarePlayer, TValue> item)
    {
        if (!_dictionary.TryGetValue(item.Key.Steam64.m_SteamID, out TValue value) || !Equals(value, item))
            return false;

        _dictionary.Remove(item.Key.Steam64.m_SteamID);
        return true;
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

    bool IReadOnlyDictionary<Player, TValue>.ContainsKey(Player key)
    {
        return _dictionary.ContainsKey(key.channel.owner.playerID.steamID.m_SteamID);
    }

    bool IDictionary<Player, TValue>.ContainsKey(Player key)
    {
        return _dictionary.ContainsKey(key.channel.owner.playerID.steamID.m_SteamID);
    }

    bool IReadOnlyDictionary<SteamPlayer, TValue>.ContainsKey(SteamPlayer key)
    {
        return _dictionary.ContainsKey(key.playerID.steamID.m_SteamID);
    }

    bool IDictionary<SteamPlayer, TValue>.ContainsKey(SteamPlayer key)
    {
        return _dictionary.ContainsKey(key.playerID.steamID.m_SteamID);
    }

    bool IReadOnlyDictionary<IPlayer, TValue>.ContainsKey(IPlayer key)
    {
        return _dictionary.ContainsKey(key.Steam64.m_SteamID);
    }

    bool IDictionary<IPlayer, TValue>.ContainsKey(IPlayer key)
    {
        return _dictionary.ContainsKey(key.Steam64.m_SteamID);
    }

    bool IReadOnlyDictionary<WarfarePlayer, TValue>.ContainsKey(WarfarePlayer key)
    {
        return _dictionary.ContainsKey(key.Steam64.m_SteamID);
    }

    bool IDictionary<WarfarePlayer, TValue>.ContainsKey(WarfarePlayer key)
    {
        return _dictionary.ContainsKey(key.Steam64.m_SteamID);
    }

    bool IReadOnlyDictionary<CSteamID, TValue>.ContainsKey(CSteamID key)
    {
        return _dictionary.ContainsKey(key.m_SteamID);
    }

    bool IDictionary<CSteamID, TValue>.ContainsKey(CSteamID key)
    {
        return _dictionary.ContainsKey(key.m_SteamID);
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

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    IEnumerable<Player> IReadOnlyDictionary<Player, TValue>.Keys => throw new NotSupportedException();

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    IEnumerable<IPlayer> IReadOnlyDictionary<IPlayer, TValue>.Keys => throw new NotSupportedException();

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    IEnumerable<WarfarePlayer> IReadOnlyDictionary<WarfarePlayer, TValue>.Keys => throw new NotSupportedException();

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    IEnumerable<SteamPlayer> IReadOnlyDictionary<SteamPlayer, TValue>.Keys => throw new NotSupportedException();

    /// <inheritdoc />
    IEnumerable<ulong> IReadOnlyDictionary<ulong, TValue>.Keys => _dictionary.Keys;

    /// <inheritdoc />
    ICollection<ulong> IDictionary<ulong, TValue>.Keys => _dictionary.Keys;

    /// <inheritdoc cref="IReadOnlyDictionary{TKey,TValue}"/>
    IEnumerable<CSteamID> IReadOnlyDictionary<CSteamID, TValue>.Keys => _dictionary.Keys.Select(x => Unsafe.As<ulong, CSteamID>(ref x));

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    ICollection<CSteamID> IDictionary<CSteamID, TValue>.Keys => throw new NotSupportedException();

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    ICollection<Player> IDictionary<Player, TValue>.Keys => throw new NotSupportedException();

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    ICollection<SteamPlayer> IDictionary<SteamPlayer, TValue>.Keys => throw new NotSupportedException();

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    ICollection<IPlayer> IDictionary<IPlayer, TValue>.Keys => throw new NotSupportedException();

    /// <summary>Not supported</summary>
    /// <exception cref="NotSupportedException"/>
    ICollection<WarfarePlayer> IDictionary<WarfarePlayer, TValue>.Keys => throw new NotSupportedException();

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<IPlayer, TValue>.Values => _dictionary.Values;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<WarfarePlayer, TValue>.Values => _dictionary.Values;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<SteamPlayer, TValue>.Values => _dictionary.Values;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<Player, TValue>.Values => _dictionary.Values;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<CSteamID, TValue>.Values => _dictionary.Values;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<ulong, TValue>.Values => _dictionary.Values;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<IPlayer, TValue>>.IsReadOnly => false;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<WarfarePlayer, TValue>>.IsReadOnly => false;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<SteamPlayer, TValue>>.IsReadOnly => false;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<Player, TValue>>.IsReadOnly => false;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<CSteamID, TValue>>.IsReadOnly => false;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<ulong, TValue>>.IsReadOnly => false;
    #endregion
}