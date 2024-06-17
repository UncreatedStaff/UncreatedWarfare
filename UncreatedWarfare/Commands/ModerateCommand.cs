using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Moderation;

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

    public override async Task Execute(CommandContext ctx, CancellationToken token)
    {
        ctx.AssertOnDuty();

        ctx.AssertRanByPlayer();
        
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);
        
        await ModerationUI.Instance.Open(ctx.Caller, token).ConfigureAwait(false);
        ctx.Defer();
    }
}
