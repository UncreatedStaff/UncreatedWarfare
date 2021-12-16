using Steamworks;
using System;
using UnityEngine;
namespace Uncreated.Warfare.Components
{
    public class VehicleComponent : MonoBehaviour
    {
        public CSteamID owner;
        public Guid item;
        public bool isVehicle = false;
    }
}
