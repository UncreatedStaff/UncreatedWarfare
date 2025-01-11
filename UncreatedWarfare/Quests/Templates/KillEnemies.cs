using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Quests.Parameters;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Quests.Templates;
public class KillEnemies : QuestTemplate<KillEnemies, KillEnemies.Tracker, KillEnemies.State>
{
    public Int32ParameterTemplate Kills { get; set; }
    public SingleParameterTemplate? Range { get; set; }
    public AssetParameterTemplate<ItemWeaponAsset>? Weapons { get; set; }
    public AssetParameterTemplate<ItemWeaponAsset>? Turrets { get; set; }
    public AssetParameterTemplate<VehicleAsset>? Emplacements { get; set; }
    public KitNameParameterTemplate? Kit { get; set; }
    public EnumParameterTemplate<Class>? KitClass { get; set; }
    public EnumParameterTemplate<Branch>? KitBranch { get; set; }
    public EnumParameterTemplate<FirearmClass>? WeaponType { get; set; }
    public bool RequireSquad { get; set; }
    public bool RequireFullSquad { get; set; }
    public bool RequireDefense { get; set; }
    public bool RequireAttack { get; set; }
    public bool RequireObjective { get; set; }
    public KillEnemies(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : IQuestState<KillEnemies>
    {
        [JsonIgnore]
        public string Text { get; set; }

        [RewardVariable("k")]
        public QuestParameterValue<int> Kills { get; set; }

        [RewardVariable("d")]
        public QuestParameterValue<float>? Range { get; set; }
        public QuestParameterValue<Guid>? Weapons { get; set; }
        public QuestParameterValue<Guid>? Turrets { get; set; }
        public QuestParameterValue<Guid>? Emplacements { get; set; }
        public QuestParameterValue<Class>? KitClass { get; set; }
        public QuestParameterValue<Branch>? KitBranch { get; set; }
        public QuestParameterValue<FirearmClass>? WeaponType { get; set; }
        public QuestParameterValue<string>? Kit { get; set; }
        public bool RequireSquad { get; set; }
        public bool RequireFullSquad { get; set; }
        public bool RequireDefense { get; set; }
        public bool RequireAttack { get; set; }
        public bool RequireObjective { get; set; }

        [JsonIgnore]
        public QuestParameterValue<int> FlagValue => Kills;
        public async UniTask CreateFromConfigurationAsync(IQuestStateConfiguration configuration, KillEnemies template, IServiceProvider serviceProvider, CancellationToken token)
        {
            Kills            = configuration.ParseInt32Value("Kills", Int32ParameterTemplate.WildcardInclusive);
            Range            = configuration.ParseSingleValue("Range");
            KitClass         = configuration.ParseEnumValue<Class>("KitClass");
            KitBranch        = configuration.ParseEnumValue<Branch>("KitBranch");
            Weapons          = configuration.ParseAssetValue<ItemWeaponAsset>("Weapons");
            Turrets          = configuration.ParseAssetValue<ItemWeaponAsset>("Turrets");
            Emplacements     = configuration.ParseAssetValue<VehicleAsset>("Emplacements");
            WeaponType       = configuration.ParseEnumValue<FirearmClass>("WeaponType");
            RequireSquad     = configuration.ParseBooleanValue("RequireSquad");
            RequireFullSquad = configuration.ParseBooleanValue("RequireFullSquad");
            RequireDefense   = configuration.ParseBooleanValue("RequireDefense");
            RequireAttack    = configuration.ParseBooleanValue("RequireAttack");
            RequireObjective = configuration.ParseBooleanValue("RequireObjective");
            Kit              = await configuration.ParseKitNameValue("Kit", serviceProvider);

            FormatText(template);
        }

        public async UniTask CreateFromTemplateAsync(KillEnemies template, CancellationToken token)
        {
            Kills = await template.Kills.CreateValue(template.ServiceProvider);

            Range        = template.Range        == null ? null : await template.Range.CreateValue(template.ServiceProvider);
            Weapons      = template.Weapons      == null ? null : await template.Weapons.CreateValue(template.ServiceProvider);
            Turrets      = template.Turrets      == null ? null : await template.Turrets.CreateValue(template.ServiceProvider);
            Emplacements = template.Emplacements == null ? null : await template.Emplacements.CreateValue(template.ServiceProvider);
            KitClass     = template.KitClass     == null ? null : await template.KitClass.CreateValue(template.ServiceProvider);
            KitBranch    = template.KitBranch    == null ? null : await template.KitBranch.CreateValue(template.ServiceProvider);
            Kit          = template.Kit          == null ? null : await template.Kit.CreateValue(template.ServiceProvider);
            WeaponType   = template.WeaponType   == null ? null : await template.WeaponType.CreateValue(template.ServiceProvider);

            RequireSquad     = template.RequireSquad;
            RequireFullSquad = template.RequireFullSquad;
            RequireDefense   = template.RequireDefense;
            RequireAttack    = template.RequireAttack;
            RequireObjective = template.RequireObjective;

            FormatText(template);
        }

        private void FormatText(KillEnemies template)
        {
            ITranslationValueFormatter formatter = template.ServiceProvider.GetRequiredService<ITranslationValueFormatter>();

            FactionInfo? kitFaction = null;
            if (Kit != null && (Kit.ValueType == ParameterValueType.Constant || Kit.SelectionType == ParameterSelectionType.Selective))
            {
                KitManager kitManager = template.ServiceProvider.GetRequiredService<KitManager>();
                IFactionDataStore factinDataStore = template.ServiceProvider.GetRequiredService<IFactionDataStore>();

                string s = Kit.GetSingleValue();
                if (kitManager.Cache.TryGetKit(s, out Kit k) && k.FactionId.HasValue)
                {
                    kitFaction = factinDataStore.FindFaction(k.FactionId);
                }
            }

            Text = string.Format(template.Text.Translate(null, template.Type.Name),
                "{0}",
                Kills.GetDisplayString(formatter),
                Range?.GetDisplayString(formatter),
                Weapons?.GetDisplayString(formatter),
                Kit?.GetDisplayString(formatter),
                kitFaction == null ? "dddddd" : HexStringHelper.FormatHexColor(kitFaction.Color),
                KitClass?.GetDisplayString(formatter),
                KitBranch?.GetDisplayString(formatter)
            );
        }

        /// <inheritdoc />
        public string CreateQuestDescriptiveString()
        {
            return Text;
        }
    }
    public class Tracker : QuestTracker, IEventListener<PlayerDied>
    {
        private readonly string _text;

        private readonly int _targetKills;
        private readonly QuestParameterValue<float> _range;
        private readonly QuestParameterValue<Guid> _weapons;
        private readonly QuestParameterValue<Guid>? _turrets;
        private readonly QuestParameterValue<Guid>? _emplacements;
        private readonly QuestParameterValue<string> _kits;
        private readonly QuestParameterValue<Class> _class;
        private readonly QuestParameterValue<Branch> _branch;
        private readonly QuestParameterValue<FirearmClass> _weaponType;
        private readonly bool _needsSquad;
        private readonly bool _squadMustBeFull;
        private readonly bool _needsDefense;
        private readonly bool _needsAttack;
        private readonly bool _needsObjective;

        private int _kills;
        public override bool IsComplete => _kills >= _targetKills;
        public override short FlagValue => (short)_kills;
        public Tracker(WarfarePlayer player, IServiceProvider serviceProvider, KillEnemies quest, State state, IQuestPreset? preset)
            : base(player, serviceProvider, quest, state, preset)
        {
            _targetKills = state.Kills.GetSingleValueOrMinimum();

            _range = state.Range               ?? SingleParameterTemplate.WildcardInclusive;
            _weapons = state.Weapons           ?? AssetParameterTemplate<ItemWeaponAsset>.WildcardInclusive;
            _kits = state.Kit                  ?? StringParameterTemplate.WildcardInclusive;
            _class = state.KitClass            ?? EnumParameterTemplate<Class>.WildcardInclusive;
            _branch = state.KitBranch          ?? EnumParameterTemplate<Branch>.WildcardInclusive;
            _weaponType = state.WeaponType     ?? EnumParameterTemplate<FirearmClass>.WildcardInclusive;

            _turrets = state.Turrets;
            _emplacements = state.Emplacements;

            _needsSquad = state.RequireSquad || state.RequireFullSquad;
            _squadMustBeFull = state.RequireFullSquad;
            _needsDefense = state.RequireDefense;
            _needsAttack = state.RequireAttack;
            _needsObjective = state.RequireObjective && (_needsDefense || _needsAttack);

            _text = state.CreateQuestDescriptiveString();
        }

        void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
        {
            if (e.Instigator.m_SteamID != Player.Steam64.m_SteamID || !e.WasEffectiveKill ||
                e.Cause == EDeathCause.SHRED)
            {
                return;
            }

            //Console.WriteLine($"{Player.Steam64} - {Quest.Name} - Applies kill on ({e.Player.Steam64}). range: {_range}, weapons: {_weapons}, turrets: {_turrets}, empl: {_emplacements}, kits: {_kits}, class: {_class}, branch: {_branch}, weapont: {_weaponType}, nSquad: {_needsSquad}, nFullSquad: {_squadMustBeFull}, def: {_needsDefense}, atk: {_needsAttack}, obj: {_needsObjective}.");
            // distance
            if (!_range.IsWildcardInclusive() && (
                    e.Cause is not EDeathCause.GUN and not EDeathCause.MISSILE and not EDeathCause.GRENADE and not EDeathCause.MELEE and not EDeathCause.SPLASH
                    || (_range.ValueType == ParameterValueType.Constant ? e.KillDistance < _range.GetSingleValue() : !_range.IsMatch(e.KillDistance)))
                )
            {
                return;
            }

            // weapon
            if (!_weapons.IsWildcardInclusive()
                && !_weapons.IsMatch<ItemWeaponAsset>(e.PrimaryAsset)
                && !_weapons.IsMatch<ItemWeaponAsset>(e.SecondaryAsset))
            {
                return;
            }

            // firearm class
            if (!_weaponType.IsWildcardInclusive())
            {
                if (!e.PrimaryAsset.TryGetAsset(out Asset? asset) || asset is not ItemGunAsset gun)
                    return;

                FirearmClass firearmClass = ItemUtility.GetFirearmClass(gun);
                if (firearmClass == FirearmClass.TooDifficultToClassify || !_weaponType.IsMatch(firearmClass))
                {
                    return;
                }
            }

            // kit name
            if (!_kits.IsWildcardInclusive() && (e.KillerKitName == null || !_kits.IsMatch(e.KillerKitName)))
            {
                return;
            }

            // kit class
            if (!_class.IsWildcardInclusive() && (!e.KillerClass.HasValue || !_class.IsMatch(e.KillerClass.Value)))
            {
                return;
            }

            // kit branch
            if (!_branch.IsWildcardInclusive() && (!e.KillerBranch.HasValue || !_branch.IsMatch(e.KillerBranch.Value)))
            {
                return;
            }

            // turret/emplacement
            if (_turrets != null || _emplacements != null)
            {
                InteractableVehicle? veh = Player.UnturnedPlayer.movement.getVehicle();
                if (veh == null)
                {
                    return;
                }

                WarfareVehicleComponent? comp = veh.GetComponent<WarfareVehicleComponent>();

                bool found = false;
                for (int i = 0; i < veh.turrets.Length; i++)
                {
                    Passenger passenger = veh.turrets[i];
                    if (passenger is not { player: not null }
                        || passenger.player.playerID.steamID.m_SteamID != Player.Steam64.m_SteamID
                        || passenger.turret == null)
                    {
                        continue;
                    }

                    if (_turrets == null)
                    {
                        if (comp?.WarfareVehicle?.Info != null && comp.WarfareVehicle.Info.Type.IsEmplacement() && _emplacements!.IsMatch<VehicleAsset>(veh.asset))
                        {
                            found = true;
                            break;
                        }
                    }
                    else if (_turrets.IsMatch<ItemWeaponAsset>(Assets.find(EAssetType.ITEM, passenger.turret.itemID)))
                    {
                        if (_emplacements != null && (comp?.WarfareVehicle?.Info == null || !comp.WarfareVehicle.Info.Type.IsEmplacement() || !_emplacements!.IsMatch<VehicleAsset>(veh.asset)))
                            return;
                        
                        found = true;
                        break;
                    }

                    return;
                }

                if (!found)
                    return;
            }

            // squads
            if (_needsSquad && Player.GetSquad() is not { Members.Count: > 1 })
            {
                return;
            }
            
            if (_squadMustBeFull && Player.GetSquad() is not { Members.Count: Squad.MaxMembers })
            {
                return;
            }

            // objectives
            if (_needsDefense || _needsAttack || _needsObjective)
            {
                if (serviceProvider.GetService<Layout>() is { ActivePhase: ActionPhase } && serviceProvider.GetService<IAttackDefenceDecider>() is { } decider)
                {
                    bool atk = decider.IsAttacking(Player) || decider.IsDefending(e.Player);
                    bool def = decider.IsDefending(Player) || decider.IsAttacking(e.Player);

                    if (!atk && _needsAttack)
                    {
                        return;
                    }

                    if (!def && _needsDefense)
                    {
                        return;
                    }

                    if (!atk && !def && _needsObjective)
                    {
                        return;
                    }
                }
            }

            Interlocked.Increment(ref _kills);
            InvokeUpdate();
        }

        public override void WriteProgress(Utf8JsonWriter writer)
        {
            writer.WriteNumber("Kills", _kills);
        }

        public override void ReadProgress(ref Utf8JsonReader reader)
        {
            JsonUtility.ReadTopLevelProperties(ref reader, (ref Utf8JsonReader reader, string property, ref object? _) =>
            {
                if (property.Equals("Kills", StringComparison.Ordinal))
                {
                    _kills = reader.GetInt32();
                }
            });
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return UnturnedUIUtility.QuickFormat(_text, _kills, 0);
        }
    }
}