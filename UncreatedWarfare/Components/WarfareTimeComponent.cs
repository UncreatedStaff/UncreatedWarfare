namespace Uncreated.Warfare.Components;
public sealed class WarfareTimeComponent : MonoBehaviour
{
    public uint Updates { get; private set; }
    public uint Ticks { get; private set; }

    [UsedImplicitly]
    private void OnUpdate()
    {
        ++Updates;
    }

    [UsedImplicitly]
    private void OnFixedUpdate()
    {
        ++Ticks;
    }
}
