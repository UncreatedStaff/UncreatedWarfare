using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Traits;

public static class TraitSigns
{
    public const string TRAIT_SIGN_PREFIX = Signs.PREFIX + Signs.TRAIT_PREFIX;
    internal static string TranslateTraitSign(TraitData trait, UCPlayer player)
    {
        string str = Localization.GetLang(player.Steam64);
        ulong team = trait.Team is 1 or 2 ? trait.Team : player.GetTeam();
        str = TranslateTraitSign(trait, str, team, out bool fmt);
        return fmt ? FormatTraitSign(trait, str, player, team) : str;
    }
    public static unsafe string FormatTraitSign(TraitData trait, string tr2, UCPlayer player, ulong team)
    {
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i].Data.Type == trait.Type)
            {
                if (trait.LastsUntilDeath)
                    return Signs.QuickFormat(tr2, T.TraitSignAlreadyActiveDeath.Translate(player));
                else
                {
                    int secs = Mathf.CeilToInt(trait.EffectDuration - (Time.realtimeSinceStartup - player.ActiveTraits[i].StartTime));
                    return Signs.QuickFormat(tr2, T.TraitSignAlreadyActiveTime.Translate(player, secs / 60, secs % 60));
                }
            }
        }
        if (!player.OnDuty())
        {
            if (CooldownManager.HasCooldown(player, ECooldownType.REQUEST_TRAIT_SINGLE, out Cooldown cooldown, trait.TypeName))
            {
                if (CooldownManager.HasCooldownNoStateCheck(player, ECooldownType.REQUEST_TRAIT_GLOBAL, out Cooldown gcooldown) && gcooldown.SecondsLeft > cooldown.SecondsLeft)
                    cooldown = gcooldown;
            }
            else if (!CooldownManager.HasCooldownNoStateCheck(player, ECooldownType.REQUEST_TRAIT_GLOBAL, out cooldown))
                goto next;

            int secs = Mathf.CeilToInt(cooldown.SecondsLeft);
            return Signs.QuickFormat(tr2, T.TraitSignCooldown.Translate(player, secs / 60, secs % 60));
        }
    next:
        if (trait.UnlockRequirements is not null)
        {
            for (int i = 0; i < trait.UnlockRequirements.Length; ++i)
            {
                BaseUnlockRequirement req = trait.UnlockRequirements[i];
                if (!req.CanAccess(player))
                    return Signs.QuickFormat(tr2, req.GetSignText(player));
            }
        }

        if (player.Kit != null && player.KitClass > EClass.UNARMED)
        {
            if (!trait.CanClassUse(player.KitClass))
            {
                if (trait.ClassListIsBlacklist || trait.ClassList.Length > 2 || trait.ClassList.Length == 0)
                    return Signs.QuickFormat(tr2, T.TraitSignClassBlacklisted.Translate(player, player.KitClass));
                else if (trait.ClassList.Length == 2)
                    return Signs.QuickFormat(tr2, T.TraitSignClassWhitelisted2.Translate(player, trait.ClassList[0], trait.ClassList[1]));
                else
                    return Signs.QuickFormat(tr2, T.TraitSignClassWhitelisted1.Translate(player, trait.ClassList[0]));
            }
        }
        else
            return Signs.QuickFormat(tr2, T.TraitSignNoKit.Translate(player));
        if (player.Squad is null)
        {
            if (trait.RequireSquadLeader)
                return Signs.QuickFormat(tr2, T.TraitSignRequiresSquadLeader.Translate(player));
            else if (trait.RequireSquad)
                return Signs.QuickFormat(tr2, T.TraitSignRequiresSquad.Translate(player));
        }
        else if (trait.RequireSquadLeader && player.Squad.Leader.Steam64 != player.Steam64)
            return Signs.QuickFormat(tr2, T.TraitSignRequiresSquadLeader.Translate(player));

        return Signs.QuickFormat(tr2, string.Empty /* T.TraitSignUnlocked.Translate(player) */);
    }
    internal static string TranslateTraitSign(TraitData trait, string language, ulong team, out bool fmt)
    {
        bool keepline = false;
        string name = trait.NameTranslations.Translate(language);
        for (int i = 0; i < name.Length; ++i)
        {
            if (name[i] == '\n')
            {
                keepline = true;
                break;
            }
        }
        name = "<b>" + name.ToUpper().ColorizeTMPro(UCWarfare.GetColorHex("kit_public_header"), true) + "</b>";

        string cost = trait.CreditCost > 0 ? T.KitCreditCost.Translate(language, trait.CreditCost) : T.TraitSignFree.Translate(language);

        if (!keepline) cost = "\n" + cost;

        string? req = null;

        if (!trait.CanGamemodeUse())
            req = T.TraitGamemodeBlacklisted.Translate(language);
        else if (trait.Delays != null && trait.Delays.Length > 0 && Delay.IsDelayed(trait.Delays, out Delay delay, team))
            req = Localization.GetDelaySignText(in delay, language, team);

        fmt = req is null;
        return
            name + "\n" +
            cost + "\n" +
            (fmt ? "{0}\n" : (req + "\n")) +
            trait.DescriptionTranslations.Translate(language).ColorizeTMPro(UCWarfare.GetColorHex("trait_desc"));
    }
    public static void BroadcastAllTraitSigns()
    {
        if (TraitManager.Loaded && BarricadeManager.regions != null)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    BarricadeRegion reg = BarricadeManager.regions[x, y];
                    for (int i = 0; i < reg.drops.Count; ++i)
                    {
                        if (reg.drops[i].interactable is InteractableSign sign && sign.text.StartsWith(TRAIT_SIGN_PREFIX, StringComparison.OrdinalIgnoreCase))
                            Signs.BroadcastSign(sign.text, sign, x, y);
                    }
                }
            }
        }
    }
    public static void SendAllTraitSigns(UCPlayer player)
    {
        if (TraitManager.Loaded && BarricadeManager.regions != null)
        {
            int maxx = Math.Min(player.Player.movement.region_x + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            int maxy = Math.Min(player.Player.movement.region_y + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            for (int x = Math.Max(0, player.Player.movement.region_x - BarricadeManager.BARRICADE_REGIONS); x <= maxx; ++x)
            {
                for (int y = Math.Max(0, player.Player.movement.region_y - BarricadeManager.BARRICADE_REGIONS); y <= maxy; ++y)
                {
                    BarricadeRegion reg = BarricadeManager.regions[x, y];
                    for (int i = 0; i < reg.drops.Count; ++i)
                    {
                        if (reg.drops[i].interactable is InteractableSign sign && sign.text.StartsWith(TRAIT_SIGN_PREFIX, StringComparison.OrdinalIgnoreCase))
                            Signs.SendSignUpdate(sign, player);
                    }
                }
            }
        }
    }
    public static void BroadcastAllTraitSigns(TraitData data)
    {
        string prefix = TRAIT_SIGN_PREFIX + data.TypeName;
        if (TraitManager.Loaded && BarricadeManager.regions != null)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    BarricadeRegion reg = BarricadeManager.regions[x, y];
                    for (int i = 0; i < reg.drops.Count; ++i)
                    {
                        if (reg.drops[i].interactable is InteractableSign sign && sign.text.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                            Signs.BroadcastSign(sign.text, sign, x, y);
                    }
                }
            }
        }
    }
    public static void SendAllTraitSigns(UCPlayer player, TraitData data)
    {
        string prefix = TRAIT_SIGN_PREFIX + data.TypeName;
        if (TraitManager.Loaded && BarricadeManager.regions != null)
        {
            int maxx = Math.Min(player.Player.movement.region_x + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            int maxy = Math.Min(player.Player.movement.region_y + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            for (int x = Math.Max(0, player.Player.movement.region_x - BarricadeManager.BARRICADE_REGIONS); x <= maxx; ++x)
            {
                for (int y = Math.Max(0, player.Player.movement.region_y - BarricadeManager.BARRICADE_REGIONS); y <= maxy; ++y)
                {
                    BarricadeRegion reg = BarricadeManager.regions[x, y];
                    for (int i = 0; i < reg.drops.Count; ++i)
                    {
                        if (reg.drops[i].interactable is InteractableSign sign && sign.text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            Signs.SendSignUpdate(sign, player);
                    }
                }
            }
        }
    }
    public static void InitTraitSign(TraitData d, BarricadeDrop drop, InteractableSign sign)
    {
        if (!drop.model.gameObject.TryGetComponent(out TraitSignComponent c))
        {
            c = drop.model.gameObject.AddComponent<TraitSignComponent>();
        }
        c.Init(d, drop, sign);
    }
    public static void TryRemoveComponent(BarricadeDrop drop, InteractableSign sign)
    {
        if (drop.model.gameObject.TryGetComponent(out TraitSignComponent c))
            UnityEngine.Object.Destroy(c);
    }
    internal static void OnBarricadeMoved(BarricadeDrop drop, InteractableSign sign)
    {
        if (drop.model.gameObject.TryGetComponent(out TraitSignComponent c))
            c.Init(c.data, drop, sign);
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
                    if (reg.drops[i].interactable is InteractableSign sign && sign.text.StartsWith(TRAIT_SIGN_PREFIX, StringComparison.OrdinalIgnoreCase))
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
        public ETraitSignState state;
        private float lastDelayCheck = 0f;
        private bool checkTime = false;
        public TraitData data;
        public InteractableSign sign;
        public byte x;
        public byte y;
        public ulong Team;
        private readonly Dictionary<ulong, ETraitSignPlayerState> _states = new Dictionary<ulong, ETraitSignPlayerState>(Provider.maxPlayers);
        private ETraitSignPlayerState GetState(ulong pl) => state < ETraitSignState.READY ? (ETraitSignPlayerState)state : (_states.TryGetValue(pl, out ETraitSignPlayerState st) ? st : (state == ETraitSignState.READY ? ETraitSignPlayerState.READY : (ETraitSignPlayerState)state));
        private void SetState(ulong pl, ETraitSignPlayerState st)
        {
            if (_states.ContainsKey(pl))
                _states[pl] = st;
            else
                _states.Add(pl, st);
        }
        public void Init(TraitData d, BarricadeDrop drop, InteractableSign sign)
        {
            if (!Regions.tryGetCoordinate(drop.model.transform.position, out x, out y))
                Destroy(this);
            else
            {
                this.data = d;
                this.sign = sign;
                if (data.Team is 1 or 2)
                {
                    Team = data.Team;
                }
                else
                {
                    Vector2 pos = new Vector2(drop.model.transform.position.x, drop.model.transform.position.z);
                    if (TeamManager.Team1Main.IsInside(pos))
                        Team = 1;
                    else if (TeamManager.Team2Main.IsInside(pos))
                        Team = 2;
                }
                state = ETraitSignState.UNKNOWN;
            }
        }
        public void UpdateTimeDelay()
        {
            checkTime = true;
        }
        private void FixedUpdate()
        {
            if (state == ETraitSignState.NOT_INITIALIZED) return;
            float time = Time.realtimeSinceStartup;

            if (checkTime || time - lastDelayCheck > 1f)
            {
                lastDelayCheck = time;
                checkTime = false;
                if (Delay.IsDelayed(data.Delays, out Delay delay, Team))
                {
                    if (delay.type == EDelayType.TIME)
                    {
                        state = ETraitSignState.TIME_DELAYED;
                        UpdateSign();
                    }
                    else if (state != ETraitSignState.DELAYED)
                    {
                        state = ETraitSignState.DELAYED;
                        UpdateSign();
                    }
                }
                else
                {
                    for (int p = 0; p < PlayerManager.OnlinePlayers.Count; ++p)
                    {
                        UCPlayer player = PlayerManager.OnlinePlayers[p];
                        if (Regions.checkArea(x, y, player.Player.movement.region_x, player.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                        {
                            ETraitSignPlayerState st = GetState(player.Steam64);
                            for (int i = 0; i < player.ActiveTraits.Count; ++i)
                            {
                                if (player.ActiveTraits[i].Data.Type == data.Type)
                                {
                                    if (!player.ActiveTraits[i].Data.LastsUntilDeath)
                                    {
                                        if (st != ETraitSignPlayerState.ACTIVE_TIME)
                                            SetState(player.Steam64, ETraitSignPlayerState.ACTIVE_TIME);
                                        UpdateSign(player);
                                        return;
                                    }
                                    else
                                    {
                                        if (st != ETraitSignPlayerState.ACTIVE_DEATH)
                                        {
                                            SetState(player.Steam64, ETraitSignPlayerState.ACTIVE_DEATH);
                                            UpdateSign(player);
                                            return;
                                        }
                                    }
                                    break;
                                }
                            }
                            if (!player.OnDuty() &&
                                (CooldownManager.HasCooldown(player, ECooldownType.REQUEST_TRAIT_SINGLE, out _, data.TypeName) ||
                                 CooldownManager.HasCooldownNoStateCheck(player, ECooldownType.REQUEST_TRAIT_GLOBAL, out _)))
                            {
                                if (st != ETraitSignPlayerState.COOLDOWN)
                                    SetState(player.Steam64, ETraitSignPlayerState.COOLDOWN);
                                UpdateSign(player);
                            }
                            if (st != ETraitSignPlayerState.READY)
                            {
                                SetState(player.Steam64, ETraitSignPlayerState.READY);
                                UpdateSign(player);
                            }
                        }
                    }
                    state = ETraitSignState.READY;
                }
            }
        }
        private void UpdateSign()
        {
            bool t = Team is 1 or 2;
            foreach (LanguageSet set in t
                         ? LanguageSet.InRegions(x, y, BarricadeManager.BARRICADE_REGIONS)
                         : LanguageSet.InRegionsByTeam(x, y, BarricadeManager.BARRICADE_REGIONS))
            {
                ulong team = t ? Team : set.Team;
                string txt = TranslateTraitSign(data, set.Language, team, out bool fmt);
                while (set.MoveNext())
                {
                    Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Unreliable, set.Next.Connection, fmt ? FormatTraitSign(data, txt, set.Next, team) : txt);
                }
            }
        }
        private void UpdateSign(UCPlayer player)
        {
            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Unreliable, player.Connection, TranslateTraitSign(data, player));
        }
        private void Start()
        {
            L.LogDebug("Registered Trait sign: " + (data?.TypeName ?? "uninited"));
            EventDispatcher.PlayerLeaving += OnPlayerLeft;
        }
        private void OnDestroy()
        {
            EventDispatcher.PlayerLeaving -= OnPlayerLeft;
            L.LogDebug("Destroyed Trait sign: " + (data?.TypeName ?? "uninited"));
        }
        private void OnPlayerLeft(PlayerEvent e)
        {
            _states.Remove(e.Steam64);
        }

        private enum ETraitSignPlayerState : byte
        {
            NOT_INITIALIZED = 0,
            UNKNOWN,
            DELAYED,
            TIME_DELAYED,
            COOLDOWN,
            ACTIVE_DEATH,
            ACTIVE_TIME,
            READY,
        }
    }

    private enum ETraitSignState : byte
    {
        NOT_INITIALIZED = 0,
        UNKNOWN,
        DELAYED,
        TIME_DELAYED,
        READY
    }
}
