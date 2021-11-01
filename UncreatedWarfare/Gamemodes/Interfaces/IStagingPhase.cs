using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IStagingPhase : IGamemode
    {
        int StagingSeconds { get; }

        void StartStagingPhase(int seconds);
        IEnumerator<WaitForSeconds> StagingPhaseLoop();
        void UpdateStagingUI(UCPlayer player, TimeSpan timeleft);
        void UpdateStagingUIForAll();
    }
}
