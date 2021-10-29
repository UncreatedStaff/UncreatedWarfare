using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IStagingPhase
    {
        int Seconds { get; set; }

        bool IsActive { get; set; }

        void StartStagingPhase(int seconds);
        IEnumerator<WaitForSeconds> StagingPhaseLoop();
    }

    public abstract class StagingPhase : MonoBehaviour, IStagingPhase
    {
        public int Seconds { get; set; }
        public bool IsActive { get; set; }

        public virtual void StartStagingPhase(int seconds)
        {
            IsActive = true;
            Seconds = seconds;

            StartCoroutine(StagingPhaseLoop());
        }
        public IEnumerator<WaitForSeconds> StagingPhaseLoop()
        {
            OnStagingPhaseStarted();

            if (!IsActive)
                yield break;

            while (Seconds > 0)
            {

                // update UI

                yield return new WaitForSeconds(1);
                Seconds -= 1;
            }

            OnStagingPhaseEnded();

        }
        public abstract void OnStagingPhaseEnded();
        public abstract void OnStagingPhaseStarted();
    }
}
