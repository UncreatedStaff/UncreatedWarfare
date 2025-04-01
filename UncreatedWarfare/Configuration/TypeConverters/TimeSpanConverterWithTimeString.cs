using System;
using System.ComponentModel;
using System.Globalization;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Configuration.TypeConverters;

/// <summary>
/// Supports time strings (7d 20hr 5min) for converting TimeSpans.
/// </summary>
public class TimeSpanConverterWithTimeString : TimeSpanConverter
{
    public static void Setup()
    {
        TypeDescriptor.AddAttributes(typeof(TimeSpan), new TypeConverterAttribute(typeof(TimeSpanConverterWithTimeString)));
    }

    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string))
            return FormattingUtility.ToTimeString((TimeSpan)value);

        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        try
        {
            return base.ConvertFrom(context, culture, value);
        }
        catch
        {
            if (value is string str)
            {
                return FormattingUtility.ParseTimespan(str);
            }

            throw;
        }
    }
}