using SDG.Unturned;
using System;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Configuration;

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

public delegate void SendToastWithCustomUI(UCPlayer player, in ToastMessage message, ToastMessageInfo info, UnturnedUI ui);
public sealed class ToastMessageInfo
{
    private bool _durationOverridden;
    private float _duration;
    public ToastMessageStyle Style { get; }
    public int Channel { get; }
    public Guid Guid { get; private set; }
    public ushort Id { get; private set; }
    public EffectAsset? Asset { get; private set; }
    public bool Inturrupt { get; }
    public bool RequiresClearing { get; }
    public bool Reliable { get; set; } = true;
    public short Key { get; }
    public UnturnedUI? UI { get; }
    public SendToastWithCustomUI? SendCallback { get; }
    public EPluginWidgetFlags DisableFlags { get; set; } = EPluginWidgetFlags.None;
    public EPluginWidgetFlags EnableFlags { get; set; } = EPluginWidgetFlags.None;
    public string[] ResendNames { get; set; } = Array.Empty<string>();
    public bool CanResend { get; }
    public bool RequiresResend { get; }
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
        Key = requiresClearing || inturrupt || canResend ? UnturnedUIKeyPool.Claim() : (short)-1;
    }
    public void UpdateAsset(IAssetContainer assetContainer)
    {
        Asset = assetContainer.Asset as EffectAsset;
        UI?.LoadFromConfig(assetContainer);
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

        if (Asset != null)
        {
            if (!_durationOverridden)
                _duration = Asset.lifetime;
            Guid = Asset.GUID;
            Id = Asset.id;
            UI?.LoadFromConfig(Asset);
        }
        else
        {
            L.LogError($"Unknown asset for toast message: {Style}, {Asset?.ToString() ?? "Not Defined"}.");
        }

        Level.onLevelLoaded -= OnLevelLoaded;
    }
}