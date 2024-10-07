using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs.Construction;
internal abstract class PointsShoveable : IShovelable
{
    public int HitsRemaining { get; private set; }
    public bool IsCompleted => HitsRemaining <= 0;
    public TickResponsibilityCollection Builders { get; }
    private readonly Guid _sessionId;

    public PointsShoveable(int hitsRemaining)
    {
        HitsRemaining = hitsRemaining;
        Builders = new TickResponsibilityCollection();
        _sessionId = Guid.NewGuid();
    }

    public abstract void Complete(WarfarePlayer shoveler);

    public bool Shovel(WarfarePlayer shoveler, Vector3 point)
    {
        if (IsCompleted)
            return false;

        HitsRemaining--;
        Builders.AddItem(new TickResponsibility(shoveler.Steam64.m_SteamID, shoveler.CurrentSession.SessionId, 1));

        if (IsCompleted)
        {
            Complete(shoveler);
        }
        return true;
    }
}
