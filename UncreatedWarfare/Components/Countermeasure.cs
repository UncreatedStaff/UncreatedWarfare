using System.Collections.Generic;

namespace Uncreated.Warfare.Components;
internal class Countermeasure : MonoBehaviour
{
    public bool Burning { get; private set; }
    private float _startTime;
    public static List<Countermeasure> ActiveCountermeasures = new List<Countermeasure>();

    [UsedImplicitly]
    private void Start()
    {
        _startTime = Time.time;
        Burning = true;
        ActiveCountermeasures.Add(this);
    }

    [UsedImplicitly]
    private void FixedUpdate()
    {
        float elapsed = Time.time - _startTime;

        if (elapsed > 3f)
            Burning = false;

        if (elapsed > 8)
            Destroy(this);
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        ActiveCountermeasures.Remove(this);

        if (transform.TryGetComponent(out InteractableVehicle v))
            VehicleSpawner.DeleteVehicle(v);
    }
}
