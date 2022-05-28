using Rocket.API;
using SDG.Unturned;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;

namespace Uncreated.Warfare.Commands;

public class BuyCommand : IRocketCommand
{
    private readonly List<string> _aliases = new List<string>(0);
    private readonly List<string> _permissions = new List<string>(1) { "uc.buy" };
    public List<string> Permissions => _permissions;
    public List<string> Aliases => _aliases;
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "buy";
    public string Help => "Must be looking at a kit request sign. Purchases a kit for credits. Doesn't work on loadouts, premium, free, or exclusive kits, or kits you can't afford.";
    public string Syntax => "/buy [help]";
    public void Execute(IRocketPlayer caller, string[] command)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CommandContext ctx = new CommandContext(caller, command);
        if (ctx.MatchParameter(0, "help"))
        {
            ctx.SendCorrectUsage(Syntax + " - " + Help);
            return;
        }
        if (!RequestSigns.Loaded || !KitManager.Loaded)
        {
            ctx.SendGamemodeError();
            return;
        }
        if ((Data.Gamemode.State != EState.ACTIVE && Data.Gamemode.State != EState.STAGING) || ctx.Caller is null)
        {
            ctx.SendUnknownError();
            return;
        }
        ulong team = ctx.Caller.GetTeam();
        if (ctx.TryGetTarget(out BarricadeDrop drop) && drop.interactable is InteractableSign sign)
        {
            if (!RequestSigns.SignExists(sign, out RequestSign requestsign))
                ctx.Reply("request_kit_e_kitnoexist");
            else if (requestsign.kit_name.StartsWith("loadout_"))
                ctx.Reply("request_kit_e_notbuyablecredits");
            else if (!KitManager.KitExists(requestsign.kit_name, out Kit kit))
                ctx.Reply("request_kit_e_kitnoexist");
            else if (ctx.Caller.Rank.Level < kit.UnlockLevel)
                ctx.Reply("request_kit_e_wronglevel", RankData.GetRankName(kit.UnlockLevel));
            else if (kit.IsPremium)
                ctx.Reply("request_kit_e_notbuyablecredits");
            else if (kit.CreditCost == 0 || KitManager.HasAccessFast(kit, ctx.Caller))
                ctx.Reply("request_kit_e_alreadyhaskit");
            else if (ctx.Caller.CachedCredits < kit.CreditCost)
                ctx.Reply("request_kit_e_notenoughcredits", (kit.CreditCost - ctx.Caller.CachedCredits).ToString());
            else
            {
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
            }
        }
        else
        {
            ctx.Reply("request_not_looking");
        }
    }
}
