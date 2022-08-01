using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Interfaces;

public interface IStagingPhase : IGamemode
{
    int StagingSeconds { get; }

    void StartStagingPhase(int seconds);
    IEnumerator<WaitForSeconds> StagingPhaseLoop();
    void ShowStagingUI(UCPlayer player);
    void ShowStagingUIForAll();
    void UpdateStagingUI(UCPlayer player, TimeSpan timeleft);
    void UpdateStagingUIForAll();
    void SkipStagingPhase();
    void ClearStagingUI(UCPlayer player);
}
