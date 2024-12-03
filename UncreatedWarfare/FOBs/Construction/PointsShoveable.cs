using HarmonyLib;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs.Construction;

public abstract class PointsShoveable : IShovelable
{
    public ShovelableInfo Info { get; }
    public int HitsRemaining { get; private set; }
    public bool IsCompleted => HitsRemaining <= 0;
    public TickResponsibilityCollection Builders { get; }
    public IBuildable Buildable { get; }

    private readonly EffectAsset? _shovelEffect;
    private readonly Guid _sessionId;

    public PointsShoveable(ShovelableInfo info, IBuildable buildable, IAssetLink<EffectAsset>? shovelEffect = null)
    {
        Info = info;
        Buildable = buildable;
        _shovelEffect = shovelEffect?.GetAssetOrFail();
        HitsRemaining = info.SupplyCost;
        Builders = new TickResponsibilityCollection();
        _sessionId = Guid.NewGuid();
    }

    public abstract void Complete(WarfarePlayer shoveler);

    public bool Shovel(WarfarePlayer shoveler, Vector3 point)
    {
        if (IsCompleted)
            return false;

        HitsRemaining--;
        if (shoveler.CurrentSession != null)
            Builders.AddItem(new TickResponsibility(shoveler.Steam64.m_SteamID, shoveler.CurrentSession.SessionId, 1));

        if (IsCompleted)
        {
            Complete(shoveler);
        }
        EffectManager.triggerEffect(new TriggerEffectParameters(_shovelEffect)
        {
            position = point,
            relevantDistance = 70,
            reliable = true
        });
        return true;
    }

    public void Dispose()
    {
        
    }
}
