using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Util;

public static class EnumUtility
{
    /// <summary>
    /// Check if an enum value is a valid field in the enum declaration (instead of a random number).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Value is not part of a field.</exception>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    public static void AssertValidField<TEnum>(TEnum value, [InvokerParameterName] string paramterName = "value") where TEnum : unmanaged, Enum
    {
        if (!ValidateValidField(value))
            throw new ArgumentOutOfRangeException(paramterName, $"Expected defined value in enum type {Accessor.ExceptionFormatter.Format(typeof(TEnum))}.");
    }

    /// <summary>
    /// Check if an enum value is a valid field in the enum declaration (instead of a random number) and is not in the provided list of invalid values.
    /// </summary>
    /// <param name="invalids">List of invalid values which should also throw an <see cref="ArgumentOutOfRangeException"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Value is not part of a field.</exception>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    public static void AssertValidField<TEnum>(TEnum value, params ReadOnlySpan<TEnum> invalids) where TEnum : unmanaged, Enum
    {
        AssertValidField(value, nameof(value), invalids);
    }

    /// <summary>
    /// Check if an enum value is a valid field in the enum declaration (instead of a random number) and is not in the provided list of invalid values.
    /// </summary>
    /// <param name="invalids">List of invalid values which should also throw an <see cref="ArgumentOutOfRangeException"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Value is not part of a field.</exception>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    public static void AssertValidField<TEnum>(TEnum value, [InvokerParameterName] string paramterName, params ReadOnlySpan<TEnum> invalids) where TEnum : unmanaged, Enum
    {
        bool anyInvalid = false;
        for (int i = 0; i < invalids.Length; ++i)
        {
            if (!Equals(invalids[i], value))
                continue;

            anyInvalid = true;
            break;
        }

        if (!anyInvalid && ValidateValidField(value))
            return;

        string[] names = new string[invalids.Length];
        for (int i = 0; i < invalids.Length; ++i)
            names[i] = GetName(invalids[i]) ?? invalids[i].ToString();

        throw new ArgumentOutOfRangeException(paramterName, $"Expected defined value in enum type {Accessor.ExceptionFormatter.Format(typeof(TEnum))} other than the following values: [ {string.Join(", ", names)} ].");
    }

    /// <summary>
    /// Check if an enum value is a valid field in the enum declaration (instead of a random number).
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    public static bool ValidateValidField<TEnum>(TEnum value) where TEnum : unmanaged, Enum
    {
        TEnum[] values = EnumDataCache<TEnum>.Values ??= (TEnum[])Enum.GetValues(typeof(TEnum));
        for (int i = 0; i < values.Length; ++i)
        {
            if (Equals(value, values[i]))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Get whether or not an enum type defines the <see cref="FlagsAttribute"/>. Significantly faster than re-checking for repeated use.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    public static bool IsFlag<TEnum>() where TEnum : unmanaged, Enum
    {
        return EnumDataCache<TEnum>.IsFlag ??= typeof(TEnum).IsDefinedSafe<FlagsAttribute>();
    }

    /// <summary>
    /// Get the name of the enum which is cached after first use. Significantly faster than <see cref="Type.GetEnumName"/> for repeated use.
    /// </summary>
    /// <remarks>Flag enums will simply call back to <see cref="Enum.ToString"/> for composite values.</remarks>
    /// <returns>The name of the field represented by the enum, or a comma separated list of all flags if the <see cref="FlagsAttribute"/> is present.</returns>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    public static string? GetName<TEnum>(TEnum value) where TEnum : unmanaged, Enum
    {
        TEnum[] values = EnumDataCache<TEnum>.Values ??= (TEnum[])Enum.GetValues(typeof(TEnum));

        EnumDataCache<TEnum>.EnumNameRecord[]? records = EnumDataCache<TEnum>.Names;
        if (records == null)
        {
            Interlocked.CompareExchange(ref EnumDataCache<TEnum>.Names, new EnumDataCache<TEnum>.EnumNameRecord[values.Length], null);
            records = EnumDataCache<TEnum>.Names;

            if (records[^1].Name == null)
            {
                string[] names = typeof(TEnum).GetEnumNames();
                if (values.Length != names.Length)
                    throw new InvalidOperationException($"Invalid enum type: {Accessor.ExceptionFormatter.Format(typeof(TEnum))}.");

                for (int i = 0; i < names.Length; ++i)
                {
                    ref EnumDataCache<TEnum>.EnumNameRecord record = ref records[i];
                    record.Name = names[i];
                    record.Value = values[i];
                }
            }

            Interlocked.MemoryBarrier();
        }

        int ind = FindName(value, records);
        if (ind != -1)
            return records[ind].Name;

        return IsFlag<TEnum>() ? value.ToString() : null;
    }

    private static int FindName<TEnum>(TEnum value, EnumDataCache<TEnum>.EnumNameRecord[] records) where TEnum : unmanaged, Enum
    {
        int low = 0;
        int high = records.Length - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            ref EnumDataCache<TEnum>.EnumNameRecord r = ref records[mid];
            if (Equals(r.Value, value))
                return mid;

            if (LessThan(r.Value, value))
                low = mid + 1;
            else
                high = mid - 1;
        }

        return -1;
    }

    /// <summary>
    /// Gets the highest numerical enum value in the given enum type.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    public static TEnum GetMaximumValue<TEnum>() where TEnum : unmanaged, Enum
    {
        if (!EnumDataCache<TEnum>.HasMinMax)
        {
            GetMinMax(out EnumDataCache<TEnum>.Min, out EnumDataCache<TEnum>.Max);
            EnumDataCache<TEnum>.HasMinMax = true;
        }

        return EnumDataCache<TEnum>.Max;
    }

    /// <summary>
    /// Gets the lowest numerical enum value in the given enum type.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    public static TEnum GetMinimumValue<TEnum>() where TEnum : unmanaged, Enum
    {
        if (!EnumDataCache<TEnum>.HasMinMax)
        {
            GetMinMax(out EnumDataCache<TEnum>.Min, out EnumDataCache<TEnum>.Max);
            EnumDataCache<TEnum>.HasMinMax = true;
        }

        return EnumDataCache<TEnum>.Min;
    }

    /// <summary>
    /// Gets an all declared fields in the enum sorted from lowest to highest.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    public static IReadOnlyList<TEnum> GetEnumValues<TEnum>() where TEnum : unmanaged, Enum
    {
        return EnumDataCache<TEnum>.ValuesReadOnly ??= new ReadOnlyCollection<TEnum>(EnumDataCache<TEnum>.Values ??= (TEnum[])Enum.GetValues(typeof(TEnum)));
    }

    /// <summary>
    /// Gets an array of all declared fields in the enum sorted from lowest to highest.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TEnum[] GetEnumValuesArray<TEnum>() where TEnum : unmanaged, Enum
    {
        return EnumDataCache<TEnum>.Values ??= (TEnum[])Enum.GetValues(typeof(TEnum));
    }

    /// <summary>
    /// Check if <paramref name="a"/> is greater than <paramref name="b"/> in value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThan<TEnum>(TEnum a, TEnum b) where TEnum : unmanaged, Enum
    {
        return Compare(a, b) > 0;
    }

    /// <summary>
    /// Check if <paramref name="a"/> is less than <paramref name="b"/> in value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThan<TEnum>(TEnum a, TEnum b) where TEnum : unmanaged, Enum
    {
        return Compare(a, b) < 0;
    }

    /// <summary>
    /// Check if <paramref name="a"/> is greater than or equal to <paramref name="b"/> in value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterOrEqual<TEnum>(TEnum a, TEnum b) where TEnum : unmanaged, Enum
    {
        return Compare(a, b) >= 0;
    }

    /// <summary>
    /// Check if <paramref name="a"/> is less than or equal to <paramref name="b"/> in value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessOrEqual<TEnum>(TEnum a, TEnum b) where TEnum : unmanaged, Enum
    {
        return Compare(a, b) <= 0;
    }

    /// <summary>
    /// Check if <paramref name="a"/> is equal to <paramref name="b"/> in value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals<TEnum>(TEnum a, TEnum b) where TEnum : unmanaged, Enum
    {
        return Compare(a, b) == 0;
    }

    /// <summary>
    /// Compare two generic enums, returning a positive number if <paramref name="a"/> is greater than <paramref name="b"/>, zero if they are equal, or a negative number if <paramref name="a"/> is less than <paramref name="b"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid enum type.</exception>
    public static int Compare<TEnum>(TEnum a, TEnum b) where TEnum : unmanaged, Enum
    {
        return EnumDataCache<TEnum>.UnderlyingType switch
        {
            TypeCode.Boolean => CompareBoolean(a, b),
            TypeCode.Char => CompareChar(a, b),
            TypeCode.SByte => CompareInt8(a, b),
            TypeCode.Byte => CompareUInt8(a, b),
            TypeCode.Int16 => CompareInt16(a, b),
            TypeCode.UInt16 => CompareUInt16(a, b),
            TypeCode.Int32 => CompareInt32(a, b),
            TypeCode.UInt32 => CompareUInt32(a, b),
            TypeCode.Int64 => CompareInt64(a, b),
            TypeCode.UInt64 => CompareUInt64(a, b),
            (TypeCode)13 => CompareNativeInt(a, b),
            (TypeCode)14 => CompareNativeUInt(a, b),
            _ => throw new InvalidOperationException($"Invalid enum type: {Accessor.ExceptionFormatter.Format(typeof(TEnum))}.")
        };
    }

    private static int CompareBoolean<TEnum>(TEnum a, TEnum b)
    {
        bool aVal = Unsafe.As<TEnum, bool>(ref a);
        if (aVal == Unsafe.As<TEnum, bool>(ref b))
            return 0;

        return !aVal ? -1 : 1;
    }

    private static int CompareChar<TEnum>(TEnum a, TEnum b)
    {
        return Unsafe.As<TEnum, char>(ref a) - Unsafe.As<TEnum, char>(ref b);
    }

    private static int CompareInt8<TEnum>(TEnum a, TEnum b)
    {
        return Unsafe.As<TEnum, sbyte>(ref a) - Unsafe.As<TEnum, sbyte>(ref b);
    }
    private static int CompareUInt8<TEnum>(TEnum a, TEnum b)
    {
        return Unsafe.As<TEnum, byte>(ref a) - Unsafe.As<TEnum, byte>(ref b);
    }

    private static int CompareInt16<TEnum>(TEnum a, TEnum b)
    {
        return Unsafe.As<TEnum, short>(ref a) - Unsafe.As<TEnum, short>(ref b);
    }

    private static int CompareUInt16<TEnum>(TEnum a, TEnum b)
    {
        return Unsafe.As<TEnum, ushort>(ref a) - Unsafe.As<TEnum, ushort>(ref b);
    }

    private static int CompareInt32<TEnum>(TEnum a, TEnum b)
    {
        int aVal = Unsafe.As<TEnum, int>(ref a);
        int bVal = Unsafe.As<TEnum, int>(ref b);
        if (aVal < bVal)
            return -1;
        return aVal > bVal ? 1 : 0;
    }

    private static int CompareUInt32<TEnum>(TEnum a, TEnum b)
    {
        uint aVal = Unsafe.As<TEnum, uint>(ref a);
        uint bVal = Unsafe.As<TEnum, uint>(ref b);
        if (aVal < bVal)
            return -1;
        return aVal > bVal ? 1 : 0;
    }

    private static int CompareInt64<TEnum>(TEnum a, TEnum b)
    {
        long aVal = Unsafe.As<TEnum, long>(ref a);
        long bVal = Unsafe.As<TEnum, long>(ref b);
        if (aVal < bVal)
            return -1;
        return aVal > bVal ? 1 : 0;
    }

    private static int CompareUInt64<TEnum>(TEnum a, TEnum b)
    {
        ulong aVal = Unsafe.As<TEnum, ulong>(ref a);
        ulong bVal = Unsafe.As<TEnum, ulong>(ref b);
        if (aVal < bVal)
            return -1;
        return aVal > bVal ? 1 : 0;
    }

    private static int CompareNativeInt<TEnum>(TEnum a, TEnum b)
    {
        nint aVal = Unsafe.As<TEnum, nint>(ref a);
        nint bVal = Unsafe.As<TEnum, nint>(ref b);
        if (aVal < bVal)
            return -1;
        return aVal > bVal ? 1 : 0;
    }

    private static int CompareNativeUInt<TEnum>(TEnum a, TEnum b)
    {
        nuint aVal = Unsafe.As<TEnum, nuint>(ref a);
        nuint bVal = Unsafe.As<TEnum, nuint>(ref b);
        if (aVal < bVal)
            return -1;
        return aVal > bVal ? 1 : 0;
    }

    private static void GetMinMax<TEnum>(out TEnum minimum, out TEnum maximum) where TEnum : unmanaged, Enum
    {
        TEnum[] values = EnumDataCache<TEnum>.Values ??= (TEnum[])Enum.GetValues(typeof(TEnum));
        if (values.Length == 0)
        {
            minimum = default;
            maximum = default;
            return;
        }

        switch (EnumDataCache<TEnum>.UnderlyingType)
        {
            case TypeCode.Boolean:
                GetMinMaxBoolean(values, out minimum, out maximum);
                break;

            case TypeCode.Char:
                GetMinMaxChar(values, out minimum, out maximum);
                break;

            case TypeCode.SByte:
                GetMinMaxInt8(values, out minimum, out maximum);
                break;

            case TypeCode.Byte:
                GetMinMaxUInt8(values, out minimum, out maximum);
                break;

            case TypeCode.Int16:
                GetMinMaxInt16(values, out minimum, out maximum);
                break;

            case TypeCode.UInt16:
                GetMinMaxUInt16(values, out minimum, out maximum);
                break;

            case TypeCode.Int32:
                GetMinMaxInt32(values, out minimum, out maximum);
                break;

            case TypeCode.UInt32:
                GetMinMaxUInt32(values, out minimum, out maximum);
                break;
            case TypeCode.Int64:

                GetMinMaxInt64(values, out minimum, out maximum);
                break;

            case TypeCode.UInt64:
                GetMinMaxUInt64(values, out minimum, out maximum);
                break;

            case (TypeCode)13:
                GetMinMaxNativeInt(values, out minimum, out maximum);
                break;

            case (TypeCode)14:
                GetMinMaxNativeUInt(values, out minimum, out maximum);
                break;

            default:
                throw new InvalidOperationException($"Invalid enum type: {Accessor.ExceptionFormatter.Format(typeof(TEnum))}.");
        }
    }

    private static void GetMinMaxBoolean<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        bool min = Unsafe.As<TEnum, bool>(ref values[0]);
        bool max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            bool v = Unsafe.As<TEnum, bool>(ref values[i]);
            if (!v)
                min = false;
            else
                max = true;
        }

        minimum = Unsafe.As<bool, TEnum>(ref min);
        maximum = Unsafe.As<bool, TEnum>(ref max);
    }

    private static void GetMinMaxChar<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        char min = Unsafe.As<TEnum, char>(ref values[0]);
        char max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            char v = Unsafe.As<TEnum, char>(ref values[i]);
            if (v < min)
                min = v;
            if (v > max)
                max = v;
        }

        minimum = Unsafe.As<char, TEnum>(ref min);
        maximum = Unsafe.As<char, TEnum>(ref max);
    }

    private static void GetMinMaxInt8<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        sbyte min = Unsafe.As<TEnum, sbyte>(ref values[0]);
        sbyte max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            sbyte v = Unsafe.As<TEnum, sbyte>(ref values[i]);
            if (v < min)
                min = v;
            if (v > max)
                max = v;
        }

        minimum = Unsafe.As<sbyte, TEnum>(ref min);
        maximum = Unsafe.As<sbyte, TEnum>(ref max);
    }
    private static void GetMinMaxUInt8<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        byte min = Unsafe.As<TEnum, byte>(ref values[0]);
        byte max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            byte v = Unsafe.As<TEnum, byte>(ref values[i]);
            if (v < min)
                min = v;
            if (v > max)
                max = v;
        }

        minimum = Unsafe.As<byte, TEnum>(ref min);
        maximum = Unsafe.As<byte, TEnum>(ref max);
    }

    private static void GetMinMaxInt16<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        short min = Unsafe.As<TEnum, short>(ref values[0]);
        short max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            short v = Unsafe.As<TEnum, short>(ref values[i]);
            if (v < min)
                min = v;
            if (v > max)
                max = v;
        }

        minimum = Unsafe.As<short, TEnum>(ref min);
        maximum = Unsafe.As<short, TEnum>(ref max);
    }

    private static void GetMinMaxUInt16<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        ushort min = Unsafe.As<TEnum, ushort>(ref values[0]);
        ushort max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            ushort v = Unsafe.As<TEnum, ushort>(ref values[i]);
            if (v < min)
                min = v;
            if (v > max)
                max = v;
        }

        minimum = Unsafe.As<ushort, TEnum>(ref min);
        maximum = Unsafe.As<ushort, TEnum>(ref max);
    }

    private static void GetMinMaxInt32<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        int min = Unsafe.As<TEnum, int>(ref values[0]);
        int max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            int v = Unsafe.As<TEnum, int>(ref values[i]);
            if (v < min)
                min = v;
            if (v > max)
                max = v;
        }

        minimum = Unsafe.As<int, TEnum>(ref min);
        maximum = Unsafe.As<int, TEnum>(ref max);
    }

    private static void GetMinMaxUInt32<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        uint min = Unsafe.As<TEnum, uint>(ref values[0]);
        uint max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            uint v = Unsafe.As<TEnum, uint>(ref values[i]);
            if (v < min)
                min = v;
            if (v > max)
                max = v;
        }

        minimum = Unsafe.As<uint, TEnum>(ref min);
        maximum = Unsafe.As<uint, TEnum>(ref max);
    }

    private static void GetMinMaxInt64<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        long min = Unsafe.As<TEnum, long>(ref values[0]);
        long max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            long v = Unsafe.As<TEnum, long>(ref values[i]);
            if (v < min)
                min = v;
            if (v > max)
                max = v;
        }

        minimum = Unsafe.As<long, TEnum>(ref min);
        maximum = Unsafe.As<long, TEnum>(ref max);
    }

    private static void GetMinMaxUInt64<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        ulong min = Unsafe.As<TEnum, ulong>(ref values[0]);
        ulong max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            ulong v = Unsafe.As<TEnum, ulong>(ref values[i]);
            if (v < min)
                min = v;
            if (v > max)
                max = v;
        }

        minimum = Unsafe.As<ulong, TEnum>(ref min);
        maximum = Unsafe.As<ulong, TEnum>(ref max);
    }

    private static void GetMinMaxNativeInt<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        nint min = Unsafe.As<TEnum, nint>(ref values[0]);
        nint max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            nint v = Unsafe.As<TEnum, nint>(ref values[i]);
            if (v < min)
                min = v;
            if (v > max)
                max = v;
        }

        minimum = Unsafe.As<nint, TEnum>(ref min);
        maximum = Unsafe.As<nint, TEnum>(ref max);
    }

    private static void GetMinMaxNativeUInt<TEnum>(TEnum[] values, out TEnum minimum, out TEnum maximum)
    {
        nuint min = Unsafe.As<TEnum, nuint>(ref values[0]);
        nuint max = min;
        for (int i = 1; i < values.Length; ++i)
        {
            nuint v = Unsafe.As<TEnum, nuint>(ref values[i]);
            if (v < min)
                min = v;
            if (v > max)
                max = v;
        }

        minimum = Unsafe.As<nuint, TEnum>(ref min);
        maximum = Unsafe.As<nuint, TEnum>(ref max);
    }

    private static class EnumDataCache<TEnum> where TEnum : unmanaged, Enum
    {
        // 13 used for nint and 14 used for nuint
        public static readonly TypeCode UnderlyingType;

        public static bool HasMinMax;
        public static TEnum Min;
        public static TEnum Max;
        public static TEnum[]? Values;
        public static IReadOnlyList<TEnum>? ValuesReadOnly;
        public static EnumNameRecord[]? Names;
        public static bool? IsFlag;

        static EnumDataCache()
        {
            Type type = typeof(TEnum);

            if (!type.IsEnum)
                throw new InvalidOperationException($"Invalid enum type: {Accessor.ExceptionFormatter.Format(type)}.");

            UnderlyingType = Type.GetTypeCode(type.GetEnumUnderlyingType());

            if (UnderlyingType is (TypeCode)13 or (TypeCode)14)
                UnderlyingType = TypeCode.Empty;
            else if (type == typeof(nint))
                UnderlyingType = (TypeCode)13;
            else if (type == typeof(nuint))
                UnderlyingType = (TypeCode)14;
        }

        public struct EnumNameRecord
        {
            public TEnum Value;
            public string? Name;
        }
    }
}