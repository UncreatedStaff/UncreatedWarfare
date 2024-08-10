namespace Uncreated.Warfare.Translations.Addons;
public sealed class LowercaseAddon : IArgumentAddon
{
    public static LowercaseAddon Instance { get; } = new LowercaseAddon();
    private LowercaseAddon() { }
    public string ApplyAddon(string text, in ValueFormatParameters args)
    {
        return text.ToLower(args.Culture);
    }
}
