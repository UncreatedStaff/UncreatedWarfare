namespace Uncreated.Warfare.Events;

/// <summary>
/// Represents event arguments which can cancel the execution of their events.
/// </summary>
public interface ICancellable
{
    /// <summary>
    /// If this execution has been cancelled.
    /// </summary>
    bool IsCancelled { get; }

    /// <summary>
    /// If the action is also cancelled.
    /// </summary>
    bool IsActionCancelled { get; }

    /// <summary>
    /// Cancel the execution which will keep the next event listener from running.
    /// </summary>
    /// <param name="cancelAction">Also cancel whatever action the event represents, such as placing a structure.</param>
    void Cancel(bool cancelAction = true);

    /// <summary>
    /// Cancel only the action but allow more event handlers to be ran.
    /// </summary>
    void CancelAction();

    /// <summary>
    /// Resume (or un-cancel) the action.
    /// </summary>
    void ResumeAction();
}
