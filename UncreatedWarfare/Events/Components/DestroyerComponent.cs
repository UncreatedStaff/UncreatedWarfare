namespace Uncreated.Warfare.Events.Components;
internal class DestroyerComponent : MonoBehaviour
{
    private ulong _destroyer;
    private float _time;
    public EDamageOrigin DamageOrigin { get; private set; }
    public float RelevantTime => _time;
    public ulong Destroyer
    {
        get => _destroyer;
        set
        {
            _destroyer = value;
            _time = Time.realtimeSinceStartup;
        }
    }
    public static ulong GetDestroyer(GameObject obj, out EDamageOrigin origin)
    {
        if (obj.TryGetComponent(out DestroyerComponent comp) && Time.realtimeSinceStartup - comp.RelevantTime < 5f)
        {
            origin = comp.DamageOrigin;
            return comp.Destroyer;
        }

        origin = EDamageOrigin.Unknown;
        return 0ul;
    }
    public static ulong GetDestroyer(GameObject obj, out EDamageOrigin origin, out float time)
    {
        if (obj.TryGetComponent(out DestroyerComponent comp))
        {
            time = comp._time;
            origin = comp.DamageOrigin;
            return comp.Destroyer;
        }

        origin = EDamageOrigin.Unknown;
        time = Time.realtimeSinceStartup;
        return 0ul;
    }
    public static void AddOrUpdate(GameObject obj, ulong destroyer, EDamageOrigin origin)
    {
        if (!obj.TryGetComponent(out DestroyerComponent comp))
            comp = obj.AddComponent<DestroyerComponent>();
        comp.DamageOrigin = origin;
        comp.Destroyer = destroyer;
    }
    public static void AddOrUpdate(GameObject obj, ulong destroyer, EDamageOrigin origin, float time)
    {
        if (!obj.TryGetComponent(out DestroyerComponent comp))
            comp = obj.AddComponent<DestroyerComponent>();
        comp.DamageOrigin = origin;
        comp._destroyer = destroyer;
        comp._time = time;
    }
}
