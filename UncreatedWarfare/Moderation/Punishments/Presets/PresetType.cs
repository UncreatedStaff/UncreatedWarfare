using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Moderation.Punishments.Presets;

[Translatable("Preset Type", IsPrioritizedTranslation = false)]
public enum PresetType
{
    None,
    Griefing,
    Toxicity,
    Soloing,
    [TranslatableValue("Asset Waste")]
    AssetWaste,
    [TranslatableValue("Intentional Teamkilling")]
    IntentionalTeamkilling,
    [TranslatableValue("Targeted Harassment")]
    TargetedHarassment,
    Discrimination,
    Cheating,
    [TranslatableValue("Disruptive Behavior")]
    DisruptiveBehavior,
    [TranslatableValue("Inappropriate Profile")]
    InappropriateProfile,
    [TranslatableValue("Bypassing Punishment")]
    BypassingPunishment
}
