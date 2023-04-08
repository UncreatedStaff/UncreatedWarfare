using System.Linq;
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

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        if (UCWarfare.Config.DisableKitMenu)
            throw ctx.SendNotImplemented();

        int c;
        await ctx.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (ctx.Caller.AccessibleKits != null)
                c = ctx.Caller.AccessibleKits.Count(x => x.Item is { } k && k.Type == KitType.Loadout);
            else
                c = 0;
        }
        finally
        {
            ctx.Caller.PurchaseSync.Release();
        }

        if (c < 13)
            throw ctx.SendNotImplemented();

        ctx.AssertHelpCheck(0, Syntax + " - " + Help);
        
        KitManager.MenuUI.OpenUI(ctx.Caller);
    }
}