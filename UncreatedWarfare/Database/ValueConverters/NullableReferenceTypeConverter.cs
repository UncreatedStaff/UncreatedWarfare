using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Uncreated.Warfare.Database.ValueConverters;
public class NullableReferenceTypeConverter<TValue, TProvider> : ValueConverter<TValue?, TProvider?> where TValue : struct where TProvider : class
{
    public ValueConverter<TValue, TProvider> ValueConverter { get; }
    public NullableReferenceTypeConverter(ValueConverter<TValue, TProvider> valueConverter) : base(
        x => x.HasValue ? (TProvider?)valueConverter.ConvertToProvider(x) : null,
        x => x != null ? (TValue)valueConverter.ConvertFromProvider(x) : null)
    {
        ValueConverter = valueConverter;
    }
}
public class NullableValueTypeConverter<TValue, TProvider> : ValueConverter<TValue?, TProvider?> where TValue : struct where TProvider : struct
{
    public ValueConverter<TValue, TProvider> ValueConverter { get; }
    public NullableValueTypeConverter(ValueConverter<TValue, TProvider> valueConverter) : base(
        x => x.HasValue ? (TProvider?)valueConverter.ConvertToProvider(x) : null,
        x => x.HasValue ? (TValue)valueConverter.ConvertFromProvider(x) : null)
    {
        ValueConverter = valueConverter;
    }
}

public class NullableReferenceValueTypeConverter<TValue, TProvider> : ValueConverter<TValue?, TProvider?> where TValue : class where TProvider : struct
{
    public ValueConverter<TValue, TProvider> ValueConverter { get; }
    public NullableReferenceValueTypeConverter(ValueConverter<TValue, TProvider> valueConverter) : base(
        x => x != null ? (TProvider?)valueConverter.ConvertToProvider(x) : null,
        x => x.HasValue ? (TValue)valueConverter.ConvertFromProvider(x) : null)
    {
        ValueConverter = valueConverter;
    }
}

public class NullableValueReferenceTypeConverter<TValue, TProvider> : ValueConverter<TValue?, TProvider?> where TValue : struct where TProvider : class
{
    public ValueConverter<TValue, TProvider> ValueConverter { get; }
    public NullableValueReferenceTypeConverter(ValueConverter<TValue, TProvider> valueConverter) : base(
        x => x.HasValue ? (TProvider?)valueConverter.ConvertToProvider(x) : null,
        x => x != null ? (TValue)valueConverter.ConvertFromProvider(x) : null)
    {
        ValueConverter = valueConverter;
    }
}
