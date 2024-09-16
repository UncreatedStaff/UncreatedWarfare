using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Kits;

/// <summary>Max field character limit: <see cref="KitEx.BranchMaxCharLimit"/>.</summary>
[Translatable("Branch", Description = "Branch or section of the military the kit falls into.")]
[ExcludedEnum(Default)]
public enum Branch : byte
{
    Default,
    [Translatable(Languages.ChineseSimplified, "步兵")]
    Infantry,
    [Translatable(Languages.ChineseSimplified, "装甲")]
    Armor,
    [Translatable(Languages.ChineseSimplified, "空军")]
    [Translatable("Air Force")]
    Airforce,
    [Translatable(Languages.ChineseSimplified, "特种部队")]
    [Translatable("Special Ops")]
    SpecOps,
    [Translatable(Languages.ChineseSimplified, "海军")]
    Navy
}