using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Moderation;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public sealed class ModerateCommand : AsyncCommand
{
    private const string Syntax = "/moderate";
    private const string Help = "Opens the moderation menu.";
    public ModerateCommand() : base("moderate", EAdminType.MODERATOR)
    {
        Structure = new CommandStructure
        {
            Description = "Opens the moderation menu"
        };
        AddAlias("mod");
        AddAlias("m");
    }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);
        
        await ModerationUI.Instance.Open(ctx.Caller, token).ConfigureAwait(false);
        ctx.Defer();
    }
}
