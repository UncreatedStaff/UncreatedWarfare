using System;
using System.Collections.Generic;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Stats;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Stats.EventHandlers;
internal sealed class PlayerDatabaseStatsEventHandlers :
    IEventListener<PlayerAided>,
    IEventListener<PlayerDamaged>,
    IEventListener<PlayerDied>,
    IEventListener<FobRegistered>,
    IEventListener<FobDestroyed>
{
    private readonly DatabaseStatsBuffer _buffer;
    private readonly DeathTracker _deathTracker;
    private readonly IPlayerService _playerService;

    public PlayerDatabaseStatsEventHandlers(DatabaseStatsBuffer buffer, DeathTracker deathTracker, IPlayerService playerService)
    {
        _buffer = buffer;
        _deathTracker = deathTracker;
        _playerService = playerService;
    }


    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    void IEventListener<PlayerAided>.HandleEvent(PlayerAided e, IServiceProvider serviceProvider)
    {
        bool hasInstigator = !e.Medic.Equals(e.Player);

        e.Medic.CurrentSession?.MarkDirty();
        e.Player.CurrentSession?.MarkDirty();

        AidRecord record = new AidRecord
        {
            Steam64 = e.Player.Steam64.m_SteamID,
            Instigator = hasInstigator ? e.Medic.Steam64.m_SteamID : null,
            Team = (byte)e.Player.Team.Id,
            Health = e.HealthChange,
            IsRevive = e.IsRevive,
            Item = new UnturnedAssetReference(e.Item),
            InstigatorPosition = e.Medic.Position,
            Position = e.Player.Position,
            InstigatorSessionId = e.Medic.CurrentSession?.SessionId,
            SessionId = e.Player.CurrentSession?.SessionId,
            Timestamp = DateTimeOffset.UtcNow
        };

        _buffer.Enqueue(record);
    }

    private readonly PlayerDied _tempPlayerDiedArgs = new PlayerDied(new DamagePlayerParameters(null))
    {
        Player = null!
    };

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    void IEventListener<PlayerDamaged>.HandleEvent(PlayerDamaged e, IServiceProvider serviceProvider)
    {
        bool injured = e.Player.IsInjured();
        
        // dont count injure ticks
        if (injured && e.Parameters.cause == EDeathCause.BLEEDING)
        {
            return;
        }

        PlayerDied args = _tempPlayerDiedArgs;

        args.ActiveVehicle = null;
        args.DriverAssist = null;
        args.PrimaryAsset = null;
        args.SecondaryAsset = null;
        args.ThirdParty = null;
        args.ThirdPartySession = null;
        args.ThirdPartyAtFault = false;
        args.ThirdPartyId = null;
        args.ThirdPartyPoint = default;
        args.ThirdPartyTeam = null;
        args.Killer = null;
        args.KillerSession = null;
        args.KillerBranch = null;
        args.KillerClass = null;
        args.KillerKitName = null;
        args.KillerPoint = default;
        args.KillerTeam = null;
        args.DefaultMessage = null;
        args.IsGuaranteedCheaterKill = false;
        args.PlayerKitName = null;
        args.MessageKey = null;
        args.TurretVehicleOwner = null;
        args.WasBleedout = false;
        args.WillBan = false;
        args.TimeDeployed = 0f;
        _deathTracker.FillArgs(e.Player, e.Parameters.cause, e.Parameters.limb, e.Parameters.killer, args);

        bool isSuicide = args.WasSuicide;
        bool isTeamkill = args.WasTeamkill;

        bool hasKiller = !isSuicide && args.Instigator.GetEAccountType() == EAccountType.k_EAccountTypeIndividual;
        bool hasThirdParty = args.ThirdPartyId.HasValue && args.ThirdPartyId.Value.GetEAccountType() == EAccountType.k_EAccountTypeIndividual;

        args.Session?.MarkDirty();
        if (hasKiller)
            args.KillerSession?.MarkDirty();
        if (hasThirdParty)
            args.ThirdPartySession?.MarkDirty();

        DamageRecord record = new DamageRecord
        {
            Steam64 = e.Player.Steam64.m_SteamID,
            Instigator = hasKiller ? args.Instigator.m_SteamID : null,
            Cause = args.Cause,
            Damage = Math.Min(byte.MaxValue, Mathf.FloorToInt(e.Parameters.damage * e.Parameters.times)),
            Position = args.Point,
            InstigatorPosition = hasKiller ? args.KillerPoint : null,
            InstigatorSessionId = hasKiller ? args.KillerSession?.SessionId : null,
            SessionId = args.Session?.SessionId,
            Distance = args.KillDistance,
            IsInjure = e.IsInjure,
            IsSuicide = isSuicide,
            IsTeamkill = isTeamkill,
            Limb = args.Limb,
            IsInjured = injured && !e.IsInjure,
            Team = (byte)(args.DeadTeam?.Id ?? 0),
            Timestamp = DateTimeOffset.UtcNow,
            TimeDeployedSeconds = args.TimeDeployed,
            PrimaryAsset = args.PrimaryAsset == null ? default : new UnturnedAssetReference(args.PrimaryAsset),
            SecondaryAsset = args.SecondaryAsset == null ? default : new UnturnedAssetReference(args.SecondaryAsset),
            RelatedPlayer = hasThirdParty ? args.ThirdPartyId!.Value.m_SteamID : null,
            RelatedPlayerPosition = hasThirdParty ? args.ThirdPartyPoint : null,
            RelatedPlayerSessionId = hasThirdParty ? args.ThirdPartySession?.SessionId : null,
            Vehicle = new UnturnedAssetReference(args.ActiveVehicle?.asset)
        };

        if (e.IsDeath)
            e.Player.Data["KillShot"] = record;

        _buffer.Enqueue(record);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        PlayerDied args = _tempPlayerDiedArgs;

        bool hasKiller = !args.WasSuicide && args.Instigator.GetEAccountType() == EAccountType.k_EAccountTypeIndividual;
        bool hasThirdParty = args.ThirdPartyId.HasValue && args.ThirdPartyId.Value.GetEAccountType() == EAccountType.k_EAccountTypeIndividual;

        args.Session?.MarkDirty();
        if (hasKiller)
            args.KillerSession?.MarkDirty();
        if (hasThirdParty)
            args.ThirdPartySession?.MarkDirty();

        DeathRecord record = new DeathRecord
        {
            Steam64 = e.Player.Steam64.m_SteamID,
            Instigator = hasKiller ? args.Instigator.m_SteamID : null,
            DeathCause = args.Cause,
            DeathMessage = e.DefaultMessage ?? string.Empty,
            Position = args.Point,
            InstigatorPosition = hasKiller ? args.KillerPoint : null,
            InstigatorSessionId = hasKiller ? args.KillerSession?.SessionId : null,
            SessionId = args.Session?.SessionId,
            Distance = hasKiller ? args.KillDistance : null,
            IsSuicide = args.WasSuicide,
            IsTeamkill = args.WasTeamkill,
            IsBleedout = e.WasBleedout,
            Team = (byte)(args.DeadTeam?.Id ?? 0),
            Timestamp = DateTimeOffset.UtcNow,
            TimeDeployedSeconds = args.TimeDeployed,
            PrimaryAsset = args.PrimaryAsset == null ? default : new UnturnedAssetReference(args.PrimaryAsset),
            SecondaryAsset = args.SecondaryAsset == null ? default : new UnturnedAssetReference(args.SecondaryAsset),
            RelatedPlayer = hasThirdParty ? args.ThirdPartyId!.Value.m_SteamID : null,
            RelatedPlayerPosition = hasThirdParty ? args.ThirdPartyPoint : null,
            RelatedPlayerSessionId = hasThirdParty ? args.ThirdPartySession?.SessionId : null,
            Vehicle = new UnturnedAssetReference(args.ActiveVehicle?.asset)
        };

        if (e.Player.Data.TryRemove("KillShot", out object? obj) && obj is DamageRecord killShot)
        {
            record.KillShot = killShot;
        }

        _buffer.Enqueue(record);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    void IEventListener<FobRegistered>.HandleEvent(FobRegistered e, IServiceProvider serviceProvider)
    {
        if (e.Fob is not BunkerFob normalFob)
            return;

        WarfarePlayer? creator = _playerService.GetOnlinePlayerOrNull(normalFob.Creator);

        creator?.CurrentSession?.MarkDirty();

        FobRecord record = new FobRecord
        {
            Steam64 = normalFob.Creator.m_SteamID,
            FobName = normalFob.Name,
            FobType = FobType.BunkerFob,
            SessionId = creator?.CurrentSession?.SessionId,
            
            Position = normalFob.Position,
            FobAngle = normalFob.Buildable.Rotation.eulerAngles,
            Timestamp = DateTimeOffset.UtcNow,
            Team = (byte)normalFob.Team.Id,

            Items = new List<FobItemRecord>()
        };

        BuildableContainer.Get(normalFob.Buildable).AddComponent(new FobRecordBuildableComponent(normalFob.Buildable, record));

        _buffer.Enqueue(record);
    }

    [EventListener(MustRunInstantly = true, RequireActiveLayout = true)]
    void IEventListener<FobDestroyed>.HandleEvent(FobDestroyed e, IServiceProvider serviceProvider)
    {
        if (e.Fob is not BunkerFob normalFob)
            return;

        FobRecordBuildableComponent? fobRecordContainer = BuildableContainer.Get(normalFob.Buildable).ComponentOrNull<FobRecordBuildableComponent>();
        if (fobRecordContainer == null)
            return;

        bool hasInstigator = e.Event.InstigatorId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual;

        FobRecord record = fobRecordContainer.Record;
        
        if (hasInstigator)
        {
            record.Instigator = e.Event.InstigatorId.m_SteamID;

            record.InstigatorSessionId = e.Event.Instigator?.CurrentSession.SessionId;
            record.InstigatorPosition = e.Event.Instigator?.Position;

            e.Event.Instigator?.CurrentSession?.MarkDirty();
        }

        record.DestroyedAt = DateTimeOffset.UtcNow;
        record.DestroyedByRoundEnd = !hasInstigator && e.Event.WasSalvaged;
        record.PrimaryAsset = e.Event.PrimaryAsset != null ? new UnturnedAssetReference(e.Event.PrimaryAsset) : null;
        record.SecondaryAsset = e.Event.SecondaryAsset != null ? new UnturnedAssetReference(e.Event.SecondaryAsset) : null;
        record.Teamkilled = e.Event.InstigatorTeam.IsFriendly(normalFob.Team);

        _buffer.Enqueue(record);
    }

    private class FobRecordBuildableComponent : IBuildableComponent
    {
        public readonly FobRecord Record;

        public IBuildable Buildable { get; }

        public FobRecordBuildableComponent(IBuildable buildable, FobRecord record)
        {
            Buildable = buildable;
            Record = record;
        }

        void IDisposable.Dispose() { }
    }
}