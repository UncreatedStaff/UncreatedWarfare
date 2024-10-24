using DanielWillett.SpeedBytes;
using System;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Players;

public struct PlayerNames : IPlayer
{
    public static readonly PlayerNames Nil = new PlayerNames { CharacterName = string.Empty, NickName = string.Empty, PlayerName = string.Empty, Steam64 = default };
    public static readonly PlayerNames Console = new PlayerNames { CharacterName = "Console", NickName = "Console", PlayerName = "Console", DisplayName = "Console", Steam64 = default };
    public CSteamID Steam64;
    public string PlayerName;
    public string CharacterName;
    public string NickName;

    // Admins have preset display names so they show up consistantly on offense records
    public string? DisplayName;
    
    public bool WasFound;
    CSteamID IPlayer.Steam64 => Steam64;

    public PlayerNames(Player player)
    {
        PlayerName = player.channel.owner.playerID.playerName;
        CharacterName = player.channel.owner.playerID.characterName;
        NickName = player.channel.owner.playerID.nickName;
        Steam64 = player.channel.owner.playerID.steamID;
        WasFound = true;
    }

    public static void Write(ByteWriter writer, in PlayerNames obj)
    {
        writer.Write(obj.Steam64.m_SteamID);
        writer.WriteShort(obj.PlayerName);
        writer.WriteShort(obj.CharacterName);
        writer.WriteShort(obj.NickName);
        writer.WriteNullableShort(obj.DisplayName);
    }

    public static PlayerNames Read(ByteReader reader) =>
        new PlayerNames
        {
            Steam64 = new CSteamID(reader.ReadUInt64()),
            PlayerName = reader.ReadShortString(),
            CharacterName = reader.ReadShortString(),
            NickName = reader.ReadShortString(),
            DisplayName = reader.ReadNullableShortString()
        };

    public readonly override string ToString() => ToString(true);
    public readonly string ToString(bool steamId)
    {
        string s64 = Steam64.m_SteamID.ToString("D17");

        if (!string.IsNullOrEmpty(DisplayName))
            return steamId ? s64 + " (" + DisplayName + ")" : DisplayName;

        string? pn = PlayerName;
        string? cn = CharacterName;
        string? nn = NickName;
        bool pws = string.IsNullOrWhiteSpace(pn);
        bool cws = string.IsNullOrWhiteSpace(cn);
        bool nws = string.IsNullOrWhiteSpace(nn);
        if (pws && cws && nws)
            return s64;

        if (pws)
        {
            if (cws)
                return steamId ? s64 + " (" + nn + ")" : nn;
            if (nws || nn.Equals(cn, StringComparison.Ordinal))
                return steamId ? s64 + " (" + cn + ")" : cn;
            return steamId ? s64 + " (" + cn + " | " + nn + ")" : cn + " | " + nn;
        }
        if (cws)
        {
            if (pws)
                return steamId ? s64 + " (" + nn + ")" : nn;
            if (nws || nn.Equals(pn, StringComparison.Ordinal))
                return steamId ? s64 + " (" + pn + ")" : pn;
            return steamId ? s64 + " (" + pn + " | " + nn + ")" : pn + " | " + nn;
        }
        if (nws)
        {
            if (pws)
                return steamId ? s64 + " (" + cn + ")" : cn;
            if (cws || cn.Equals(pn, StringComparison.Ordinal))
                return steamId ? s64 + " (" + pn + ")" : pn;
            return steamId ? s64 + " (" + pn + " | " + cn + ")" : pn + " | " + cn;
        }

        bool nep = nn.Equals(pn, StringComparison.Ordinal);
        bool nec = nn.Equals(cn, StringComparison.Ordinal);
        bool pec = nec && nep || pn.Equals(cn, StringComparison.Ordinal);
        if (nep && nec)
            return steamId ? s64 + " (" + nn + ")" : nn;
        if (pec || nec)
            return steamId ? s64 + " (" + pn + " | " + nn + ")" : pn + " | " + nn;
        if (nep)
            return steamId ? s64 + " (" + pn + " | " + cn + ")" : pn + " | " + cn;

        return steamId ? s64 + " (" + pn + " | " + cn + " | " + nn + ")" : pn + " | " + cn + " | " + nn;
    }
    public static bool operator ==(PlayerNames left, PlayerNames right) => left.Steam64.m_SteamID == right.Steam64.m_SteamID;
    public static bool operator !=(PlayerNames left, PlayerNames right) => left.Steam64.m_SteamID != right.Steam64.m_SteamID;
    public override bool Equals(object? obj) => obj is PlayerNames pn && Steam64.m_SteamID == pn.Steam64.m_SteamID;
    public override int GetHashCode() => Steam64.GetHashCode();
    string ITranslationArgument.Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters) => new OfflinePlayer(in this).Translate(formatter, in parameters);

    public static string SelectPlayerName(PlayerNames names) => names.PlayerName;
    public static string SelectCharacterName(PlayerNames names) => names.CharacterName;
    public static string SelectNickName(PlayerNames names) => names.NickName;

    public readonly string GetDisplayNameOrPlayerName()
    {
        return DisplayName ?? PlayerName;
    }
    public readonly string GetDisplayNameOrCharacterName()
    {
        return DisplayName ?? CharacterName;
    }
    public readonly string GetDisplayNameOrNickName()
    {
        return DisplayName ?? NickName;
    }
}