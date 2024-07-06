using Cysharp.Threading.Tasks;
using SDG.Unturned;
using System;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Commands;

[Command("buy")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class BuyCommand : IExecutableCommand
{
    const string Help = "Must be looking at a kit request sign. Purchases a kit for credits.";
    const string Syntax = "/buy [help]";

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
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (Context.MatchParameter(0, "help"))
        {
            throw Context.SendCorrectUsage(Syntax + " - " + Help);
        }

        Context.AssertGamemode(out IKitRequests gm);

        KitManager manager = gm.KitManager;
        if (Context.Caller is null || (Data.Gamemode.State != State.Active && Data.Gamemode.State != State.Staging))
        {
            throw Context.SendUnknownError();
        }

        if (!Context.TryGetBarricadeTarget(out BarricadeDrop? drop) || drop.interactable is not InteractableSign)
        {
            throw Context.Reply(T.RequestNoTarget);
        }

        if (Signs.GetKitFromSign(drop, out int ld) is { } kit)
        {
            await manager.Requests.BuyKit(Context, kit, drop.model.position, token).ConfigureAwait(false);
            return;
        }

        if (ld <= -1)
            throw Context.Reply(T.RequestKitNotRegistered);

        if (UCWarfare.Config.WebsiteUri == null || Data.PurchasingDataStore.LoadoutProduct == null)
            throw Context.Reply(T.RequestNotBuyable);

        Context.Player.Player.sendBrowserRequest("Purchase loadouts on our website.", new Uri(UCWarfare.Config.WebsiteUri, "kits/loadout").OriginalString);
        throw Context.Defer();
    }
}
