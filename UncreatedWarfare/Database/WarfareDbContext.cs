using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Reflection;
using Uncreated.Networking;
using Uncreated.SQL;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Database.ValueConverters;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Database;
#nullable disable
public class WarfareDbContext : DbContext, IFactionDbContext, IUserDataDbContext, ILanguageDbContext
#nullable restore
{
    internal static string? ConnStringOverride = null;

    public DbSet<Models.Localization.LanguageInfo> Languages => Set<Models.Localization.LanguageInfo>();
    public DbSet<LanguagePreferences> LanguagePreferences => Set<LanguagePreferences>();
    public DbSet<WarfareUserData> UserData => Set<WarfareUserData>();
    public DbSet<Faction> Factions => Set<Faction>();

    /* automatically applied value converters */
    private static void AddValueConverters(IDictionary<Type, Type> valueConverters)
    {
        valueConverters.Add(typeof(Guid), typeof(GuidStringValueConverter));
        valueConverters.Add(typeof(UnturnedAssetReference), typeof(UnturnedAssetReferenceValueConverter));
        valueConverters.Add(typeof(HWID), typeof(HWIDValueConverter));
        valueConverters.Add(typeof(IPAddress), typeof(IPAddressConverter));
        valueConverters.Add(typeof(IPv4Range), typeof(IPv4RangeConverter));
    }

    /* configure database settings */
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connectionString = ConnStringOverride ?? UCWarfare.Config.SqlConnectionString ?? (UCWarfare.Config.RemoteSQL ?? UCWarfare.Config.SQL).GetConnectionString("UCWarfare", true, true);

        optionsBuilder.UseMySql(connectionString, x => x
            .CharSet(CharSet.Utf8Mb4)
            .CharSetBehavior(CharSetBehavior.AppendToAllColumns));

        optionsBuilder.EnableSensitiveDataLogging();

        IDbContextOptionsBuilderInfrastructure settings = optionsBuilder;
        
        // for some reason default logging completely crashes the server
        CoreOptionsExtension extension = (optionsBuilder.Options.FindExtension<CoreOptionsExtension>() ?? new CoreOptionsExtension()).WithLoggerFactory(new L.UCLoggerFactory());
        settings.AddOrUpdateExtension(extension);
    }

    /* further configure models than what's possible with attributes */
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ILanguageDbContext.ConfigureModels(modelBuilder);
        IFactionDbContext.ConfigureModels(modelBuilder);
        IUserDataDbContext.ConfigureModels(modelBuilder);

        /* Adds preset value converters */
        ApplyValueConverterConfig(modelBuilder);
    }

    #region Internals
    private static void ApplyValueConverterConfig(ModelBuilder modelBuilder)
    {
        Dictionary<Type, Type> valueConverters = new Dictionary<Type, Type>(16);
        AddValueConverters(valueConverters);

        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes().OrderBy(x => x.ClrType.FullName).ToList())
        {
            PropertyInfo[] properties = entity.ClrType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            if (valueConverters.ContainsKey(entity.ClrType) || entity.ClrType.IsDefinedSafe<ValueConverterAttribute>())
            {
                if (modelBuilder.Model.RemoveEntityType(entity.ClrType) != null)
                    Log($"Removed entity type {entity.ClrType.Name}.");

                continue;
            }

            foreach (PropertyInfo property in properties)
            {
                if (entity.GetProperties().Any(x => x.PropertyInfo == property) || property.IsDefinedSafe<NotMappedAttribute>())
                    continue;

                Type clrType = property.PropertyType;
                if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    clrType = clrType.GenericTypeArguments[0];

                ValueConverterAttribute? propertyAttribute = property.GetAttributeSafe<ValueConverterAttribute>();
                ValueConverterAttribute? typeAttribute = clrType.GetAttributeSafe<ValueConverterAttribute>();
                Type? type = propertyAttribute?.Type ?? typeAttribute?.Type;
                if (type == null && !valueConverters.TryGetValue(clrType, out type))
                    continue;

                entity.AddProperty(property);
                Log($"Added field {entity.ClrType.Name + "." + property.Name,-66} that was excluded.");
                if (propertyAttribute?.Type == null && modelBuilder.Model.RemoveEntityType(clrType) != null)
                    Log($"Removed entity type {entity.ClrType.Name}.");
            }

            FieldInfo[] fields = entity.ClrType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                if (entity.GetProperties().Any(x => x.FieldInfo == field) || field.IsDefinedSafe<NotMappedAttribute>())
                    continue;

                Type clrType = field.FieldType;
                if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    clrType = clrType.GenericTypeArguments[0];

                ValueConverterAttribute? propertyAttribute = field.GetAttributeSafe<ValueConverterAttribute>();
                ValueConverterAttribute? typeAttribute = clrType.GetAttributeSafe<ValueConverterAttribute>();
                Type? type = propertyAttribute?.Type ?? typeAttribute?.Type;
                if (type == null && !valueConverters.TryGetValue(clrType, out type))
                    continue;

                entity.AddProperty(field);
                Log($"Added field {entity.ClrType.Name + "." + field.Name,-66} that was excluded.");
                if (propertyAttribute?.Type == null && modelBuilder.Model.RemoveEntityType(clrType) != null)
                    Log($"Removed entity type {entity.ClrType.Name}.");
            }
        }
#pragma warning disable EF1001
        foreach (IMutableProperty property in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetProperties()).OrderBy(x => x.DeclaringEntityType.ClrType.FullName).ThenBy(x => x.GetColumnOrdinal()).ToList())
#pragma warning restore EF1001
        {
            bool nullable = false;
            Type clrType = property.ClrType;
            if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                nullable = true;
                clrType = clrType.GenericTypeArguments[0];
            }

            // Log($"Checking property {property.DeclaringEntityType.ClrType.Name + "." + property.Name,-60} of type {clrType.Name,-30}.");

            MemberInfo? member = (MemberInfo?)property.PropertyInfo ?? property.FieldInfo;

            ValueConverterAttribute? propertyAttribute = member?.GetAttributeSafe<ValueConverterAttribute>();
            ValueConverterAttribute? typeAttribute = property.ClrType.GetAttributeSafe<ValueConverterAttribute>();

            if (clrType == typeof(IPAddress) && (member == null || !member.IsDefinedSafe<DontAddPackedColumnAttribute>()))
            {
                // add packed column for IP addresses
                string name = property.Name + "Packed";
                if (property.DeclaringEntityType.GetProperties().Any(x => x.Name.Equals(name, StringComparison.Ordinal)))
                {
                    property.DeclaringEntityType.AddProperty(name, typeof(uint));
                    Log($"Added packed IP column for {property.DeclaringEntityType.ClrType.Name}.{member?.Name ?? "null"}: {name}.");
                }
            }

            // enum types
            if (clrType.IsEnum && propertyAttribute == null)
            {
                Type converterType = typeof(EnumToStringConverter<>).MakeGenericType(clrType);
                string dataType;
                if (member != null)
                {
                    Attribute[] attributes = Attribute.GetCustomAttributes(member, typeof(ExcludedEnumAttribute));

                    if (attributes.Length == 0)
                        attributes = Attribute.GetCustomAttributes(clrType, typeof(ExcludedEnumAttribute));

                    object[] values = attributes.Select(x => ((ExcludedEnumAttribute)x).Value!).Where(x => x != null).ToArray();
                    if (values.Length == 0)
                    {
                        MethodInfo sqlEnumMethod = typeof(SqlTypes)
                            .GetMethods()
                            .Single(x => x.Name.Equals(nameof(SqlTypes.Enum), StringComparison.Ordinal)
                                         && x.GetParameters().Length == 0);

                        dataType = (string)sqlEnumMethod
                            .MakeGenericMethod(clrType)
                            .Invoke(null, values);
                    }
                    else if (values.Length == 1)
                    {
                        MethodInfo sqlEnumMethod = typeof(SqlTypes)
                            .GetMethods()
                            .Single(x => x.Name.Equals(nameof(SqlTypes.Enum), StringComparison.Ordinal)
                                         && x.GetParameters().Length == 1 && !typeof(IEnumerable<>).MakeGenericType(x.GetGenericArguments()[0]).IsAssignableFrom(x.GetParameters()[0].ParameterType));

                        dataType = (string)sqlEnumMethod
                            .MakeGenericMethod(clrType)
                            .Invoke(null, values);
                    }
                    else if (values.Length == 2)
                    {
                        MethodInfo sqlEnumMethod = typeof(SqlTypes)
                            .GetMethods()
                            .Single(x => x.Name.Equals(nameof(SqlTypes.Enum), StringComparison.Ordinal)
                                         && x.GetParameters().Length == 2);

                        dataType = (string)sqlEnumMethod
                            .MakeGenericMethod(clrType)
                            .Invoke(null, values);
                    }
                    else
                    {
                        ArrayList list = new ArrayList();
                        for (int i = 0; i < values.Length; ++i)
                        {
                            object value1 = values[i];

                            // check for duplicates
                            for (int j = 0; j < i; ++j)
                                if (values[j].Equals(value1))
                                    goto c;

                            list.Add(Convert.ChangeType(value1, clrType));
                        c:;
                        }

                        Array array = list.ToArray(clrType);

                        MethodInfo sqlEnumMethod = typeof(SqlTypes)
                            .GetMethods()
                            .Single(x => x.Name.Equals(nameof(SqlTypes.Enum), StringComparison.Ordinal)
                                         && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == x.GetGenericArguments()[0].MakeArrayType());

                        dataType = (string)sqlEnumMethod
                            .MakeGenericMethod(clrType)
                            .Invoke(null, new object[] { array });
                    }
                }
                else
                {
                    MethodInfo sqlEnumMethod = typeof(SqlTypes)
                        .GetMethods()
                        .Single(x => x.Name.Equals(nameof(SqlTypes.Enum), StringComparison.Ordinal) && x.GetParameters().Length == 0);

                    dataType = (string)sqlEnumMethod
                        .MakeGenericMethod(clrType)
                        .Invoke(null, Array.Empty<object>());
                }

                ValueConverter converter = CreateValueConverter(ref converterType, clrType, nullable);

                property.SetValueConverter(converter);
                property.SetColumnType(dataType);
                property.IsNullable = nullable;

                Log($"Set converter for {property.DeclaringEntityType.Name}.{property.Name} to {converterType.Name}.");
                Log($" - Type: {dataType}.");
                continue;
            }

            Type? valConverterType;
            if (typeAttribute == null && propertyAttribute == null)
                valueConverters.TryGetValue(clrType, out valConverterType);
            else if (propertyAttribute is not { Type: null })
                valConverterType = propertyAttribute?.Type ?? propertyAttribute?.Type;
            else continue;

            if (valConverterType == null)
                continue;
            
            ValueConverterCallbackAttribute? callback = valConverterType.GetAttributeSafe<ValueConverterCallbackAttribute>();

            if (valConverterType.IsGenericTypeDefinition)
            {
                if (clrType.IsGenericType && valConverterType.GenericTypeArguments.Length == clrType.GenericTypeArguments.Length)
                {
                    try
                    {
                        valConverterType = valConverterType.MakeGenericType(clrType.GenericTypeArguments);
                    }
                    catch (ArgumentException)
                    {
                        try
                        {
                            if (valConverterType.GenericTypeArguments.Length == 1)
                                valConverterType = valConverterType.MakeGenericType(clrType);
                        }
                        catch (ArgumentException)
                        {

                        }
                    }
                }
                else
                {
                    try
                    {
                        if (valConverterType.GenericTypeArguments.Length == 1)
                            valConverterType = valConverterType.MakeGenericType(clrType);
                    }
                    catch (ArgumentException)
                    {

                    }
                }
            }


            if (valConverterType.IsGenericTypeDefinition)
                throw new InvalidOperationException($"Can not use generic type definition: {valConverterType.Name} as a converter.");

            if (!typeof(ValueConverter).IsAssignableFrom(valConverterType))
                throw new InvalidOperationException($"Can not use type: {valConverterType.Name} as a converter as it doesn't inherit {nameof(ValueConverter)}.");

            if (callback == null || string.IsNullOrEmpty(callback.MethodName))
            {
                try
                {
                    property.SetValueConverter((ValueConverter)Activator.CreateInstance(valConverterType));

                    Log($"Set converter for {property.DeclaringEntityType.Name}.{property.Name} to {valConverterType.Name}.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create value converter of type {valConverterType.Name}.", ex);
                }
            }
            else
            {
                MethodInfo? method = valConverterType.GetMethod(callback.MethodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { typeof(ModelBuilder), typeof(IMutableProperty), typeof(bool) }, null);
                if (method == null)
                    throw new MethodAccessException($"Failed to find value converter callback: {valConverterType.Name}.{callback.MethodName}.");

                try
                {
                    method.Invoke(null, new object[] { modelBuilder, property, nullable });
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception invoking value converter callback for {valConverterType.Name}.", ex);
                }

                if (valConverterType.IsInstanceOfType(property.GetValueConverter()) ||
                    nullable && valConverterType.IsGenericType && valConverterType.GetGenericTypeDefinition() == typeof(NullableConverter<,>))
                {
                    Log($"Set converter for {property.DeclaringEntityType.Name}.{property.Name} to {property.GetValueConverter().GetType().Name}.");
                    Log($" - Type: {property.GetColumnType()}.");
                    if (clrType.IsValueType)
                        property.IsNullable = nullable;

                    continue;
                }

                try
                {
                    ValueConverter converter = CreateValueConverter(ref valConverterType, clrType, nullable);
                    property.SetValueConverter(converter);

                    Log($"Set converter for {property.DeclaringEntityType.Name}.{property.Name} to {valConverterType.Name}.");
                    Log($" - Type: {property.GetColumnType()}.");
                    if (clrType.IsValueType)
                        property.IsNullable = nullable;

                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create value converter of type {valConverterType.Name}.", ex);
                }
            }
        }

        static ValueConverter CreateValueConverter(ref Type valConverterType, Type clrType, bool nullable)
        {
            ValueConverter converter;
            try
            {
                if (valConverterType.GetConstructor(Array.Empty<Type>()) == null && valConverterType.GetConstructor(new Type[] { typeof(ConverterMappingHints) }) != null)
                    converter = (ValueConverter)Activator.CreateInstance(valConverterType, new object?[] { null });
                else
                    converter = (ValueConverter)Activator.CreateInstance(valConverterType);
            }
            catch
            {
                Log($"Failed to create value converter of type {valConverterType.Name}.");
                throw;
            }
            if (nullable)
            {
                Type? secondaryType = null;
                valConverterType.ForEachBaseType((type, _) =>
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueConverter<,>))
                    {
                        secondaryType = type.GenericTypeArguments[1];
                        return false;
                    }

                    return true;
                });
                if (secondaryType != null)
                {
                    valConverterType = typeof(NullableConverter<,>).MakeGenericType(clrType, secondaryType);
                    try
                    {
                        converter = (ValueConverter)Activator.CreateInstance(valConverterType, new object?[] { converter });
                    }
                    catch
                    {
                        Log($"Failed to create nullable value converter of type {valConverterType.Name}.");
                        throw;
                    }
                }
            }

            return converter;
        }

        Log("Done");

        static void Log(string message)
        {
            if (UCWarfare.IsLoaded)
                L.LogDebug(message);
            else
                Console.WriteLine("UCWarfare: " + message);
        }
    }
    #endregion
}
