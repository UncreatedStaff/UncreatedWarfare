using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;

public class BuyCommand : Command
{
    const string HELP = "Must be looking at a kit request sign. Purchases a kit for credits.";
    const string SYNTAX = "/buy [help]";
    public BuyCommand() : base("buy", EAdminType.MEMBER) { }
    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        if (ctx.MatchParameter(0, "help"))
            throw ctx.SendCorrectUsage(SYNTAX + " - " + HELP);
        if (!RequestSigns.Loaded || !KitManager.Loaded)
            throw ctx.SendGamemodeError();
        if ((Data.Gamemode.State != EState.ACTIVE && Data.Gamemode.State != EState.STAGING) || ctx.Caller is null)
            throw ctx.SendUnknownError();
        ulong team = ctx.Caller.GetTeam();
        if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
        {
            if (!RequestSigns.SignExists(sign, out RequestSign requestsign))
                throw ctx.Reply(T.RequestKitNotRegistered);
            if (requestsign.kit_name.StartsWith("loadout_"))
                throw ctx.Reply(T.RequestNotBuyable);
            if (!KitManager.KitExists(requestsign.kit_name, out Kit kit))
                throw ctx.Reply(T.KitNotFound, requestsign.kit_name);
            if (ctx.Caller.Rank.Level < kit.UnlockLevel)
                throw ctx.Reply(T.RequestKitLowLevel, RankData.GetRankName(kit.UnlockLevel));
            if (kit.IsPremium)
                throw ctx.Reply(T.RequestNotBuyable);
            if (kit.CreditCost == 0 || KitManager.HasAccessFast(kit, ctx.Caller))
                throw ctx.Reply(T.RequestKitAlreadyOwned);
            if (ctx.Caller.CachedCredits < kit.CreditCost)
                throw ctx.Reply(T.RequestKitCantAfford, kit.CreditCost - ctx.Caller.CachedCredits, kit.CreditCost);

            Task.Run(async () =>
            {
                if (!ctx.Caller.HasDownloadedKits)
                    await ctx.Caller.DownloadKits();

                await ctx.Caller.PurchaseSync.WaitAsync();
                try
                {
                    await Points.UpdatePointsAsync(ctx.Caller);
                    if (ctx.Caller.CachedCredits < kit.CreditCost)
                    {
                        ctx.Reply(T.RequestKitCantAfford, kit.CreditCost - ctx.Caller.CachedCredits, kit.CreditCost);
                        return;
                    }
                    await Points.AwardCreditsAsync(ctx.Caller, -kit.CreditCost, isPurchase: true);
                }
                finally
                {
                    ctx.Caller.PurchaseSync.Release();
                }

                await KitManager.GiveAccess(kit, ctx.Caller, EKitAccessType.CREDITS);

                await UCWarfare.ToUpdate();

                KitManager.UpdateSigns(kit, ctx.Caller);
                if (requestsign != null && requestsign.barricadetransform != null)
                    EffectManager.sendEffect(81, 7f, requestsign.barricadetransform.position);
                ctx.Reply(T.RequestKitBought, kit.CreditCost);
                ctx.LogAction(EActionLogType.BUY_KIT, "BOUGHT KIT " + kit.Name + " FOR " + kit.CreditCost + " CREDITS");
                L.Log(ctx.Caller.Name.PlayerName + " (" + ctx.Caller.Steam64 + ") bought " + kit.Name);
            });
            ctx.Defer();
        }
        else throw ctx.Reply(T.RequestNoTarget);
    }
}
