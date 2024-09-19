using System;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Players.UI;
public enum ToastMessageStyle
{
    GameOver,
    Large,
    Medium,
    Mini,
    ProgressBar,
    Tip,
    Popup,
    FlashingWarning
}

public delegate void SendToastWithCustomUI(WarfarePlayer player, in ToastMessage message, ToastMessageInfo info, UnturnedUI ui);
public sealed class ToastMessageInfo
{
    private bool _durationOverridden;
    private float _duration;

    /// <summary>
    /// Toast style this info represents.
    /// </summary>
    public ToastMessageStyle Style { get; }

    /// <summary>
    /// Overlapping toasts are split up into channels. Each channel can only show one toast at a time.
    /// </summary>
    public int Channel { get; }

    /// <summary>
    /// The effect to send to the player.
    /// </summary>
    public IAssetLink<EffectAsset> Asset { get; private set; }
    
    /// <summary>
    /// If this toast should inturrupt whatever toast is currently playing instead of queueing after it plays.
    /// </summary>
    public bool Inturrupt { get; }

    /// <summary>
    /// If this effect needs to be cleared when it's <see cref="Duration"/> is over.
    /// </summary>
    /// <remarks>Most effects will use Lifetime = X and Lifetime_Spread = 0 to auto-clear.</remarks>
    public bool RequiresClearing { get; }

    /// <summary>
    /// Should this effect be sent with a high reliability (in terms of how it's networked).
    /// </summary>
    public bool Reliable { get; set; } = true;

    /// <summary>
    /// Unique key for UI that requires clearing or setting values.
    /// </summary>
    public short Key { get; }

    /// <summary>
    /// Optional managed UI to use.
    /// </summary>
    public UnturnedUI? UI { get; }

    /// <summary>
    /// Callback to invoke when sending with a managed <see cref="UI"/>.
    /// </summary>
    public SendToastWithCustomUI? SendCallback { get; }

    /// <summary>
    /// Widget flags to disable when the UI is sent.
    /// </summary>
    public EPluginWidgetFlags DisableFlags { get; set; } = EPluginWidgetFlags.None;
    
    /// <summary>
    /// Widget flags to enable when the UI is sent.
    /// </summary>
    public EPluginWidgetFlags EnableFlags { get; set; } = EPluginWidgetFlags.None;

    /// <summary>
    /// Names of all text components in order of their arguments ({0}, etc). <see cref="CanResend"/> must be set to <see langword="true"/> for this to work.
    /// </summary>
    /// <remarks>Embedding plugin keys only works when the value is sent separately.</remarks>
    public string[] ResendNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// If <see cref="ResendNames"/> have been configured to allow resending the text components when 
    /// </summary>
    public bool CanResend { get; }
    public bool RequiresResend { get; }

    /// <summary>
    /// Number of actual slots in the UI that will be sent an empty string if not supplied as an argument.
    /// </summary>
    public int ClearableSlots { get; set; }
    public float Duration
    {
        get => _duration;
        set
        {
            _duration = value;
            _durationOverridden = true;
        }
    }
    public ToastMessageInfo(ToastMessageStyle style, int channel, UnturnedUI ui, SendToastWithCustomUI sendCallbackAction, bool requiresClearing = false, bool inturrupt = false)
    {
        UI = ui;
        SendCallback = sendCallbackAction;
        Style = style;
        Channel = channel;
        RequiresClearing = requiresClearing;
        Inturrupt = inturrupt;
        Key = UI.Key;
        CanResend = false;
        UpdateAsset(AssetLink.Create(ui.Asset));
    }
    public ToastMessageInfo(ToastMessageStyle style, int channel, IAssetLink<EffectAsset> asset, bool requiresClearing = false, bool inturrupt = false, bool canResend = false, bool requiresResend = false)
        : this(style, channel, requiresClearing, inturrupt, canResend, requiresResend)
    {
        UpdateAsset(asset);
    }
    public ToastMessageInfo(ToastMessageStyle style, int channel, bool requiresClearing = false, bool inturrupt = false, bool canResend = false, bool requiresResend = false)
    {
        Style = style;
        Channel = channel;
        RequiresClearing = requiresClearing;
        Inturrupt = inturrupt;
        CanResend = canResend;
        RequiresResend = requiresResend && canResend;
        Key = requiresClearing || canResend ? UnturnedUIKeyPool.Claim() : (short)-1;
    }
    public void UpdateAsset(IAssetLink<EffectAsset> assetContainer)
    {
        Asset = assetContainer;
        UI?.LoadFromConfig(Asset);
        if (!Assets.isLoading)
        {
            OnLevelLoaded(Level.BUILD_INDEX_GAME);
        }
        else
        {
            Level.onPrePreLevelLoaded += OnLevelLoaded;
        }
    }

    private void OnLevelLoaded(int level)
    {
        if (level != Level.BUILD_INDEX_GAME)
            return;

        if (Asset.TryGetAsset(out EffectAsset? asset))
        {
            if (!_durationOverridden)
                _duration = asset.lifetime;
            UI?.LoadFromConfig(Asset);
        }
        else
        {
            L.LogError($"Unknown asset for toast message: {Style}, {Asset?.ToString() ?? "Not Defined"}.");
        }

        Level.onLevelLoaded -= OnLevelLoaded;
    }
}