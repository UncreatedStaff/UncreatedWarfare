using SDG.NetTransport;
using System;
using System.Drawing;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Traits;
public class BuffUI : UnturnedUI
{
    public const int MAX_BUFFS = 6;
    public const string DEFAULT_BUFF_ICON = "±";
    public UnturnedUIElement Buff1 = new UnturnedUIElement("Buff1");
    public UnturnedUIElement Buff2 = new UnturnedUIElement("Buff2");
    public UnturnedUIElement Buff3 = new UnturnedUIElement("Buff3");
    public UnturnedUIElement Buff4 = new UnturnedUIElement("Buff4");
    public UnturnedUIElement Buff5 = new UnturnedUIElement("Buff5");
    public UnturnedUIElement Buff6 = new UnturnedUIElement("Buff6");

    public UnturnedLabel SolidIcon1 = new UnturnedLabel("Buff1_Solid");
    public UnturnedLabel SolidIcon2 = new UnturnedLabel("Buff2_Solid");
    public UnturnedLabel SolidIcon3 = new UnturnedLabel("Buff3_Solid");
    public UnturnedLabel SolidIcon4 = new UnturnedLabel("Buff4_Solid");
    public UnturnedLabel SolidIcon5 = new UnturnedLabel("Buff5_Solid");
    public UnturnedLabel SolidIcon6 = new UnturnedLabel("Buff6_Solid");

    public UnturnedLabel BlinkingIcon1 = new UnturnedLabel("Buff1_Blinking");
    public UnturnedLabel BlinkingIcon2 = new UnturnedLabel("Buff2_Blinking");
    public UnturnedLabel BlinkingIcon3 = new UnturnedLabel("Buff3_Blinking");
    public UnturnedLabel BlinkingIcon4 = new UnturnedLabel("Buff4_Blinking");
    public UnturnedLabel BlinkingIcon5 = new UnturnedLabel("Buff5_Blinking");
    public UnturnedLabel BlinkingIcon6 = new UnturnedLabel("Buff6_Blinking");

    public UnturnedUIElement[] Parents;
    public UnturnedLabel[]     SolidIcons;
    public UnturnedLabel[]     BlinkingIcons;
    public BuffUI() : base(12013, Gamemode.Config.UI.BuffUI, true, false)
    {
        Parents = new UnturnedUIElement[MAX_BUFFS]
        {
            Buff1, Buff2, Buff3, Buff4, Buff5, Buff6
        };
        SolidIcons = new UnturnedLabel[MAX_BUFFS]
        {
            SolidIcon1, SolidIcon2, SolidIcon3, SolidIcon4, SolidIcon5, SolidIcon6
        };
        BlinkingIcons = new UnturnedLabel[MAX_BUFFS]
        {
            BlinkingIcon1, BlinkingIcon2, BlinkingIcon3, BlinkingIcon4, BlinkingIcon5, BlinkingIcon6
        };
    }

    public void SendBuffs(UCPlayer player)
    {
        ITransportConnection c = player.Connection;
        SendToPlayer(c);
        for (int i = 0; i < MAX_BUFFS; ++i)
        {
            IBuff? buff = player.ActiveBuffs[i];
            if (buff != null)
            {
                string icon = buff.Icon;
                SolidIcons[i].SetText(c, icon);
                SolidIcons[i].SetVisibility(c, !buff.IsBlinking);
                BlinkingIcons[i].SetVisibility(c, buff.IsBlinking);
                Parents[i].SetVisibility(c, true);
            }
            else break;
        }
    }
    public bool AddBuff(UCPlayer player, IBuff buff)
    {
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
            if (ind == -1)
                return false; // no room
            string icon = buff.Icon;
            ITransportConnection c = player.Connection;

            SolidIcons[ind].SetText(c, icon);
            SolidIcons[ind].SetVisibility(c, !buff.IsBlinking);
            BlinkingIcons[ind].SetVisibility(c, buff.IsBlinking);
            Parents[ind].SetVisibility(c, true);
            player.ActiveBuffs[ind] = buff;
        }
        return true;
    }

    public bool RemoveBuff(UCPlayer player, IBuff buff)
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

            for (int i = ind; i < MAX_BUFFS; ++i)
            {
                IBuff? b = player.ActiveBuffs[i];
                if (b == null)
                    break;

                if (i == MAX_BUFFS - 1)
                {
                    Parents[i].SetVisibility(c, false);
                    player.ActiveBuffs[i] = null;
                    break;
                }
                else
                {
                    IBuff? next = player.ActiveBuffs[i + 1];
                    if (next != null)
                    {
                        string icon = next.Icon;
                        SolidIcons[i].SetText(c, icon);
                        SolidIcons[ind].SetVisibility(c, !buff.IsBlinking);
                        BlinkingIcons[ind].SetVisibility(c, buff.IsBlinking);
                        Parents[i].SetVisibility(c, true);
                        player.ActiveBuffs[i] = next;
                    }
                    else
                    {
                        player.ActiveBuffs[i] = null;
                        Parents[i].SetVisibility(c, false);
                        break;
                    }
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
        UpdateBuffTimeState(buff, buff.Player, blink);
    }
    private void UpdateBuffTimeState(IBuff buff, UCPlayer player, bool isBlinking)
    {
        for (int i = 0; i < MAX_BUFFS; ++i)
        {
            if (player.ActiveBuffs[i] == buff)
            {
                ITransportConnection c = player.Connection;
                (isBlinking ? BlinkingIcons : SolidIcons)[i].SetText(c, buff.Icon);
                SolidIcons[i].SetVisibility(c, !isBlinking);
                BlinkingIcons[i].SetVisibility(c, isBlinking);
                return;
            }
        }
    }
}
