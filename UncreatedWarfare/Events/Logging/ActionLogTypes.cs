using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Events.Logging;

public static class ActionLogTypes
{
    public static ActionLogType Punch                   { get; } = new ActionLogType("Player punched",                      "PUNCH",                        1);
    public static ActionLogType BuildableDestroyed      { get; } = new ActionLogType("Buildable destroyed",                 "BUILDABLE_DESTROYED",          2);
    public static ActionLogType BuildablePlaced         { get; } = new ActionLogType("Buildable placed",                    "BUILDABLE_PLACED",             3);
    public static ActionLogType BuildableTransformed    { get; } = new ActionLogType("Buildable transformed",               "BUILDABLE_TRANSFORMED",        4);
    public static ActionLogType BuildableSignChanged    { get; } = new ActionLogType("Sign text changed",                   "BUILDABLE_SIGN_TEXT_CHANGED",  5);
    public static ActionLogType TrapTriggered           { get; } = new ActionLogType("Trap triggered",                      "TRAP_TRIGGERED",               6);
    public static ActionLogType FlagCaptured            { get; } = new ActionLogType("Flag captured",                       "FLAG_CAPTURED",                7);
    public static ActionLogType FlagNeutralized         { get; } = new ActionLogType("Flag neutralized",                    "FLAG_NEUTRALIZED",             8);
    public static ActionLogType FlagStateChanged        { get; } = new ActionLogType("Flag state changed",                  "FLAG_STATE_CHANGED",           9);
    public static ActionLogType FlagsSetUp              { get; } = new ActionLogType("Flags set up",                        "FLAGS_LOADED",                 10);
    public static ActionLogType ObjectiveChanged        { get; } = new ActionLogType("Objective changed",                   "OBJECTIVE_CHANGED",            11);
    public static ActionLogType PlayerEnteredObjective  { get; } = new ActionLogType("Player entered objective",            "ENTERED_OBJECTIVE",            12);
    public static ActionLogType PlayerExitedObjective   { get; } = new ActionLogType("Player exited objective",             "EXITED_OBJECTIVE",             13);
    public static ActionLogType FobBuilt                { get; } = new ActionLogType("FOB shoveled",                        "FOB_BUILT",                    14);
    public static ActionLogType FobCreated              { get; } = new ActionLogType("FOB created",                         "FOB_CREATED",                  15);
    public static ActionLogType FobRemoved              { get; } = new ActionLogType("FOB removed",                         "FOB_REMOVED",                  16);
    public static ActionLogType FobDestroyed            { get; } = new ActionLogType("FOB destroyed",                       "FOB_DESTROYED",                17);
    public static ActionLogType FobUpdated              { get; } = new ActionLogType("FOB updated",                         "FOB_UPDATED",                  18);
    public static ActionLogType ShovelableBuilt         { get; } = new ActionLogType("Buildable shoveled",                  "SHOVELABLE_BUILT",             19);
    public static ActionLogType KitRearmed              { get; } = new ActionLogType("Kit rearmed",                         "KIT_REARMED",                  20);
    public static ActionLogType DroppedItem             { get; } = new ActionLogType("Dropped item",                        "DROPPED_ITEM",                 21);
    public static ActionLogType ChangedKit              { get; } = new ActionLogType("Changed kit",                         "KIT_CHANGED",                  22);
    public static ActionLogType AidedPlayer             { get; } = new ActionLogType("Aided player",                        "AIDED_PLAYER",                 23);
    public static ActionLogType Chat                    { get; } = new ActionLogType("Send chat message",                   "CHAT",                         24);
    public static ActionLogType PlayerDamaged           { get; } = new ActionLogType("Damaged player",                      "DAMAGED_PLAYER",               25);
    public static ActionLogType PlayerDeployed          { get; } = new ActionLogType("Player deployed",                     "PLAYER_DEPLOYED",              26);
    public static ActionLogType PlayerDied              { get; } = new ActionLogType("Player died",                         "DEATH",                        27);
    public static ActionLogType KilledPlayer            { get; } = new ActionLogType("Player killed",                       "KILL",                         28);
    public static ActionLogType Teamkilled              { get; } = new ActionLogType("Player teamkilled",                   "TEAMKILL",                     29);
    public static ActionLogType TryConnect              { get; } = new ActionLogType("Attempt to connect to the server",    "TRY_CONNECT",                  30);
    public static ActionLogType Connect                 { get; } = new ActionLogType("Fully connect to the server",         "CONNECT",                      31);
    public static ActionLogType Disconnect              { get; } = new ActionLogType("Disconnect from the server",          "DISCONNECT",                   32);
    public static ActionLogType ChangeTeam              { get; } = new ActionLogType("Change teams",                        "CHANGE_TEAM",                  33);
    public static ActionLogType EnterVehicle            { get; } = new ActionLogType("Enter vehicle",                       "ENTER_VEHICLE",                34);
    public static ActionLogType LeaveVehicle            { get; } = new ActionLogType("Exit vehicle",                        "EXIT_VEHICLE",                 35);
    public static ActionLogType SwapVehicleSeats        { get; } = new ActionLogType("Swap vehicle seats",                  "SWAP_VEHICLE_SEATS",           36);
    public static ActionLogType VehicleExploded         { get; } = new ActionLogType("Vehicle exploded",                    "VEHICLE_EXPLODED",             37);
    public static ActionLogType VehicleDespawned        { get; } = new ActionLogType("Vehicle despawned",                   "VEHICLE_DESPAWNED",            38);
    public static ActionLogType PlayerEnteredZone       { get; } = new ActionLogType("Player entered zone",                 "ENTERED_ZONE",                 39);
    public static ActionLogType PlayerExitedZone        { get; } = new ActionLogType("Player exited zone",                  "EXITED_ZONE",                  40);
    public static ActionLogType PlayerInjured           { get; } = new ActionLogType("Player injured",                      "INJURED",                      41);
    public static ActionLogType Melee                   { get; } = new ActionLogType("Player meleed",                       "MELEE",                        42);
    public static ActionLogType FlagDiscovered          { get; } = new ActionLogType("Flag discovered",                     "FLAG_DISCOVERED",              43);
    public static ActionLogType CreatedKit              { get; } = new ActionLogType("Created kit",                         "KIT_CREATED",                  44);

    // 50-59 reserved for Insurgency
    // 60-69 reserved for Invasion

    private static readonly ActionLogType?[] TypesById;

    /// <summary>
    /// List of all action log types sorted in ascending order of <see cref="ActionLogType.Id"/>.
    /// </summary>
    public static IReadOnlyList<ActionLogType> All { get; }

    /// <summary>
    /// Get the action log type corresponding to the given <paramref name="id"/>.
    /// </summary>
    public static ActionLogType? FromId(ushort id)
    {
        if (id >= TypesById.Length)
            return null;

        return TypesById[id];
    }

    public static bool TryParse(ReadOnlySpan<char> span, [MaybeNullWhen(false)] out ActionLogType type)
    {
        if (span.IsEmpty)
        {
            type = null;
            return false;
        }

        char firstLetter = span[0];
        char otherFirstLetter = char.IsUpper(firstLetter) ? char.ToLowerInvariant(firstLetter) : char.ToUpperInvariant(firstLetter);

        foreach (ActionLogType? t in TypesById)
        {
            if (t == null)
                continue;

            if (t.LogName[0] != firstLetter && t.LogName[0] != otherFirstLetter)
                continue;

            if (t.LogName.AsSpan().Equals(span, StringComparison.OrdinalIgnoreCase))
            {
                type = t;
                return true;
            }
        }

        type = null;
        return false;
    }

    static ActionLogTypes()
    {
        PropertyInfo[] types = typeof(ActionLogTypes).GetProperties(BindingFlags.Public | BindingFlags.Static);

        List<ActionLogType> logTypes = new List<ActionLogType>();
        foreach (PropertyInfo prop in types)
        {
            if (prop.GetValue(null) is ActionLogType type)
                logTypes.Add(type);
        }

        logTypes.Sort((a, b) => a.Id.CompareTo(b.Id));

        int maxId = logTypes[^1].Id;

        TypesById = new ActionLogType[maxId + 1];
        foreach (ActionLogType type in logTypes)
        {
            TypesById[type.Id] = type;
        }

        All = new ReadOnlyCollection<ActionLogType>(logTypes.ToArray());
    }

    public static ActionLogType? FromLegacyType(ActionLogTypeOld legacyType)
    {
        return legacyType switch
        {
            ActionLogTypeOld.Disconnect => Disconnect,
            ActionLogTypeOld.GiveItem => null,
            ActionLogTypeOld.ChangeLanguage => null,
            ActionLogTypeOld.LoadSupplies => null,
            ActionLogTypeOld.LoadOldBans => null,
            ActionLogTypeOld.MutePlayer => null,
            ActionLogTypeOld.UnmutePlayer => null,
            ActionLogTypeOld.ReloadComponent => null,
            ActionLogTypeOld.RequestKit => null,
            ActionLogTypeOld.RequestVehicle => null,
            ActionLogTypeOld.ShutdownServer => null,
            ActionLogTypeOld.PopStructure => null,
            ActionLogTypeOld.SaveStructure => null,
            ActionLogTypeOld.UnsaveStructure => null,
            ActionLogTypeOld.SaveRequestSign => null,
            ActionLogTypeOld.UnsaveRequestSign => null,
            ActionLogTypeOld.AddWhitelist => null,
            ActionLogTypeOld.RemoveWhitelist => null,
            ActionLogTypeOld.SetWhitelistMaxAmount => null,
            ActionLogTypeOld.DestroyBarricade => BuildableDestroyed,
            ActionLogTypeOld.DestroyStructure => BuildableDestroyed,
            ActionLogTypeOld.PlaceBarricade => BuildablePlaced,
            ActionLogTypeOld.PlaceStructure => BuildablePlaced,
            ActionLogTypeOld.EnterVehicleSeat => EnterVehicle,
            ActionLogTypeOld.LeaveVehicleSeat => LeaveVehicle,
            ActionLogTypeOld.HelpBuildBuildable => ShovelableBuilt,
            ActionLogTypeOld.DeployToLocation => PlayerDeployed,
            ActionLogTypeOld.Teleport => null,
            ActionLogTypeOld.ChangeGamemodeCommand => null,
            ActionLogTypeOld.GamemodeChangedAuto => null,
            ActionLogTypeOld.TeamWon => null,
            ActionLogTypeOld.TeamCapturedObjective => null,
            ActionLogTypeOld.BuildZoneMap => null,
            ActionLogTypeOld.DischargeOfficer => null,
            ActionLogTypeOld.SetOfficerRank => null,
            ActionLogTypeOld.Injured => PlayerInjured,
            ActionLogTypeOld.RevivedPlayer => AidedPlayer,
            ActionLogTypeOld.Death => PlayerDied,
            ActionLogTypeOld.StartQuest => null,
            ActionLogTypeOld.MakeQuestProgress => null,
            ActionLogTypeOld.CompleteQuest => null,
            ActionLogTypeOld.XPChanged => null,
            ActionLogTypeOld.CreditsChanged => null,
            ActionLogTypeOld.CreatedSquad => null,
            ActionLogTypeOld.JoinedSquad => null,
            ActionLogTypeOld.LeftSquad => null,
            ActionLogTypeOld.DisbandedSquad => null,
            ActionLogTypeOld.LockedSquad => null,
            ActionLogTypeOld.UnlockedSquad => null,
            ActionLogTypeOld.PlacedRally => null,
            ActionLogTypeOld.TeleportedToRally => null,
            ActionLogTypeOld.CreatedOrder => null,
            ActionLogTypeOld.FufilledOrder => null,
            ActionLogTypeOld.OwnedVehicleDied => VehicleExploded,
            ActionLogTypeOld.ServerStartup => null,
            ActionLogTypeOld.CreateKit => null,
            ActionLogTypeOld.DeleteKit => null,
            ActionLogTypeOld.GiveKit => null,
            ActionLogTypeOld.ChangeKitAccess => null,
            ActionLogTypeOld.EditKit => null,
            ActionLogTypeOld.SetKitProperty => null,
            ActionLogTypeOld.CreateVehicleData => null,
            ActionLogTypeOld.DeleteVehicleData => null,
            ActionLogTypeOld.RegisteredSpawn => null,
            ActionLogTypeOld.DeregisteredSpawn => null,
            ActionLogTypeOld.LinkedVehicleBaySign => null,
            ActionLogTypeOld.UnlinkedVehicleBaySign => null,
            ActionLogTypeOld.SetVehicleDataProperty => null,
            ActionLogTypeOld.VehicleBayForceSpawn => null,
            ActionLogTypeOld.PermissionLevelChanged => null,
            ActionLogTypeOld.ChatFilterViolation => null,
            ActionLogTypeOld.KickedByBattlEye => null,
            ActionLogTypeOld.Teamkill => Teamkilled,
            ActionLogTypeOld.Kill => KilledPlayer,
            ActionLogTypeOld.RequestTrait => null,
            ActionLogTypeOld.SetSavedStructureProperty => null,
            ActionLogTypeOld.SetTraitProperty => null,
            ActionLogTypeOld.GiveTrait => null,
            ActionLogTypeOld.RevokeTrait => null,
            ActionLogTypeOld.ClearTraits => null,
            ActionLogTypeOld.MainCampAttempt => null,
            ActionLogTypeOld.LeftMain => PlayerExitedZone,
            ActionLogTypeOld.PossibleSolo => null,
            ActionLogTypeOld.SoloRTB => null,
            ActionLogTypeOld.EnterMain => PlayerEnteredZone,
            ActionLogTypeOld.Attach => null,
            ActionLogTypeOld.Detach => null,
            ActionLogTypeOld.SetAmmo => null,
            ActionLogTypeOld.SetFiremode => null,
            ActionLogTypeOld.AddSkillset => null,
            ActionLogTypeOld.RemoveSkillset => null,
            ActionLogTypeOld.NitroBoostStateUpdated => null,
            ActionLogTypeOld.UpgradeLoadout => null,
            ActionLogTypeOld.UnlockLoadout => null,
            ActionLogTypeOld.IPWhitelist => null,
            ActionLogTypeOld.ChangeCulture => null,
            ActionLogTypeOld.ForgiveModerationEntry => null,
            ActionLogTypeOld.EditModerationEntry => null,
            ActionLogTypeOld.CreateModerationEntry => null,
            ActionLogTypeOld.RemoveModerationEntry => null,
            ActionLogTypeOld.LockLoadout => null,
            ActionLogTypeOld.ReputationChanged => null,
            _ => null
        };
    }
}