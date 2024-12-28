using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Util.DamageTracking;
public class DamageTracker
{
    private readonly PlayerContributionTracker _damageContributors;
    public DateTime TimeLastDamaged { get; private set; }
    /// <summary>
    /// The very last player to damage the object, if the latest damaged was from a player. Will be <see langword="null"/> if the object's most recent damage was not from a player.
    /// </summary>
    public CSteamID? LatestDamageInstigator { get; private set; }
    /// <summary>
    /// The player who is the last known person to have damaged this object. Will not be <see langword="null"/> even if the object's most recent damage was not from a player.
    /// </summary>
    public CSteamID? LastKnownDamageInstigator { get; private set; }
    public EDamageOrigin? LatestDamageCause { get; private set; }
    public ItemAsset? LatestInstigatorWeapon { get; private set; }
    public DamageTracker()
    {
        TimeLastDamaged = DateTime.MinValue;
        LastKnownDamageInstigator = null;
        LatestDamageCause = null;
        LatestInstigatorWeapon = null;
        _damageContributors = new PlayerContributionTracker();
    }
    public virtual void RecordDamage(WarfarePlayer onlineInstigator, ushort damage, EDamageOrigin cause)
    {
        TimeLastDamaged = DateTime.Now;
        LatestDamageInstigator = onlineInstigator.Steam64;
        LastKnownDamageInstigator = onlineInstigator.Steam64;
        LatestDamageCause = cause;
        _damageContributors.RecordWork(onlineInstigator.Steam64, damage, TimeLastDamaged);
    }
    public virtual void RecordDamage(CSteamID playerId, ushort damage, EDamageOrigin cause)
    {
        TimeLastDamaged = DateTime.Now;
        LatestDamageInstigator = playerId;
        LastKnownDamageInstigator = playerId;
        LatestDamageCause = cause;
        _damageContributors.RecordWork(playerId, damage, TimeLastDamaged);
    }
    public virtual void RecordDamage(EDamageOrigin cause)
    {
        TimeLastDamaged = DateTime.Now;
        LatestDamageInstigator = null;
        LatestDamageCause = cause;
    }

    public void UpdateLatestInstigatorWeapon(ItemAsset asset) // todo: need to call this method places, otherwise advanced damage won't work properly
    {
        LatestInstigatorWeapon = asset;
    }
    public PlayerWork? GetDamageContribution(CSteamID playerId) => _damageContributors.GetContribution(playerId);
    public PlayerWork? GetDamageContribution(CSteamID playerId, DateTime after) => _damageContributors.GetContribution(playerId, after);
    public float GetDamageContributionPercentage(CSteamID playerId) => _damageContributors.GetContributionPercentage(playerId);
    public float GetDamageContributionPercentage(CSteamID playerId, DateTime after) => _damageContributors.GetContributionPercentage(playerId, after);
    public IEnumerable<CSteamID> Contributors => _damageContributors.Contributors;
}
