using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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

    public void HandleEvent(BarricadePlaced e, IServiceProvider serviceProvider)
    {
        FactionInfo? factionInfo = e.Owner?.Team?.Faction;
        BarricadeStateSave? save = _barricadeStateStore.FindBarricadeSave(e.Barricade.asset, factionInfo);

        if (save == null)
            return;
        
        try
        {
            byte[] state = Convert.FromBase64String(save.Base64State);

            // todo: finish VerifyState
            BarricadeData data = e.Barricade.GetServersideData();
            BarricadeUtility.WriteOwnerAndGroup(state, e.Barricade, data.owner, data.group);
            BarricadeUtility.SetState(e.Barricade, state);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning($"Failed to apply saved state onto barricade {e.Buildable} - it's saved state could not be deserialized from Base64. Detailed exception: {ex}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to apply saved state onto barricade {e.Buildable} - an unexpected exception was thrown: {ex}");
        }
    }

}