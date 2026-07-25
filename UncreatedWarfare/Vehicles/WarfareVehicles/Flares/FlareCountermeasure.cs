namespace Uncreated.Warfare.Vehicles.WarfareVehicles.Flares;

internal class FlareCountermeasure : MonoBehaviour
{
    public bool Burning { get; private set; }

    public bool IsDestroyed { get; private set; }

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
        IsDestroyed = true;
        if (_vehicle != null)
            VehicleManager.askVehicleDestroy(_vehicle);
        else
            Destroy(gameObject);
    }

    private void OnDisable()
    {
        IsDestroyed = true;
    }

    private void OnEnable()
    {
        IsDestroyed = false;
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        ActiveCountermeasures.Remove(this);
        IsDestroyed = true;
    }
}
