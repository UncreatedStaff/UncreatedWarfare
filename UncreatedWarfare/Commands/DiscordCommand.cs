using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Commands;

public class DiscordCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "discord";
    public string Help => "Links you to the Uncreated Discord server.";
    public string Syntax => "/discord";
    private readonly List<string> _aliases = new List<string>(0);
    public List<string> Aliases => _aliases;
    private readonly List<string> _permissions = new List<string>(1) { "uc.discord" };
		public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
        UCCommandContext ctx = new UCCommandContext(caller, command);
        if (ctx.Caller is not null)
        ctx.Caller.Player.channel.owner.SendURL("Join our Discord Server", "https://discord.gg/" + UCWarfare.Config.DiscordInviteCode);
    }
}
