using System;
using System.ComponentModel;
using System.Globalization;

namespace Uncreated.Warfare.NewQuests.Parameters;

public class StringParameterTemplateTypeConverter : TypeConverter
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
            throw GetConvertFromException(value);
        }

        return context?.PropertyDescriptor?.PropertyType == typeof(KitNameParameterTemplate)
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
        return sourceType == typeof(string);
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
        return sourceType == typeof(string);
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
        return sourceType == typeof(string);
    }
}

public class EnumParameterTemplateTypeConverter : TypeConverter
{
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

        Type enumType = context?.PropertyDescriptor?.PropertyType ?? throw GetConvertFromException(value);

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

        Type assetType = context?.PropertyDescriptor?.PropertyType ?? typeof(Asset);

        return assetType == typeof(Asset)
            ? new AssetParameterTemplate<Asset>(str)
            : Activator.CreateInstance(typeof(AssetParameterTemplate<>).MakeGenericType(assetType), [ str ]);
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