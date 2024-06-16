using SDG.NetTransport;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags.UI;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF;

public static class CTFUI
{
    public static readonly CaptureUI CaptureUI = new CaptureUI();
    public static readonly FlagListUI ListUI = new FlagListUI();
    public static readonly StagingUI StagingUI = new StagingUI();
    public static int FromMax(int cap) => FromMax(cap, Mathf.RoundToInt(Flag.MaxPoints));
    public static int FromMax(int cap, int max) => Math.Abs(cap) >= max ? Gamemode.Config.UICircleFontCharacters.Length - 1 : (Gamemode.Config.UICircleFontCharacters.Length - 1) / max * Math.Abs(cap);
    public static CaptureUIParameters ComputeUI(ulong team, Flag flag, bool inVehicle)
    {
        if (inVehicle)
            return new CaptureUIParameters(team, EFlagStatus.IN_VEHICLE, flag);

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
    public static void ClearCaptureUI(UCPlayer player)
    {
        CaptureUI.ClearFromPlayer(player.Connection);
    }
    public static void ClearCaptureUI(ITransportConnection player)
    {
        CaptureUI.ClearFromPlayer(player);
    }
    public static void ClearCaptureUI()
    {
        CaptureUI.ClearFromAllPlayers();
    }
    public static void ClearFlagList(UCPlayer player)
    {
        ListUI.ClearFromPlayer(player.Connection);
    }
    public static void ClearFlagList(ITransportConnection player)
    {
        ListUI.ClearFromPlayer(player);
    }
    public static void SendFlagList(UCPlayer player)
    {
        if (player == null || player.HasUIHidden) return;
        ulong team = player.GetTeam();
        if (team < 1 || team > 3) return;
        if (Data.Is(out IFlagRotation gm))
        {
            ITransportConnection c = player.Player.channel.owner.transportConnection;
            List<Flag> rotation = gm.Rotation;
            ListUI.SendToPlayer(c);
            ListUI.Header.SetVisibility(c, true);
            ListUI.Header.SetText(c, T.FlagsHeader.Translate(player));
            if (team == 1 || team == 2)
            {
                for (int i = 0; i < ListUI.Rows.Length; i++)
                {
                    if (rotation.Count <= i)
                    {
                        ListUI.Rows[i].Root.SetVisibility(c, false);
                    }
                    else
                    {
                        ListUI.Rows[i].Root.SetVisibility(c, true);
                        int index = team == 1 ? i : rotation.Count - i - 1;
                        Flag flag = rotation[index];
                        string objective = string.Empty;
                        if (flag.T1Obj)
                        {
                            if (team == 1)
                                objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UIIconAttack}</color>";
                            else if (team == 2 && flag.Owner == 2)
                                objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UIIconDefend}</color>";
                        }
                        if (flag.T2Obj)
                        {
                            if (team == 2)
                                objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UIIconAttack}</color>";
                            else if (team == 1 && flag.Owner == 1)
                                objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UIIconDefend}</color>";
                        }
                        ListUI.Rows[i].Name.SetText(c, flag.Discovered(team) ?
                            $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" :
                            T.UndiscoveredFlag.Translate(player));
                        ListUI.Rows[i].Icon.SetText(c, objective);
                    }
                }
            }
            else if (team == 3)
            {
                for (int i = 0; i < ListUI.Rows.Length; i++)
                {
                    if (rotation.Count <= i)
                    {
                        ListUI.Rows[i].Root.SetVisibility(c, false);
                    }
                    else
                    {
                        ListUI.Rows[i].Root.SetVisibility(c, true);
                        Flag flag = rotation[i];
                        string objective = string.Empty;
                        if (flag.T1Obj)
                        {
                            objective = $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconAttack}</color>";
                            if (flag.Owner == 2)
                                objective += $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconDefend}</color>";
                        }
                        if (flag.T2Obj)
                        {
                            objective = $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconAttack}</color>";
                            if (flag.Owner == 1)
                                objective += $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconDefend}</color>";
                        }
                        ListUI.Rows[i].Name.SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                                                       $"{(flag.Discovered(1) ? string.Empty : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                                                       $"{(flag.Discovered(2) ? string.Empty : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                        ListUI.Rows[i].Icon.SetText(c, objective);
                    }
                }
            }
        }
    }
    public static void ReplicateFlagUpdate(Flag flag, bool ownerChanged = true)
    {
        if (Data.Is(out IFlagRotation gm))
        {
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
                    if (flag.T1Obj)
                    {
                        if (team == 1)
                            objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UIIconAttack}</color>";
                        else if (team == 2 && flag.Owner == 2)
                            objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UIIconDefend}</color>";
                    }
                    if (flag.T2Obj)
                    {
                        if (team == 2)
                            objective = $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Gamemode.Config.UIIconAttack}</color>";
                        else if (team == 1 && flag.Owner == 1)
                            objective = $"<color=#{UCWarfare.GetColorHex("defend_icon_color")}>{Gamemode.Config.UIIconDefend}</color>";
                    }
                    if (ownerChanged)
                        ListUI.Rows[i3].Name.SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>");
                    ListUI.Rows[i3].Icon.SetText(c, objective);
                }
                else
                {
                    if (flag.T1Obj)
                    {
                        objective = $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconAttack}</color>";
                        if (flag.Owner == 2)
                            objective += $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconDefend}</color>";
                    }
                    if (flag.T2Obj)
                    {
                        objective = $"<color=#{TeamManager.Team2ColorHex}>{Gamemode.Config.UIIconAttack}</color>";
                        if (flag.Owner == 1)
                            objective += $"<color=#{TeamManager.Team1ColorHex}>{Gamemode.Config.UIIconDefend}</color>";
                    }
                    ListUI.Rows[i3].Name.SetText(c, $"<color=#{flag.TeamSpecificHexColor}>{flag.Name}</color>" +
                                                    $"{(flag.Discovered(1) ? string.Empty : $" <color=#{TeamManager.Team1ColorHex}>?</color>")}" +
                                                    $"{(flag.Discovered(2) ? string.Empty : $" <color=#{TeamManager.Team2ColorHex}>?</color>")}");
                    ListUI.Rows[i3].Icon.SetText(c, objective);
                }
            }
        }
    }
    public static CaptureUIParameters RefreshStaticUI(ulong team, Flag flag, bool inVehicle)
    {
        if (team != 1 && team != 2) return new CaptureUIParameters(0, EFlagStatus.DONT_DISPLAY, null!);

        return ComputeUI(team, flag, inVehicle);
    }
}

[Translatable("Flag Status", Description = "Displayed on the capturing progress circle UI.")]
public enum EFlagStatus
{
    [Translatable("CAPTURING", Description = "Shown when your team is capturing the flag.")]
    [Translatable(Languages.ChineseSimplified, "占领中")]
    CAPTURING,
    [Translatable(Languages.ChineseSimplified, "失去中")]
    [Translatable("LOSING", Description = "Shown when your team is losing the flag because the other team has more players.")]
    LOSING,
    [Translatable(Languages.ChineseSimplified, "保护")]
    [Translatable("SECURED", Description = "Shown when your team is holding the flag after it has been captured.")]
    SECURED,
    [Translatable(Languages.ChineseSimplified, "中立")]
    [Translatable("NEUTRALIZED", Description = "Shown when the flag has not been captured by either team.")]
    NEUTRALIZED,
    [Translatable(Languages.ChineseSimplified, "已失去")]
    [Translatable("LOST", Description = "Shown when your team lost the flag and you dont have enough people on the flag to clear.")]
    LOST,
    [Translatable(Languages.ChineseSimplified, "对峙中")]
    [Translatable("CONTESTED", Description = "Shown when your team and the other team have the same amount of people on the flag.")]
    CONTESTED,
    [Translatable(Languages.ChineseSimplified, "不是目标点")]
    [Translatable("INEFFECTIVE", Description = "Shown when you're on a flag but it's not the objective.")]
    INEFFECTIVE,
    [Translatable(Languages.ChineseSimplified, "清理中")]
    [Translatable("CLEARING", Description = "Shown when your team is capturing a flag still owned by the other team.")]
    CLEARING,
    [Translatable("", Description = "Leave blank.", IsPrioritizedTranslation = false)]
    BLANK,
    [Translatable("", Description = "Leave blank.", IsPrioritizedTranslation = false)]
    DONT_DISPLAY,
    [Translatable(Languages.ChineseSimplified, "在载具中")]
    [Translatable("IN VEHICLE", Description = "Shown when you're trying to capture a flag while in a vehicle.")]
    IN_VEHICLE,
    [Translatable(Languages.ChineseSimplified, "已被锁定")]
    [Translatable("LOCKED", Description = "Shown in Invasion when a flag has already been captured by attackers and can't be recaptured.")]
    LOCKED
}
