using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Globalization;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Database.ValueConverters;

[ValueConverterCallback(nameof(Apply))]
public class UnturnedAssetReferenceValueConverter : ValueConverter<UnturnedAssetReference, string>
{
    public static readonly UnturnedAssetReferenceValueConverter Instance = new UnturnedAssetReferenceValueConverter();
    public static readonly NullableReferenceTypeConverter<UnturnedAssetReference, string> NullableInstance = new NullableReferenceTypeConverter<UnturnedAssetReference, string>(Instance);
    public UnturnedAssetReferenceValueConverter() : base(
            x => x.Guid == Guid.Empty ? x.Id != 0 ? x.Id.ToString(CultureInfo.InvariantCulture) : string.Empty : x.Guid.ToString("N"),
            x => x == null ? default : UnturnedAssetReference.Parse(x)) { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        EFCompat.SetValueConverter(property, nullable ? NullableInstance : Instance);
        property.SetColumnType("char(32)");
    }
}