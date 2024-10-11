using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Database.Automation;

namespace Uncreated.Warfare.Database;

/// <summary>
/// Abstracts some properties used in <see cref="WarfareDatabaseReflection"/> to support later versions of EF.
/// </summary>
internal static class EFCompat
{
    private static Func<ITypeBase, Type>? _getClrType;
    private static Func<ITypeBase, string>? _getTypeName;
    private static Func<IMutableProperty, Type>? _getPropClrType;
    private static Func<IMutableProperty, PropertyInfo?>? _getPropertyInfo;
    private static Func<IPropertyBase, string>? _getPropertyName;
    private static Func<IProperty, ValueConverter?>? _getValueConverter;
    private static Func<IPropertyBase, bool, bool, MemberInfo>? _getMemberInfo;
    private static Func<IMutableEntityType, IMutableProperty, IMutableIndex>? _addIndex;
    private static Func<IMutableEntityType, MemberInfo, IMutableProperty>? _addProperty;
    private static Func<IMutableEntityType, string, Type, IMutableProperty>? _addPropertyShadow;
    private static Func<IMutableModel, Type, IMutableEntityType?>? _removeEntityType;
    private static Action<IMutableProperty, int?>? _setMaxLength;
    private static Action<IMutableProperty, ValueConverter?>? _setValueConverter;
    private static Action<IMutableProperty, Func<IProperty, IEntityType, ValueGenerator>?>? _setValueGeneratorFactory;
    private static Func<IMutableEntityType, IEnumerable<IMutableProperty>>? _getProperties;
    public static Type GetClrType(ITypeBase type)
    {
        if (_getClrType != null)
            return _getClrType(type);

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("get_ClrType", attributes, conventions, typeof(Type), [ typeof(ITypeBase) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "type");
        ILGenerator il = method.GetILGenerator();

        Type? roType = Type.GetType("Microsoft.EntityFrameworkCore.Metadata.IReadOnlyTypeBase, Microsoft.EntityFrameworkCore");
        PropertyInfo? property = (roType ?? typeof(ITypeBase)).GetProperty(nameof(ITypeBase.ClrType), BindingFlags.Instance | BindingFlags.Public);

        if (property == null)
            throw new InvalidProgramException("Type CLR type property not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, property.GetMethod);
        il.Emit(OpCodes.Ret);
        _getClrType = (Func<ITypeBase, Type>)method.CreateDelegate(typeof(Func<ITypeBase, Type>));
        return _getClrType(type);
    }
    public static Type GetClrType(IMutableProperty prop)
    {
        if (_getPropClrType != null)
            return _getPropClrType(prop);

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("get_ClrType", attributes, conventions, typeof(Type), [ typeof(IMutableProperty) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "property");
        ILGenerator il = method.GetILGenerator();

        Type? roType = Type.GetType("Microsoft.EntityFrameworkCore.Metadata.IReadOnlyPropertyBase, Microsoft.EntityFrameworkCore");
        PropertyInfo? property = (roType ?? typeof(IPropertyBase)).GetProperty(nameof(IPropertyBase.ClrType), BindingFlags.Instance | BindingFlags.Public);

        if (property == null)
            throw new InvalidProgramException("Property CLR type property not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, property.GetMethod);
        il.Emit(OpCodes.Ret);
        _getPropClrType = (Func<IMutableProperty, Type>)method.CreateDelegate(typeof(Func<IMutableProperty, Type>));
        return _getPropClrType(prop);
    }
    public static PropertyInfo? GetPropertyInfo(IMutableProperty prop)
    {
        if (_getPropertyInfo != null)
            return _getPropertyInfo(prop);

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("get_PropertyInfo", attributes, conventions, typeof(PropertyInfo), [ typeof(IMutableProperty) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "property");
        ILGenerator il = method.GetILGenerator();

        Type? roType = Type.GetType("Microsoft.EntityFrameworkCore.Metadata.IReadOnlyPropertyBase, Microsoft.EntityFrameworkCore");
        PropertyInfo? property = (roType ?? typeof(IPropertyBase)).GetProperty(nameof(IPropertyBase.PropertyInfo), BindingFlags.Instance | BindingFlags.Public);

        if (property == null)
            throw new InvalidProgramException("Property PropertyInfo property not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, property.GetMethod);
        il.Emit(OpCodes.Ret);
        _getPropertyInfo = (Func<IMutableProperty, PropertyInfo>)method.CreateDelegate(typeof(Func<IMutableProperty, PropertyInfo>));
        return _getPropertyInfo(prop);
    }
    public static string GetName(IPropertyBase prop)
    {
        if (_getPropertyName != null)
            return _getPropertyName(prop);

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("get_Name", attributes, conventions, typeof(string), [ typeof(IPropertyBase) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "property");
        ILGenerator il = method.GetILGenerator();

        Type? roType = Type.GetType("Microsoft.EntityFrameworkCore.Metadata.IReadOnlyPropertyBase, Microsoft.EntityFrameworkCore");
        PropertyInfo? property = (roType ?? typeof(IPropertyBase)).GetProperty(nameof(IPropertyBase.Name), BindingFlags.Instance | BindingFlags.Public);

        if (property == null)
            throw new InvalidProgramException("Property Name property not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, property.GetMethod);
        il.Emit(OpCodes.Ret);
        _getPropertyName = (Func<IPropertyBase, string>)method.CreateDelegate(typeof(Func<IPropertyBase, string>));
        return _getPropertyName(prop);
    }
    public static ValueConverter? GetValueConverter(IProperty prop)
    {
        if (_getValueConverter != null)
            return _getValueConverter(prop);

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("get_ValueConverter", attributes, conventions, typeof(ValueConverter), [ typeof(IProperty) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "property");
        ILGenerator il = method.GetILGenerator();

        Type? roType = Type.GetType("Microsoft.EntityFrameworkCore.Metadata.IReadOnlyProperty, Microsoft.EntityFrameworkCore");
        MethodInfo? getValueConverter = (roType ?? Type.GetType("Microsoft.EntityFrameworkCore.PropertyExtensions, Microsoft.EntityFrameworkCore"))?
            .GetMethod(nameof(PropertyExtensions.GetValueConverter), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);

        if (getValueConverter == null)
            throw new InvalidProgramException("Property GetValueConverter method not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(getValueConverter.IsStatic ? OpCodes.Call : OpCodes.Callvirt, getValueConverter);
        il.Emit(OpCodes.Ret);
        _getValueConverter = (Func<IProperty, ValueConverter?>)method.CreateDelegate(typeof(Func<IProperty, ValueConverter?>));
        return _getValueConverter(prop);
    }
    public static string GetName(ITypeBase type)
    {
        if (_getTypeName != null)
            return _getTypeName(type);

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("get_Name", attributes, conventions, typeof(string), [ typeof(ITypeBase) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "property");
        ILGenerator il = method.GetILGenerator();

        Type? roType = Type.GetType("Microsoft.EntityFrameworkCore.Metadata.IReadOnlyTypeBase, Microsoft.EntityFrameworkCore");
        PropertyInfo? property = (roType ?? typeof(ITypeBase)).GetProperty(nameof(ITypeBase.Name), BindingFlags.Instance | BindingFlags.Public);

        if (property == null)
            throw new InvalidProgramException("Type Name property not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, property.GetMethod);
        il.Emit(OpCodes.Ret);
        _getTypeName = (Func<ITypeBase, string>)method.CreateDelegate(typeof(Func<ITypeBase, string>));
        return _getTypeName(type);
    }
    public static MemberInfo GetMemberInfo(IPropertyBase prop, bool forMaterialization, bool forSet)
    {
        if (_getMemberInfo != null)
            return _getMemberInfo(prop, forMaterialization, forSet);

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("GetMemberInfo", attributes, conventions, typeof(MemberInfo), [typeof(IPropertyBase), typeof(bool), typeof(bool)], typeof(EFCompat), true);
        method.DefineParameter(1, default, "type");
        method.DefineParameter(2, default, "property");
        ILGenerator il = method.GetILGenerator();

        MethodInfo? getMemberInfo = Type.GetType("Microsoft.EntityFrameworkCore.PropertyBaseExtensions, Microsoft.EntityFrameworkCore")?
            .GetMethod("GetMemberInfo", BindingFlags.Public | BindingFlags.Static, null, [typeof(IPropertyBase), typeof(bool), typeof(bool)], null);
        getMemberInfo ??= typeof(IPropertyBase).GetMethod("GetMemberInfo", BindingFlags.Public | BindingFlags.Instance, null, [typeof(bool), typeof(bool)], null);

        if (getMemberInfo == null)
            throw new InvalidProgramException("Property GetMemberInfo method not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);

        il.Emit(getMemberInfo.IsStatic ? OpCodes.Call : OpCodes.Callvirt, getMemberInfo);

        il.Emit(OpCodes.Ret);
        _getMemberInfo = (Func<IPropertyBase, bool, bool, MemberInfo>)method.CreateDelegate(typeof(Func<IPropertyBase, bool, bool, MemberInfo>));
        return _getMemberInfo(prop, forMaterialization, forSet);
    }
    public static IMutableEntityType? RemoveEntityType(IMutableModel model, Type type)
    {
        if (_removeEntityType != null)
            return _removeEntityType(model, type);

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("RemoveEntityType", attributes, conventions, typeof(IMutableEntityType), [ typeof(IMutableModel), typeof(Type) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "model");
        method.DefineParameter(2, default, "type");
        ILGenerator il = method.GetILGenerator();

        MethodInfo? removeEntityType = Type.GetType("Microsoft.EntityFrameworkCore.MutableModelExtensions, Microsoft.EntityFrameworkCore")?
            .GetMethod("RemoveEntityType", BindingFlags.Public | BindingFlags.Static, null, [ typeof(IMutableModel), typeof(Type) ], null);
        removeEntityType ??= typeof(IMutableModel).GetMethod("RemoveEntityType", BindingFlags.Public | BindingFlags.Instance, null, [ typeof(Type) ], null);

        if (removeEntityType == null)
            throw new InvalidProgramException("Type RemoveEntityType method not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);

        il.Emit(removeEntityType.IsStatic ? OpCodes.Call : OpCodes.Callvirt, removeEntityType);

        il.Emit(OpCodes.Ret);
        _removeEntityType = (Func<IMutableModel, Type, IMutableEntityType?>)method.CreateDelegate(typeof(Func<IMutableModel, Type, IMutableEntityType?>));
        return _removeEntityType(model, type);
    }
    public static IMutableProperty AddProperty(IMutableEntityType type, MemberInfo member)
    {
        if (_addProperty != null)
            return _addProperty(type, member);

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("AddProperty", attributes, conventions, typeof(IMutableProperty), [ typeof(IMutableEntityType), typeof(MemberInfo) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "type");
        ILGenerator il = method.GetILGenerator();

        MethodInfo? addProperty = Type.GetType("Microsoft.EntityFrameworkCore.MutableEntityTypeExtensions, Microsoft.EntityFrameworkCore")?
            .GetMethod("AddProperty", BindingFlags.Public | BindingFlags.Static, null, [typeof(IMutableEntityType), typeof(MemberInfo)], null);
        addProperty ??= typeof(IMutableEntityType).GetMethod("AddProperty", BindingFlags.Public | BindingFlags.Instance, null, [typeof(MemberInfo)], null);

        if (addProperty == null)
            throw new InvalidProgramException("Type AddProperty method not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);

        il.Emit(addProperty.IsStatic ? OpCodes.Call : OpCodes.Callvirt, addProperty);

        il.Emit(OpCodes.Ret);
        _addProperty = (Func<IMutableEntityType, MemberInfo, IMutableProperty>)method.CreateDelegate(typeof(Func<IMutableEntityType, MemberInfo, IMutableProperty>));
        return _addProperty(type, member);
    }
    public static IMutableProperty AddProperty(IMutableEntityType type, string name, Type clrType)
    {
        if (_addPropertyShadow != null)
            return _addPropertyShadow(type, name, clrType);

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("AddProperty", attributes, conventions, typeof(IMutableProperty), [typeof(IMutableEntityType), typeof(string), typeof(Type)], typeof(EFCompat), true);
        method.DefineParameter(1, default, "type");
        method.DefineParameter(2, default, "property");
        ILGenerator il = method.GetILGenerator();

        MethodInfo? addProperty = Type.GetType("Microsoft.EntityFrameworkCore.MutableEntityTypeExtensions, Microsoft.EntityFrameworkCore")?
            .GetMethod("AddProperty", BindingFlags.Public | BindingFlags.Static, null, [typeof(IMutableEntityType), typeof(string), typeof(Type)], null);
        addProperty ??= typeof(IMutableEntityType).GetMethod("AddProperty", BindingFlags.Public | BindingFlags.Instance, null, [typeof(string), typeof(Type)], null);

        if (addProperty == null)
            throw new InvalidProgramException("Type AddProperty shadow method not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);

        il.Emit(addProperty.IsStatic ? OpCodes.Call : OpCodes.Callvirt, addProperty);

        il.Emit(OpCodes.Ret);
        _addPropertyShadow = (Func<IMutableEntityType, string, Type, IMutableProperty>)method.CreateDelegate(typeof(Func<IMutableEntityType, string, Type, IMutableProperty>));
        return _addPropertyShadow(type, name, clrType);
    }
    public static IMutableIndex AddIndex(IMutableEntityType type, IMutableProperty prop)
    {
        if (_addIndex != null)
            return _addIndex(type, prop);

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("AddIndex", attributes, conventions, typeof(IMutableIndex), [ typeof(IMutableEntityType), typeof(IMutableProperty) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "type");
        method.DefineParameter(2, default, "index");
        ILGenerator il = method.GetILGenerator();

        MethodInfo? addIndex = Type.GetType("Microsoft.EntityFrameworkCore.MutableEntityTypeExtensions, Microsoft.EntityFrameworkCore")?
            .GetMethod("AddIndex", BindingFlags.Public | BindingFlags.Static, null, [ typeof(IMutableEntityType), typeof(IMutableProperty) ], null);
        addIndex ??= typeof(IMutableEntityType).GetMethod("AddIndex", BindingFlags.Public | BindingFlags.Instance, null, [ typeof(IMutableProperty) ], null);

        if (addIndex == null)
            throw new InvalidProgramException("Type AddIndex method not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);

        il.Emit(addIndex.IsStatic ? OpCodes.Call : OpCodes.Callvirt, addIndex);

        il.Emit(OpCodes.Ret);
        _addIndex = (Func<IMutableEntityType, IMutableProperty, IMutableIndex>)method.CreateDelegate(typeof(Func<IMutableEntityType, IMutableProperty, IMutableIndex>));
        return _addIndex(type, prop);
    }
    public static void SetValueConverter(IMutableProperty prop, ValueConverter? valueConverter)
    {
        if (_setValueConverter != null)
        {
            _setValueConverter(prop, valueConverter);
            return;
        }

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("SetValueConverter", attributes, conventions, typeof(void), [ typeof(IMutableProperty), typeof(ValueConverter) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "property");
        method.DefineParameter(2, default, "valueConverter");
        ILGenerator il = method.GetILGenerator();

        MethodInfo? setValueConverter = Type.GetType("Microsoft.EntityFrameworkCore.MutablePropertyExtensions, Microsoft.EntityFrameworkCore")?
            .GetMethod("SetValueConverter", BindingFlags.Public | BindingFlags.Static, null, [ typeof(IMutableProperty), typeof(ValueConverter) ], null);
        setValueConverter ??= typeof(IMutableProperty).GetMethod("SetValueConverter", BindingFlags.Public | BindingFlags.Instance, null, [ typeof(ValueConverter) ], null);

        if (setValueConverter == null)
            throw new InvalidProgramException("Property SetValueConverter method not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);

        il.Emit(setValueConverter.IsStatic ? OpCodes.Call : OpCodes.Callvirt, setValueConverter);

        il.Emit(OpCodes.Ret);
        _setValueConverter = (Action<IMutableProperty, ValueConverter?>)method.CreateDelegate(typeof(Action<IMutableProperty, ValueConverter?>));
        _setValueConverter(prop, valueConverter);
    }
    public static void SetMaxLength(IMutableProperty prop, int? maxLength)
    {
        if (_setMaxLength != null)
        {
            _setMaxLength(prop, maxLength);
            return;
        }

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("SetMaxLength", attributes, conventions, typeof(void), [ typeof(IMutableProperty), typeof(int?) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "property");
        method.DefineParameter(2, default, "maxLength");
        ILGenerator il = method.GetILGenerator();

        MethodInfo? setMaxLength = Type.GetType("Microsoft.EntityFrameworkCore.MutablePropertyExtensions, Microsoft.EntityFrameworkCore")?
            .GetMethod("SetMaxLength", BindingFlags.Public | BindingFlags.Static, null, [ typeof(IMutableProperty), typeof(int?) ], null);
        setMaxLength ??= typeof(IMutableProperty).GetMethod("SetMaxLength", BindingFlags.Public | BindingFlags.Instance, null, [ typeof(int?) ], null);

        if (setMaxLength == null)
            throw new InvalidProgramException("Property SetMaxLength method not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);

        il.Emit(setMaxLength.IsStatic ? OpCodes.Call : OpCodes.Callvirt, setMaxLength);

        il.Emit(OpCodes.Ret);
        _setMaxLength = (Action<IMutableProperty, int?>)method.CreateDelegate(typeof(Action<IMutableProperty, int?>));
        _setMaxLength(prop, maxLength);
    }
    public static void SetValueGeneratorFactory(IMutableProperty prop, Func<IProperty, IEntityType, ValueGenerator>? valueGeneratorFactory)
    {
        if (_setValueGeneratorFactory != null)
        {
            _setValueGeneratorFactory(prop, valueGeneratorFactory);
            return;
        }

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("SetValueGeneratorFactory", attributes, conventions, typeof(void), [ typeof(IMutableProperty), typeof(Func<IProperty, IEntityType, ValueGenerator>) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "property");
        method.DefineParameter(2, default, "valueGeneratorFactory");
        ILGenerator il = method.GetILGenerator();

        MethodInfo? setValueGeneratorFactory = Type.GetType("Microsoft.EntityFrameworkCore.MutablePropertyExtensions, Microsoft.EntityFrameworkCore")?
            .GetMethod("SetValueGeneratorFactory", BindingFlags.Public | BindingFlags.Static, null, [ typeof(IMutableProperty), typeof(Func<IProperty, IEntityType, ValueGenerator>) ], null);
        setValueGeneratorFactory ??= typeof(IMutableProperty).GetMethod("SetValueGeneratorFactory", BindingFlags.Public | BindingFlags.Instance, null, [ typeof(Func<IProperty, IEntityType, ValueGenerator>) ], null);

        if (setValueGeneratorFactory == null)
            throw new InvalidProgramException("Property SetValueGeneratorFactory method not found.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);

        il.Emit(setValueGeneratorFactory.IsStatic ? OpCodes.Call : OpCodes.Callvirt, setValueGeneratorFactory);

        il.Emit(OpCodes.Ret);
        _setValueGeneratorFactory = (Action<IMutableProperty, Func<IProperty, IEntityType, ValueGenerator>?>)method.CreateDelegate(typeof(Action<IMutableProperty, Func<IProperty, IEntityType, ValueGenerator>?>));
        _setValueGeneratorFactory(prop, valueGeneratorFactory);
    }

    public static IEnumerable<IMutableProperty> GetProperties(IMutableEntityType entity)
    {
        if (_getProperties != null)
        {
            return _getProperties(entity);
        }

        MethodInfo? getProperties = Type.GetType("Microsoft.EntityFrameworkCore.Metadata.IMutableTypeBase, Microsoft.EntityFrameworkCore")?
            .GetMethod("GetProperties", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, Type.EmptyTypes, null);

        getProperties ??= typeof(IMutableEntityType)
            .GetMethod("GetProperties", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, Type.EmptyTypes, null);

        if (getProperties == null)
            throw new InvalidProgramException("Method GetProperties method not found.");

        Accessor.GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions conventions);
        DynamicMethod method = new DynamicMethod("GetProperties", attributes, conventions, typeof(IEnumerable<IMutableProperty>), [ typeof(IMutableEntityType) ], typeof(EFCompat), true);
        method.DefineParameter(1, default, "this");
        ILGenerator il = method.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, getProperties);
        il.Emit(OpCodes.Ret);
        _getProperties = (Func<IMutableEntityType, IEnumerable<IMutableProperty>>)method.CreateDelegate(typeof(Func<IMutableEntityType, IEnumerable<IMutableProperty>>));

        return _getProperties(entity);
    }
}
