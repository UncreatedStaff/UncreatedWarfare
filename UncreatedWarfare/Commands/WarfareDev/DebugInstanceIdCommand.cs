using Uncreated.Warfare.Interaction.Commands;

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

        if (ObjectManager.tryGetRegion(raycast.transform, out byte x, out byte y, out ushort index))
        {
            LevelObject obj = LevelObjects.objects[x, y][index];
            Context.ReplyString($"Level object {obj.asset.objectName} ({obj.asset.name}, {obj.asset.GUID:N}): #{obj.instanceID.ToString(Context.Culture)}");
        }

        Context.ReplyString("You must be looking at a barricade, structure, vehicle, or object.");
        return UniTask.CompletedTask;
    }
}