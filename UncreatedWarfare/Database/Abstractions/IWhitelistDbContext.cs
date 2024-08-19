using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IWhitelistDbContext : IDbContext
{
    DbSet<ItemWhitelist> Whitelists { get; }

    public static void ConfigureModels(ModelBuilder modelBuilder) { }
}
