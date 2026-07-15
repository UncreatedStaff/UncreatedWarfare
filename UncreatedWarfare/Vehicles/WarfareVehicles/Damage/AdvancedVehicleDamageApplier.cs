using System;
using System.Collections.Generic;
using System.Globalization;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles.Damage;

public class AdvancedVehicleDamageApplier
{
    private readonly Queue<AdvancedDamagePending> _damageQueue;
    private readonly ILogger _logger;

    public AdvancedVehicleDamageApplier(ILogger<AdvancedVehicleDamageApplier> logger)
    {
        _logger = logger;
        _damageQueue = new Queue<AdvancedDamagePending>();
    }

    public void RegisterDirectHitDamageMultiplier(float damageMultiplier, float fundamentalBaseDamage)
    {
        _damageQueue.Enqueue(new AdvancedDamagePending
        {
            FundamentalBaseDamage = fundamentalBaseDamage,
            Multiplier = damageMultiplier,
            Timestamp = DateTime.Now
        });
        //_logger.LogDebug($"Registered direct hit damage multiplier of {damageMultiplier} for vehicle. Multipliers queued for this vehicle: {_damageQueue.Count}");
    }

    public AdvancedDamagePending? ApplyLatestPendingDirectHit()
    {
        while (_damageQueue.Count > 0)
        {
            AdvancedDamagePending pendingDamage = _damageQueue.Dequeue();
            TimeSpan timeElapsedSinceDamageRegistered = DateTime.Now - pendingDamage.Timestamp;
            if (timeElapsedSinceDamageRegistered.TotalSeconds > 0.1f) // do not apply pending that's too old (older than a fraction of a second)
                continue;
            
            //_logger.LogDebug($"Applying advanced vehicle damage multiplier of {pendingDamage.Multiplier}.");
            return pendingDamage;
        }
        
        return null;
    }
    public static float GetComponentDamageMultiplier(InputInfo hitInfo)
    {
        if (hitInfo.colliderTransform == null)
            return 1;
        
        return GetComponentDamageMultiplier(hitInfo.colliderTransform);
    }
    
    public static float GetComponentDamageMultiplier(Transform colliderTransform)
    {
        if (!colliderTransform.name.StartsWith("damage_"))
            return 1;

        if (!float.TryParse(colliderTransform.name.AsSpan(7), NumberStyles.Any,
                CultureInfo.InvariantCulture, out float multiplier))
            return 1;

        return multiplier;
    }

    public struct AdvancedDamagePending
    {
        /// <summary>
        /// The true base damage before the bullet/projectile/thing hits or explodes, specifically
        /// before any distance-based damage falloff and/or plugin damage modifiers are applied.
        /// </summary>
        public required float FundamentalBaseDamage { get; init; }
        public required float Multiplier { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}