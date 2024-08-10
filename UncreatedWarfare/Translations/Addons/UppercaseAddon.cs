namespace Uncreated.Warfare.Translations.Addons;
public sealed class UppercaseAddon : IArgumentAddon
{
    public static UppercaseAddon Instance { get; } = new UppercaseAddon();
    private UppercaseAddon() { }
    public string ApplyAddon(string text, in ValueFormatParameters args)
    {
        return text.ToUpper(args.Culture);
    }
}
