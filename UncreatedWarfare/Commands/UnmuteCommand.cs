using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class UnmuteCommand : Command
{
    private const string SYNTAX = "/unmute";
    private const string HELP = "Does nothing.";

    public UnmuteCommand() : base("unmute", EAdminType.MODERATOR) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertArgs(1, SYNTAX);

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (ctx.TryGet(0, out ulong playerId, out UCPlayer? onlinePlayer))
        {
            if (onlinePlayer is not null)
			{
				if (onlinePlayer.MuteType == EMuteType.NONE || onlinePlayer.TimeUnmuted < DateTime.Now)
                {
                    ctx.Reply("unmute_not_muted", onlinePlayer.CharacterName);
                    return;
                }
			}
            Task.Run(async () =>
            {
                FPlayerName names = onlinePlayer is null ? await Data.DatabaseManager.GetUsernamesAsync(playerId) : F.GetPlayerOriginalNames(onlinePlayer);
                if (names.WasFound)
				{
					int rows = await Data.DatabaseManager.NonQueryAsync("UPDATE `muted` SET `Deactivated` = 1 WHERE `Steam64` = @0 AND " +
						"`Deactivated` = 0 AND (`Duration` = -1 OR TIME_TO_SEC(TIMEDIFF(`Timestamp`, NOW())) / -60 < `Duration`)", new object[] { playerId });
                    await UCWarfare.ToUpdate();
					if (rows == 0)
					{
						ctx.Reply("unmute_not_muted", names.CharacterName);
					}
                    else
					{
						if (onlinePlayer is null)
							onlinePlayer = UCPlayer.FromID(playerId);
						if (onlinePlayer is not null)
						{
							onlinePlayer.MuteReason = null;
							onlinePlayer.MuteType = EMuteType.NONE;
							onlinePlayer.TimeUnmuted = DateTime.MinValue;
						}
						if (ctx.IsConsole)
                        {
                            Chat.BroadcastToAllExcept(playerId, "unmute_unmuted_broadcast_operator", names.CharacterName);
                            onlinePlayer?.SendChat("unmute_unmuted_dm_operator");
                        }
                        else
						{
                            FPlayerName n2 = F.GetPlayerOriginalNames(ctx.Caller!);
							Chat.BroadcastToAllExcept(playerId, "unmute_unmuted_broadcast", names.CharacterName, n2.CharacterName);
							onlinePlayer?.SendChat("unmute_unmuted_dm", n2.CharacterName);
						}
                        ctx.LogAction(EActionLogType.UNMUTE_PLAYER, playerId.ToString() + " unmuted.");
                        ctx.Reply("unmute_unmuted", names.CharacterName);
                    }
				}
                else
				{
					await UCWarfare.ToUpdate();
		            ctx.Reply("unmute_not_found");
				}
            });
            ctx.Defer();
        }
        else ctx.Reply("unmute_not_found");
	}
}
