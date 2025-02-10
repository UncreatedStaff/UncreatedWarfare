using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Kits;

[Translatable("Kit Type", Description = "Determines kit request behavior.")]
public enum KitType : byte
{
    [Translatable("Public", Description = "Free kits or kits bought with in-game credits.")]
    [Translatable(Languages.ChineseSimplified, "公用")]
    Public,
    [Translatable("Elite", Description = "Pre-made kits bought with real money.")]
    [Translatable(Languages.ChineseSimplified, "精英")]
    Elite,
    [Translatable("Special", Description = "Exclusive kits won through events or other means.")]
    [Translatable(Languages.ChineseSimplified, "特别")]
    Special,
    [Translatable("Loadout", Description = "Custom kits bought with real money.")]
    [Translatable(Languages.ChineseSimplified, "套装")]
    Loadout,
    [Translatable("Template", Description = "Kits meant to act as templates for creating other kits.")]
    Template
}