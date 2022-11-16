using SDG.Unturned;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes;
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
        if (!RequestSigns.Loaded || !KitManager.Loaded)
            throw ctx.SendGamemodeError();
        if ((Data.Gamemode.State != EState.ACTIVE && Data.Gamemode.State != EState.STAGING) || ctx.Caller is null)
            throw ctx.SendUnknownError();
        ulong team = ctx.Caller.GetTeam();
        if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
        {
            if (!RequestSigns.SignExists(sign, out RequestSign requestsign))
                throw ctx.Reply(T.RequestKitNotRegistered);
            if (requestsign.KitName.StartsWith("loadout_"))
                throw ctx.Reply(T.RequestNotBuyable);
            if (!KitManager.KitExists(requestsign.KitName, out Kit kit))
                throw ctx.Reply(T.KitNotFound, requestsign.KitName);
            if (ctx.Caller.Rank.Level < kit.UnlockLevel)
                throw ctx.Reply(T.RequestKitLowLevel, RankData.GetRankName(kit.UnlockLevel));
            if (kit.IsPremium)
                throw ctx.Reply(T.RequestNotBuyable);
            if (kit.CreditCost == 0 || KitManager.HasAccessFast(kit, ctx.Caller))
                throw ctx.Reply(T.RequestKitAlreadyOwned);
            if (ctx.Caller.CachedCredits < kit.CreditCost)
                throw ctx.Reply(T.RequestKitCantAfford, kit.CreditCost - ctx.Caller.CachedCredits, kit.CreditCost);
            
            await ctx.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            if (!ctx.Caller.HasDownloadedKits)
                await ctx.Caller.DownloadKits(false, token).ConfigureAwait(false);
            try
            {
                await Points.UpdatePointsAsync(ctx.Caller, false, token).ConfigureAwait(false);
                if (ctx.Caller.CachedCredits < kit.CreditCost)
                {
                    await UCWarfare.ToUpdate();
                    ctx.Reply(T.RequestKitCantAfford, kit.CreditCost - ctx.Caller.CachedCredits, kit.CreditCost);
                    return;
                }

                CreditsParameters parameters = new CreditsParameters(ctx.Caller, team, -kit.CreditCost)
                {
                    IsPurchase = true,
                    IsPunishment = false
                };
                await Points.AwardCreditsAsync(parameters, token, false).ConfigureAwait(false);
            }
            finally
            {
                ctx.Caller.PurchaseSync.Release();
            }

            await KitManager.GiveAccess(kit, ctx.Caller, EKitAccessType.CREDITS).ThenToUpdate(token);

            KitManager.UpdateSigns(kit, ctx.Caller);
            if (Gamemode.Config.EffectPurchase.ValidReference(out EffectAsset effect))
            {
                F.TriggerEffectReliable(effect, EffectManager.SMALL,
                    requestsign.BarricadeTransform != null
                    ? requestsign.Position
                    : ctx.Caller.Position);
            }

            ctx.Reply(T.RequestKitBought, kit.CreditCost);
            ctx.LogAction(EActionLogType.BUY_KIT, "BOUGHT KIT " + kit.Name + " FOR " + kit.CreditCost + " CREDITS");
            L.Log(ctx.Caller.Name.PlayerName + " (" + ctx.Caller.Steam64 + ") bought " + kit.Name);
        }
        else throw ctx.Reply(T.RequestNoTarget);
    }
}
