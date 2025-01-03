using Microsoft.EntityFrameworkCore;
using System;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Stats.Records;
using Uncreated.Warfare.Players.Extensions;

namespace Uncreated.Warfare.Stats.EventHandlers;
internal sealed class PlayerDatabaseStatsEventHandlers : IEventListener<PlayerAided>, IEventListener<PlayerDamaged>, IEventListener<PlayerDied>
{
    private readonly DatabaseStatsBuffer _buffer;
    private readonly DeathTracker _deathTracker;

    public PlayerDatabaseStatsEventHandlers(DatabaseStatsBuffer buffer, DeathTracker deathTracker)
    {
        _buffer = buffer;
        _deathTracker = deathTracker;
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerAided>.HandleEvent(PlayerAided e, IServiceProvider serviceProvider)
    {
        bool hasInstigator = !e.Medic.Equals(e.Player);

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
            InstigatorSessionId = e.Medic.CurrentSession.SessionId,
            SessionId = e.Player.CurrentSession.SessionId,
            NearestLocation = F.GetClosestLocationName(e.Player.Position),
            Timestamp = DateTimeOffset.UtcNow
        };

        Interlocked.Increment(ref e.Player.CurrentSession.EventCount);

        Task.Run(async () =>
        {
            await _buffer.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                _buffer.DbContext.AidRecords.Add(record);
                _buffer.IsDirty = true;
            }
            finally
            {
                _buffer.Release();
            }
        });
    }

    private readonly PlayerDied _tempPlayerDiedArgs = new PlayerDied(new DamagePlayerParameters(null))
    {
        Player = null!
    };

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerDamaged>.HandleEvent(PlayerDamaged e, IServiceProvider serviceProvider)
    {
        bool injured = e.Player.IsInjured();
        
        // dont count injure ticks
        if (injured && e.Parameters.cause == EDeathCause.BLEEDING)
            return;

        PlayerDied args = _tempPlayerDiedArgs;

        _deathTracker.FillArgs(e.Player, e.Parameters.cause, e.Parameters.limb, e.Parameters.killer, args);

        bool isSuicide = args.WasSuicide;
        bool isTeamkill = args.WasTeamkill;

        bool hasKiller = !isSuicide && args.Instigator.GetEAccountType() == EAccountType.k_EAccountTypeIndividual;
        bool hasThirdParty = args.ThirdPartyId.HasValue && args.ThirdPartyId.Value.GetEAccountType() == EAccountType.k_EAccountTypeIndividual;

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
            IsInjure = injured && e.IsInjure,
            IsSuicide = isSuicide,
            IsTeamkill = isTeamkill,
            Limb = args.Limb,
            NearestLocation = F.GetClosestLocationName(args.Point),
            IsInjured = injured && !e.IsInjure,
            Team = (byte)args.DeadTeam.Id,
            Timestamp = DateTimeOffset.UtcNow,
            TimeDeployedSeconds = args.TimeDeployed,
            PrimaryAsset = args.PrimaryAsset == null ? default : new UnturnedAssetReference(args.PrimaryAsset),
            SecondaryAsset = args.SecondaryAsset == null ? default : new UnturnedAssetReference(args.SecondaryAsset),
            RelatedPlayer = hasThirdParty ? args.ThirdPartyId!.Value.m_SteamID : null,
            RelatedPlayerPosition = hasThirdParty ? args.ThirdPartyPoint : null,
            RelatedPlayerSessionId = hasThirdParty ? args.ThirdPartySession?.SessionId : null,
            Vehicle = new UnturnedAssetReference(args.ActiveVehicle?.asset)
        };

        e.Player.Data["KillShot"] = record;

        Task.Run(async () =>
        {
            await _buffer.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                _buffer.DbContext.DamageRecords.Add(record);
                _buffer.IsDirty = true;
            }
            finally
            {
                _buffer.Release();
            }
        });
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        PlayerDied args = _tempPlayerDiedArgs;

        bool hasKiller = !args.WasSuicide && args.Instigator.GetEAccountType() == EAccountType.k_EAccountTypeIndividual;
        bool hasThirdParty = args.ThirdPartyId.HasValue && args.ThirdPartyId.Value.GetEAccountType() == EAccountType.k_EAccountTypeIndividual;

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
            NearestLocation = F.GetClosestLocationName(args.Point),
            Team = (byte)args.DeadTeam.Id,
            Timestamp = DateTimeOffset.UtcNow,
            TimeDeployedSeconds = args.TimeDeployed,
            PrimaryAsset = args.PrimaryAsset == null ? default : new UnturnedAssetReference(args.PrimaryAsset),
            SecondaryAsset = args.SecondaryAsset == null ? default : new UnturnedAssetReference(args.SecondaryAsset),
            RelatedPlayer = hasThirdParty ? args.ThirdPartyId!.Value.m_SteamID : null,
            RelatedPlayerPosition = hasThirdParty ? args.ThirdPartyPoint : null,
            RelatedPlayerSessionId = hasThirdParty ? args.ThirdPartySession?.SessionId : null,
            Vehicle = new UnturnedAssetReference(args.ActiveVehicle?.asset)
        };

        DamageRecord? killShot = null;
        if (e.Player.Data.TryGetValue("KillShot", out object? obj))
        {
            killShot = obj as DamageRecord;
            e.Player.Data.Remove("KillShot");
        }

        Task.Run(async () =>
        {
            await _buffer.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                if (killShot != null)
                {
                    if (killShot.Id is 0 or ulong.MaxValue)
                        record.KillShot = killShot;
                    else
                        record.KillShotId = killShot.Id;
                }

                _buffer.DbContext.DeathRecords.Add(record);
                _buffer.IsDirty = true;
            }
            finally
            {
                _buffer.Release();
            }
        });
    }
}
