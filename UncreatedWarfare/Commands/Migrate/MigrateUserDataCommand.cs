#if DEBUG
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("userdata"), HideFromHelp, SubCommandOf(typeof(MigrateCommand))]
[Obsolete]
internal sealed class MigrateUserDataCommand : IExecutableCommand
{
    private readonly IManualMySqlProvider _mySqlProvider;
    private readonly WarfareDbContext _dbContext;

    // list of all preset display names before migration to user data
    private readonly Dictionary<ulong, string> _displayNames = new Dictionary<ulong, string>
    {
        { 76561198857595123, "420" },
        { 76561198267927009, "BlazingFlame" },
        { 76561198312948915, "Michael" },
        { 76561198129133477, "Elex Davina" },
        { 76561198267033135, "DMJaxun" },
        { 76561198046242218, "Kamil19" },
        { 76561198275957279, "Supermatt" },
        { 76561198130166011, "Duffles" },
        { 76561198383423254, "Sasha" },
        { 76561199033851730, "Vyress" },
        { 76561199022174396, "Bmxmb" },
        { 76561198088963430, "WolfiePenguin" },
        { 76561198257651053, "ecco" },
        { 76561198399621836, "Ozgi" },
        { 76561198282562527, "SilverHawk" },
        { 76561198416416152, "Pepenes" },
        { 76561199107830689, "Panzer" },
        { 76561198856241780, "AngelAbov3" },
        { 76561198974022341, "Wildfire" },
        { 76561198364938298, "gorge" },
        { 76561198182444534, "Kristian" },
        { 76561199124264730, "goose" },
        { 76561198205248925, "NerfBoy" },
        { 76561198426374316, "NoSnapchat" },
        { 76561198424853546, "pxy66" },
        { 76561198362471305, "Justi" },
        { 76561198201986200, "Karp (3312b)" },
        { 76561198429514139, "QQ" },
        { 76561198197480782, "Phrög" },
        { 76561198260111704, "Scromker" },
        { 76561198289265188, "AstroSlav" }
    };

    public required CommandContext Context { get; init; }

    public MigrateUserDataCommand(IManualMySqlProvider mySqlProvider, WarfareDbContext dbContext)
    {
        _mySqlProvider = mySqlProvider;
        _dbContext = dbContext;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByTerminal();

        return MigrateUsers(token);
    }

    private async UniTask MigrateUsers(CancellationToken token)
    {
        Context.AssertRanByTerminal();

        HashSet<ulong> knownPlayers = new HashSet<ulong>(8192);

        await foreach (PlayerIPAddress data in _dbContext.IPAddresses.AsAsyncEnumerable().WithCancellation(token))
            knownPlayers.Add(data.Steam64);

        await foreach (PlayerHWID data in _dbContext.HWIDs.AsAsyncEnumerable().WithCancellation(token))
            knownPlayers.Add(data.Steam64);

        await _mySqlProvider.QueryAsync(
            $"SELECT `{DatabaseInterface.ColumnUsernamesSteam64}` FROM `{DatabaseInterface.TableUsernames}` GROUP BY `{DatabaseInterface.ColumnUsernamesSteam64}`;",
            null, token,
            reader =>
            {
                knownPlayers.Add(reader.GetUInt64(0));
            });

        await foreach (KitAccess access in _dbContext.KitAccess.AsAsyncEnumerable().WithCancellation(token))
            knownPlayers.Add(access.Steam64);

        await foreach(WarfareUserData data in _dbContext.UserData.AsAsyncEnumerable().WithCancellation(token))
            knownPlayers.Remove(data.Steam64);

        int c = 0;
        foreach (ulong steam64 in knownPlayers)
        {
            ++c;
            if (c % 50 == 0 || c == knownPlayers.Count || c == 1)
            {
                Context.ReplyString($"Users migrated: {c} / {knownPlayers.Count} ({c / knownPlayers.Count:P2}.");
            }

            PlayerNames username = default;
            await _mySqlProvider.QueryAsync(
                $"SELECT `{DatabaseInterface.ColumnUsernamesCharacterName}`, `{DatabaseInterface.ColumnUsernamesNickName}`, `{DatabaseInterface.ColumnUsernamesPlayerName}` FROM " +
                $"`{DatabaseInterface.TableUsernames}` WHERE `{DatabaseInterface.ColumnUsernamesSteam64}` = {steam64}.",
                null, token,
                reader =>
                {
                    username.CharacterName = reader.IsDBNull(0) ? null! : reader.GetString(0);
                    username.NickName      = reader.IsDBNull(1) ? null! : reader.GetString(1);
                    username.PlayerName    = reader.IsDBNull(2) ? null! : reader.GetString(2);
                    username.WasFound = true;
                    return false;
                });

            username.CharacterName ??= steam64.ToString("D17", CultureInfo.InvariantCulture);
            username.NickName      ??= steam64.ToString("D17", CultureInfo.InvariantCulture);
            username.PlayerName    ??= steam64.ToString("D17", CultureInfo.InvariantCulture);

            DateTimeOffset? firstJoined = null;
            DateTimeOffset? lastJoined = null;
            await _mySqlProvider.QueryAsync($"SELECT `FirstLoggedIn`, `LastLoggedIn` FROM `logindata` WHERE `Steam64` = {steam64};", null, token,
                reader =>
                {
                    firstJoined = reader.IsDBNull(0) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Utc));
                    lastJoined = reader.IsDBNull(1) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc));
                    return false;
                });

            ulong discordId = 0;
            await _mySqlProvider.QueryAsync(
                $"SELECT `{DatabaseInterface.ColumnDiscordIdsDiscordId}` FROM " +
                $"`{DatabaseInterface.TableDiscordIds}` WHERE `{DatabaseInterface.ColumnDiscordIdsSteam64}` = {steam64};",
                null, token,
                reader =>
                {
                    discordId = reader.GetUInt64(0);
                    return false;
                });

            WarfareUserData data = new WarfareUserData
            {
                FirstJoined = firstJoined,
                LastJoined = lastJoined,
                Steam64 = steam64,
                CharacterName = username.CharacterName.Truncate(30),
                DisplayName = _displayNames.GetValueOrDefault(steam64),
                NickName = username.NickName.Truncate(30),
                PlayerName = username.PlayerName.Truncate(48),
                DiscordId = discordId
            };

            _dbContext.UserData.Add(data);
        }

        Context.ReplyString("Saving...");
        await _dbContext.SaveChangesAsync(CancellationToken.None);
        Context.ReplyString("Done.");
    }
}
#endif