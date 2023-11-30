using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Traits.Buffs;
/// <summary>
/// Any crewmen that enter a vehicle driven by a player under this effect will get a xp boost until they leave.
/// </summary>
public class AceArmor : Buff
{
    public static TraitData DefaultData = new TraitData()
    {
        TypeName = nameof(AceArmor),
        NameTranslations = new TranslationList("Ace Armor"),
        DescriptionTranslations = new TranslationList("Give an XP boost to crewmen."),
        CreditCost = 200,
        ClassList = new Class[] { Class.Crewman },
        ClassListIsBlacklist = false,
        Icon = "§",
        Cooldown = 330,
        EffectDuration = 300,
        UnlockRequirements = new UnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 1 } },
        Delays = new Delay[] { new Delay(DelayType.Time, 540f) }, // same delay as lowest armor vehicle
        EffectDistributedToSquad = false,
        Data = "0.45,0,¼" // xp boost multiplier, xp refund multiplier, xp boost icon
    };
    private float _multiplier = -1;
    private float _refundMultiplier = -1;
    private string _icon = null!;
    private readonly List<AceBoost> _boosts = new List<AceBoost>(6);
    protected override void StartEffect(bool onStart)
    {
        if (onStart)
        {
            string[] datas = Data.Data is null ? Array.Empty<string>() : Data.Data.Split(DataSplitChars, StringSplitOptions.RemoveEmptyEntries);
            if (datas.Length > 0)
            {
                float.TryParse(datas[0], NumberStyles.Number, Warfare.Data.AdminLocale, out _multiplier);
                if (datas.Length > 1)
                {
                    float.TryParse(datas[1], NumberStyles.Number, Warfare.Data.AdminLocale, out _refundMultiplier);
                    if (datas.Length > 2)
                        _icon = datas[2];
                }
            }
            if (_multiplier < 0f)
                _multiplier = 0.45f;
            if (_refundMultiplier < 0f)
                _refundMultiplier = 0.1f;
            if (string.IsNullOrEmpty(_icon))
                _icon = Data.Icon;
            EventDispatcher.EnterVehicle += OnEnterVehicle;
            EventDispatcher.ExitVehicle += OnExitVehicle;
            EventDispatcher.VehicleSwapSeat += OnSwapSeats;
        }

        base.StartEffect(onStart);
        InteractableVehicle vehicle = TargetPlayer.Player.movement.getVehicle();
        if (vehicle != null && TargetPlayer.Player.movement.getSeat() == 0)
        {
            for (int i = 1; i < vehicle.passengers.Length; ++i)
            {
                Passenger p = vehicle.passengers[i];
                if (p.player != null && p.player.playerID.steamID.m_SteamID != TargetPlayer.Steam64)
                {
                    UCPlayer? pl = UCPlayer.FromSteamPlayer(p.player);
                    if (pl is not null)
                        BoostPlayer(pl);
                }
            }
        }
    }
    private void OnEnterVehicle(EnterVehicle e)
    {
        if (IsActivated)
        {
            if (
                   e.Player.Steam64 != TargetPlayer.Steam64
                && e.PassengerIndex != 0
                && e.Vehicle.passengers[0].player != null
                && e.Vehicle.passengers[0].player.playerID.steamID.m_SteamID == TargetPlayer.Steam64
                && TargetPlayer.GetTeam() == e.Player.GetTeam()
                && e.Player.ActiveBuffs[e.Player.ActiveBuffs.Length - 1] == null
                && Data.CanClassUse(e.Player.KitClass))
            {
                BoostPlayer(e.Player);
            }
            else if (e.PassengerIndex == 0 && e.Player.Steam64 == TargetPlayer.Steam64)
            {
                for (int i = 1; i < e.Vehicle.passengers.Length; ++i)
                {
                    Passenger p = e.Vehicle.passengers[i];
                    if (p.player != null)
                    {
                        UCPlayer? pl = UCPlayer.FromSteamPlayer(p.player);
                        if (pl is not null)
                            BoostPlayer(pl);
                    }
                }
            }
        }
    }
    private void BoostPlayer(UCPlayer player)
    {
        for (int i = 0; i < player.ActiveBuffs.Length; ++i)
            if (player.ActiveBuffs[i] is AceBoost) return;

        AceBoost boost = new AceBoost(_icon, _multiplier, _refundMultiplier, player, this);
        _boosts.Add(boost);
        TraitManager.BuffUI.AddBuff(player, boost);
    }
    private void RemoveBoost(AceBoost boost)
    {
        TraitManager.BuffUI.RemoveBuff(boost.Player, boost);
        _boosts.Remove(boost);
        boost.Trait = null!;
    }
    private void OnExitVehicle(ExitVehicle e)
    {
        for (int i = e.Player.ActiveBuffs.Length - 1; i >= 0; --i)
        {
            if (e.Player.ActiveBuffs[i] is AceBoost ab)
                RemoveBoost(ab);
        }
        if (IsActivated && e.Player.Steam64 == TargetPlayer.Steam64)
        {
            for (int i = 0; i < e.Vehicle.passengers.Length; ++i)
            {
                Passenger p = e.Vehicle.passengers[i];
                if (p.player != null)
                {
                    UCPlayer? pl = UCPlayer.FromSteamPlayer(p.player);
                    if (pl is not null)
                    {
                        for (int b = pl.ActiveBuffs.Length - 1; b >= 0; --b)
                        {
                            if (pl.ActiveBuffs[b] is AceBoost ab && ab.Trait == this)
                                RemoveBoost(ab);
                        }
                    }
                }
            }
        }
    }
    private void OnSwapSeats(VehicleSwapSeat e)
    {
        if (IsActivated && e.Player.Steam64 == TargetPlayer.Steam64)
        {
            if (e.NewSeat == 0)
            {
                for (int i = 1; i < e.Vehicle.passengers.Length; ++i)
                {
                    Passenger p = e.Vehicle.passengers[i];
                    if (p.player != null)
                    {
                        UCPlayer? pl = UCPlayer.FromSteamPlayer(p.player);
                        if (pl is not null)
                            BoostPlayer(pl);
                    }
                }
            }
            else
            {
                for (int i = 0; i < e.Vehicle.passengers.Length; ++i)
                {
                    Passenger p = e.Vehicle.passengers[i];
                    if (p.player != null && p.player.playerID.steamID.m_SteamID != TargetPlayer.Steam64)
                    {
                        UCPlayer? pl = UCPlayer.FromSteamPlayer(p.player);
                        if (pl is not null)
                        {
                            for (int b = pl.ActiveBuffs.Length - 1; b >= 0; --b)
                            {
                                if (pl.ActiveBuffs[b] is AceBoost ab && ab.Trait == this)
                                    RemoveBoost(ab);
                            }
                        }
                    }
                }
            }
        }
    }
    internal override void OnBlinkingUpdated()
    {
        base.OnBlinkingUpdated();
        for (int i = 0; i < _boosts.Count; ++i)
        {
            _boosts[i].IsBlinking = true;
            TraitManager.BuffUI.UpdateBuffTimeState(_boosts[i]);
        }
    }
    protected override void ClearEffect(bool onDestroy)
    {
        if (onDestroy)
        {
            EventDispatcher.VehicleSwapSeat -= OnSwapSeats;
            EventDispatcher.ExitVehicle -= OnExitVehicle;
            EventDispatcher.EnterVehicle -= OnEnterVehicle;
        }
        for (int i = _boosts.Count - 1; i >= 0; --i)
        {
            AceBoost boost = _boosts[i];
            TraitManager.BuffUI.RemoveBuff(_boosts[i].Player, boost);
            boost.Trait = null!;
        }

        _boosts.Clear();
        base.ClearEffect(_shouldBlink);
    }
    private sealed class AceBoost : NonTraitBuff, IXPBoostBuff
    {
        private readonly float _multiplier;
        private readonly float _refundMultiplier;
        public AceArmor Trait;
        public AceBoost(string icon, float multiplier, float refundMultiplier, UCPlayer player, AceArmor owner) : base(icon, player)
        {
            _multiplier = multiplier;
            _refundMultiplier = refundMultiplier;
            Trait = owner;
        }
        float IXPBoostBuff.Multiplier => Trait != null && Trait.IsActivated ? _multiplier : 1f;
        void IXPBoostBuff.OnXPBoostUsed(float amount, bool awardCredits) { }
    }
}