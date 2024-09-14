using System;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Translations.Addons;
public sealed class LowercaseAddon : IArgumentAddon
{
    public static LowercaseAddon Instance { get; } = new LowercaseAddon();

    private static readonly IArgumentAddon[] InstanceArray = [ Instance ];
    public string DisplayName => "Lowercase";
    private LowercaseAddon() { }
    public string ApplyAddon(ITranslationValueFormatter formatter, string text, TypedReference value, in ValueFormatParameters args)
    {
        return text.ToLower(args.Culture);
    }

    public static implicit operator ArgumentFormat(LowercaseAddon addon) => ReferenceEquals(addon, Instance) ? new ArgumentFormat(InstanceArray) : new ArgumentFormat(addon);
}
