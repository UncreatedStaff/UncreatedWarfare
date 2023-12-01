using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IUserDataDbContext : IDbContext
{
    DbSet<WarfareUserData> UserData { get; }
    DbSet<PlayerIPAddress> IPAddresses { get; }
    DbSet<PlayerHWID> HWIDs { get; }

    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WarfareUserData>()
            .HasMany(x => x.IPAddresses)
            .WithOne(x => x.PlayerData)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WarfareUserData>()
            .HasMany(x => x.HWIDs)
            .WithOne(x => x.PlayerData)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PlayerIPAddress>()
            .Property(x => x.IPAddress);
    }
}
