using System;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Translations;

// going to add more later, figure its easier to get everything working with 2 instead of a bunch, then duplicate it later
public class Translation<T0> : Translation
{
    [ThreadStatic]
    private static ArgumentAccessor? _argAccessor;

    public override int ArgumentCount => 1;
    public ArgumentFormat FormatArg0 { get; set; }

    public Translation(string defaultValue, TranslationOptions options = default, ArgumentFormat arg0Fmt = default)
        : base(defaultValue, options)
    {
        arg0Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        FormatArg0 = arg0Fmt;
    }

    public string Translate(scoped in TranslationArguments args, T0? arg0)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;

        ValueFormatParameters p = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 1);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p);

        ArgumentSpan[] pluralizers = args.ValueSet.GetPluralizations(in args, out int argOffset);

        ReadOnlySpan<char> preformattedValue = args.PreformattedValue;
        if (pluralizers.Length > 0)
        {
            preformattedValue = ApplyPluralizers(in args, pluralizers, argOffset, 1, _argAccessor.AccessFunc);
        }

        return TranslationFormattingUtility.FormatString(preformattedValue, f0.AsSpan(), default);
    }

    protected override string UnsafeTranslateIntl(in TranslationArguments arguments, object?[] formattingParameters)
    {
        return Translate(in arguments, (T0?)formattingParameters[0]);
    }

    public override ArgumentFormat GetArgumentFormat(int index)
    {
        if (index != 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        return FormatArg0;
    }

    private class ArgumentAccessor
    {
        public T0? Arg0;
        public readonly Func<int, object?> AccessFunc;
        public ArgumentAccessor() { AccessFunc = Access; }
        private object? Access(int index)
        {
            return index == 0 ? Arg0 : null;
        }
    }
}

public class Translation<T0, T1> : Translation
{
    [ThreadStatic]
    private static ArgumentAccessor? _argAccessor;
    public override int ArgumentCount => 2;
    public ArgumentFormat FormatArg0 { get; set; }
    public ArgumentFormat FormatArg1 { get; set; }
    public Translation(string defaultValue, TranslationOptions options = default, ArgumentFormat arg0Fmt = default, ArgumentFormat arg1Fmt = default)
        : base(defaultValue, options)
    {
        arg0Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg1Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();

        FormatArg0 = arg0Fmt;
        FormatArg1 = arg1Fmt;
    }

    public string Translate(scoped in TranslationArguments args, T0? arg0, T1? arg1)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;
        _argAccessor.Arg1 = arg1;

        ValueFormatParameters p0 = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 2);
        ValueFormatParameters p1 = new ValueFormatParameters(1, in args, FormatArg1, _argAccessor.AccessFunc, 2);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p0);
        string f1 = TranslationService.ValueFormatter.Format(arg1, in p1);

        Span<int> indices = stackalloc int[1];
        indices[0] = f0.Length;

        Span<char> collectionSpan = stackalloc char[indices[0] + f1.Length];
        f0.AsSpan().CopyTo(collectionSpan);
        f1.AsSpan().CopyTo(collectionSpan[indices[0]..]);

        ArgumentSpan[] pluralizers = args.ValueSet.GetPluralizations(in args, out int argOffset);
        ReadOnlySpan<char> preformattedValue = args.PreformattedValue;
        if (pluralizers.Length > 0)
        {
            preformattedValue = ApplyPluralizers(in args, pluralizers, argOffset, 2, _argAccessor.AccessFunc);
        }

        return TranslationFormattingUtility.FormatString(preformattedValue, collectionSpan, indices);
    }

    protected override string UnsafeTranslateIntl(in TranslationArguments arguments, object?[] formattingParameters)
    {
        return Translate(
            in arguments,
            (T0?)formattingParameters[0],
            (T1?)formattingParameters[1]
        );
    }

    public override ArgumentFormat GetArgumentFormat(int index)
    {
        return index switch
        {
            0 => FormatArg0,
            1 => FormatArg1,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private class ArgumentAccessor
    {
        public T0? Arg0;
        public T1? Arg1;
        public readonly Func<int, object?> AccessFunc;
        public ArgumentAccessor() { AccessFunc = Access; }
        private object? Access(int index)
        {
            return index switch
            {
                0 => Arg0,
                1 => Arg1,
                _ => null
            };
        }
    }
}
public class Translation<T0, T1, T2> : Translation
{
    [ThreadStatic]
    private static ArgumentAccessor? _argAccessor;
    public override int ArgumentCount => 3;
    public ArgumentFormat FormatArg0 { get; set; }
    public ArgumentFormat FormatArg1 { get; set; }
    public ArgumentFormat FormatArg2 { get; set; }
    public Translation(string defaultValue, TranslationOptions options = default, ArgumentFormat arg0Fmt = default, ArgumentFormat arg1Fmt = default, ArgumentFormat arg2Fmt = default)
        : base(defaultValue, options)
    {
        arg0Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg1Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg2Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();

        FormatArg0 = arg0Fmt;
        FormatArg1 = arg1Fmt;
        FormatArg2 = arg2Fmt;
    }

    public string Translate(scoped in TranslationArguments args, T0? arg0, T1? arg1, T2? arg2)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;
        _argAccessor.Arg1 = arg1;
        _argAccessor.Arg2 = arg2;

        ValueFormatParameters p0 = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 3);
        ValueFormatParameters p1 = new ValueFormatParameters(1, in args, FormatArg1, _argAccessor.AccessFunc, 3);
        ValueFormatParameters p2 = new ValueFormatParameters(2, in args, FormatArg2, _argAccessor.AccessFunc, 3);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p0);
        string f1 = TranslationService.ValueFormatter.Format(arg1, in p1);
        string f2 = TranslationService.ValueFormatter.Format(arg2, in p2);

        Span<int> indices = stackalloc int[2];
        indices[0] = f0.Length;
        indices[1] = indices[0] + f1.Length;

        Span<char> collectionSpan = stackalloc char[indices[1] + f2.Length];
        f0.AsSpan().CopyTo(collectionSpan);
        f1.AsSpan().CopyTo(collectionSpan[indices[0]..]);
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);

        ArgumentSpan[] pluralizers = args.ValueSet.GetPluralizations(in args, out int argOffset);
        ReadOnlySpan<char> preformattedValue = args.PreformattedValue;
        if (pluralizers.Length > 0)
        {
            preformattedValue = ApplyPluralizers(in args, pluralizers, argOffset, 3, _argAccessor.AccessFunc);
        }

        return TranslationFormattingUtility.FormatString(preformattedValue, collectionSpan, indices);
    }

    protected override string UnsafeTranslateIntl(in TranslationArguments arguments, object?[] formattingParameters)
    {
        return Translate(
            in arguments,
            (T0?)formattingParameters[0],
            (T1?)formattingParameters[1],
            (T2?)formattingParameters[2]
        );
    }

    public override ArgumentFormat GetArgumentFormat(int index)
    {
        return index switch
        {
            0 => FormatArg0,
            1 => FormatArg1,
            2 => FormatArg2,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private class ArgumentAccessor
    {
        public T0? Arg0;
        public T1? Arg1;
        public T2? Arg2;
        public readonly Func<int, object?> AccessFunc;
        public ArgumentAccessor() { AccessFunc = Access; }
        private object? Access(int index)
        {
            return index switch
            {
                0 => Arg0,
                1 => Arg1,
                2 => Arg2,
                _ => null
            };
        }
    }
}
public class Translation<T0, T1, T2, T3> : Translation
{
    [ThreadStatic]
    private static ArgumentAccessor? _argAccessor;
    public override int ArgumentCount => 4;
    public ArgumentFormat FormatArg0 { get; set; }
    public ArgumentFormat FormatArg1 { get; set; }
    public ArgumentFormat FormatArg2 { get; set; }
    public ArgumentFormat FormatArg3 { get; set; }
    public Translation(string defaultValue, TranslationOptions options = default, ArgumentFormat arg0Fmt = default, ArgumentFormat arg1Fmt = default, ArgumentFormat arg2Fmt = default, ArgumentFormat arg3Fmt = default)
        : base(defaultValue, options)
    {
        arg0Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg1Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg2Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg3Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();

        FormatArg0 = arg0Fmt;
        FormatArg1 = arg1Fmt;
        FormatArg2 = arg2Fmt;
        FormatArg3 = arg3Fmt;
    }

    public string Translate(scoped in TranslationArguments args, T0? arg0, T1? arg1, T2? arg2, T3? arg3)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;
        _argAccessor.Arg1 = arg1;
        _argAccessor.Arg2 = arg2;
        _argAccessor.Arg3 = arg3;

        ValueFormatParameters p0 = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 4);
        ValueFormatParameters p1 = new ValueFormatParameters(1, in args, FormatArg1, _argAccessor.AccessFunc, 4);
        ValueFormatParameters p2 = new ValueFormatParameters(2, in args, FormatArg2, _argAccessor.AccessFunc, 4);
        ValueFormatParameters p3 = new ValueFormatParameters(3, in args, FormatArg3, _argAccessor.AccessFunc, 4);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p0);
        string f1 = TranslationService.ValueFormatter.Format(arg1, in p1);
        string f2 = TranslationService.ValueFormatter.Format(arg2, in p2);
        string f3 = TranslationService.ValueFormatter.Format(arg3, in p3);

        Span<int> indices = stackalloc int[3];
        indices[0] = f0.Length;
        indices[1] = indices[0] + f1.Length;
        indices[2] = indices[1] + f2.Length;

        Span<char> collectionSpan = stackalloc char[indices[2] + f3.Length];
        f0.AsSpan().CopyTo(collectionSpan);
        f1.AsSpan().CopyTo(collectionSpan[indices[0]..]);
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);
        f3.AsSpan().CopyTo(collectionSpan[indices[2]..]);

        ArgumentSpan[] pluralizers = args.ValueSet.GetPluralizations(in args, out int argOffset);
        ReadOnlySpan<char> preformattedValue = args.PreformattedValue;
        if (pluralizers.Length > 0)
        {
            preformattedValue = ApplyPluralizers(in args, pluralizers, argOffset, 4, _argAccessor.AccessFunc);
        }

        return TranslationFormattingUtility.FormatString(preformattedValue, collectionSpan, indices);
    }

    protected override string UnsafeTranslateIntl(in TranslationArguments arguments, object?[] formattingParameters)
    {
        return Translate(
            in arguments,
            (T0?)formattingParameters[0],
            (T1?)formattingParameters[1],
            (T2?)formattingParameters[2],
            (T3?)formattingParameters[3]
        );
    }

    public override ArgumentFormat GetArgumentFormat(int index)
    {
        return index switch
        {
            0 => FormatArg0,
            1 => FormatArg1,
            2 => FormatArg2,
            3 => FormatArg3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private class ArgumentAccessor
    {
        public T0? Arg0;
        public T1? Arg1;
        public T2? Arg2;
        public T3? Arg3;
        public readonly Func<int, object?> AccessFunc;
        public ArgumentAccessor() { AccessFunc = Access; }
        private object? Access(int index)
        {
            return index switch
            {
                0 => Arg0,
                1 => Arg1,
                2 => Arg2,
                3 => Arg3,
                _ => null
            };
        }
    }
}
public class Translation<T0, T1, T2, T3, T4> : Translation
{
    [ThreadStatic]
    private static ArgumentAccessor? _argAccessor;
    public override int ArgumentCount => 5;
    public ArgumentFormat FormatArg0 { get; set; }
    public ArgumentFormat FormatArg1 { get; set; }
    public ArgumentFormat FormatArg2 { get; set; }
    public ArgumentFormat FormatArg3 { get; set; }
    public ArgumentFormat FormatArg4 { get; set; }
    public Translation(string defaultValue, TranslationOptions options = default, ArgumentFormat arg0Fmt = default, ArgumentFormat arg1Fmt = default, ArgumentFormat arg2Fmt = default, ArgumentFormat arg3Fmt = default, ArgumentFormat arg4Fmt = default)
        : base(defaultValue, options)
    {
        arg0Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg1Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg2Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg3Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg4Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();

        FormatArg0 = arg0Fmt;
        FormatArg1 = arg1Fmt;
        FormatArg2 = arg2Fmt;
        FormatArg3 = arg3Fmt;
        FormatArg4 = arg4Fmt;
    }

    protected override string UnsafeTranslateIntl(in TranslationArguments arguments, object?[] formattingParameters)
    {
        return Translate(
            in arguments,
            (T0?)formattingParameters[0],
            (T1?)formattingParameters[1],
            (T2?)formattingParameters[2],
            (T3?)formattingParameters[3],
            (T4?)formattingParameters[4]
        );
    }

    public string Translate(scoped in TranslationArguments args, T0? arg0, T1? arg1, T2? arg2, T3? arg3, T4? arg4)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;
        _argAccessor.Arg1 = arg1;
        _argAccessor.Arg2 = arg2;
        _argAccessor.Arg3 = arg3;
        _argAccessor.Arg4 = arg4;

        ValueFormatParameters p0 = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 5);
        ValueFormatParameters p1 = new ValueFormatParameters(1, in args, FormatArg1, _argAccessor.AccessFunc, 5);
        ValueFormatParameters p2 = new ValueFormatParameters(2, in args, FormatArg2, _argAccessor.AccessFunc, 5);
        ValueFormatParameters p3 = new ValueFormatParameters(3, in args, FormatArg3, _argAccessor.AccessFunc, 5);
        ValueFormatParameters p4 = new ValueFormatParameters(4, in args, FormatArg4, _argAccessor.AccessFunc, 5);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p0);
        string f1 = TranslationService.ValueFormatter.Format(arg1, in p1);
        string f2 = TranslationService.ValueFormatter.Format(arg2, in p2);
        string f3 = TranslationService.ValueFormatter.Format(arg3, in p3);
        string f4 = TranslationService.ValueFormatter.Format(arg4, in p4);

        Span<int> indices = stackalloc int[4];
        indices[0] = f0.Length;
        indices[1] = indices[0] + f1.Length;
        indices[2] = indices[1] + f2.Length;
        indices[3] = indices[2] + f3.Length;

        Span<char> collectionSpan = stackalloc char[indices[3] + f4.Length];
        f0.AsSpan().CopyTo(collectionSpan);
        f1.AsSpan().CopyTo(collectionSpan[indices[0]..]);
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);
        f3.AsSpan().CopyTo(collectionSpan[indices[2]..]);
        f4.AsSpan().CopyTo(collectionSpan[indices[3]..]);

        ArgumentSpan[] pluralizers = args.ValueSet.GetPluralizations(in args, out int argOffset);
        ReadOnlySpan<char> preformattedValue = args.PreformattedValue;
        if (pluralizers.Length > 0)
        {
            preformattedValue = ApplyPluralizers(in args, pluralizers, argOffset, 5, _argAccessor.AccessFunc);
        }

        return TranslationFormattingUtility.FormatString(preformattedValue, collectionSpan, indices);
    }

    public override ArgumentFormat GetArgumentFormat(int index)
    {
        return index switch
        {
            0 => FormatArg0,
            1 => FormatArg1,
            2 => FormatArg2,
            3 => FormatArg3,
            4 => FormatArg4,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private class ArgumentAccessor
    {
        public T0? Arg0;
        public T1? Arg1;
        public T2? Arg2;
        public T3? Arg3;
        public T4? Arg4;
        public readonly Func<int, object?> AccessFunc;
        public ArgumentAccessor() { AccessFunc = Access; }
        private object? Access(int index)
        {
            return index switch
            {
                0 => Arg0,
                1 => Arg1,
                2 => Arg2,
                3 => Arg3,
                4 => Arg4,
                _ => null
            };
        }
    }
}
public class Translation<T0, T1, T2, T3, T4, T5> : Translation
{
    [ThreadStatic]
    private static ArgumentAccessor? _argAccessor;
    public override int ArgumentCount => 6;
    public ArgumentFormat FormatArg0 { get; set; }
    public ArgumentFormat FormatArg1 { get; set; }
    public ArgumentFormat FormatArg2 { get; set; }
    public ArgumentFormat FormatArg3 { get; set; }
    public ArgumentFormat FormatArg4 { get; set; }
    public ArgumentFormat FormatArg5 { get; set; }
    public Translation(string defaultValue, TranslationOptions options = default, ArgumentFormat arg0Fmt = default, ArgumentFormat arg1Fmt = default, ArgumentFormat arg2Fmt = default, ArgumentFormat arg3Fmt = default, ArgumentFormat arg4Fmt = default, ArgumentFormat arg5Fmt = default)
        : base(defaultValue, options)
    {
        arg0Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg1Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg2Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg3Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg4Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg5Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();

        FormatArg0 = arg0Fmt;
        FormatArg1 = arg1Fmt;
        FormatArg2 = arg2Fmt;
        FormatArg3 = arg3Fmt;
        FormatArg4 = arg4Fmt;
        FormatArg5 = arg5Fmt;
    }

    public string Translate(scoped in TranslationArguments args, T0? arg0, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;
        _argAccessor.Arg1 = arg1;
        _argAccessor.Arg2 = arg2;
        _argAccessor.Arg3 = arg3;
        _argAccessor.Arg4 = arg4;
        _argAccessor.Arg5 = arg5;

        ValueFormatParameters p0 = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 6);
        ValueFormatParameters p1 = new ValueFormatParameters(1, in args, FormatArg1, _argAccessor.AccessFunc, 6);
        ValueFormatParameters p2 = new ValueFormatParameters(2, in args, FormatArg2, _argAccessor.AccessFunc, 6);
        ValueFormatParameters p3 = new ValueFormatParameters(3, in args, FormatArg3, _argAccessor.AccessFunc, 6);
        ValueFormatParameters p4 = new ValueFormatParameters(4, in args, FormatArg4, _argAccessor.AccessFunc, 6);
        ValueFormatParameters p5 = new ValueFormatParameters(5, in args, FormatArg5, _argAccessor.AccessFunc, 6);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p0);
        string f1 = TranslationService.ValueFormatter.Format(arg1, in p1);
        string f2 = TranslationService.ValueFormatter.Format(arg2, in p2);
        string f3 = TranslationService.ValueFormatter.Format(arg3, in p3);
        string f4 = TranslationService.ValueFormatter.Format(arg4, in p4);
        string f5 = TranslationService.ValueFormatter.Format(arg5, in p5);

        Span<int> indices = stackalloc int[5];
        indices[0] = f0.Length;
        indices[1] = indices[0] + f1.Length;
        indices[2] = indices[1] + f2.Length;
        indices[3] = indices[2] + f3.Length;
        indices[4] = indices[3] + f4.Length;

        Span<char> collectionSpan = stackalloc char[indices[4] + f5.Length];
        f0.AsSpan().CopyTo(collectionSpan);
        f1.AsSpan().CopyTo(collectionSpan[indices[0]..]);
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);
        f3.AsSpan().CopyTo(collectionSpan[indices[2]..]);
        f4.AsSpan().CopyTo(collectionSpan[indices[3]..]);
        f5.AsSpan().CopyTo(collectionSpan[indices[4]..]);

        ArgumentSpan[] pluralizers = args.ValueSet.GetPluralizations(in args, out int argOffset);
        ReadOnlySpan<char> preformattedValue = args.PreformattedValue;
        if (pluralizers.Length > 0)
        {
            preformattedValue = ApplyPluralizers(in args, pluralizers, argOffset, 6, _argAccessor.AccessFunc);
        }

        return TranslationFormattingUtility.FormatString(preformattedValue, collectionSpan, indices);
    }

    protected override string UnsafeTranslateIntl(in TranslationArguments arguments, object?[] formattingParameters)
    {
        return Translate(
            in arguments,
            (T0?)formattingParameters[0],
            (T1?)formattingParameters[1],
            (T2?)formattingParameters[2],
            (T3?)formattingParameters[3],
            (T4?)formattingParameters[4],
            (T5?)formattingParameters[5]
        );
    }

    public override ArgumentFormat GetArgumentFormat(int index)
    {
        return index switch
        {
            0 => FormatArg0,
            1 => FormatArg1,
            2 => FormatArg2,
            3 => FormatArg3,
            4 => FormatArg4,
            5 => FormatArg5,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private class ArgumentAccessor
    {
        public T0? Arg0;
        public T1? Arg1;
        public T2? Arg2;
        public T3? Arg3;
        public T4? Arg4;
        public T5? Arg5;
        public readonly Func<int, object?> AccessFunc;
        public ArgumentAccessor() { AccessFunc = Access; }
        private object? Access(int index)
        {
            return index switch
            {
                0 => Arg0,
                1 => Arg1,
                2 => Arg2,
                3 => Arg3,
                4 => Arg4,
                5 => Arg5,
                _ => null
            };
        }
    }
}
public class Translation<T0, T1, T2, T3, T4, T5, T6> : Translation
{
    [ThreadStatic]
    private static ArgumentAccessor? _argAccessor;
    public override int ArgumentCount => 7;
    public ArgumentFormat FormatArg0 { get; set; }
    public ArgumentFormat FormatArg1 { get; set; }
    public ArgumentFormat FormatArg2 { get; set; }
    public ArgumentFormat FormatArg3 { get; set; }
    public ArgumentFormat FormatArg4 { get; set; }
    public ArgumentFormat FormatArg5 { get; set; }
    public ArgumentFormat FormatArg6 { get; set; }
    public Translation(string defaultValue, TranslationOptions options = default, ArgumentFormat arg0Fmt = default, ArgumentFormat arg1Fmt = default, ArgumentFormat arg2Fmt = default, ArgumentFormat arg3Fmt = default, ArgumentFormat arg4Fmt = default, ArgumentFormat arg5Fmt = default, ArgumentFormat arg6Fmt = default)
        : base(defaultValue, options)
    {
        arg0Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg1Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg2Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg3Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg4Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg5Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg6Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();

        FormatArg0 = arg0Fmt;
        FormatArg1 = arg1Fmt;
        FormatArg2 = arg2Fmt;
        FormatArg3 = arg3Fmt;
        FormatArg4 = arg4Fmt;
        FormatArg5 = arg5Fmt;
        FormatArg6 = arg6Fmt;
    }

    public string Translate(scoped in TranslationArguments args, T0? arg0, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;
        _argAccessor.Arg1 = arg1;
        _argAccessor.Arg2 = arg2;
        _argAccessor.Arg3 = arg3;
        _argAccessor.Arg4 = arg4;
        _argAccessor.Arg5 = arg5;
        _argAccessor.Arg6 = arg6;

        ValueFormatParameters p0 = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 7);
        ValueFormatParameters p1 = new ValueFormatParameters(1, in args, FormatArg1, _argAccessor.AccessFunc, 7);
        ValueFormatParameters p2 = new ValueFormatParameters(2, in args, FormatArg2, _argAccessor.AccessFunc, 7);
        ValueFormatParameters p3 = new ValueFormatParameters(3, in args, FormatArg3, _argAccessor.AccessFunc, 7);
        ValueFormatParameters p4 = new ValueFormatParameters(4, in args, FormatArg4, _argAccessor.AccessFunc, 7);
        ValueFormatParameters p5 = new ValueFormatParameters(5, in args, FormatArg5, _argAccessor.AccessFunc, 7);
        ValueFormatParameters p6 = new ValueFormatParameters(6, in args, FormatArg6, _argAccessor.AccessFunc, 7);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p0);
        string f1 = TranslationService.ValueFormatter.Format(arg1, in p1);
        string f2 = TranslationService.ValueFormatter.Format(arg2, in p2);
        string f3 = TranslationService.ValueFormatter.Format(arg3, in p3);
        string f4 = TranslationService.ValueFormatter.Format(arg4, in p4);
        string f5 = TranslationService.ValueFormatter.Format(arg5, in p5);
        string f6 = TranslationService.ValueFormatter.Format(arg6, in p6);

        Span<int> indices = stackalloc int[6];
        indices[0] = f0.Length;
        indices[1] = indices[0] + f1.Length;
        indices[2] = indices[1] + f2.Length;
        indices[3] = indices[2] + f3.Length;
        indices[4] = indices[3] + f4.Length;
        indices[5] = indices[4] + f5.Length;

        Span<char> collectionSpan = stackalloc char[indices[5] + f6.Length];
        f0.AsSpan().CopyTo(collectionSpan);
        f1.AsSpan().CopyTo(collectionSpan[indices[0]..]);
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);
        f3.AsSpan().CopyTo(collectionSpan[indices[2]..]);
        f4.AsSpan().CopyTo(collectionSpan[indices[3]..]);
        f5.AsSpan().CopyTo(collectionSpan[indices[4]..]);
        f6.AsSpan().CopyTo(collectionSpan[indices[5]..]);

        ArgumentSpan[] pluralizers = args.ValueSet.GetPluralizations(in args, out int argOffset);
        ReadOnlySpan<char> preformattedValue = args.PreformattedValue;
        if (pluralizers.Length > 0)
        {
            preformattedValue = ApplyPluralizers(in args, pluralizers, argOffset, 7, _argAccessor.AccessFunc);
        }

        return TranslationFormattingUtility.FormatString(preformattedValue, collectionSpan, indices);
    }

    protected override string UnsafeTranslateIntl(in TranslationArguments arguments, object?[] formattingParameters)
    {
        return Translate(
            in arguments,
            (T0?)formattingParameters[0],
            (T1?)formattingParameters[1],
            (T2?)formattingParameters[2],
            (T3?)formattingParameters[3],
            (T4?)formattingParameters[4],
            (T5?)formattingParameters[5],
            (T6?)formattingParameters[6]
        );
    }

    public override ArgumentFormat GetArgumentFormat(int index)
    {
        return index switch
        {
            0 => FormatArg0,
            1 => FormatArg1,
            2 => FormatArg2,
            3 => FormatArg3,
            4 => FormatArg4,
            5 => FormatArg5,
            6 => FormatArg6,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private class ArgumentAccessor
    {
        public T0? Arg0;
        public T1? Arg1;
        public T2? Arg2;
        public T3? Arg3;
        public T4? Arg4;
        public T5? Arg5;
        public T6? Arg6;
        public readonly Func<int, object?> AccessFunc;
        public ArgumentAccessor() { AccessFunc = Access; }
        private object? Access(int index)
        {
            return index switch
            {
                0 => Arg0,
                1 => Arg1,
                2 => Arg2,
                3 => Arg3,
                4 => Arg4,
                5 => Arg5,
                6 => Arg6,
                _ => null
            };
        }
    }
}
public class Translation<T0, T1, T2, T3, T4, T5, T6, T7> : Translation
{
    [ThreadStatic]
    private static ArgumentAccessor? _argAccessor;
    public override int ArgumentCount => 8;
    public ArgumentFormat FormatArg0 { get; set; }
    public ArgumentFormat FormatArg1 { get; set; }
    public ArgumentFormat FormatArg2 { get; set; }
    public ArgumentFormat FormatArg3 { get; set; }
    public ArgumentFormat FormatArg4 { get; set; }
    public ArgumentFormat FormatArg5 { get; set; }
    public ArgumentFormat FormatArg6 { get; set; }
    public ArgumentFormat FormatArg7 { get; set; }
    public Translation(string defaultValue, TranslationOptions options = default, ArgumentFormat arg0Fmt = default, ArgumentFormat arg1Fmt = default, ArgumentFormat arg2Fmt = default, ArgumentFormat arg3Fmt = default, ArgumentFormat arg4Fmt = default, ArgumentFormat arg5Fmt = default, ArgumentFormat arg6Fmt = default, ArgumentFormat arg7Fmt = default)
        : base(defaultValue, options)
    {
        arg0Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg1Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg2Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg3Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg4Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg5Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg6Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg7Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();

        FormatArg0 = arg0Fmt;
        FormatArg1 = arg1Fmt;
        FormatArg2 = arg2Fmt;
        FormatArg3 = arg3Fmt;
        FormatArg4 = arg4Fmt;
        FormatArg5 = arg5Fmt;
        FormatArg6 = arg6Fmt;
        FormatArg7 = arg7Fmt;
    }

    public string Translate(scoped in TranslationArguments args, T0? arg0, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;
        _argAccessor.Arg1 = arg1;
        _argAccessor.Arg2 = arg2;
        _argAccessor.Arg3 = arg3;
        _argAccessor.Arg4 = arg4;
        _argAccessor.Arg5 = arg5;
        _argAccessor.Arg6 = arg6;
        _argAccessor.Arg7 = arg7;

        ValueFormatParameters p0 = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 8);
        ValueFormatParameters p1 = new ValueFormatParameters(1, in args, FormatArg1, _argAccessor.AccessFunc, 8);
        ValueFormatParameters p2 = new ValueFormatParameters(2, in args, FormatArg2, _argAccessor.AccessFunc, 8);
        ValueFormatParameters p3 = new ValueFormatParameters(3, in args, FormatArg3, _argAccessor.AccessFunc, 8);
        ValueFormatParameters p4 = new ValueFormatParameters(4, in args, FormatArg4, _argAccessor.AccessFunc, 8);
        ValueFormatParameters p5 = new ValueFormatParameters(5, in args, FormatArg5, _argAccessor.AccessFunc, 8);
        ValueFormatParameters p6 = new ValueFormatParameters(6, in args, FormatArg6, _argAccessor.AccessFunc, 8);
        ValueFormatParameters p7 = new ValueFormatParameters(7, in args, FormatArg7, _argAccessor.AccessFunc, 8);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p0);
        string f1 = TranslationService.ValueFormatter.Format(arg1, in p1);
        string f2 = TranslationService.ValueFormatter.Format(arg2, in p2);
        string f3 = TranslationService.ValueFormatter.Format(arg3, in p3);
        string f4 = TranslationService.ValueFormatter.Format(arg4, in p4);
        string f5 = TranslationService.ValueFormatter.Format(arg5, in p5);
        string f6 = TranslationService.ValueFormatter.Format(arg6, in p6);
        string f7 = TranslationService.ValueFormatter.Format(arg7, in p7);

        Span<int> indices = stackalloc int[7];
        indices[0] = f0.Length;
        indices[1] = indices[0] + f1.Length;
        indices[2] = indices[1] + f2.Length;
        indices[3] = indices[2] + f3.Length;
        indices[4] = indices[3] + f4.Length;
        indices[5] = indices[4] + f5.Length;
        indices[6] = indices[5] + f6.Length;

        Span<char> collectionSpan = stackalloc char[indices[6] + f7.Length];
        f0.AsSpan().CopyTo(collectionSpan);
        f1.AsSpan().CopyTo(collectionSpan[indices[0]..]);
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);
        f3.AsSpan().CopyTo(collectionSpan[indices[2]..]);
        f4.AsSpan().CopyTo(collectionSpan[indices[3]..]);
        f5.AsSpan().CopyTo(collectionSpan[indices[4]..]);
        f6.AsSpan().CopyTo(collectionSpan[indices[5]..]);
        f7.AsSpan().CopyTo(collectionSpan[indices[6]..]);

        ArgumentSpan[] pluralizers = args.ValueSet.GetPluralizations(in args, out int argOffset);
        ReadOnlySpan<char> preformattedValue = args.PreformattedValue;
        if (pluralizers.Length > 0)
        {
            preformattedValue = ApplyPluralizers(in args, pluralizers, argOffset, 8, _argAccessor.AccessFunc);
        }

        return TranslationFormattingUtility.FormatString(preformattedValue, collectionSpan, indices);
    }

    protected override string UnsafeTranslateIntl(in TranslationArguments arguments, object?[] formattingParameters)
    {
        return Translate(
            in arguments,
            (T0?)formattingParameters[0],
            (T1?)formattingParameters[1],
            (T2?)formattingParameters[2],
            (T3?)formattingParameters[3],
            (T4?)formattingParameters[4],
            (T5?)formattingParameters[5],
            (T6?)formattingParameters[6],
            (T7?)formattingParameters[7]
        );
    }

    public override ArgumentFormat GetArgumentFormat(int index)
    {
        return index switch
        {
            0 => FormatArg0,
            1 => FormatArg1,
            2 => FormatArg2,
            3 => FormatArg3,
            4 => FormatArg4,
            5 => FormatArg5,
            6 => FormatArg6,
            7 => FormatArg7,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private class ArgumentAccessor
    {
        public T0? Arg0;
        public T1? Arg1;
        public T2? Arg2;
        public T3? Arg3;
        public T4? Arg4;
        public T5? Arg5;
        public T6? Arg6;
        public T7? Arg7;
        public readonly Func<int, object?> AccessFunc;
        public ArgumentAccessor() { AccessFunc = Access; }
        private object? Access(int index)
        {
            return index switch
            {
                0 => Arg0,
                1 => Arg1,
                2 => Arg2,
                3 => Arg3,
                4 => Arg4,
                5 => Arg5,
                6 => Arg6,
                7 => Arg7,
                _ => null
            };
        }
    }
}
public class Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> : Translation
{
    [ThreadStatic]
    private static ArgumentAccessor? _argAccessor;
    public override int ArgumentCount => 9;
    public ArgumentFormat FormatArg0 { get; set; }
    public ArgumentFormat FormatArg1 { get; set; }
    public ArgumentFormat FormatArg2 { get; set; }
    public ArgumentFormat FormatArg3 { get; set; }
    public ArgumentFormat FormatArg4 { get; set; }
    public ArgumentFormat FormatArg5 { get; set; }
    public ArgumentFormat FormatArg6 { get; set; }
    public ArgumentFormat FormatArg7 { get; set; }
    public ArgumentFormat FormatArg8 { get; set; }
    public Translation(string defaultValue, TranslationOptions options = default, ArgumentFormat arg0Fmt = default, ArgumentFormat arg1Fmt = default, ArgumentFormat arg2Fmt = default, ArgumentFormat arg3Fmt = default, ArgumentFormat arg4Fmt = default, ArgumentFormat arg5Fmt = default, ArgumentFormat arg6Fmt = default, ArgumentFormat arg7Fmt = default, ArgumentFormat arg8Fmt = default)
        : base(defaultValue, options)
    {
        arg0Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg1Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg2Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg3Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg4Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg5Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg6Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg7Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg8Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();

        FormatArg0 = arg0Fmt;
        FormatArg1 = arg1Fmt;
        FormatArg2 = arg2Fmt;
        FormatArg3 = arg3Fmt;
        FormatArg4 = arg4Fmt;
        FormatArg5 = arg5Fmt;
        FormatArg6 = arg6Fmt;
        FormatArg7 = arg7Fmt;
        FormatArg8 = arg8Fmt;
    }

    public string Translate(scoped in TranslationArguments args, T0? arg0, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;
        _argAccessor.Arg1 = arg1;
        _argAccessor.Arg2 = arg2;
        _argAccessor.Arg3 = arg3;
        _argAccessor.Arg4 = arg4;
        _argAccessor.Arg5 = arg5;
        _argAccessor.Arg6 = arg6;
        _argAccessor.Arg7 = arg7;
        _argAccessor.Arg8 = arg8;

        ValueFormatParameters p0 = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 9);
        ValueFormatParameters p1 = new ValueFormatParameters(1, in args, FormatArg1, _argAccessor.AccessFunc, 9);
        ValueFormatParameters p2 = new ValueFormatParameters(2, in args, FormatArg2, _argAccessor.AccessFunc, 9);
        ValueFormatParameters p3 = new ValueFormatParameters(3, in args, FormatArg3, _argAccessor.AccessFunc, 9);
        ValueFormatParameters p4 = new ValueFormatParameters(4, in args, FormatArg4, _argAccessor.AccessFunc, 9);
        ValueFormatParameters p5 = new ValueFormatParameters(5, in args, FormatArg5, _argAccessor.AccessFunc, 9);
        ValueFormatParameters p6 = new ValueFormatParameters(6, in args, FormatArg6, _argAccessor.AccessFunc, 9);
        ValueFormatParameters p7 = new ValueFormatParameters(7, in args, FormatArg7, _argAccessor.AccessFunc, 9);
        ValueFormatParameters p8 = new ValueFormatParameters(8, in args, FormatArg8, _argAccessor.AccessFunc, 9);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p0);
        string f1 = TranslationService.ValueFormatter.Format(arg1, in p1);
        string f2 = TranslationService.ValueFormatter.Format(arg2, in p2);
        string f3 = TranslationService.ValueFormatter.Format(arg3, in p3);
        string f4 = TranslationService.ValueFormatter.Format(arg4, in p4);
        string f5 = TranslationService.ValueFormatter.Format(arg5, in p5);
        string f6 = TranslationService.ValueFormatter.Format(arg6, in p6);
        string f7 = TranslationService.ValueFormatter.Format(arg7, in p7);
        string f8 = TranslationService.ValueFormatter.Format(arg8, in p8);

        Span<int> indices = stackalloc int[8];
        indices[0] = f0.Length;
        indices[1] = indices[0] + f1.Length;
        indices[2] = indices[1] + f2.Length;
        indices[3] = indices[2] + f3.Length;
        indices[4] = indices[3] + f4.Length;
        indices[5] = indices[4] + f5.Length;
        indices[6] = indices[5] + f6.Length;
        indices[7] = indices[6] + f7.Length;

        Span<char> collectionSpan = stackalloc char[indices[7] + f8.Length];
        f0.AsSpan().CopyTo(collectionSpan);
        f1.AsSpan().CopyTo(collectionSpan[indices[0]..]);
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);
        f3.AsSpan().CopyTo(collectionSpan[indices[2]..]);
        f4.AsSpan().CopyTo(collectionSpan[indices[3]..]);
        f5.AsSpan().CopyTo(collectionSpan[indices[4]..]);
        f6.AsSpan().CopyTo(collectionSpan[indices[5]..]);
        f7.AsSpan().CopyTo(collectionSpan[indices[6]..]);
        f8.AsSpan().CopyTo(collectionSpan[indices[7]..]);

        ArgumentSpan[] pluralizers = args.ValueSet.GetPluralizations(in args, out int argOffset);
        ReadOnlySpan<char> preformattedValue = args.PreformattedValue;
        if (pluralizers.Length > 0)
        {
            preformattedValue = ApplyPluralizers(in args, pluralizers, argOffset, 9, _argAccessor.AccessFunc);
        }

        return TranslationFormattingUtility.FormatString(preformattedValue, collectionSpan, indices);
    }

    protected override string UnsafeTranslateIntl(in TranslationArguments arguments, object?[] formattingParameters)
    {
        return Translate(
            in arguments,
            (T0?)formattingParameters[0],
            (T1?)formattingParameters[1],
            (T2?)formattingParameters[2],
            (T3?)formattingParameters[3],
            (T4?)formattingParameters[4],
            (T5?)formattingParameters[5],
            (T6?)formattingParameters[6],
            (T7?)formattingParameters[7],
            (T8?)formattingParameters[8]
        );
    }

    public override ArgumentFormat GetArgumentFormat(int index)
    {
        return index switch
        {
            0 => FormatArg0,
            1 => FormatArg1,
            2 => FormatArg2,
            3 => FormatArg3,
            4 => FormatArg4,
            5 => FormatArg5,
            6 => FormatArg6,
            7 => FormatArg7,
            8 => FormatArg8,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private class ArgumentAccessor
    {
        public T0? Arg0;
        public T1? Arg1;
        public T2? Arg2;
        public T3? Arg3;
        public T4? Arg4;
        public T5? Arg5;
        public T6? Arg6;
        public T7? Arg7;
        public T8? Arg8;
        public readonly Func<int, object?> AccessFunc;
        public ArgumentAccessor() { AccessFunc = Access; }
        private object? Access(int index)
        {
            return index switch
            {
                0 => Arg0,
                1 => Arg1,
                2 => Arg2,
                3 => Arg3,
                4 => Arg4,
                5 => Arg5,
                6 => Arg6,
                7 => Arg7,
                8 => Arg8,
                _ => null
            };
        }
    }
}
public class Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : Translation
{
    [ThreadStatic]
    private static ArgumentAccessor? _argAccessor;
    public override int ArgumentCount => 10;
    public ArgumentFormat FormatArg0 { get; set; }
    public ArgumentFormat FormatArg1 { get; set; }
    public ArgumentFormat FormatArg2 { get; set; }
    public ArgumentFormat FormatArg3 { get; set; }
    public ArgumentFormat FormatArg4 { get; set; }
    public ArgumentFormat FormatArg5 { get; set; }
    public ArgumentFormat FormatArg6 { get; set; }
    public ArgumentFormat FormatArg7 { get; set; }
    public ArgumentFormat FormatArg8 { get; set; }
    public ArgumentFormat FormatArg9 { get; set; }
    public Translation(string defaultValue, TranslationOptions options = default, ArgumentFormat arg0Fmt = default, ArgumentFormat arg1Fmt = default, ArgumentFormat arg2Fmt = default, ArgumentFormat arg3Fmt = default, ArgumentFormat arg4Fmt = default, ArgumentFormat arg5Fmt = default, ArgumentFormat arg6Fmt = default, ArgumentFormat arg7Fmt = default, ArgumentFormat arg8Fmt = default, ArgumentFormat arg9Fmt = default)
        : base(defaultValue, options)
    {
        arg0Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg1Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg2Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg3Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg4Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg5Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg6Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg7Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg8Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();
        arg9Fmt.FormatAddons ??= Array.Empty<IArgumentAddon>();

        FormatArg0 = arg0Fmt;
        FormatArg1 = arg1Fmt;
        FormatArg2 = arg2Fmt;
        FormatArg3 = arg3Fmt;
        FormatArg4 = arg4Fmt;
        FormatArg5 = arg5Fmt;
        FormatArg6 = arg6Fmt;
        FormatArg7 = arg7Fmt;
        FormatArg8 = arg8Fmt;
        FormatArg9 = arg9Fmt;
    }

    public string Translate(scoped in TranslationArguments args, T0? arg0, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, T9? arg9)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;
        _argAccessor.Arg1 = arg1;
        _argAccessor.Arg2 = arg2;
        _argAccessor.Arg3 = arg3;
        _argAccessor.Arg4 = arg4;
        _argAccessor.Arg5 = arg5;
        _argAccessor.Arg6 = arg6;
        _argAccessor.Arg7 = arg7;
        _argAccessor.Arg8 = arg8;
        _argAccessor.Arg9 = arg9;

        ValueFormatParameters p0 = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 10);
        ValueFormatParameters p1 = new ValueFormatParameters(1, in args, FormatArg1, _argAccessor.AccessFunc, 10);
        ValueFormatParameters p2 = new ValueFormatParameters(2, in args, FormatArg2, _argAccessor.AccessFunc, 10);
        ValueFormatParameters p3 = new ValueFormatParameters(3, in args, FormatArg3, _argAccessor.AccessFunc, 10);
        ValueFormatParameters p4 = new ValueFormatParameters(4, in args, FormatArg4, _argAccessor.AccessFunc, 10);
        ValueFormatParameters p5 = new ValueFormatParameters(5, in args, FormatArg5, _argAccessor.AccessFunc, 10);
        ValueFormatParameters p6 = new ValueFormatParameters(6, in args, FormatArg6, _argAccessor.AccessFunc, 10);
        ValueFormatParameters p7 = new ValueFormatParameters(7, in args, FormatArg7, _argAccessor.AccessFunc, 10);
        ValueFormatParameters p8 = new ValueFormatParameters(8, in args, FormatArg8, _argAccessor.AccessFunc, 10);
        ValueFormatParameters p9 = new ValueFormatParameters(9, in args, FormatArg9, _argAccessor.AccessFunc, 10);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p0);
        string f1 = TranslationService.ValueFormatter.Format(arg1, in p1);
        string f2 = TranslationService.ValueFormatter.Format(arg2, in p2);
        string f3 = TranslationService.ValueFormatter.Format(arg3, in p3);
        string f4 = TranslationService.ValueFormatter.Format(arg4, in p4);
        string f5 = TranslationService.ValueFormatter.Format(arg5, in p5);
        string f6 = TranslationService.ValueFormatter.Format(arg6, in p6);
        string f7 = TranslationService.ValueFormatter.Format(arg7, in p7);
        string f8 = TranslationService.ValueFormatter.Format(arg8, in p8);
        string f9 = TranslationService.ValueFormatter.Format(arg9, in p9);

        Span<int> indices = stackalloc int[9];
        indices[0] = f0.Length;
        indices[1] = indices[0] + f1.Length;
        indices[2] = indices[1] + f2.Length;
        indices[3] = indices[2] + f3.Length;
        indices[4] = indices[3] + f4.Length;
        indices[5] = indices[4] + f5.Length;
        indices[6] = indices[5] + f6.Length;
        indices[7] = indices[6] + f7.Length;
        indices[8] = indices[7] + f8.Length;

        Span<char> collectionSpan = stackalloc char[indices[8] + f9.Length];
        f0.AsSpan().CopyTo(collectionSpan);
        f1.AsSpan().CopyTo(collectionSpan[indices[0]..]);
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);
        f3.AsSpan().CopyTo(collectionSpan[indices[2]..]);
        f4.AsSpan().CopyTo(collectionSpan[indices[3]..]);
        f5.AsSpan().CopyTo(collectionSpan[indices[4]..]);
        f6.AsSpan().CopyTo(collectionSpan[indices[5]..]);
        f7.AsSpan().CopyTo(collectionSpan[indices[6]..]);
        f8.AsSpan().CopyTo(collectionSpan[indices[7]..]);
        f9.AsSpan().CopyTo(collectionSpan[indices[8]..]);

        ArgumentSpan[] pluralizers = args.ValueSet.GetPluralizations(in args, out int argOffset);
        ReadOnlySpan<char> preformattedValue = args.PreformattedValue;
        if (pluralizers.Length > 0)
        {
            preformattedValue = ApplyPluralizers(in args, pluralizers, argOffset, 10, _argAccessor.AccessFunc);
        }

        return TranslationFormattingUtility.FormatString(preformattedValue, collectionSpan, indices);
    }

    protected override string UnsafeTranslateIntl(in TranslationArguments arguments, object?[] formattingParameters)
    {
        return Translate(
            in arguments,
            (T0?)formattingParameters[0],
            (T1?)formattingParameters[1],
            (T2?)formattingParameters[2],
            (T3?)formattingParameters[3],
            (T4?)formattingParameters[4],
            (T5?)formattingParameters[5],
            (T6?)formattingParameters[6],
            (T7?)formattingParameters[7],
            (T8?)formattingParameters[8],
            (T9?)formattingParameters[9]
        );
    }

    public override ArgumentFormat GetArgumentFormat(int index)
    {
        return index switch
        {
            0 => FormatArg0,
            1 => FormatArg1,
            2 => FormatArg2,
            3 => FormatArg3,
            4 => FormatArg4,
            5 => FormatArg5,
            6 => FormatArg6,
            7 => FormatArg7,
            8 => FormatArg8,
            9 => FormatArg9,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private class ArgumentAccessor
    {
        public T0? Arg0;
        public T1? Arg1;
        public T2? Arg2;
        public T3? Arg3;
        public T4? Arg4;
        public T5? Arg5;
        public T6? Arg6;
        public T7? Arg7;
        public T8? Arg8;
        public T9? Arg9;
        public readonly Func<int, object?> AccessFunc;
        public ArgumentAccessor() { AccessFunc = Access; }
        private object? Access(int index)
        {
            return index switch
            {
                0 => Arg0,
                1 => Arg1,
                2 => Arg2,
                3 => Arg3,
                4 => Arg4,
                5 => Arg5,
                6 => Arg6,
                7 => Arg7,
                8 => Arg8,
                9 => Arg9,
                _ => null
            };
        }
    }
}