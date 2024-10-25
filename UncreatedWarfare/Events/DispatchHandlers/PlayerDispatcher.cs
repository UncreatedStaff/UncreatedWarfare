using System.Diagnostics;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher2
{
    /// <summary>
    /// Invoked by <see cref="DamageTool.damagePlayerRequested"/> when a player starts to get damaged. Can be cancelled.
    /// </summary>
    public void DamageToolOnPlayerDamageRequested(ref DamagePlayerParameters parameters, ref bool shouldallow)
    {
        if (!shouldallow || parameters.times == 0f)
            return;

        WarfarePlayer player = _playerService.GetOnlinePlayer(parameters.player);

        DamagePlayerRequested args = new DamagePlayerRequested(in parameters, _playerService)
        {
            Player = player
        };

        // can't support async event handlers because any code calling damagePlayer
        // may expect the player to take damage or die instantly
        //  ex. hitmarkers are handled by checking which players were damaged immediately after shooting
        shouldallow = DispatchEventAsync(args, _unloadToken, allowAsync: false).GetAwaiter().GetResult();

        if (shouldallow)
            parameters = args.Parameters;
    }

    private void UseableConsumeableOnPlayerPerformingAid(Player instigator, Player target, ItemConsumeableAsset asset, ref bool shouldAllow)
    {
        WarfarePlayer medic = _playerService.GetOnlinePlayer(instigator);
        WarfarePlayer player = _playerService.GetOnlinePlayer(target);

        AidPlayerRequested args = new AidPlayerRequested
        {
            Item = AssetLink.Create(asset),
            Player = player,
            Medic = medic,
            IsRevive = false
        };

        shouldAllow = DispatchEventAsync(args, _unloadToken, allowAsync: false).GetAwaiter().GetResult();

        if (shouldAllow && args.IsRevive)
        {
            player.ComponentOrNull<PlayerInjureComponent>()?.PrepAidRevive(args);
        }
    }

    /// <summary>
    /// Invoked by <see cref="PlayerEquipment.OnPunch_Global"/> when a player punches with either hand.
    /// </summary>
    private void PlayerEquipmentOnPlayerPunch(PlayerEquipment player, EPlayerPunch punchType)
    {
        PlayerPunched args = new PlayerPunched
        {
            Player = _playerService.GetOnlinePlayer(player.player),
            PunchType = punchType
        };

        _ = DispatchEventAsync(args, _unloadToken);
    }

    /// <summary>
    /// Invoked by <see cref="PlayerQuests.onGroupChanged"/> when a player's group ID or rank chnages.
    /// </summary>
    private void PlayerQuestsOnGroupChanged(PlayerQuests sender, CSteamID oldGroupId, EPlayerGroupRank oldGroupRank, CSteamID newGroupId, EPlayerGroupRank newGroupRank)
    {
        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(sender);
        if (player == null)
        {
            // this can get invoked before the player's WarfarePlayer gets created when moving them into their team.
            return;
        }

        ITeamManager<Team>? teamManager = _warfare.IsLayoutActive() ? _warfare.GetActiveLayout().TeamManager : null;

        Team oldTeam = Team.NoTeam,
             newTeam = Team.NoTeam;

        if (teamManager != null)
        {
            oldTeam = teamManager.GetTeam(oldGroupId);
            newTeam = teamManager.GetTeam(newGroupId);

            player.UpdateTeam(newTeam);
        }
        else
        {
            player.UpdateTeam(Team.NoTeam);
        }

        PlayerGroupChanged args = new PlayerGroupChanged
        {
            Player = player,
            OldGroupId = oldGroupId,
            NewGroupId = newGroupId,
            OldRank = oldGroupRank,
            NewRank = newGroupRank,
            OldTeam = oldTeam,
            NewTeam = newTeam
        };

        _ = DispatchEventAsync(args, _unloadToken);
    }
}
