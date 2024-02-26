using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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

            MemberInfo member = assetProperty.GetMemberInfo(true, false);

            Type delegateType = typeof(Func<,>).MakeGenericType(entityType, assetProperty.IsNullable ? typeof(UnturnedAssetReference?) : typeof(UnturnedAssetReference));
            Delegate caller;
            if (member is PropertyInfo property)
            {
                MethodInfo getMethod = property.GetGetMethod(true) ??
                                       throw new ArgumentException($"Asset property does not have a getter ({member}).", nameof(assetProperty));

                caller = Accessor.GenerateInstanceCaller(delegateType, getMethod, true, true)!;
            }
            else
            {
                if (member is not FieldInfo underlyingField)
                    throw new ArgumentException($"Asset is not a field or property ({member}).", nameof(assetProperty));

                Delegate generateInstanceGetter = assetProperty.IsNullable
                    ? Accessor.GenerateInstanceGetter<UnturnedAssetReference?>(entityType, underlyingField.Name, throwOnError: true)!
                    : Accessor.GenerateInstanceGetter<UnturnedAssetReference>(entityType, underlyingField.Name, throwOnError: true)!;

                MethodInfo getMethod = generateInstanceGetter.Method;

                caller = getMethod.IsStatic 
                    ? Accessor.GenerateStaticCaller(delegateType, generateInstanceGetter.Method, true, true)!
                    : Accessor.GenerateInstanceCaller(delegateType, generateInstanceGetter.Method, true, true)!;
            }

            ValueGenerator? newGenerator = (ValueGenerator?)typeof(AssetNameValueGenerator<>).MakeGenericType(entityType).GetConstructor([delegateType])?.Invoke([caller]);

            if (newGenerator == null)
                throw new NotSupportedException("Failed to create AssetNameValueGenerator<" + entityType.FullName + ">.");

            Cache.Add(entityType, newGenerator);
            return newGenerator;
        }
    }
}
