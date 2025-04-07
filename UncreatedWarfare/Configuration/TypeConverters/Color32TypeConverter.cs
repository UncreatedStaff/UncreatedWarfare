using System;
using System.ComponentModel;
using System.Globalization;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Configuration.TypeConverters;

public class Color32TypeConverter : TypeConverter
{
    public static void Setup()
    {
        TypeDescriptor.AddAttributes(typeof(Color32), new TypeConverterAttribute(typeof(Color32TypeConverter)));
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string) || destinationType == typeof(Color);
    }

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || sourceType == typeof(Color);
    }

    public override bool IsValid(ITypeDescriptorContext context, object value)
    {
        if (value is string str)
            return HexStringHelper.TryParseColor32(str, CultureInfo.InvariantCulture, out _);

        return false;
    }

    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        Color32 val = (Color32)value;

        if (destinationType != typeof(string))
        {
            if (destinationType == typeof(Color))
                return (Color)val;
            return base.ConvertTo(context, culture, value, destinationType);
        }

        return HexStringHelper.FormatHexColor(val);
    }

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is not string str)
        {
            if (value is Color clr)
                return (Color32)clr;
            return base.ConvertFrom(context, culture, value);
        }

        if (!HexStringHelper.TryParseColor32(str, CultureInfo.InvariantCulture, out Color32 color))
        {
            throw GetConvertFromException(str);
        }

        return color;
    }
}