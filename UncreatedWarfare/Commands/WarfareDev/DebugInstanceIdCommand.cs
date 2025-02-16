using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("instanceid", "instid"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugInstanceIdCommand : IExecutableCommand
{
    public required CommandContext Context { get; init; }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGetTargetInfo(out RaycastInfo? raycast, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.LARGE | RayMasks.MEDIUM | RayMasks.SMALL | RayMasks.VEHICLE))
        {
            throw Context.ReplyString("You must be looking at a barricade, structure, vehicle, or object.");
        }

        if (raycast.vehicle != null)
        {
            Context.ReplyString($"Vehicle {raycast.vehicle.asset.vehicleName}: #{raycast.vehicle.instanceID.ToString(Context.Culture)}");
            return UniTask.CompletedTask;
        }

        if (raycast.player != null)
        {
            Context.ReplyString($"Player {raycast.player.channel.owner.playerID.playerName}: #{raycast.player.channel.owner.playerID.steamID.m_SteamID.ToString(Context.Culture)} (@ {raycast.limb})");
            return UniTask.CompletedTask;
        }

        BarricadeDrop? barricade = BarricadeManager.FindBarricadeByRootTransform(raycast.transform);
        if (barricade != null)
        {
            Context.ReplyString($"Barricade {barricade.asset.itemName}: #{barricade.instanceID.ToString(Context.Culture)}");
            return UniTask.CompletedTask;
        }

        StructureDrop? structure = StructureManager.FindStructureByRootTransform(raycast.transform);
        if (structure != null)
        {
            Context.ReplyString($"Structure {structure.asset.itemName}: #{structure.instanceID.ToString(Context.Culture)}");
            return UniTask.CompletedTask;
        }

        if (LevelObjectUtility.FindObject(raycast.transform) is { HasValue: true } obj)
        {
            Context.ReplyString($"Level object {obj.Object.asset.objectName} ({obj.Object.asset.name}, {obj.Object.asset.GUID:N}): #{obj.Object.instanceID.ToString(Context.Culture)}");
            return UniTask.CompletedTask;
        }

        Context.ReplyString("You must be looking at a barricade, structure, vehicle, or object.");
        return UniTask.CompletedTask;
    }
}