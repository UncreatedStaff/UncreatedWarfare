using System;
using System.ComponentModel;
using System.Globalization;

namespace Uncreated.Warfare.Quests.Parameters;

public class StringParameterTemplateTypeConverter : TypeConverter
{
    private readonly Type _type;

    public StringParameterTemplateTypeConverter(Type type)
    {
        _type = type;
    }

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        return destinationType == typeof(string)
            ? value.ToString()
            : throw GetConvertToException(value, destinationType);
    }

    /// <inheritdoc />
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is not string str)
        {
            if (value is not IConvertible c)
            {
                throw GetConvertFromException(value);
            }

            str = c.ToString(culture);
        }

        return _type == typeof(KitNameParameterTemplate)
            ? new KitNameParameterTemplate(str.AsSpan())
            : new StringParameterTemplate(str.AsSpan());
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string);
    }

    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || typeof(IConvertible).IsAssignableFrom(sourceType);
    }
}

public class SingleParameterTemplateTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        return destinationType == typeof(string)
            ? value.ToString()
            : throw GetConvertToException(value, destinationType);
    }

    /// <inheritdoc />
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is not string str)
        {
            if (value is IConvertible c)
            {
                float single = c.ToSingle(culture);
                return new SingleParameterTemplate(single);
            }

            throw GetConvertFromException(value);
        }

        return new SingleParameterTemplate(str.AsSpan());
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string);
    }

    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || typeof(IConvertible).IsAssignableFrom(sourceType);
    }
}

public class Int32ParameterTemplateTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        return destinationType == typeof(string)
            ? value.ToString()
            : throw GetConvertToException(value, destinationType);
    }

    /// <inheritdoc />
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is not string str)
        {
            if (value is IConvertible convertible)
                return new Int32ParameterTemplate(convertible.ToInt32(culture));

            throw GetConvertFromException(value);
        }

        return new Int32ParameterTemplate(str.AsSpan());
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string);
    }

    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || typeof(IConvertible).IsAssignableFrom(sourceType);
    }
}

public class EnumParameterTemplateTypeConverter : TypeConverter
{
    private readonly Type _type;

    public EnumParameterTemplateTypeConverter(Type type)
    {
        _type = type;
    }

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType != typeof(string))
            throw GetConvertToException(value, destinationType);
        
        return value.ToString();
    }

    /// <inheritdoc />
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is not string str)
        {
            throw GetConvertFromException(value);
        }

        Type enumType = _type.GetGenericArguments()[0];

        return Activator.CreateInstance(typeof(EnumParameterTemplate<>).MakeGenericType(enumType), [ str ]);
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string);
    }

    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string);
    }
}

public class AssetParameterTemplateTypeConverter : TypeConverter
{
    private readonly Type _type;

    public AssetParameterTemplateTypeConverter(Type type)
    {
        _type = type;
    }

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        return destinationType == typeof(string)
            ? value.ToString()
            : throw GetConvertToException(value, destinationType);
    }

    /// <inheritdoc />
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is not string str)
        {
            throw GetConvertFromException(value);
        }

        Type assetType = _type.GetGenericArguments()[0];
        
        return Activator.CreateInstance(typeof(AssetParameterTemplate<>).MakeGenericType(assetType), [ str ]);
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string);
    }

    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string);
    }
}