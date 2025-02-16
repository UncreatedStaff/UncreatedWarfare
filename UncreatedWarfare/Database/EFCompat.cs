using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Database.Automation;

namespace Uncreated.Warfare.Database;

/// <summary>
/// Abstracts some properties used in <see cref="WarfareDatabaseReflection"/> to support later versions of EF.
/// </summary>
public static class EFCompat
{
    [field: AllowNull, MaybeNull]
    public static IEFCompatProvider Instance
    {
        get => field ??= new EF5Compat();
        set;
    }

    public class EF5Compat : IEFCompatProvider
    {
        public Type GetClrType(ITypeBase type)
        {
            return type.ClrType;
        }

        public Type GetClrType(IMutableProperty prop)
        {
            return prop.ClrType;
        }

        public PropertyInfo? GetPropertyInfo(IMutableProperty prop)
        {
            return prop.PropertyInfo;
        }

        public string GetName(IPropertyBase prop)
        {
            return prop.Name;
        }

        public ValueConverter? GetValueConverter(IProperty prop)
        {
            return prop.GetValueConverter();
        }

        public string GetName(ITypeBase type)
        {
            return type.Name;
        }

        public MemberInfo GetMemberInfo(IPropertyBase prop, bool forMaterialization, bool forSet)
        {
            return prop.GetMemberInfo(forMaterialization, forSet);
        }

        public IMutableEntityType? RemoveEntityType(IMutableModel model, Type type)
        {
            return model.RemoveEntityType(type);
        }

        public IMutableProperty AddProperty(IMutableEntityType type, MemberInfo member)
        {
            return type.AddProperty(member);
        }

        public IMutableProperty AddProperty(IMutableEntityType type, string name, Type clrType)
        {
            return type.AddProperty(name, clrType);
        }

        public IMutableIndex AddIndex(IMutableEntityType type, IMutableProperty prop)
        {
            return type.AddIndex(prop);
        }

        public void SetValueConverter(IMutableProperty prop, ValueConverter? valueConverter)
        {
            prop.SetValueConverter(valueConverter);
        }

        public void SetMaxLength(IMutableProperty prop, int? maxLength)
        {
            prop.SetMaxLength(maxLength);
        }

        public void SetValueGeneratorFactory(IMutableProperty prop, Func<IProperty, IEntityType, ValueGenerator> valueGeneratorFactory)
        {
            prop.SetValueGeneratorFactory(valueGeneratorFactory);
        }

        public IEnumerable<IMutableProperty> GetProperties(IMutableEntityType entity)
        {
            return entity.GetProperties();
        }

        public IEnumerable<IProperty> GetProperties(IEntityType entity)
        {
            return entity.GetProperties();
        }

        public void SetIsNullable(IMutableProperty prop, bool isNullable)
        {
            prop.IsNullable = isNullable;
        }

        public bool GetIsNullable(IProperty prop)
        {
            return prop.IsNullable;
        }

        public void SetDefaultValue(IMutableProperty prop, string? defaultValue)
        {
            prop.SetDefaultValue(defaultValue);
        }

        public void SetColumnType(IMutableProperty prop, string columnType)
        {
            prop.SetColumnType(columnType);
        }

        public IMutableEntityType GetDeclaringEntityType(IMutableProperty prop)
        {
            return prop.DeclaringEntityType;
        }

        public string GetColumnType(IMutableProperty prop)
        {
            return prop.GetColumnType();
        }

        private static readonly EventDefinitionBase NoExceptionDuringSaveChanges =
            new EventDefinition<Type, string, Exception>(new LoggingOptions(), default, LogLevel.None, "NONE", _ => (_, _, _, _, _) => { });

        public void DontLogExceptionDuringSaveChanges(DbContext dbContext)
        {
            ((IDbContext)dbContext).UpdateLogger.Definitions.LogExceptionDuringSaveChanges = NoExceptionDuringSaveChanges;
        }
    }
}

public interface IEFCompatProvider
{
    Type GetClrType(ITypeBase type);
    Type GetClrType(IMutableProperty prop);
    PropertyInfo? GetPropertyInfo(IMutableProperty prop);
    string GetName(IPropertyBase prop);
    ValueConverter? GetValueConverter(IProperty prop);
    string GetName(ITypeBase type);
    MemberInfo GetMemberInfo(IPropertyBase prop, bool forMaterialization, bool forSet);
    IMutableEntityType? RemoveEntityType(IMutableModel model, Type type);
    IMutableProperty AddProperty(IMutableEntityType type, MemberInfo member);
    IMutableProperty AddProperty(IMutableEntityType type, string name, Type clrType);
    IMutableIndex AddIndex(IMutableEntityType type, IMutableProperty prop);
    void SetValueConverter(IMutableProperty prop, ValueConverter? valueConverter);
    void SetMaxLength(IMutableProperty prop, int? maxLength);
    void SetValueGeneratorFactory(IMutableProperty prop, Func<IProperty, IEntityType, ValueGenerator> valueGeneratorFactory);
    IEnumerable<IMutableProperty> GetProperties(IMutableEntityType entity);
    IEnumerable<IProperty> GetProperties(IEntityType entity);
    void SetIsNullable(IMutableProperty prop, bool isNullable);
    bool GetIsNullable(IProperty prop);
    void SetDefaultValue(IMutableProperty prop, string? defaultValue);
    void SetColumnType(IMutableProperty prop, string columnType);
    IMutableEntityType GetDeclaringEntityType(IMutableProperty prop);
    string GetColumnType(IMutableProperty prop);
    void DontLogExceptionDuringSaveChanges(DbContext dbContext);
}