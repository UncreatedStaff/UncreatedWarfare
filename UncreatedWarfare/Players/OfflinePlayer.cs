using System;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players;

public readonly struct OfflinePlayerName(CSteamID steam64, string name) : IPlayer
{
    public CSteamID Steam64 { get; } = steam64;
    public string Name { get; } = name;
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        // string? format = parameters.Format.Format;
        // if (format is null) goto end;
        // 
        // if (format.Equals(WarfarePlayer.FormatCharacterName, StringComparison.Ordinal) ||
        //     format.Equals(WarfarePlayer.FormatNickName, StringComparison.Ordinal) ||
        //     format.Equals(WarfarePlayer.FormatPlayerName, StringComparison.Ordinal))
        //     return Name;
        // if (format.Equals(WarfarePlayer.FormatSteam64, StringComparison.Ordinal))
        //     goto end;
        // string hex = TeamManager.GetTeamHexColor(pl is null || !pl.IsOnline ? (GameThread.IsCurrent && PlayerSave.TryReadSaveFile(Steam64.m_SteamID, out PlayerSave save) ? save.Team : 0) : pl.GetTeam());
        // if (format.Equals(WarfarePlayer.FormatColoredCharacterName, StringComparison.Ordinal) ||
        //     format.Equals(WarfarePlayer.FormatColoredNickName, StringComparison.Ordinal) ||
        //     format.Equals(WarfarePlayer.FormatColoredPlayerName, StringComparison.Ordinal))
        //     return Localization.Colorize(hex, Name, flags);
        // if (format.Equals(WarfarePlayer.FormatColoredSteam64, StringComparison.Ordinal))
        //     return Localization.Colorize(hex, Steam64.m_SteamID.ToString(culture ?? Data.LocalLocale), flags);
        // end:
        return Steam64.m_SteamID.ToString(parameters.Culture);
    }
}

public struct OfflinePlayer : IPlayer
{
    private readonly CSteamID _s64;
    private PlayerNames? _names;
    public readonly CSteamID Steam64 => _s64;
    public OfflinePlayer(CSteamID steam64)
    {
        _s64 = steam64;
        _names = null;
    }
    public OfflinePlayer(in PlayerNames names)
    {
        _s64 = names.Steam64;
        _names = names;
    }
    public async ValueTask CacheUsernames(IUserDataService userDataService, CancellationToken token = default)
    {
        if (!TryCacheLocal())
            _names = await userDataService.GetUsernamesAsync(_s64.m_SteamID, token).ConfigureAwait(false);
    }
    public bool TryCacheLocal()
    {
        // WarfarePlayer? pl = WarfarePlayer.FromCSteamID(Steam64);
        // if (pl != null)
        //     _names = pl.Name;
        // return _names.HasValue;

        return false;
    }
    public readonly string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        // string? format = parameters.Format.Format;
        // WarfarePlayer? pl = WarfarePlayer.FromCSteamID(Steam64);
        // if (format is null || !_names.HasValue) goto end;
        // PlayerNames names = _names.Value;
        // 
        // if (format.Equals(WarfarePlayer.FormatCharacterName, StringComparison.Ordinal))
        //     return names.CharacterName;
        // if (format.Equals(WarfarePlayer.FormatNickName, StringComparison.Ordinal))
        //     return names.NickName;
        // if (format.Equals(WarfarePlayer.FormatPlayerName, StringComparison.Ordinal))
        //     return names.PlayerName;
        // if (format.Equals(WarfarePlayer.FormatSteam64, StringComparison.Ordinal))
        //     goto end;
        // string hex = TeamManager.GetTeamHexColor(pl is null || !pl.IsOnline ? (GameThread.IsCurrent && PlayerSave.TryReadSaveFile(_s64.m_SteamID, out PlayerSave save) ? save.Team : 0) : pl.GetTeam());
        // if (format.Equals(WarfarePlayer.FormatColoredCharacterName, StringComparison.Ordinal))
        //     return Localization.Colorize(hex, names.CharacterName, flags);
        // if (format.Equals(WarfarePlayer.FormatColoredNickName, StringComparison.Ordinal))
        //     return Localization.Colorize(hex, names.NickName, flags);
        // if (format.Equals(WarfarePlayer.FormatColoredPlayerName, StringComparison.Ordinal))
        //     return Localization.Colorize(hex, names.PlayerName, flags);
        // if (format.Equals(WarfarePlayer.FormatColoredSteam64, StringComparison.Ordinal))
        //     return Localization.Colorize(hex, _s64.m_SteamID.ToString(culture ?? Data.LocalLocale), flags);
        // end:
        return _s64.m_SteamID.ToString(parameters.Culture);
    }
}