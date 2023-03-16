using SDG.Unturned;
using System;
using System.Globalization;
using Uncreated.Encoding;
using Uncreated.Warfare;
using Uncreated.Warfare.Events;
using UnityEngine;

namespace Uncreated.Players;

public struct PlayerNames : IPlayer
{
    public static readonly PlayerNames Nil = new PlayerNames { CharacterName = string.Empty, NickName = string.Empty, PlayerName = string.Empty, Steam64 = 0 };
    public static readonly PlayerNames Console = new PlayerNames { CharacterName = "Console", NickName = "Console", PlayerName = "Console", Steam64 = 0 };
    public ulong Steam64;
    public string PlayerName;
    public string CharacterName;
    public string NickName;
    public bool WasFound;
    ulong IPlayer.Steam64 => Steam64;

    public PlayerNames(SteamPlayerID player)
    {
        this.PlayerName = player.playerName;
        this.CharacterName = player.characterName;
        this.NickName = player.nickName;
        this.Steam64 = player.steamID.m_SteamID;
        WasFound = true;
    }
    public PlayerNames(ulong player)
    {
        string ts = player.ToString();
        this.PlayerName = ts;
        this.CharacterName = ts;
        this.NickName = ts;
        this.Steam64 = player;
        WasFound = true;
    }
    public PlayerNames(SteamPlayer player)
    {
        this.PlayerName = player.playerID.playerName;
        this.CharacterName = player.playerID.characterName;
        this.NickName = player.playerID.nickName;
        this.Steam64 = player.playerID.steamID.m_SteamID;
        WasFound = true;
    }
    public PlayerNames(Player player)
    {
        this.PlayerName = player.channel.owner.playerID.playerName;
        this.CharacterName = player.channel.owner.playerID.characterName;
        this.NickName = player.channel.owner.playerID.nickName;
        this.Steam64 = player.channel.owner.playerID.steamID.m_SteamID;
        WasFound = true;
    }
    public static void Write(ByteWriter writer, PlayerNames obj)
    {
        writer.Write(obj.Steam64);
        writer.Write(obj.PlayerName);
        writer.Write(obj.CharacterName);
        writer.Write(obj.NickName);
    }
    public static PlayerNames Read(ByteReader reader) =>
        new PlayerNames
        {
            Steam64 = reader.ReadUInt64(),
            PlayerName = reader.ReadString(),
            CharacterName = reader.ReadString(),
            NickName = reader.ReadString()
        };
    public override string ToString() => PlayerName;
    public static bool operator ==(PlayerNames left, PlayerNames right) => left.Steam64 == right.Steam64;
    public static bool operator !=(PlayerNames left, PlayerNames right) => left.Steam64 != right.Steam64;
    public override bool Equals(object obj) => obj is PlayerNames pn && this.Steam64 == pn.Steam64;
    public override int GetHashCode() => Steam64.GetHashCode();
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags) => new OfflinePlayer(in this).Translate(language, format, target, culture, ref flags);
}
public sealed class UCPlayerEvents : IDisposable
{
    public UCPlayer Player { get; private set; }
    public UCPlayerEvents(UCPlayer player)
    {
        this.Player = player;
        Player.Player.inventory.onDropItemRequested += OnDropItemRequested;
        Player.Player.inventory.onInventoryRemoved += OnItemRemoved;
    }
    public void Dispose()
    {
        if (Player.Player != null)
        {
            Player.Player.inventory.onInventoryRemoved -= OnItemRemoved;
            Player.Player.inventory.onDropItemRequested -= OnDropItemRequested;
        }

        Player = null!;
    }
    private void OnDropItemRequested(PlayerInventory inventory, Item item, ref bool shouldAllow) => EventDispatcher.InvokeOnDropItemRequested(Player ?? UCPlayer.FromPlayer(inventory.player)!, inventory, item, ref shouldAllow);
    private void OnItemRemoved(byte page, byte index, ItemJar jar)
    {
        if (Player is { IsOnline: true })
            EventDispatcher.InvokeOnItemRemoved(Player, page, index, jar);
    }
}
public sealed class UCPlayerKeys : IDisposable
{
    private static readonly int KeyCount = 10 + ControlsSettings.NUM_PLUGIN_KEYS;

    private static readonly KeyDown?[] DownEvents = new KeyDown?[KeyCount];
    private static readonly KeyUp?[] UpEvents = new KeyUp?[KeyCount];
    private static readonly bool[] EventMask = new bool[KeyCount];
    private static bool _anySubs;

    public readonly UCPlayer Player;
    private bool[] _lastKeys = new bool[KeyCount];
    private float[] _keyDownTimes = new float[KeyCount];
    private bool _first = true;
    private bool _disposed;
    public UCPlayerKeys(UCPlayer player)
    {
        Player = player;
        if (Player.Player.input.keys.Length != KeyCount)
            throw new InvalidOperationException("Nelson changed the amount of keys in PlayerInput!");
        float time = Time.realtimeSinceStartup;
        for (int i = 0; i < KeyCount; ++i)
            _keyDownTimes[i] = time;
    }
    public bool IsKeyDown(PlayerKey key)
    {
        CheckKey(key);
        return !_disposed && Player.Player.input.keys[(int)key];
    }
    public static void SubscribeKeyUp(KeyUp action, PlayerKey key)
    {
        if (action == null) return;
        _anySubs = true;
        CheckKey(key);
        ref KeyUp? d = ref UpEvents[(int)key];
        EventMask[(int)key] = true;
        d += action;
    }
    public static void SubscribeKeyDown(KeyDown action, PlayerKey key)
    {
        if (action == null) return;
        _anySubs = true;
        CheckKey(key);
        ref KeyDown? d = ref DownEvents[(int)key];
        EventMask[(int)key] = true;
        d += action;
    }
#nullable disable
    public static void UnsubscribeKeyUp(KeyUp action, PlayerKey key)
    {
        if (action == null) return;
        CheckKey(key);
        ref KeyUp d = ref UpEvents[(int)key];
        if (d != null)
            d -= action;
        CheckAnySubs();
    }
    public static void UnsubscribeKeyDown(KeyDown action, PlayerKey key)
    {
        if (action == null) return;
        CheckKey(key);
        ref KeyDown d = ref DownEvents[(int)key];
        if (d != null)
            d -= action;
        CheckAnySubs();
    }
#nullable restore
    private static void CheckKey(PlayerKey key)
    {
#pragma warning disable CS0618
        if (key == PlayerKey.Reserved || (int)key < 0 || (int)key >= KeyCount)
            throw new ArgumentOutOfRangeException(nameof(key), key.ToString() + " doesn't match a valid key.");
#pragma warning restore CS0618
    }
    private static void CheckAnySubs()
    {
        _anySubs = false;
        for (int i = 0; i < UpEvents.Length; ++i)
        {
            if (UpEvents[i] != null)
            {
                _anySubs = true;
                EventMask[i] = true;
            }
            else
                EventMask[i] = false;
        }
        for (int i = 0; i < DownEvents.Length; ++i)
        {
            if (DownEvents[i] != null)
            {
                _anySubs = true;
                EventMask[i] = true;
            }
        }
    }
    internal void Simulate()
    {
        if (_disposed) return;
        bool[] keys = Player.Player.input.keys;
        if (_first || !_anySubs)
        {
            for (int i = 0; i < KeyCount; ++i)
                this._lastKeys[i] = keys[i];
            _first = false;
        }
        else
        {
            for (int i = 0; i < KeyCount; ++i)
            {
                bool st = keys[i];
                if (EventMask[i])
                {
                    bool ost = this._lastKeys[i];
                    if (st == ost) continue;
                    if (st)
                    {
                        EventDispatcher.OnKeyDown(Player, (PlayerKey)i, DownEvents[i]);
                        _keyDownTimes[i] = Time.realtimeSinceStartup;
                    }
                    else
                    {
                        EventDispatcher.OnKeyUp(Player, (PlayerKey)i, Time.realtimeSinceStartup - _keyDownTimes[i], UpEvents[i]);
                    }
                }
                this._lastKeys[i] = st;
            }
        }
    }
    public void Dispose()
    {
        if (!_disposed)
        {
            _lastKeys = null!;
            _keyDownTimes = null!;
            _first = false;
            _disposed = true;
        }
    }
}

public delegate void KeyDown(UCPlayer player, ref bool handled);
public delegate void KeyUp(UCPlayer player, float timeDown, ref bool handled);

public enum PlayerKey
{
    Jump = 0,
    Primary = 1,
    Secondary = 2,
    Crouch = 3,
    Prone = 4,
    Sprint = 5,
    LeanLeft = 6,
    LeanRight = 7,
    [Obsolete("This is not in use right now.")]
    Reserved = 8,
    SteadyAim = 9,
    PluginKey1 = 10,
    PluginKey2 = 11,
    PluginKey3 = 12,
    PluginKey4 = 13,
    PluginKey5 = 14
}