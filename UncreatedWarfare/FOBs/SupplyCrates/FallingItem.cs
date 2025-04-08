using SDG.Framework.Utilities;
using System;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public abstract class FallingItem
{
    protected ItemData ItemData;
    protected Vector3 FinalRestPosition { get; }

    public WarfarePlayer Player { get; }
    public Team Team { get; }

    protected FallingItem(WarfarePlayer player, ItemData itemData, Vector3 landingPoint, Vector3 dropPoint)
    {
        Player = player;
        Team = player.Team;
        ItemData = itemData;
        FinalRestPosition = landingPoint;
        float distanceFallen = Math.Max(dropPoint.y - landingPoint.y, 0.5f);

        float secondsUntilConversion = Mathf.Sqrt(2 * 9.8f * distanceFallen) / 9.8f; // calculated using an equation of motion
        secondsUntilConversion += 0.1f; // add 0.1 second for good vibes

        TimeUtility.InvokeAfterDelay(OnHitGround, secondsUntilConversion);
    }

    protected abstract void OnHitGround();

    protected void DropItem()
    {

    }
}