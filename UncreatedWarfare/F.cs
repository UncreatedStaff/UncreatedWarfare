using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare;

public static class F
{
    public static string RemoveMany(this string source, bool caseSensitive, params char[] replacables)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (replacables.Length == 0) return source;
        char[] chars = source.ToCharArray();
        char[] lowerchars = caseSensitive ? chars : source.ToLower().ToCharArray();
        char[] lowerrepls;
        if (!caseSensitive)
        {
            lowerrepls = new char[replacables.Length];
            for (int i = 0; i < replacables.Length; i++)
            {
                lowerrepls[i] = char.ToLower(replacables[i]);
            }
        }
        else lowerrepls = replacables;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < chars.Length; i++)
        {
            bool found = false;
            for (int c = 0; c < lowerrepls.Length; c++)
            {
                if (lowerrepls[c] == lowerchars[i])
                {
                    found = true;
                }
            }
            if (!found) sb.Append(chars[i]);
        }
        return sb.ToString();
    }

    /// <remarks>Write-locks <see cref="ZoneList"/> if <paramref name="zones"/> is <see langword="true"/>.</remarks>
    public static string GetClosestLocationName(Vector3 point, bool zones = false, bool shortNames = false)
    {
        // todo use zones maybe
        
        LocationDevkitNode? node = null;
        float smallest = 0f;
        foreach (LocationDevkitNode existingNode in LocationDevkitNodeSystem.Get().GetAllNodes())
        {
            Vector3 nodePos = existingNode.transform.position;
            float dist = MathUtility.SquaredDistance(in point, in nodePos, true);
            if (dist < smallest && node is not null)
                continue;

            node = existingNode;
            smallest = dist;
        }

        return node == null ? new GridLocation(in point).ToString() : node.locationName;
    }

    public static bool RoughlyEquals(string? a, string? b) => string.Compare(a, b, CultureInfo.InvariantCulture,
        CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols) == 0;

    public static string ActionLogDisplay(this Asset asset) =>
        $"{asset.FriendlyName} / {asset.id.ToString(CultureInfo.InvariantCulture)} / {asset.GUID:N}";
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="type"/> is not a valid value.</exception>
    public static EItemType GetItemType(this ClothingType type) => type switch
    {
        ClothingType.Shirt => EItemType.SHIRT,
        ClothingType.Pants => EItemType.PANTS,
        ClothingType.Vest => EItemType.VEST,
        ClothingType.Hat => EItemType.HAT,
        ClothingType.Mask => EItemType.MASK,
        ClothingType.Backpack => EItemType.BACKPACK,
        ClothingType.Glasses => EItemType.GLASSES,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
    public static bool ServerTrackQuest(this WarfarePlayer player, QuestAsset quest)
    {
        GameThread.AssertCurrent();
        if (player is not { IsOnline: true })
            return false;
        QuestAsset? current = player.UnturnedPlayer.quests.GetTrackedQuest();
        if (current != null && quest != null)
        {
            if (current.GUID == quest.GUID && !player.Save.TrackQuests)
                player.UnturnedPlayer.quests.ServerAddQuest(quest);

            return false;
        }
        if (player.Save.TrackQuests)
            player.UnturnedPlayer.quests.ServerAddQuest(quest);
        return true;
    }
    public static bool ServerUntrackQuest(this WarfarePlayer player, QuestAsset quest)
    {
        GameThread.AssertCurrent();
        if (player is not { IsOnline: true })
            return false;
        QuestAsset current = player.UnturnedPlayer.quests.GetTrackedQuest();
        if (current == quest)
            return false;

        if (player.UnturnedPlayer.quests.GetTrackedQuest() == quest)
            player.UnturnedPlayer.quests.TrackQuest(null);
        return true;
    }
    public static CombinedTokenSources CombineTokensIfNeeded(this ref CancellationToken token, CancellationToken other)
    {
        if (token.CanBeCanceled)
        {
            if (!other.CanBeCanceled)
                return new CombinedTokenSources(token, null);

            if (token == other)
                return new CombinedTokenSources(token, null);

            CancellationTokenSource src = CancellationTokenSource.CreateLinkedTokenSource(token, other);
            token = src.Token;
            return new CombinedTokenSources(token, src);
        }

        if (!other.CanBeCanceled)
            return default;
        
        token = other;
        return new CombinedTokenSources(other, null);
    }
    public static CombinedTokenSources CombineTokensIfNeeded(this ref CancellationToken token, CancellationToken other1, CancellationToken other2)
    {
        CancellationTokenSource src;
        if (token.CanBeCanceled)
        {
            if (other1.CanBeCanceled)
            {
                if (other2.CanBeCanceled)
                {
                    src = CancellationTokenSource.CreateLinkedTokenSource(token, other1, other2);
                    token = src.Token;
                    return new CombinedTokenSources(token, src);
                }

                src = CancellationTokenSource.CreateLinkedTokenSource(token, other1);
                token = src.Token;
                return new CombinedTokenSources(token, src);
            }

            if (!other2.CanBeCanceled)
                return new CombinedTokenSources(token, null);
            
            src = CancellationTokenSource.CreateLinkedTokenSource(token, other2);
            token = src.Token;
            return new CombinedTokenSources(token, src);
        }
        
        if (other1.CanBeCanceled)
        {
            if (other2.CanBeCanceled)
            {
                src = CancellationTokenSource.CreateLinkedTokenSource(other1, other2);
                token = src.Token;
                return new CombinedTokenSources(token, src);
            }

            token = other1;
            return new CombinedTokenSources(other1, null);
        }

        if (!other2.CanBeCanceled)
            return default;

        token = other2;
        return new CombinedTokenSources(token, null);

    }

    public static T[] AsArrayFast<T>(this IEnumerable<T> enumerable, bool copy = false)
    {
        if (enumerable == null)
            return Array.Empty<T>();
        if (enumerable is T[] arr1)
        {
            if (arr1.Length == 0)
                return Array.Empty<T>();
            if (copy)
            {
                T[] arr2 = new T[arr1.Length];
                Array.Copy(arr1, 0, arr2, 0, arr1.Length);
                return arr2;
            }

            return arr1;
        }
        if (enumerable is List<T> list1)
            return list1.Count == 0 ? Array.Empty<T>() : list1.ToArray();
        if (enumerable is ICollection<T> col1)
        {
            if (col1.Count == 0)
                return Array.Empty<T>();
            T[] arr2 = new T[col1.Count];
            col1.CopyTo(arr2, 0);
            return arr2;
        }

        return enumerable.ToArray();
    }

    public static T[] ToArrayFast<T>(this IEnumerable<T> enumerable, bool copy = false)
    {
        if (!copy && enumerable is T[] array)
            return array;

        if (enumerable is List<T> list)
        {
            if (!copy && list.Count == list.Capacity && Accessor.TryGetUnderlyingArray(list, out T[] underlying))
                return underlying;

            return list.ToArray();
        }

        return enumerable.ToArray();
    }
    /// <summary>
    /// Takes a list of primary key pairs and calls <paramref name="action"/> for each array of values per id.
    /// </summary>
    /// <remarks>The values should be sorted by ID, if not set <paramref name="sort"/> to <see langword="true"/>.</remarks>
    public static void ApplyQueriedList<T>(List<PrimaryKeyPair<T>> list, Action<uint, T[]> action, bool sort = true)
    {
        if (list.Count == 0) return;

        if (sort && list.Count != 1)
            list.Sort((a, b) => a.Key.CompareTo(b.Key));

        T[] arr;
        uint key;
        int last = -1;
        for (int i = 0; i < list.Count; i++)
        {
            PrimaryKeyPair<T> val = list[i];
            if (i <= 0 || list[i - 1].Key == val.Key)
                continue;

            arr = new T[i - 1 - last];
            for (int j = 0; j < arr.Length; ++j)
                arr[j] = list[last + j + 1].Value;
            last = i - 1;

            key = list[i - 1].Key;
            action(key, arr);
        }

        arr = new T[list.Count - 1 - last];
        for (int j = 0; j < arr.Length; ++j)
            arr[j] = list[last + j + 1].Value;

        key = list[^1].Key;
        action(key, arr);
    }

    public static T? FindIndexed<T>(this T[] array, Func<T, int, bool> predicate)
    {
        for (int i = 0; i < array.Length; ++i)
        {
            if (predicate(array[i], i))
                return array[i];
        }

        return default;
    }

}
public readonly struct PrimaryKeyPair<T>
{
    public uint Key { get; }
    public T Value { get; }
    public PrimaryKeyPair(uint key, T value)
    {
        Key = key;
        Value = value;
    }

    public override string ToString() => $"({{{Key}}}, {(Value is null ? "NULL" : Value.ToString())})";
}
public readonly struct CombinedTokenSources : IDisposable
{
    private readonly CancellationTokenSource? _tknSrc;
    public readonly CancellationToken Token;
    internal CombinedTokenSources(CancellationToken token, CancellationTokenSource? tknSrc)
    {
        Token = token;
        _tknSrc = tknSrc;
    }
    public void Dispose()
    {
        _tknSrc?.Dispose();
    }
}