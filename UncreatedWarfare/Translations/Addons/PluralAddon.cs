using System;
using System.Globalization;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Translations.Addons;
public sealed class PluralAddon : IArgumentAddon
{
    private static readonly PluralAddon?[] Instances = new PluralAddon?[10];
    private static readonly PluralAddon PluralAlways = new PluralAddon(-1);

    private readonly int _argIndex;
    public string DisplayName { get; }

    /// <summary>
    /// This argument will become plural when <paramref name="argIndex"/> isn't one if it's numeric.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="argIndex"/> is less than zero.</exception>
    public static PluralAddon WhenArgument(int argIndex)
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

    /// <summary>
    /// This argument will become plural no matter the values of other parameters.
    /// </summary>
    public static PluralAddon Always()
    {
        return PluralAlways;
    }

    private PluralAddon(int argIndex)
    {
        _argIndex = argIndex;
        DisplayName = argIndex < 0 ? "Always Plural" : $"Plural if {{{argIndex.ToString(CultureInfo.InvariantCulture)}}} ≠ 1.";
    }

    public string ApplyAddon(ITranslationValueFormatter formatter, string text, TypedReference value, in ValueFormatParameters args)
    {
        if (!args.Language.SupportsPluralization)
            return text;

        if (_argIndex < 0)
        {
            return TranslationPluralizations.Pluralize(text, args.Language);
        }

        if (args.ArgumentAccessor == null || _argIndex >= args.ArgumentCount)
            return text;

        object? arg = args.ArgumentAccessor(_argIndex);
        if (arg is not IConvertible conv)
            return text;

        bool isOne = TranslationPluralizations.IsOne(conv);
        return isOne ? text : TranslationPluralizations.Pluralize(text, args.Language);
    }

    public static implicit operator ArgumentFormat(PluralAddon addon) => new ArgumentFormat(addon);
}