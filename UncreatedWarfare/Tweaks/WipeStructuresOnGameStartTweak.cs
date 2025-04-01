using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Tweaks;

public class WipeStructuresOnGameStartTweak : ILayoutStartingListener
{
    private readonly BuildableSaver _buildableSaver;

    public WipeStructuresOnGameStartTweak(BuildableSaver buildableSaver)
    {
        _buildableSaver = buildableSaver;
    }

    /// <inheritdoc />
    public async UniTask HandleLayoutStartingAsync(Layout layout, CancellationToken token = default)
    {
        await _buildableSaver.DestroyUnsavedBuildables(token: token);
    }
}