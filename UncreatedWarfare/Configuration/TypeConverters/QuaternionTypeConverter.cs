using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Configuration.TypeConverters;

public class QuaternionTypeConverter : TypeConverter
{
    public static void Setup()
    {
        TypeDescriptor.AddAttributes(typeof(Quaternion), new TypeConverterAttribute(typeof(QuaternionTypeConverter)));
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
            return TryParseQuaternion(str, out _, false);

        return false;
    }

    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        Quaternion val = (Quaternion)value;

        if (destinationType != typeof(string))
            return base.ConvertTo(context, culture, value, destinationType);

        // format (x, y, z)
        Span<char> outSpan = stackalloc char[64];

        outSpan[0] = '(';
        if (!val.x.TryFormat(outSpan.Slice(1), out int charsWritten, provider: CultureInfo.InvariantCulture))
            return Fallback(in val);

        int index = charsWritten + 1;
        if (index + 10 >= outSpan.Length)
            return Fallback(in val);
        outSpan[index] = ',';
        outSpan[++index] = ' ';
        ++index;

        if (!val.y.TryFormat(outSpan.Slice(index), out charsWritten, provider: CultureInfo.InvariantCulture))
            return Fallback(in val);

        index += charsWritten;
        if (index + 7 >= outSpan.Length)
            return Fallback(in val);
        outSpan[index] = ',';
        outSpan[++index] = ' ';
        ++index;

        if (!val.z.TryFormat(outSpan.Slice(index), out charsWritten, provider: CultureInfo.InvariantCulture))
            return Fallback(in val);

        index += charsWritten;
        if (index + 4 >= outSpan.Length)
            return Fallback(in val);
        outSpan[index] = ',';
        outSpan[++index] = ' ';
        ++index;

        if (!val.w.TryFormat(outSpan.Slice(index), out charsWritten, provider: CultureInfo.InvariantCulture))
            return Fallback(in val);

        index += charsWritten;
        if (index + 1 >= outSpan.Length)
            return Fallback(in val);
        outSpan[index] = ')';
        ++index;

        return new string(outSpan.Slice(0, index));

        string Fallback(in Quaternion val)
        {
            return $"({val.x.ToString(CultureInfo.InvariantCulture)}, {val.y.ToString(CultureInfo.InvariantCulture)}, {val.z.ToString(CultureInfo.InvariantCulture)}, {val.w.ToString(CultureInfo.InvariantCulture)})";
        }
    }

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is not string str)
            return base.ConvertFrom(context, culture, value);

        if (!TryParseQuaternion(str, out object? q, true))
        {
            throw GetConvertFromException(str);
        }

        return q;
    }

    private static bool TryParseQuaternion(string str, [MaybeNullWhen(false)] out object obj, bool create)
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

        int commaThree = parsableString.IndexOf(',', commaTwo + 1);
        if (commaThree < 0 || commaThree >= parsableString.Length - 1)
            return false;

        ReadOnlySpan<char> xStr = parsableString.Slice(0, commaOne);
        ReadOnlySpan<char> yStr = parsableString.Slice(commaOne + 1, commaTwo - commaOne - 1);
        ReadOnlySpan<char> zStr = parsableString.Slice(commaTwo + 1, commaThree - commaTwo - 1);
        ReadOnlySpan<char> wStr = parsableString.Slice(commaThree + 1, parsableString.Length - commaThree - 1);
        const NumberStyles style = NumberStyles.AllowDecimalPoint
                                   | NumberStyles.AllowLeadingSign
                                   | NumberStyles.AllowLeadingWhite
                                   | NumberStyles.AllowTrailingWhite;

        if (!float.TryParse(xStr, style, CultureInfo.InvariantCulture, out float x)
            || !float.TryParse(yStr, style, CultureInfo.InvariantCulture, out float y)
            || !float.TryParse(zStr, style, CultureInfo.InvariantCulture, out float z)
            || !float.TryParse(wStr, style, CultureInfo.InvariantCulture, out float w))
        {
            return false;
        }

        obj = create ? new Quaternion(x, y, z, w) : null!;
        return true;
    }
}