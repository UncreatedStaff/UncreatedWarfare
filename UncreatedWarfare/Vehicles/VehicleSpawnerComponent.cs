using System;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Vehicles;

#pragma warning disable IDE0051 // unity messages

/// <summary>
/// Handles logic for spawning vehicles.
/// </summary>
public class VehicleSpawnerComponent : MonoBehaviour, IManualOnDestroy
{
    public VehicleSpawnInfo SpawnInfo { get; private set; }

    public VehicleSpawnerState State { get; private set; }


    public void Init(VehicleSpawnInfo spawnInfo)
    {
        SpawnInfo = spawnInfo;
    }

    [UsedImplicitly]
    private void Update()
    {

    }

    void IManualOnDestroy.ManualOnDestroy()
    {
        Destroy(this);
    }

    public TimeSpan GetRespawnDueTime()
    {
        return default;
    }

    public string GetLocation()
    {
        return new GridLocation(transform.position).ToString();
    }

    public static void StartLinkingSign(VehicleSpawnerComponent spawner, WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        VehicleSpawnerLinkComponent comp = player.UnturnedPlayer.gameObject.GetOrAddComponent<VehicleSpawnerLinkComponent>();
        comp.Spawner = spawner;
    }

    public static VehicleSpawnInfo? EndLinkingSign(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        VehicleSpawnerLinkComponent? comp = player.UnturnedPlayer.GetComponent<VehicleSpawnerLinkComponent>();
        if (comp == null || comp.Spawner == null)
            return null;

        VehicleSpawnInfo info = comp.Spawner.SpawnInfo;
        Destroy(comp.Spawner);
        return info;
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class VehicleSpawnerLinkComponent : MonoBehaviour
    {
        public VehicleSpawnerComponent? Spawner;

        [UsedImplicitly]
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(30f);
            Destroy(this);
            Spawner = null;
        }
    }
}

public enum VehicleSpawnerState
{
    Destroyed,
    Deployed,
    Idle,
    Ready
}
#pragma warning restore IDE0051