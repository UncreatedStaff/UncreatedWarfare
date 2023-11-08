using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq.Expressions;

namespace Uncreated.Warfare.Database.ValueConverters;
public static class EnumStringConverterHelper
{
    public static void WithEnumConverter<TEntity, TEnum>(this ModelBuilder modelBuilder, Expression<Func<TEntity, TEnum>> property, TEnum? defaultValue = null) where TEnum : unmanaged, Enum where TEntity : class
    {
        modelBuilder.Entity<TEntity>()
            .Property(property)
            .HasConversion(new EnumToStringConverter<TEnum>())
            .HasColumnType(DatabaseHelper.EnumType<TEnum>());

        if (defaultValue.HasValue)
        {
            modelBuilder.Entity<TEntity>()
                .Property(property)
                .HasDefaultValue(defaultValue.Value);
        }
    }
}
