using System;

namespace Uncreated.Warfare.Translations.Addons;
public interface IArgumentAddon
{
    string DisplayName { get; }
    string ApplyAddon(ITranslationValueFormatter formatter, string text, TypedReference value, in ValueFormatParameters args);
}
