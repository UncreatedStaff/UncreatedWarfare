using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Kits;

/// <summary>
/// Helper functions for formatting and parsing loadout IDs.
/// </summary>
/// <remarks>Loadout IDs are formatted with the following layout: <c>Steam64_letters</c> <c>7650000000000000_abc</c></remarks>
public static class LoadoutIdHelper
{
    /// <summary>
    /// Get a consistant display string for 'kit not found' when the kit is a target sign.
    /// </summary>
    public static string GetLoadoutSignDisplayText(int signDataLoadoutNumber)
    {
        return "Loadout #" + signDataLoadoutNumber;
    }

    /// <summary>
    /// Parses the number and at the end of the loadout ID.
    /// </summary>
    /// <returns>-1 if operation results in an overflow, the string is too short, or invalid characters are found, otherwise, the id of the loadout.</returns>
    public static int Parse(ReadOnlySpan<char> kitId)
    {
        return Parse(kitId, out _);
    }

    /// <summary>
    /// Parses the number and at the end of the loadout ID and the player ID at the beginning.
    /// </summary>
    /// <returns>-1 if operation results in an overflow, the string is too short, or invalid characters are found, otherwise, the id of the loadout.</returns>
    public static int Parse(ReadOnlySpan<char> kitId, out CSteamID player)
    {
        if (kitId.Length <= 17 || kitId[17] != '_' || !ulong.TryParse(kitId[..17], NumberStyles.Number, CultureInfo.InvariantCulture, out ulong playerId))
        {
            player = CSteamID.Nil;
            return -1;
        }

        player = Unsafe.As<ulong, CSteamID>(ref playerId);
        return ParseNumber(kitId[18..]);
    }

    /// <summary>
    /// Parses a excel column header style number into an integer. Note that <paramref name="chars"/> should only be the section with the letters.
    /// </summary>
    /// <remarks>Indexed from 1.</remarks>
    /// <returns>-1 if operation results in an overflow or invalid characters are found, otherwise, the id of the loadout.</returns>
    public static int ParseNumber(ReadOnlySpan<char> chars)
    {
        int id = 0;
        if (chars.Length > 18)
        {
            id = Parse(chars, out _);
            if (id > 0)
                return id;

            id = 0;
        }

        for (int i = chars.Length - 1; i >= 0; --i)
        {
            int c = chars[i];
            int lastId = id;
            if (c is > 96 and < 123)
                id += (c - 96) * (int)Math.Pow(26, chars.Length - i - 1);
            else if (c is > 64 and < 91)
                id += (c - 64) * (int)Math.Pow(26, chars.Length - i - 1);
            else return -1;

            if (id < lastId) // overflow
                return -1;
        }

        return id;
    }

    /// <summary>
    /// Gets the full loadout ID for a as a <see cref="string"/>.
    /// </summary>
    /// <remarks>Indexed from 1.</remarks>
    public static string GetLoadoutName(CSteamID player, int id)
    {
        if (id <= 0)
        {
            id = 1;
        }

        int len = GetLoadoutLetterCount(id) + 18;
        GetLoadoutNameState state = default;
        state.Id = id;
        state.Player = player.m_SteamID;

        return string.Create(len, state, (span, state) =>
        {
            state.Player.TryFormat(span, out _, "D17", CultureInfo.InvariantCulture);
            span[17] = '_';
            WriteLoadoutLetters(span[18..], state.Id);
        });
    }

    private struct GetLoadoutNameState
    {
        public ulong Player;
        public int Id;
    }

    /// <summary>
    /// Gets the letters of a loadout ID as a <see cref="string"/>.
    /// </summary>
    /// <remarks>Indexed from 1.</remarks>
    public static string GetLoadoutLetter(int id)
    {
        if (id <= 0)
        {
            id = 1;
        }

        int len = GetLoadoutLetterCount(id);
        return string.Create(len, id, WriteLoadoutLetters);
    }
    
    private static int GetLoadoutLetterCount(int id)
    {
        return (int)Math.Ceiling(Math.Log(id == 1 ? 2 : id, 26));
    }

    private static void WriteLoadoutLetters(Span<char> span, int id)
    {
        int index = span.Length - 1;
        while (true)
        {
            span[index] = (char)(((id - 1) % 26) + 97);
            int rem = (id - 1) / 26;
            if (rem <= 0)
                break;

            --index;
            id = rem;
        }
    }
}