using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;

namespace Uncreated.Warfare.Components;

public class BarricadeComponent : MonoBehaviour, IManualOnDestroy
{
    public ulong Owner;
    /// <summary>For Signs</summary>
    public CSteamID LastEditor;
    public uint EditTick;
    public Player? Player;
    public Guid BarricadeGUID;

    public void Destroy()
    {
        Destroy(this);
    }

    void IManualOnDestroy.ManualOnDestroy() => Destroy(this);
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