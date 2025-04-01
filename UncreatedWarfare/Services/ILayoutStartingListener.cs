using Uncreated.Warfare.Layouts;

namespace Uncreated.Warfare.Services;

/// <summary>
/// Allows services to perform an action before each layout starts.
/// </summary>
public interface ILayoutStartingListener
{
    /// <summary>
    /// Invoked after the layout is initialized but before the first phase starts.
    /// </summary>
    UniTask HandleLayoutStartingAsync(Layout layout, CancellationToken token = default);
}
