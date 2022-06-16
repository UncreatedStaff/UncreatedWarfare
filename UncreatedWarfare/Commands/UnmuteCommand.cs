using Rocket.API;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Players;
namespace Uncreated.Warfare.Commands;

public class UnmuteCommand : IRocketCommand
{
	private readonly List<string> _permissions = new List<string>(1) { "uc.unmute" };
	private readonly List<string> _aliases = new List<string>(0);
	public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "unmute";
    public string Help => "Unmute previously muted players.";
    public string Syntax => "/unmute <player>";
    public List<string> Aliases => _aliases;
	public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
        UCCommandContext ctx = new UCCommandContext(caller, command);
        if (!ctx.HasArg(0))
            ctx.SendCorrectUsage(Syntax);
        if (ctx.MatchParameter(0, "help"))
            ctx.SendCorrectUsage(Syntax + " - " + Help);
        else if (ctx.TryGet(0, out ulong playerId, out UCPlayer? onlinePlayer))
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
        }
        else ctx.Reply("unmute_not_found");
	}
}
