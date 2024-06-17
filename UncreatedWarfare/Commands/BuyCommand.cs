using SDG.Unturned;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Commands;

public class BuyCommand : AsyncCommand
{
    const string Help = "Must be looking at a kit request sign. Purchases a kit for credits.";
    const string Syntax = "/buy [help]";
    public BuyCommand() : base("buy", EAdminType.MEMBER)
    {
        Structure = new CommandStructure
        {
            Description = "Purchase a kit for credits, must be looking at a kit sign."
        };
    }
    public override async Task Execute(CommandContext ctx, CancellationToken token)
    {
        ctx.AssertRanByPlayer();

        if (ctx.MatchParameter(0, "help"))
            throw ctx.SendCorrectUsage(Syntax + " - " + Help);
        ctx.AssertGamemode(out IKitRequests gm);
        KitManager manager = gm.KitManager;
        if ((Data.Gamemode.State != State.Active && Data.Gamemode.State != State.Staging) || ctx.Caller is null)
            throw ctx.SendUnknownError();
        if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign)
        {
            if (Signs.GetKitFromSign(drop, out int ld) is { } kit)
            {
                await manager.Requests.BuyKit(ctx, kit, drop.model.position, token).ConfigureAwait(false);
                return;
            }
            if (ld > -1)
            {
                if (UCWarfare.Config.WebsiteUri != null && Data.PurchasingDataStore.LoadoutProduct != null)
                {
                    ctx.Caller.Player.sendBrowserRequest("Purchase loadouts on our website.",
                        new Uri(UCWarfare.Config.WebsiteUri, "kits/loadout").OriginalString);

                    throw ctx.Defer();
                }
                throw ctx.Reply(T.RequestNotBuyable);
            }
            throw ctx.Reply(T.RequestKitNotRegistered);
        }
        throw ctx.Reply(T.RequestNoTarget);
    }
}
