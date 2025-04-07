namespace Uncreated.Warfare.Events;

/// <summary>
/// Handles syncing OnXxxRequested events so that only one can run at a time.
/// </summary>
public class EventSynchronizer
{
    public EventSynchronizer()
    {

    }

    public async UniTask EnterEvent<TEventArgs>(TEventArgs args, EventModelAttribute modelInfo) where TEventArgs : class
    {
        if (modelInfo.SynchronizationContext is not EventSynchronizationContext.PerPlayer and not EventSynchronizationContext.Global)
        {
            return;
        }
    }
}
