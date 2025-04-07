using System;
using System.ComponentModel;
using System.Globalization;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Configuration.TypeConverters;

public class ColorTypeConverter : TypeConverter
{
    public static void Setup()
    {
        TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(ColorTypeConverter)));
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string) || destinationType == typeof(Color32);
    }

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || sourceType == typeof(Color32);
    }

    public override bool IsValid(ITypeDescriptorContext context, object value)
    {
        if (value is string str)
            return HexStringHelper.TryParseColor(str, CultureInfo.InvariantCulture, out _);

        return false;
    }

    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        Color val = (Color)value;

        if (destinationType != typeof(string))
        {
            if (destinationType == typeof(Color32))
                return (Color32)val;
            return base.ConvertTo(context, culture, value, destinationType);
        }

        return HexStringHelper.FormatHexColor(val);
    }

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is not string str)
        {
            if (value is Color32 clr)
                return (Color)clr;
            return base.ConvertFrom(context, culture, value);
        }

        if (!HexStringHelper.TryParseColor(str, CultureInfo.InvariantCulture, out Color color))
        {
            throw GetConvertFromException(str);
        }

        return color;
    }
}