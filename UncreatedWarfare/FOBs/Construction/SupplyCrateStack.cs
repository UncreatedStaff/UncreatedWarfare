using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Construction;

public class SupplyCrateStack : IDisposable
{
    private static readonly Collider?[] ColliderBuffer = new Collider?[1];

    private readonly List<List<StackedSupplyCrate?>> _levels;
    private readonly List<StackedSupplyCrate> _crates;
    private readonly Bounds _buildableBounds;
    private Bounds _stackBounds;
    private readonly Quaternion _barricadeRotation;
    private readonly Quaternion _rotation;

    public GameObject ColliderObject { get; private set; }
    public BoxCollider Collider { get; private set; }

    public ItemPlaceableAsset Asset { get; }

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

    public Quaternion Rotation { get; }

    public SupplyCrateStack(SupplyCrate firstCrate)
    {
        if (!BuildableExtensions.TryGetBuildableBounds(firstCrate.Buildable.Asset, out _buildableBounds))
        {
            _buildableBounds = new Bounds(Vector3.zero, Vector3.one);
        }

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

    private bool CheckIndicesValid(StackedSupplyCrate crate)
    {
        return crate is { Index: >= 0, Level: >= 0 } && crate.Level < _levels.Count && crate.Index < _levels[crate.Level].Count;
    }

    public bool TryGetNextCratePosition(out int level, out int index, out Vector3 position)
    {
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
                    return true;
            }

            // the level above this one has less than this level minus one
            if (lvl.Count > 1 && (l == _levels.Count - 1 || _levels[l + 1].Count < lvl.Count - 1))
            {
                continue;
            }

            if (lvl.Count >= MaxWidth - l)
            {
                continue;
            }

            level = l;
            index = lvl.Count;
            if (HasSupport(level, index) && TestEmptyCratePosition(level, index, out position))
                return true;
        }

        // start new level
        if (_levels.Count < MaxHeight && (_levels.Count == 0 || _levels[^1].Count > 1))
        {
            level = _levels.Count;
            index = 0;
            while (!HasSupport(level, index) && index < _levels[^1].Count)
            {
                ++index;
            }
            if (HasSupport(level, index) && TestEmptyCratePosition(level, index, out position))
                return true;
        }

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
        position = GetPosition(level, index);
        int ct = Physics.OverlapBoxNonAlloc(
            ColliderObject.transform.TransformPoint(position + _buildableBounds.center),
            _buildableBounds.extents * 0.75f,
            ColliderBuffer,
            _rotation,
            RayMasks.BLOCK_BARRICADE
        );
        position = ColliderObject.transform.TransformPoint(position);
        ColliderBuffer[0] = null;

        return ct == 0;
    }

    public StackedSupplyCrate AddCrate(SupplyCrate supplyCrate, int level, int index)
    {
        StackedSupplyCrate crate = new StackedSupplyCrate(level, index, supplyCrate)
        {
            RelativePosition = ColliderObject.transform.InverseTransformPoint(supplyCrate.Buildable.Position - GetBuildableOffset(supplyCrate.Buildable.Asset)),
            Bounds = _buildableBounds
        };

        crate.Bounds.center += crate.RelativePosition;

        _crates.Add(crate);
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
        return crate;
    }

    public void RemoveCrate(StackedSupplyCrate crate)
    {
        if (!CheckIndicesValid(crate))
        {
            return;
        }

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
        return;

        void Remove(int level, int index, bool updateBounds)
        {
            List<StackedSupplyCrate?> lvl = _levels[level];
            lvl[index] = null;
            if (lvl.All(x => x == null))
            {
                _levels.RemoveAt(level);
                if (updateBounds)
                    UpdateBounds();
            }
            else if (index == 0 || index == lvl.Count - 1)
            {
                if (updateBounds)
                    UpdateBounds();
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
}

internal class SupplyStackComponent : MonoBehaviour
{
    public SupplyCrateStack Stack { get; private set; }

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
