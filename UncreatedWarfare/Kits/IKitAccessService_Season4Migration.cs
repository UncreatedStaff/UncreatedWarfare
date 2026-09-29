using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;

// temp code that migrates season <= 4 kit access (per-kit) to the new per-level kit access system

partial class MySqlKitAccessService
{
    internal async Task MigrateSeason4KitAccessAsync(bool delete, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        IKitDataStore dataStore = _kitDataStore ?? serviceProvider.GetRequiredService<IKitDataStore>();

        PublicKitLevelConfiguration config = serviceProvider.GetRequiredService<PublicKitLevelConfiguration>();

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        IKitsDbContext kitsDbContext = scope.ServiceProvider.GetRequiredService<IKitsDbContext>();
        kitsDbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        Dictionary<PublicKitLevel, List<(ulong, DateTimeOffset)>> ownership = new Dictionary<PublicKitLevel, List<(ulong, DateTimeOffset)>>();

        StringBuilder? deleteCommand = delete ? new StringBuilder(250000) : null;
        deleteCommand?.Append("DELETE FROM `kits_access` WHERE (Steam64, Kit) IN (");
        bool deleteComma = false;

        Kit[] allPublicKits = await dataStore.QueryKitsAsync(KitInclude.None, k => k.Type == KitType.Public && !k.RequiresNitro, token: token);

        _logger.LogInformation($"Found {allPublicKits.Length} candidate public kits.");

        foreach (Kit kit in allPublicKits)
        {
            if (!kit.TryGetLevel(out PublicKitLevel lvl))
            {
                // kit isn't a public kit
                continue;
            }

            if (!config.CreditCostBylevel.TryGetValue(lvl, out double cost) || cost <= 0)
            {
                // kit is free
                continue;
            }

            uint kitId = kit.Key;
            
            var allPlayers = await kitsDbContext.KitAccess
                .Where(x => x.KitId == kitId && (x.AccessType == KitAccessType.Unknown || x.AccessType == KitAccessType.Credits))
                .Select(x => new { x.Steam64, x.Timestamp })
                .ToListAsync(token);

            _logger.LogInformation($"Found {allPlayers.Count} player(s) with ownership of {kit.Id}.");

            if (!ownership.TryGetValue(lvl, out List<(ulong, DateTimeOffset)> allPlayersAlreadyOwned))
            {
                allPlayersAlreadyOwned = new List<(ulong, DateTimeOffset)>(16384);
                ownership[lvl] = allPlayersAlreadyOwned;
            }

            string kitIdString = kit.Key.ToString(CultureInfo.InvariantCulture);

            foreach (var player in allPlayers)
            {
                bool found = false;
                for (int i = 0; i < allPlayersAlreadyOwned.Count; ++i)
                {
                    (ulong, DateTimeOffset) tuple = allPlayersAlreadyOwned[i];
                    if (tuple.Item1 != player.Steam64)
                        continue;

                    if (player.Timestamp < tuple.Item2)
                    {
                        tuple.Item2 = player.Timestamp;
                        allPlayersAlreadyOwned[i] = tuple;
                    }

                    found = true;
                    break;
                }

                if (!found)
                {
                    allPlayersAlreadyOwned.Add((player.Steam64, player.Timestamp));
                }

                if (deleteCommand != null)
                {
                    if (deleteComma)
                        deleteCommand.Append(',');
                    else
                        deleteComma = true;

                    deleteCommand.Append('(')
                        .Append(player.Steam64).Append(',')
                        .Append(kitIdString)
                        .Append(')');
                }
            }
        }

        deleteCommand?.Append(");");

        string seasonString = WarfareModule.Season.ToString(CultureInfo.InvariantCulture);

        int ttlRows = 0;

        StringBuilder sb = new StringBuilder(250000);
        foreach (KeyValuePair<PublicKitLevel, List<(ulong, DateTimeOffset)>> set in ownership)
        {
            PublicKitLevel lvl = set.Key;

            List<(ulong, DateTimeOffset)> players = set.Value;
            if (players.Count == 0)
            {
                _logger.LogInformation($"{lvl}: 0 added.");
                continue;
            }

            string lvlString = lvl.Level.ToString(CultureInfo.InvariantCulture);
            string classString = EnumUtility.GetNameSafe(lvl.Class);

            sb.Clear();
            sb.Append("INSERT INTO `kits_level_access` (Steam64, Class, Level, Season, GivenAt) VALUES ");
            
            bool comma = false;
            foreach ((ulong steam64, DateTimeOffset timestamp) in players)
            {
                if (comma)
                    sb.Append(',');
                else
                    comma = true;

                sb.Append('(')
                    .Append(steam64).Append(',')
                    .Append('"').Append(classString).Append('"').Append(',')
                    .Append(lvlString).Append(',')
                    .Append(seasonString).Append(',')
                    .Append('\'').Append(timestamp.ToString("s", CultureInfo.InvariantCulture)).Append('\'')
                    .Append(')');
            }

            sb.Append(';');

            int rows = await kitsDbContext.Database.ExecuteSqlRawAsync(sb.ToString(), token);
            _logger.LogInformation($"{lvl}: {rows} added.");
            token = CancellationToken.None;
            ttlRows += rows;
        }

        _logger.LogInformation($"Done adding rows. {ttlRows} rows added total.");
        if (deleteCommand == null)
        {
            return;
        }

        int rowsDeleted = await kitsDbContext.Database.ExecuteSqlRawAsync(deleteCommand.ToString(), token);

        _logger.LogInformation($"Done deleting rows. {rowsDeleted} row(s) deleted.");
    }
}