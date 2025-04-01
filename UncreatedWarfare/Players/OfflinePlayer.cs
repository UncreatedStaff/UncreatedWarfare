using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Players;

public struct OfflinePlayer : IPlayer
{
    private PlayerNames _names;
    private Color32 _teamColor;
    public readonly CSteamID Steam64 => _names.Steam64;

    public OfflinePlayer(CSteamID steam64, Team? team = null)
    {
        _names.Steam64 = steam64;
        _teamColor = team?.Faction.Color ?? FactionInfo.NoFaction.Color;
    }

    public OfflinePlayer(in PlayerNames names, Team? team = null)
    {
        _names = names;
        _teamColor = team?.Faction.Color ?? FactionInfo.NoFaction.Color;
    }

    public async ValueTask CacheUsernames(IUserDataService userDataService, CancellationToken token = default)
    {
        _names = await userDataService.GetUsernamesAsync(_names.Steam64.m_SteamID, token).ConfigureAwait(false);
    }

    public bool TryCacheLocal(IPlayerService playerService)
    {
        WarfarePlayer? pl = playerService.GetOnlinePlayerOrNull(Steam64);
        if (pl == null)
            return false;

        _names = pl.Names;
        return true;
    }

    public readonly string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        ref readonly PlayerNames names = ref _names;
        
        if (names.WasFound)
        {
            if (WarfarePlayer.FormatCharacterName.Match(in parameters))
                return names.CharacterName;
            if (WarfarePlayer.FormatNickName.Match(in parameters))
                return names.NickName;
            if (WarfarePlayer.FormatPlayerName.Match(in parameters))
                return names.PlayerName;

            if (WarfarePlayer.FormatColoredCharacterName.Match(in parameters))
                return formatter.Colorize(names.CharacterName, _teamColor, parameters.Options);
            if (WarfarePlayer.FormatColoredNickName.Match(in parameters))
                return formatter.Colorize(names.NickName, _teamColor, parameters.Options);
            if (WarfarePlayer.FormatColoredPlayerName.Match(in parameters))
                return formatter.Colorize(names.PlayerName, _teamColor, parameters.Options);

            if (WarfarePlayer.FormatDisplayOrCharacterName.Match(in parameters))
                return names.GetDisplayNameOrCharacterName();
            if (WarfarePlayer.FormatDisplayOrNickName.Match(in parameters))
                return names.GetDisplayNameOrNickName();
            if (WarfarePlayer.FormatDisplayOrPlayerName.Match(in parameters))
                return names.GetDisplayNameOrPlayerName();

            if (WarfarePlayer.FormatColoredDisplayOrCharacterName.Match(in parameters))
                return formatter.Colorize(names.GetDisplayNameOrCharacterName(), _teamColor, parameters.Options);
            if (WarfarePlayer.FormatColoredDisplayOrNickName.Match(in parameters))
                return formatter.Colorize(names.GetDisplayNameOrNickName(), _teamColor, parameters.Options);
            if (WarfarePlayer.FormatColoredDisplayOrPlayerName.Match(in parameters))
                return formatter.Colorize(names.GetDisplayNameOrPlayerName(), _teamColor, parameters.Options);
        }

        return names.Steam64.m_SteamID.ToString("D17", parameters.Culture);
    }
}