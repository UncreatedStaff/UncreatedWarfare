using System;

namespace Uncreated.Warfare.Database;
internal static class DatabaseHelper
{
    public static string EnumType<TEnum>(TEnum exclude1, TEnum exclude2) where TEnum : unmanaged, Enum
    {
        TEnum[] values = (TEnum[])typeof(TEnum).GetEnumValues();
        int c = 0;
        for (int i = 0; i < values.Length; ++i)
        {
            if (!values[i].Equals(exclude1) && !values[i].Equals(exclude2))
                ++c;
        }
        string[] strs = new string[c];
        if (strs.Length > 0)
        {
            for (int i = values.Length - 1; i >= 0; --i)
            {
                if (!values[i].Equals(exclude1) && !values[i].Equals(exclude2))
                {
                    strs[--c] = '\'' + values[i].ToString() + '\'';
                }
            }
        }
        return "enum(" + string.Join(",", strs, 0, strs.Length) + ")";
    }
    public static string EnumType<TEnum>(TEnum exclude) where TEnum : unmanaged, Enum
    {
        TEnum[] values = (TEnum[])typeof(TEnum).GetEnumValues();
        int c = 0;
        for (int i = 0; i < values.Length; ++i)
        {
            if (!values[i].Equals(exclude))
                ++c;
        }
        string[] strs = new string[c];
        if (strs.Length > 0)
        {
            for (int i = values.Length - 1; i >= 0; --i)
            {
                if (!values[i].Equals(exclude))
                {
                    strs[--c] = '\'' + values[i].ToString() + '\'';
                }
            }
        }
        return "enum(" + string.Join(",", strs, 0, strs.Length) + ")";
    }
    public static string EnumType<TEnum>() where TEnum : unmanaged, Enum
    {
        TEnum[] values = (TEnum[])typeof(TEnum).GetEnumValues();
        string[] strs = new string[values.Length];
        for (int i = 0; i < values.Length; ++i)
        {
            strs[i] = '\'' + values[i].ToString() + '\'';
        }
        return "enum(" + string.Join(",", strs, 0, strs.Length) + ")";
    }
}
