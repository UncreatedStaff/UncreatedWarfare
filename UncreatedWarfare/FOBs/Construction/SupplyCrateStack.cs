using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction.Icons;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Construction;

public class SupplyCrateStack : IDisposable
{
    internal static readonly int RayMaskBlockSupplyCrate = RayMasks.BLOCK_COLLISION & ~RayMasks.VEHICLE;

    private readonly WorldIconManager? _worldIconManager;
    private readonly AssetConfiguration _assetConfiguration;

    private readonly List<List<StackedSupplyCrate?>> _levels;
    private readonly List<StackedSupplyCrate> _crates;
    private readonly Bounds _buildableBounds;
    private Bounds _stackBounds;
    private readonly Quaternion _barricadeRotation;
    private readonly Quaternion _rotation;

    public GameObject ColliderObject { get; private set; }
    public BoxCollider Collider { get; private set; }

    public ItemPlaceableAsset Asset { get; }
    public Team Team { get; }

    public int MaxHeight { get; }
    public int MaxWidth { get; }

    /// <summary>
    /// The axis that is perpendicular with the stack's front/back.
    /// </summary>
    public SnapAxis Axis { get; }

    public Vector3 HorizontalVector { get; }
    public Vector3 VerticalVector { get; }

    public IReadOnlyList<IReadOnlyList<StackedSupplyCrate?>> Levels { get; }
    public IReadOnlyList<StackedSupplyCrate> Crates { get; }

    public WorldIconInfo? Icon { get; private set; }

    public Quaternion Rotation { get; }

    public SupplyCrateStack(SupplyCrate firstCrate, IServiceProvider serviceProvider)
    {
        if (!BuildableExtensions.TryGetBuildableBounds(firstCrate.Buildable.Asset, out _buildableBounds))
        {
            _buildableBounds = new Bounds(Vector3.zero, Vector3.one);
        }

        _worldIconManager = serviceProvider.GetService<WorldIconManager>();
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();

        Vector3 size = _buildableBounds.size;

        const float crateSpacing = 0.2f;
        const float layerSpacing = -0.05f;

        Axis = firstCrate.Info!.StackAxis;
        HorizontalVector = Axis switch
        {
            SnapAxis.X => new Vector3(0f, 0f, size.z + crateSpacing),
            SnapAxis.Y => new Vector3(0f, size.y + layerSpacing, 0f),
            _ => new Vector3(size.x + crateSpacing, 0f, 0f)
        };
        VerticalVector = Axis switch
        {
            SnapAxis.Y => new Vector3(0f, 0f, size.z + crateSpacing),
            _ => new Vector3(0f, size.y + layerSpacing, 0f)
        };

        MaxHeight = firstCrate.Info!.MaxStackHeight;
        MaxWidth = firstCrate.Info!.MaxStackWidth;
        Axis = firstCrate.Info.StackAxis;

        Asset = firstCrate.Buildable.Asset;

        _barricadeRotation = firstCrate.Buildable.Rotation;
        _rotation = _barricadeRotation * BarricadeUtility.InverseDefaultBarricadeRotation;

        Rotation = _barricadeRotation;

        StackedSupplyCrate crate = new StackedSupplyCrate(0, 0, firstCrate)
        {
            Bounds = _buildableBounds
        };

        Icon = null;

        _levels = new List<List<StackedSupplyCrate?>>(3) { new List<StackedSupplyCrate?> { crate } };
        Levels = _levels.AsReadOnly();

        _crates = new List<StackedSupplyCrate>(8) { crate };
        Crates = _crates.AsReadOnly();

        ColliderObject = new GameObject(firstCrate.Buildable.Asset.name + " Stack", typeof(BoxCollider), typeof(SupplyStackComponent))
        {
            layer = LayerMasks.LOGIC
        };

        ColliderObject.transform.SetPositionAndRotation(firstCrate.Buildable.Position - new Vector3(0f, firstCrate.Buildable.Asset switch
        {
            ItemBarricadeAsset b => b.offset,
            _ => 0f
        }, 0f), _rotation);
        
        Collider = ColliderObject.GetComponent<BoxCollider>();
        Collider.isTrigger = true;

        UpdateBounds();

        ColliderObject.GetComponent<SupplyStackComponent>().Init(this);

        LogMessage("New single supply crate created.");
    }

    private void UpdateBounds()
    {
        if (_crates.Count == 0)
            return;

        Bounds bounds = _crates[0].Bounds;

        for (int i = 1; i < _crates.Count; i++)
        {
            bounds.Encapsulate(_crates[i].Bounds);
        }

        Vector3 e = bounds.extents;
        bounds.extents = new Vector3(Math.Abs(e.x), Math.Abs(e.y), Math.Abs(e.z));
        _stackBounds = bounds;

        Collider.center = _stackBounds.center;
        Collider.size = _stackBounds.size;
    }

    internal void UpdateIconDisplay()
    {
        if (_worldIconManager == null)
            return;

        SupplyCrate firstCrate = Crates[0].Crate;
        if (Crates.Count == 1)
        {
            if (Icon == null)
                return;

            Icon.Dispose();
            Icon = null;
            firstCrate.IsIconVisible = true;
            return;
        }

        WorldIconInfo? existingIcon = firstCrate.Icon;

        Team team = firstCrate.Team;

        string? iconPath = firstCrate.Info?.Icon;
        IAssetLink<EffectAsset>? asset = string.IsNullOrEmpty(iconPath) ? null : _assetConfiguration.GetAssetLink<EffectAsset>(iconPath);

        bool newIcon = false;
        if (Icon == null)
        {
            if (existingIcon == null || asset == null)
                return;

            Icon = new WorldIconInfo(ColliderObject.transform, asset, team);
            newIcon = true;
        }
        else if (existingIcon == null)
        {
            Icon.Dispose();
            Icon = null;
            return;
        }
        else
        {
            if (asset == null)
            {
                Icon.Dispose();
                Icon = null;
                return;
            }

            if (!Icon.Effect.MatchAsset(asset))
            {
                Icon.Dispose();
                Icon = new WorldIconInfo(ColliderObject.transform, asset, team);
                newIcon = true;
            }
        }

        Vector3 originalOffset = firstCrate.Info?.IconOffset ?? default;
        Icon.Offset = originalOffset + new Vector3(_stackBounds.center.x, _stackBounds.size.y, _stackBounds.center.z);

        if (newIcon)
        {
            _worldIconManager.CreateIcon(Icon);
        }

        foreach (StackedSupplyCrate crate in Crates)
        {
            crate.Crate.IsIconVisible = false;
        }
    }

    private bool CheckIndicesValid(StackedSupplyCrate crate)
    {
        return crate is { Index: >= 0, Level: >= 0 } && crate.Level < _levels.Count && crate.Index < _levels[crate.Level].Count;
    }

    public bool TryGetNextCratePosition(out int level, out int index, out Vector3 position)
    {
#if FALLING_EFFECT_DEBUG_LOGGING
        EffectUtility.ClearDebugEffect();
#endif

        level = -1;
        index = -1;
        for (int l = 0; l < _levels.Count; ++l)
        {
            List<StackedSupplyCrate?> lvl = _levels[l];

            // fill empty slot
            int emptySlot = lvl.IndexOf(null);

            while (emptySlot >= 0 && !HasSupport(l, emptySlot))
            {
                if (emptySlot >= lvl.Count - (l % 2 == 1 ? 1 : 0))
                    emptySlot = -1;
                else
                    emptySlot = lvl.IndexOf(null, emptySlot + 1);
            }

            if (emptySlot != -1)
            {
                level = l;
                index = emptySlot;
                if (TestEmptyCratePosition(level, index, out position))
                {
                    LogMessage($"Filled empty slot ({level}, {index}).");
                    return true;
                }
                
                LogMessage($"({level}, {index}) invalid (1).");
            }

            // the level above this one has less than this level minus one
            if (lvl.Count >= 2 && l + 1 < MaxHeight)
            {
                int nextLvlCt = l >= _levels.Count - 1 ? 0 : _levels[l + 1].Count;
                if (nextLvlCt < lvl.Count - 1)
                {
                    LogMessage($"Next level {l + 1} has more space than level {l}: {lvl.Count} vs {nextLvlCt}.");
                    continue;
                }
            }

            if (lvl.Count >= MaxWidth - (l % 2 == 1 ? 1 : 0))
            {
                LogMessage($"Level {l} at max width ({lvl.Count}: {MaxWidth - l}).");
                continue;
            }

            level = l;
            index = lvl.Count;
            if (HasSupport(level, index) && TestEmptyCratePosition(level, index, out position))
            {
                LogMessage($"Added new slot ({level}, {index}).");
                return true;
            }

            LogMessage($"({level}, {index}) invalid (2).");
        }

        // start new level
        if (_levels.Count < MaxHeight && (_levels.Count == 0 || _levels[^1].Count > 1))
        {
            level = _levels.Count;
            index = 0;
            while (!HasSupport(level, index) && index < _levels[^1].Count)
            {
                LogMessage($"Initial new level position ({level}, {index}) missing support.");
                ++index;
            }
            if (HasSupport(level, index) && TestEmptyCratePosition(level, index, out position))
            {
                LogMessage($"Started new level {level}.");
                return true;
            }

            LogMessage($"({level}, {index}) invalid (3).");
        }

        LogMessage("No open space.");
        position = default;
        return false;
    }

    private bool HasSupport(int level, int index)
    {
        if (level <= 0)
            return true;

        if (level > _levels.Count)
            return false;

        List<StackedSupplyCrate?> baseLevel = _levels[level - 1];

        if (level % 2 == 1)
        {
            return index + 1 < baseLevel.Count && baseLevel[index] != null && baseLevel[index + 1] != null;
        }

        return index > 0 && index < baseLevel.Count && baseLevel[index - 1] != null && baseLevel[index] != null;
    }

    private bool TestEmptyCratePosition(int level, int index, out Vector3 position)
    {
        LogMessage($"Testing ({level}, {index})...");
        position = GetPosition(level, index);
        Vector3 boxCenter = ColliderObject.transform.TransformPoint(position + _buildableBounds.center);
        Vector3 boundsExtents = ColliderObject.transform.TransformVector(_buildableBounds.extents);
#if FALLING_EFFECT_DEBUG_LOGGING
        EffectUtility.TriggerDebugEffectBox(boxCenter, boundsExtents, _rotation, clear: false, effectScale: 0.2f);
#endif

        // test for stuff blocking the box
        Vector3 blockingExtents = boundsExtents * 0.5f;
        bool blocking = Physics.CheckBox(boxCenter, blockingExtents, _rotation, RayMaskBlockSupplyCrate, QueryTriggerInteraction.Ignore);
        if (blocking)
        {
            LogMessage(" - Blocked.");
#if FALLING_EFFECT_DEBUG_LOGGING
            EffectUtility.TriggerDebugEffectBox(boxCenter, blockingExtents, _rotation, clear: false, effectScale: 0.5f);
#else
            position = default;
            return false;
#endif
        }

        bool supporting = true;
        if (level == 0) // we already checked on the higher levels
        {
            float supportBoxSize = _buildableBounds.extents.y;

            // test for support below. basically checks a rectangle stretching the bottom face of the rectangle
            Vector3 supportCenter = new Vector3(boxCenter.x, boxCenter.y - boundsExtents.y, boxCenter.z);
            Vector3 supprtExtents = new Vector3(boundsExtents.x * 0.9f, supportBoxSize, boundsExtents.z * 0.9f);
            supporting = Physics.CheckBox(supportCenter, supprtExtents, _rotation, RayMaskBlockSupplyCrate, QueryTriggerInteraction.Ignore);
            if (!supporting)
            {
                LogMessage(" - Unsupported.");
#if FALLING_EFFECT_DEBUG_LOGGING
                EffectUtility.TriggerDebugEffectBox(supportCenter, supprtExtents, _rotation, clear: false);
#else
                position = default;
                return false;
#endif
            }
        }

        position = ColliderObject.transform.TransformPoint(position);
        return !blocking && supporting;
    }

    public StackedSupplyCrate AddCrate(SupplyCrate supplyCrate, int level, int index)
    {
        StackedSupplyCrate crate = new StackedSupplyCrate(level, index, supplyCrate)
        {
            RelativePosition = ColliderObject.transform.InverseTransformPoint(supplyCrate.Buildable.Position - GetBuildableOffset(supplyCrate.Buildable.Asset)),
            Bounds = _buildableBounds
        };

        LogMessage($"Adding crate ({level}, {index}).");
        crate.Bounds.center += crate.RelativePosition;

        _crates.Add(crate);
        supplyCrate.IsIconVisible = _crates.Count == 1;
        List<StackedSupplyCrate?> crates;
        if (_levels.Count <= level)
        {
            crates = new List<StackedSupplyCrate?>(index + 2);
            _levels.Add(crates);
        }
        else
        {
            crates = _levels[level];
        }

        for (int i = crates.Count; i <= index; ++i)
            crates.Add(null);
        crates[index] = crate;
        UpdateBounds();
        UpdateIconDisplay();
        return crate;
    }

    public void RemoveCrate(StackedSupplyCrate crate)
    {
        if (!CheckIndicesValid(crate))
        {
            return;
        }

        LogMessage($"Removing crate ({crate.Level}, {crate.Index}).");
        crate.IsRemoved = true;

        List<StackedSupplyCrate?> level = _levels[crate.Level];

        // removing from top level
        if (_levels.Count == crate.Level + 1)
        {
            Remove(crate.Level, crate.Index, true);
            return;
        }

        // removing from intermediate layer
        int topLevelIndex = _levels.Count - 1;
        List<StackedSupplyCrate?> topLevel = _levels[topLevelIndex];
        int firstIndex = topLevel.FindIndex(x => x != null);
        if (firstIndex == -1)
            throw new InvalidOperationException("Somehow left an empty list on the stack.");

        StackedSupplyCrate crateToReplaceOld = topLevel[firstIndex]!;
        topLevel[firstIndex] = null;
        Remove(topLevelIndex, firstIndex, false);

        // move another crate from top level to the place where it was removed
        level[crate.Index] = crateToReplaceOld;
        crateToReplaceOld.Level = crate.Level;
        crateToReplaceOld.Index = crate.Index;

        Vector3 position = crate.RelativePosition;
        crateToReplaceOld.Crate.Buildable.SetPositionAndRotation(
            ColliderObject.transform.TransformPoint(position) + GetBuildableOffset(crateToReplaceOld.Crate.Buildable.Asset),
            _barricadeRotation
        );
        crateToReplaceOld.RelativePosition = position;
        crateToReplaceOld.Bounds.center = position + _buildableBounds.center;
        UpdateBounds();
        UpdateIconDisplay();
        return;

        void Remove(int level, int index, bool updateBounds)
        {
            List<StackedSupplyCrate?> lvl = _levels[level];
            lvl[index] = null;
            if (lvl.All(x => x == null))
            {
                _levels.RemoveAt(level);
                if (_levels.Count == 0)
                    Dispose();
                else if (updateBounds)
                {
                    UpdateBounds();
                    UpdateIconDisplay();
                }
            }
            else if (index == 0 || index == lvl.Count - 1)
            {
                if (!updateBounds)
                    return;

                UpdateBounds();
                UpdateIconDisplay();
            }
        }
    }

    private static Vector3 GetBuildableOffset(ItemPlaceableAsset asset)
    {
        return new Vector3(0f, asset is ItemBarricadeAsset b ? b.offset : 0f, 0f);
    }

    public Vector3 GetPosition(int level, int index)
    {
        bool isOffsetLevel = level % 2 == 1;

        Vector3 layerCenter = level * VerticalVector;
        return layerCenter + HorizontalVector * (index + (isOffsetLevel ? 0.5f : 0f));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (ColliderObject is null)
            return;

        if (GameThread.IsCurrent)
        {
            if (ColliderObject != null)
                Object.Destroy(ColliderObject);
            ColliderObject = null!;
            Collider = null!;
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                if (ColliderObject != null)
                    Object.Destroy(ColliderObject);
                ColliderObject = null!;
                Collider = null!;
            });
        }
    }

    [Conditional("FALLING_EFFECT_DEBUG_LOGGING")]
    internal void LogMessage(string msg, LogLevel lvl = LogLevel.Debug)
    {
        ILogger<SupplyCrateStack> logger = WarfareModule.Singleton.ServiceProvider.Resolve<ILogger<SupplyCrateStack>>();

        logger.Log(lvl, $"[{Asset.FriendlyName})] {Collider.transform.gameObject.GetInstanceID()} {msg}");
    }
}

internal class SupplyStackComponent : MonoBehaviour
{
#nullable disable

    public SupplyCrateStack Stack { get; private set; }

#nullable restore

    internal void Init(SupplyCrateStack stack)
    {
        Stack = stack;
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        Stack.Dispose();
    }
}

public class StackedSupplyCrate
{
    internal Bounds Bounds;
    internal Vector3 RelativePosition;
    public SupplyCrate Crate { get; }
    public int Level { get; set; }
    public int Index { get; set; }

    public bool IsRemoved { get; set; }

    public StackedSupplyCrate(int initialLevel, int initialIndex, SupplyCrate crate)
    {
        Crate = crate;
        Level = initialLevel;
        Index = initialIndex;
    }
}
