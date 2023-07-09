using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Moderation;
public class ModerationUI : UnturnedUI
{
    public static readonly ModerationUI Instance = new ModerationUI();
    public ModerationUI() : base(Gamemode.Config.UIModerationMenu) { }
}
