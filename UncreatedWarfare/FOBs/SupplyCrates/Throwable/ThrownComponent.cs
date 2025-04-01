namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable;

public class ThrownComponent : MonoBehaviour
{
    public Action OnThrowableDestroyed;

    private void OnDestroy()
    {
        OnThrowableDestroyed?.Invoke();
    }
    
}