using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Net;
using Uncreated.Warfare.Database.Automation;

namespace Uncreated.Warfare.Database.ValueConverters;

[ValueConverterCallback(nameof(Apply))]
public class IPAddressConverter : ValueConverter<IPAddress?, string?>
{
    public static readonly IPAddressConverter Instance = new IPAddressConverter();

    public IPAddressConverter() : base(
        x => x == null ? null : x.ToString(),
        x => x == null ? null : IPAddress.Parse(x))
    { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        property.SetValueConverter(Instance);
        property.SetColumnType("varchar(45)");
    }
}