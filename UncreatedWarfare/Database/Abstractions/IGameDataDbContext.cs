using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IGameDataDbContext : IFactionDbContext, ISeasonsDbContext
{
    DbSet<GameRecord> Games { get; }
    DbSet<SessionRecord> Sessions { get; }
    public new static void ConfigureModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WarfareUserData>()
            .HasMany<SessionRecord>()
            .WithOne(x => x.PlayerData)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GameRecord>()
            .HasMany(x => x.Sessions)
            .WithOne(x => x.Game)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<SessionRecord>()
            .HasOne(x => x.Map)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SessionRecord>()
            .HasOne(x => x.Kit)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SessionRecord>()
            .HasOne(x => x.Season)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SessionRecord>()
            .HasOne(x => x.Faction)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
