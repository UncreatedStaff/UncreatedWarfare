using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System;
using System.Collections.Generic;
using System.Reflection;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Database.ValueGenerators;

public class AssetNameValueGenerator<TEntity> : ValueGenerator<string?> where TEntity : class, new()
{
    private readonly Func<TEntity, UnturnedAssetReference>? _assetFactory;
    private readonly Func<TEntity, UnturnedAssetReference?>? _nullableAssetFactory;

    public AssetNameValueGenerator(Func<TEntity, UnturnedAssetReference> assetFactory)
    {
        _assetFactory = assetFactory;
    }
    public AssetNameValueGenerator(Func<TEntity, UnturnedAssetReference?> assetFactory)
    {
        _nullableAssetFactory = assetFactory;
    }
    public override string? Next(EntityEntry entry)
    {
        if (_assetFactory != null)
        {
            UnturnedAssetReference reference = _assetFactory.Invoke((TEntity)entry.Entity);
            return reference.GetFriendlyName() ?? string.Empty;
        }
        else
        {
            UnturnedAssetReference? reference = _nullableAssetFactory!.Invoke((TEntity)entry.Entity);
            return reference.HasValue ? reference.Value.GetFriendlyName() ?? string.Empty : null;
        }
    }

    public override bool GeneratesTemporaryValues => false;

}
public static class AssetNameValueGenerator
{
    private static readonly Dictionary<Type, ValueGenerator> Cache = new Dictionary<Type, ValueGenerator>();

    public static ValueGenerator Get(Type entityType, IProperty assetProperty)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(entityType, out ValueGenerator val))
                return val;

            MethodInfo getMethod = (assetProperty.GetMemberInfo(false, false) as PropertyInfo)?.GetGetMethod(true) ??
                                   throw new ArgumentException("Asset property does not have a getter or is not a property.", nameof(assetProperty));

            Type delegateType = typeof(Func<,>).MakeGenericType(entityType, assetProperty.IsNullable ? typeof(UnturnedAssetReference?) : typeof(UnturnedAssetReference));
            Delegate caller = Accessor.GenerateInstanceCaller(delegateType, getMethod, true, true)!;
            ValueGenerator? newGenerator = (ValueGenerator?)typeof(AssetNameValueGenerator<>).MakeGenericType(entityType).GetConstructor(new Type[] { delegateType })?.Invoke(new object[] { caller });

            if (newGenerator == null)
                throw new NotSupportedException("Failed to create AssetNameValueGenerator<" + entityType.FullName + ">.");

            Cache.Add(entityType, newGenerator);
            return newGenerator;
        }
    }
}
