using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;

namespace Uncreated.Warfare.Commands;

public class DiscordCommand : Command
{
    const string HELP = "Sends the Discord link to the Uncreated Network server.";
    const string SYNTAX = "/discord";
    public DiscordCommand() : base("discord", EAdminType.MEMBER)
    {
        Structure = new CommandStructure
        {
            Description = "Sends a link to our discord server."
        };
    }
    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (ctx.Caller is not null)
            ctx.Caller.Player.channel.owner.SendURL("Join our Discord Server", "https://discord.gg/" + UCWarfare.Config.DiscordInviteCode);
        else
            ctx.ReplyString("https://discord.gg/" + UCWarfare.Config.DiscordInviteCode);
    }
}
