using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.NewQuests.Templates;
public class KillEnemies : QuestTemplate<KillEnemies, KillEnemies.Tracker, KillEnemies.State>
{
    public Int32ParameterTemplate Kills { get; set; }
    public SingleParameterTemplate? Range { get; set; }
    public AssetParameterTemplate<ItemWeaponAsset>? Weapons { get; set; }
    public AssetParameterTemplate<ItemWeaponAsset>? Turrets { get; set; }
    public AssetParameterTemplate<ItemWeaponAsset>? Emplacements { get; set; }
    public KitNameParameterTemplate? Kit { get; set; }
    public EnumParameterTemplate<Class>? KitClass { get; set; }
    public EnumParameterTemplate<Branch>? KitBranch { get; set; }
    public bool RequireSquad { get; set; }
    public bool RequireFullSquad { get; set; }
    public bool RequireDefense { get; set; }
    public bool RequireAttack { get; set; }
    public bool RequireObjective { get; set; }
    public KillEnemies(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : BaseState
    {
        [RewardField("k")]
        public QuestParameterValue<int> Kills { get; set; }

        [RewardField("d")]
        public QuestParameterValue<float>? Range { get; set; }
        public QuestParameterValue<Guid>? Weapons { get; set; }
        public QuestParameterValue<Guid>? Turrets { get; set; }
        public QuestParameterValue<Guid>? Emplacements { get; set; }
        public QuestParameterValue<Class>? KitClass { get; set; }
        public QuestParameterValue<Branch>? KitBranch { get; set; }
        public QuestParameterValue<string>? Kit { get; set; }
        public bool RequireSquad { get; set; }
        public bool RequireFullSquad { get; set; }
        public bool RequireDefense { get; set; }
        public bool RequireAttack { get; set; }
        public bool RequireObjective { get; set; }
        public override QuestParameterValue<int> FlagValue => Kills;
        public override async UniTask CreateFromConfigurationAsync(IConfiguration configuration, IServiceProvider serviceProvider, CancellationToken token)
        {
            string? killsStr = configuration["Kills"],
                    rangeStr = configuration["Range"],
                    classStr = configuration["KitClass"],
                    branchStr = configuration["KitBranch"],
                    kitNameStr = configuration["Kit"],
                    weaponsStr = configuration["Weapons"],
                    turretsStr = configuration["Turrets"],
                    emplacementsStr = configuration["Emplacements"];

            RequireSquad     = configuration.GetValue("RequireSquad", defaultValue: false);
            RequireFullSquad = configuration.GetValue("RequireFullSquad", defaultValue: false);
            RequireDefense   = configuration.GetValue("RequireDefense", defaultValue: false);
            RequireAttack    = configuration.GetValue("RequireAttack", defaultValue: false);
            RequireObjective = configuration.GetValue("RequireObjective", defaultValue: false);

            QuestParameterValue<float>? range = null;
            QuestParameterValue<Class>? kitClass = null;
            QuestParameterValue<Branch>? kitBranch = null;

            QuestParameterValue<Guid>? weapons = null,
                                       turrets = null,
                                       emplacements = null;

            if (string.IsNullOrEmpty(killsStr) || !Int32ParameterTemplate.TryParseValue(killsStr, out QuestParameterValue<int>? kills))
                throw new QuestConfigurationException(typeof(KillEnemies), "Failed to parse integer parameter for \"Kills\".");
            
            if (!string.IsNullOrEmpty(rangeStr) && !SingleParameterTemplate.TryParseValue(rangeStr, out range))
                throw new QuestConfigurationException(typeof(KillEnemies), "Failed to parse float parameter for \"Range\".");
            
            if (!string.IsNullOrEmpty(weaponsStr) && !AssetParameterTemplate<ItemWeaponAsset>.TryParseValue(weaponsStr, out weapons))
                throw new QuestConfigurationException(typeof(KillEnemies), "Failed to parse ItemWeaponAsset parameter for \"Weapons\".");
            
            if (!string.IsNullOrEmpty(turretsStr) && !AssetParameterTemplate<ItemWeaponAsset>.TryParseValue(turretsStr, out turrets))
                throw new QuestConfigurationException(typeof(KillEnemies), "Failed to parse ItemWeaponAsset parameter for \"Turrets\".");
            
            if (!string.IsNullOrEmpty(emplacementsStr) && !AssetParameterTemplate<ItemWeaponAsset>.TryParseValue(emplacementsStr, out emplacements))
                throw new QuestConfigurationException(typeof(KillEnemies), "Failed to parse ItemWeaponAsset parameter for \"Emplacements\".");
            
            if (!string.IsNullOrEmpty(classStr) && !EnumParameterTemplate<Class>.TryParseValue(classStr, out kitClass))
                throw new QuestConfigurationException(typeof(KillEnemies), "Failed to parse Class parameter for \"KitClass\".");
            
            if (!string.IsNullOrEmpty(branchStr) && !EnumParameterTemplate<Branch>.TryParseValue(branchStr, out kitBranch))
                throw new QuestConfigurationException(typeof(KillEnemies), "Failed to parse Branch parameter for \"KitBranch\".");

            if (!string.IsNullOrEmpty(kitNameStr))
            {
                Kit = await KitNameParameterTemplate.TryParseValue(kitNameStr, serviceProvider)
                      ?? throw new QuestConfigurationException(typeof(KillEnemies), "Failed to parse kit name parameter for \"Kit\".");
            }

            Kills = kills;
            Range = range;
            Weapons = weapons;
            Turrets = turrets;
            Emplacements = emplacements;
            KitClass = kitClass;
            KitBranch = kitBranch;
        }
        public override async UniTask CreateFromTemplateAsync(KillEnemies data, CancellationToken token)
        {
            Kills = await data.Kills.CreateValue(data.ServiceProvider);

            Range        = data.Range        == null ? null : await data.Range.CreateValue(data.ServiceProvider);
            Weapons      = data.Weapons      == null ? null : await data.Weapons.CreateValue(data.ServiceProvider);
            Turrets      = data.Turrets      == null ? null : await data.Turrets.CreateValue(data.ServiceProvider);
            Emplacements = data.Emplacements == null ? null : await data.Emplacements.CreateValue(data.ServiceProvider);
            KitClass     = data.KitClass     == null ? null : await data.KitClass.CreateValue(data.ServiceProvider);
            KitBranch    = data.KitBranch    == null ? null : await data.KitBranch.CreateValue(data.ServiceProvider);
            Kit          = data.Kit          == null ? null : await data.Kit.CreateValue(data.ServiceProvider);

            RequireSquad     = data.RequireSquad;
            RequireFullSquad = data.RequireFullSquad;
            RequireDefense   = data.RequireDefense;
            RequireAttack    = data.RequireAttack;
            RequireObjective = data.RequireObjective;
        }
    }
    public class Tracker : QuestTracker, IEventListener<PlayerDied>
    {
        private readonly int _targetKills;
        private readonly QuestParameterValue<float> _range;
        private readonly QuestParameterValue<Guid> _weapons;
        private readonly QuestParameterValue<Guid> _turrets;
        private readonly QuestParameterValue<Guid> _emplacements;
        private readonly QuestParameterValue<string> _kits;
        private readonly QuestParameterValue<Class> _class;
        private readonly QuestParameterValue<Branch> _branch;
        private readonly bool _needsSquad;
        private readonly bool _squadMustBeFull;
        private readonly bool _needsDefense;
        private readonly bool _needsAttack;
        private readonly bool _needsObjective;

        private int _kills;
        public override bool IsComplete => _kills >= _targetKills;
        public override short FlagValue => (short)_kills;
        public Tracker(UCPlayer player, IServiceProvider serviceProvider, KillEnemies quest, State state, IQuestPreset? preset)
            : base(player, serviceProvider, quest, state, preset)
        {
            _targetKills = state.Kills.GetSingleValueOrMinimum();

            _range = state.Range               ?? SingleParameterTemplate.WildcardInclusive;
            _weapons = state.Weapons           ?? AssetParameterTemplate<ItemWeaponAsset>.WildcardInclusive;
            _turrets = state.Turrets           ?? AssetParameterTemplate<ItemWeaponAsset>.WildcardInclusive;
            _emplacements = state.Emplacements ?? AssetParameterTemplate<ItemWeaponAsset>.WildcardInclusive;
            _kits = state.Kit                  ?? StringParameterTemplate.WildcardInclusive;
            _class = state.KitClass            ?? EnumParameterTemplate<Class>.WildcardInclusive;
            _branch = state.KitBranch          ?? EnumParameterTemplate<Branch>.WildcardInclusive;

            _needsSquad = state.RequireSquad || state.RequireFullSquad;
            _squadMustBeFull = state.RequireFullSquad;
            _needsDefense = state.RequireDefense;
            _needsAttack = state.RequireAttack;
            _needsObjective = state.RequireObjective && (_needsDefense || _needsAttack);
        }

        [EventListener(RequiresMainThread = false)]
        void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
        {
            if (e.Instigator.m_SteamID != Player.Steam64 || !e.WasEffectiveKill || e.Cause == EDeathCause.SHRED)
                return;

            // distance
            if (!_range.IsWildcardInclusive() && (
                    e.Cause is not EDeathCause.GUN and not EDeathCause.MISSILE and not EDeathCause.GRENADE and not EDeathCause.MELEE and not EDeathCause.SPLASH
                    || !_range.IsMatch(e.KillDistance))
                )
            {
                return;
            }

            // weapon
            if (!_weapons.IsWildcardInclusive()
                && (e.PrimaryAsset == null || !_weapons.IsMatch<ItemWeaponAsset>(e.PrimaryAsset))
                && (e.SecondaryAsset == null || !_weapons.IsMatch<ItemWeaponAsset>(e.SecondaryAsset)))
            {
                return;
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
            QuestParameterValue<Guid>? emplOrTurrets = null;
            if (_turrets.IsWildcardInclusive())
            {
                if (!_emplacements.IsWildcardInclusive())
                    emplOrTurrets = _emplacements;
            }
            else if (_emplacements.IsWildcardInclusive())
            {
                emplOrTurrets = _turrets;
            }

            if (emplOrTurrets != null)
            {
                InteractableVehicle? veh = Player.Player.movement.getVehicle();
                if (veh == null)
                    return;

                bool found = false;
                for (int i = 0; i < veh.turrets.Length; i++)
                {
                    Passenger passenger = veh.turrets[i];
                    if (passenger is not { player: not null }
                        || passenger.player.playerID.steamID.m_SteamID != Player.Steam64
                        || passenger.turret == null)
                    {
                        continue;
                    }

                    if (!_turrets.IsMatch<ItemWeaponAsset>(Assets.find(EAssetType.ITEM, passenger.turret.itemID)))
                        return;

                    if (VehicleBay.GetSingletonQuick() is { } manager)
                    {
                        if (manager.GetDataSync(veh.asset.GUID) is { } data && VehicleData.IsEmplacement(data.Type) != ReferenceEquals(emplOrTurrets, _emplacements))
                            return;
                    }

                    found = true;
                    break;
                }

                if (!found)
                    return;
            }

            // squads
            if (_needsSquad && Player.Squad is not { Members.Count: > 1 })
            {
                return;
            }

            if (_squadMustBeFull && Player.Squad is not { Members.Count: SquadManager.SQUAD_MAX_MEMBERS })
            {
                return;
            }

            // objectives
            bool obj = false;
            if (_needsDefense && !WasDefending(e, serviceProvider, out obj))
            {
                return;
            }
            if (_needsAttack && !WasAttacking(e, serviceProvider, out obj))
            {
                return;
            }
            if (_needsObjective && !obj)
            {
                return;
            }
            
            Interlocked.Increment(ref _kills);
            InvokeUpdate();
        }

        private bool WasDefending(PlayerDied e, IServiceProvider serviceProvider, out bool objective)
        {
            objective = false;
            Layout? layout = serviceProvider.GetService<Layout>();
            if (layout == null)
                return false;

            // todo
            return false;
        }
        private bool WasAttacking(PlayerDied e, IServiceProvider serviceProvider, out bool objective)
        {
            objective = false;
            Layout? layout = serviceProvider.GetService<Layout>();
            if (layout == null)
                return false;

            // todo
            return false;
        }

        protected override void WriteProgress(Utf8JsonWriter writer)
        {
            writer.WriteNumber("kills", _kills);
        }

        protected override void ReadProgress(ref Utf8JsonReader reader)
        {
            JsonUtility.ReadTopLevelProperties(ref reader, (ref Utf8JsonReader reader, string property, ref object? _) =>
            {
                if (property.Equals("kills", StringComparison.Ordinal))
                {
                    _kills = reader.GetInt32();
                }
            });
        }
    }
}