using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Players;

public struct OfflinePlayer : IPlayer
{
    private PlayerNames _names;
    public readonly CSteamID Steam64 => _names.Steam64;

    public OfflinePlayer(CSteamID steam64)
    {
        _names.Steam64 = steam64;
    }

    public OfflinePlayer(in PlayerNames names)
    {
        _names = names;
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
                return formatter.Colorize(names.CharacterName, FactionInfo.NoFaction.Color, parameters.Options);
            if (WarfarePlayer.FormatColoredNickName.Match(in parameters))
                return formatter.Colorize(names.NickName, FactionInfo.NoFaction.Color, parameters.Options);
            if (WarfarePlayer.FormatColoredPlayerName.Match(in parameters))
                return formatter.Colorize(names.PlayerName, FactionInfo.NoFaction.Color, parameters.Options);
        }

        return names.Steam64.m_SteamID.ToString("D17", parameters.Culture);
    }
}