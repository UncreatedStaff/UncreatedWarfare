using System;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Translations.Addons;
public sealed class LowercaseAddon : IArgumentAddon
{
    public static LowercaseAddon Instance { get; } = new LowercaseAddon();
    public string DisplayName => "Lowercase";
    private LowercaseAddon() { }
    public string ApplyAddon(ITranslationValueFormatter formatter, string text, TypedReference value, in ValueFormatParameters args)
    {
        return text.ToLower(args.Culture);
    }

    public static implicit operator ArgumentFormat(LowercaseAddon addon) => new ArgumentFormat(addon);
}
