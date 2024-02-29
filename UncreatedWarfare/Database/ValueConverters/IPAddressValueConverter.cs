using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Net;
using Uncreated.Warfare.Database.Automation;

namespace Uncreated.Warfare.Database.ValueConverters;

[ValueConverterCallback(nameof(Apply))]
public class IPAddressValueConverter : ValueConverter<IPAddress?, string?>
{
    public static readonly IPAddressValueConverter Instance = new IPAddressValueConverter();

    public IPAddressValueConverter() : base(
        x => x == null ? null : x.ToString(),
        x => x == null ? null : IPAddress.Parse(x))
    { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        EFCompat.SetValueConverter(property, Instance);
        property.SetColumnType("varchar(45)");
    }
}