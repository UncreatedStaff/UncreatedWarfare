using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Database.ValueConverters;

[ValueConverterCallback(nameof(Apply))]
public class ActorValueConverter : ValueConverter<IModerationActor, ulong>
{
    public static readonly ActorValueConverter Instance = new ActorValueConverter();
    public static readonly NullableReferenceValueTypeConverter<IModerationActor, ulong> NullableInstance = new NullableReferenceValueTypeConverter<IModerationActor, ulong>(Instance);
    public ActorValueConverter() : base(
        x => x.Id,
        x => Actors.GetActor(x))
    { }

    [UsedImplicitly]
    public static void Apply(ModelBuilder modelBuilder, IMutableProperty property, bool nullable)
    {
        EFCompat.Instance.SetValueConverter(property, nullable ? NullableInstance : Instance);
        property.SetColumnType("char(32)");
    }
}