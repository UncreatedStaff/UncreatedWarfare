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
        PlayerName = player.playerName;
        CharacterName = player.characterName;
        NickName = player.nickName;
        Steam64 = player.steamID.m_SteamID;
        WasFound = true;
    }
    public PlayerNames(ulong player)
    {
        string ts = player.ToString();
        PlayerName = ts;
        CharacterName = ts;
        NickName = ts;
        Steam64 = player;
        WasFound = true;
    }
    public PlayerNames(SteamPlayer player)
    {
        PlayerName = player.playerID.playerName;
        CharacterName = player.playerID.characterName;
        NickName = player.playerID.nickName;
        Steam64 = player.playerID.steamID.m_SteamID;
        WasFound = true;
    }
    public PlayerNames(Player player)
    {
        PlayerName = player.channel.owner.playerID.playerName;
        CharacterName = player.channel.owner.playerID.characterName;
        NickName = player.channel.owner.playerID.nickName;
        Steam64 = player.channel.owner.playerID.steamID.m_SteamID;
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

    public override string ToString() => ToString(true);
    public string ToString(bool steamId)
    {
        string? pn = PlayerName;
        string? cn = CharacterName;
        string? nn = NickName;
        string s64 = Steam64.ToString("D17");
        bool pws = string.IsNullOrWhiteSpace(pn);
        bool cws = string.IsNullOrWhiteSpace(cn);
        bool nws = string.IsNullOrWhiteSpace(nn);
        if (pws && cws && nws)
            return s64;
        if (pws)
        {
            if (cws)
                return steamId ? (s64 + " (" + nn + ")") : nn;
            if (nws || nn!.Equals(cn, StringComparison.Ordinal))
                return steamId ? (s64 + " (" + cn + ")") : cn;
            return steamId ? (s64 + " (" + cn + " | " + nn + ")") : (cn + " | " + nn);
        }
        if (cws)
        {
            if (pws)
                return steamId ? (s64 + " (" + nn + ")") : nn;
            if (nws || nn!.Equals(pn, StringComparison.Ordinal))
                return steamId ? (s64 + " (" + pn + ")") : pn;
            return steamId ? (s64 + " (" + pn + " | " + nn + ")") : (pn + " | " + nn);
        }
        if (nws)
        {
            if (pws)
                return steamId ? (s64 + " (" + cn + ")") : cn;
            if (cws || cn!.Equals(pn, StringComparison.Ordinal))
                return steamId ? (s64 + " (" + pn + ")") : pn;
            return steamId ? (s64 + " (" + pn + " | " + cn + ")") : pn + " | " + cn;
        }

        bool nep = nn!.Equals(pn, StringComparison.Ordinal);
        bool nec = nn.Equals(cn, StringComparison.Ordinal);
        bool pec = nec && nep || pn!.Equals(cn, StringComparison.Ordinal);
        if (nep && nec)
            return steamId ? (s64 + " (" + nn + ")") : nn;
        if (pec || nec)
            return steamId ? (s64 + " (" + pn + " | " + nn + ")") : (pn + " | " + nn); 
        if (nep)
            return steamId ? (s64 + " (" + pn + " | " + cn + ")") : (pn + " | " + cn);

        return steamId ? (s64 + " (" + pn + " | " + cn + " | " + nn + ")") : (pn + " | " + cn + " | " + nn);
    }
    public static bool operator ==(PlayerNames left, PlayerNames right) => left.Steam64 == right.Steam64;
    public static bool operator !=(PlayerNames left, PlayerNames right) => left.Steam64 != right.Steam64;
    public override bool Equals(object obj) => obj is PlayerNames pn && Steam64 == pn.Steam64;
    public override int GetHashCode() => Steam64.GetHashCode();
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags) => new OfflinePlayer(in this).Translate(language, format, target, culture, ref flags);

    public static string SelectPlayerName(PlayerNames names) => names.PlayerName;
    public static string SelectCharacterName(PlayerNames names) => names.CharacterName;
    public static string SelectNickName(PlayerNames names) => names.NickName;
}
public sealed class UCPlayerEvents : IDisposable
{
    public UCPlayer Player { get; private set; }
    public UCPlayerEvents(UCPlayer player)
    {
        Player = player;
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
        if (key is PlayerKey.Primary or PlayerKey.Secondary or PlayerKey.Reserved || (int)key < 0 || (int)key >= KeyCount)
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
                _lastKeys[i] = keys[i];
            _first = false;
        }
        else
        {
            for (int i = 0; i < KeyCount; ++i)
            {
                bool st = keys[i];
                if (EventMask[i])
                {
                    bool ost = _lastKeys[i];
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
                _lastKeys[i] = st;
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
    [Obsolete("Replaced with PlayerInput.pendingPrimaryAttackInput.")]
    Primary = 1,
    [Obsolete("Replaced with PlayerInput.pendingSecondaryAttackInput.")]
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