using System;
using System.Data.Common;

namespace Uncreated.Warfare.Database.Manual;
public static class DataReaderExtensions
{
    public static byte[] ReadByteArray(this DbDataReader reader, int ordinal)
    {
        return (byte[])reader[ordinal];
    }

    public static TEnum ReadStringEnum<TEnum>(this DbDataReader reader, int ordinal, TEnum @default) where TEnum : unmanaged, Enum
    {
        return !reader.IsDBNull(ordinal) && Enum.TryParse(reader.GetString(ordinal), true, out TEnum rtn) ? rtn : @default;
    }

    public static TEnum? ReadStringEnum<TEnum>(this DbDataReader reader, int ordinal) where TEnum : unmanaged, Enum
    {
        return !reader.IsDBNull(ordinal) && Enum.TryParse(reader.GetString(ordinal), true, out TEnum rtn) ? rtn : null;
    }

    public static string ToGuidString(this Guid guid) => guid.ToString("N");

    public static Guid? ReadGuidString(this DbDataReader reader, int ordinal)
    {
        return !reader.IsDBNull(ordinal) && Guid.TryParse(reader.GetString(ordinal), out Guid guid) ? guid : null;
    }

    public static Guid ReadGuidStringOrDefault(this DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return Guid.Empty;

        Guid.TryParse(reader.GetString(ordinal), out Guid guid);
        return guid;
    }
}
