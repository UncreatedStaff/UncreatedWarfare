namespace Uncreated.Warfare.Translations;

// going to add more later, figure its easier to get everything working with 2 instead of a bunch, then duplicate it later
public class Translation<T0> : Translation
{
    public override int ArgumentCount => 1;
    public string? FormatArg0 { get; set; }
    public Translation(string defaultValue, TranslationOptions options = default, string? arg0Fmt = null)
        : base(defaultValue, options)
    {
        FormatArg0 = arg0Fmt;
    }
}
public class Translation<T0, T1> : Translation
{
    public override int ArgumentCount => 2;
    public string? FormatArg0 { get; set; }
    public string? FormatArg1 { get; set; }
    public Translation(string defaultValue, TranslationOptions options = default, string? arg0Fmt = null, string? arg1Fmt = null)
        : base(defaultValue, options)
    {
        FormatArg0 = arg0Fmt;
        FormatArg1 = arg1Fmt;
    }
}