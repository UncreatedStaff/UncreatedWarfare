using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Framework.UI;

namespace Uncreated.Warfare.Squads.UI;
public class SquadOrderUI : UnturnedUI
{
    public readonly UnturnedLabel OrderInfo = new UnturnedLabel("OrderInfo");
    public readonly UnturnedLabel OrderText = new UnturnedLabel("Order");
    public readonly UnturnedLabel TimeLeft  = new UnturnedLabel("Time");
    public readonly UnturnedLabel Reward    = new UnturnedLabel("Reward");

    public SquadOrderUI() : base(12004, SquadManager.Config?.Data?.SquadRallyUI, true, false) { }
    public void SetOrder(UCPlayer player, Order order)
    {
        ITransportConnection c = player.Connection;
        OrderInfo.SetText(c, Translation.Translate("order_ui_commander", player, order.Commander.CharacterName));
        OrderText.SetText(c, Translation.Translate("order_ui_text", player, order.Message));
        OrderText.SetText(c, Translation.Translate("order_ui_time", player, order.MinutesLeft));
        OrderText.SetText(c, Translation.Translate("order_ui_reward", player, order.RewardLevel));
    }
}
