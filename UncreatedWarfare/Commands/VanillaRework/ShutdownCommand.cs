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
    private const string HELP = "Schedule shutdowns for the server.";
    public static Coroutine? Messager = null;
    public ShutdownCommand() : base("shutdown", EAdminType.VANILLA_ADMIN, 1) { }
    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (ctx.ArgumentCount == 0)
        {
            ActionLogger.AddPriority(EActionLogType.SHUTDOWN_SERVER, "INSTANT", ctx.CallerID);
            UCWarfare.ShutdownNow("None specified", ctx.CallerID);
            throw ctx.Defer();
        }
        if (ctx.MatchParameter(0, "inst", "instant"))
        {
            if (ctx.TryGetRange(1, out string reason))
            {
                ActionLogger.AddPriority(EActionLogType.SHUTDOWN_SERVER, "INSTANT: " + reason, ctx.CallerID);
                UCWarfare.ShutdownNow(reason, ctx.CallerID);
            }
            else
            {
                ActionLogger.AddPriority(EActionLogType.SHUTDOWN_SERVER, "INSTANT", ctx.CallerID);
                UCWarfare.ShutdownNow("None specified", ctx.CallerID);
            }
            ctx.Defer();
        }
        else if (ctx.MatchParameter(0, "cancel", "abort"))
        {
            ctx.LogAction(EActionLogType.SHUTDOWN_SERVER, $"CANCELLED");
            Data.Gamemode.CancelShutdownAfterGame();
            Chat.Broadcast(T.ShutdownBroadcastCancelled);
            L.Log("The scheduled shutdown was cancelled.", ConsoleColor.Cyan);
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
                Chat.Broadcast(T.ShutdownBroadcastAfterGame, reason);
                if (ctx.IsConsole)
                    L.Log($"A shutdown has been scheduled after this game because: {reason}.", ConsoleColor.Cyan);
                else
                    L.Log($"A shutdown has been scheduled after this game by {ctx.Caller.Name.PlayerName} ({ctx.CallerID}) because: {reason}.", ConsoleColor.Cyan);
                if (Messager != null)
                    UCWarfare.I.StopCoroutine(Messager);
                Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
                NetCalls.SendShuttingDownAfter.NetInvoke(0UL, reason);
                ctx.Defer();
            }
            else throw ctx.SendCorrectUsage("/shutdown after <reason>");
        }
        else if (ctx.TryGet(0, out string time) && ctx.TryGetRange(1, out string reason))
        {
            int secs = Util.ParseTime(time);
            if (secs == 0)
                throw ctx.Reply(T.InvalidTime, time);
            ShutdownIn(secs, reason, ctx.CallerID);
            ctx.Defer();
        }
        else throw ctx.SendCorrectUsage(SYNTAX + " - " + HELP);
    }
    internal static void ShutdownIn(int seconds, string reason, ulong instigator = 0)
    {
        string time;
        bool a = false;
        foreach (LanguageSet set in LanguageSet.AllBut(instigator))
        {
            time = seconds.GetTimeFromSeconds(set.Language);
            if (set.Language.Equals(L.DEFAULT))
            {
                a = true;
                L.Log($"A shutdown has been scheduled in {time} by {instigator} because: {reason}.", ConsoleColor.Cyan);
                ActionLogger.Add(EActionLogType.SHUTDOWN_SERVER, $"IN " + time.ToUpper() + ": " + reason, instigator);
            }
            Chat.Broadcast(set, T.ShutdownBroadcastTime, time, reason);
        }
        if (!a)
        {
            time = seconds.GetTimeFromSeconds(L.DEFAULT);
            L.Log($"A shutdown has been scheduled in {time} by {instigator} because: {reason}.", ConsoleColor.Cyan);
            ActionLogger.Add(EActionLogType.SHUTDOWN_SERVER, $"IN " + time.ToUpper() + ": " + reason, instigator);
        }
        NetCalls.SendShuttingDownInSeconds.NetInvoke(instigator, reason, (uint)seconds);
        Provider.shutdown(seconds, reason);
    }
    public static void ShutdownAfterGameDaily() => ShutdownAfterGame("Daily Restart", true);
    public static void ShutdownAfterGame(string reason, bool isDaily)
    {
        ActionLogger.Add(EActionLogType.SHUTDOWN_SERVER, $"AFTER GAME " + (Data.Gamemode == null ? "null" : Data.Gamemode.GameID.ToString(Data.Locale)) + ": " + reason);
        Chat.Broadcast(isDaily ? T.ShutdownBroadcastDaily : T.ShutdownBroadcastAfterGame, reason);
        L.Log($"A shutdown has been scheduled after thi game because: {reason}.", ConsoleColor.Cyan);
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
            Chat.Broadcast(T.ShutdownBroadcastReminder, reason);
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
