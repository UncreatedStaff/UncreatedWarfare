using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Tweaks;

internal sealed class KeepItemsAndStatsOnDeathTweak : IHostedService
{
    public UniTask StartAsync(CancellationToken token)
    {
        Provider.modeConfigData.Players.Lose_Items_PvP = 0f;
        Provider.modeConfigData.Players.Lose_Items_PvE = 0f;
        Provider.modeConfigData.Players.Lose_Clothes_PvP = false;
        Provider.modeConfigData.Players.Lose_Clothes_PvE = false;
        Provider.modeConfigData.Players.Lose_Weapons_PvP = false;
        Provider.modeConfigData.Players.Lose_Weapons_PvE = false;
        Provider.modeConfigData.Players.Lose_Experience_PvE = 1f;
        Provider.modeConfigData.Players.Lose_Experience_PvP = 1f;
        Provider.modeConfigData.Players.Lose_Skills_PvE = 1f;
        Provider.modeConfigData.Players.Lose_Skills_PvP = 1f;
        Provider.modeConfigData.Players.Lose_Skill_Levels_PvE = 0;
        Provider.modeConfigData.Players.Lose_Skill_Levels_PvP = 0;
        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
}
