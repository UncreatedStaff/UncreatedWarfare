using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Maps;
internal class MapScheduler : MonoBehaviour
{
    internal static MapScheduler Instance;

    void Awake()
    {
        if (Instance != null)
            Destroy(Instance);
        Instance = this;

    }
}
