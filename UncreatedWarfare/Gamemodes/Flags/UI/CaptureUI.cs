using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Framework.UI;

namespace Uncreated.Warfare.Gamemodes.Flags.UI;
public class CaptureUI : UnturnedUI
{
    public readonly UnturnedLabel Background = new UnturnedLabel("BackgroundCircle");
    public readonly UnturnedLabel Foreground = new UnturnedLabel("ForegroundCircle");
    public readonly UnturnedUIElement T1CountIcon = new UnturnedUIElement("T1CountIcon");
    public readonly UnturnedUIElement T1Count = new UnturnedLabel("T1Count");
    public readonly UnturnedUIElement T2CountIcon = new UnturnedUIElement("T2CountIcon");
    public readonly UnturnedUIElement T2Count = new UnturnedLabel("T2Count");
    public readonly UnturnedLabel Status = new UnturnedLabel("Status");
    public CaptureUI() : base(12005, Gamemode.Config.UI.CaptureGUID, true, false) { }
}
