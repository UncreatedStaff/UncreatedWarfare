using DanielWillett.SpeedBytes;
using System;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits;

/// <summary>
/// Tracks a player's current kit and information about it's request parameters.
/// </summary>
public sealed class CurrentKitState
{
    // false if read from legacy binary save that only stored the key
    internal bool HasFullState;

    /// <summary>
    /// The class of the kit when it was requested.
    /// </summary>
    public Class Class { get; private set; }

    /// <summary>
    /// The branch of the kit when it was requested.
    /// </summary>
    public Branch Branch { get; private set; }

    /// <summary>
    /// The kit as it was given.
    /// Use <see cref="KitPlayerComponent.GetActiveKitAsync"/> to get an up-to-date copy.
    /// </summary>
    /// <remarks>Guaranteed to have <see cref="KitInclude.Giveable"/> data.</remarks>
    public Kit CachedKit { get; internal set; }

    /// <summary>
    /// The primary key of the kit.
    /// </summary>
    public uint Key { get; private set; }

    /// <summary>
    /// The ID of the kit.
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// If the player's current kit was equipped temporarily for previewing the kit.
    /// </summary>
    public bool IsPreview { get; private set; }

    /// <summary>
    /// If the player's current kit was equipped with low ammo.
    /// </summary>
    public bool IsLowAmmo { get; private set; }

    /// <summary>
    /// The kit that was equipped before this kit was previewed.
    /// </summary>
    public CurrentKitState? PreviewFallback { get; internal set; }

    internal IItem[]? ItemsFallback { get; set; }

    internal string ParameterString => $"IsLowAmmo: {(IsLowAmmo ? "Y" : "N")}, IsPreview: {(IsPreview ? "Y" : "N")}";

#pragma warning disable CS8618
    internal CurrentKitState(uint kitPk = 0, bool wasKitLowOnAmmo = false)
    {
        Key = kitPk;
        IsLowAmmo = wasKitLowOnAmmo;
    }
#pragma warning restore CS8618

    public CurrentKitState(Kit cachedKit, bool isPreview, bool isLowAmmo, CurrentKitState? previewFallback)
    {
        if (!isPreview && previewFallback != null)
            throw new ArgumentException("Should only be supplied for preview kits.", nameof(previewFallback));

        Key = cachedKit.Key;

        UpdateCachedKit(cachedKit);

        IsPreview = isPreview;
        IsLowAmmo = isLowAmmo;
        if (isPreview)
        {
            PreviewFallback = previewFallback;
        }

        HasFullState = true;
    }

    [MemberNotNull(nameof(Id))]
    [MemberNotNull(nameof(CachedKit))]
    internal void UpdateCachedKit(Kit kit)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));

        if (kit.Key != Key)
            throw new InvalidOperationException("Expected a kit with the same ID as already configured.");

        CachedKit = kit;
        Id = kit.Id;

        if (HasFullState)
            return;

        Class = kit.Class;
        Branch = kit.Branch;
        Key = kit.Key;
        HasFullState = true;
    }

    internal KitBestowData CreateBestowData()
    {
        return new KitBestowData(CachedKit)
        {
            IsPreview = IsPreview,
            IsLowAmmo = IsLowAmmo
        };
    }

    internal static void ToWriter(CurrentKitState? state, ByteWriter writer)
    {
        if (state == null)
            writer.Write((byte)0);
        else
            state.Write(writer);
    }

    internal static CurrentKitState? FromReader(ByteReader reader)
    {
        byte version = reader.ReadUInt8();
        if (version == 0)
            return null;
        CurrentKitState state = new CurrentKitState();
        state.ReadDetails(version, reader);
        return state;
    }

    internal void Write(ByteWriter writer)
    {
        writer.Write((byte)1);
        writer.Write((byte)Class);
        writer.Write((byte)Branch);
        writer.Write(Key);
        writer.WriteNullable(Id);

        byte flags = 0;
        if (IsPreview)
            flags |= 1;
        if (IsLowAmmo)
            flags |= 2;
        CurrentKitState? fallback = PreviewFallback;
        if (fallback != null)
            flags |= 4;

        IItem[]? fallbackItems = ItemsFallback;
        if (fallbackItems != null)
            flags |= 8;

        writer.Write(flags);

        fallback?.Write(writer);

        if (fallbackItems == null)
            return;

        int ct = Math.Min(byte.MaxValue, fallbackItems.Length);
        writer.Write((byte)ct);
        for (int i = 0; i < ct; ++i)
        {
            KitItemUtility.WriteItem(fallbackItems[i], writer);
        }
    }

    private void ReadDetails(byte _, ByteReader reader)
    {
        Class = (Class)reader.ReadUInt8();
        Branch = (Branch)reader.ReadUInt8();
        Key = reader.ReadUInt32();
        Id = reader.ReadNullableString()!;

        if (CachedKit != null && CachedKit.Key != Key)
            CachedKit = null!;

        byte flags = reader.ReadUInt8();
        IsPreview = (flags & 1) != 0;
        IsLowAmmo = (flags & 2) != 0;

        if ((flags & 4) != 0)
            PreviewFallback = FromReader(reader);
        else
            PreviewFallback = null;

        if ((flags & 8) != 0)
        {
            int ct = reader.ReadUInt8();
            IItem[] items = new IItem[ct];
            int index = 0;
            for (int i = 0; i < ct; ++i)
            {
                IItem? item = KitItemUtility.ReadItem(reader);
                if (item == null)
                    continue;

                items[index] = item;
                ++index;
            }

            if (index < items.Length)
                Array.Resize(ref items, index);
        }

        HasFullState = true;
    }
}