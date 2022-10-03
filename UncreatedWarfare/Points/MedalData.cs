using System;
using UnityEngine;

namespace Uncreated.Warfare.Point
{
    public struct MedalData
    {
        public static readonly MedalData Nil = new MedalData(-1);
        public int TotalTW;
        public int NumberOfMedals;
        public int CurrentTW;
        public int RequiredTW;
        public MedalData(int totalPoints)
        {
            TotalTW = totalPoints;
            NumberOfMedals = -1;
            RequiredTW = -1;
            CurrentTW = -1;
            Update(totalPoints);
        }
        public void Update(int totalPoints)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            TotalTW = totalPoints;

            int a = Points.TWConfig.FirstMedalPoints;
            int d = Points.TWConfig.PointsIncreasePerMedal;

            float x = d / 2f;
            float y = a - (3 * x);
            float z = 0 - x - y;

            NumberOfMedals = Mathf.RoundToInt(Mathf.Floor((x - a + Mathf.Sqrt(Mathf.Pow(a - x, 2f) + (2f * d * TotalTW))) / d));

            float n = NumberOfMedals + 1;

            RequiredTW = a + NumberOfMedals * d;

            CurrentTW = (int)(TotalTW - ((x * Math.Pow(n, 2)) + (y * n) + z));
        }

        public bool IsNil => TotalTW == -1;
    }
}
