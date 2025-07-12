using Uncreated.Warfare.Util.DamageTracking;

namespace Uncreated.Warfare.FOBs;

public interface IDamageableFob : IBuildableFob
{
    bool CanRecordDamage { get; }
    DamageTracker DamageTracker { get; }
}
