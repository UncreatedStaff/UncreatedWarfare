using System;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Translations.Addons;
public sealed class UppercaseAddon : IArgumentAddon
{
    public static UppercaseAddon Instance { get; } = new UppercaseAddon();
    public string DisplayName => "Uppercase";
    private UppercaseAddon() { }
    public string ApplyAddon(ITranslationValueFormatter formatter, string text, TypedReference value, in ValueFormatParameters args)
    {
        return text.ToUpper(args.Culture);
    }

    public static implicit operator ArgumentFormat(UppercaseAddon addon) => new ArgumentFormat(addon);
}
