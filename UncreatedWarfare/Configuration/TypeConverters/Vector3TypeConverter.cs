using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Configuration.TypeConverters;

public class Vector3TypeConverter : TypeConverter
{
    public static void Setup()
    {
        TypeDescriptor.AddAttributes(typeof(Vector3), new TypeConverterAttribute(typeof(Vector3TypeConverter)));
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
            return TryParseVector3(str, out _, false);

        return false;
    }

    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        Vector3 val = (Vector3)value;

        if (destinationType != typeof(string))
            return base.ConvertTo(context, culture, value, destinationType);

        // format (x, y, z)
        Span<char> outSpan = stackalloc char[48];

        outSpan[0] = '(';
        if (!val.x.TryFormat(outSpan.Slice(1), out int charsWritten, provider: CultureInfo.InvariantCulture))
            return Fallback(in val);

        int index = charsWritten + 1;
        if (index + 7 >= outSpan.Length)
            return Fallback(in val);
        outSpan[index] = ',';
        outSpan[++index] = ' ';
        ++index;

        if (!val.y.TryFormat(outSpan.Slice(index), out charsWritten, provider: CultureInfo.InvariantCulture))
            return Fallback(in val);

        index += charsWritten;
        if (index + 4 >= outSpan.Length)
            return Fallback(in val);
        outSpan[index] = ',';
        outSpan[++index] = ' ';
        ++index;

        if (!val.z.TryFormat(outSpan.Slice(index), out charsWritten, provider: CultureInfo.InvariantCulture))
            return Fallback(in val);

        index += charsWritten;
        if (index + 1 >= outSpan.Length)
            return Fallback(in val);
        outSpan[index] = ')';
        ++index;

        return new string(outSpan.Slice(0, index));

        string Fallback(in Vector3 val)
        {
            return $"({val.x.ToString(CultureInfo.InvariantCulture)}, {val.y.ToString(CultureInfo.InvariantCulture)}, {val.z.ToString(CultureInfo.InvariantCulture)})";
        }
    }

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is not string str)
            return base.ConvertFrom(context, culture, value);

        if (!TryParseVector3(str, out object? v3, true))
        {
            throw GetConvertFromException(str);
        }

        return v3;
    }

    private static bool TryParseVector3(string str, [MaybeNullWhen(false)] out object obj, bool create)
    {
        obj = null;
        ReadOnlySpan<char> parsableString = str.AsSpan().Trim();

        if (parsableString.Length > 2 && parsableString[0] == '(' && parsableString[^1] == ')')
        {
            parsableString = parsableString.Slice(1, parsableString.Length - 2);
        }

        int commaOne = parsableString.IndexOf(',');
        if (commaOne < 0 || commaOne >= parsableString.Length - 1)
            return false;

        int commaTwo = parsableString.IndexOf(',', commaOne + 1);
        if (commaTwo < 0 || commaTwo >= parsableString.Length - 1)
            return false;

        ReadOnlySpan<char> xStr = parsableString.Slice(0, commaOne);
        ReadOnlySpan<char> yStr = parsableString.Slice(commaOne + 1, commaTwo - commaOne - 1);
        ReadOnlySpan<char> zStr = parsableString.Slice(commaTwo + 1, parsableString.Length - commaTwo - 1);
        const NumberStyles style = NumberStyles.AllowDecimalPoint
                                   | NumberStyles.AllowLeadingSign
                                   | NumberStyles.AllowLeadingWhite
                                   | NumberStyles.AllowTrailingWhite;

        if (!float.TryParse(xStr, style, CultureInfo.InvariantCulture, out float x)
            || !float.TryParse(yStr, style, CultureInfo.InvariantCulture, out float y)
            || !float.TryParse(zStr, style, CultureInfo.InvariantCulture, out float z))
        {
            return false;
        }

        obj = create ? new Vector3(x, y, z) : null!;
        return true;
    }
}