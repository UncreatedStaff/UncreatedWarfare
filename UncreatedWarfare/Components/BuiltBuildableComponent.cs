using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare.FOBs;
public class BuiltBuildableComponent : MonoBehaviour
{
    public bool IsVehicle;
    public BarricadeDrop? Barricade { get; private set; }
    public InteractableVehicle? Vehicle { get; private set; }
    public BuildableData Buildable { get; private set; }
    public Dictionary<ulong, float> PlayerHits { get; private set; }
    public void Initialize(BarricadeDrop barricade, BuildableData buildable, Dictionary<ulong, float> playerHits)
    {
        Barricade = barricade;
        Buildable = buildable;
        PlayerHits = playerHits;
        IsVehicle = false;
    }
    public void Initialize(InteractableVehicle vehicle, BuildableData emplacement, Dictionary<ulong, float> playerHits)
    {
        Vehicle = vehicle;
        Buildable = emplacement;
        PlayerHits = playerHits;
        IsVehicle = true;
    }
    public float GetPlayerContribution(ulong player) => PlayerHits.TryGetValue(player, out float hits) ? hits / Buildable.RequiredHits : 0f;
}
