using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Database.ValueConverters;

[ValueConverterCallback(nameof(Apply))]
public class HWIDValueConverter : ValueConverter<HWID, byte[]>
{
    public static readonly HWIDValueConverter Instance = new HWIDValueConverter();
    public static readonly NullableConverter<HWID, byte[]> NullableInstance = new NullableConverter<HWID, byte[]>(Instance);

    public HWIDValueConverter() : base(
        x => x.ToByteArray(),
        x => new HWID(x, 0)) { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        property.SetValueConverter(nullable ? NullableInstance : Instance);
        property.SetColumnType("binary(20)");
    }
}