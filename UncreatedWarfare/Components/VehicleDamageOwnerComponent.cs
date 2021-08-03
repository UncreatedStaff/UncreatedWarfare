﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SDG.Unturned;
using Steamworks;
namespace Uncreated.Warfare.Components
{
    public class VehicleDamageOwnerComponent : MonoBehaviour
    {
        public CSteamID owner;
        public ushort item;
        public bool isVehicle = false;
    }
}
