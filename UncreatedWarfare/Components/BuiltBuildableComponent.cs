using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.FOBs;
public class BuiltBuildableComponent : MonoBehaviour
{
    public bool IsVehicle;
    public BarricadeDrop? Barricade { get; private set; }
    public InteractableVehicle? Vehicle { get; private set; }
    public BuildableData Buildable { get; private set; }
    public Dictionary<ulong, int> PlayerHits { get; private set; }
    public void Initialize(BarricadeDrop barricade, BuildableData buildable, Dictionary<ulong, int> playerHits)
    {
        Barricade = barricade;
        Buildable = buildable;
        PlayerHits = playerHits;
        IsVehicle = false;
    }
    public void Initialize(InteractableVehicle vehicle, BuildableData emplacement, Dictionary<ulong, int> playerHits)
    {
        Vehicle = vehicle;
        Buildable = emplacement;
        PlayerHits = playerHits;
        IsVehicle = true;
    }
    public float GetPlayerContribution(ulong player) => PlayerHits.TryGetValue(player, out int hits) ? hits / (float)Buildable.requiredHits : 0f;
}
