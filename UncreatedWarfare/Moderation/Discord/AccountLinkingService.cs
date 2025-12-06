using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Moderation.Discord;

[GenerateRpcSource]
public partial class AccountLinkingService
{
    private static System.Random? _randomGenerator;
    private readonly IUserDataService _userDataService;
    private readonly IUserDataDbContext _dbContext;
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<AccountLinkingService> _logger;

    /// <summary>
    /// Invoked when either a discord or steam link is updated. Will be invoked twice during an unlink (once for the discord accont and once for the steam account).
    /// <para>
    /// If DiscordID == 0 then a steam account was unlinked.
    /// If Steam64ID == 0 then a discord account was unlinked.
    /// Else an account was linked.
    /// </para>
    /// </summary>
    public event Action<CSteamID, ulong>? OnLinkUpdated;

    /// <summary>
    /// Invoked when a linked user joins or leaves the guild. Only invoked when the bot is connected.
    /// </summary>
    public event Action<CSteamID, ulong, GuildStatusResult>? OnGuildStatusUpdated;

    public AccountLinkingService(IUserDataService userDataService, IUserDataDbContext dbContext, ILogger<AccountLinkingService> logger)
    {
        _userDataService = userDataService;
        _dbContext = dbContext;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _logger = logger;

        _semaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Force-link a Discord ID to a Steam64 ID without any kind of verification.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid Discord or Steam64 ID.</exception>
    public async Task LinkAccountsAsync(CSteamID steamId, ulong discordId, CancellationToken token = default)
    {
        if (steamId.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            throw new ArgumentException("Invalid Steam64 ID.", nameof(steamId));

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = steamId.m_SteamID;

            // remove pending links for either accounts, also expired links while we're at it
            DateTime now = DateTime.UtcNow.AddSeconds(30d);
            await _dbContext.PendingLinks
                .DeleteRangeAsync((DbContext)_dbContext, x => x.Steam64 == s64 || discordId != 0 && x.DiscordId == discordId || x.ExpiryTimestamp < now, cancellationToken: token)
                .ConfigureAwait(false);

            await LinkAccountsIntl(s64, discordId, token).ConfigureAwait(false);

            _dbContext.ChangeTracker.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Starts a discord account link for a player from their Steam account. Ex. /link ingame then /link &lt;code&gt; in discord.
    /// </summary>
    /// <returns>The new or already pending link.</returns>
    public async Task<SteamDiscordPendingLink> BeginLinkFromSteamAsync(CSteamID steamId, TimeSpan expiry = default, CancellationToken token = default)
    {
        if (expiry.Ticks <= 0)
            expiry = TimeSpan.FromHours(1);

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await BeginLinkIntl(steamId, 0ul, expiry, token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Starts a discord account link for a player from their Steam account. Ex. /link in discord then /link &lt;code&gt; ingame.
    /// </summary>
    /// <returns>The new or already pending link.</returns>
    public async Task<SteamDiscordPendingLink> BeginLinkFromDiscordAsync(ulong discordId, TimeSpan expiry = default, CancellationToken token = default)
    {
        if (expiry.Ticks <= 0)
            expiry = TimeSpan.FromHours(1);

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await BeginLinkIntl(CSteamID.Nil, discordId, expiry, token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Ends a discord account link for a player that started their link in-game and ended it in Discord.
    /// </summary>
    /// <returns><see langword="true"/> if the link was successful, otherwise <see langword="false"/>, usually because the <paramref name="matchingToken"/> isn't recognized.</returns>
    public async Task<bool> ResolveLinkFromDiscordAsync(string matchingToken, ulong discordId, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await ResolveLinkIntl(matchingToken, CSteamID.Nil, discordId, token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Ends a discord account link for a player that started their link in-game and ended it in Discord.
    /// </summary>
    /// <returns><see langword="true"/> if the link was successful, otherwise <see langword="false"/>, usually because the <paramref name="matchingToken"/> isn't recognized.</returns>
    public async Task<bool> ResolveLinkFromSteamAsync(string matchingToken, CSteamID steamId, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await ResolveLinkIntl(matchingToken, steamId, 0, token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<SteamDiscordPendingLink> BeginLinkIntl(CSteamID user, ulong discordId, TimeSpan expiry, CancellationToken token = default)
    {
        DateTimeOffset startingTimestamp = DateTimeOffset.UtcNow;
        DateTimeOffset expireTimestamp = startingTimestamp + expiry;

        await RemoveExpiredAsync(token).ConfigureAwait(false);

        ulong s64 = user.m_SteamID;
        SteamDiscordPendingLink? existing = await (s64 != 0
            ? _dbContext.PendingLinks.FirstOrDefaultAsync(x => x.ExpiryTimestamp >= startingTimestamp && x.Steam64 == s64, token)
            : _dbContext.PendingLinks.FirstOrDefaultAsync(x => x.ExpiryTimestamp >= startingTimestamp && x.DiscordId == discordId, token));

        if (existing != null)
        {
            _dbContext.ChangeTracker.Clear();

            return existing;
        }

        // keep regenerating the token until not taken (unlikely to ever happen but not impossible)
        SteamDiscordPendingLink newLink;
        while (true)
        {
            newLink = new SteamDiscordPendingLink
            {
                Steam64 = s64 == 0 ? null : s64,
                DiscordId = discordId == 0 ? null : discordId,
                ExpiryTimestamp = expireTimestamp,
                StartedTimestamp = startingTimestamp,
                Token = GenerateRandomToken()
            };

            // duplicate token
            try
            {
                _dbContext.PendingLinks.Add(newLink);
                await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
            {
                continue;
            }
            finally
            {
                _dbContext.ChangeTracker.Clear();
            }

            break;
        }

        return newLink;
    }

    /// <summary>
    /// Generate a random token for validating links.
    /// </summary>
    public static string GenerateRandomToken()
    {
        return string.Create<object?>(9, null, (span, _) =>
        {
            System.Random r = _randomGenerator ??= new System.Random();
            for (int i = 0; i < 8; ++i)
            {
                // random capital or lowercase letter
                int letter = r.Next(0, 52);
                span[i >= 4 ? i + 1 : i] = (char)(letter + (letter >= 26 ? 71 : 65));
            }

            span[4] = '-';
        });
    }

    private async Task<bool> ResolveLinkIntl(string matchingToken, CSteamID steamId, ulong discordId, CancellationToken token = default)
    {
        string? validToken = NormalizeToken(matchingToken);
        if (validToken == null)
            return false;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        ulong s64 = steamId.m_SteamID;

        await RemoveExpiredAsync(token).ConfigureAwait(false);

        SteamDiscordPendingLink? existing = await (s64 != 0
            ? _dbContext.PendingLinks.FirstOrDefaultAsync(x => x.ExpiryTimestamp >= now && x.DiscordId.HasValue && x.Token == validToken, token)
            : _dbContext.PendingLinks.FirstOrDefaultAsync(x => x.ExpiryTimestamp >= now && x.Steam64.HasValue && x.Token == validToken, token));

        if (existing == null)
            return false;

        ulong steam64 = s64 != 0 ? s64 : existing.Steam64.GetValueOrDefault();
        if (discordId == 0)
        {
            discordId = existing.DiscordId.GetValueOrDefault();
            if (discordId == 0)
                return false;
        }

        if (Unsafe.As<ulong, CSteamID>(ref steam64).GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            return false;

        await LinkAccountsIntl(steam64, discordId, token).ConfigureAwait(false);

        _dbContext.Remove(existing);
        await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        _dbContext.ChangeTracker.Clear();
        return true;
    }

    private async Task LinkAccountsIntl(ulong steam64, ulong discordId, CancellationToken token)
    {
        ulong oldDiscordId = 0;

        // remove other Discord IDs linked to a Steam64 ID and update the ID to the new one
        await _userDataService
            .AddOrUpdateAsync(steam64, (userData, _) =>
            {
                oldDiscordId = userData.DiscordId;
                userData.DiscordId = discordId;
            }, CancellationToken.None)
            .ConfigureAwait(false);

        // remove other Steam IDs linked to a discord ID
        List<WarfareUserData>? toClear = discordId == 0 ? null : await _dbContext.UserData
            .Where(x => x.DiscordId == discordId && x.Steam64 != steam64)
            .AsNoTracking()
            .ToListAsync(token);

        int numUpdated = discordId == 0 ? 0 : await _dbContext.UserData.BatchUpdate((DbContext)_dbContext)
            .Set(x => x.DiscordId, _ => 0ul)
            .Where(x => x.DiscordId == discordId && x.Steam64 != steam64)
            .ExecuteAsync(cancellationToken: token)
            .ConfigureAwait(false);

        if (oldDiscordId != 0)
        {
            InvokeLinkUpdated(steam64, oldDiscordId, isUnlink: true);
        }

        if (numUpdated > 0 && toClear != null)
        {
            foreach (WarfareUserData data in toClear)
            {
                InvokeLinkUpdated(data.Steam64, 0ul, isUnlink: false);
            }
        }

        InvokeLinkUpdated(steam64, discordId, isUnlink: false);

        if (oldDiscordId != 0)
        {
            _logger.LogInformation("Relinked Discord account {0} with Steam account {1} after unlinking {2} other Steam account(s). Old discord ID was {3}.", discordId, steam64, numUpdated, oldDiscordId);
        }
        else if (discordId == 0)
        {
            _logger.LogInformation("Unlinked Steam account {0}.", steam64);
        }
        else
        {
            _logger.LogInformation("Linked Discord account {0} with Steam account {1} after unlinking {2} other Steam account(s).", discordId, steam64, numUpdated);
        }
    }

    private async Task RemoveExpiredAsync(CancellationToken token)
    {
        DateTime now = DateTime.UtcNow.AddSeconds(30d);
        int removed = await _dbContext.PendingLinks
            .DeleteRangeAsync((DbContext)_dbContext, x => x.ExpiryTimestamp < now, cancellationToken: token)
            .ConfigureAwait(false);

        if (removed != 0)
            _logger.LogDebug("Removed {0} expired pending link(s).", removed);
    }

    /// <summary>
    /// Check if a player is currently in the Discord server by their Steam64 ID.
    /// </summary>
    public async Task<GuildStatusResult> IsInGuild(CSteamID steam64, CancellationToken token = default)
    {
        if (steam64.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            return GuildStatusResult.NotLinked;

        ulong discordId = await _userDataService.GetDiscordIdAsync(steam64.m_SteamID, token).ConfigureAwait(false);
        return await IsInGuild(discordId).ConfigureAwait(false);
    }

    /// <summary>
    /// Check if a player is currently in the Discord server by their Discord user ID.
    /// </summary>
    public async Task<GuildStatusResult> IsInGuild(ulong discordId)
    {
        if (discordId == 0)
            return GuildStatusResult.NotLinked;

        try
        {
            GuildStatusResult result = await SendIsInGuild(discordId);
            return result;
        }
        catch
        {
            return GuildStatusResult.Unknown;
        }
    }

    /// <summary>
    /// Invoked by the bot to tell the server to invoke <see cref="OnGuildStatusUpdated"/>.
    /// </summary>
    [RpcSend(nameof(ReceiveGuildUpdate)), RpcFireAndForget]
    protected partial void SendGuildUpdate(ulong discordId, ulong steam64Id, GuildStatusResult result);

    [RpcReceive]
    private void ReceiveGuildUpdate(ulong discordId, ulong steam64Id, GuildStatusResult result)
    {
        try
        {
            OnGuildStatusUpdated?.Invoke(new CSteamID(steam64Id), discordId, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error on OnGuildStatusUpdated.");
        }
    }

    /// <summary>
    /// Check if a player is currently in the Discord server by their Discord user ID.
    /// </summary>
    [RpcSend]
    protected partial RpcTask<GuildStatusResult> SendIsInGuild(ulong discordId);

    private void InvokeLinkUpdated(ulong steam64, ulong discordId, bool isUnlink)
    {
        ReceiveLinkUpdated(steam64, discordId, isUnlink);
        try
        {
            SendLinkUpdated(steam64, discordId, isUnlink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking SendLinkUpdated.");
        }
    }

    /// <summary>
    /// Invoke the 'link updated' method on the remote side.
    /// </summary>
    [RpcSend(nameof(ReceiveLinkUpdated)), RpcFireAndForget]
    protected partial void SendLinkUpdated(ulong steam64, ulong discordId, bool isUnlink);

    [RpcReceive]
    private void ReceiveLinkUpdated(ulong steam64, ulong discordId, bool isUnlink)
    {
        try
        {
            if (isUnlink)
            {
                OnLinkUpdated?.Invoke(CSteamID.Nil, discordId);
                OnLinkUpdated?.Invoke(new CSteamID(steam64), 0ul);
            }
            else
            {
                OnLinkUpdated?.Invoke(new CSteamID(steam64), discordId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error on OnLinkUpdated.");
        }
    }


    private static readonly char[] Dashes = [ '-', '‐', '‑', '‒', '–', '—', '―', '⸺', '⸻', '﹘' ];
    private static bool IsLatinChar(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

    // tested (DiscordLinkTokenTests)

#pragma warning disable CS8500

    /// <summary>
    /// Normalize a string token from input to a consistant format.
    /// </summary>
    public static unsafe string? NormalizeToken(string token)
    {
        if (token.Length < 8)
            return null;

        if (token.Length == 9
            && token[4] == '-'
            && IsLatinChar(token[0])
            && IsLatinChar(token[1])
            && IsLatinChar(token[2])
            && IsLatinChar(token[3])
            && IsLatinChar(token[5])
            && IsLatinChar(token[6])
            && IsLatinChar(token[7])
            && IsLatinChar(token[8]))
        {
            // abcd-efgh
            return token;
        }

        ReadOnlySpan<char> trimmed = token.AsSpan().Trim();

        if (trimmed.Length == 9
            && IsLatinChar(token[0])
            && IsLatinChar(token[1])
            && IsLatinChar(token[2])
            && IsLatinChar(token[3])
            && IsLatinChar(token[5])
            && IsLatinChar(token[6])
            && IsLatinChar(token[7])
            && IsLatinChar(token[8])
            && Array.IndexOf(Dashes, token[4]) >= 0)
        {
            // abcd-efgh
            return string.Create(9, token, static (span, state) =>
            {
                ReadOnlySpan<char> trimmed = state.AsSpan().Trim();
                trimmed[..4].CopyTo(span);
                span[4] = '-';
                trimmed.Slice(5, 4).CopyTo(span[5..]);
            });
        }

        // abcdefgh
        if (trimmed.Length == 8)
        {
            for (int i = 0; i < 8; ++i)
            {
                if (IsLatinChar(trimmed[i]))
                    continue;
                return null;
            }

            return string.Create(9, token, static (span, state) =>
            {
                ReadOnlySpan<char> trimmed = state.AsSpan().Trim();
                trimmed[..4].CopyTo(span);
                span[4] = '-';
                trimmed.Slice(4, 4).CopyTo(span[5..]);
            });
        }

        // ab-cdefgh, abcdef-gh
        int dashIndex = trimmed.IndexOfAny(Dashes);
        if (dashIndex > 0 && dashIndex + 1 < trimmed.Length)
        {
            ReadOnlySpan<char> part1 = trimmed.Slice(0, dashIndex).TrimEnd();
            ReadOnlySpan<char> part2 = trimmed.Slice(dashIndex + 1).TrimStart();

            if (part1.Length + part2.Length != 8)
                return null;

            NormalizedImproperSplitTokenState state = default;

            state.Part1 = &part1;
            state.Part2 = &part2;

            return string.Create(9, state, static (span, state) =>
            {
                if (state.Part1->Length == 4)
                {
                    state.Part1->CopyTo(span);
                    state.Part2->Slice(0, 4).CopyTo(span[5..]);
                }
                else if (state.Part1->Length < 4)
                {
                    state.Part1->CopyTo(span);
                    state.Part2->Slice(0, state.Part2->Length - 4).CopyTo(span.Slice(state.Part1->Length));
                    state.Part2->Slice(state.Part2->Length - 4, 4).CopyTo(span[5..]);
                }
                else
                {
                    state.Part1->Slice(0, 4).CopyTo(span);
                    state.Part1->Slice(4).CopyTo(span[5..]);
                    state.Part2->CopyTo(span.Slice(state.Part1->Length + 1));
                }
                span[4] = '-';
            });
        }

        if (dashIndex >= 0)
            return null;

        // ab cdefgh, abcd efgh, abcdef gh
        int firstWhiteSpace = -1;
        int endOfFirstWhiteSpace = -1;

        for (int i = 0; i < trimmed.Length; ++i)
        {
            if (!char.IsWhiteSpace(trimmed[i]))
            {
                if (firstWhiteSpace == -1)
                    continue;

                break;
            }

            if (firstWhiteSpace != -1)
                continue;

            firstWhiteSpace = i;
            endOfFirstWhiteSpace = i;
        }

        if (firstWhiteSpace > 0 && endOfFirstWhiteSpace + 1 < trimmed.Length)
        {
            ReadOnlySpan<char> part1 = trimmed.Slice(0, firstWhiteSpace).TrimEnd();
            ReadOnlySpan<char> part2 = trimmed.Slice(endOfFirstWhiteSpace + 1).TrimStart();

            if (part1.Length + part2.Length != 8)
                return null;

            NormalizedImproperSplitTokenState state = default;

            state.Part1 = &part1;
            state.Part2 = &part2;

            return string.Create(9, state, static (span, state) =>
            {
                if (state.Part1->Length == 4)
                {
                    state.Part1->CopyTo(span);
                    state.Part2->Slice(0, 4).CopyTo(span[5..]);
                }
                else if (state.Part1->Length < 4)
                {
                    state.Part1->CopyTo(span);
                    state.Part2->Slice(0, state.Part2->Length - 4).CopyTo(span.Slice(state.Part1->Length));
                    state.Part2->Slice(state.Part2->Length - 4, 4).CopyTo(span[5..]);
                }
                else
                {
                    state.Part1->Slice(0, 4).CopyTo(span);
                    state.Part1->Slice(4).CopyTo(span[5..]);
                    state.Part2->CopyTo(span.Slice(state.Part1->Length + 1));
                }
                span[4] = '-';
            });
        }

        return null;
    }

    private unsafe struct NormalizedImproperSplitTokenState
    {
        public ReadOnlySpan<char>* Part1;
        public ReadOnlySpan<char>* Part2;
    }
#pragma warning restore CS8500

}

public enum GuildStatusResult : byte
{
    /// <summary>
    /// The player is in the guild, for sure.
    /// </summary>
    InGuild,

    /// <summary>
    /// The player is not in the guild, for sure.
    /// </summary>
    NotInGuild,

    /// <summary>
    /// The player's account is not linked.
    /// </summary>
    NotLinked,

    /// <summary>
    /// Unable to determine whether the player is actually in the guild or not.
    /// </summary>
    Unknown
}