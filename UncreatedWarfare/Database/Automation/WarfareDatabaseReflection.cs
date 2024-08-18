using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Reflection;
using Uncreated.Warfare.Database.ValueConverters;
using Uncreated.Warfare.Database.ValueGenerators;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Database.Automation;
public static class WarfareDatabaseReflection
{
    public static int MaxAssetNameLength => 48;

    /* automatically applied value converters by type */
    public static void AddValueConverters(IDictionary<Type, Type> valueConverters)
    {
        valueConverters.Add(typeof(Guid), typeof(GuidStringValueConverter));
        valueConverters.Add(typeof(UnturnedAssetReference), typeof(UnturnedAssetReferenceValueConverter));
        valueConverters.Add(typeof(HWID), typeof(HWIDValueConverter));
        valueConverters.Add(typeof(IPAddress), typeof(IPAddressValueConverter));
        valueConverters.Add(typeof(IPv4Range), typeof(IPv4RangeValueConverter));
        valueConverters.Add(typeof(DateTimeOffset), typeof(DateTimeOffsetValueConverter));
    }

    public static void ApplyValueConverterConfig(ModelBuilder modelBuilder, Action<Dictionary<Type, Type>>? modValueConverters = null)
    {
        Dictionary<Type, Type> valueConverters = new Dictionary<Type, Type>(16);
        AddValueConverters(valueConverters);
        modValueConverters?.Invoke(valueConverters);

        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes().OrderBy(x => EFCompat.GetClrType(x).FullName).ToList())
        {
            Type entityClrType = EFCompat.GetClrType(entity);
            PropertyInfo[] properties = entityClrType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            if (valueConverters.ContainsKey(entityClrType) || entityClrType.IsDefinedSafe<ValueConverterAttribute>())
            {
                if (EFCompat.RemoveEntityType(modelBuilder.Model, entityClrType) != null)
                    Log($"Removed entity type {entityClrType.Name}.");

                continue;
            }

            foreach (PropertyInfo property in properties)
            {
                if (entity.GetProperties().Any(x => EFCompat.GetPropertyInfo(x) == property) || property.IsDefinedSafe<NotMappedAttribute>())
                    continue;

                Type clrType = property.PropertyType;
                if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    clrType = clrType.GenericTypeArguments[0];

                ValueConverterAttribute? propertyAttribute = property.GetAttributeSafe<ValueConverterAttribute>();
                ValueConverterAttribute? typeAttribute = clrType.GetAttributeSafe<ValueConverterAttribute>();
                Type? type = propertyAttribute?.Type ?? typeAttribute?.Type;
                if (type == null && !valueConverters.TryGetValue(clrType, out type))
                    continue;

                EFCompat.AddProperty(entity, property);
                Log($"Added field {entityClrType.Name + "." + property.Name,-66} that was excluded.");
                if (propertyAttribute?.Type == null && EFCompat.RemoveEntityType(modelBuilder.Model, clrType) != null)
                    Log($"Removed entity type {entityClrType.Name}.");
            }

            FieldInfo[] fields = entityClrType.GetFields(BindingFlags.Instance | BindingFlags.Public);
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

                EFCompat.AddProperty(entity, field);
                Log($"Added field {entityClrType.Name + "." + field.Name,-66} that was excluded.");
                if (propertyAttribute?.Type == null && EFCompat.RemoveEntityType(modelBuilder.Model, clrType) != null)
                    Log($"Removed entity type {entityClrType.Name}.");
            }
        }
#pragma warning disable EF1001
        foreach (IMutableProperty property in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetProperties()).OrderBy(x => EFCompat.GetClrType(x.DeclaringEntityType).FullName).ToList())
#pragma warning restore EF1001
        {
            bool nullable = false;
            Type clrType = EFCompat.GetClrType(property);
            if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                nullable = true;
                clrType = clrType.GenericTypeArguments[0];
            }

            // Log($"Checking property {EFCompat.GetClrType(property.DeclaringEntityType).Name + "." + EFCompat.GetName(property),-60} of type {clrType.Name,-30}.");

            MemberInfo? member = (MemberInfo?)EFCompat.GetPropertyInfo(property) ?? property.FieldInfo;

            ValueConverterAttribute? propertyAttribute = member?.GetAttributeSafe<ValueConverterAttribute>();
            ValueConverterAttribute? typeAttribute = clrType.GetAttributeSafe<ValueConverterAttribute>();

            if (clrType == typeof(IPAddress) && (member == null || !member.IsDefinedSafe<DontAddPackedColumnAttribute>()))
            {
                // add packed column for IP addresses
                string name = EFCompat.GetName(property) + "Packed";
                if (property.DeclaringEntityType.GetProperties().Any(x => EFCompat.GetName(x).Equals(name, StringComparison.Ordinal)))
                {
                    EFCompat.AddProperty(property.DeclaringEntityType, name, typeof(uint));
                    Log($"Added packed IP column for {EFCompat.GetClrType(property.DeclaringEntityType).Name}.{member?.Name ?? "null"}: {name}.");
                }
            }

            if (member != null && member.IsDefinedSafe<IndexAttribute>())
            {
                EFCompat.AddIndex(property.DeclaringEntityType, property);
                Log($"Added generic index for {EFCompat.GetClrType(property.DeclaringEntityType).Name}.{member?.Name ?? "null"}.");
            }

            if (member != null && member.TryGetAttributeSafe(out AddNameAttribute addNameAttribute))
            {
                string originalName = EFCompat.GetName(property);
                string name = addNameAttribute.ColumnName ?? (originalName + "Name");
                if (!property.DeclaringEntityType.GetProperties().Any(x => EFCompat.GetName(x).Equals(name, StringComparison.Ordinal)))
                {
                    IMutableProperty assetNameProperty = EFCompat.AddProperty(property.DeclaringEntityType, name, typeof(string));
                    EFCompat.SetMaxLength(assetNameProperty, MaxAssetNameLength);
                    assetNameProperty.IsNullable = nullable;
                    assetNameProperty.SetDefaultValue(nullable ? null : new string('0', 32));

                    EFCompat.SetValueGeneratorFactory(assetNameProperty,
                        (_, entityType) => AssetNameValueGenerator.Get(entityType, originalName));

                    Log($"Added asset name column for {EFCompat.GetClrType(property.DeclaringEntityType).Name}.{member?.Name ?? "null"}: {name} (max length: {MaxAssetNameLength})");
                }
                else
                {
                    Log($"Asset name column already exists in {EFCompat.GetClrType(property.DeclaringEntityType).Name}.{member?.Name ?? "null"}: {name}");
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
                            .Invoke(null, values)!;
                    }
                    else if (values.Length == 1)
                    {
                        MethodInfo sqlEnumMethod = typeof(SqlTypes)
                            .GetMethods()
                            .Single(x => x.Name.Equals(nameof(SqlTypes.Enum), StringComparison.Ordinal)
                                         && x.GetParameters().Length == 1 && !typeof(IEnumerable<>).MakeGenericType(x.GetGenericArguments()[0]).IsAssignableFrom(x.GetParameters()[0].ParameterType));

                        dataType = (string)sqlEnumMethod
                            .MakeGenericMethod(clrType)
                            .Invoke(null, values)!;
                    }
                    else if (values.Length == 2)
                    {
                        MethodInfo sqlEnumMethod = typeof(SqlTypes)
                            .GetMethods()
                            .Single(x => x.Name.Equals(nameof(SqlTypes.Enum), StringComparison.Ordinal)
                                         && x.GetParameters().Length == 2);

                        dataType = (string)sqlEnumMethod
                            .MakeGenericMethod(clrType)
                            .Invoke(null, values)!;
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
                            .Invoke(null, [ array ])!;
                    }
                }
                else
                {
                    MethodInfo sqlEnumMethod = typeof(SqlTypes)
                        .GetMethods()
                        .Single(x => x.Name.Equals(nameof(SqlTypes.Enum), StringComparison.Ordinal) && x.GetParameters().Length == 0);

                    dataType = (string)sqlEnumMethod
                        .MakeGenericMethod(clrType)
                        .Invoke(null, Array.Empty<object>())!;
                }
                
                ValueConverter converter = CreateValueConverter(ref converterType, clrType, nullable);

                EFCompat.SetValueConverter(property, converter);
                property.SetColumnType(dataType);
                property.IsNullable = nullable;

                Log($"Set converter for {EFCompat.GetName(property.DeclaringEntityType)}.{EFCompat.GetName(property)} to {converterType.Name}.");
                Log($" - Type: {dataType}.");
                continue;
            }

            Type? valConverterType = null;
            if (typeAttribute == null && propertyAttribute == null)
            {
                valueConverters.TryGetValue(clrType, out valConverterType);
            }
            else if (propertyAttribute is not { Type: null })
            {
                valConverterType = propertyAttribute?.Type ?? typeAttribute?.Type;
            }
            else if (typeAttribute is not { Type: null })
            {
                valConverterType = typeAttribute?.Type ?? propertyAttribute.Type;
            }

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
                    EFCompat.SetValueConverter(property, (ValueConverter)Activator.CreateInstance(valConverterType)!);

                    Log($"Set converter for {EFCompat.GetName(property.DeclaringEntityType)}.{EFCompat.GetName(property)} to {valConverterType.Name}.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create value converter of type {valConverterType.Name}.", ex);
                }
            }
            else
            {
                MethodInfo? method = valConverterType.GetMethod(callback.MethodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, [ typeof(ModelBuilder), typeof(IMutableProperty), typeof(bool) ], null);
                if (method == null)
                    throw new MethodAccessException($"Failed to find value converter callback: {valConverterType.Name}.{callback.MethodName}.");

                try
                {
                    method.Invoke(null, [ modelBuilder, property, nullable ]);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception invoking value converter callback for {valConverterType.Name}.", ex);
                }

                ValueConverter? vc = EFCompat.GetValueConverter(property);
                if (valConverterType.IsInstanceOfType(vc) ||
                    nullable && valConverterType.IsGenericType && (valConverterType.GetGenericTypeDefinition() == typeof(NullableReferenceTypeConverter<,>) || valConverterType.GetGenericTypeDefinition() == typeof(NullableValueTypeConverter<,>)))
                {
                    Log($"Set converter for {EFCompat.GetName(property.DeclaringEntityType)}.{EFCompat.GetName(property)} to {vc!.GetType().Name}.");
                    Log($" - Type: {property.GetColumnType()}.");
                    if (clrType.IsValueType)
                        property.IsNullable = nullable;

                    continue;
                }

                try
                {
                    ValueConverter converter = CreateValueConverter(ref valConverterType, clrType, nullable);
                    EFCompat.SetValueConverter(property, converter);

                    Log($"Set converter for {EFCompat.GetName(property.DeclaringEntityType)}.{EFCompat.GetName(property)} to {valConverterType.Name}.");
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
                    converter = (ValueConverter)Activator.CreateInstance(valConverterType, new object?[] { null })!;
                else
                    converter = (ValueConverter)Activator.CreateInstance(valConverterType)!;
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
                    valConverterType = (secondaryType.IsValueType ? typeof(NullableValueTypeConverter<,>) : typeof(NullableReferenceTypeConverter<,>))
                        .MakeGenericType(clrType, secondaryType);
                    try
                    {
                        converter = (ValueConverter)Activator.CreateInstance(valConverterType, new object?[] { converter })!;
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
}
