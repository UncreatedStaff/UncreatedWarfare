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

        bool isOne = IsOne(conv);
        return isOne ? text : TranslationPluralizations.Pluralize(text, args.Language);
    }

    private static bool IsOne(IConvertible conv)
    {
        TypeCode tc = conv.GetTypeCode();
        return tc switch
        {
            TypeCode.Boolean => (bool)conv,
            TypeCode.Char => (char)conv == 1,
            TypeCode.SByte => (sbyte)conv == 1,
            TypeCode.Byte => (byte)conv == 1,
            TypeCode.Int16 => (short)conv == 1,
            TypeCode.UInt16 => (ushort)conv == 1,
            TypeCode.Int32 => (int)conv == 1,
            TypeCode.UInt32 => (uint)conv == 1,
            TypeCode.Int64 => (long)conv == 1,
            TypeCode.UInt64 => (ulong)conv == 1,
            TypeCode.Single => Math.Abs((float)conv - 1) <= float.Epsilon,
            TypeCode.Double => Math.Abs((double)conv - 1) <= double.Epsilon,
            TypeCode.Decimal => ((decimal)conv).Equals(1m),
            TypeCode.DateTime => ((DateTime)conv).Ticks == 1,
            TypeCode.String => ((string)conv).Equals("1", StringComparison.InvariantCultureIgnoreCase) ||
                               ((string)conv).Equals("one", StringComparison.InvariantCultureIgnoreCase),
            _ => false
        };
    }
}
