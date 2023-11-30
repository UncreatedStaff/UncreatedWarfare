using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IUserDataDbContext : IDbContext
{
    DbSet<WarfareUserData> UserData { get; }

    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        // for some reason EF tries to make a new table for IPAddress instead of using my converter, this fixes it
        modelBuilder.Entity<WarfareUserData>()
            .Property(x => x.LastIPAddress);
    }
}
