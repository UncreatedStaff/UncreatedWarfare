using System;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Stats;

namespace Uncreated.Warfare.Players.PendingTasks;

[PlayerTask]
internal class CheckReputationPlayerTask(IPointsStore pointsSql) : IPlayerPendingTask
{
    private double _reputation;
    public async Task<bool> RunAsync(PlayerPending e, CancellationToken token)
    {
        _reputation = await pointsSql.GetReputationAsync(e.Steam64, token).ConfigureAwait(false);
        return true;
    }

    public void Apply(WarfarePlayer player)
    {
        Patches.CancelReputationPatch.IsSettingReputation = true;
        try
        {
            PlayerSkills playerSkill = player.UnturnedPlayer.skills;

            playerSkill.askRep((int)Math.Round(_reputation) - playerSkill.reputation);
        }
        finally
        {
            Patches.CancelReputationPatch.IsSettingReputation = false;
        }

        player.CachedReputation = _reputation;
    }

    bool IPlayerPendingTask.CanReject => false;
}