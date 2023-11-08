using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenMod.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore.Configurator;
using SDG.Unturned;
using System;
using Uncreated.Warfare.API.Items;
using Uncreated.Warfare.Automation;
using Uncreated.Warfare.Database.ValueConverters;
using Uncreated.Warfare.Models.Teams;

namespace Uncreated.Warfare.Database.DbContexts;

/// <summary>
/// <see cref="Faction"/>
/// </summary>
[WarfareDatabaseContext]
public class FactionDbContext : OpenModDbContext<FactionDbContext>
{
    public FactionDbContext(IServiceProvider serviceProvider) : base(serviceProvider) { }
    public FactionDbContext(IDbContextConfigurator configurator, IServiceProvider serviceProvider)
        : base(configurator, serviceProvider) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.WithAssetReferenceConverter<FactionAsset, ItemAsset>(x => x.Asset);

        modelBuilder.Entity<FactionAsset>()
            .Property(x => x.Faction)
            .HasColumnType(DatabaseHelper.EnumType(ItemRedirect.None))
            .HasConversion<EnumToStringConverter<ItemRedirect>>();
    }
}
