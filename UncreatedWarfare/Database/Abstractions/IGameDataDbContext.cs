using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IGameDataDbContext : IDbContext
{
    DbSet<GameRecord> Games { get; }
    DbSet<SessionRecord> Sessions { get; }
    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WarfareUserData>()
            .HasMany<SessionRecord>()
            .WithOne(x => x.PlayerData)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<GameRecord>()
            .HasMany(x => x.Sessions)
            .WithOne(x => x.Game)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SessionRecord>()
            .HasOne(x => x.PreviousSession)
            .WithOne()
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SessionRecord>()
            .HasOne(x => x.NextSession)
            .WithOne()
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
