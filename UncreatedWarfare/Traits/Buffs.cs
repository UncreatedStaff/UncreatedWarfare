using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Traits;
public abstract class Buff : Trait, IBuff
{
    public const float BLINK_LEAD_TIME = 10f;
    internal bool _shouldBlink = false;
    private bool _isActivated = true;
    public virtual bool IsBlinking => _shouldBlink;
    UCPlayer IBuff.Player => TargetPlayer;
    string IBuff.Icon => Data.Icon.HasValue ? Data.Icon.Value : BuffUI.DEFAULT_BUFF_ICON;
    public bool IsActivated
    {
        get => _isActivated;
        set
        {
            if (_isActivated == value)
                return;
            _isActivated = value;
            if (value)
                Reactivate();
            else
                Deactivate();
        }
    }
    protected virtual void Reactivate()
    {
        StartEffect();
        L.LogDebug("Buff reactivated: " + this.Data.TypeName);
    }
    protected virtual void Deactivate()
    {
        ClearEffect();
        L.LogDebug("Buff deactivated: " + this.Data.TypeName);
    }
    protected virtual void StartEffect()
    {
        Squad? sq;
        if (Data.EffectDistributedToSquad && (sq = TargetPlayer.Squad) is not null)
        {
            for (int i = 0; i < sq.Members.Count; ++i)
                AddPlayer(sq.Members[i]);
        }
        else
            AddPlayer(TargetPlayer);
        if (!IsActivated)
            _isActivated = true;
    }

    protected virtual void ClearEffect()
    {
        if (IsActivated)
            _isActivated = false;
        Squad? sq;
        if (Data.EffectDistributedToSquad && (sq = TargetPlayer.Squad) is not null)
        {
            for (int i = 0; i < sq.Members.Count; ++i)
                RemovePlayer(sq.Members[i]);
        }
        else
        {
            RemovePlayer(TargetPlayer);
        }
    }

    internal virtual void AddPlayer(UCPlayer player)
    {
        TraitManager.BuffUI.AddBuff(player, this);
    }
    internal virtual void RemovePlayer(UCPlayer player)
    {
        TraitManager.BuffUI.RemoveBuff(player, this);
    }

    internal virtual void SquadLeaderPromoted()
    {
        if (!IsActivated && (Data.RequireSquadLeader || Data.RequireSquad))
        {
            IsActivated = true;
            TargetPlayer.SendChat(T.TraitReactivated, this);
        }
    }
    internal virtual void SquadLeaderDemoted()
    {
        if (Data.RequireSquadLeader)
        {
            TargetPlayer.SendChat(T.TraitDisabledSquadLeaderDemoted, this);
            IsActivated = false;
        }
    }
    protected override void OnActivate()
    {
        base.OnActivate();
        StartEffect();
    }
    protected override void OnDeactivate()
    {
        base.OnDeactivate();
        ClearEffect();
    }
}
public interface IBuff
{
    bool IsBlinking { get; }
    string Icon { get; }
    UCPlayer Player { get; }
}
public interface IXPBoostBuff
{
    float Multiplier { get; }
    void OnXPBoostUsed(float amount, bool awardCredits = true);
}