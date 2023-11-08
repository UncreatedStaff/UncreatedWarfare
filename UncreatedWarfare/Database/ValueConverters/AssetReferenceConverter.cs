using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SDG.Unturned;
using System;
using System.Linq.Expressions;

namespace Uncreated.Warfare.Database.ValueConverters;
public class AssetReferenceConverter<TAsset> : ValueConverter<AssetReference<TAsset>, string> where TAsset : Asset
{
    public AssetReferenceConverter()
        : base(x => x.GUID.ToString("N"), x => new AssetReference<TAsset>(Guid.ParseExact(x, "N"))) { }
}
public static class AssetReferenceConverterHelper
{
    public static void WithAssetReferenceConverter<TEntity, TAsset>(this ModelBuilder builder,
        Expression<Func<TEntity, AssetReference<TAsset>>> expression) where TEntity : class where TAsset : Asset
    {
        builder.Entity<TEntity>()
            .Property(expression)
            .HasConversion(new AssetReferenceConverter<TAsset>());
    }
}