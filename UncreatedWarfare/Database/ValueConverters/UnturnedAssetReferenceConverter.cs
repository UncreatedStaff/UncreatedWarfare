using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Globalization;
using System.Linq.Expressions;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Database.ValueConverters;
public class UnturnedAssetReferenceConverter : ValueConverter<UnturnedAssetReference, string>
{
    public UnturnedAssetReferenceConverter()
        : base(x => x.Guid != Guid.Empty ? x.Guid.ToString("N") : x.Id.ToString(CultureInfo.InvariantCulture), x => UnturnedAssetReference.Parse(x))
    {

    }
}
public static class AssetReferenceConverterHelper
{
    private static UnturnedAssetReferenceConverter? _instance;
    public static void WithAssetReferenceConverter<TEntity>(this ModelBuilder builder,
        Expression<Func<TEntity, UnturnedAssetReference>> expression) where TEntity : class
    {
        _instance ??= new UnturnedAssetReferenceConverter();
        builder.Entity<TEntity>()
            .Property(expression)
            .HasConversion(_instance)
            .HasColumnType("char(32)");
    }
}