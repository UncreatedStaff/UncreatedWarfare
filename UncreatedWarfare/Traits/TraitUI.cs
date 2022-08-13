using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Traits;
public class TraitUI : UnturnedUI
{
    public TraitUI() : base(12013, Gamemode.Config.UI.TraitUI, true, false)
    {

    }

    public void SendTraits(UCPlayer player, bool isNew)
    {
        if (isNew)
            SendToPlayer(player.Connection);
        throw new NotImplementedException();
    }
}
