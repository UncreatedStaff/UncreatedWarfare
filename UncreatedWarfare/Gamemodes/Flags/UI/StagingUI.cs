using SDG.NetTransport;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;

namespace Uncreated.Warfare.Gamemodes.Flags.UI;

[UnturnedUI(BasePath = "Canvas")]
public class StagingUI : UnturnedUI
{
    public readonly UnturnedLabel Top = new UnturnedLabel("Top");
    public readonly UnturnedLabel Bottom = new UnturnedLabel("Bottom");
    public StagingUI() : base(Gamemode.Config.UIHeader.AsAssetContainer()) { }
    public void SetText(ITransportConnection connection, string top, string bottom)
    {
        Top.SetText(connection, top);
        Bottom.SetText(connection, bottom);
    }
}
