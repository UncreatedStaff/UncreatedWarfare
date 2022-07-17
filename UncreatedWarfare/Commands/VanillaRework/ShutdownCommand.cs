using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Warfare.Commands.CommandSystem;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands.VanillaRework;
public class ShutdownCommand : Command
{
    private const string SYNTAX = "/shutdown [instant|after|cancel|*time*] [reason]";
    private const string HELP = "Does nothing.";
    public static Coroutine? Messager = null;
    public ShutdownCommand() : base("shutdown", EAdminType.VANILLA_ADMIN, 1) { }
    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (ctx.ArgumentCount == 0)
        {
            ActionLog.AddPriority(EActionLogType.SHUTDOWN_SERVER, "INSTANT", ctx.CallerID);
            UCWarfare.ShutdownNow("None specified", ctx.CallerID);
            throw ctx.Defer();
        }
        if (ctx.MatchParameter(0, "inst", "instant"))
        {
            if (ctx.TryGetRange(1, out string reason))
            {
                ActionLog.AddPriority(EActionLogType.SHUTDOWN_SERVER, "INSTANT: " + reason, ctx.CallerID);
                UCWarfare.ShutdownNow(reason, ctx.CallerID);
            }
            else
            {
                ActionLog.AddPriority(EActionLogType.SHUTDOWN_SERVER, "INSTANT", ctx.CallerID);
                UCWarfare.ShutdownNow("None specified", ctx.CallerID);
            }
            ctx.Defer();
        }
        else if (ctx.MatchParameter(0, "cancel", "abort"))
        {
            ctx.LogAction(EActionLogType.SHUTDOWN_SERVER, $"CANCELLED");
            Data.Gamemode.CancelShutdownAfterGame();
            Chat.Broadcast("shutdown_broadcast_after_game_canceled");
            L.Log(Localization.Translate("shutdown_broadcast_after_game_canceled_console", 0, out _), ConsoleColor.Cyan);
            if (Messager != null)
                UCWarfare.I.StopCoroutine(Messager);
            NetCalls.SendCancelledShuttingDownAfter.NetInvoke(ctx.CallerID);
            ctx.Defer();
        }
        else if (ctx.MatchParameter(0, "after", "aftergame", "game"))
        {
            if (ctx.TryGetRange(1, out string reason))
            {
                ctx.LogAction(EActionLogType.SHUTDOWN_SERVER, $"AFTER GAME " + (Data.Gamemode == null ? "null" : Data.Gamemode.GameID.ToString(Data.Locale)) + ": " + reason);
                Data.Gamemode?.ShutdownAfterGame(reason, ctx.CallerID);
                Chat.Broadcast("shutdown_broadcast_after_game", reason);
                if (ctx.IsConsole)
                    L.Log(Localization.Translate("shutdown_broadcast_after_game_console", 0, out _, reason), ConsoleColor.Cyan);
                else
                    L.Log(Localization.Translate("shutdown_broadcast_after_game_console_player", 0, out _, F.GetPlayerOriginalNames(ctx.Caller).PlayerName, reason), ConsoleColor.Cyan);
                if (Messager != null)
                    UCWarfare.I.StopCoroutine(Messager);
                Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
                NetCalls.SendShuttingDownAfter.NetInvoke(0UL, reason);
                ctx.Defer();
            }
            else throw ctx.SendCorrectUsage("/shutdown after <reason>");
        }
        else if (ctx.TryGet(0, out int seconds) && ctx.TryGetRange(1, out string reason))
        {
            ShutdownIn(seconds, reason, ctx.CallerID);
            ctx.Defer();
        }
        else throw ctx.Reply("shutdown_syntax");
    }
    internal static void ShutdownIn(int seconds, string reason, ulong instigator = 0)
    {
        string time;
        bool a = false;
        foreach (LanguageSet set in Localization.EnumerateLanguageSetsExclude(instigator))
        {
            time = seconds.GetTimeFromSeconds(set.Language);
            if (set.Language.Equals(JSONMethods.DEFAULT_LANGUAGE))
            {
                a = true;
                L.Log(Localization.Translate("shutdown_broadcast_after_time_console", 0, out _, time, reason), ConsoleColor.Cyan);
                ActionLog.Add(EActionLogType.SHUTDOWN_SERVER, $"IN " + time.ToUpper() + ": " + reason, instigator);
            }
            Chat.Broadcast(set, "shutdown_broadcast_after_time", time, reason);
        }
        if (!a)
        {
            time = seconds.GetTimeFromSeconds(JSONMethods.DEFAULT_LANGUAGE);
            L.Log(Localization.Translate("shutdown_broadcast_after_time_console", 0, out _, time, reason), ConsoleColor.Cyan);
            ActionLog.Add(EActionLogType.SHUTDOWN_SERVER, $"IN " + time.ToUpper() + ": " + reason, instigator);
        }
        NetCalls.SendShuttingDownInSeconds.NetInvoke(instigator, reason, (uint)seconds);
        Provider.shutdown(seconds, reason);
    }
    public static void ShutdownAfterGameDaily() => ShutdownAfterGame("Daily Restart", true);
    public static void ShutdownAfterGame(string reason, bool isDaily)
    {
        ActionLog.Add(EActionLogType.SHUTDOWN_SERVER, $"AFTER GAME " + (Data.Gamemode == null ? "null" : Data.Gamemode.GameID.ToString(Data.Locale)) + ": " + reason);
        Chat.Broadcast(isDaily ? "shutdown_broadcast_after_game_daily" : "shutdown_broadcast_after_game", reason);
        L.Log(Localization.Translate("shutdown_broadcast_after_game_console", 0, out _, reason), ConsoleColor.Cyan);
        Data.Gamemode?.ShutdownAfterGame(reason, 0);
        if (Messager != null)
            UCWarfare.I.StopCoroutine(Messager);
        Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
        NetCalls.SendShuttingDownAfter.NetInvoke(0UL, reason);
    }
    public static IEnumerator<WaitForSeconds> ShutdownMessageSender(string reason)
    {
        if (UCWarfare.Config.ModerationSettings.TimeBetweenShutdownMessages == 0) yield break;
        while (true)
        {
            yield return new WaitForSeconds(UCWarfare.Config.ModerationSettings.TimeBetweenShutdownMessages);
            Chat.Broadcast("shutdown_broadcast_after_game_reminder", reason);
        }
    }
    public static class NetCalls
    {
        public static readonly NetCall<ulong, string> SendShuttingDownInstant = new NetCall<ulong, string>(1012);
        public static readonly NetCall<ulong, string> SendShuttingDownAfter = new NetCall<ulong, string>(1013);
        public static readonly NetCall<ulong> SendCancelledShuttingDownAfter = new NetCall<ulong>(1014);
        public static readonly NetCall<ulong, string, uint> SendShuttingDownInSeconds = new NetCall<ulong, string, uint>(1015);
    }
}
