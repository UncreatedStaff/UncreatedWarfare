#if false
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Traits.Buffs;
/// <summary>
/// Any players that enter a vehicle driven by a player under this effect will get a (by default 5 minute) xp boost.
/// </summary>
// rewrite: only crewmen get the buff and it stays until they leave the vehicle
public class AceArmor : Buff
{
    public static TraitData DEFAULT_DATA = new TraitData()
    {
        TypeName = nameof(AceArmor),
        NameTranslations = new TranslationList("Ace Armor"),
        DescriptionTranslations = new TranslationList("Give temporary XP boost to passengers."),
        CreditCost = 200,
        ClassList = new EClass[] { EClass.PILOT, EClass.CREWMAN },
        ClassListIsBlacklist = false,
        Icon = "§",
        TickSpeed = 1f,
        EffectDuration = 300,
        UnlockRequirements = new BaseUnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 1 } },
        EffectDistributedToSquad = false,
        Data = "0.45,0,300.1,¼" // xp boost multiplier, xp refund multiplier, xp boost length, xp boost icon
    };
    private float _multiplier = -1;
    private float _refundMultiplier = -1;
    private float _effectLength = -1;
    private string _icon = null!;
    private readonly List<AceBoost> _boosts = new List<AceBoost>(6);
    protected override void StartEffect(onStart)
    {
        if (onStart)
        {
            string[] datas = Data.Data is null ? Array.Empty<string>() : Data.Data.Split(dataSplitChars, StringSplitOptions.RemoveEmptyEntries);
            if (datas.Length > 0)
            {
                float.TryParse(datas[0], NumberStyles.Number, Warfare.Data.Locale, out _multiplier);
                if (datas.Length > 1)
                {
                    float.TryParse(datas[1], NumberStyles.Number, Warfare.Data.Locale, out _refundMultiplier);
                    if (datas.Length > 2)
                    {
                        float.TryParse(datas[2], NumberStyles.Number, Warfare.Data.Locale, out _effectLength);
                        if (datas.Length > 3)
                            _icon = datas[3];
                    }
                }
            }
            if (_multiplier == -1f)
                _multiplier = 0.45f;
            if (_refundMultiplier == -1f)
                _refundMultiplier = 0.1f;
            if (string.IsNullOrEmpty(_icon))
                _icon = Data.Icon;
            if (_effectLength == -1f)
                _effectLength = Data.EffectDuration;
            EventDispatcher.OnEnterVehicle += OnEnterVehicle;
        }
        base.StartEffect(onStart);
    }

    private void OnEnterVehicle(EnterVehicle e)
    {
        if (e.Player.Steam64 != TargetPlayer.Steam64 
            && e.PassengerIndex != 0 
            && e.Vehicle.passengers[0].player != null 
            && e.Vehicle.passengers[0].player.playerID.steamID.m_SteamID == TargetPlayer.Steam64 
            && TargetPlayer.GetTeam() == e.Player.GetTeam()
            && e.Player.ActiveBuffs[e.Player.ActiveBuffs.Length - 1] == null)
        {
            AceBoost boost = new AceBoost(_icon, _multiplier, _refundMultiplier, e.Player, this);
            _boosts.Add(boost);
            TraitManager.BuffUI.AddBuff(e.Player, boost);
        }
    }
    protected override void Tick()
    {
        InteractableVehicle? vehicle = TargetPlayer.Player.movement.getVehicle();
        if (vehicle == null
            || TargetPlayer.Player.movement.getSeat() != 0
            || !VehicleBay.Loaded
            || !VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data)
            || data.RequiredClass is not EClass.CREWMAN and not EClass.PILOT
            ) return;
        float time = Time.realtimeSinceStartup;
        for (int i = 0; i < vehicle.passengers.Length; ++i)
        {
            if (vehicle.passengers[i].player != null)
            {
                UCPlayer? pl = UCPlayer.FromSteamPlayer(vehicle.passengers[i].player);
                if (pl is null) continue;
                for (int k = 0; k < pl.ActiveBuffs.Length; ++k)
                {
                    if (pl.ActiveBuffs[k] is AceBoost boost && boost.Trait == this)
                    {
                        boost.StartTime = time;
                    }
                }
            }
        }
        for (int i = 0; i < _boosts.Count; ++i)
        {
            AceBoost boost = _boosts[i];
            if ((boost.Player is null || !boost.Player.IsOnline))
            {
                if (boost.StartTime + _effectLength < time || boost.Trait == null)
                    TraitManager.BuffUI.RemoveBuff(boost.Player!, boost);
                else if (boost.StartTime + _effectLength < time - BLINK_LEAD_TIME)
                {
                    boost.IsBlinking = true;
                    TraitManager.BuffUI.UpdateBuffTimeState(boost);
                }
            }
        }

        base.Tick();
    }
    protected override void ClearEffect(bool onDestroy)
    {
        if (onDestroy)
            EventDispatcher.OnEnterVehicle -= OnEnterVehicle;
        for (int i = _boosts.Count - 1; i >= 0; --i)
        {
            AceBoost boost = _boosts[i];
            TraitManager.BuffUI.RemoveBuff(_boosts[i].Player, boost);
            boost.Trait = null!;
        }

        _boosts.Clear();
        base.ClearEffect();
    }
    public sealed class AceBoost : NonTraitBuff, IXPBoostBuff
    {
        private readonly float multiplier;
        private readonly float refundMultiplier;
        public AceArmor Trait;
        public AceBoost(string icon, float multiplier, float refundMultiplier, UCPlayer player, AceArmor owner) : base(icon, player)
        {
            this.multiplier = multiplier;
            this.refundMultiplier = refundMultiplier;
            Trait = owner;
        }
        float IXPBoostBuff.Multiplier => multiplier;
        void IXPBoostBuff.OnXPBoostUsed(float amount, bool awardCredits)
        {
            if (Trait is null) return;
            UCPlayer? pl = Trait.TargetPlayer;
            if (pl is null)
                return;
            if (!pl.IsOnline)
                pl = UCPlayer.FromID(pl.Steam64);
            if (pl is not null)
                Point.Points.AwardXP(pl, Mathf.CeilToInt(amount * refundMultiplier), T.XPToastAceArmorRefund, true);
        }
    }
}
#endif