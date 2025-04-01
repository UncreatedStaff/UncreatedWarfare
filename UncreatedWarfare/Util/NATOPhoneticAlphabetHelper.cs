using System;
using AlphabetRow = (string Proper, string Lower, string Upper);

namespace Uncreated.Warfare.Util;

/// <summary>
/// Helper for parsing and getting NATO code words for the latin alphabet.
/// </summary>
public static class NATOPhoneticAlphabetHelper
{
    // https://www.nato.int/nato_static_fl2014/assets/pdf/pdf_2018_01/20180111_nato-alphabet-sign-signal.pdf

    private static readonly AlphabetRow[] NatoPhoneticAlphabet =
    [
        ("Alfa",     "alfa",     "ALFA"),       // A
        ("Bravo",    "bravo",    "BRAVO"),      // B
        ("Charlie",  "charlie",  "CHARLIE"),    // C
        ("Delta",    "delta",    "DELTA"),      // D
        ("Echo",     "echo",     "ECHO"),       // E
        ("Foxtrot",  "foxtrot",  "FOXTROT"),    // F
        ("Golf",     "golf",     "GOLF"),       // G
        ("Hotel",    "hotel",    "HOTEL"),      // H
        ("India",    "india",    "INDIA"),      // I
        ("Juliett",  "juliett",  "JULIETT"),    // J
        ("Kilo",     "kilo",     "KILO"),       // K
        ("Lima",     "lima",     "LIMA"),       // L
        ("Mike",     "mike",     "MIKE"),       // M
        ("November", "november", "NOVEMBER"),   // N
        ("Oscar",    "oscar",    "OSCAR"),      // O
        ("Papa",     "papa",     "PAPA"),       // P
        ("Quebec",   "quebec",   "QUEBEC"),     // Q
        ("Romeo",    "romeo",    "ROMEO"),      // R
        ("Sierra",   "sierra",   "SIERRA"),     // S
        ("Tango",    "tango",    "TANGO"),      // T
        ("Uniform",  "uniform",  "UNIFORM"),    // U
        ("Victor",   "victor",   "VICTOR"),     // V
        ("Whiskey",  "whiskey",  "WHISKEY"),    // W
        ("Xray",     "xray",     "XRAY"),       // X
        ("Yankee",   "yankee",   "YANKEE"),     // Y
        ("Zulu",     "zulu",     "ZULU")        // Z
    ];

    private static readonly AlphabetRow[] Digits =
    [
        ("Zero",  "zero",  "ZERO"),  // 0
        ("One",   "one",   "ONE"),   // 1
        ("Two",   "two",   "TWO"),   // 2
        ("Three", "three", "THREE"), // 3
        ("Four",  "four",  "FOUR"),  // 4
        ("Five",  "five",  "FIVE"),  // 5
        ("Six",   "six",   "SIX"),   // 6
        ("Seven", "seven", "SEVEN"), // 7
        ("Eight", "eight", "EIGHT"), // 8
        ("Nine",  "nine",  "NINE")   // 9
    ];

    /// <summary>
    /// Get the word for the phonetic alphabet <paramref name="character"/> in proper-case. Example: 'Bravo'.
    /// </summary>
    /// <param name="character">Any character in the ranges <c>'a'-'z'</c>, <c>'A'-'Z'</c>, or <c>'0'-'9'</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static string GetProperCase(char character)
    {
        return GetRow(character).Proper;
    }

    /// <summary>
    /// Get the word for the phonetic alphabet <paramref name="character"/> in lower-case. Example: 'bravo'.
    /// </summary>
    /// <param name="character">Any character in the ranges <c>'a'-'z'</c>, <c>'A'-'Z'</c>, or <c>'0'-'9'</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static string GetLowerCase(char character)
    {
        return GetRow(character).Lower;
    }

    /// <summary>
    /// Get the word for the phonetic alphabet <paramref name="character"/> in upper-case. Example: 'BRAVO'.
    /// </summary>
    /// <param name="character">Any character in the ranges <c>'a'-'z'</c>, <c>'A'-'Z'</c>, or <c>'0'-'9'</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static string GetUpperCase(char character)
    {
        return GetRow(character).Upper;
    }

    /// <summary>
    /// Attempts to identify the code word in <paramref name="word"/> and output the character associated with it.
    /// </summary>
    /// <exception cref="FormatException">Failed to identify a code word.</exception>
    /// <returns>A character in the ranges <c>'a'-'z'</c>, <c>'A'-'Z'</c>, or <c>'0'-'9'</c>.</returns>
    public static char Parse(ReadOnlySpan<char> word)
    {
        if (TryParse(word, out char alphabetCharacter))
            return alphabetCharacter;

        throw new FormatException("Failed to identify a NATO code word.");
    }

    /// <summary>
    /// Attempts to identify the code word in <paramref name="word"/> and output the character associated with it.
    /// </summary>
    /// <param name="alphabetCharacter">A character in the ranges <c>'a'-'z'</c>, <c>'A'-'Z'</c>, or <c>'0'-'9'</c>, or <c>NUL</c> if parse failed.</param>
    /// <returns><see langword="true"/> if a code word was parsed, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> word, out char alphabetCharacter)
    {
        if (word.IsEmpty)
        {
            alphabetCharacter = '\0';
            return false;
        }

        char character = word[0];

        word = word.Trim();

        ReadOnlySpan<char> compare;
        switch (character)
        {
            case >= 'A' and <= 'Z':
                compare = NatoPhoneticAlphabet[character - 'A'].Lower;
                alphabetCharacter = character;
                break;

            case >= 'a' and <= 'z':
                compare = NatoPhoneticAlphabet[character - 'a'].Lower;
                alphabetCharacter = (char)(character - 32); // to lower
                break;

            case >= '0' and <= '9':
                compare = Digits[character - '0'].Lower;
                alphabetCharacter = character;
                break;

            default:
                alphabetCharacter = '\0';
                return false;
        }


        if (compare.Equals(word, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return alphabetCharacter switch
        {
            // common misconceptions 
            'X' => word.Equals("x-ray", StringComparison.OrdinalIgnoreCase),
            'A' => word.Equals("alpha", StringComparison.OrdinalIgnoreCase),
            'J' => word.Equals("juliet", StringComparison.OrdinalIgnoreCase),

            // digits
            >= '0' and <= '9' => word.Length == 1,

            _ => false
        };
    }

    private static ref readonly AlphabetRow GetRow(char character)
    {
        // ReSharper disable once ConvertSwitchStatementToSwitchExpression (doesnt work for some reason)
        switch (character)
        {
            case >= 'A' and <= 'Z':
                return ref NatoPhoneticAlphabet[character - 'A'];

            case >= 'a' and <= 'z':
                return ref NatoPhoneticAlphabet[character - 'a'];
                
            case >= '0' and <= '9':
                return ref Digits[character - '0'];

            default:
                throw new ArgumentOutOfRangeException(nameof(character));
        }
    }
}