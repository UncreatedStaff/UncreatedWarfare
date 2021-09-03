using Steamworks;
using UnityEngine;
namespace Uncreated.Warfare.Components
{
    public class VehicleDamageOwnerComponent : MonoBehaviour
    {
        public CSteamID owner;
        public ushort item;
        public bool isVehicle = false;
    }
}
