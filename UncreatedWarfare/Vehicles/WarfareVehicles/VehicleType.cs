using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles;

[Translatable("Vehicle Type", Description = "Variant of vehicle, influences death messages, XP toasts, and spotting icons.")]
public enum VehicleType
{
    [TranslatableValue("Unknown", IsPrioritizedTranslation = false)]
    None,

    Humvee,

    [TranslatableValue("Transport Truck")]
    TransportGround,

    [TranslatableValue("Scout Car")]
    ScoutCar,

    [TranslatableValue("Logistics Truck")]
    LogisticsGround,

    [TranslatableValue("APC")]
    APC,

    [TranslatableValue("IFV")]
    IFV,

    [TranslatableValue("Tank")]
    MBT,

    [TranslatableValue("Transport Heli")]
    TransportHeli,

    [TranslatableValue("Attack Heli")]
    AttackHeli,

    [TranslatableValue("Jet")]
    Jet,

    [TranslatableValue("Anti-Aircraft", Description = "Emplacement")]
    AA,

    [TranslatableValue("Heavy Machine Gun", Description = "Emplacement")]
    HMG,

    [TranslatableValue("ATGM", Description = "Emplacement")]
    ATGM,

    [TranslatableValue("Mortar", Description = "Emplacement")]
    Mortar
}