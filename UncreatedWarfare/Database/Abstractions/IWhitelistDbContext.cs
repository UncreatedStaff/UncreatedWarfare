using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models;

namespace Uncreated.Warfare.Database.Abstractions;

#nullable disable

public interface IWhitelistDbContext : IDbContext
{
    DbSet<ItemWhitelist> Whitelists { get; }

    static void ConfigureModels(ModelBuilder modelBuilder) { }
}
