using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Commands;

[Command("kits")]
[MetadataFile(nameof(GetHelpMetadata))]
public sealed class KitsCommand : IExecutableCommand
{
    private const string Syntax = "/kits";
    private const string Help = "Open the kit menu.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (UCWarfare.Config.DisableKitMenu)
            throw Context.SendNotImplemented();

#if false
        if (!Context.Caller.OnDuty() && Context.CallerID is not 76561198839009178ul)
        {
            int c;
            await Context.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (Context.Caller.AccessibleKits != null)
                    c = Context.Caller.AccessibleKits.Count(x => x is { } k && k.Type == KitType.Loadout);
                else
                    c = 0;
            }
            finally
            {
                Context.Caller.PurchaseSync.Release();
            }

            if (c < 13)
                throw Context.SendNotImplemented();
        }
#endif

        Context.AssertHelpCheck(0, Syntax + " - " + Help);
        
        KitManager.MenuUI.OpenUI(Context.Player);
        throw Context.Defer();
    }
}