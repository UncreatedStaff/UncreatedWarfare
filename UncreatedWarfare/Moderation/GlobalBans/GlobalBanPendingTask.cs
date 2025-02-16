using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.PendingTasks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Moderation.GlobalBans;

[PlayerTask]
internal class GlobalBanPendingTask : IPlayerPendingTask
{
    private readonly IServiceProvider _serviceProvider;

    private readonly string? _discordInvite;

    public bool CanReject => true;

    public GlobalBanPendingTask(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _discordInvite = _serviceProvider.GetRequiredService<IConfiguration>()["discord_invite_code"];
    }

    public async Task<bool> RunAsync(PlayerPending e, CancellationToken token = default)
    {
        IGlobalBanService[] services = _serviceProvider.GetServices<IGlobalBanService>().ToArrayFast();

        CSteamID steam64 = e.Steam64;
        IPv4Range ip = e.PendingPlayer.transportConnection.TryGetIPv4Address(out uint ipPacked)
            ? new IPv4Range(ipPacked, 32)
            : default;

        byte[][] hwidArray = e.PendingPlayer.playerID.GetHwids().ToArrayFast();

        HWID[] hwids = new HWID[hwidArray.Length];
        for (int i = 0; i < hwids.Length; ++i)
            hwids[i] = new HWID(hwidArray[i]);

        GlobalBan[] bans = await Task.WhenAll(services.Select(x => x.GetGlobalBanAsync(steam64, ip, hwids, token)));

        // aggregate newest ban
        int newestBan = -1;
        DateTimeOffset newestBanTime = default;
        for (int i = 0; i < bans.Length; ++i)
        {
            ref GlobalBan ban = ref bans[i];
            if (!ban.IsBanned)
                continue;

            if (newestBan >= 0 && newestBanTime <= ban.BanTimestamp)
                continue;

            newestBan = i;
            newestBanTime = ban.BanTimestamp;
        }

        if (newestBan == -1)
            return true;

        GlobalBan newestBanInfo = bans[newestBan];

        ModerationTranslations translations = _serviceProvider.GetRequiredService<TranslationInjection<ModerationTranslations>>().Value;

        ILogger logger = _serviceProvider.GetRequiredService<ILogger<GlobalBanPendingTask>>();

        IGlobalBanWhitelistService? whitelister = _serviceProvider.GetService<IGlobalBanWhitelistService>();
        if (whitelister != null)
        {
            DateTimeOffset? whitelistDate = await whitelister.GetWhitelistEffectiveDate(steam64, token).ConfigureAwait(false);
            if (whitelistDate.HasValue && whitelistDate.Value < newestBanTime.DateTime)
            {
                logger.LogInformation("Global ban whitelisted pending player {0} is banned on {1}.", e.Steam64, newestBanInfo.BanSystemName);
                return true;
            }
        }

        logger.LogInformation("Player {0} tried to join while global banned by {1}.", e.Steam64, newestBanInfo.BanSystemName);

        if (newestBanInfo.BannedPlayer == steam64.m_SteamID || newestBanInfo.BannedPlayer == 0)
        {
            e.RejectReason = translations.RejectGloballyBanned.Translate(
                newestBanInfo.BanSystemName ?? "Unknown",
                newestBanInfo.BanTimestamp.UtcDateTime,
                _discordInvite ?? string.Empty,
                newestBanInfo.BanID, 
                e.LanguageInfo,
                e.CultureInfo,
                e.TimeZone
            );
        }
        else
        {
            e.RejectReason = translations.RejectGloballyLinkedBanned.Translate(
                newestBanInfo.BanSystemName ?? "Unknown",
                newestBanInfo.BanTimestamp.UtcDateTime,
                _discordInvite ?? string.Empty,
                newestBanInfo.BannedPlayer,
                newestBanInfo.BannedPlayerName ?? newestBanInfo.BannedPlayer.ToString("D17", CultureInfo.InvariantCulture),
                newestBanInfo.BanID,
                e.LanguageInfo,
                e.CultureInfo,
                e.TimeZone
            );
        }

        return false;
    }

    public void Apply(WarfarePlayer player) { }
}
