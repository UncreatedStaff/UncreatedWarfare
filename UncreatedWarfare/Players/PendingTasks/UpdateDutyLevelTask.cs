using System;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Players.PendingTasks;

/// <summary>
/// Updates a player's duty status.
/// </summary>
[PlayerTask]
public class UpdateDutyLevelTask : IPlayerPendingTask
{
    private readonly DutyService _dutyService;

    private DutyLevel _dutyLevel;
    private bool _isOnDuty;

    public UpdateDutyLevelTask(DutyService dutyService)
    {
        _dutyService = dutyService;
    }

    async Task<bool> IPlayerPendingTask.RunAsync(PlayerPending e, CancellationToken token)
    {
        (_dutyLevel, _isOnDuty) = await _dutyService.CheckDutyStateAsync(e.Steam64, false, token).ConfigureAwait(false);
        return true;
    }

    void IPlayerPendingTask.Apply(WarfarePlayer player)
    {
        player.UpdateDutyState(_isOnDuty, _dutyLevel);
        UniTask.Create(async () =>
        {
            try
            {
                await (player.IsOnDuty
                    ? _dutyService.ApplyOnDuty(player, _dutyLevel)
                    : _dutyService.ApplyOffDuty(player, _dutyLevel));
            }
            catch (Exception ex)
            {
                WarfareModule.Singleton.GlobalLogger.LogError(ex, "Error in UpdateDutyLevelTask.Apply.");
            }
        });
    }

    bool IPlayerPendingTask.CanReject => false;
}
