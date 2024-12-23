using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles;

[Translatable("Vehicle Type", Description = "Variant of vehicle, influences death messages, XP toasts, and spotting icons.")]
public enum VehicleType
{
    [Translatable("Unknown", IsPrioritizedTranslation = false)]
    None,

    [Translatable(Languages.Russian, "Хамви")]
    [Translatable(Languages.Spanish, "Humvee")]
    [Translatable(Languages.Romanian, "Humvee")]
    [Translatable(Languages.PortugueseBrazil, "Humvee")]
    [Translatable(Languages.Polish, "Humvee")]
    [Translatable(Languages.ChineseSimplified, "悍马")]
    Humvee,

    [Translatable(Languages.Russian, "Транспорт")]
    [Translatable(Languages.Spanish, "Transporte")]
    [Translatable(Languages.Romanian, "Transport")]
    [Translatable(Languages.PortugueseBrazil, "Transporte")]
    [Translatable(Languages.Polish, "Humvee")]
    [Translatable(Languages.ChineseSimplified, "运输卡车")]
    [Translatable("Transport Truck")]
    TransportGround,

    [Translatable(Languages.ChineseSimplified, "侦查车")]
    ScoutCar,

    [Translatable(Languages.Russian, "Логистический")]
    [Translatable(Languages.Spanish, "Logistico")]
    [Translatable(Languages.Romanian, "Camion")]
    [Translatable(Languages.PortugueseBrazil, "Logística")]
    [Translatable(Languages.Polish, "Transport Logistyczny")]
    [Translatable(Languages.ChineseSimplified, "补给卡车")]
    [Translatable("Logistics Truck")]
    LogisticsGround,

    [Translatable(Languages.Russian, "БТР")]
    [Translatable(Languages.Spanish, "APC")]
    [Translatable(Languages.Romanian, "TAB")]
    [Translatable(Languages.Polish, "APC")]
    [Translatable(Languages.ChineseSimplified, "轮式步战车")]
    [Translatable("APC")]
    APC,

    [Translatable(Languages.Russian, "БМП")]
    [Translatable(Languages.Spanish, "IFV")]
    [Translatable(Languages.Romanian, "MLI")]
    [Translatable(Languages.Polish, "BWP")]
    [Translatable(Languages.ChineseSimplified, "步战车")]
    [Translatable("IFV")]
    IFV,

    [Translatable(Languages.Russian, "ТАНК")]
    [Translatable(Languages.Spanish, "Tanque")]
    [Translatable(Languages.Romanian, "Tanc")]
    [Translatable(Languages.PortugueseBrazil, "Tanque")]
    [Translatable(Languages.Polish, "Czołg")]
    [Translatable(Languages.ChineseSimplified, "坦克")]
    [Translatable("Tank")]
    MBT,

    [Translatable(Languages.Russian, "Верталёт")]
    [Translatable(Languages.Spanish, "Helicoptero")]
    [Translatable(Languages.Romanian, "Elicopter")]
    [Translatable(Languages.PortugueseBrazil, "Helicóptero")]
    [Translatable(Languages.Polish, "Helikopter")]
    [Translatable(Languages.ChineseSimplified, "运输直升机")]
    [Translatable("Transport Heli")]
    TransportAir,

    [Translatable(Languages.Russian, "Верталёт")]
    [Translatable(Languages.Spanish, "Helicoptero")]
    [Translatable(Languages.Romanian, "Elicopter")]
    [Translatable(Languages.PortugueseBrazil, "Helicóptero")]
    [Translatable(Languages.Polish, "Helikopter")]
    [Translatable(Languages.ChineseSimplified, "武装直升机")]
    [Translatable("Attack Heli")]
    AttackHeli,

    [Translatable(Languages.Russian, "реактивный")]
    [Translatable(Languages.ChineseSimplified, "战斗机")]
    [Translatable("Jet")]
    Jet,

    [Translatable(Languages.Russian, "зенитный")]
    [Translatable(Languages.ChineseSimplified, "防空")]
    [Translatable("Anti-Aircraft")]
    AA,

    [Translatable(Languages.Russian, "Тяжелый пулемет")]
    [Translatable(Languages.ChineseSimplified, "重机枪")]
    [Translatable("Heavy Machine Gun")]
    HMG,

    [Translatable(Languages.Russian, "противотанковая ракета")]
    [Translatable(Languages.ChineseSimplified, "反坦克导弹发射器")]
    [Translatable("ATGM")]
    ATGM,

    [Translatable(Languages.Russian, "Миномет")]
    [Translatable(Languages.Spanish, "Mortero")]
    [Translatable(Languages.ChineseSimplified, "迫击炮")]
    [Translatable("Mortar")]
    Mortar
}