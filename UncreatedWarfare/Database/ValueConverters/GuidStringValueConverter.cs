using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using Uncreated.Warfare.Database.Automation;

namespace Uncreated.Warfare.Database.ValueConverters;

[ValueConverterCallback(nameof(Apply))]
public class GuidStringValueConverter : ValueConverter<Guid, string>
{
    public static readonly GuidStringValueConverter Instance = new GuidStringValueConverter();
    public static readonly NullableReferenceTypeConverter<Guid, string> NullableInstance = new NullableReferenceTypeConverter<Guid, string>(Instance);
    public GuidStringValueConverter() : base(
        x => x.ToString("N"),
        x => Guid.ParseExact(x, "N")) { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        EFCompat.Instance.SetValueConverter(property, nullable ? NullableInstance : Instance);
        property.SetColumnType("char(32)");
    }
}