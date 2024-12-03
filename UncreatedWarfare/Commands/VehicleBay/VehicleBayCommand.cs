using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Commands;

[Command("vehiclebay", "vb"), MetadataFile]
public class VehicleBayCommand : ICommand;
public class VehicleBayCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Vehicle Bay";

    [TranslationData("Sent when a vehicle indentifier is invalid.", IsPriorityTranslation = false)]
    public readonly Translation<string> InvalidVehicleAsset = new Translation<string>("<#ff8c69><#fff>{0}</color> is not a valid vehicle asset.");
    
    [TranslationData("Sent when a vehicle bay vehicle is not a known vehicle.", IsPriorityTranslation = false)]
    public readonly Translation<string> VehicleNotRegistered = new Translation<string>("<#ff8c69><#fff>{0}</color> is not registered in the vehicle bay. Did you forget to configure it?");

    [TranslationData("Sent when a the provided unique name is already taken by another existing vehicle spawner.", IsPriorityTranslation = false)]
    public readonly Translation<string> NameNotUnique = new Translation<string>("<#ff8c69>The unique name '<#fff>{0}</color>' is already taken by an existing vehicle spawner.");

    [TranslationData("Sent when a player is not looking at a vehicle bay.", IsPriorityTranslation = false)]
    public readonly Translation NoTarget = new Translation("<#ff8c69>Look at a vehicle, spawn pad, or sign to use this command.");

    [TranslationData("Sent when a player tries to register a spawner that is already registered with the same vehicle type.", IsPriorityTranslation = false)]
    public readonly Translation<VehicleAsset> SpawnAlreadyRegistered = new Translation<VehicleAsset>("<#ff8c69>This spawn is already registered to a {0}.", arg0Fmt: RarityColorAddon.Instance);

    [TranslationData("Sent after a spawner is linked to a vehicle type.", IsPriorityTranslation = false)]
    public readonly Translation<string, VehicleAsset> SpawnRegistered = new Translation<string, VehicleAsset>("<#a0ad8e>Successfully registered spawn '{0}'. {1} will spawn here.", arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance));
    
    [TranslationData("Sent after a spawner is unlinked from a vehicle type.", IsPriorityTranslation = false)]
    public readonly Translation<string, VehicleAsset> SpawnDeregistered = new Translation<string, VehicleAsset>("<#a0ad8e>Successfully deregistered spawn '{0}'. {1} will no longer spawn here.", arg0Fmt: new ArgumentFormat(PluralAddon.Always(), RarityColorAddon.Instance));

    [TranslationData("Sent when a player tries to deregister a spawner that isn't registered.", IsPriorityTranslation = false)]
    public readonly Translation SpawnNotRegistered = new Translation("<#ff8c69>This vehicle bay is not registered.");
    
    [TranslationData("Sent when a player tries to unlink a sign that isn't linked.", IsPriorityTranslation = false)]
    public readonly Translation SignNotLinked = new Translation("<#ff8c69>This sign isn't linked to any vehicle bays.");

    [TranslationData("Sent after a player forces a vehicle to respawn, possibly destroying the active vehicle.", IsPriorityTranslation = false)]
    public readonly Translation<VehicleAsset> VehicleBayForceSuccess = new Translation<VehicleAsset>("<#a0ad8e>Skipped timer for {0}.", arg0Fmt: RarityColorAddon.Instance);

    [TranslationData("Sent to simply see what vehicle is registered on a spawner.", IsPriorityTranslation = false)]
    public readonly Translation<string, uint, VehicleAsset, Guid> VehicleBayCheck = new Translation<string, uint, VehicleAsset, Guid>("<#a0ad8e>This spawn '{0}' (<#8ce4ff>{1}</color>) is registered with vehicle: {2} <#fff>({3})</color>.", arg2Fmt: RarityColorAddon.Instance, arg3Fmt: "N");

    [TranslationData("Sent to a player after they start linking a sign and spawner.", IsPriorityTranslation = false)]
    public readonly Translation VehicleBayLinkStarted = new Translation("<#a0ad8e>Started linking, do <#ddd>/vb link</color> on the sign now.");

    [TranslationData("Sent to a player after they complete a link between a sign and spawner.", IsPriorityTranslation = false)]
    public readonly Translation<VehicleAsset> VehicleBayLinkFinished = new Translation<VehicleAsset>("<#a0ad8e>Successfully linked vehicle sign to a {0} vehicle bay.", arg0Fmt: RarityColorAddon.Instance);

    [TranslationData("Sent to a player after they remove a link between a sign and spawner.", IsPriorityTranslation = false)]
    public readonly Translation<VehicleAsset> VehicleBayUnlinked = new Translation<VehicleAsset>("<#a0ad8e>Successfully unlinked {0} vehicle sign.", arg0Fmt: RarityColorAddon.Instance);
}