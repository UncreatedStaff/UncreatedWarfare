using SDG.Unturned;
using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Uncreated.Warfare.Components;

public class BarricadeComponent : MonoBehaviour
{
    public ulong Owner;
    /// <summary>For Signs</summary>
    public ulong LastEditor;
    public uint EditTick;
    public Player? Player;
    public Guid BarricadeGUID;
    public ulong LastDamager;
    public float LastDamagerTime;
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

}