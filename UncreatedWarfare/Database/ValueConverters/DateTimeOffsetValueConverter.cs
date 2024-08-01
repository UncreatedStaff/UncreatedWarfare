using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using Uncreated.Warfare.Database.Automation;

namespace Uncreated.Warfare.Database.ValueConverters;

[ValueConverterCallback(nameof(Apply))]
public class DateTimeOffsetValueConverter : ValueConverter<DateTimeOffset, DateTime>
{
    public static readonly DateTimeOffsetValueConverter Instance = new DateTimeOffsetValueConverter();
    public static readonly NullableValueTypeConverter<DateTimeOffset, DateTime> NullableInstance = new NullableValueTypeConverter<DateTimeOffset, DateTime>(Instance);
    public DateTimeOffsetValueConverter() : base(
        x => x.UtcDateTime,
        x => new DateTimeOffset(DateTime.SpecifyKind(x, DateTimeKind.Utc)))
    { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        EFCompat.SetValueConverter(property, nullable ? NullableInstance : Instance);
        property.SetColumnType("datetime");
    }
}