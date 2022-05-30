using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;

namespace Uncreated.Warfare.Commands;

public class MuteCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "mute";
    public string Help => "Mute players in either voice chat or text chat.";
    public string Syntax => "/mute <voice|text|both> <name or steam64> <permanent | duration in minutes> <reason...>";
    private readonly List<string> _aliases = new List<string>(0);
    public List<string> Aliases => _aliases;
    private readonly List<string> _permissions = new List<string>(1) { "uc.mute" };
		public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
        CommandContext ctx = new CommandContext(caller, command);
        
        if (command.Length < 4)
            goto Syntax;
        EMuteType type;
        switch (command[0].ToLower())
        {
            case "voice":
                type = EMuteType.VOICE_CHAT;
                break;
            case "text":
                type = EMuteType.TEXT_CHAT;
                break;
            case "both":
                type = EMuteType.BOTH;
                break;
            default:
                goto Syntax;
        }

        if (!ctx.TryGet(2, out int duration) || duration < -1 || duration == 0)
            if (ctx.MatchParameterPartial(2, "perm"))
                duration = -1;
            else
                goto CantReadDuration;
        if (!ctx.TryGet(1, out ulong targetId, out _))
            goto NoPlayerFound;

        string reason = string.Join(" ", command, 3, command.Length - 3);
        OffenseManager.MutePlayer(targetId, ctx.Caller is null ? 0ul : ctx.Caller.Steam64, type, duration, reason);
        return;
    Syntax:
        ctx.Reply("mute_syntax");
        return;
    NoPlayerFound:
        ctx.Reply("mute_no_player_found");
        return;
    CantReadDuration:
        ctx.Reply("mute_cant_read_duration");
        return;
    }
}
[Translatable("Mute Severity")]
public enum EMuteType : byte
{
    NONE = 0,
    [Translatable("Voice Chat Only")]
    VOICE_CHAT = 1,
    [Translatable("Text Chat Only")]
    TEXT_CHAT = 2,
    [Translatable("Voice and Text Chat")]
    BOTH = 3
}