using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using static Uncreated.Warfare.Moderation.DatabaseInterface;

namespace Uncreated.Warfare.Moderation;

/// <summary>
/// Handles applying bans, kicks, etc when players join or they're added.
/// </summary>
internal sealed class ModerationEventHandlers : IHostedService, IAsyncEventListener<PlayerPending>
{
    private static readonly InstanceGetter<SteamPending, EClientPlatform>? GetPlatform = Accessor.GenerateInstanceGetter<SteamPending, EClientPlatform>("clientPlatform", throwOnError: false);

    private readonly DatabaseInterface _moderationSql;
    private readonly IPlayerService _playerService;
    private readonly IUserDataService _userDataService;
    private readonly ILogger<ModerationEventHandlers> _logger;
    private readonly ChatService _chatService;
    private readonly ModerationTranslations _translations;
    private readonly IPointsStore _pointsStore;

    public ModerationEventHandlers(
        DatabaseInterface moderationSql,
        IPlayerService playerService,
        IUserDataService userDataService,
        TranslationInjection<ModerationTranslations> translations,
        ILogger<ModerationEventHandlers> logger,
        ChatService chatService,
        IPointsStore pointsStore)
    {
        _moderationSql = moderationSql;
        _playerService = playerService;
        _userDataService = userDataService;
        _logger = logger;
        _chatService = chatService;
        _pointsStore = pointsStore;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        _moderationSql.OnModerationEntryUpdated += OnModerationEntryUpdated;
        _moderationSql.OnNewModerationEntryAdded += OnModerationEntryCreated;
        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        _moderationSql.OnModerationEntryUpdated -= OnModerationEntryUpdated;
        _moderationSql.OnNewModerationEntryAdded -= OnModerationEntryCreated;
        return UniTask.CompletedTask;
    }

    private void OnModerationEntryUpdated(ModerationEntry entry)
    {
        OnModerationEntryUpdatedOrCreated(entry, false);
    }

    private void OnModerationEntryCreated(ModerationEntry entry)
    {
        OnModerationEntryUpdatedOrCreated(entry, true);
    }

    private void OnModerationEntryUpdatedOrCreated(ModerationEntry entry, bool isNew)
    {
        if (entry.IsLegacy)
            return;

        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(entry.Player);

        // broadcasted entries
        if (entry is Ban or Mute or Kick or Warning)
        {
            UniTask.Create(async () =>
            {
                try
                {
                    await BroadcastNewEntry(isNew, entry, player);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting moderation entry {0}.", entry.GetType());
                }
            });
        }

        if (player == null)
            return;

        if (isNew && entry is Kick kick)
        {
            string message = kick.Message ?? "<no message>";
            string rejectMessage = _translations.RejectKicked.Translate(message);

            Provider.kick(new CSteamID(entry.Player), rejectMessage);
        }

        bool needsToCheckPending = true;
        switch (entry)
        {
            case Ban ban when ban.ResolvedTimestamp.HasValue && ban.IsApplied(true):
                string message = ban.Message ?? "<no message>";
                string rejectMessage = ban.IsPermanent
                    ? _translations.RejectPermanentBanned.Translate(message)
                    : _translations.RejectBanned.Translate(message, ban.GetTimeUntilExpiry(false));

                Provider.kick(new CSteamID(entry.Player), rejectMessage);
                break;

            case Warning { HasBeenDisplayed: false } warning:
                // no need to save twice
                needsToCheckPending = false;
                UniTask.Create(async () =>
                {
                    CancellationToken token = player.DisconnectToken;
                    IPlayer? adminPlayer = null;
                    if (warning.TryGetPrimaryAdmin(out RelatedActor actor))
                    {
                        ulong id = await _moderationSql.GetActorSteam64ID(actor.Actor, token).ConfigureAwait(false);
                        adminPlayer = _playerService.GetOnlinePlayerOrNullThreadSafe(id);
                        if (adminPlayer == null)
                        {
                            OfflinePlayer offlinePlayer = new OfflinePlayer(Unsafe.As<ulong, CSteamID>(ref id));
                            await offlinePlayer.CacheUsernames(_userDataService, token).ConfigureAwait(false);
                            adminPlayer = offlinePlayer;
                        }
                    }

                    await UniTask.SwitchToMainThread(token);

                    if (player.IsOnline)
                    {
                        string message = warning.Message ?? "<no message>";

                        string title = _translations.WarnPopupTitle.Translate(player);

                        string desc = adminPlayer == null
                            ? _translations.WarnPopupDescriptionNoActor.Translate(message, player)
                            : _translations.WarnPopupDescription.Translate(adminPlayer, message, player);

                        player.SendToast(ToastMessage.Popup(title, desc, PopupUI.Okay));
                        warning.DisplayedTimestamp = DateTimeOffset.UtcNow;
                    }

                    double oldPendingRep = warning.PendingReputation;
                    try
                    {
                        // add pending reputation
                        CancellationToken token2 = token;
                        if (Math.Abs(oldPendingRep) > 0.05)
                        {
                            await _pointsStore.AddToReputationAsync(new CSteamID(warning.Player), oldPendingRep, token);
                            token2 = CancellationToken.None;
                        }

                        warning.PendingReputation = 0;

                        await _moderationSql.AddOrUpdate(entry, token2).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        warning.DisplayedTimestamp = null;
                        warning.PendingReputation = oldPendingRep;
                        throw;
                    }
                });
                break;

            case Mute:
                PlayerModerationCacheComponent? modComp = player.ComponentOrNull<PlayerModerationCacheComponent>();

                if (modComp == null)
                    break;

                Task.Run(async () =>
                {
                    try
                    {
                        await modComp.RefreshActiveMute();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error refreshing player mute.");
                    }
                });

                break;
        }

        if (!needsToCheckPending || entry.PendingReputation == 0)
            return;

        UniTask.Create(async () =>
        {
            try
            {
                // add pending reputation
                if (Math.Abs(entry.Reputation) > 0.05)
                {
                    await _pointsStore.AddToReputationAsync(new CSteamID(entry.Player), entry.Reputation);
                }

                entry.PendingReputation = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding pending reputation from entry {0}.", entry.Id);
            }

            try
            {

                await _moderationSql.AddOrUpdate(entry, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying pending reputation from entry {0}.", entry.Id);
            }
        });
    }

    /// <inheritdoc />
    async UniTask IAsyncEventListener<PlayerPending>.HandleEventAsync(PlayerPending e, IServiceProvider serviceProvider, CancellationToken token)
    {
        DateTime now = DateTime.UtcNow;
        Task<PlayerIPAddress[]> getAddressesTask = _moderationSql.GetIPAddresses(e.Steam64, false, token);

        PlayerHWID[] hwids = await _moderationSql.GetHWIDs(e.Steam64, token);
        PlayerIPAddress[] addresses = await getAddressesTask;

        if (!e.PendingPlayer.transportConnection.TryGetIPv4Address(out uint ip))
        {
            if (!Provider.configData.Server.Use_FakeIP)
                e.Reject("Unable to get IPv4 address. This may be caused by connecting in an unusual way like a connection code.");
        }

        StringBuilder? queryBuilder = new StringBuilder();

        List<object> args = new List<object>(12) { e.Steam64.m_SteamID, now };

        if (ip != 0)
        {
            int currentIndex = Array.FindIndex(addresses, x => x.PackedIP == ip);

            if (currentIndex == -1)
            {
                queryBuilder.Append($"INSERT INTO `{TableIPAddresses}` (`{ColumnIPAddressesSteam64}`," +
                                    $"`{ColumnIPAddressesPackedIP}`,`{ColumnIPAddressesUnpackedIP}`," +
                                    $"`{ColumnIPAddressesFirstLogin}`,`{ColumnIPAddressesLastLogin}`," +
                                    $"`{ColumnIPAddressesLoginCount}`) VALUES (@0, @2, @3, @1, @1, 1);");

                args.Add(ip);
                args.Add(Parser.getIPFromUInt32(ip));

                if (!_moderationSql.IsRemotePlay(ip) && !await _moderationSql.IsIPFiltered(ip, e.Steam64, token))
                {
                    CollectionUtility.AddToArray(ref addresses, new PlayerIPAddress(0, e.Steam64.m_SteamID, ip, 1, now, now));
                }
            }
            else
            {
                queryBuilder.Append($"UPDATE `{TableIPAddresses}` SET `{ColumnIPAddressesLoginCount}` = `{ColumnIPAddressesLoginCount}` + 1," +
                                    $"`{ColumnIPAddressesLastLogin}` = @1 WHERE `{ColumnIPAddressesPrimaryKey}` = @2;");

                args.Add(addresses[currentIndex].Id);
            }
        }

        byte[][] currentHwids = e.PendingPlayer.playerID.GetHwids().ToArrayFast();

        // Windows users should have 3 HWIDs, Max and Linux should have 2.
        if (currentHwids.Length != 3)
        {
            if (currentHwids.Length != 2 || GetPlatform != null && GetPlatform(e.PendingPlayer) == EClientPlatform.Windows)
            {
                e.Reject("Su" + "spe" + "cte" + "d H" + "WI" + "D " + "sp" + "o" + "of" + "er.");
                return;
            }
        }

        Span<int> hwidIndices = stackalloc int[currentHwids.Length];
        int newCt = 0;
        for (int i = 0; i < currentHwids.Length; ++i)
        {
            int index = Array.FindIndex(hwids, x => x.HWID.Equals(currentHwids[i]));
            hwidIndices[i] = index;
            if (index < 0)
                ++newCt;
        }

        // some new
        if (newCt > 0)
        {
            queryBuilder.Append($"INSERT INTO `{TableHWIDs}` (`{ColumnHWIDsSteam64}`," +
                                $"`{ColumnHWIDsIndex}`,`{ColumnHWIDsHWID}`," +
                                $"`{ColumnHWIDsFirstLogin}`,`{ColumnHWIDsLastLogin}`," +
                                $"`{ColumnHWIDsLoginCount}`) VALUES ");

            PlayerHWID[] oldHwids = hwids;
            int newHwidIndex = oldHwids.Length;
            hwids = new PlayerHWID[newHwidIndex + newCt];
            Array.Copy(oldHwids, hwids, newHwidIndex);

            int argIndex = args.Count;
            for (int i = 0; i < newCt; ++i)
            {
                if (i != 0)
                    queryBuilder.Append(',');
                queryBuilder.Append("(@0, @").Append(argIndex).Append(", @").Append(argIndex + 1).Append(", @1, @1, 1)");
                argIndex += 2;
            }

            for (int i = 0; i < currentHwids.Length; ++i)
            {
                if (hwidIndices[i] >= 0)
                    continue;

                args.Add(i);
                args.Add(currentHwids[i]);

                // add new HWIDs to 'hwids'
                hwids[newHwidIndex] = new PlayerHWID(0, i, e.Steam64.m_SteamID, new HWID(currentHwids[i]), 1, now, now);
                ++newHwidIndex;
            }

            queryBuilder.Append(';');
        }

        // some existing
        int oldCt = currentHwids.Length - newCt;
        if (oldCt > 0)
        {
            queryBuilder.Append($"UPDATE `{TableHWIDs}` SET `{ColumnHWIDsLoginCount}` = `{ColumnHWIDsLoginCount}` + 1," +
                                $"`{ColumnHWIDsLastLogin}` = @1 WHERE `{ColumnHWIDsPrimaryKey}` ");

            int argIndex = args.Count;
            if (oldCt == 1)
            {
                queryBuilder.Append("= @").Append(argIndex);
            }
            else
            {
                queryBuilder.Append("IN (");
                for (int i = 0; i < oldCt; ++i)
                {
                    if (i != 0)
                        queryBuilder.Append(',');
                    queryBuilder.Append('@').Append(i + argIndex);
                }

                queryBuilder.Append(')');
            }

            for (int i = 0; i < currentHwids.Length; ++i)
            {
                int index = hwidIndices[i];
                if (index < 0)
                    continue;

                args.Add(hwids[index].Id);
            }

            queryBuilder.Append(';');
        }

        await _moderationSql.Sql.NonQueryAsync(queryBuilder.ToString(), args, token).ConfigureAwait(false);

        // ReSharper disable once RedundantAssignment (let it get GC'd)
        queryBuilder = null;

        Ban[] bans = await _moderationSql.GetActiveEntries<Ban>(e.Steam64, addresses, hwids, token: token);

        int banIndex = 0;

        bool? isIpWhitelisted = null;
        for (; banIndex < bans.Length; ++banIndex)
        {
            Ban ban = bans[banIndex];

            if (!ban.ResolvedTimestamp.HasValue || !ban.WasAppliedAt(DateTimeOffset.UtcNow.AddSeconds(2d), true))
                continue;

            if (ban.Player == e.Steam64.m_SteamID)
                break;

            if (isIpWhitelisted.HasValue)
            {
                if (isIpWhitelisted.Value)
                    continue;

                break;
            }

            isIpWhitelisted = ip != 0 && await _moderationSql.IsIPFiltered(ip, e.Steam64, token).ConfigureAwait(false);

            if (!isIpWhitelisted.Value)
                break;

            _logger.LogDebug("IP whitelisted: {0} - {1}.", e.Steam64, new IPv4Range(ip).ToIPv4String());
        }

        if (bans.Length == banIndex)
            return;

        Ban worstBan = bans[0];

        string message = worstBan.Message ?? "<no message>";

        if (worstBan.Player == e.Steam64.m_SteamID)
        {
            e.Reject(worstBan.IsPermanent
                ? _translations.RejectPermanentBanned.Translate(message, e.LanguageInfo, e.CultureInfo)
                : _translations.RejectBanned.Translate(message, worstBan.GetTimeUntilExpiry(false), e.LanguageInfo, e.CultureInfo)
            );
        }
        else
        {
            IPlayer player = await _playerService.GetOfflinePlayer(worstBan.Player, _userDataService, token);

            e.Reject(worstBan.IsPermanent
                ? _translations.RejectPermanentLinkedBanned.Translate(message, player, player, e.LanguageInfo, e.CultureInfo)
                : _translations.RejectLinkedBanned.Translate(message, worstBan.GetTimeUntilExpiry(false), player, player, e.LanguageInfo, e.CultureInfo)
            );
        }
    }

    private async UniTask BroadcastNewEntry(bool isNew, ModerationEntry entry, IPlayer? player = null, CancellationToken token = default)
    {
        if (player == null || player.Steam64.m_SteamID != entry.Player)
        {
            player = _playerService.GetOnlinePlayerOrNullThreadSafe(entry.Player);
            if (player == null)
            {
                OfflinePlayer offlinePlayer = new OfflinePlayer(new CSteamID(entry.Player));
                await offlinePlayer.CacheUsernames(_userDataService, token).ConfigureAwait(false);
                player = offlinePlayer;
            }
        }

        IModerationActor? adminActor = null;
        IPlayer? adminPlayer = null;
        bool isForgiven = false;

        // was recently forgiven
        if (!isNew
            && entry is IForgiveableModerationEntry { ForgivenBy: not null } forgiveable
            && !forgiveable.IsApplied(true)
            && forgiveable.ForgiveTimestamp.HasValue
            && (DateTimeOffset.UtcNow - forgiveable.ForgiveTimestamp.Value).TotalSeconds < 60)
        {
            adminActor = forgiveable.ForgivenBy;
            isForgiven = true;
        }
        else if (entry.TryGetPrimaryAdmin(out RelatedActor actor))
        {
            adminActor = actor.Actor;
        }

        if (!isForgiven && entry is IForgiveableModerationEntry forgiveable2 && !forgiveable2.IsApplied(true))
        {
            return;
        }

        if (adminActor != null)
        {
            ulong id = await _moderationSql.GetActorSteam64ID(adminActor, token).ConfigureAwait(false);
            adminPlayer = _playerService.GetOnlinePlayerOrNullThreadSafe(id);
            if (adminPlayer == null && new CSteamID(id).GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
            {
                OfflinePlayer offlinePlayer = new OfflinePlayer(Unsafe.As<ulong, CSteamID>(ref id));
                await offlinePlayer.CacheUsernames(_userDataService, token).ConfigureAwait(false);
                adminPlayer = offlinePlayer;
            }
        }

        LanguageSetEnumerator set = _translations.TranslationService.SetOf.AllPlayersExcept(entry.Player);

        switch (entry)
        {
            case Ban ban when !isForgiven:
                if (adminPlayer != null)
                {
                    if (ban.IsPermanent)
                        _chatService.Broadcast(set, _translations.BanPermanentSuccessBroadcast, player, adminPlayer);
                    else
                        _chatService.Broadcast(set, _translations.BanSuccessBroadcast, player, adminPlayer, TimeSpan.FromSeconds((int)Math.Round(ban.Duration.TotalSeconds, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    if (ban.IsPermanent)
                        _chatService.Broadcast(set, _translations.BanPermanentSuccessBroadcastNoActor, player);
                    else
                        _chatService.Broadcast(set, _translations.BanSuccessBroadcastNoActor, player, TimeSpan.FromSeconds((int)Math.Round(ban.Duration.TotalSeconds, MidpointRounding.AwayFromZero)));
                }
                break;

            case Ban: // when isForgiven
                if (adminPlayer != null)
                    _chatService.Broadcast(set, _translations.UnbanSuccessBroadcast, player, adminPlayer);
                else
                    _chatService.Broadcast(set, _translations.UnbanSuccessBroadcastNoActor, player);
                break;

            case Mute mute when !isForgiven:
                if (adminPlayer != null)
                {
                    if (mute.IsPermanent)
                        _chatService.Broadcast(set, _translations.MutePermanentSuccessBroadcast, player, adminPlayer, mute.Type);
                    else
                        _chatService.Broadcast(set, _translations.MuteSuccessBroadcast, player, adminPlayer, TimeSpan.FromSeconds((int)Math.Round(mute.Duration.TotalSeconds, MidpointRounding.AwayFromZero)), mute.Type);
                }
                else
                {
                    if (mute.IsPermanent)
                        _chatService.Broadcast(set, _translations.MutePermanentSuccessBroadcastNoActor, player, mute.Type);
                    else
                        _chatService.Broadcast(set, _translations.MuteSuccessBroadcastNoActor, player, TimeSpan.FromSeconds((int)Math.Round(mute.Duration.TotalSeconds, MidpointRounding.AwayFromZero)), mute.Type);
                }
                break;

            case Mute: // when isForgiven
                if (adminPlayer != null)
                    _chatService.Broadcast(set, _translations.UnmuteSuccessBroadcast, player, adminPlayer);
                else
                    _chatService.Broadcast(set, _translations.UnmuteSuccessBroadcastNoActor, player);
                break;

            case Kick:
                if (adminPlayer != null)
                    _chatService.Broadcast(set, _translations.KickSuccessBroadcast, player, adminPlayer);
                else
                    _chatService.Broadcast(set, _translations.KickSuccessBroadcastNoActor, player);

                break;

            case Warning:
                if (adminPlayer != null)
                    _chatService.Broadcast(set, _translations.WarnSuccessBroadcast, player, adminPlayer);
                else
                    _chatService.Broadcast(set, _translations.WarnSuccessBroadcastNoActor, player);
                break;
        }
    }
}
