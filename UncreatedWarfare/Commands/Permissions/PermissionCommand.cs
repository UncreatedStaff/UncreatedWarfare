using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands.Permissions;
public class PermissionCommand : Command
{
    private const string SYNTAX = "/p [grant|revoke|reload] [player] [level]";
    private const string HELP = "See your permission level or manage other's permission levels.";
    public PermissionCommand() : base("permissions", EAdminType.MEMBER)
    {
        AddAlias("p");
        AddAlias("perms");
    }
    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (ctx.HasArgsExact(0))
            throw ctx.Reply("permissions_current", Translation.TranslateEnum(F.GetPermissions(ctx.CallerID), ctx.CallerID));

        ctx.AssertPermissions(EAdminType.VANILLA_ADMIN);

        if (!ctx.TryGet(1, out ulong steam64, out _))
            throw ctx.SendPlayerNotFound();

        if (ctx.MatchParameter(0, "grant", "give", "add"))
        {
            ctx.AssertHelpCheck(1, "/permisions grant <player> <admin|intern|helper|member>");

            if (TryGetType(ctx, out EAdminType type))
            {
                Task.Run(async () =>
                {
                    FPlayerName name = await F.GetPlayerOriginalNamesAsync(steam64);
                    await UCWarfare.ToUpdate();
                    EAdminType t = PermissionSaver.Instance.GetPlayerPermissionLevel(steam64);
                    if (t == type)
                        ctx.Reply("permissions_grant_already", Translation.TranslateEnum(type, ctx.CallerID), name.PlayerName, steam64.ToString(Data.Locale));
                    else
                    {
                        PermissionSaver.Instance.SetPlayerPermissionLevel(steam64, type);
                        string f = Translation.TranslateEnum(type, ctx.CallerID);
                        ctx.Reply("permissions_grant_success", f, name.PlayerName, steam64.ToString(Data.Locale));
                        ctx.LogAction(EActionLogType.PERMISSION_LEVEL_CHANGED, $"{steam64} {Translation.TranslateEnum(t, JSONMethods.DEFAULT_LANGUAGE)} >> {f}");
                    }
                });
                ctx.Defer();
            }
            else throw ctx.SendCorrectUsage("/permisions grant <player> <admin|intern|helper|member>");
        }
        else if (ctx.MatchParameter(0, "revoke", "remove", "leave"))
        {
            Task.Run(async () =>
            {
                FPlayerName name = await F.GetPlayerOriginalNamesAsync(steam64);
                await UCWarfare.ToUpdate();
                EAdminType t = PermissionSaver.Instance.GetPlayerPermissionLevel(steam64);
                if (t == EAdminType.MEMBER)
                    ctx.Reply("permissions_revoke_already", name.CharacterName, steam64.ToString(Data.Locale));
                else
                {
                    PermissionSaver.Instance.SetPlayerPermissionLevel(steam64, EAdminType.MEMBER);
                    ctx.Reply("permissions_revoke_success", name.CharacterName, steam64.ToString(Data.Locale));
                    ctx.LogAction(EActionLogType.PERMISSION_LEVEL_CHANGED, $"{steam64} {Translation.TranslateEnum(t, JSONMethods.DEFAULT_LANGUAGE)} >> {Translation.TranslateEnum(EAdminType.MEMBER, JSONMethods.DEFAULT_LANGUAGE)}");
                }
            });
            ctx.Defer();
        }
        else if (ctx.MatchParameter(0, "reload", "refresh"))
        {
            ReloadCommand.ReloadPermissions();
            ctx.Reply("reload_reloaded_permissions");
            ctx.LogAction(EActionLogType.RELOAD_COMPONENT, "PERMISSIONS");
        }
        else throw ctx.SendCorrectUsage(SYNTAX);
    }
    private bool TryGetType(CommandInteraction ctx, out EAdminType type)
    {
        if (ctx.MatchParameter(2, "admin", "admin-od"))
            type = EAdminType.ADMIN_OFF_DUTY;
        else if (ctx.MatchParameter(2, "intern", "trial", "intern-od"))
            type = EAdminType.ADMIN_OFF_DUTY;
        else if (ctx.MatchParameter(2, "helper", "help", "assistant"))
            type = EAdminType.HELPER;
        else if (ctx.MatchParameter(2, "member", "0", "none"))
            type = EAdminType.MEMBER;
        else
        {
            type = 0;
            return false;
        }
        return true;
    }
}
