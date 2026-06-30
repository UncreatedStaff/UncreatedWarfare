using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Kits;

[Translatable("Kit Type", Description = "Kit categories, determining how they are requested and whether or not they cost real money.")]
public enum KitType : byte
{
    [TranslatableValue(Description = "Free kits or kits bought with in-game credits.")]
    Public,
    [TranslatableValue(Description = "Pre-made kits bought with real money.")]
    Elite,
    [TranslatableValue(Description = "Exclusive kits won through events or other means.")]
    Special,
    [TranslatableValue(Description = "Custom kits bought with real money.")]
    Loadout,
    [TranslatableValue(Description = "Kits meant to act as templates for creating other kits.")]
    Template
}