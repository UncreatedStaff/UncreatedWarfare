using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Events.Barricades;
public class LandmineExploding : BreakableEvent
{
    private readonly UCPlayer? owner;
    private readonly BarricadeDrop barricade;
    private readonly InteractableTrap trap;
    private readonly UCPlayer triggerer;
    private readonly GameObject triggerObject;
    public UCPlayer? BarricadeOwner => owner;
    public BarricadeDrop TrapBarricade => barricade;
    public InteractableTrap Trap => trap;
    public UCPlayer Triggerer => triggerer;
    public GameObject TriggerObject => triggerObject;
    public LandmineExploding(UCPlayer? owner, BarricadeDrop barricade, InteractableTrap trap, UCPlayer triggerer, GameObject triggerObject, bool shouldExplode)
    {
        this.owner = owner;
        this.barricade = barricade;
        this.trap = trap;
        this.triggerer = triggerer;
        this.triggerObject = triggerObject;
        if (!shouldExplode) Break();
    }
}
