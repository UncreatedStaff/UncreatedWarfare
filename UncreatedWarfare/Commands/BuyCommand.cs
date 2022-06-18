using SDG.Unturned;
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
                throw ctx.Reply("request_kit_e_kitnoexist");
            if (requestsign.kit_name.StartsWith("loadout_"))
                throw ctx.Reply("request_kit_e_notbuyablecredits");
            if (!KitManager.KitExists(requestsign.kit_name, out Kit kit))
                throw ctx.Reply("request_kit_e_kitnoexist");
            if (ctx.Caller.Rank.Level < kit.UnlockLevel)
                throw ctx.Reply("request_kit_e_wronglevel", RankData.GetRankName(kit.UnlockLevel));
            if (kit.IsPremium)
                throw ctx.Reply("request_kit_e_notbuyablecredits");
            if (kit.CreditCost == 0 || KitManager.HasAccessFast(kit, ctx.Caller))
                throw ctx.Reply("request_kit_e_alreadyhaskit");
            if (ctx.Caller.CachedCredits < kit.CreditCost)
                throw ctx.Reply("request_kit_e_notenoughcredits", (kit.CreditCost - ctx.Caller.CachedCredits).ToString());

            Task.Run(async () =>
            {
                if (ctx.Caller.AccessibleKits == null)
                    ctx.Caller.AccessibleKits = await Data.DatabaseManager.GetAccessibleKits(ctx.Caller.Steam64);

                await KitManager.GiveAccess(kit, ctx.Caller, EKitAccessType.CREDITS);

                await UCWarfare.ToUpdate();

                RequestSigns.UpdateSignsOfKit(kit.Name, ctx.Caller.SteamPlayer);
                EffectManager.sendEffect(81, 7f, (requestsign.barricadetransform?.position).GetValueOrDefault());
                ctx.Reply("request_kit_boughtcredits", kit.CreditCost.ToString());
                Points.AwardCredits(ctx.Caller, -kit.CreditCost, isPurchase: true);
                ctx.LogAction(EActionLogType.BUY_KIT, "BOUGHT KIT " + kit.Name + " FOR " + kit.CreditCost + " CREDITS");
                L.Log(F.GetPlayerOriginalNames(ctx.Caller).PlayerName + " (" + ctx.Caller.Steam64 + ") bought " + kit.Name);
            });
            ctx.Defer();
        }
        else throw ctx.Reply("request_not_looking");
    }
}
