using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Tweaks;

internal sealed class DisallowGroupsTweak : IHostedService
{
    public UniTask StartAsync(CancellationToken token)
    {
        Provider.modeConfigData.Gameplay.Allow_Dynamic_Groups = false;
        Provider.modeConfigData.Gameplay.Allow_Static_Groups = false;
        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
}
