using System.Collections.Generic;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles.Flares;

internal class FlareCountermeasure : MonoBehaviour
{
    public bool Burning { get; private set; }

    public static readonly List<FlareCountermeasure> ActiveCountermeasures = [];

    private InteractableVehicle? _vehicle;

    [UsedImplicitly]
    private IEnumerator Start()
    {
        Burning = true;
        ActiveCountermeasures.Add(this);
        _vehicle = GetComponent<InteractableVehicle>();

        yield return new WaitForSeconds(3f);
        Burning = false;

        yield return new WaitForSeconds(5f);
        if (_vehicle != null)
            VehicleManager.askVehicleDestroy(_vehicle);
        else
            Destroy(gameObject);
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        ActiveCountermeasures.Remove(this);
    }
}
