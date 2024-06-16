using SDG.NetTransport;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;

namespace Uncreated.Warfare.Gamemodes.Flags.Invasion;

public static class InvasionUI
{
    public static CaptureUIParameters ComputeUI(ulong team, Flag flag, bool inVehicle, ulong atkTeam)
    {
        if (flag.Owner == atkTeam)
        {
            if (team == atkTeam)
                return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
            else
                return new CaptureUIParameters(team, EFlagStatus.LOCKED, flag);
        }

        if (flag.Points == Flag.MaxPoints)
        // flag is fully capped by team 1
        {
            if (flag.IsCapturable(2))
            {
                if (flag.IsContested(out ulong winner))
                    return new CaptureUIParameters(team, EFlagStatus.CONTESTED, flag);
                else
                {
                    if (winner == 1)
                    {
                        if (team == 1)
                            return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
                        else if (team == 2)
                            return new CaptureUIParameters(team, EFlagStatus.LOST, flag);

                        return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
                    }
                    else if (winner == 2)
                    {
                        if (team == 1)
                            return new CaptureUIParameters(team, EFlagStatus.CLEARING, flag);
                        else if (team == 2)
                            return new CaptureUIParameters(team, EFlagStatus.LOSING, flag);

                        return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
                    }

                    return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
                }
            }
            else
            {
                if (team == 1)
                    return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
                else if (team == 2)
                    return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);

                return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
            }
        }
        else if (flag.Points == -Flag.MaxPoints)
        // flag is fully capped by team 2
        {
            if (flag.IsCapturable(1))
            {
                if (flag.IsContested(out ulong winner))
                    return new CaptureUIParameters(team, EFlagStatus.CONTESTED, flag);
                else
                {
                    if (winner == 2)
                    {
                        if (team == 2)
                            return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
                        else if (team == 1)
                            return new CaptureUIParameters(team, EFlagStatus.LOST, flag);

                        return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
                    }
                    else if (winner == 1)
                    {
                        if (team == 2)
                            return new CaptureUIParameters(team, EFlagStatus.CLEARING, flag);
                        else if (team == 1)
                            return new CaptureUIParameters(team, EFlagStatus.LOSING, flag);

                        return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
                    }

                    return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
                }
            }
            else
            {
                if (team == 2)
                    return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
                else if (team == 1)
                    return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);

                return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
            }
        }
        else if (flag.Points > 0)
        // cap is on team 1's side
        {
            if (flag.LastDeltaPoints > 0)
            // team 1 is capping
            {
                if (team == 1)
                    return new CaptureUIParameters(team, EFlagStatus.CAPTURING, flag);
                else if (team == 2)
                    return new CaptureUIParameters(team, EFlagStatus.LOSING, flag);

                return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
            }
            else if (flag.LastDeltaPoints < 0)
            // team 2 is capping
            {
                if (team == 1)
                    return new CaptureUIParameters(team, EFlagStatus.LOSING, flag);
                else if (team == 2)
                    return new CaptureUIParameters(team, EFlagStatus.CLEARING, flag);

                return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
            }
            else
            // no cap
            {
                if (flag.IsContested(out _))
                    return new CaptureUIParameters(team, EFlagStatus.CONTESTED, flag);
                else
                    return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
            }
        }
        else if (flag.Points < 0)
        // cap is on team 2's side
        {
            if (flag.LastDeltaPoints > 0)
            // team 1 is capping
            {
                if (team == 1)
                    return new CaptureUIParameters(team, EFlagStatus.CLEARING, flag);
                else if (team == 2)
                    return new CaptureUIParameters(team, EFlagStatus.LOSING, flag);
                else
                    return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
            }
            else if (flag.LastDeltaPoints < 0)
            // team 2 is capping
            {
                if (team == 1)
                    return new CaptureUIParameters(team, EFlagStatus.LOSING, flag);
                else if (team == 2)
                    return new CaptureUIParameters(team, EFlagStatus.CAPTURING, flag);
                else
                    return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
            }
            else
            // no cap
            {
                if (flag.IsContested(out _))
                    return new CaptureUIParameters(team, EFlagStatus.CONTESTED, flag);
                else
                    return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
            }
        }
        else
        // flag is neutral
        {
            if (flag.IsCapturable(team))
                return new CaptureUIParameters(team, EFlagStatus.NEUTRALIZED, flag);
            else
                return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
        }
    }
    public static void SendFlagList(UCPlayer player)
    {
        if (player == null || player.HasUIHidden) return;
        ulong team = player.GetTeam();
        if (team < 1 || team > 3) return;
        if (Data.Is(out IFlagRotation gm) && Data.Is(out IAttackDefense atkdef))
        {
            ulong attack = atkdef.AttackingTeam;
            ulong defense = atkdef.DefendingTeam;
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            List<Flag> rotation = gm.Rotation;
            CTFUI.ListUI.SendToPlayer(c);
            CTFUI.ListUI.Header.SetVisibility(c, true);
            CTFUI.ListUI.Header.SetText(c, T.FlagsHeader.Translate(player));
            if (team == 1 || team == 2)
            {
                for (int i = 0; i < CTFUI.ListUI.Rows.Length; i++)
                {
                    if (rotation.Count <= i)
                    {
                        CTFUI.ListUI.Rows[i].Root.SetVisibility(c, false);
                    }
                    else
                    {
                        CTFUI.ListUI.Rows[i].Root.SetVisibility(c, true);
                        int index = team == 1 ? i : rotation.Count - i - 1;
                        Flag flag = rotation[index];
                        string objective = string.Empty;
                        if (flag.Owner == attack)
                        {
                            objective = $"<color=#{UCWarfare.GetColorHex("locked_icon_color")}>{Gamemode.Config.UIIconLocked}</color>";
                        }
                        else
                        {
                            if (flag.IsObj(attack))
                            {
                                if (team == attack)
                                    objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UIIconAttack}</color>";
                                else
                                    objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UIIconDefend}</color>";
                            }
                        }
                        CTFUI.ListUI.Rows[i].Name.SetText(c,
                            flag.Discovered(team)
                            ? $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>"
                            : T.UndiscoveredFlag.Translate(player)
                        );
                        CTFUI.ListUI.Rows[i].Icon.SetText(c, objective);
                    }
                }
            }
            else if (team == 3)
            {
                for (int i = 0; i < CTFUI.ListUI.Rows.Length; i++)
                {
                    if (rotation.Count <= i)
                    {
                        CTFUI.ListUI.Rows[i].Root.SetVisibility(c, false);
                    }
                    else
                    {
                        CTFUI.ListUI.Rows[i].Root.SetVisibility(c, true);
                        Flag flag = rotation[i];
                        string objective = string.Empty;
                        if (flag.IsObj(attack))
                        {
                            objective = $"<color=#{TeamManager.GetTeamHexColor(attack)}>{Gamemode.Config.UIIconAttack}</color> <color=#{TeamManager.GetTeamHexColor(defense)}>{Gamemode.Config.UIIconDefend}</color>";
                        }
                        if (flag.Owner == attack)
                            objective = $"<color=#{TeamManager.GetTeamHexColor(defense)}>{Gamemode.Config.UIIconLocked}</color>";
                        if (flag.T2Obj)
                        {
                            objective = $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconAttack}</color>";
                            if (flag.Owner == 1)
                                objective += $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconDefend}</color>";
                        }
                        CTFUI.ListUI.Rows[i].Name.SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                                                             $"{(flag.Discovered(1) ? string.Empty : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                                                             $"{(flag.Discovered(2) ? string.Empty : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                        CTFUI.ListUI.Rows[i].Icon.SetText(c, objective);
                    }
                }
            }
        }
    }
    public static void ReplicateFlagUpdate(Flag flag, bool ownerChanged = true)
    {
        if (Data.Is(out IFlagRotation gm) && Data.Is(out IAttackDefense atkdef))
        {
            ulong attack = atkdef.AttackingTeam;
            ulong defense = atkdef.DefendingTeam;
            List<Flag> rotation = gm.Rotation;
            int index = rotation.IndexOf(flag);
            if (index == -1) return;
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer player = PlayerManager.OnlinePlayers[i];
                ulong team = player.GetTeam();
                if (team < 1 || team > 3 || player.HasUIHidden) continue;
                if (!flag.Discovered(team)) continue;
                ITransportConnection c = player.Player.channel.owner.transportConnection;
                int i3 = team == 2 ? rotation.Count - index - 1 : index;
                string objective = string.Empty;
                if (team == 1 || team == 2)
                {
                    if (flag.Owner == attack)
                    {
                        objective = $"<color=#{UCWarfare.GetColorHex("locked_icon_color")}>{Gamemode.Config.UIIconLocked}</color>";
                    }
                    else
                    {
                        if (flag.IsObj(attack))
                        {
                            if (team == attack)
                                objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UIIconAttack}</color>";
                            else
                                objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UIIconDefend}</color>";
                        }
                    }
                    if (ownerChanged)
                        CTFUI.ListUI.Rows[i3].Name.SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>");
                    CTFUI.ListUI.Rows[i3].Icon.SetText(c, objective);
                }
                else
                {
                    if (flag.IsObj(attack))
                    {
                        objective = $"<color=#{TeamManager.GetTeamHexColor(attack)}>{Gamemode.Config.UIIconAttack}</color> <color=#{TeamManager.GetTeamHexColor(defense)}>{Gamemode.Config.UIIconDefend}</color>";
                    }
                    if (flag.Owner == attack)
                        objective = $"<color=#{UCWarfare.GetColorHex("locked_icon_color")}>{Gamemode.Config.UIIconLocked}</color>";
                    if (flag.T2Obj)
                    {
                        objective = $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconAttack}</color>";
                        if (flag.Owner == 1)
                            objective += $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconDefend}</color>";
                    }
                    if (ownerChanged)
                        CTFUI.ListUI.Rows[i3].Name.SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                                                              $"{(flag.Discovered(1) ? "" : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                                                              $"{(flag.Discovered(2) ? "" : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                    CTFUI.ListUI.Rows[i3].Icon.SetText(c, objective);
                }
            }
        }
    }
    public static CaptureUIParameters RefreshStaticUI(ulong team, Flag flag, bool inVehicle, ulong atkTeam)
    {
        if (team != 1 && team != 2) return new CaptureUIParameters(0, EFlagStatus.DONT_DISPLAY, null);
        if (flag.IsObj(atkTeam))
        {
            return ComputeUI(team, flag, inVehicle, atkTeam); // if flag is objective send capturing ui.
        }
        else
        {
            if (flag.Owner == team)
            {
                return new CaptureUIParameters(team, EFlagStatus.SECURED, flag);
            }
            else if (flag.Owner == TeamManager.Other(team))
            {
                if (flag.Owner == TeamManager.Other(atkTeam))
                {
                    return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
                }
                else
                {
                    return new CaptureUIParameters(team, EFlagStatus.LOCKED, flag);
                }
            }
            else
            {
                return new CaptureUIParameters(team, EFlagStatus.INEFFECTIVE, flag);
            }
        }
    }
}