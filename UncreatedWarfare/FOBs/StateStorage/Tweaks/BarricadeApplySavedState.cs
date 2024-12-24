using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.StateStorage.Tweaks;

public class BarricadeApplySavedStateTweaks : IEventListener<BarricadePlaced>
{
    private readonly ILogger _logger;
    private readonly BarricadeStateStore _barricadeStateStore;

    public BarricadeApplySavedStateTweaks(ILogger<BarricadeApplySavedStateTweaks> logger, BarricadeStateStore barricadeStateStore)
    {
        _logger = logger;
        _barricadeStateStore = barricadeStateStore;

    }

    void IEventListener<BarricadePlaced>.HandleEvent(BarricadePlaced e, IServiceProvider serviceProvider)
    {
        FactionInfo? factionInfo = e.Owner?.Team?.Faction;
        BarricadeStateSave? save = _barricadeStateStore.FindBarricadeSave(e.Barricade.asset, factionInfo);

        if (save == null)
            return;
        
        try
        {
            byte[] state = Convert.FromBase64String(save.Base64State);

            // known issue: sometimes owner doesn't get updated client-side until the owner relogs.
            BarricadeData data = e.Barricade.GetServersideData();
            BarricadeUtility.WriteOwnerAndGroup(state, e.Barricade, data.owner, data.group);
            BarricadeUtility.SetState(e.Barricade, state);
            _logger.LogDebug("Updated state of {0}: {1}.", e.Buildable, data.barricade.state);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Failed to apply saved state onto barricade {0} - it's saved state could not be deserialized from Base64.", e.Buildable);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply saved state onto barricade {0} - an unexpected exception was thrown.", e.Buildable);
        }
    }
}