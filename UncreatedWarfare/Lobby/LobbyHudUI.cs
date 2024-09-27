using SDG.NetTransport;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Lobby;

[UnturnedUI(BasePath = "Container")]
public class LobbyHudUI : UnturnedUI
{
    public readonly UnturnedLabel FactionName = new UnturnedLabel("FactionName");
    public readonly UnturnedLabel[] FactionInfo = ElementPatterns.CreateArray<UnturnedLabel>("FactionBar/FactionInfo_{0}", 0, to: 25);
    public readonly UnturnedUIElement LogicClear = new UnturnedUIElement("~/Logic_Clear");
    public LobbyHudUI(AssetConfiguration assetConfig, ILoggerFactory loggerFactory) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:LobbyHud")) { }

    /// <summary>
    /// Sets <see cref="FactionInfo"/> #s 2-25 inclusive to empty text.
    /// </summary>
    public void ResetInfo(ITransportConnection transportConnection)
    {
        LogicClear.SetVisibility(transportConnection, true);
    }
}