using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Kits.Bundles;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Seasons;
using Uncreated.Warfare.Models.Stats.Records;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Database;
#pragma warning disable CS8644
public class WarfareDbContext : DbContext, IFactionDbContext, IUserDataDbContext, ILanguageDbContext, IKitsDbContext, IStatsDbContext, ISeasonsDbContext, IGameDataDbContext
{
    internal static string? ConnStringOverride = null;

    public DbSet<LanguageInfo> Languages => Set<LanguageInfo>();
    public DbSet<LanguagePreferences> LanguagePreferences => Set<LanguagePreferences>();
    public DbSet<WarfareUserData> UserData => Set<WarfareUserData>();
    public DbSet<PlayerIPAddress> IPAddresses => Set<PlayerIPAddress>();
    public DbSet<PlayerHWID> HWIDs => Set<PlayerHWID>();
    public DbSet<Faction> Factions => Set<Faction>();
    public DbSet<Kit> Kits => Set<Kit>();
    public DbSet<KitAccess> KitAccess => Set<KitAccess>();
    public DbSet<KitHotkey> KitHotkeys => Set<KitHotkey>();
    public DbSet<KitLayoutTransformation> KitLayoutTransformations => Set<KitLayoutTransformation>();
    public DbSet<KitFavorite> KitFavorites => Set<KitFavorite>();
    public DbSet<EliteBundle> EliteBundles => Set<EliteBundle>();
    public DbSet<GameRecord> Games => Set<GameRecord>();
    public DbSet<SessionRecord> Sessions => Set<SessionRecord>();
    public DbSet<MapData> Maps => Set<MapData>();
    public DbSet<SeasonData> Seasons => Set<SeasonData>();
    public DbSet<DeathRecord> DeathRecords => Set<DeathRecord>();
    public DbSet<DamageRecord> DamageRecords => Set<DamageRecord>();
    public DbSet<AidRecord> AidRecords => Set<AidRecord>();

    /* configure database settings */
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connectionString = ConnStringOverride ?? UCWarfare.Config.SqlConnectionString ?? (UCWarfare.Config.RemoteSQL ?? UCWarfare.Config.SQL).GetConnectionString("UCWarfare", true, true);

        optionsBuilder.UseMySql(connectionString, x => x
            .CharSet(CharSet.Utf8Mb4)
            .CharSetBehavior(CharSetBehavior.AppendToAllColumns));

        optionsBuilder.EnableSensitiveDataLogging();

        IDbContextOptionsBuilderInfrastructure settings = optionsBuilder;
        
        // for some reason default logging completely crashes the server
        CoreOptionsExtension extension = (optionsBuilder.Options.FindExtension<CoreOptionsExtension>() ?? new CoreOptionsExtension()).WithLoggerFactory(new L.UCLoggerFactory());
        settings.AddOrUpdateExtension(extension);
    }

    /* further configure models than what's possible with attributes */
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ILanguageDbContext.ConfigureModels(modelBuilder);
        IFactionDbContext.ConfigureModels(modelBuilder);
        IUserDataDbContext.ConfigureModels(modelBuilder);
        IKitsDbContext.ConfigureModels(modelBuilder);
        IStatsDbContext.ConfigureModels(modelBuilder);
        ISeasonsDbContext.ConfigureModels(modelBuilder);
        IGameDataDbContext.ConfigureModels(modelBuilder);

        /* Adds preset value converters */
        WarfareDatabaseReflection.ApplyValueConverterConfig(modelBuilder);

        Console.WriteLine("Model created.");
    }
    [Obsolete]
    public Task WaitAsync(CancellationToken token = default) => WarfareDatabases.WaitAsync(token);
    [Obsolete]
    public Task WaitAsync(TimeSpan timeout, CancellationToken token = default) => WarfareDatabases.WaitAsync(timeout, token);
    [Obsolete]
    public Task WaitAsync(int timeoutMilliseconds, CancellationToken token = default) => WarfareDatabases.WaitAsync(timeoutMilliseconds, token);
    [Obsolete]
    public void Wait(CancellationToken token = default) => WarfareDatabases.Wait(token);
    [Obsolete]
    public void Wait(TimeSpan timeout, CancellationToken token = default) => WarfareDatabases.Wait(timeout, token);
    [Obsolete]
    public void Wait(int timeoutMilliseconds, CancellationToken token = default) => WarfareDatabases.Wait(timeoutMilliseconds, token);
    [Obsolete]
    public void Release(int amt = 1) => WarfareDatabases.Release(amt);
}
#pragma warning restore CS8644