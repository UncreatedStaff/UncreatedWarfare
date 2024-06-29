using SDG.Unturned;
using System;
using JetBrains.Annotations;
using Steamworks;
using Uncreated.Warfare.Events;
using UnityEngine;

namespace Uncreated.Warfare.Components;

public class BarricadeComponent : MonoBehaviour, IManualOnDestroy
{
    public ulong Owner;
    /// <summary>For Signs</summary>
    public CSteamID LastEditor;
    public uint EditTick;
    public Player? Player;
    public Guid BarricadeGUID;
    public float CreateTime;

    [UsedImplicitly]
    private void Awake()
    {
        CreateTime = Time.realtimeSinceStartup;
    }
    public void Destroy()
    {
        Destroy(this);
    }

    public void ManualOnDestroy() => Destroy(this);
}

public interface ISalvageInfo
{
    bool IsSalvaged { set; get; }
    CSteamID Salvager { set; get; }
}
public interface ISalvageListener : ISalvageInfo
{
    void OnSalvageRequested(SalvageRequested e);
}
public interface IDestroyInfo
{
    IBuildableDestroyedEvent? DestroyInfo { get; set; }
}