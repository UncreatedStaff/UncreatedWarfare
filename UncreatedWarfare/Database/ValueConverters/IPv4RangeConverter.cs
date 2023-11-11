using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Uncreated.Networking;
using Uncreated.Warfare.Database.Automation;

namespace Uncreated.Warfare.Database.ValueConverters;

// ReSharper disable once InconsistentNaming

[ValueConverterCallback(nameof(Apply))]
public class IPv4RangeConverter : ValueConverter<IPv4Range, string>
{
    public static readonly IPv4RangeConverter Instance = new IPv4RangeConverter();
    public static readonly NullableConverter<IPv4Range, string> NullableInstance = new NullableConverter<IPv4Range, string>(Instance);

    public IPv4RangeConverter() : base(
        x => x.ToString(),
        x => IPv4Range.Parse(x))
    { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        property.SetValueConverter(nullable ? NullableInstance : Instance);
        property.SetColumnType("varchar(18)");
    }
}

// ReSharper disable once InconsistentNaming

[ValueConverterCallback(nameof(Apply))]
public class IPv4Converter : ValueConverter<IPv4Range, string>
{
    public static readonly IPv4Converter Instance = new IPv4Converter();
    public static readonly NullableConverter<IPv4Range, string> NullableInstance = new NullableConverter<IPv4Range, string>(Instance);

    public IPv4Converter() : base(
        x => x.IPToString(),
        x => IPv4Range.ParseIPv4(x))
    { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        property.SetValueConverter(nullable ? NullableInstance : Instance);
        property.SetColumnType("varchar(15)");
    }
}