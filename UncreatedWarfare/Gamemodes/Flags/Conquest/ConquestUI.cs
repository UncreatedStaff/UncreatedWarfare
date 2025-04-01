using SDG.NetTransport;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;

namespace Uncreated.Warfare.Gamemodes.Flags;
public static class ConquestUI
{
    public static void SendFlagList(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player == null || player.HasUIHidden) return;
        ulong team = player.GetTeam();
        if (team < 1 || team > 3) return;
        if (Data.Is(out IFlagRotation gm))
        {
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            List<Flag> rotation = gm.Rotation;
            CTFUI.ListUI.SendToPlayer(c);
            CTFUI.ListUI.Header.SetVisibility(c, true);
            CTFUI.ListUI.Header.SetText(c, T.FlagsHeader.Translate(player));
            if (team == 1 || team == 2)
            {
                for (int i = 0; i < CTFUI.ListUI.Parents.Length; i++)
                {
                    if (rotation.Count <= i)
                        CTFUI.ListUI.Parents[i].SetVisibility(c, false);
                    else
                    {
                        CTFUI.ListUI.Parents[i].SetVisibility(c, true);
                        int index = team == 1 ? i : rotation.Count - i - 1;
                        Flag flag = rotation[index];
                        string objective;
                        if (flag.Owner == 0)
                            objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UIIconAttack}</color>";
                        else if (flag.Owner == team)
                            objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UIIconDefend}</color>";
                        else
                            objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UIIconAttack}</color>";

                        CTFUI.ListUI.Names[i].SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>");
                        CTFUI.ListUI.Icons[i].SetText(c, objective);
                    }
                }
            }
            else if (team == 3)
            {
                for (int i = 0; i < CTFUI.ListUI.Parents.Length; i++)
                {
                    if (rotation.Count <= i)
                        CTFUI.ListUI.Parents[i].SetVisibility(c, false);
                    else
                    {
                        CTFUI.ListUI.Parents[i].SetVisibility(c, true);
                        Flag flag = rotation[i];
                        string objective = flag.Owner switch
                        {
                            0 => $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconAttack}</color>" +
                                 $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconAttack}</color>",
                            1 => $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconDefend}</color>" +
                                 $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconAttack}</color>",
                            2 => $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconAttack}</color>" +
                                 $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconDefend}</color>",
                            _ => string.Empty
                        };
                        CTFUI.ListUI.Names[i].SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>");
                        CTFUI.ListUI.Icons[i].SetText(c, objective);
                    }
                }
            }
        }
    }

    public static void UpdateFlag(Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Is(out IFlagRotation gm))
        {
            int index = gm.Rotation.IndexOf(flag);
            if (index == -1) return;
            int indt2 = gm.Rotation.Count - index - 1;
            string obj1;
            string obj2;
            string name = $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>";
            if (flag.Owner == 0)
                obj2 = obj1 = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UIIconAttack}</color>";
            else
            {
                obj1 = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UIIconDefend}</color>";
                obj2 = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UIIconAttack}</color>";
                if (flag.Owner == 2)
                    (obj1, obj2) = (obj2, obj1);
            }

            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.HasUIHidden) continue;
                ulong team = pl.GetTeam();
                int i2 = team == 2 ? indt2 : index;
                CTFUI.ListUI.Names[i2].SetText(pl.Connection, name);
                if (team == 1 || team == 2)
                {
                    CTFUI.ListUI.Icons[i2].SetText(pl.Connection, team == 2 ? obj2 : obj1);
                }
                else if (team == 3)
                {
                    string obj3 = flag.Owner switch
                    {
                        0 => $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconAttack}</color>" +
                             $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconAttack}</color>",
                        1 => $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconDefend}</color>" +
                             $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconAttack}</color>",
                        2 => $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconAttack}</color>" +
                             $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconDefend}</color>",
                        _ => string.Empty
                    };
                    CTFUI.ListUI.Icons[i2].SetText(pl.Connection, obj3);
                }
            }
        }
    }
    public static CaptureUIParameters ComputeUI(ulong team, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        if (flag.IsContested(out _))
        {
            return new CaptureUIParameters(team, EFlagStatus.CONTESTED, flag);
        }

        if (flag.Points == 0)
            return new CaptureUIParameters(team, EFlagStatus.NEUTRALIZED, flag);
        if (flag.Owner == 0)
            return new CaptureUIParameters(team, EFlagStatus.CAPTURING, flag);
        else if (flag.Owner == team)
            return new CaptureUIParameters(team, Mathf.Abs(flag.Points) < Flag.MaxPoints ? EFlagStatus.CAPTURING : EFlagStatus.SECURED, flag);
        else
            return new CaptureUIParameters(team, EFlagStatus.CLEARING, flag);
    }
}
