using System;
using System.Globalization;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations.Util;

/// <summary>
/// Argument modifiers are in-line modifiers to existing words based on the value of an argument.
/// <br/>
/// For example:
/// <code>There ${p:0:is} {0} ${p:0:apple}.</code> could be turned into <code>There is 1 apple.</code> or <code>There are 2 apples.</code>
/// They can be inverted by placing an exclamation point before the ending brace: <c>${p:0:word!}</c>.
/// </summary>
/// <remarks>Right now only the 'p' modifier (plural) is supported.</remarks>
internal static class TranslationArgumentModifiers
{
#pragma warning disable CS8500
    /// <summary>
    /// Replace text in <paramref name="input"/> with text from <paramref name="collection"/>, which is a block of text split up by <paramref name="indices"/>.
    /// </summary>
    /// <remarks>
    /// An index with a value less than 0 results in the original text staying the same.
    /// <paramref name="collection"/> must be in order.
    /// <paramref name="indices"/> and <paramref name="arguments"/> must be the same length.
    /// </remarks>
    public static unsafe string ReplaceModifiers(ReadOnlySpan<char> input, ReadOnlySpan<char> collection, ReadOnlySpan<int> indices, ReadOnlySpan<ArgumentSpan> arguments, int argumentStartIndexOffset = 0)
    {
        if (arguments.Length != indices.Length)
            throw new ArgumentException("Arguments and indices lists must match in length.");
        int length = input.Length;
        int argCt = arguments.Length;
        if (argCt == 0)
            return new string(input);

        for (int i = 0; i < argCt; ++i)
        {
            int segmentIndex = indices[i];
            if (segmentIndex < 0)
                continue;

            ref readonly ArgumentSpan argSpan = ref arguments[i];

            int nextIndex = collection.Length;
            for (int j = i + 1; j < argCt; ++j)
            {
                int fwdIndex = indices[j];
                if (fwdIndex < 0)
                    continue;

                nextIndex = fwdIndex;
                break;
            }

            int segmentLength = nextIndex - segmentIndex;
            int argLength = argSpan.Length;
            length += segmentLength - argLength;
        }

        ReplaceModifiersState state = default;
        state.Input = &input;
        state.Collection = &collection;
        state.Indices = &indices;
        state.Arguments = &arguments;
        state.ArgumentStartIndexOffset = argumentStartIndexOffset;

        return string.Create(length, state, static (span, state) =>
        {
            ReadOnlySpan<char> input = *state.Input;
            ReadOnlySpan<char> collection = *state.Collection;
            ReadOnlySpan<int> indices = *state.Indices;
            ReadOnlySpan<ArgumentSpan> arguments = *state.Arguments;

            int argCt = arguments.Length;
            int outputIndex = 0;
            int index = 0;
            for (int i = 0; i < argCt; ++i)
            {
                ref readonly ArgumentSpan argSpan = ref arguments[i];
                int segmentIndex = indices[i];
                if (segmentIndex < 0)
                    continue;

                int nextIndex = collection.Length;
                for (int j = i + 1; j < argCt; ++j)
                {
                    int fwdIndex = indices[j];
                    if (fwdIndex < 0)
                        continue;

                    nextIndex = fwdIndex;
                    break;
                }

                int segmentLength = nextIndex - segmentIndex;

                int startIndex = argSpan.StartIndex + state.ArgumentStartIndexOffset;

                int lenToCopy = startIndex - index;
                input.Slice(index, lenToCopy).CopyTo(span[outputIndex..]);
                outputIndex += lenToCopy;

                collection.Slice(segmentIndex, segmentLength).CopyTo(span[outputIndex..]);
                outputIndex += segmentLength;
                index = startIndex + argSpan.Length;
            }

            input.Slice(index, input.Length - index).CopyTo(span[outputIndex..]);
        });
    }
    private unsafe struct ReplaceModifiersState
    {
        public ReadOnlySpan<char>* Input;
        public ReadOnlySpan<char>* Collection;
        public ReadOnlySpan<int>* Indices;
        public ReadOnlySpan<ArgumentSpan>* Arguments;
        public int ArgumentStartIndexOffset;
    }

    /// <summary>
    /// Extract an array of where all the modifiers were and remove them from the original text.
    /// </summary>
    /// <param name="modifierChar">The character to look for.</param>
    public static unsafe ArgumentSpan[] ExtractModifiers(out string? text, ReadOnlySpan<char> input, char modifierChar)
    {
        int count = 0;
        int index = 0;
        ArgumentSpan span = default;
        int totalLength = 0;
        while (true)
        {
            bool foundOne = NextArgumentSpan(input, modifierChar, index, ref span, out int modifierStartIndex, out int modifierLength);
            if (modifierStartIndex == -1)
                break;

            if (!foundOne)
            {
                index = modifierStartIndex + 1;
                continue;
            }

            ++count;
            totalLength += span.Length + (modifierStartIndex - index);
            index = modifierStartIndex + modifierLength;
        }

        if (count == 0)
        {
            text = null;
            return Array.Empty<ArgumentSpan>();
        }

        totalLength += input.Length - index;

        ExtractModifiersState state = default;
        state.Spans = new ArgumentSpan[count];
        state.Input = &input;
        state.ModifierChar = modifierChar;

        text = string.Create(totalLength, state, static (span, state) =>
        {
            ReadOnlySpan<char> input = *state.Input;
            int index = 0;
            int count = 0;
            int outputIndex = 0;
            while (true)
            {
                ArgumentSpan argSpan = default;
                bool foundOne = NextArgumentSpan(input, state.ModifierChar, index, ref argSpan, out int modifierStartIndex, out int modifierLength);
                if (modifierStartIndex == -1)
                    break;

                if (!foundOne)
                {
                    index = modifierStartIndex + 1;
                    continue;
                }

                int lenToCopy = modifierStartIndex - index;
                input.Slice(index, lenToCopy).CopyTo(span[outputIndex..]);
                outputIndex += lenToCopy;

                input.Slice(argSpan.StartIndex, argSpan.Length).CopyTo(span[outputIndex..]);
                argSpan.StartIndex = outputIndex;
                outputIndex += argSpan.Length;

                state.Spans[count] = argSpan;
                index = modifierStartIndex + modifierLength;

                ++count;
            }

            input.Slice(index, input.Length - index).CopyTo(span[outputIndex..]);
        });

        return state.Spans;
    }

    private unsafe struct ExtractModifiersState
    {
        public ReadOnlySpan<char>* Input;
        public ArgumentSpan[] Spans;
        public char ModifierChar;
    }

#pragma warning restore CS8500
    private static bool NextArgumentSpan(ReadOnlySpan<char> span, char modifierChar, int startIndex, ref ArgumentSpan arg, out int modifierStartIndex, out int modifierLength)
    {
        int nextIndex = span.IndexOf('$', startIndex);
        modifierStartIndex = nextIndex;
        if (nextIndex == -1 || nextIndex >= span.Length - 7 || span[nextIndex + 1] != '{')
        {
            modifierLength = 0;
            return false;
        }

        int endIndex = span.IndexOf('}', nextIndex + 2);

        if (endIndex == -1)
        {
            modifierLength = 0;
            return false;
        }

        modifierLength = endIndex - nextIndex + 1;
        if (endIndex - nextIndex < 6)
        {
            return false;
        }

        // ${p:0:word}
        ReadOnlySpan<char> modifier = span.Slice(modifierStartIndex, modifierLength);
        int colon1 = modifier.IndexOf(':', 2);
        if (colon1 == -1)
            return false;

        int colon2 = modifier.IndexOf(':', colon1 + 1);
        if (colon2 == -1)
            return false;

        int letterIndex = modifier.Slice(2, colon1 - 2).IndexOf(modifierChar);
        if (letterIndex == -1)
            return false;

        if (!int.TryParse(modifier.Slice(colon1 + 1, colon2 - colon1 - 1), NumberStyles.Number, CultureInfo.InvariantCulture, out int argIndex))
        {
            return false;
        }

        arg.Inverted = modifier[^2] == '!';
        arg.Argument = argIndex;
        arg.StartIndex = nextIndex + colon2 + 1;
        arg.Length = modifierLength - colon2 - 2 - (arg.Inverted ? 1 : 0);
        return true;
    }
}
