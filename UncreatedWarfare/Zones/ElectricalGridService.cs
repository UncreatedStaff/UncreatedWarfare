using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Zones;
public class ElectricalGridService : IHostedService
{
    private readonly ILogger<ElectricalGridService> _logger;

    public bool Enabled { get; private set; }

    public ElectricalGridService(ILogger<ElectricalGridService> logger)
    {
        _logger = logger;
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        // todo run this on level load.
        return UniTask.CompletedTask;
        
        if (!Level.info.configData.Has_Global_Electricity)
        {
            _logger.LogWarning("Level does not have global electricity enabled, electrical grid effects will not work!");
            Enabled = false;
        }
        else
        {
            Enabled = true;
        }

        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
}
