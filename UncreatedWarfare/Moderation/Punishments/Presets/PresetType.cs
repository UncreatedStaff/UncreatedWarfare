namespace Uncreated.Warfare.Moderation.Punishments.Presets;

[Translatable("Preset Type", IsPrioritizedTranslation = false)]
public enum PresetType
{
    None,
    Griefing,
    Toxicity,
    Soloing,
    [Translatable("Asset Waste")]
    AssetWaste,
    [Translatable("Int. TKing")]
    IntentionalTeamkilling,
    [Translatable("Harassment")]
    TargetedHarassment,
    Discrimination,
    Cheating,
    [Translatable("Disrpt. Bhvr.")]
    DisruptiveBehavior,
    [Translatable("Inappr. Profile")]
    InappropriateProfile,
    [Translatable("Bypassing Pnshmnt.")]
    BypassingPunishment
}
