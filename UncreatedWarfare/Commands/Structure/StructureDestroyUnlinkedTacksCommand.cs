using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.StrategyMaps;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("badtacks"), SubCommandOf(typeof(StructureDestroyCommand))]
internal sealed class StructureDestroyUnlinkedTacksCommand : IExecutableCommand
{
    private readonly StrategyMapManager _strategyMapManager;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly IFactionDataStore _factionDataStore;
    private readonly ZoneStore _globalZoneStore;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public StructureDestroyUnlinkedTacksCommand(StrategyMapManager strategyMapManager, ZoneStore globalZoneStore, AssetConfiguration assetConfiguration, IFactionDataStore factionDataStore)
    {
        _strategyMapManager = strategyMapManager;
        _globalZoneStore = globalZoneStore;
        _assetConfiguration = assetConfiguration;
        _factionDataStore = factionDataStore;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Zone? warRoom = _globalZoneStore.EnumerateInsideZones(Context.Player.Position, ZoneType.WarRoom).FirstOrDefault();
        if (warRoom == null)
        {
            throw Context.Reply(Context.CommonTranslations.NotInWarRoom);
        }

        Unsafe.SkipInit(out Bounds bounds);
        IProximity? proximity = null;
        foreach (ZoneProximity proximityZone in _globalZoneStore.ProximityZones)
        {
            if (!ReferenceEquals(proximityZone.Zone, warRoom))
                continue;

            proximity = proximityZone.Proximity;
            bounds = proximity.worldBounds;
            break;
        }

        if (proximity == null)
        {
            throw Context.Reply(Context.CommonTranslations.NotInWarRoom);
        }

        HashSet<Guid> mapTacks = new HashSet<Guid>();
        AddMapTacksRecursive(mapTacks, _assetConfiguration.GetSection("Buildables:MapTacks"));

        foreach (FactionInfo faction in _factionDataStore.Factions)
        {
            if (faction.MapTackFlag.TryGetGuid(out Guid guid))
                mapTacks.Add(guid);
        }

        Context.ReplyString($"Found {mapTacks.Count} registered map tacks.");

        float distance = MathF.Sqrt(MathF.Max((bounds.min - warRoom.Center).sqrMagnitude, (bounds.max - warRoom.Center).sqrMagnitude));
        int regionCount = (int)MathF.Ceiling(distance / Regions.REGION_SIZE);

        int count = 0;

        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades(warRoom.Center, (byte)regionCount))
        {
            if (barricade.IsOnVehicle)
                break;

            BarricadeDrop drop = barricade.Drop;
            if (!mapTacks.Contains(drop.asset.GUID) || !Regions.tryGetCoordinate(drop.GetServersideData().point, out byte x, out byte y))
                continue;

            if (!proximity.TestPoint(in drop.GetServersideData().point))
                continue;

            bool registered = false;

            foreach (StrategyMap map in _strategyMapManager.StrategyMaps)
            {
                if (!map.ActiveMapTacks.Exists(m => m.Tack.Marker.Equals(drop)))
                    continue;
                
                registered = true;
                break;
            }

            if (registered)
                continue;

            BuildableExtensions.SetSalvageInfo(drop.model, EDamageOrigin.Unknown, Context.CallerId, true, null);
            BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
            ++count;
        }

        throw Context.ReplyString($"Destroyed {count} barricade(s).");
    }

    private static void AddMapTacksRecursive(HashSet<Guid> mapTacks, IConfigurationSection tack)
    {
        if (Guid.TryParse(tack.Value, out Guid guid))
        {
            mapTacks.Add(guid);
        }

        foreach (IConfigurationSection section in tack.GetChildren())
        {
            AddMapTacksRecursive(mapTacks, section);
        }
    }
}