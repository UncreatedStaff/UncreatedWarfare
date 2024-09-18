using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.UI;

[UnturnedUI(BasePath = "Canvas/Circles")]
public class CaptureUI : UnturnedUI
{
    private readonly ITranslationValueFormatter _valueFormatter;

    public readonly UnturnedLabel Background = new UnturnedLabel("BackgroundCircle");
    public readonly UnturnedLabel Foreground = new UnturnedLabel("BackgroundCircle/ForegroundCircle");
    public readonly UnturnedLabel T1CountIcon = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T1CountIcon");
    public readonly UnturnedLabel T1Count = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T1CountIcon/T1Count");
    public readonly UnturnedLabel T2CountIcon = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T2CountIcon");
    public readonly UnturnedLabel T2Count = new UnturnedLabel("BackgroundCircle/ForegroundCircle/T2CountIcon/T2Count");
    public readonly UnturnedLabel Status = new UnturnedLabel("Status");
    public CaptureUI(ITranslationValueFormatter valueFormatter, AssetConfiguration assetConfig) : base(assetConfig.GetAssetLink<EffectAsset>("UI:CaptureProgress"), reliable: false)
    {
        _valueFormatter = valueFormatter;
    }
}

public enum FlagStatus
{
    [Translatable("CAPTURING", Description = "Shown when your team is capturing the flag.")]
    Capturing,

    [Translatable("LOSING", Description = "Shown when your team is losing the flag because the other team has more players.")]
    Losing,

    [Translatable("SECURED", Description = "Shown when your team is holding the flag after it has been captured.")]
    Secured,

    [Translatable("NEUTRALIZED", Description = "Shown when the flag has not been captured by either team.")]
    Neutralized,

    [Translatable("LOST", Description = "Shown when your team lost the flag and you dont have enough people on the flag to clear.")]
    Lost,

    [Translatable("CONTESTED", Description = "Shown when your team and the other team have the same amount of people on the flag.")]
    Contested,

    [Translatable("INEFFECTIVE", Description = "Shown when you're on a flag but it's not the objective.")]
    Ineffective,

    [Translatable("CLEARING", Description = "Shown when your team is capturing a flag still owned by the other team.")]
    Clearing,

    /// <summary>
    /// No text on the UI.
    /// </summary>
    [Translatable("", Description = "Leave blank.", IsPrioritizedTranslation = false)]
    Blank,
    
    /// <summary>
    /// Removes the UI completely.
    /// </summary>
    [Translatable("", Description = "Leave blank.", IsPrioritizedTranslation = false)]
    DontDisplay,

    [Translatable("IN VEHICLE", Description = "Shown when you're trying to capture a flag while in a vehicle.")]
    InVehicle,

    [Translatable("LOCKED", Description = "Shown in Invasion when a flag has already been captured by attackers and can't be recaptured.")]
    Locked
}