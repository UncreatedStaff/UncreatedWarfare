#if DEBUG
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Stats;

namespace Uncreated.Warfare.Commands;

[Command("offenses"), HideFromHelp, SubCommandOf(typeof(MigrateCommand))]
public class MigrateLegacyPointsCommand : IExecutableCommand
{
    private readonly IManualMySqlProvider _mySqlProvider;
    private readonly IPointsStore _pointsSql;
    private readonly WarfareDbContext _dbContext;
    public CommandContext Context { get; set; }

    public MigrateLegacyPointsCommand(IManualMySqlProvider mySqlProvider, IPointsStore pointsSql, WarfareDbContext dbContext)
    {
        _mySqlProvider = mySqlProvider;
        _pointsSql = pointsSql;
        _dbContext = dbContext;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByTerminal();

        if (Context.TryGet(0, out int seasonId))
        {
            if (seasonId is < 1 or > 3)
                throw Context.ReplyString("Only seasons 1, 2, 3 can be migrated.");

            await MigrateSeason(seasonId, token);
            return;
        }
        
        for (int i = 1; i <= 3; ++i)
        {
            await MigrateSeason(i, token);
        }
    }

    private async Task MigrateSeason(int seasonId, CancellationToken token)
    {
        string tableName = seasonId == 1 ? "levels" : ("s" + seasonId.ToString(CultureInfo.InvariantCulture) + "_levels");
        string creditsName = seasonId == 1 ? "OfficerPoints" : "Credits";
        string xpName = seasonId == 1 ? "XP" : "Experience";

        // s0 (no longer have this data), s1 (usa vs russia), s2 (usa vs russia), s3 (usa vs mec)
        (string t1, string t2) = seasonId switch
        {
            3 => ("usa", "mec"),
            _ => ("usa", "russia")
        };

        Faction t1Faction = await _dbContext.Factions.FirstAsync(x => x.InternalName == t1, token).ConfigureAwait(false);
        Faction t2Faction = await _dbContext.Factions.FirstAsync(x => x.InternalName == t2, token).ConfigureAwait(false);

        try
        {
            int rowCt = 0;
            List<LegacyPointsInfo> points = new List<LegacyPointsInfo>(65536);

            await _mySqlProvider.QueryAsync($"SELECT `Steam64`,`Team`,`{xpName}`,`{creditsName}` FROM `{tableName}`;", null, token, reader =>
            {
                LegacyPointsInfo pt = default;
                pt.Steam64 = reader.GetUInt64(0);
                pt.Team = reader.GetUInt64(1);
                pt.Experience = reader.IsDBNull(2) ? 0 : reader.GetUInt32(2);
                pt.Credits = reader.IsDBNull(3) ? (seasonId == 1 ? 0u : 500u /* starting credits */) : reader.GetUInt32(3);
                points.Add(pt);
            }).ConfigureAwait(false);

            foreach (LegacyPointsInfo pt in points)
            {
                uint factionId;

                if (pt.Team == 1)
                    factionId = t1Faction.Key;
                else if (pt.Team == 2)
                    factionId = t2Faction.Key;
                else continue;

                await _pointsSql.SetPointsAsync(new CSteamID(pt.Steam64), factionId, seasonId, pt.Experience, pt.Credits, token);
            }
        }
        catch (MySqlException ex)
        {
            Context.Logger.LogWarning(ex, "Table may not exist: {0}.", tableName);
        }
    }

    private struct LegacyPointsInfo
    {
        public ulong Steam64;
        public ulong Team;
        public uint Experience;
        public uint Credits;
    }
}
#endif