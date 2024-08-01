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
    public static readonly NullableReferenceTypeConverter<HWID, byte[]> NullableInstance = new NullableReferenceTypeConverter<HWID, byte[]>(Instance);

    public HWIDValueConverter() : base(
        x => x.ToByteArray(),
        x => new HWID(x, 0)) { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        EFCompat.SetValueConverter(property, nullable ? NullableInstance : Instance);
        property.SetColumnType("binary(20)");
    }
}