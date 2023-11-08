using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq.Expressions;

namespace Uncreated.Warfare.Database.ValueConverters;
public class GuidStringConverter : ValueConverter<Guid, string>
{
    public GuidStringConverter() : base(x => x.ToString("N"), x => Guid.ParseExact(x, "N")) { }
}

public static class GuidStringConverterHelper
{
    private static readonly GuidStringConverter Instance = new GuidStringConverter();
    public static void WithGuidStringConverter<TEntity, TProperty>(this ModelBuilder builder,
        Expression<Func<TEntity, TProperty>> expression) where TEntity : class
    {
        builder.Entity<TEntity>()
            .Property(expression)
            .HasConversion(Instance);
    }
}