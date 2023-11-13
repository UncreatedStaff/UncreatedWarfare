using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Framework;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Traits;

internal static class TraitSigns
{
    internal static string TranslateTraitSign(TraitData trait, UCPlayer player)
    {
        ulong team = trait.Team is 1 or 2 ? trait.Team : player.GetTeam();
        string str = TranslateTraitSign(player.Locale.LanguageInfo, player.Locale.CultureInfo, trait, team, out bool fmt);
        return fmt ? FormatTraitSign(player, trait, str) : str;
    }
    public static string FormatTraitSign(UCPlayer player, TraitData trait, string translated)
    {
        UCPlayer.TryApplyViewLens(ref player);
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i].Data.Type == trait.Type)
            {
                if (trait.LastsUntilDeath)
                    return Util.QuickFormat(translated, T.TraitSignAlreadyActiveDeath.Translate(player));
                else
                {
                    int secs = Mathf.CeilToInt(trait.EffectDuration - (Time.realtimeSinceStartup - player.ActiveTraits[i].StartTime));
                    return Util.QuickFormat(translated, T.TraitSignAlreadyActiveTime.Translate(player, false, secs / 60, secs % 60));
                }
            }
        }
        if (!player.OnDuty())
        {
            if (CooldownManager.HasCooldown(player, CooldownType.IndividualRequestTrait, out Cooldown cooldown, trait.TypeName))
            {
                if (CooldownManager.HasCooldownNoStateCheck(player, CooldownType.GlobalRequestTrait, out Cooldown gcooldown) && gcooldown.SecondsLeft > cooldown.SecondsLeft)
                    cooldown = gcooldown;
            }
            else if (!CooldownManager.HasCooldownNoStateCheck(player, CooldownType.GlobalRequestTrait, out cooldown))
                goto next;

            int secs = Mathf.CeilToInt(cooldown.SecondsLeft);
            return Util.QuickFormat(translated, T.TraitSignCooldown.Translate(player, false, secs / 60, secs % 60));
        }
        next:
        if (trait.UnlockRequirements is not null)
        {
            for (int i = 0; i < trait.UnlockRequirements.Length; ++i)
            {
                UnlockRequirement req = trait.UnlockRequirements[i];
                if (!req.CanAccess(player))
                    return Util.QuickFormat(translated, req.GetSignText(player));
            }
        }

        if (player.KitClass > Class.Unarmed)
        {
            if (!trait.CanClassUse(player.KitClass))
            {
                if (trait.ClassListIsBlacklist || trait.ClassList.Length > 2 || trait.ClassList.Length == 0)
                    return Util.QuickFormat(translated, T.TraitSignClassBlacklisted.Translate(player, false, player.KitClass));
                if (trait.ClassList.Length == 2)
                    return Util.QuickFormat(translated, T.TraitSignClassWhitelisted2.Translate(player, false, trait.ClassList[0], trait.ClassList[1]));
                return Util.QuickFormat(translated, T.TraitSignClassWhitelisted1.Translate(player, false, trait.ClassList[0]));
            }
        }
        else
            return Util.QuickFormat(translated, T.TraitSignNoKit.Translate(player));
        if (player.Squad is null)
        {
            if (trait.RequireSquadLeader)
                return Util.QuickFormat(translated, T.TraitSignRequiresSquadLeader.Translate(player));
            else if (trait.RequireSquad)
                return Util.QuickFormat(translated, T.TraitSignRequiresSquad.Translate(player));
        }
        else if (trait.RequireSquadLeader && player.Squad.Leader.Steam64 != player.Steam64)
            return Util.QuickFormat(translated, T.TraitSignRequiresSquadLeader.Translate(player));

        return Util.QuickFormat(translated, string.Empty /* T.TraitSignUnlocked.Translate(player) */);
    }
    internal static string TranslateTraitSign(LanguageInfo language, CultureInfo culture, TraitData trait, ulong team, out bool fmt)
    {
        string name = "<b>" + trait.NameTranslations.Translate(language, trait.TypeName).ToUpper().ColorizeTMPro(UCWarfare.GetColorHex("kit_public_header"), true) + "</b>";

        string cost = trait.CreditCost > 0 ? T.KitCreditCost.Translate(language, culture, trait.CreditCost) : T.TraitSignFree.Translate(language, culture);

        if (name.IndexOf('\n') == -1) cost = "\n" + cost;

        string? req = null;

        if (!trait.CanGamemodeUse())
            req = T.TraitGamemodeBlacklisted.Translate(language, culture);
        else if (trait.Delays != null && trait.Delays.Length > 0 && Delay.IsDelayed(trait.Delays, out Delay delay, team))
            req = Localization.GetDelaySignText(in delay, language, culture, team);

        fmt = req is null;
        return
            name + "\n" +
            cost + "\n" +
            (fmt ? "{0}\n" : (req + "\n")) +
            trait.DescriptionTranslations.Translate(language, string.Empty).ColorizeTMPro(UCWarfare.GetColorHex("trait_desc"));
    }
    public static void InitTraitSign(TraitData d, BarricadeDrop drop)
    {
        if (!drop.model.gameObject.TryGetComponent(out TraitSignComponent c))
        {
            c = drop.model.gameObject.AddComponent<TraitSignComponent>();
        }
        c.Init(d, drop);
    }
    public static void TryRemoveComponent(BarricadeDrop drop)
    {
        if (drop.model.gameObject.TryGetComponent(out TraitSignComponent c))
            UnityEngine.Object.Destroy(c);
    }
    internal static void OnBarricadeMoved(BarricadeDrop drop)
    {
        if (drop.model.gameObject.TryGetComponent(out TraitSignComponent c))
            c.Init(c.Data, drop);
    }
    internal static void TimeSync()
    {
        for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                BarricadeRegion reg = BarricadeManager.regions[x, y];
                for (int i = 0; i < reg.drops.Count; ++i)
                {
                    if (reg.drops[i].interactable is InteractableSign sign && sign.text.StartsWith(Signs.Prefix + Signs.TraitPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        if (reg.drops[i].model.TryGetComponent(out TraitSignComponent c))
                            c.UpdateTimeDelay();
                    }
                }
            }
        }
    }
    private class TraitSignComponent : MonoBehaviour
    {
        private TraitSignState _state;
        private float _lastDelayCheck;
        private bool _checkTime;
        public TraitData Data;
        private BarricadeDrop _drop;
        private byte _x;
        private byte _y;
        private ulong _team;
        private readonly Dictionary<ulong, TraitSignState> _states = new Dictionary<ulong, TraitSignState>(Provider.maxPlayers);
        private TraitSignState GetState(ulong pl) => _state < TraitSignState.Ready ? _state : (_states.TryGetValue(pl, out TraitSignState st) ? st : _state);
        private void SetState(ulong pl, TraitSignState st) => _states[pl] = st;
        public void Init(TraitData d, BarricadeDrop drop)
        {
            if (!Regions.tryGetCoordinate(drop.model.transform.position, out _x, out _y))
                Destroy(this);
            else
            {
                Data = d;
                _drop = drop;
                if (Data.Team is 1 or 2)
                {
                    _team = Data.Team;
                }
                else
                {
                    Vector2 pos = new Vector2(drop.model.transform.position.x, drop.model.transform.position.z);
                    if (TeamManager.Team1Main.IsInside(pos))
                        _team = 1;
                    else if (TeamManager.Team2Main.IsInside(pos))
                        _team = 2;
                }
                _state = TraitSignState.Unknown;
            }
        }
        public void UpdateTimeDelay()
        {
            _checkTime = true;
        }
        [UsedImplicitly]
        private void FixedUpdate()
        {
            if (_state == TraitSignState.NotInitialized) return;
            float time = Time.realtimeSinceStartup;

            if (_checkTime || time - _lastDelayCheck > 1f)
            {
                _lastDelayCheck = time;
                _checkTime = false;
                if (Delay.IsDelayed(Data.Delays, out Delay delay, _team))
                {
                    if (delay.Type == DelayType.Time)
                    {
                        _state = TraitSignState.TimeDelayed;
                        UpdateSign();
                    }
                    else if (_state != TraitSignState.Delayed)
                    {
                        _state = TraitSignState.Delayed;
                        UpdateSign();
                    }
                }
                else
                {
                    for (int p = 0; p < PlayerManager.OnlinePlayers.Count; ++p)
                    {
                        UCPlayer player = PlayerManager.OnlinePlayers[p];
                        if (Regions.checkArea(_x, _y, player.Player.movement.region_x, player.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                        {
                            TraitSignState st = GetState(player.Steam64);
                            for (int i = 0; i < player.ActiveTraits.Count; ++i)
                            {
                                if (player.ActiveTraits[i].Data.Type == Data.Type)
                                {
                                    if (!player.ActiveTraits[i].Data.LastsUntilDeath)
                                    {
                                        if (st != TraitSignState.ActiveTime)
                                            SetState(player.Steam64, TraitSignState.ActiveTime);
                                        UpdateSign(player);
                                        return;
                                    }
                                    else
                                    {
                                        if (st != TraitSignState.ActiveDeath)
                                        {
                                            SetState(player.Steam64, TraitSignState.ActiveDeath);
                                            UpdateSign(player);
                                            return;
                                        }
                                    }
                                    break;
                                }
                            }
                            if (!player.OnDuty() &&
                                (CooldownManager.HasCooldown(player, CooldownType.IndividualRequestTrait, out _, Data.TypeName) ||
                                 CooldownManager.HasCooldownNoStateCheck(player, CooldownType.GlobalRequestTrait, out _)))
                            {
                                if (st != TraitSignState.Cooldown)
                                    SetState(player.Steam64, TraitSignState.Cooldown);
                                UpdateSign(player);
                            }
                            if (st != TraitSignState.Ready)
                            {
                                SetState(player.Steam64, TraitSignState.Ready);
                                UpdateSign(player);
                            }
                        }
                    }
                    _state = TraitSignState.Ready;
                }
            }
        }
        private void UpdateSign()
        {
            if (_drop is not null && !_drop.GetServersideData().barricade.isDead)
                Signs.BroadcastSignUpdate(_drop, false);
        }
        private void UpdateSign(UCPlayer player)
        {
            if (_drop is not null && !_drop.GetServersideData().barricade.isDead)
                Signs.SendSignUpdate(_drop, player);
        }
        [UsedImplicitly]
        private void Start()
        {
            L.LogDebug("Registered Trait sign: " + (Data?.TypeName ?? "uninited"));
            EventDispatcher.PlayerLeft += OnPlayerLeft;
        }
        [UsedImplicitly]
        private void OnDestroy()
        {
            EventDispatcher.PlayerLeft -= OnPlayerLeft;
            L.LogDebug("Destroyed Trait sign: " + (Data?.TypeName ?? "uninited"));
        }
        private void OnPlayerLeft(PlayerEvent e)
        {
            _states.Remove(e.Steam64);
        }
        private enum TraitSignState : byte
        {
            NotInitialized = 0,
            Unknown = 1,
            Delayed = 2,
            TimeDelayed = 3,
            Cooldown = 5,
            ActiveDeath = 6,
            ActiveTime = 7,
            Ready = 4,
        }
    }
}
