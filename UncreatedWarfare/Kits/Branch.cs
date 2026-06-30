using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Kits;

[Translatable("Branch", Description = "Branch or section of the military the kit falls into.")]
[ExcludedEnum(Default)]
public enum Branch : byte
{
    Default,
    Infantry,
    Armor,
    [TranslatableValue("Air Force")]
    Airforce,
    [TranslatableValue("Special Forces")]
    SpecOps,
    Navy
}