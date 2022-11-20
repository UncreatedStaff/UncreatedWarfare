using UnityEngine;

namespace Uncreated.Warfare.Events.Components;
internal class DestroyerComponent : MonoBehaviour
{
    private ulong _destroyer;
    private float _time;
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
    public static ulong GetDestroyer(GameObject obj)
    {
        if (obj.TryGetComponent(out DestroyerComponent comp))
            return comp.Destroyer;
        return 0ul;
    }
    public static ulong GetDestroyer(GameObject obj, out float time)
    {
        if (obj.TryGetComponent(out DestroyerComponent comp))
        {
            time = comp._time;
            return comp.Destroyer;
        }

        time = Time.realtimeSinceStartup;
        return 0ul;
    }
    public static void AddOrUpdate(GameObject obj, ulong destroyer)
    {
        if (!obj.TryGetComponent(out DestroyerComponent comp))
            comp = obj.AddComponent<DestroyerComponent>();
        comp.Destroyer = destroyer;
    }
}
