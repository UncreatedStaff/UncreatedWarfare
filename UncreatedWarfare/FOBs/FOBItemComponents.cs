using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs;
#if false
public class RadioComponent : MonoBehaviour, IManualOnDestroy, IFOBItem, IShovelable, ISalvageListener
{
    private bool _destroyed;
#pragma warning disable CS0067 // (event not used even though it's part of an interface)
    public event Action<Action<FobItemRecord>>? UpdateRecord;
#pragma warning restore CS0067
#nullable disable
    public FOB FOB { get; set; }
#nullable restore
    public RadioState State { get; private set; }
    public BuildableType Type => BuildableType.Radio;
    public BarricadeDrop Barricade { get; private set; }
    public BuildableData? Buildable => null;
    public ulong Owner { get; private set; }
    public IAssetLink<EffectAsset>? Icon { get; private set; }
    public float IconOffset => 3.5f;
    public ulong Team { get; private set; }
    public bool IsSalvaged { get; set; }
    public CSteamID Salvager { get; set; }
    public bool Destroyed => _destroyed;
    public TickResponsibilityCollection Builders { get; } = new TickResponsibilityCollection();
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;
    public bool NeedsRestock { get; internal set; }
    public float LastRestock { get; internal set; }
    public ulong RecordId { get; set; }

    [UsedImplicitly]
    private void Awake()
    {
        Barricade = BarricadeManager.FindBarricadeByRootTransform(transform);
        if (Barricade == null)
        {
            L.LogDebug($"[FOBS] RadioComponent added to unknown barricade: {name}.");
            goto destroy;
        }
        
        if (!GamemodeOld.Config.FOBRadios.ContainsGuid(Barricade.asset.GUID))
        {
            if (GamemodeOld.Config.BarricadeFOBRadioDamaged.MatchAsset(Barricade.asset))
            {
                State = RadioState.Bleeding;
                Icon = GamemodeOld.Config.EffectMarkerRadioDamaged;
            }
            else
            {
                L.LogDebug($"[FOBS] RadioComponent unable to find a valid buildable: {Buildable?.Foundation?.GetAsset()?.itemName}.");
                goto destroy;
            }
        }
        else
        {
            State = RadioState.Alive;
            Icon = GamemodeOld.Config.EffectMarkerRadio;
        }

        Owner = Barricade.GetServersideData().owner;
        Team = Barricade.GetServersideData().group.GetTeam();

        UCPlayer? owner = UCPlayer.FromID(Owner);

        Builders.Set(Owner, FOBManager.Config.BaseFOBRepairHits, owner?.CurrentSession?.SessionId ?? 0);
        
        if (Barricade.interactable is InteractableStorage storage)
        {
            storage.despawnWhenDestroyed = true;
            storage.items.onStateUpdated += InvalidateRestock;
        }

        return;
        destroy:
        State = RadioState.Destroyed;
        Destroy(this);
    }

    internal void InvalidateRestock()
    {
        if (!NeedsRestock)
            LastRestock = Time.realtimeSinceStartup - 40f;
        NeedsRestock = true;
    }

    void ISalvageListener.OnSalvageRequested(SalvageRequested e)
    {
        if (e.Player.OnDuty())
            return;
        
        L.Log($"[FOBS] [{FOB?.Name ?? "FLOATING"}] {e.Player} tried to salvage the radio.");
        e.Player.SendChat(T.WhitelistProhibitedSalvage, Barricade.asset);
        e.Cancel();
    }

    [UsedImplicitly]
    private void Start()
    {
        FOB?.Restock();
        L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Radio Initialized: {Barricade.asset.itemName}. (State: {State}).");
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        if (!_destroyed && Barricade != null && Barricade.model != null && !Barricade.GetServersideData().barricade.isDead &&
            BarricadeManager.tryGetRegion(Barricade.model, out byte x, out byte y, out ushort plant, out _))
        {
            BarricadeManager.destroyBarricade(Barricade, x, y, plant);
            _destroyed = true;
            Barricade = null!;
        }

        FOBManager.EnsureDisposed(this);

        State = RadioState.Destroyed;
    }

    void IManualOnDestroy.ManualOnDestroy()
    {
        if (Barricade is { interactable: InteractableStorage { items: { } } storage })
            storage.items.onStateUpdated -= InvalidateRestock;
        _destroyed = true;
        Destroy(this);
    }

    public enum RadioState
    {
        Alive,
        Bleeding,
        Destroyed
    }

    public bool Shovel(UCPlayer shoveler, Vector3 point)
    {
        if (shoveler.GetTeam() != Team) return false;
        if (State is RadioState.Bleeding or RadioState.Alive)
        {
            ushort maxHealth = Barricade.asset.health;
            float amt = maxHealth / FOBManager.Config.BaseFOBRepairHits * FOBManager.GetBuildIncrementMultiplier(shoveler);
            if (Barricade.GetServersideData().barricade.health + amt > maxHealth)
                amt = Barricade.asset.health - Barricade.GetServersideData().barricade.health;
            if (amt == 0)
                return true;
            BarricadeManager.repair(Barricade.model, amt, 1, shoveler.CSteamID);
            FOBManager.TriggerBuildEffect(point);
            Builders.Increment(shoveler.Steam64, amt, shoveler.CurrentSession?.SessionId ?? 0);
            UpdateHitsUI();

            if (State == RadioState.Bleeding && Barricade.GetServersideData().barricade.health >= maxHealth)
                FOB?.UpdateRadioState(RadioState.Alive);

            return true;
        }

        return false;
    }
    public void QuickShovel(UCPlayer shoveler)
    {
        if (State is RadioState.Bleeding or RadioState.Alive)
        {
            ushort maxHealth = Barricade.asset.health;
            float amt = maxHealth - Barricade.GetServersideData().barricade.health;
            BarricadeManager.repair(Barricade.model, amt, 1, shoveler.CSteamID);
            FOBManager.TriggerBuildEffect(transform.position);
            Builders.Increment(shoveler.Steam64, amt, shoveler.CurrentSession?.SessionId ?? 0);
            UpdateHitsUI();

            if (State == RadioState.Bleeding)
                FOB?.UpdateRadioState(RadioState.Alive);
        }
    }
    private void UpdateHitsUI()
    {
        Builders.RetrieveLock();
        try
        {
            float time = Time.realtimeSinceStartup;
            ToastMessage msg = new ToastMessage(ToastMessageStyle.ProgressBar, Points.GetProgressBar(Barricade.GetServersideData().barricade.health, Barricade.asset.health, 25).Colorize("ff9966"));
            foreach (TickResponsibility responsibility in Builders.GetGroupedEnumerator())
            {
                if (time - responsibility.LastUpdated < 5f)
                {
                    if (UCPlayer.FromID(responsibility.Steam64) is { } pl)
                        pl.Toasts.Queue(in msg);
                }
            }
        }
        finally
        {
            Builders.ReturnLock();
        }
    }
}

public class ShovelableComponent : MonoBehaviour, IManualOnDestroy, IFOBItem, IShovelable, ISalvageListener, IDestroyInfo
{
    private bool _destroyed;
    private int _buildRemoved;
    private float _progressToBuild;
    private float _progressToRepair;
    private int _repairBuildRemoved;
    private double _useTimeSeconds; // todo implement this
#pragma warning disable CS0067 // (event not used even though it's part of an interface)
    public event Action<Action<FobItemRecord>>? UpdateRecord;
#pragma warning restore CS0067
    public FOB? FOB { get; set; }
    public BuildableType Type { get; private set; }
    public BuildableState State { get; private set; }
    public BuildableData Buildable { get; private set; }
    public IBuildable? ActiveStructure { get; private set; }
    public InteractableVehicle? ActiveVehicle { get; private set; }
    public IBuildable? Base { get; private set; }
    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; }
    public ulong Team { get; private set; }
    public ulong Owner { get; private set; }
    public float Progress { get; private set; }
    public float Total { get; private set; }
    public bool IsSalvaged { get; set; }
    public CSteamID Salvager { get; set; }
    public IBuildableDestroyedEvent? DestroyInfo { get; set; }
    public bool IsFloating { get; private set; }
    public IAssetLink<EffectAsset>? Icon { get; protected set; }
    public float IconOffset { get; protected set; }
    public TickResponsibilityCollection Builders { get; } = new TickResponsibilityCollection();
    public Asset Asset { get; protected set; }
    public ulong RecordId { get; set; }
    public string ClosestLocation { get; set; }

    [UsedImplicitly]
    private void Awake()
    {
        Transform model = transform;
        BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(model);
        if (barricade == null)
        {
            StructureDrop structure = StructureManager.FindStructureByRootTransform(model);
            if (structure == null)
            {
                InteractableVehicle vehicle = DamageTool.getVehicle(model);
                if (vehicle == null)
                {
                    L.LogWarning($"[FOBS] ShovelableComponent not added to barricade, structure, or vehicle: {name}.");
                    goto destroy;
                }

                ActiveVehicle = vehicle;
                ActiveStructure = null;
                Asset = vehicle.asset;
                Position = vehicle.transform.position;
                Rotation = vehicle.transform.rotation;
                Team = vehicle.lockedGroup.m_SteamID.GetTeam();
                Owner = vehicle.lockedOwner.m_SteamID;
            }
            else
            {
                ActiveStructure = new BuildableStructure(structure);
                Asset = structure.asset;
                Position = structure.model.position;
                Rotation = structure.model.rotation;
                Team = structure.GetServersideData().group.GetTeam();
                Owner = structure.GetServersideData().owner;
            }
        }
        else
        {
            ActiveStructure = new BuildableBarricade(barricade);
            Asset = barricade.asset;
            Position = barricade.model.position;
            Rotation = barricade.model.rotation;
            Team = barricade.GetServersideData().group.GetTeam();
            Owner = barricade.GetServersideData().owner;
        }

        Progress = 0f;

        Buildable = FOBManager.FindBuildable(Asset)!;
        Type = Buildable.Type;
        if (Buildable is not { Type: not BuildableType.Radio })
        {
            L.LogWarning($"[FOBS] ShovelableComponent unable to find a valid buildable: " +
                         $"{Buildable?.Foundation?.GetAsset()?.itemName} ({Asset.FriendlyName}).");
            goto destroy;
        }

        IconOffset = Buildable.Type switch
        {
            BuildableType.Bunker => 5.5f,
            BuildableType.AmmoCrate => 1.75f,
            BuildableType.RepairStation => 4.5f,
            _ => default
        };
        if (ActiveStructure != null && Buildable.Foundation.MatchGuid(ActiveStructure.Asset.GUID))
        {
            Total = Buildable.RequiredHits;
            State = BuildableState.Foundation;
            if (Buildable.RequiredHits > 8)
                Icon = GamemodeOld.Config.EffectMarkerBuildable;
            if (IconOffset == 0)
                IconOffset = 1.5f;
        }
        else
        {
            State = BuildableState.Full;
            Icon = Buildable.Type switch
            {
                BuildableType.Bunker => GamemodeOld.Config.EffectMarkerBunker,
                BuildableType.AmmoCrate => GamemodeOld.Config.EffectMarkerAmmo,
                BuildableType.RepairStation => GamemodeOld.Config.EffectMarkerRepair,
                _ => default
            };
        }

        InitAwake();
        if (State == BuildableState.Foundation && TeamManager.IsInMain(Team, Position) && UCPlayer.FromID(Owner) is { IsOnline: true } pl)
        {
            TimeUtility.InvokeAfterDelay(() =>
            {
                if (pl.IsOnline)
                    QuickShovel(pl);
            }, 1.25f);
        }

        _useTimeSeconds = 0;

        return;
        destroy:
        Destroy(this);
    }

    [UsedImplicitly]
    private void Start()
    {
        IsFloating = FOB == null;
        ClosestLocation = !IsFloating ? FOB!.ClosestLocation : F.GetClosestLocationName(transform.position, true, true);
        InitStart();
        L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] {Asset.FriendlyName} Initialized: {Buildable} in state: {State}.");
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        Destroy();
        if (!_destroyed)
        {
            if (ActiveStructure != null)
            {
                if (ActiveStructure.Destroy())
                {
                    _destroyed = true;
                    ActiveStructure = null!;
                }
            }
            else if (ActiveVehicle != null)
            {
                for (int i = 0; i < ActiveVehicle.turrets.Length; ++i)
                {
                    byte[] state = ActiveVehicle.turrets[i].state;
                    if (state.Length != 18)
                        continue;
                    Attachments.parseFromItemState(state, out _, out _, out _, out _, out ushort mag);
                    byte amt = state[10];
                    if (mag != 0 && Assets.find(EAssetType.ITEM, mag) is ItemMagazineAsset asset)
                        ItemManager.dropItem(new Item(asset.id, amt, 100), ActiveVehicle.transform.position, true, false, true);
                }
                VehicleBarricadeRegion region = BarricadeManager.findRegionFromVehicle(ActiveVehicle);
                if (region != null)
                {
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        if (region.drops[i].interactable is InteractableStorage st)
                            st.despawnWhenDestroyed = true;
                    }
                }
#if false // explode the vehicle instead of destroying it
                if (!ActiveVehicle.isExploded)
                    VehicleManager.sendVehicleExploded(ActiveVehicle);
#else
                VehicleManager.askVehicleDestroy(ActiveVehicle);
#endif
                ActiveVehicle = null;
                _destroyed = true;
            }
        }
        if (Base != null && Base.Destroy())
        {
            _destroyed = true;
            ActiveStructure = null!;
        }

        bool destroyedByRoundEnd = Data.Gamemode == null || Data.Gamemode.State is not Gamemodes.State.Active and not Gamemodes.State.Staging;
        bool teamkilled = !destroyedByRoundEnd && DestroyInfo != null && DestroyInfo.Instigator != null && DestroyInfo.Buildable.Group.GetTeam() == DestroyInfo.Instigator.GetTeam();

        if (FOB?.Record != null && Buildable != null && State == BuildableState.Full && !destroyedByRoundEnd)
        {
            Task.Run(async () =>
            {
                try
                {
                    await FOB.Record.Update(record =>
                    {
                        if (Buildable.Emplacement != null)
                            ++record.EmplacementsDestroyed;
                        else if (Buildable.Type == BuildableType.AmmoCrate)
                            ++record.AmmoCratesDestroyed;
                        else if (Buildable.Type == BuildableType.RepairStation)
                            ++record.RepairStationsDestroyed;
                        else if (Buildable.Type == BuildableType.Fortification)
                            ++record.FortificationsDestroyed;
                        else if (Buildable.Type == BuildableType.Bunker)
                            ++record.BunkersDestroyed;
                    });
                }
                catch (Exception ex)
                {
                    L.LogError($"[FOBS] [{FOB.Name}] Failed to update FOB record tracker after buildable destroyed.");
                    L.LogError(ex);
                }
            });
        }
        if (FOB?.Record != null)
        {
            UCPlayer? instigator = DestroyInfo != null ? UCPlayer.FromID(DestroyInfo.InstigatorId.m_SteamID) : null;

            SessionRecord? session = instigator?.CurrentSession;

            Task.Run(async () =>
            {
                try
                {
                    await FOB.Record.Update(this, record =>
                    {
                        record.DestroyedAt = DateTime.UtcNow;
                        record.DestroyedByRoundEnd = destroyedByRoundEnd;
                        record.Teamkilled = teamkilled;
                        record.Instigator = instigator?.Steam64;
                        record.InstigatorPosition = instigator?.Position;
                        record.InstigatorSessionId = session?.SessionId;
                        record.PrimaryAsset = UnturnedAssetReference.FromAssetLink(DestroyInfo?.PrimaryAsset);
                        record.SecondaryAsset = UnturnedAssetReference.FromAssetLink(DestroyInfo?.SecondaryAsset);
                        record.UseTimeSeconds = _useTimeSeconds;
                    });
                }
                catch (Exception ex)
                {
                    L.LogError($"[FOBS] [{FOB.Name}] Failed to update FOB record tracker after buildable destroyed.");
                    L.LogError(ex);
                }
            });
        }

        FOBManager.EnsureDisposed(this);
        L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Destroyed: {Buildable} ({Asset.FriendlyName}).");
    }
    void IManualOnDestroy.ManualOnDestroy()
    {
        _destroyed = true;
        Destroy(this);
    }
    void ISalvageListener.OnSalvageRequested(SalvageRequested e)
    {
        if (State != BuildableState.Foundation)
        {
            if (e.Player.OnDuty())
                return;

            L.Log($"[FOBS] [{FOB?.Name ?? "FLOATING"}] {e.Player} tried to salvage {Buildable}.");
            e.Player.SendChat(T.WhitelistProhibitedSalvage, ActiveStructure?.Asset ?? Buildable.Foundation.GetAsset()!);
            e.Cancel();
        }
        else if (_buildRemoved > 0 && FOB != null)
        {
            int refund = Mathf.CeilToInt(_buildRemoved * (FOBManager.Config.SalvageRefundPercentage / 100f));
            FOBManager.ShowResourceToast(new LanguageSet(e.Player), build: refund);
            FOB.ModifyBuild(refund);
            _buildRemoved = 0;
        }
    }
    protected virtual void InitAwake() { }
    protected virtual void InitStart() { }
    protected virtual void Destroy() { }

    public bool Shovel(UCPlayer shoveler, Vector3 point)
    {
        if (State is BuildableState.Foundation or BuildableState.Full && shoveler.GetTeam() == Team)
        {
            if (FOB == null && !IsFloating)
            {
                shoveler.SendChat(T.BuildTickNotInRadius);
                return true;
            }
            if (!IsFloating && !FOB!.ValidatePlacement(Buildable, shoveler, this) ||
                IsFloating && Data.Is(out IFOBs? fobs) && !fobs.FOBManager.ValidateFloatingPlacement(Buildable, shoveler, transform.position, this))
            {
                return false;
            }

            float amount = FOBManager.GetBuildIncrementMultiplier(shoveler);
            float build;
            if (State == BuildableState.Foundation)
            {
                Progress += amount;

                Builders.Increment(shoveler.Steam64, amount, shoveler.CurrentSession?.SessionId ?? 0);


                if (FOB != null)
                {
                    build = Buildable.RequiredBuild / (float)Buildable.RequiredHits * amount;
                    _progressToBuild += build;
                    int build2 = Mathf.FloorToInt(_progressToBuild);
                    if (FOB.BuildSupply < _progressToBuild && Mathf.Abs(_progressToBuild - FOB.BuildSupply) >= 0.05)
                    {
                        shoveler.SendChat(T.BuildMissingSupplies, FOB.BuildSupply, Buildable.RequiredBuild - _buildRemoved);
                        return true;
                    }
                    if (build2 > 0)
                    {
                        _progressToBuild -= build2;
                        _buildRemoved += build2;
                        SendBuildToastToBuilders(-build2);
                        FOB.ModifyBuild(-build2);
                    }
                }

                FOBManager.TriggerBuildEffect(point);

                UpdateHitsUI();
                if (Progress >= Total || Mathf.Abs(Progress - Total) < 0.05)
                {
                    int buildRemaining = Buildable.RequiredBuild - _buildRemoved;
                    _progressToBuild = 0;
                    if (FOB != null && buildRemaining > 0)
                    {
                        if (FOB.BuildSupply < buildRemaining)
                        {
                            shoveler.SendChat(T.BuildMissingSupplies, FOB.BuildSupply, Buildable.RequiredBuild - _buildRemoved);
                            return true;
                        }
                        
                        SendBuildToastToBuilders(-buildRemaining);
                        FOB.ModifyBuild(-buildRemaining);
                    }
                    Build();
                    Progress = Total;
                }

                return true;
            }
            
            ushort maxHealth;
            ushort health;
            if (ActiveStructure == null || ActiveStructure.Drop is not BarricadeDrop and not StructureDrop)
            {
                if (ActiveVehicle == null || ActiveVehicle.isDead)
                    return false;

                health = ActiveVehicle.health;
                maxHealth = ActiveVehicle.asset.health;
            }
            else
            {
                maxHealth = ActiveStructure?.Drop switch
                {
                    BarricadeDrop barricade => barricade.asset.health,
                    StructureDrop structure => structure.asset.health,
                    _ => 0
                };
                health = ActiveStructure!.Drop switch
                {
                    BarricadeDrop barricade => barricade.GetServersideData().barricade.health,
                    _ => ((StructureDrop)ActiveStructure.Drop).GetServersideData().structure.health,
                };
            }

            float amt = maxHealth / (float)Buildable.RequiredHits * FOBManager.GetBuildIncrementMultiplier(shoveler);
            if (health + amt > maxHealth)
                amt = maxHealth - health;
            if (amt <= 0)
                return false;

            build = Buildable.RequiredBuild * (amt / maxHealth) * (1f - FOBManager.Config.RepairBuildDiscountPercentage / 100f);
            float hits = (float)Buildable.RequiredHits / maxHealth * amt;
            _progressToRepair += build;
            int floor = Mathf.FloorToInt(_progressToRepair);
            int dif = floor - _repairBuildRemoved;
            bool chargedFob = false;
            if (dif > 0)
            {
                _repairBuildRemoved = floor;
                if (FOB != null)
                {
                    if (FOB.BuildSupply < dif)
                    {
                        shoveler.SendChat(T.BuildMissingSupplies, FOB.BuildSupply, dif);
                        return true;
                    }

                    Builders.Increment(shoveler.Steam64, hits, shoveler.CurrentSession?.SessionId ?? 0);
                    chargedFob = true;

                    SendBuildToastToBuilders(-dif);
                    FOB.ModifyBuild(-dif);
                }
            }
            else if (FOB != null && FOB.BuildSupply <= 0)
            {
                shoveler.SendChat(T.BuildMissingSupplies, 0, dif);
                return true;
            }

            if (!chargedFob)
            {
                Builders.Increment(shoveler.Steam64, hits, shoveler.CurrentSession?.SessionId ?? 0);
            }

            if (ActiveStructure != null)
            {
                if (!ActiveStructure.IsStructure)
                    BarricadeManager.repair(ActiveStructure!.Model, amt, 1, shoveler.CSteamID);
                else
                    StructureManager.repair(ActiveStructure!.Model, amt, 1, shoveler.CSteamID);
            }
            else
                VehicleManager.repair(ActiveVehicle!, amt, 1, shoveler.CSteamID);

            ushort newHealth;
            if (ActiveStructure == null || ActiveStructure.Drop is not BarricadeDrop and not StructureDrop)
            {
                newHealth = ActiveVehicle!.health;
            }
            else
            {
                newHealth = ActiveStructure!.Drop switch
                {
                    BarricadeDrop barricade => barricade.GetServersideData().barricade.health,
                    _ => ((StructureDrop)ActiveStructure.Drop).GetServersideData().structure.health,
                };
            }

            FOBManager.TriggerBuildEffect(point);
            Builders.Increment(shoveler.Steam64, amt, shoveler.CurrentSession?.SessionId ?? 0);
            UpdateRepairUI(newHealth, maxHealth);

            if (Base != null)
            {
                float percent = (float)newHealth / maxHealth;
                if (percent > 0.9f)
                    percent = 1f;
                if (!Base.IsStructure)
                {
                    maxHealth = (ushort)Mathf.Clamp(Mathf.RoundToInt(percent * ((ItemBarricadeAsset)Base.Asset).health), 0, ushort.MaxValue);
                    health = ((BarricadeDrop)Base.Drop).GetServersideData().barricade.health;
                    if (maxHealth > health)
                        BarricadeManager.repair(Base!.Model, maxHealth - health, 1, shoveler.CSteamID);
                }
                else
                {
                    maxHealth = (ushort)Mathf.Clamp(Mathf.RoundToInt(percent * ((ItemBarricadeAsset)Base.Asset).health), 0, ushort.MaxValue);
                    health = ((StructureDrop)Base.Drop).GetServersideData().structure.health;
                    if (maxHealth > health)
                        StructureManager.repair(Base!.Model, maxHealth - health, 1, shoveler.CSteamID);
                }
            }

            return true;
        }

        return false;
    }
    public void QuickShovel(UCPlayer shoveler)
    {
        if (State == BuildableState.Foundation)
        {
            float amount = Total - Progress;
            L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Incrementing build: {shoveler} ({Progress} + {amount} = {Progress + amount} / {Total}).");
            Progress += amount;

            FOBManager.TriggerBuildEffect(transform.position);

            Builders.Increment(shoveler.Steam64, amount, shoveler.CurrentSession?.SessionId ?? 0);
            UpdateHitsUI();

            if (Progress >= Total)
                Build();
        }
        else if (State != BuildableState.Destroyed)
        {
            FOBManager.TriggerBuildEffect(transform.position);

            ushort maxHealth;
            ushort health;
            if (ActiveStructure == null || ActiveStructure.Drop is not BarricadeDrop and not StructureDrop)
            {
                if (ActiveVehicle == null)
                    return;

                health = ActiveVehicle.health;
                maxHealth = ActiveVehicle.asset.health;
            }
            else
            {
                maxHealth = ActiveStructure?.Drop switch
                {
                    BarricadeDrop barricade => barricade.asset.health,
                    StructureDrop structure => structure.asset.health,
                    _ => 0
                };
                health = ActiveStructure!.Drop switch
                {
                    BarricadeDrop barricade => barricade.GetServersideData().barricade.health,
                    _ => ((StructureDrop)ActiveStructure.Drop).GetServersideData().structure.health,
                };
            }

            float amt = maxHealth - health;
            if (health + amt > maxHealth)
                amt = maxHealth - health;
            if (amt <= 0)
                return;

            if (ActiveStructure != null)
            {
                if (!ActiveStructure.IsStructure)
                    BarricadeManager.repair(ActiveStructure!.Model, amt, 1, shoveler.CSteamID);
                else
                    StructureManager.repair(ActiveStructure!.Model, amt, 1, shoveler.CSteamID);
            }
            else
                VehicleManager.repair(ActiveVehicle!, amt, 1, shoveler.CSteamID);

            UpdateRepairUI(1, 1);
            Builders.Increment(shoveler.Steam64, amt, shoveler.CurrentSession?.SessionId ?? 0);
            FOBManager.TriggerBuildEffect(transform.position);
            if (Base != null)
            {
                if (!Base.IsStructure)
                {
                    maxHealth = ((ItemBarricadeAsset)Base.Asset).health;
                    health = ((BarricadeDrop)Base.Drop).GetServersideData().barricade.health;
                    if (maxHealth > health)
                        BarricadeManager.repair(Base!.Model, maxHealth - health, 1, shoveler.CSteamID);
                }
                else
                {
                    maxHealth = ((ItemBarricadeAsset)Base.Asset).health;
                    health = ((BarricadeDrop)Base.Drop).GetServersideData().barricade.health;
                    if (maxHealth > health)
                        StructureManager.repair(Base!.Model, maxHealth - health, 1, shoveler.CSteamID);
                }
            }
        }
    }

    private void UpdateHitsUI()
    {
        Builders.RetrieveLock();
        try
        {
            float time = Time.realtimeSinceStartup;
            ToastMessage msg = new ToastMessage(ToastMessageStyle.ProgressBar, Points.GetProgressBar(Progress, Total, 25));
            foreach (TickResponsibility responsibility in Builders.GetGroupedEnumerator())
            {
                if (time - responsibility.LastUpdated < 5f && UCPlayer.FromID(responsibility.Steam64) is { } pl)
                    pl.Toasts.Queue(in msg);
            }
        }
        finally
        {
            Builders.ReturnLock();
        }
    }
    private void UpdateRepairUI(ushort health, ushort maxHealth)
    {
        Builders.RetrieveLock();
        try
        {
            float time = Time.realtimeSinceStartup;
            ToastMessage msg = new ToastMessage(ToastMessageStyle.ProgressBar, Points.GetProgressBar(health, maxHealth, 25).Colorize("ff9966"));
            foreach (TickResponsibility responsibility in Builders.GetGroupedEnumerator())
            {
                if (time - responsibility.LastUpdated < 5f && UCPlayer.FromID(responsibility.Steam64) is { } pl)
                    pl.Toasts.Queue(in msg);
            }
        }
        finally
        {
            Builders.ReturnLock();
        }
    }
    private void SendBuildToastToBuilders(int delta)
    {
        if (delta == 0) return;
        Builders.RetrieveLock();
        try
        {
            float time = Time.realtimeSinceStartup;
            foreach (TickResponsibility responsibility in Builders.GetGroupedEnumerator())
            {
                if (time - responsibility.LastUpdated < 5f && UCPlayer.FromID(responsibility.Steam64) is { } pl)
                    FOBManager.ShowResourceToast(new LanguageSet(pl), build: delta);
            }
        }
        finally
        {
            Builders.ReturnLock();
        }
    }

    public bool Build()
    {
        if (State != BuildableState.Foundation)
            return false;
        IBuildable? newBase = null;
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        ulong group = TeamManager.GetGroupID(Team);
        // base
        if (Buildable.Emplacement != null && Buildable.Emplacement.BaseBarricade.TryGetAsset(out ItemAsset? @base))
        {
            if (@base is ItemBarricadeAsset bAsset)
            {
                FOBManager.IgnorePlacingBarricade = true;
                try
                {
                    Barricade b = new Barricade(bAsset, bAsset.health, bAsset.getState());
                    Transform? t = BarricadeManager.dropNonPlantedBarricade(b, position, rotation, Owner, group);
                    BarricadeDrop? drop = t == null ? null : BarricadeManager.FindBarricadeByRootTransform(t);
                    if (drop != null)
                        newBase = new BuildableBarricade(drop);
                }
                finally
                {
                    FOBManager.IgnorePlacingBarricade = false;
                }
            }
            else if (@base is ItemStructureAsset sAsset)
            {
                FOBManager.IgnorePlacingStructure = true;
                try
                {
                    Structure s = new Structure(sAsset, sAsset.health);
                    bool success = StructureManager.dropReplicatedStructure(s, position, rotation, Owner, group);
                    if (success)
                    {
                        if (Regions.tryGetCoordinate(position, out byte x, out byte y) && StructureManager.tryGetRegion(x, y, out StructureRegion region))
                            newBase = new BuildableStructure(region.drops.GetTail());
                    }
                }
                finally
                {
                    FOBManager.IgnorePlacingStructure = false;
                }
            }


            if (newBase == null)
                L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to place base: {@base.itemName}.");
            else
                L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Placed base: {@base.itemName}.");
        }

        Transform? newTransform = null;

        // emplacement
        if (Buildable.Emplacement != null && Buildable.Emplacement.EmplacementVehicle.TryGetAsset(out VehicleAsset? vehicle))
        {
            InteractableVehicle veh = FOBManager.SpawnEmplacement(vehicle, position, rotation, Owner, group);

            if (veh == null)
                L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to spawn vehicle: {vehicle.vehicleName}.");
            else
            {
                newTransform = veh.transform;
                L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Spawned vehicle: {vehicle.vehicleName}.");
            }
        }

        // fortification
        if (newTransform == null && Buildable.FullBuildable.TryGetAsset(out ItemAsset? buildable))
        {
            if (buildable is ItemBarricadeAsset bAsset)
            {
                FOBManager.IgnorePlacingBarricade = true;
                try
                {
                    Barricade b = new Barricade(bAsset, bAsset.health, bAsset.getState());
                    Transform? t = BarricadeManager.dropNonPlantedBarricade(b, position, rotation, Owner, group);
                    BarricadeDrop? drop = t == null ? null : BarricadeManager.FindBarricadeByRootTransform(t);
                    if (drop != null)
                        newTransform = drop.model;
                }
                finally
                {
                    FOBManager.IgnorePlacingBarricade = false;
                }
            }
            else if (buildable is ItemStructureAsset sAsset)
            {
                FOBManager.IgnorePlacingStructure = true;
                try
                {
                    Structure s = new Structure(sAsset, sAsset.health);
                    bool success = StructureManager.dropReplicatedStructure(s, position, rotation, Owner, group);
                    if (success)
                    {
                        if (Regions.tryGetCoordinate(position, out byte x, out byte y) && StructureManager.tryGetRegion(x, y, out StructureRegion region))
                            newTransform = region.drops.GetTail().model;
                    }
                }
                finally
                {
                    FOBManager.IgnorePlacingStructure = false;
                }
            }

            if (newTransform == null)
                L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to place buildable: {buildable.itemName}.");
            else
                L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Placed buildable: {buildable.itemName}.");
        }

        if (newTransform == null)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Parent for buildable upgrade not spawned: {Buildable}.");
            if (newBase != null)
                newBase.Destroy();
            return false;
        }

        IFOBItem? @new = null;
        if (FOB != null)
            @new = FOB.UpgradeItem(this, newTransform);
        else if (Data.Is(out IFOBs? fobs))
            @new = fobs.FOBManager.UpgradeFloatingItem(this, newTransform);
        
        List<FobItemBuilderRecord> records = new List<FobItemBuilderRecord>(Builders.Count);

        DateTime now = DateTime.UtcNow;
        Builders.RetrieveLock();
        try
        {
            if (RecordId != 0)
            {
                foreach (TickResponsibility responsibility in Builders)
                {
                    UCPlayer? player = UCPlayer.FromID(responsibility.Steam64);

                    records.Add(new FobItemBuilderRecord
                    {
                        Steam64 = responsibility.Steam64,
                        Team = (byte)Team,
                        SessionId = responsibility.SessionId == 0 ? null : responsibility.SessionId,
                        FobItemId = RecordId,
                        Hits = responsibility.Ticks,
                        Responsibility = responsibility.Ticks / Builders.Ticks,
                        NearestLocation = ClosestLocation ?? F.GetClosestLocationName(transform.position, true, true),
                        Position = player?.Position ?? Vector3.zero,
                        Timestamp = now - TimeSpan.FromSeconds(Time.realtimeSinceStartup - responsibility.LastUpdated)
                    });
                }
            }

            foreach (TickResponsibility responsibility in Builders.GetGroupedEnumerator())
            {
                UCPlayer? player = UCPlayer.FromID(responsibility.Steam64);

                float contribution = responsibility.Ticks / Builders.GetTicksNoLock();

                ActionLog.Add(ActionLogType.HelpBuildBuildable, $"{Buildable} - {Mathf.RoundToInt(contribution * 100f).ToString(CultureInfo.InvariantCulture)}%", responsibility.Steam64);

                if (contribution < 0.1f || player == null)
                    continue;

                XPReward reward;
                if (Buildable.Type == BuildableType.Bunker)
                    reward = XPReward.BunkerBuilt;
                else
                    reward = XPReward.Shoveling;

                string msg = Buildable.Translate(player).ToUpperInvariant();

                Points.AwardXP(player, reward, msg + " BUILT", multiplier: contribution);
                if (contribution > 0.3333f)
                    QuestManager.OnBuildableBuilt(player, Buildable);
            }
        }
        finally
        {
            Builders.ReturnLock();
        }

        if (FOB?.Record != null)
        {
            if (Buildable != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await FOB.Record.Update(record =>
                        {
                            if (Buildable.Emplacement != null)
                                ++record.EmplacementsBuilt;
                            else if (Buildable.Type == BuildableType.Fortification)
                                ++record.FortificationsBuilt;
                            else if (Buildable.Type == BuildableType.AmmoCrate)
                                ++record.AmmoCratesBuilt;
                            else if (Buildable.Type == BuildableType.Bunker)
                                ++record.BunkersBuilt;
                            else if (Buildable.Type == BuildableType.RepairStation)
                                ++record.RepairStationsBuilt;
                        });
                    }
                    catch (Exception ex)
                    {
                        L.LogError($"[FOBS] [{FOB.Name}] Failed to update FOB record after building a buildable.");
                        L.LogError(ex);
                    }
                });
            }

            Task.Run(async () =>
            {
                try
                {
                    await FOB.Record.Update(this, record =>
                    {
                        record.Builders = records;
                        record.BuiltAt = now;
                    });
                }
                catch (Exception ex)
                {
                    L.LogError($"[FOBS] [{FOB.Name}] Failed to update FOB record after building a buildable.");
                    L.LogError(ex);
                }
            });
        }

        if (@new == null)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to upgrade buildable: {Buildable}.");
            newBase?.Destroy();
            return false;
        }

        if (@new is ShovelableComponent sh)
            sh.Base = newBase;

        return true;
    }

    public enum BuildableState
    {
        Full,
        Foundation,
        Destroyed
    }
}

public class BunkerComponent : ShovelableComponent
{
    public Vector3 SpawnPosition => transform.position;
    public float SpawnYaw => transform.rotation.eulerAngles.y;
}

public class RepairStationComponent : ShovelableComponent
{
    private static readonly List<InteractableVehicle> WorkingVehicles = new List<InteractableVehicle>(12);
    public readonly Dictionary<uint, int> VehiclesRepairing = new Dictionary<uint, int>(3);
    private int _counter;
    protected override void InitAwake()
    {
        if (Buildable.Type != BuildableType.RepairStation)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] RepairStationComponent not added to a repair station: {Buildable}.");
            goto destroy;
        }
        if (ActiveStructure == null)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] RepairStationComponent not added to a barricade or structure: {Buildable}.");
            goto destroy;
        }
        
        return;
        destroy:
        Destroy(this);
    }

    protected override void InitStart()
    {
        StartCoroutine(RepairStationLoop());
    }
    private IEnumerator<WaitForSeconds> RepairStationLoop()
    {
        const int tickCountPerBuild = 9;
        const float tickSpeed = 1.5f;

        while (true)
        {
            if (State == BuildableState.Full && Data.Gamemode is { State: Gamemodes.State.Staging or Gamemodes.State.Active } && (FOB == null || !FOB.Bleeding))
            {
                VehicleManager.getVehiclesInRadius(Position, 25f * 25f, WorkingVehicles);
                try
                {
                    for (int i = 0; i < WorkingVehicles.Count; i++)
                    {
                        InteractableVehicle vehicle = WorkingVehicles[i];
                        if (vehicle.lockedGroup.m_SteamID.GetTeam() != Team || vehicle.isDead || vehicle.TryGetComponent<IFOBItem>(out _))
                            continue;

                        if (vehicle.asset.engine is not EEngine.PLANE and not EEngine.HELICOPTER &&
                            (Position - vehicle.transform.position).sqrMagnitude > 12f * 12f)
                            continue;

                        if (vehicle.health >= vehicle.asset.health && vehicle.fuel >= vehicle.asset.fuel - 10) // '- 10' so it doesn't use a build as you drive away
                        {
                            if (VehiclesRepairing.ContainsKey(vehicle.instanceID))
                                VehiclesRepairing.Remove(vehicle.instanceID);
                        }
                        else
                        {
                            if (VehiclesRepairing.TryGetValue(vehicle.instanceID, out int ticks))
                            {
                                if (ticks > 0)
                                {
                                    if (vehicle.health < vehicle.asset.health)
                                    {
                                        TickRepair(vehicle);
                                        --ticks;
                                    }
                                    else if (_counter % 3 == 0 && !vehicle.isEngineOn)
                                    {
                                        TickRefuel(vehicle);
                                        --ticks;
                                    }
                                }
                                if (ticks <= 0)
                                    VehiclesRepairing.Remove(vehicle.instanceID);
                                else
                                    VehiclesRepairing[vehicle.instanceID] = ticks;
                            }
                            else
                            {
                                bool inMain = TeamManager.IsInMain(Team, Position);
                                FOB? owningFob = inMain ? null : FOB;
                                if (inMain || (owningFob != null && owningFob.BuildSupply > 0))
                                {
                                    VehiclesRepairing.Add(vehicle.instanceID, tickCountPerBuild);
                                    TickRepair(vehicle);

                                    if (owningFob != null)
                                    {
                                        owningFob.ModifyBuild(-1);
                                        if (vehicle.TryGetComponent(out VehicleComponent comp) && UCPlayer.FromID(comp.LastDriver) is { } lastDriver)
                                            FOBManager.ShowResourceToast(new LanguageSet(lastDriver), build: -1, message: T.FOBResourceToastRepairVehicle.Translate(lastDriver));

                                        UCPlayer? stationPlacer = UCPlayer.FromID(Owner);
                                        if (stationPlacer != null)
                                        {
                                            if (stationPlacer.CSteamID != vehicle.lockedOwner)
                                                Points.AwardXP(stationPlacer, XPReward.RepairVehicle);

                                            if (stationPlacer.Steam64 != owningFob.Owner)
                                                Points.TryAwardFOBCreatorXP(owningFob, XPReward.RepairVehicle, 0.5f);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    _counter++;
                }
                finally
                {
                    WorkingVehicles.Clear();
                }
            }
            yield return new WaitForSeconds(tickSpeed);
        }
    }
    public void TickRepair(InteractableVehicle vehicle)
    {
        if (vehicle.health >= vehicle.asset.health)
            return;

        const ushort amount = 25;

        ushort newHealth = (ushort)Math.Min(vehicle.health + amount, ushort.MaxValue);
        if (vehicle.health + amount >= vehicle.asset.health)
        {
            newHealth = vehicle.asset.health;
            if (vehicle.transform.TryGetComponent(out VehicleComponent c))
            {
                c.DamageTable.Clear();
            }
        }

        VehicleManager.sendVehicleHealth(vehicle, newHealth);
        if (GamemodeOld.Config.EffectRepair.TryGetAsset(out EffectAsset? effect))
            EffectUtility.TriggerEffect(effect, EffectManager.SMALL, vehicle.transform.position, true);
        vehicle.updateVehicle();
    }
    public void TickRefuel(InteractableVehicle vehicle)
    {
        if (vehicle.fuel >= vehicle.asset.fuel)
            return;

        const ushort amount = 180;

        vehicle.askFillFuel(amount);

        if (GamemodeOld.Config.EffectRefuel.TryGetAsset(out EffectAsset? effect))
            EffectUtility.TriggerEffect(effect, EffectManager.SMALL, vehicle.transform.position, true);
        vehicle.updateVehicle();
    }
}
#endif

#if false
public interface IFOBItem
{
    FOB? FOB { get; set; }
    BuildableType Type { get; }
    BuildableData? Buildable { get; }
    ulong Team { get; }
    ulong Owner { get; }
    ulong RecordId { get; set; }
    IAssetLink<EffectAsset>? Icon { get; }
    float IconOffset { get; }
    Vector3 Position { get; }
    Quaternion Rotation { get; }
    event Action<Action<FobItemRecord>> UpdateRecord;
}
#endif
public enum FobRadius : byte
{
    Short,
    Full,
    FullBunkerDependant,
    FobPlacement,
    EnemyBunkerClaim
}
