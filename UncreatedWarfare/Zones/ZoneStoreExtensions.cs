using Uncreated.Warfare.Players;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Zones;
public static class ZoneStoreExtensions
{
    public static bool IsInMainBase(this ZoneStore zoneStore, Vector3 point)
    {
        return zoneStore.IsInsideZone(point, ZoneType.MainBase, null);
    }

    public static bool IsInMainBase(this ZoneStore zoneStore, Vector2 point)
    {
        return zoneStore.IsInsideZone(point, ZoneType.MainBase, null);
    }

    public static bool IsInMainBase(this ZoneStore zoneStore, WarfarePlayer player)
    {
        return zoneStore.IsInsideZone(player.Position, ZoneType.MainBase, null);
    }

    public static bool IsInMainBase(this ZoneStore zoneStore, Vector3 point, FactionInfo faction)
    {
        return zoneStore.IsInsideZone(point, ZoneType.MainBase, faction);
    }

    public static bool IsInMainBase(this ZoneStore zoneStore, Vector2 point, FactionInfo faction)
    {
        return zoneStore.IsInsideZone(point, ZoneType.MainBase, faction);
    }

    public static bool IsInMainBase(this ZoneStore zoneStore, WarfarePlayer player, FactionInfo faction)
    {
        return zoneStore.IsInsideZone(player.Position, ZoneType.MainBase, faction);
    }

    public static bool IsInAntiMainCamp(this ZoneStore zoneStore, Vector3 point)
    {
        return zoneStore.IsInsideZone(point, ZoneType.AntiMainCampArea, null);
    }

    public static bool IsInAntiMainCamp(this ZoneStore zoneStore, Vector2 point)
    {
        return zoneStore.IsInsideZone(point, ZoneType.AntiMainCampArea, null);
    }

    public static bool IsInAntiMainCamp(this ZoneStore zoneStore, WarfarePlayer player)
    {
        return zoneStore.IsInsideZone(player.Position, ZoneType.AntiMainCampArea, null);
    }

    public static bool IsInAntiMainCamp(this ZoneStore zoneStore, Vector3 point, FactionInfo faction)
    {
        return zoneStore.IsInsideZone(point, ZoneType.AntiMainCampArea, faction);
    }

    public static bool IsInAntiMainCamp(this ZoneStore zoneStore, Vector2 point, FactionInfo faction)
    {
        return zoneStore.IsInsideZone(point, ZoneType.AntiMainCampArea, faction);
    }

    public static bool IsInAntiMainCamp(this ZoneStore zoneStore, WarfarePlayer player, FactionInfo faction)
    {
        return zoneStore.IsInsideZone(player.Position, ZoneType.AntiMainCampArea, faction);
    }

    public static bool IsInLobby(this ZoneStore zoneStore, Vector3 point)
    {
        return zoneStore.IsInsideZone(point, ZoneType.Lobby, null);
    }

    public static bool IsInLobby(this ZoneStore zoneStore, Vector2 point)
    {
        return zoneStore.IsInsideZone(point, ZoneType.Lobby, null);
    }

    public static bool IsInLobby(this ZoneStore zoneStore, WarfarePlayer player)
    {
        return zoneStore.IsInsideZone(player.Position, ZoneType.Lobby, null);
    }
}