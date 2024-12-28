using System.Collections.Generic;

namespace Uncreated.Warfare.Components;
internal class FlareCountermeasure : MonoBehaviour
{
    public bool Burning { get; private set; }
    private float _startTime;
    public static readonly List<FlareCountermeasure> ActiveCountermeasures = [];

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
            Destroy(gameObject);
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        ActiveCountermeasures.Remove(this);
    }
}
