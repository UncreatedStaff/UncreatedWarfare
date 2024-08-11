using System;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Translations.Addons;
public sealed class PluralAddon : IArgumentAddon
{
    private readonly int _argIndex;
    private static readonly PluralAddon?[] Instances = new PluralAddon?[10];
    
    /// <summary>
    /// This argument will become plural when <paramref name="argIndex"/> isn't one if it's numeric.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="argIndex"/> is less than zero.</exception>
    public PluralAddon WhenArgument(int argIndex)
    {
        if (argIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(argIndex));

        if (argIndex > 9)
            return new PluralAddon(argIndex);

        PluralAddon? addon = Volatile.Read(ref Instances[argIndex]);
        if (addon != null)
            return addon;

        addon = new PluralAddon(argIndex);
        Volatile.Write(ref Instances[argIndex], addon);
        return addon;
    }

    private PluralAddon(int argIndex)
    {
        _argIndex = argIndex;
    }

    public string ApplyAddon(string text, in ValueFormatParameters args)
    {
        if (!args.Language.SupportsPluralization || args.ArgumentAccessor == null || _argIndex >= args.ArgumentCount)
            return text;

        object? arg = args.ArgumentAccessor(_argIndex);
        if (arg is not IConvertible conv)
            return text;

        bool isOne = TranslationPluralizations.IsOne(conv);
        return isOne ? text : TranslationPluralizations.Pluralize(text, args.Language);
    }
}