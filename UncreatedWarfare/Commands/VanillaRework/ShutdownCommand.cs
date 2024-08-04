using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("shutdown"), Priority(1)]
[HelpMetadata(nameof(GetHelpMetadata))]
public class ShutdownCommand : IExecutableCommand
{
    private const string Syntax = "/shutdown [instant|after|cancel|*time*] [reason]";
    private const string Help = "Schedule shutdowns for the server.";
    public static Coroutine? Messager;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Schedule shutdowns for the server.",
            Parameters =
            [
                new CommandParameter("Delay", typeof(TimeSpan))
                {
                    IsOptional = true,
                    Description = "Shut down the server in a specified amount of time.",
                    Parameters =
                    [
                        new CommandParameter("Reason", typeof(string))
                    ]
                },
                new CommandParameter("Instant")
                {
                    Aliases = [ "inst" ],
                    IsOptional = true,
                    Description = "Shut down the server immediately.",
                    Parameters =
                    [
                        new CommandParameter("Reason", typeof(string))
                        {
                            IsOptional = true
                        }
                    ]
                },
                new CommandParameter("After")
                {
                    Aliases = [ "aftergame", "game" ],
                    IsOptional = true,
                    Description = "Shut down the server after the current game.",
                    Parameters =
                    [
                        new CommandParameter("Reason", typeof(string))
                    ]
                },
                new CommandParameter("Cancel")
                {
                    Aliases = [ "abort" ],
                    IsOptional = true
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        if (Context.ArgumentCount == 0)
        {
#if RELEASE
            // use this to keep the panel from auto-restarting it.
            if (Data.Gamemode != null && Data.Gamemode.ShouldShutdownAfterGame)
            {
                throw Context.ReplyString("Already shutting down.");
            }
#endif
            ActionLog.AddPriority(ActionLogType.ShutdownServer, "INSTANT", Context.CallerId.m_SteamID);
            UCWarfare.ShutdownNow("None specified", Context.CallerId.m_SteamID);
            throw Context.Defer();
        }

        string? reason;

        if (Context.MatchParameter(0, "inst", "instant"))
        {
            if (!Context.TryGetRange(1, out reason))
                reason = "None Specified";

            ActionLog.AddPriority(ActionLogType.ShutdownServer, "INSTANT: " + reason, Context.CallerId.m_SteamID);
            UCWarfare.ShutdownNow(reason, Context.CallerId.m_SteamID);
            throw Context.Defer();
        }
        
        if (Context.MatchParameter(0, "cancel", "abort"))
        {
            Context.LogAction(ActionLogType.ShutdownServer, "CANCELLED");
            Data.Gamemode.CancelShutdownAfterGame();
            Chat.Broadcast(T.ShutdownBroadcastCancelled);
            L.Log("The scheduled shutdown was cancelled.", ConsoleColor.Cyan);
            if (Messager != null)
                UCWarfare.I.StopCoroutine(Messager);
            //NetCalls.SendCancelledShuttingDownAfter.NetInvoke(Context.CallerId.m_SteamID);
            throw Context.Defer();
        }
        
        if (Context.MatchParameter(0, "after", "aftergame", "game"))
        {
            if (!Context.TryGetRange(1, out reason))
                throw Context.SendCorrectUsage("/shutdown after <reason>");
            
            Context.LogAction(ActionLogType.ShutdownServer,
                "AFTER GAME " + (Data.Gamemode == null ? "null" : Data.Gamemode.GameId.ToString(Data.AdminLocale)) +
                ": " + reason);
            Data.Gamemode?.ShutdownAfterGame(reason, Context.CallerId.m_SteamID);
            Chat.Broadcast(T.ShutdownBroadcastAfterGame, reason);
            L.Log(Context.IsConsole
                    ? $"A shutdown has been scheduled after this game because: {reason}."
                    : $"A shutdown has been scheduled after this game by {Context.Player.Name.PlayerName} ({Context.CallerId.m_SteamID}) because: {reason}.",
                ConsoleColor.Cyan);
            if (Messager != null)
                UCWarfare.I.StopCoroutine(Messager);
            Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
            //NetCalls.SendShuttingDownAfter.NetInvoke(0UL, reason);
            throw Context.Defer();
        }
        
        if (Context.TryGet(0, out string time) && Context.TryGetRange(1, out reason))
        {
            int secs = (int)Math.Round(FormattingUtility.ParseTimespan(time).TotalSeconds);
            if (secs == 0)
                throw Context.Reply(T.InvalidTime, time);
            
            ShutdownIn(secs, reason, Context.CallerId.m_SteamID);
            throw Context.Defer();
        }
        
        throw Context.SendCorrectUsage(Syntax + " - " + Help);
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
        //NetCalls.SendShuttingDownInSeconds.NetInvoke(instigator, reason, (uint)seconds);
        Console.WriteLine("shutdown");
        Provider.shutdown(seconds, reason);
    }
    public static void ShutdownAfterGameDaily() => ShutdownAfterGame("Daily Restart", true);
    public static void ShutdownAfterGame(string reason, bool isDaily)
    {
        ActionLog.Add(ActionLogType.ShutdownServer, "AFTER GAME " + (Data.Gamemode == null ? "null" : Data.Gamemode.GameId.ToString(Data.AdminLocale)) + ": " + reason);
        Chat.Broadcast(isDaily ? T.ShutdownBroadcastDaily : T.ShutdownBroadcastAfterGame, reason);
        L.Log($"A shutdown has been scheduled after this game because: {reason}.", ConsoleColor.Cyan);
        Data.Gamemode?.ShutdownAfterGame(reason, 0);
        if (Messager != null)
            UCWarfare.I.StopCoroutine(Messager);
        Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
        //NetCalls.SendShuttingDownAfter.NetInvoke(0UL, reason);
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
#if false
    public static class NetCalls
    {
        public static readonly NetCall<ulong, string> SendShuttingDownInstant = new NetCall<ulong, string>(KnownNetMessage.SendShuttingDownInstant);
        public static readonly NetCall<ulong, string> SendShuttingDownAfter = new NetCall<ulong, string>(KnownNetMessage.SendShuttingDownAfter);
        public static readonly NetCall<ulong> SendCancelledShuttingDownAfter = new NetCall<ulong>(KnownNetMessage.SendCancelledShuttingDownAfter);
        public static readonly NetCall<ulong, string, uint> SendShuttingDownInSeconds = new NetCall<ulong, string, uint>(KnownNetMessage.SendShuttingDownInSeconds);
    }
#endif
}
