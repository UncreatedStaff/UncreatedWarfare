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

    public string Translate(in TranslationArguments args, T0 arg0)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;

        ValueFormatParameters p = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 1);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p);
        return TranslationFormattingUtility.FormatString(args.PreformattedValue, f0.AsSpan(), default);
    }

    private class ArgumentAccessor
    {
        public T0 Arg0;
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

    public string Translate(in TranslationArguments args, T0 arg0, T1 arg1)
    {
        _argAccessor ??= new ArgumentAccessor();
        _argAccessor.Arg0 = arg0;
        _argAccessor.Arg1 = arg1;

        ValueFormatParameters p0 = new ValueFormatParameters(0, in args, FormatArg0, _argAccessor.AccessFunc, 2);
        ValueFormatParameters p1 = new ValueFormatParameters(1, in args, FormatArg1, _argAccessor.AccessFunc, 2);
        string f0 = TranslationService.ValueFormatter.Format(arg0, in p0);
        string f1 = TranslationService.ValueFormatter.Format(arg1, in p1);

        Span<char> collectionSpan = stackalloc char[f0.Length + f1.Length];
        f0.AsSpan().CopyTo(collectionSpan);
        f1.AsSpan().CopyTo(collectionSpan[f0.Length..]);

        Span<int> indices = stackalloc int[1];
        indices[0] = f0.Length;

        return TranslationFormattingUtility.FormatString(args.PreformattedValue, collectionSpan, indices);
    }

    private class ArgumentAccessor
    {
        public T0 Arg0;
        public T1 Arg1;
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

    public string Translate(in TranslationArguments args, T0 arg0, T1 arg1, T2 arg2)
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

        Span<char> collectionSpan = stackalloc char[f0.Length + f1.Length];
        Span<int> indices = stackalloc int[2];
        f0.AsSpan().CopyTo(collectionSpan);
        indices[0] = f0.Length;
        f1.AsSpan().CopyTo(collectionSpan[f0.Length..]);
        indices[1] = indices[0] + f1.Length;
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);

        return TranslationFormattingUtility.FormatString(args.PreformattedValue, collectionSpan, indices);
    }

    private class ArgumentAccessor
    {
        public T0 Arg0;
        public T1 Arg1;
        public T2 Arg2;
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

    public string Translate(in TranslationArguments args, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
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

        Span<char> collectionSpan = stackalloc char[f0.Length + f1.Length];
        Span<int> indices = stackalloc int[3];
        f0.AsSpan().CopyTo(collectionSpan);
        indices[0] = f0.Length;
        f1.AsSpan().CopyTo(collectionSpan[f0.Length..]);
        indices[1] = indices[0] + f1.Length;
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);
        indices[2] = indices[1] + f2.Length;
        f3.AsSpan().CopyTo(collectionSpan[indices[2]..]);

        return TranslationFormattingUtility.FormatString(args.PreformattedValue, collectionSpan, indices);
    }

    private class ArgumentAccessor
    {
        public T0 Arg0;
        public T1 Arg1;
        public T2 Arg2;
        public T3 Arg3;
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

    public string Translate(in TranslationArguments args, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
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

        Span<char> collectionSpan = stackalloc char[f0.Length + f1.Length];
        Span<int> indices = stackalloc int[4];
        f0.AsSpan().CopyTo(collectionSpan);
        indices[0] = f0.Length;
        f1.AsSpan().CopyTo(collectionSpan[f0.Length..]);
        indices[1] = indices[0] + f1.Length;
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);
        indices[2] = indices[1] + f2.Length;
        f3.AsSpan().CopyTo(collectionSpan[indices[2]..]);
        indices[3] = indices[2] + f3.Length;
        f4.AsSpan().CopyTo(collectionSpan[indices[3]..]);

        return TranslationFormattingUtility.FormatString(args.PreformattedValue, collectionSpan, indices);
    }

    private class ArgumentAccessor
    {
        public T0 Arg0;
        public T1 Arg1;
        public T2 Arg2;
        public T3 Arg3;
        public T4 Arg4;
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

    public string Translate(in TranslationArguments args, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
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

        Span<char> collectionSpan = stackalloc char[f0.Length + f1.Length];
        Span<int> indices = stackalloc int[5];
        f0.AsSpan().CopyTo(collectionSpan);
        indices[0] = f0.Length;
        f1.AsSpan().CopyTo(collectionSpan[f0.Length..]);
        indices[1] = indices[0] + f1.Length;
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);
        indices[2] = indices[1] + f2.Length;
        f3.AsSpan().CopyTo(collectionSpan[indices[2]..]);
        indices[3] = indices[2] + f3.Length;
        f4.AsSpan().CopyTo(collectionSpan[indices[3]..]);
        indices[4] = indices[3] + f4.Length;
        f5.AsSpan().CopyTo(collectionSpan[indices[4]..]);

        return TranslationFormattingUtility.FormatString(args.PreformattedValue, collectionSpan, indices);
    }

    private class ArgumentAccessor
    {
        public T0 Arg0;
        public T1 Arg1;
        public T2 Arg2;
        public T3 Arg3;
        public T4 Arg4;
        public T5 Arg5;
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

    public string Translate(in TranslationArguments args, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
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

        Span<char> collectionSpan = stackalloc char[f0.Length + f1.Length];
        Span<int> indices = stackalloc int[6];
        f0.AsSpan().CopyTo(collectionSpan);
        indices[0] = f0.Length;
        f1.AsSpan().CopyTo(collectionSpan[f0.Length..]);
        indices[1] = indices[0] + f1.Length;
        f2.AsSpan().CopyTo(collectionSpan[indices[1]..]);
        indices[2] = indices[1] + f2.Length;
        f3.AsSpan().CopyTo(collectionSpan[indices[2]..]);
        indices[3] = indices[2] + f3.Length;
        f4.AsSpan().CopyTo(collectionSpan[indices[3]..]);
        indices[4] = indices[3] + f4.Length;
        f5.AsSpan().CopyTo(collectionSpan[indices[4]..]);
        indices[5] = indices[4] + f5.Length;
        f6.AsSpan().CopyTo(collectionSpan[indices[5]..]);

        return TranslationFormattingUtility.FormatString(args.PreformattedValue, collectionSpan, indices);
    }

    private class ArgumentAccessor
    {
        public T0 Arg0;
        public T1 Arg1;
        public T2 Arg2;
        public T3 Arg3;
        public T4 Arg4;
        public T5 Arg5;
        public T6 Arg6;
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