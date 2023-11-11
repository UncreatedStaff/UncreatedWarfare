using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Uncreated.Warfare.Database.ValueConverters;
public class NullableConverter<TValue, TProvider> : ValueConverter<TValue?, TProvider?> where TValue : struct where TProvider : class
{
    public ValueConverter<TValue, TProvider> ValueConverter { get; }
    public NullableConverter(ValueConverter<TValue, TProvider> valueConverter) : base(
        x => x.HasValue ? (TProvider?)valueConverter.ConvertToProvider(x) : null,
        x => x != null ? (TValue)valueConverter.ConvertFromProvider(x) : null)
    {
        ValueConverter = valueConverter;
    }
}
