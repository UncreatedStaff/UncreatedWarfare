using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Uncreated.SQL;
using Uncreated.Warfare.Database.Automation;

namespace Uncreated.Warfare.Database.ValueConverters;

[ValueConverterCallback(nameof(Apply))]
public class PrimaryKeyValueConverter : ValueConverter<PrimaryKey, uint>
{
    public static readonly PrimaryKeyValueConverter Instance = new PrimaryKeyValueConverter();
    public static readonly NullableValueTypeConverter<PrimaryKey, uint> NullableInstance = new NullableValueTypeConverter<PrimaryKey, uint>(Instance);

    public PrimaryKeyValueConverter() : base(
        x => x.Key,
        x => x) { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        EFCompat.SetValueConverter(property, nullable ? NullableInstance : Instance);
        property.SetColumnType("int unsigned");
    }
}