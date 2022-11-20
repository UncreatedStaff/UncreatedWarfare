using SDG.NetTransport;
using System;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Squads.UI;
public class SquadOrderUI : UnturnedUI
{
    public readonly UnturnedLabel OrderInfo = new UnturnedLabel("OrderInfo");
    public readonly UnturnedLabel OrderText = new UnturnedLabel("Order");
    public readonly UnturnedLabel TimeLeft = new UnturnedLabel("Time");
    public readonly UnturnedLabel Reward = new UnturnedLabel("Reward");

    public SquadOrderUI() : base(12004, Gamemode.Config.UIOrder, true, false) { }
    public void SetOrder(UCPlayer player, Order order)
    {
        ITransportConnection c = player.Connection;
        OrderInfo.SetText(c, Localization.Translate(T.OrderUICommander, player, order.Commander));
        OrderText.SetText(c, Localization.Translate(T.OrderUIMessage, player, order));
        OrderText.SetText(c, Localization.Translate(T.OrderUITimeLeft, player, TimeSpan.FromSeconds(order.TimeLeft)));
        OrderText.SetText(c, Localization.Translate(T.OrderUIReward, player, order.RewardLevel));
    }
}
