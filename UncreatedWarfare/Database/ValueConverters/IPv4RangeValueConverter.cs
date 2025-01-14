using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Database.ValueConverters;

// ReSharper disable once InconsistentNaming

[ValueConverterCallback(nameof(Apply))]
public class IPv4RangeValueConverter : ValueConverter<IPv4Range, string>
{
    public static readonly IPv4RangeValueConverter Instance = new IPv4RangeValueConverter();
    public static readonly NullableReferenceTypeConverter<IPv4Range, string> NullableInstance = new NullableReferenceTypeConverter<IPv4Range, string>(Instance);

    public IPv4RangeValueConverter() : base(
        x => x.ToString(),
        x => IPv4Range.Parse(x))
    { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        EFCompat.Instance.SetValueConverter(property, nullable ? NullableInstance : Instance);
        property.SetColumnType("varchar(18)");
    }
}

// ReSharper disable once InconsistentNaming

[ValueConverterCallback(nameof(Apply))]
public class IPv4Converter : ValueConverter<IPv4Range, string>
{
    public static readonly IPv4Converter Instance = new IPv4Converter();
    public static readonly NullableReferenceTypeConverter<IPv4Range, string> NullableInstance = new NullableReferenceTypeConverter<IPv4Range, string>(Instance);

    public IPv4Converter() : base(
        x => x.ToIPv4String(),
        x => IPv4Range.ParseIPv4(x))
    { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        EFCompat.Instance.SetValueConverter(property, nullable ? NullableInstance : Instance);
        property.SetColumnType("varchar(15)");
    }
}