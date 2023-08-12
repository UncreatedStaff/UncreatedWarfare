using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Configuration;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Warfare.Commands.CommandSystem;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands.VanillaRework;
public class ShutdownCommand : Command
{
    private const string Syntax = "/shutdown [instant|after|cancel|*time*] [reason]";
    private const string Help = "Schedule shutdowns for the server.";
    public static Coroutine? Messager;

    public ShutdownCommand() : base("shutdown", EAdminType.VANILLA_ADMIN, 1)
    {
        Structure = new CommandStructure
        {
            Description = "Schedule shutdowns for the server.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Delay", typeof(TimeSpan))
                {
                    IsOptional = true,
                    Description = "Shut down the server in a specified amount of time.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Reason", typeof(string))
                    }
                },
                new CommandParameter("Instant")
                {
                    Aliases = new string[] { "inst" },
                    IsOptional = true,
                    Description = "Shut down the server immediately.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Reason", typeof(string))
                        {
                            IsOptional = true
                        }
                    }
                },
                new CommandParameter("After")
                {
                    Aliases = new string[] { "aftergame", "game" },
                    IsOptional = true,
                    Description = "Shut down the server after the current game.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Reason", typeof(string))
                    }
                },
                new CommandParameter("Cancel")
                {
                    Aliases = new string[] { "abort" },
                    IsOptional = true
                }
            }
        };
    }
    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        if (ctx.ArgumentCount == 0)
        {
#if RELEASE
            // use this to keep the panel from auto-restarting it.
            if (Data.Gamemode != null && Data.Gamemode.ShouldShutdownAfterGame)
            {
                throw ctx.ReplyString("Already shutting down.");
            }
#endif
            ActionLog.AddPriority(ActionLogType.ShutdownServer, "INSTANT", ctx.CallerID);
            UCWarfare.ShutdownNow("None specified", ctx.CallerID);
            throw ctx.Defer();
        }
        if (ctx.MatchParameter(0, "inst", "instant"))
        {
            if (!ctx.TryGetRange(1, out string reason))
                reason = "None Specified";

            ActionLog.AddPriority(ActionLogType.ShutdownServer, "INSTANT: " + reason, ctx.CallerID);
            UCWarfare.ShutdownNow(reason, ctx.CallerID);
            ctx.Defer();
        }
        else if (ctx.MatchParameter(0, "cancel", "abort"))
        {
            ctx.LogAction(ActionLogType.ShutdownServer, "CANCELLED");
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
                ctx.LogAction(ActionLogType.ShutdownServer, "AFTER GAME " + (Data.Gamemode == null ? "null" : Data.Gamemode.GameID.ToString(Data.AdminLocale)) + ": " + reason);
                Data.Gamemode?.ShutdownAfterGame(reason, ctx.CallerID);
                Chat.Broadcast(T.ShutdownBroadcastAfterGame, reason);
                L.Log(ctx.IsConsole
                        ? $"A shutdown has been scheduled after this game because: {reason}."
                        : $"A shutdown has been scheduled after this game by {ctx.Caller.Name.PlayerName} ({ctx.CallerID}) because: {reason}.",
                    ConsoleColor.Cyan);
                if (Messager != null)
                    UCWarfare.I.StopCoroutine(Messager);
                Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
                NetCalls.SendShuttingDownAfter.NetInvoke(0UL, reason);
                ctx.Defer();
#if RELEASE
                Console.WriteLine("shutdown");
#endif
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
#if RELEASE
            Console.WriteLine("shutdown");
#endif
        }
        else throw ctx.SendCorrectUsage(Syntax + " - " + Help);
    }
    internal static void ShutdownIn(int seconds, string reason, ulong instigator = 0)
    {
        string time;
        bool a = false;
        foreach (LanguageSet set in LanguageSet.AllBut(instigator))
        {
            time = Localization.GetTimeFromSeconds(seconds, in set);
            if (!a && set.IsDefault)
            {
                a = true;
                L.Log($"A shutdown has been scheduled in {time} by {instigator} because: {reason}.", ConsoleColor.Cyan);
                ActionLog.Add(ActionLogType.ShutdownServer, "IN " + time.ToUpper() + ": " + reason, instigator);
            }
            Chat.Broadcast(set, T.ShutdownBroadcastTime, time, reason);
        }
        if (!a)
        {
            time = Localization.GetTimeFromSeconds(seconds);
            L.Log($"A shutdown has been scheduled in {time} by {instigator} because: {reason}.", ConsoleColor.Cyan);
            ActionLog.Add(ActionLogType.ShutdownServer, "IN " + time.ToUpper() + ": " + reason, instigator);
        }
        NetCalls.SendShuttingDownInSeconds.NetInvoke(instigator, reason, (uint)seconds);
        Console.WriteLine("shutdown");
        Provider.shutdown(seconds, reason);
    }
    public static void ShutdownAfterGameDaily() => ShutdownAfterGame("Daily Restart", true);
    public static void ShutdownAfterGame(string reason, bool isDaily)
    {
        ActionLog.Add(ActionLogType.ShutdownServer, "AFTER GAME " + (Data.Gamemode == null ? "null" : Data.Gamemode.GameID.ToString(Data.AdminLocale)) + ": " + reason);
        Chat.Broadcast(isDaily ? T.ShutdownBroadcastDaily : T.ShutdownBroadcastAfterGame, reason);
        L.Log($"A shutdown has been scheduled after this game because: {reason}.", ConsoleColor.Cyan);
        Data.Gamemode?.ShutdownAfterGame(reason, 0);
        if (Messager != null)
            UCWarfare.I.StopCoroutine(Messager);
        Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
        NetCalls.SendShuttingDownAfter.NetInvoke(0UL, reason);
        Console.WriteLine("shutdown");
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
