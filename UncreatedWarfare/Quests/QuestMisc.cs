using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.NewQuests;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Quests;

[Translatable("Weapon Class", IsPrioritizedTranslation = false)]
public enum WeaponClass : byte
{
    Unknown,
    [Translatable(Languages.ChineseSimplified, "突击步枪")]
    AssaultRifle,
    [Translatable(Languages.ChineseSimplified, "战斗步枪")]
    BattleRifle,
    MarksmanRifle,
    [Translatable(Languages.ChineseSimplified, "狙击步枪")]
    SniperRifle,
    [Translatable(Languages.ChineseSimplified, "机枪")]
    MachineGun,
    [Translatable(Languages.ChineseSimplified, "手枪")]
    Pistol,
    [Translatable(Languages.ChineseSimplified, "霰弹枪")]
    Shotgun,
    [Translatable(Languages.ChineseSimplified, "火箭筒")]
    [Translatable("Rocket Launcher")]
    Rocket,
    [Translatable(Languages.ChineseSimplified, "冲锋枪")]
    [Translatable("SMG")]
    SMG
}
[Translatable("Quest Type", Description = "Display names of quests.", IsPrioritizedTranslation = false)]
public enum QuestType : byte
{
    Invalid,
    /// <summary><see cref="KillEnemiesQuest"/></summary>
    KillEnemies,
    /// <summary><see cref="KillEnemiesQuestWeapon"/></summary>
    KillEnemiesWithWeapon,
    /// <summary><see cref="KillEnemiesQuestKit"/></summary>
    KillEnemiesWithKit,
    /// <summary><see cref="KillEnemiesQuestKitClass"/></summary>
    KillEnemiesWithKitClass,
    /// <summary><see cref="KillEnemiesQuestWeaponClass"/></summary>
    KillEnemiesWithWeaponClass,
    /// <summary><see cref="KillEnemiesQuestBranch"/></summary>
    KillEnemiesWithBranch,
    /// <summary><see cref="KillEnemiesQuestTurret"/></summary>
    KillEnemiesWithTurret,
    /// <summary><see cref="KillEnemiesQuestEmplacement"/></summary>
    KillEnemiesWithEmplacement,
    /// <summary><see cref="KillEnemiesQuestSquad"/></summary>
    KillEnemiesInSquad,
    /// <summary><see cref="KillEnemiesQuestFullSquad"/></summary>
    KillEnemiesInFullSquad,
    /// <summary><see cref="KillEnemiesQuestDefense"/></summary>
    [Translatable("Kill Enemies While Defending Point")]
    KillEnemiesOnPointDefense,
    /// <summary><see cref="KillEnemiesQuestAttack"/></summary>
    [Translatable("Kill Enemies While Attacking Point")]
    KillEnemiesOnPointAttack,
    /// <summary><see cref="HelpBuildQuest"/></summary>
    ShovelBuildables,
    /// <summary><see cref="BuildFOBsQuest"/></summary>
    [Translatable("Build FOBs")]
    BuildFOBs,
    /// <summary><see cref="BuildFOBsNearObjQuest"/></summary>
    [Translatable("Build FOBs Near Objectives")]
    BuildFOBsNearObjectives,
    /// <summary><see cref="BuildFOBsOnObjQuest"/></summary>
    [Translatable("Build FOBs Near Current Objective")]
    BuildFOBOnActiveObjective,
    /// <summary><see cref="DeliverSuppliesQuest"/></summary>
    DeliverSupplies,
    /// <summary><see cref="CaptureObjectivesQuest"/></summary>
    CaptureObjectives,
    /// <summary><see cref="DestroyVehiclesQuest"/></summary>
    DestroyVehicles,
    /// <summary><see cref="DriveDistanceQuest"/></summary>
    DriveDistance,
    /// <summary><see cref="TransportPlayersQuest"/></summary>
    TransportPlayers,
    /// <summary><see cref="RevivePlayersQuest"/></summary>
    RevivePlayers,
    /// <summary><see cref="KingSlayerQuest"/></summary>
    [Translatable("King-slayer")]
    KingSlayer,
    /// <summary><see cref="KillStreakQuest"/></summary>
    [Translatable("Killstreak")]
    KillStreak,
    /// <summary><see cref="XPInGamemodeQuest"/></summary>
    [Translatable("Earn XP From Gamemode")]
    XPInGamemode,
    /// <summary><see cref="KillEnemiesRangeQuest"/></summary>
    [Translatable("Kill From Distance")]
    KillFromRange,
    /// <summary><see cref="KillEnemiesRangeQuestWeapon"/></summary>
    [Translatable("Kill From Distance With Weapon")]
    KillFromRangeWithWeapon,
    /// <summary><see cref="KillEnemiesQuestKitClassRange"/></summary>
    [Translatable("Kill From Distance With Class")]
    KillFromRangeWithClass,
    /// <summary><see cref="KillEnemiesQuestKitRange"/></summary>
    [Translatable("Kill From Distance With Kit")]
    KillFromRangeWithKit,
    /// <summary><see cref="RallyUseQuest"/></summary>
    [Translatable("Teammates Use Rallypoint")]
    TeammatesDeployOnRally,
    /// <summary><see cref="FOBUseQuest"/></summary>
    [Translatable("Teammates Use FOB")]
    TeammatesDeployOnFOB,
    /// <summary><see cref="NeutralizeFlagsQuest"/></summary>
    NeutralizeFlags,
    /// <summary><see cref="WinGamemodeQuest"/></summary>
    WinGamemode,
    /// <summary><see cref="DiscordKeySetQuest"/></summary>
    [Translatable("Custom Key")]
    DiscordKeyBinary,
    /// <summary><see cref="PlaceholderQuest"/></summary>
    Placeholder
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class QuestDataAttribute(QuestType type) : Attribute
{
    public QuestType Type { get; } = type;
}

public static class QuestJsonEx
{
    public static bool IsWildcardInclusive<TValueType>(this QuestParameterValue<TValueType> choice)
    {
        return choice is { ValueType: ParameterValueType.Wildcard, SelectionType: ParameterSelectionType.Inclusive };
    }

    public static WeaponClass GetWeaponClass(this Guid item)
    {
        if (Assets.find(item) is not ItemGunAsset weapon)
            return WeaponClass.Unknown;

        if (weapon.action == EAction.Pump)
        {
            return WeaponClass.Shotgun;
        }
        if (weapon.action == EAction.Rail)
        {
            return WeaponClass.SniperRifle;
        }
        if (weapon.action == EAction.Minigun)
        {
            return WeaponClass.MachineGun;
        }
        if (weapon.action == EAction.Rocket)
        {
            return WeaponClass.Rocket;
        }
        if (weapon.itemDescription.IndexOf("smg", StringComparison.OrdinalIgnoreCase) != -1 ||
            weapon.itemDescription.IndexOf("sub", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return WeaponClass.SMG;
        }
        if (weapon.itemDescription.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return WeaponClass.Pistol;
        }
        if (weapon.itemDescription.IndexOf("marksman", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return WeaponClass.MarksmanRifle;
        }
        if (weapon.itemDescription.IndexOf("rifle", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return WeaponClass.BattleRifle;
        }
        if (weapon.itemDescription.IndexOf("machine", StringComparison.OrdinalIgnoreCase) != -1 || weapon.itemDescription.IndexOf("lmg", StringComparison.OrdinalIgnoreCase) != -1)
        {
            return WeaponClass.MachineGun;
        }

        return WeaponClass.Unknown;
    }
}

public interface INotifyTracker
{
    public WarfarePlayer? Player { get; }
}

public interface IQuestPreset
{
    public Guid Key { get; }
    public IQuestState State { get; }
    public IQuestReward[]? RewardOverrides { get; }
    public ulong TeamFilter { get; }
    public ushort Flag { get; }
}

/// <summary>Stores information about the values of variations of <see cref="BaseQuestData"/>.</summary>
public interface IQuestState
{
    public QuestParameterValue<int> FlagValue { get; }
    public bool IsEligible(WarfarePlayer player);
    public UniTask CreateFromConfigurationAsync(IConfiguration configuration, IServiceProvider serviceProvider, CancellationToken token = default);
}
/// <inheritdoc/>
/// <typeparam name="TQuestData">Class deriving from <see cref="BaseQuestData"/> used as a template for random variations to be created.</typeparam>
public interface IQuestState<in TQuestData> : IQuestState where TQuestData : QuestTemplate
{
    public UniTask CreateFromTemplateAsync(TQuestData data, CancellationToken token = default);
}