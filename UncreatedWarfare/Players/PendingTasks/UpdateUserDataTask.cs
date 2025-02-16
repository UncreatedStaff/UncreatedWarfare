using SDG.NetTransport;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.PendingTasks;

/// <summary>
/// Updates a player's IP addresses, HWIDs, and user data in our database.
/// </summary>
[PlayerTask]
public class UpdateUserDataTask : IPlayerPendingTask
{
    private readonly IUserDataService _userDataService;
    private readonly DatabaseInterface _moderationSql;
    private bool _isRemotePlay;
    private WarfareUserData? _userData;
    public UpdateUserDataTask(IUserDataService userDataService, DatabaseInterface moderationSql)
    {
        _userDataService = userDataService;
        _moderationSql = moderationSql;
    }

    async Task<bool> IPlayerPendingTask.RunAsync(PlayerPending e, CancellationToken token)
    {
        bool valid = false;
        _userData = await _userDataService.AddOrUpdateAsync(e.Steam64.m_SteamID, (data, db) =>
        {
            data.CharacterName = e.CharacterName;
            data.NickName = e.NickName;
            data.PlayerName = e.PlayerName;

            valid = TryUpdateIdentifiers(e, data, db);
            if (valid)
                data.LastJoined = DateTimeOffset.UtcNow;

        }, token).ConfigureAwait(false);

        return _userData != null && valid;
    }

    private bool TryUpdateIdentifiers(PlayerPending e, WarfareUserData data, IDbContext dbContext)
    {
        ITransportConnection connection = e.PendingPlayer.transportConnection;

        if (connection.TryGetSteamId(out ulong steam64) && steam64 != e.Steam64.m_SteamID)
        {
            e.RejectReason = "Inconsistant Steam ID.";
            return false;
        }

        bool hasIpv4 = false;
        uint ipv4 = 0;
        if (!Provider.configData.Server.Use_FakeIP && !(hasIpv4 = connection.TryGetIPv4Address(out ipv4)))
        {
#if !DEBUG // allow server codes for testing
            e.RejectReason = "No valid IPv4 address.";
            return false;
#else
            WarfareModule.Singleton.ServiceProvider.Resolve<ILogger<UpdateUserDataTask>>().LogWarning("Player {0} connecting without an IP address (probably via Server Code).", e.Steam64);
#endif
        }

        _isRemotePlay = hasIpv4 && _moderationSql.IsRemotePlay(ipv4);

        byte[][] hwids = e.PendingPlayer.playerID.GetHwids().ToArrayFast();

        if (hwids.Length is not 2 and not 3 || Array.Exists(hwids, x => x.Length != HWID.Size))
        {
            e.RejectReason = "Su" + "spe" + "cte" + "d H" + "WI" + "D " + "sp" + "o" + "of" + "er.";
            return false;
        }

        bool found;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        for (int i = 0; i < hwids.Length; ++i)
        {
            HWID hwid = new HWID(hwids[i]);

            found = false;
            foreach (PlayerHWID existingHwid in data.HWIDs)
            {
                if (existingHwid.Index != i || existingHwid.HWID != hwid)
                {
                    continue;
                }

                found = true;
                ++existingHwid.LoginCount;
                existingHwid.LastLogin = now;
                dbContext.Update(existingHwid);
                break;
            }

            if (found)
                continue;

            PlayerHWID newHwid = new PlayerHWID(0, i, e.Steam64.m_SteamID, hwid, 1, now, now);
            dbContext.Add(newHwid);
            data.HWIDs.Add(newHwid);
        }

        if (!hasIpv4)
            return true;

        found = false;
        foreach (PlayerIPAddress existingIp in data.IPAddresses)
        {
            if (existingIp.PackedIP != ipv4)
            {
                continue;
            }

            found = true;
            ++existingIp.LoginCount;
            existingIp.LastLogin = now;
            existingIp.RemotePlay = _isRemotePlay;
            dbContext.Update(existingIp);
            break;
        }

        if (found)
            return true;

        PlayerIPAddress newIp = new PlayerIPAddress(0, e.Steam64.m_SteamID, ipv4, 1, now, now)
        {
            RemotePlay = _isRemotePlay
        };

        data.IPAddresses.Add(newIp);

        return true;
    }

    void IPlayerPendingTask.Apply(WarfarePlayer player)
    {
        player.Names.DisplayName = _userData!.DisplayName;
    }

    bool IPlayerPendingTask.CanReject => true;
}
