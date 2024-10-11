using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
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

/// <summary>
/// Adds a column to <see cref="UnturnedAssetReference"/> columns that stores the English name of the asset.
/// </summary>
public static class AssetNameValueGenerator
{
    private static readonly Dictionary<ValueGeneratorInstance, ValueGenerator> Cache = new Dictionary<ValueGeneratorInstance, ValueGenerator>();

    private readonly struct ValueGeneratorInstance(Type type, string name) : IEquatable<ValueGeneratorInstance>
    {
        public readonly Type Type = type;
        public readonly string Name = name;

        public override bool Equals(object? obj)
        {
            return obj is ValueGeneratorInstance inst && Equals(inst);
        }

        public bool Equals(ValueGeneratorInstance other)
        {
            return other.Type == Type && other.Name == Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Name);
        }
    }

    public static ValueGenerator Get(IEntityType type, string name)
    {
        lock (Cache)
        {
            IEFCompatProvider efCompat = EFCompat.Instance;
            Type entityType = efCompat.GetClrType(type);
            ValueGeneratorInstance inst = new ValueGeneratorInstance(entityType, name);
            if (Cache.TryGetValue(inst, out ValueGenerator val))
                return val;

            IProperty assetProperty = efCompat.GetProperties(type).First(x => efCompat.GetName(x).Equals(name, StringComparison.Ordinal));
            
            MemberInfo member = efCompat.GetMemberInfo(assetProperty, true, false);

            bool isNullable = efCompat.GetIsNullable(assetProperty);

            Type delegateType = typeof(Func<,>).MakeGenericType(entityType, isNullable ? typeof(UnturnedAssetReference?) : typeof(UnturnedAssetReference));
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

                Delegate generateInstanceGetter = isNullable
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

            Cache.Add(inst, newGenerator);
            return newGenerator;
        }
    }
}
