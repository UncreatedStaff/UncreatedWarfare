using DanielWillett.ReflectionTools;
using System;
using Uncreated.Warfare.Commands.Dispatch;

namespace Uncreated.Warfare.Commands;

[Command("ban"), Priority(1)]
[MetadataFile(nameof(GetHelpMetadata))]
public class BanCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Prevents a player from joining the server for a specified amount of time.",
            Parameters =
            [
                new CommandParameter("Player", typeof(IPlayer))
                {
                    Parameters =
                    [
                        new CommandParameter("Duration", typeof(TimeSpan), "Permanent")
                        {
                            Aliases = [ "perm" ],
                            Parameters =
                            [
                                new CommandParameter("Reason", typeof(string))
                                {
                                    IsRemainder = true
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        throw Context.SendNotImplemented();
#if false
        Context.AssertArgs(3, Syntax);

        if (!Context.TryGet(0, out ulong targetId, out UCPlayer? target))
            throw Context.Reply(T.PlayerNotFound);
        int duration = Util.ParseTime(Context.Get(1)!);

        if (duration == 0 || duration < -1)
            throw Context.Reply(T.InvalidTime);

        string? reason = Context.GetRange(2);
        if (string.IsNullOrEmpty(reason))
            throw Context.Reply(T.NoReasonProvided);
        PlayerNames name;
        uint ipv4;
        List<byte[]> hwids = await OffenseManager.GetAllHWIDs(targetId, token).ConfigureAwait(false);
        await UniTask.SwitchToMainThread(token);

        if (target is not null && target.IsOnline) // player is online
        {
            CSteamID id = target.Player.channel.owner.playerID.steamID;
            target.Player.channel.owner.transportConnection.TryGetIPv4Address(out ipv4);
            name = target.Name;
            Provider.requestBanPlayer(Provider.server, id, ipv4, hwids, reason, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration));
        }
        else
        {
            ipv4 = await Data.DatabaseManager.TryGetPackedIPAsync(targetId, token).ConfigureAwait(false);
            name = await F.GetPlayerOriginalNamesAsync(targetId, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            F.OfflineBan(targetId, ipv4, Context.Caller == null ? CSteamID.Nil : Context.Caller.Player.channel.owner.playerID.steamID,
                reason!, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration), hwids.ToArray());
        }
        PlayerNames callerName = Context.Caller is not null ? Context.Caller.Name : PlayerNames.Console;
        ActionLog.Add(ActionLogType.BanPlayer, $"BANNED {targetId.ToString(Data.AdminLocale)} FOR \"{reason}\" DURATION: " +
            (duration == -1 ? "PERMANENT" : duration.ToString(Data.AdminLocale) + " SECONDS"), Context.CallerID);

        OffenseManager.LogBanPlayer(targetId, Context.CallerID, reason!, duration, DateTime.Now);

        if (duration == -1)
        {
            if (Context.IsConsole)
            {
                Context.ReplyString($"{name.PlayerName} ({targetId.ToString(Data.LocalLocale)}) was permanently banned by an operator because: {reason!}", ConsoleColor.Cyan);
                Chat.Broadcast(T.BanPermanentSuccessBroadcastOperator, name);
            }
            else
            {
                L.Log($"{name.PlayerName} ({targetId.ToString(Data.AdminLocale)}) was banned by {callerName.PlayerName} ({Context.CallerID}) because: {reason!}.", ConsoleColor.Cyan);
                Chat.Broadcast(LanguageSet.AllBut(Context.CallerID), T.BanPermanentSuccessBroadcast, name, callerName);
                Context.Reply(T.BanPermanentSuccessFeedback, name);
            }
        }
        else
        {
            string time = Localization.GetTimeFromSeconds(duration);
            if (Context.IsConsole)
            {
                L.Log($"{name.PlayerName} ({targetId.ToString(Data.AdminLocale)}) was banned by an operator for {time} because: {reason}.", ConsoleColor.Cyan);
                bool f = false;
                foreach (LanguageSet set in LanguageSet.All())
                {
                    if (f || !set.IsDefault)
                    {
                        time = Localization.GetTimeFromSeconds(duration, set.Language, set.CultureInfo);
                        f = true;
                    }
                    Chat.Broadcast(set, T.BanSuccessBroadcastOperator, name, time);
                }
            }
            else
            {
                L.Log($"{name.PlayerName} ({targetId}) was banned by {callerName.PlayerName} ({Context.CallerID.ToString(Data.AdminLocale)}) for {time} because: {reason}.", ConsoleColor.Cyan);
                bool f = false;
                foreach (LanguageSet set in LanguageSet.AllBut(Context.CallerID))
                {
                    if (f || !set.IsDefault)
                    {
                        time = Localization.GetTimeFromSeconds(duration, set.Language, set.CultureInfo);
                        f = true;
                    }
                    Chat.Broadcast(set, T.BanSuccessBroadcast, name, callerName, time);
                }
                if (f)
                    time = Localization.GetTimeFromSeconds(duration, Context.LanguageInfo, Context.CultureInfo);
                Context.Reply(T.BanSuccessFeedback, name, time);
            }
        }
#endif
    }
}