using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models;
using Uncreated.Warfare.Models.Authentication;
using Uncreated.Warfare.Models.Buildables;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Kits.Bundles;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Seasons;
using Uncreated.Warfare.Models.Stats;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Models.Web;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Database;
#pragma warning disable CS8644
public class WarfareDbContext : DbContext, IUserDataDbContext, ILanguageDbContext, IKitsDbContext, IStatsDbContext, IGameDataDbContext, IBuildablesDbContext, IWhitelistDbContext
{
    private readonly ILogger<WarfareDbContext> _logger;

    public DbSet<LanguageInfo> Languages => Set<LanguageInfo>();
    public DbSet<LanguagePreferences> LanguagePreferences => Set<LanguagePreferences>();
    public DbSet<WarfareUserData> UserData => Set<WarfareUserData>();
    public DbSet<GlobalBanWhitelist> GlobalBanWhitelists => Set<GlobalBanWhitelist>();
    public DbSet<PlayerIPAddress> IPAddresses => Set<PlayerIPAddress>();
    public DbSet<PlayerHWID> HWIDs => Set<PlayerHWID>();
    public DbSet<Faction> Factions => Set<Faction>();
    public DbSet<KitModel> Kits => Set<KitModel>();
    public DbSet<KitAccess> KitAccess => Set<KitAccess>();
    public DbSet<KitHotkey> KitHotkeys => Set<KitHotkey>();
    public DbSet<KitLayoutTransformation> KitLayoutTransformations => Set<KitLayoutTransformation>();
    public DbSet<KitFavorite> KitFavorites => Set<KitFavorite>();
    public DbSet<EliteBundle> EliteBundles => Set<EliteBundle>();
    public DbSet<GameRecord> Games => Set<GameRecord>();
    public DbSet<SessionRecord> Sessions => Set<SessionRecord>();
    public DbSet<MapData> Maps => Set<MapData>();
    public DbSet<SeasonData> Seasons => Set<SeasonData>();
    public DbSet<FobRecord> FobRecords => Set<FobRecord>();
    public DbSet<FobItemRecord> FobItemRecords => Set<FobItemRecord>();
    public DbSet<DeathRecord> DeathRecords => Set<DeathRecord>();
    public DbSet<DamageRecord> DamageRecords => Set<DamageRecord>();
    public DbSet<AidRecord> AidRecords => Set<AidRecord>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<BuildableSave> Saves => Set<BuildableSave>();
    public DbSet<ItemWhitelist> Whitelists => Set<ItemWhitelist>();
    public DbSet<SteamDiscordPendingLink> PendingLinks => Set<SteamDiscordPendingLink>();
    
    public DbSet<LoadoutPurchase> Loadouts => Set<LoadoutPurchase>();
    public DbSet<HomebaseAuthenticationKey> HomebaseAuthenticationKeys => Set<HomebaseAuthenticationKey>();



    public WarfareDbContext(ILogger<WarfareDbContext> logger, DbContextOptions<WarfareDbContext> options) : base(options)
    {
        // supress exceptions from being logged separately
        // this allows us to catch Unique Key and Primary Key
        // constraint violation exceptions for threadsafe add operations (MySQL 1062: DuplicateKeyEntry)

        _logger = logger;
        EFCompat.Instance.DontLogExceptionDuringSaveChanges(this);
    }

    /// <summary>
    /// Used to auto-fill the 'options' parameters.
    /// </summary>
    internal static DbContextOptions<WarfareDbContext> GetOptions(IServiceProvider serviceProvider)
    {
        DbContextOptionsBuilder<WarfareDbContext> builder = new DbContextOptionsBuilder<WarfareDbContext>();

        builder.UseApplicationServiceProvider(serviceProvider);
        Configure(builder, serviceProvider.GetRequiredService<ILogger<WarfareDbContext>>(), serviceProvider.GetRequiredService<IConfiguration>());

        return builder.Options;
    }

    /* configure database settings */
    private static void Configure(DbContextOptionsBuilder optionsBuilder, ILogger logger, IConfiguration sysConfig)
    {
        IConfiguration databaseSection = sysConfig.GetSection("database");

        string? connectionStringType = databaseSection["connection_string_name"];

        if (string.IsNullOrWhiteSpace(connectionStringType))
            connectionStringType = "warfare-db";

        string? connectionString = sysConfig.GetConnectionString(connectionStringType);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Missing connection string: \"{connectionStringType}\".");

        bool sensitiveDataLogging = false;
        if (databaseSection.GetValue<bool>("sensitive_data_logging"))
        {
            sensitiveDataLogging = true;
            logger.LogInformation("Sensitive data logging is enabled.");
        }

        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString),
            o => o
                .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                .EnableRetryOnFailure()
        );

        optionsBuilder.UseBatchEF_MySQLPomelo();

        if (sensitiveDataLogging)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }

    /* further configure models than what's possible with attributes */
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCharSet(CharSet.Utf8Mb4, DelegationModes.ApplyToColumns);

        ILanguageDbContext.ConfigureModels(modelBuilder);
        IFactionDbContext.ConfigureModels(modelBuilder);
        IBuildablesDbContext.ConfigureModels(modelBuilder);
        IUserDataDbContext.ConfigureModels(modelBuilder);
        IKitsDbContext.ConfigureModels(modelBuilder);
        IStatsDbContext.ConfigureModels(modelBuilder);
        ISeasonsDbContext.ConfigureModels(modelBuilder);
        IGameDataDbContext.ConfigureModels(modelBuilder);
        IWhitelistDbContext.ConfigureModels(modelBuilder);

        // add the RAND() function in EFCore 5
        //if (WarfareModule.IsActive)
        //{
        //    modelBuilder.HasDbFunction(Accessor.GetMethod(WarfareEFFunctions.Random)!, bldr =>
        //    {
        //        // ReSharper disable once UseArrayEmptyMethod
        //        bldr.HasTranslation(_ => new SqlFunctionExpression("RAND", new SqlExpression[0], nullable: false,
        //            Array.Empty<bool>(), typeof(double), new DoubleTypeMapping("double", DbType.Double)));
        //    });
        //}

        /* Adds preset value converters */
        WarfareDatabaseReflection.ApplyValueConverterConfig(modelBuilder, _logger);

        _logger.LogInformation("Model created.");
    }
}
#pragma warning restore CS8644