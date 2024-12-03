using System;
using System.Collections.Generic;
using System.Text;

namespace Uncreated.Warfare.FOBs.SupplyCrates;
public enum SupplyChangeReason
{
    Unknown,
    ResupplyFob,
    ResupplyShoveableSalvaged,
    ConsumeGeneral,
    ConsumeShovelablePlaced,
    ConsumeRepairBuildable,
    ConsumeRepairVehicle,
    ConsumeSuppliesDestroyed
}
