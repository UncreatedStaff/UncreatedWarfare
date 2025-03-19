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
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Database.ValueConverters;
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

    public static void ApplyValueConverterConfig(ModelBuilder modelBuilder, ILogger logger, Action<Dictionary<Type, Type>>? modValueConverters = null)
    {
        Dictionary<Type, Type> valueConverters = new Dictionary<Type, Type>(16);
        AddValueConverters(valueConverters);
        modValueConverters?.Invoke(valueConverters);

        IEFCompatProvider efCompat = EFCompat.Instance;
        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes().OrderBy(x => efCompat.GetClrType(x).FullName).ToList())
        {
            Type entityClrType = efCompat.GetClrType(entity);
            PropertyInfo[] properties = entityClrType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            if (valueConverters.ContainsKey(entityClrType) || entityClrType.IsDefinedSafe<ValueConverterAttribute>())
            {
                if (efCompat.RemoveEntityType(modelBuilder.Model, entityClrType) != null)
                    logger.LogDebug("Removed entity type {0}.", Accessor.Formatter.Format(entityClrType));

                continue;
            }

            foreach (PropertyInfo property in properties)
            {
                if (efCompat.GetProperties(entity).Any(x => efCompat.GetPropertyInfo(x) == property) || property.IsDefinedSafe<NotMappedAttribute>())
                    continue;

                Type clrType = property.PropertyType;
                if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    clrType = clrType.GenericTypeArguments[0];

                ValueConverterAttribute? propertyAttribute = property.GetAttributeSafe<ValueConverterAttribute>();
                ValueConverterAttribute? typeAttribute = clrType.GetAttributeSafe<ValueConverterAttribute>();
                Type? type = propertyAttribute?.Type ?? typeAttribute?.Type;
                if (type == null && !valueConverters.TryGetValue(clrType, out type))
                    continue;

                efCompat.AddProperty(entity, property);
                logger.LogDebug("Added field {0} that was excluded.", Accessor.Formatter.Format(property));
                if (propertyAttribute?.Type == null && efCompat.RemoveEntityType(modelBuilder.Model, clrType) != null)
                    logger.LogDebug("Removed entity type {0}.", Accessor.Formatter.Format(entityClrType));
            }

            FieldInfo[] fields = entityClrType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                if (efCompat.GetProperties(entity).Any(x => x.FieldInfo == field) || field.IsDefinedSafe<NotMappedAttribute>())
                    continue;

                Type clrType = field.FieldType;
                if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    clrType = clrType.GenericTypeArguments[0];

                ValueConverterAttribute? propertyAttribute = field.GetAttributeSafe<ValueConverterAttribute>();
                ValueConverterAttribute? typeAttribute = clrType.GetAttributeSafe<ValueConverterAttribute>();
                Type? type = propertyAttribute?.Type ?? typeAttribute?.Type;
                if (type == null && !valueConverters.TryGetValue(clrType, out type))
                    continue;

                efCompat.AddProperty(entity, field);
                logger.LogDebug("Added field {0} that was excluded.", Accessor.Formatter.Format(field));
                if (propertyAttribute?.Type == null && efCompat.RemoveEntityType(modelBuilder.Model, clrType) != null)
                    logger.LogDebug("Removed entity type {0}.", Accessor.Formatter.Format(entityClrType));
            }
        }
#pragma warning disable EF1001
        foreach (IMutableProperty property in modelBuilder.Model.GetEntityTypes().SelectMany(efCompat.GetProperties).OrderBy(x => efCompat.GetClrType(x.DeclaringEntityType).FullName).ToList())
#pragma warning restore EF1001
        {
            bool nullable = false;
            Type clrType = efCompat.GetClrType(property);
            if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                nullable = true;
                clrType = clrType.GenericTypeArguments[0];
            }

            // logger.LogDebug("Checking property {0}.{1} of type {2}.", Accessor.Formatter.Format(EFCompat.Instance.GetClrType(property.DeclaringEntityType)), EFCompat.Instance.GetName(property), Accessor.Formatter.Format(clrType));

            MemberInfo? member = (MemberInfo?)efCompat.GetPropertyInfo(property) ?? property.FieldInfo;

            ValueConverterAttribute? propertyAttribute = member?.GetAttributeSafe<ValueConverterAttribute>();
            ValueConverterAttribute? typeAttribute = clrType.GetAttributeSafe<ValueConverterAttribute>();

            if (clrType == typeof(IPAddress) && (member == null || !member.IsDefinedSafe<DontAddPackedColumnAttribute>()))
            {
                // add packed column for IP addresses
                string name = efCompat.GetName(property) + "Packed";
                if (efCompat.GetProperties(property.DeclaringEntityType).Any(x => efCompat.GetName(x).Equals(name, StringComparison.Ordinal)))
                {
                    efCompat.AddProperty(property.DeclaringEntityType, name, typeof(uint));
                    logger.LogDebug("Added packed IP column for {0}.{1}: {2}.", Accessor.Formatter.Format(efCompat.GetClrType(property.DeclaringEntityType)), member?.Name ?? "null", name);
                }
            }

            if (member != null && member.IsDefinedSafe<IndexAttribute>())
            {
                efCompat.AddIndex(property.DeclaringEntityType, property);
                logger.LogDebug("Added generic index for {0}.{1}.", Accessor.Formatter.Format(efCompat.GetClrType(property.DeclaringEntityType)), member?.Name ?? "null");
            }

            // enum types
            if (clrType.IsEnum && propertyAttribute == null)
            {
                Type converterType = typeof(EnumToStringConverter<>).MakeGenericType(clrType);
                string dataType;
                if (member != null)
                {
                    List<ExcludedEnumAttribute> excludedList = Attribute
                        .GetCustomAttributes(member, typeof(ExcludedEnumAttribute))
                        .Concat(Attribute.GetCustomAttributes(clrType, typeof(ExcludedEnumAttribute)))
                        .Cast<ExcludedEnumAttribute>()
                        .ToList();

                    List<IncludedEnumAttribute> includedList = Attribute
                        .GetCustomAttributes(member, typeof(IncludedEnumAttribute))
                        .Concat(Attribute.GetCustomAttributes(clrType, typeof(IncludedEnumAttribute)))
                        .Cast<IncludedEnumAttribute>()
                        .ToList();

                    excludedList.RemoveAll(x => !clrType.IsInstanceOfType(x.Value));

                    object[] excludedValues = excludedList.Select(x => x.Value!).ToArray();
                    if (includedList.Count > 0)
                    {
                        ArrayList list = new ArrayList();
                        for (int i = 0; i < excludedValues.Length; ++i)
                        {
                            object value1 = excludedValues[i];
                            list.Add(Convert.ChangeType(value1, clrType));
                        }

                        Array excludeArray = list.ToArray(clrType);
                        list.Clear();
                        for (int i = 0; i < includedList.Count; ++i)
                        {
                            object value1 = includedList[i].Value!;
                            list.Add(Convert.ChangeType(value1, clrType));
                        }
                        Array includeArray = list.ToArray(clrType);

                        MethodInfo sqlEnumMethod = typeof(MySqlSnippets)
                            .GetMethods()
                            .Single(x => x.Name.Equals(nameof(MySqlSnippets.EnumList), StringComparison.Ordinal)
                                         && x.GetParameters().Length == 2 && x.GetParameters()[1].ParameterType.IsArray);

                        dataType = (string)sqlEnumMethod
                            .MakeGenericMethod(clrType)
                            .Invoke(null, [ excludeArray, includeArray ])!;
                    }
                    else if (excludedValues.Length == 0)
                    {
                        MethodInfo sqlEnumMethod = typeof(MySqlSnippets)
                            .GetMethods()
                            .Single(x => x.Name.Equals(nameof(MySqlSnippets.EnumList), StringComparison.Ordinal)
                                         && x.GetParameters().Length == 0);

                        dataType = (string)sqlEnumMethod
                            .MakeGenericMethod(clrType)
                            .Invoke(null, excludedValues)!;
                    }
                    else if (excludedValues.Length == 1)
                    {
                        MethodInfo sqlEnumMethod = typeof(MySqlSnippets)
                            .GetMethods()
                            .Single(x => x.Name.Equals(nameof(MySqlSnippets.EnumList), StringComparison.Ordinal)
                                         && x.GetParameters().Length == 1 && !typeof(IEnumerable<>).MakeGenericType(x.GetGenericArguments()[0]).IsAssignableFrom(x.GetParameters()[0].ParameterType));

                        dataType = (string)sqlEnumMethod
                            .MakeGenericMethod(clrType)
                            .Invoke(null, excludedValues)!;
                    }
                    else if (excludedValues.Length == 2)
                    {
                        MethodInfo sqlEnumMethod = typeof(MySqlSnippets)
                            .GetMethods()
                            .Single(x => x.Name.Equals(nameof(MySqlSnippets.EnumList), StringComparison.Ordinal)
                                         && x.GetParameters().Length == 2 && !x.GetParameters()[1].ParameterType.IsArray);

                        dataType = (string)sqlEnumMethod
                            .MakeGenericMethod(clrType)
                            .Invoke(null, excludedValues)!;
                    }
                    else
                    {
                        ArrayList list = new ArrayList();
                        for (int i = 0; i < excludedValues.Length; ++i)
                        {
                            object value1 = excludedValues[i];
                            list.Add(Convert.ChangeType(value1, clrType));
                        }

                        Array array = list.ToArray(clrType);

                        MethodInfo sqlEnumMethod = typeof(MySqlSnippets)
                            .GetMethods()
                            .Single(x => x.Name.Equals(nameof(MySqlSnippets.EnumList), StringComparison.Ordinal)
                                         && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == x.GetGenericArguments()[0].MakeArrayType());

                        dataType = (string)sqlEnumMethod
                            .MakeGenericMethod(clrType)
                            .Invoke(null, [ array ])!;
                    }
                }
                else
                {
                    MethodInfo sqlEnumMethod = typeof(MySqlSnippets)
                        .GetMethods()
                        .Single(x => x.Name.Equals(nameof(MySqlSnippets.EnumList), StringComparison.Ordinal) && x.GetParameters().Length == 0);

                    dataType = (string)sqlEnumMethod
                        .MakeGenericMethod(clrType)
                        .Invoke(null, Array.Empty<object>())!;
                }
                
                ValueConverter converter = CreateValueConverter(ref converterType, clrType, logger, nullable);

                efCompat.SetValueConverter(property, converter);
                efCompat.SetColumnType(property, dataType);
                efCompat.SetIsNullable(property, nullable);

                logger.LogDebug("Set converter for {0}.{1} to {2}.", efCompat.GetName(property.DeclaringEntityType), efCompat.GetName(property), converterType.Name);
                logger.LogDebug(" - Type: {0}.", dataType);
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
                throw new InvalidOperationException($"Can not use generic type definition: {Accessor.ExceptionFormatter.Format(valConverterType)} as a converter.");

            if (!typeof(ValueConverter).IsAssignableFrom(valConverterType))
                throw new InvalidOperationException($"Can not use type: {Accessor.ExceptionFormatter.Format(valConverterType)} as a converter as it doesn't inherit {Accessor.ExceptionFormatter.Format<ValueConverter>()}.");

            if (callback == null || string.IsNullOrEmpty(callback.MethodName))
            {
                try
                {
                    efCompat.SetValueConverter(property, (ValueConverter)Activator.CreateInstance(valConverterType)!);

                    logger.LogDebug("Set converter for {0}.{1} to {2}.", efCompat.GetName(property.DeclaringEntityType), efCompat.GetName(property), Accessor.Formatter.Format(valConverterType));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create value converter of type {Accessor.ExceptionFormatter.Format(valConverterType)}.", ex);
                }
            }
            else
            {
                MethodInfo? method = valConverterType.GetMethod(callback.MethodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, [ typeof(ModelBuilder), typeof(IMutableProperty), typeof(bool) ], null);
                if (method == null)
                    throw new MethodAccessException($"Failed to find value converter callback: {Accessor.ExceptionFormatter.Format(valConverterType)}.{callback.MethodName}.");

                try
                {
                    method.Invoke(null, [ modelBuilder, property, nullable ]);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception invoking value converter callback for {Accessor.ExceptionFormatter.Format(valConverterType)}.", ex);
                }

                ValueConverter? vc = efCompat.GetValueConverter(property);
                if (valConverterType.IsInstanceOfType(vc) ||
                    nullable && valConverterType.IsGenericType && (valConverterType.GetGenericTypeDefinition() == typeof(NullableReferenceTypeConverter<,>) || valConverterType.GetGenericTypeDefinition() == typeof(NullableValueTypeConverter<,>)))
                {
                    logger.LogDebug("Set converter for {0}.{1} to {2}.", efCompat.GetName(efCompat.GetDeclaringEntityType(property)), efCompat.GetName(property), Accessor.Formatter.Format(vc!.GetType()));
                    logger.LogDebug(" - Type: {0}.", efCompat.GetColumnType(property));
                    if (clrType.IsValueType)
                        efCompat.SetIsNullable(property, nullable);

                    continue;
                }

                try
                {
                    ValueConverter converter = CreateValueConverter(ref valConverterType, clrType, logger, nullable);
                    efCompat.SetValueConverter(property, converter);

                    logger.LogDebug("Set converter for {0}.{1} to {2}.", efCompat.GetName(efCompat.GetDeclaringEntityType(property)), efCompat.GetName(property), Accessor.Formatter.Format(valConverterType));
                    logger.LogDebug(" - Type: {0}.", efCompat.GetColumnType(property));
                    if (clrType.IsValueType)
                        efCompat.SetIsNullable(property, nullable);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create value converter of type {Accessor.ExceptionFormatter.Format(valConverterType)}.", ex);
                }
            }
        }

        static ValueConverter CreateValueConverter(ref Type valConverterType, Type clrType, ILogger logger, bool nullable)
        {
            ValueConverter converter;
            try
            {
                if (valConverterType.GetConstructor(Array.Empty<Type>()) == null && valConverterType.GetConstructor([ typeof(ConverterMappingHints) ]) != null)
                    converter = (ValueConverter)Activator.CreateInstance(valConverterType, [ null ])!;
                else
                    converter = (ValueConverter)Activator.CreateInstance(valConverterType)!;
            }
            catch
            {
                logger.LogDebug("Failed to create value converter of type {0}.", Accessor.Formatter.Format(valConverterType));
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
                        converter = (ValueConverter)Activator.CreateInstance(valConverterType, [ converter ])!;
                    }
                    catch
                    {
                        logger.LogDebug("Failed to create nullable value converter of type {0}.", Accessor.Formatter.Format(valConverterType));
                        throw;
                    }
                }
            }

            return converter;
        }

        logger.LogDebug("Done");
    }
}
