using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Uncreated.Warfare.Configuration.TypeConverters;

public class Vector2TypeConverter : TypeConverter
{
    public static void Setup()
    {
        TypeDescriptor.AddAttributes(typeof(Vector2), new TypeConverterAttribute(typeof(Vector2TypeConverter)));
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string);
    }

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override bool IsValid(ITypeDescriptorContext context, object value)
    {
        if (value is string str)
            return TryParseVector2(str, out _, false);

        return false;
    }

    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        Vector2 val = (Vector2)value;

        if (destinationType != typeof(string))
            return base.ConvertTo(context, culture, value, destinationType);

        // format (x, y)
        Span<char> outSpan = stackalloc char[32];

        outSpan[0] = '(';
        if (!val.x.TryFormat(outSpan.Slice(1), out int charsWritten, provider: CultureInfo.InvariantCulture))
            return Fallback(in val);

        int index = charsWritten + 1;
        if (index + 4 >= outSpan.Length)
            return Fallback(in val);
        outSpan[index] = ',';
        outSpan[++index] = ' ';
        ++index;

        if (!val.y.TryFormat(outSpan.Slice(index), out charsWritten, provider: CultureInfo.InvariantCulture))
            return Fallback(in val);

        index += charsWritten;
        if (index + 1 >= outSpan.Length)
            return Fallback(in val);
        outSpan[index] = ')';
        ++index;

        return new string(outSpan.Slice(0, index));

        string Fallback(in Vector2 val)
        {
            return $"({val.x.ToString(CultureInfo.InvariantCulture)}, {val.y.ToString(CultureInfo.InvariantCulture)})";
        }
    }

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is not string str)
            return base.ConvertFrom(context, culture, value);

        if (!TryParseVector2(str, out object? v2, true))
        {
            throw GetConvertFromException(str);
        }

        return v2;
    }

    private static bool TryParseVector2(string str, [MaybeNullWhen(false)] out object obj, bool create)
    {
        obj = null;
        ReadOnlySpan<char> parsableString = str.AsSpan().Trim();

        if (parsableString.Length > 2 && parsableString[0] == '(' && parsableString[^1] == ')')
        {
            parsableString = parsableString.Slice(1, parsableString.Length - 2);
        }

        int comma = parsableString.IndexOf(',');
        if (comma < 0 || comma >= parsableString.Length - 1)
            return false;

        ReadOnlySpan<char> xStr = parsableString.Slice(0, comma);
        ReadOnlySpan<char> yStr = parsableString.Slice(comma + 1, parsableString.Length - comma - 1);
        const NumberStyles style = NumberStyles.AllowDecimalPoint
                                   | NumberStyles.AllowLeadingSign
                                   | NumberStyles.AllowLeadingWhite
                                   | NumberStyles.AllowTrailingWhite;

        if (!float.TryParse(xStr, style, CultureInfo.InvariantCulture, out float x)
            || !float.TryParse(yStr, style, CultureInfo.InvariantCulture, out float y))
        {
            return false;
        }

        obj = create ? new Vector2(x, y) : null!;
        return true;
    }
}