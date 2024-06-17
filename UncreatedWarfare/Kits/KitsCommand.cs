using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Commands;
public sealed class KitsCommand : AsyncCommand
{
    private const string Syntax = "/kits";
    private const string Help = "Open the kit menu.";

    public KitsCommand() : base("kits", EAdminType.MEMBER)
    {
        Structure = new CommandStructure
        {
            Description = Help
        };
    }

    public override async Task Execute(CommandContext ctx, CancellationToken token)
    {
        await UCWarfare.ToUpdate(token);
        ctx.AssertRanByPlayer();

        if (UCWarfare.Config.DisableKitMenu)
            throw ctx.SendNotImplemented();

#if false
        if (!ctx.Caller.OnDuty() && ctx.CallerID is not 76561198839009178ul)
        {
            int c;
            await ctx.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (ctx.Caller.AccessibleKits != null)
                    c = ctx.Caller.AccessibleKits.Count(x => x is { } k && k.Type == KitType.Loadout);
                else
                    c = 0;
            }
            finally
            {
                ctx.Caller.PurchaseSync.Release();
            }

            if (c < 13)
                throw ctx.SendNotImplemented();
        }
#endif

        ctx.AssertHelpCheck(0, Syntax + " - " + Help);
        
        KitManager.MenuUI.OpenUI(ctx.Caller);
    }
}