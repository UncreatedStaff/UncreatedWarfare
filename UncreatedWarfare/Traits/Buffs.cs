using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Traits;
public abstract class Buff : Trait, IBuff
{
    public const float BLINK_LEAD_TIME = 10f;
    internal bool _shouldBlink = false;
    private bool _isActivated = true;
    public virtual bool IsBlinking => _shouldBlink;
    bool IBuff.Reserved => false;
    UCPlayer IBuff.Player => TargetPlayer;
    string IBuff.Icon => Data.Icon.HasValue ? Data.Icon.Value : BuffUI.DefaultBuffIcon;
    public bool IsActivated
    {
        get => _isActivated && !IsAwaitingStagingPhase;
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
    public virtual bool CanEnable => true;
    protected virtual void Reactivate()
    {
        StartEffect(false);
        L.LogDebug("Buff reactivated: " + Data.TypeName);
    }
    protected virtual void Deactivate()
    {
        ClearEffect(false);
        L.LogDebug("Buff deactivated: " + Data.TypeName);
    }
    protected virtual void StartEffect(bool onStart)
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
    internal virtual void OnBlinkingUpdated()
    {
        TraitManager.BuffUI.UpdateBuffTimeState(this);
    }
    protected virtual void ClearEffect(bool onDestroy)
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
        StartEffect(true);
    }
    protected override void OnDeactivate()
    {
        base.OnDeactivate();
        ClearEffect(true);
    }
}
public interface IBuff
{
    bool IsBlinking { get; }
    bool Reserved { get; }
    string Icon { get; }
    UCPlayer Player { get; }
}
public interface IXPBoostBuff
{
    float Multiplier { get; }
    void OnXPBoostUsed(float amount, bool awardCredits = true);
}