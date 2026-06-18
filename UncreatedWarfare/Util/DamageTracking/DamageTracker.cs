#define DAMAGE_LOGGING
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Util.DamageTracking;
public class DamageTracker
{
    private readonly string _context;
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

    /// <summary>
    /// This can be an item or vehicle (ex. a vehicle explodes next to this vehicle).
    /// </summary>
    /// <remarks>It may not necessarily be a weapon if its an item. Could be melee, explosive consumable, landmine, C4 charge, etc.</remarks>
    public Asset? LatestInstigatorWeapon { get; private set; }
    public DamageTracker(string context)
    {
        _context = context;
        TimeLastDamaged = DateTime.MinValue;
        LastKnownDamageInstigator = null;
        LatestDamageCause = null;
        LatestInstigatorWeapon = null;
        _damageContributors = new PlayerContributionTracker();
    }

    /// <summary>
    /// Invoked when damage becomes irrelevant, like when the object is repaired.
    /// </summary>
    public virtual void ClearDamage()
    {
        TimeLastDamaged = default;
        LatestDamageCause = null;
        LatestDamageInstigator = null;
        LastKnownDamageInstigator = null;
        _damageContributors.Clear();
    }

    public virtual void RecordDamage(WarfarePlayer onlineInstigator, ushort damage, EDamageOrigin cause, bool isFriendly)
    {
        TimeLastDamaged = DateTime.Now;
        LatestDamageInstigator = onlineInstigator.Steam64;
        LastKnownDamageInstigator = onlineInstigator.Steam64;
        LatestDamageCause = cause;
        _damageContributors.RecordWork(onlineInstigator.Steam64, isFriendly, damage, TimeLastDamaged);
#if DAMAGE_LOGGING
        WarfareModule.Singleton.GlobalLogger.LogDebug($"RecordDamage called for {_context}: Inst: {onlineInstigator}, dmg: {damage}, cause: {cause}, friendly: {isFriendly}.");
#endif
    }
    public virtual void RecordDamage(CSteamID playerId, ushort damage, EDamageOrigin cause, bool isFriendly)
    {
        TimeLastDamaged = DateTime.Now;
        LatestDamageInstigator = playerId;
        LastKnownDamageInstigator = playerId;
        LatestDamageCause = cause;
        _damageContributors.RecordWork(playerId, isFriendly, damage, TimeLastDamaged);
#if DAMAGE_LOGGING
        WarfareModule.Singleton.GlobalLogger.LogDebug($"RecordDamage called for {_context}: Inst: {playerId}, dmg: {damage}, cause: {cause}, friendly: {isFriendly}.");
#endif
    }
    public virtual void RecordDamage(EDamageOrigin cause)
    {
        TimeLastDamaged = DateTime.Now;
        LatestDamageInstigator = null;
        LatestDamageCause = cause;
#if DAMAGE_LOGGING
        WarfareModule.Singleton.GlobalLogger.LogDebug($"RecordDamage called for {_context}: Inst: -none-, dmg: --irrelevant--, cause: {cause}.");
#endif
    }

    public float TotalWorkDone => _damageContributors.TotalWorkDone;
    public void UpdateLatestInstigatorWeapon(Asset? asset) // todo: need to call this method places, otherwise advanced damage won't work properly
    {
        LatestInstigatorWeapon = asset;
    }
    public float GetDamageContribution(CSteamID playerId, bool isFriendly, out float total) => _damageContributors.GetContribution(playerId, isFriendly, out total);
    public float GetDamageContribution(CSteamID playerId, bool isFriendly, DateTime after, out float total) => _damageContributors.GetContribution(playerId, isFriendly, after, out total);
    public float GetDamageContributionPercentage(CSteamID playerId, bool isFriendly) => _damageContributors.GetContributionPercentage(playerId, isFriendly);
    public float GetDamageContributionPercentage(CSteamID playerId, bool isFriendly, DateTime after) => _damageContributors.GetContributionPercentage(playerId, isFriendly, after);
    public PlayerContributionTracker.ContributorEnumerator Contributors => _damageContributors.Contributors;

    /// <inheritdoc />
    public override string ToString()
    {
        return "DamageTracker for \"" + _context + "\"";
    }
}
