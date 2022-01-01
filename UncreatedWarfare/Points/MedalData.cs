using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Point
{
    public class MedalData
    {
        public readonly int TotalTW;
        public readonly int NumberOfMedals;
        public readonly int CurrentTW;
        public readonly int RequiredTW;
        public MedalData(int totalPoints)
        {
            TotalTW = totalPoints;

            int a = Points.TWConfig.FirstStarPoints;
            int d = Points.TWConfig.PointsIncreasePerStar;

            float x = d / 2f;
            float y = a - (3 * x);
            float z = 0 - x - y;

            NumberOfMedals = Mathf.RoundToInt(Mathf.Floor((x - a + Mathf.Sqrt(Mathf.Pow(a - x, 2f) + (2f * d * TotalTW))) / d));

            float n = NumberOfMedals + 1;

            RequiredTW = a + NumberOfMedals * d;

            CurrentTW = (int)(TotalTW - ((x * Math.Pow(n, 2)) + (y * n) + z));
        }
    }
}
