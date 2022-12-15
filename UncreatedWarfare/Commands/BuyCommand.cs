using SDG.Unturned;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;

namespace Uncreated.Warfare.Commands;

public class BuyCommand : AsyncCommand
{
    const string HELP = "Must be looking at a kit request sign. Purchases a kit for credits.";
    const string SYNTAX = "/buy [help]";
    public BuyCommand() : base("buy", EAdminType.MEMBER) { }
    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        if (ctx.MatchParameter(0, "help"))
            throw ctx.SendCorrectUsage(SYNTAX + " - " + HELP);
        if (!RequestSignsOld.Loaded)
            throw ctx.SendGamemodeError();
        ctx.AssertGamemode(out IKitRequests gm);
        KitManager manager = gm.KitManager;
        if ((Data.Gamemode.State != State.Active && Data.Gamemode.State != State.Staging) || ctx.Caller is null)
            throw ctx.SendUnknownError();
        if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
        {
            if (!RequestSignsOld.SignExists(sign, out RequestSign requestsign))
                throw ctx.Reply(T.RequestKitNotRegistered);
            if (requestsign.KitName.StartsWith(Signs.LoadoutPrefix))
                throw ctx.Reply(T.RequestNotBuyable);
            SqlItem<Kit>? proxy = await manager.FindKit(requestsign.KitName, token).ConfigureAwait(false);
            if (proxy?.Item == null)
                throw ctx.Reply(T.KitNotFound, requestsign.KitName);
            await manager.BuyKit(ctx, proxy, drop.model.position, token).ConfigureAwait(false);
        }
        else throw ctx.Reply(T.RequestNoTarget);
    }
}
