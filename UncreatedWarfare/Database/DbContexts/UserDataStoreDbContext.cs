using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenMod.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore.Configurator;
using System;
using Uncreated.Warfare.API.Permissions;
using Uncreated.Warfare.Automation;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Database.DbContexts;

/// <summary>
/// <see cref="LanguageInfo"/>
/// </summary>
[WarfareDatabaseContext]
public class UserDataStoreDbContext : OpenModDbContext<UserDataStoreDbContext>
{
    public DbSet<WarfareUserData> UserData => Set<WarfareUserData>();
    public UserDataStoreDbContext(IServiceProvider serviceProvider) : base(serviceProvider) { }
    public UserDataStoreDbContext(IDbContextConfigurator configurator, IServiceProvider serviceProvider)
        : base(configurator, serviceProvider) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WarfareUserData>()
            .Property(x => x.PermissionLevel)
            .HasConversion<EnumToStringConverter<PermissionLevel>>();
    }
}