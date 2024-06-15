using SDG.NetTransport;
using Uncreated.Framework.UI;

namespace Uncreated.Warfare.Gamemodes.Flags.UI;
public class StagingUI : UnturnedUI
{
    public readonly UnturnedLabel Top = new UnturnedLabel("Canvas/Content/Top");
    public readonly UnturnedLabel Bottom = new UnturnedLabel("Canvas/Content/Bottom");
    public StagingUI() : base(Gamemode.Config.UIHeader.GetId()) { }
    public void SetText(ITransportConnection connection, string top, string bottom)
    {
        Top.SetText(connection, top);
        Bottom.SetText(connection, bottom);
    }
}
