using SDG.NetTransport;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Traits;
#if false
public class BuffUI : UnturnedUI
{
    public const int MaxBuffs = 8;
    public const int ReservedBuffs = 2;
    public const string DefaultBuffIcon = "±";
    
    public readonly BuffIcon[] Buffs = ElementPatterns.CreateArray<BuffIcon>("Canvas/GameObject/Buff{0}", 1, length: MaxBuffs);
    public BuffUI(AssetConfiguration assetConfig) : base(assetConfig.GetAssetLink<EffectAsset>("UI:Buffs")) { }
    public void SendBuffs(WarfarePlayer player)
    {
        ITransportConnection c = player.Connection;
        SendToPlayer(c);
        for (int i = 0; i < MaxBuffs; ++i)
        {
            IBuff? buff = player.ActiveBuffs[i];
            if (buff != null)
            {
                string icon = buff.Icon;

                BuffIcon ui = Buffs[i];

                ui.Solid.SetText(c, icon);
                ui.Solid.SetVisibility(c, !buff.IsBlinking);
                ui.Blinking.SetVisibility(c, buff.IsBlinking);
                ui.Root.SetVisibility(c, true);
            }
            else break;
        }
    }
    public bool AddBuff(WarfarePlayer player, IBuff buff)
    {
        bool res = buff.Reserved;
        lock (player.ActiveBuffs)
        {
            int ind = -1;
            for (int i = 0; i < player.ActiveBuffs.Length; ++i)
            {
                if (player.ActiveBuffs[i] == buff)
                    return false; // already added
                else if (player.ActiveBuffs[i] == null)
                {
                    ind = i;
                    break;
                }
            }
            if (ind == -1 || !res && ind < MaxBuffs - ReservedBuffs - 1)
                return false; // no room

            string icon = buff.Icon;
            ITransportConnection c = player.Connection;

            BuffIcon ui = Buffs[ind];

            (buff.IsBlinking ? ui.Blinking : ui.Solid).SetText(c, icon);
            ui.Solid.SetVisibility(c, !buff.IsBlinking);
            ui.Blinking.SetVisibility(c, buff.IsBlinking);
            ui.Root.SetVisibility(c, true);
            player.ActiveBuffs[ind] = buff;
        }
        return true;
    }
    public static bool HasBuffRoom(WarfarePlayer player, bool reserved)
    {
        lock (player.ActiveBuffs)
        {
            for (int i = 0; i < player.ActiveBuffs.Length; ++i)
            {
                IBuff? buff = player.ActiveBuffs[i];
                if (buff == null)
                {
                    if (reserved || i < MaxBuffs - ReservedBuffs)
                        return true;
                }
            }
        }
        return false;
    }

    public bool RemoveBuff(WarfarePlayer player, IBuff buff)
    {
        lock (player.ActiveBuffs)
        {
            int ind = -1;
            for (int i = 0; i < player.ActiveBuffs.Length; ++i)
            {
                if (player.ActiveBuffs[i] == buff)
                {
                    ind = i;
                    break;
                }
            }

            if (ind == -1)
                return false; // not added

            ITransportConnection c = player.Connection;

            for (int i = ind; i < MaxBuffs; ++i)
            {
                IBuff? b = player.ActiveBuffs[i];
                if (b == null)
                    break;

                if (i == MaxBuffs - 1)
                {
                    Buffs[i].Root.SetVisibility(c, false);
                    player.ActiveBuffs[i] = null;
                    break;
                }

                BuffIcon ui = Buffs[i];

                IBuff? next = player.ActiveBuffs[i + 1];
                if (next != null)
                {
                    (next.IsBlinking ? ui.Blinking : ui.Solid).SetText(c, next.Icon);
                    ui.Solid.SetVisibility(c, !next.IsBlinking);
                    ui.Blinking.SetVisibility(c, next.IsBlinking);
                    ui.Root.SetVisibility(c, true);
                    player.ActiveBuffs[i] = next;
                }
                else
                {
                    player.ActiveBuffs[i] = null;
                    ui.Solid.SetText(c, buff.Icon);
                    ui.Solid.SetVisibility(c, true);
                    ui.Blinking.SetVisibility(c, false);
                    ui.Root.SetVisibility(c, false);
                    break;
                }
            }
        }
        return true;
    }
    internal void UpdateBuffTimeState(IBuff buff)
    {
        bool blink = buff.IsBlinking;
        if (buff is Buff b2)
        {
            Squad? sq = b2.TargetPlayer.Squad;
            if (b2.Data.EffectDistributedToSquad && sq is not null)
            {
                for (int i = 0; i < sq.Members.Count; ++i)
                    UpdateBuffTimeState(b2, sq.Members[i], blink);
                return;
            }
        }
        if (buff.Player.IsOnline)
            UpdateBuffTimeState(buff, buff.Player, blink);
    }
    private void UpdateBuffTimeState(IBuff buff, WarfarePlayer player, bool isBlinking)
    {
        for (int i = 0; i < MaxBuffs; ++i)
        {
            if (player.ActiveBuffs[i] != buff)
                continue;
            
            BuffIcon ui = Buffs[i];

            ITransportConnection c = player.Connection;
            (isBlinking ? ui.Blinking : ui.Solid).SetText(c, buff.Icon);
            ui.Solid.SetVisibility(c, !isBlinking);
            ui.Blinking.SetVisibility(c, isBlinking);
            return;
        }
    }
    public class BuffIcon
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("Buff{0}_Solid")]
        public UnturnedLabel Solid { get; set; }

        [Pattern("Buff{0}_Blinking")]
        public UnturnedLabel Blinking { get; set; }
    }
}
#endif