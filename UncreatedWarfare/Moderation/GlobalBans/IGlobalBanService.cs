using System;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Moderation.GlobalBans;

public interface IGlobalBanService
{
    Task<GlobalBan> GetGlobalBanAsync(CSteamID steam64, IPv4Range ipAddress, HWID[] hwids, CancellationToken token = default);
}

public readonly struct GlobalBan
{
    public DateTimeOffset BanTimestamp { get; }
    public string? Message { get; }
    public string? BanSystemName { get; }
    public bool IsBanned { get; }
    public uint BanID { get; }
    public ulong BannedPlayer { get; }
    public string? BannedPlayerName { get; }

    public GlobalBan(DateTimeOffset banTimestamp, string? message, string? banSystemName, uint banID, ulong bannedPlayer, string? bannedPlayerName)
    {
        BanTimestamp = banTimestamp;
        Message = message;
        BanSystemName = banSystemName;
        BanID = banID;
        BannedPlayer = bannedPlayer;
        BannedPlayerName = bannedPlayerName;
        IsBanned = true;
    }
}