using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Framework;

namespace Uncreated.Warfare.Commands.CommandSystem;
public sealed class CommandStructure
{
    private const string NoTypeColor = "ddd";
    private const string DefaultValueTypeColor = "bfbfbf";
    private const string DefaultClassTypeColor = "ecc6d9";
    private const string QuoteColor = "d69d85";

    private static readonly KeyValuePair<Type, string>[] TypeColors =
    {
        new KeyValuePair<Type, string>(typeof(object), DefaultValueTypeColor),
        new KeyValuePair<Type, string>(typeof(string), "66b3ff"),
        new KeyValuePair<Type, string>(typeof(char), "4da6ff"),
        new KeyValuePair<Type, string>(typeof(IPlayer), "b3ffe0"),
        new KeyValuePair<Type, string>(typeof(int), "ffdd99"),
        new KeyValuePair<Type, string>(typeof(uint), "ffdd99"),
        new KeyValuePair<Type, string>(typeof(long), "ffdd99"),
        new KeyValuePair<Type, string>(typeof(ulong), "ffdd99"),
        new KeyValuePair<Type, string>(typeof(short), "ffdd99"),
        new KeyValuePair<Type, string>(typeof(ushort), "ffdd99"),
        new KeyValuePair<Type, string>(typeof(byte), "ffdd99"),
        new KeyValuePair<Type, string>(typeof(sbyte), "ffdd99"),
        new KeyValuePair<Type, string>(typeof(float), "ffbb99"),
        new KeyValuePair<Type, string>(typeof(double), "ffbb99"),
        new KeyValuePair<Type, string>(typeof(decimal), "ffbb99"),
        new KeyValuePair<Type, string>(typeof(bool), "ffb3b3"),
        new KeyValuePair<Type, string>(typeof(TimeSpan), "b3b3ff"),
        new KeyValuePair<Type, string>(typeof(DateTime), "b3b3ff"),
        new KeyValuePair<Type, string>(typeof(DateTimeOffset), "b3b3ff"),
        new KeyValuePair<Type, string>(typeof(Type), "cc66ff"),
        new KeyValuePair<Type, string>(typeof(Asset), "4dffb8"),
    };
    public string Description { get; set; }
    public TranslationList? DescriptionTranslations { get; set; }
    public CommandParameter[] Parameters { get; set; } = Array.Empty<CommandParameter>();
    public string? GetDescription(string language)
    {
        if (DescriptionTranslations == null)
            return Description;

        if (DescriptionTranslations.TryGetValue(language, out string desc))
            return desc;

        return Description;
    }
    /// <exception cref="BaseCommandInteraction"/>
    public void OnHelpCommand(CommandInteraction ctx, IExecutableCommand cmd)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (string.IsNullOrEmpty(Description) && DescriptionTranslations == null && Parameters is not { Length: > 0 })
            return;

        if (!cmd.CheckPermission(ctx))
        {
            throw ctx.SendNoPermission();
        }

        string? desc = GetDescription(ctx.Language);

        StringBuilder builder = new StringBuilder(ctx.IMGUI ? "/<color=#ffffff>" : "/<#fff>", Chat.MaxMessageSize);
        builder.Append(cmd.CommandName.ToLowerInvariant()).Append("</color>");
        if (Parameters is { Length: > 0 })
        {
            builder.Append(' ');
            FormatParameters(Parameters, builder, ctx, out string? desc2);
            if (desc2 != null)
                desc = desc2;
            if (builder.Length > Chat.MaxMessageSize - 64) // retry without colors
            {
                builder.Clear()
                    .Append("<#fff>").Append(ctx.IMGUI ? "/<color=#ffffff>" : "/<#fff>")
                    .Append(cmd.CommandName.ToLowerInvariant()).Append("</color>")
                    .Append(' ');
                FormatParameters(Parameters, builder, ctx, out _, colors: false);
            }
        }
        if (desc != null)
        {
            ctx.ReplyString(desc, "b3ffb3");
        }
        string str = builder.ToString();

        // if theres enough space add syntax prefix
        if (builder.Length < Chat.MaxMessageSize - 12)
            str = "Syntax: " + str;

        ctx.ReplyString(str, "b3ffb3");
        throw ctx;
    }
    private static string GetTypeColor(object[] types)
    {
        if (types.Length == 0)
            return QuoteColor;
        object type = types[0];
        if (types.Length != 1 && type is not Type)
            type = types.FirstOrDefault(x => x is Type) ?? types[0];

        if (type is not Type t)
            t = typeof(object);

        for (int i = 0; i < TypeColors.Length; ++i)
        {
            if (TypeColors[i].Key == t)
                return TypeColors[i].Value;
        }
        for (int i = TypeColors.Length - 1; i >= 0; --i)
        {
            if (TypeColors[i].Key.IsAssignableFrom(t))
                return TypeColors[i].Value;
        }
        return t.IsValueType ? DefaultValueTypeColor : DefaultClassTypeColor;
    }
    private static void FormatParameters(CommandParameter[] paramters, StringBuilder builder, CommandInteraction ctx, out string? desc, bool colors = true)
    {
        desc = null;
        ++ctx.Offset;
        try
        {
            for (int i = 0; i < paramters.Length; ++i)
            {
                CommandParameter p = paramters[i];
                string? flag = p.FlagName;
                if (string.IsNullOrEmpty(flag))
                    continue;
                bool has = ctx.MatchFlag(flag!);
                if (has)
                    builder.Append('[');
                if (flag![0] != '-')
                    builder.Append('-');
                if (ctx.MatchParameter(0, flag[0] == '-' ? flag : ("-" + flag)))
                    desc = p.GetDescription(ctx.Language);
                builder.Append(flag);
                builder.Append(": ").Append(p.Name);
            }
            if (ctx.TryGet(0, out string val)) // part of path
            {
                // match to a parameter or explicit value
                CommandParameter? p = null;
                for (int i = 0; i < paramters.Length; ++i)
                {
                    if (paramters[i].Name.Equals(val, StringComparison.InvariantCultureIgnoreCase))
                    {
                        p = paramters[i];
                        break;
                    }
                }
                if (p == null)
                {
                    for (int i = 0; i < paramters.Length; ++i)
                    {
                        if (paramters[i].Values
                            .Any(x => x is string str &&
                                      str.Equals(val, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            p = paramters[i];
                            break;
                        }
                    }
                }
                if (p != null)
                {
                    if (colors)
                    {
                        builder.Append(ctx.IMGUI ? "<#" : "<color=#").Append(GetTypeColor(p.Values)).Append('>');
                    }
                    builder.Append(val.ToLowerInvariant());
                    if (colors)
                        builder.Append("</color>");
                    if (p.Parameters is { Length: > 0 })
                    {
                        builder.Append(' ');
                        FormatParameters(p.Parameters, builder, ctx, out string? d2, colors);
                        if (d2 != null)
                            desc = d2;
                    }
                    desc = p.GetDescription(ctx.Language);
                    return;
                }
            }

            // is end
            bool allIsOptional = paramters.All(x => x.IsOptional);
            for (int i = 0; i < paramters.Length; ++i)
            {
                CommandParameter p = paramters[i];
                if (!string.IsNullOrEmpty(p.FlagName))
                    continue;
                if (i == 0)
                    builder.Append(allIsOptional ? '[' : '<');
                else
                    builder.Append('|');

                void Chain(string? tclr)
                {
                    int r = p.ChainDisplayCount;
                    CommandParameter p2 = p;
                    while (p2.Parameters.Length == 1 && --r > 0)
                    {
                        p2 = p2.Parameters[0];
                        builder.Append(' ');
                        bool cclr = false;
                        if (colors)
                        {
                            string clr2 = GetTypeColor(p2.Values);
                            if (!clr2.Equals(tclr, StringComparison.Ordinal))
                            {
                                cclr = true;
                                builder.Append(ctx.IMGUI ? "<#" : "<color=#").Append(clr2).Append('>');
                            }
                        }
                        builder.Append(p2.Name.ToLowerInvariant());
                        if (cclr)
                            builder.Append("</color>");
                        if (p2.IsRemainder)
                            builder.Append("... ");
                    }
                }
                bool started = p.Values.Any(x => x is Type);
                if (started)
                {
                    string tclr = colors ? GetTypeColor(p.Values) : null!;
                    if (colors)
                        builder.Append(ctx.IMGUI ? "<#" : "<color=#").Append(tclr).Append('>');
                    builder.Append(p.Name.ToLowerInvariant());
                    if (p.ChainDisplayCount > 0 && paramters.Length != 1)
                        Chain(tclr);

                    if (colors)
                        builder.Append("</color>");
                    if (p.ChainDisplayCount <= 0 && p.IsRemainder)
                        builder.Append("... ");
                }
                foreach (string str in p.Values.OfType<string>())
                {
                    if (started)
                        builder.Append('|');
                    else
                    {
                        if (colors)
                            builder.Append(ctx.IMGUI ? "<#" : "<color=#").Append(NoTypeColor).Append(">");
                        builder.Append(p.Name.ToLowerInvariant()).Append(": ");
                        if (colors)
                            builder.Append("</color>");
                    }
                    if (colors)
                    {
                        builder.Append("<b>").Append(ctx.IMGUI ? "<#" : "<color=#").Append(QuoteColor).Append(">");
                        builder.Append(str.ToLowerInvariant());
                        builder.Append("</color></b>");
                    }
                    else
                        builder.Append('"').Append(str).Append('"');
                    started = true;
                }
                if (!started)
                {
                    string tclr = colors ? GetTypeColor(p.Values) : null!;
                    if (colors)
                        builder.Append(ctx.IMGUI ? "<#" : "<color=#").Append(tclr).Append('>');
                    if (colors)
                        builder.Append("<b>").Append(ctx.IMGUI ? "<#" : "<color=#").Append(tclr).Append(">");
                    builder.Append(p.Name.ToLowerInvariant());
                    if (p.ChainDisplayCount > 0 && paramters.Length != 1)
                    {
                        if (colors)
                            builder.Append("</b>");
                        Chain(tclr);
                        if (colors)
                            builder.Append("<b>");
                    }

                    if (colors)
                        builder.Append("</color></b>");
                }
                if (i == paramters.Length - 1)
                    builder.Append(p.IsOptional ? ']' : '>');
                if (paramters.Length == 1 && string.IsNullOrEmpty(paramters[0].FlagName) && builder.Length < Chat.MaxMessageSize / 2)
                {
                    builder.Append(' ');
                    FormatParameters(p.Parameters, builder, ctx, out _, colors);
                }
            }
        }
        finally
        {
            --ctx.Offset;
        }
    }
}
public sealed class CommandParameter
{
    public string Name { get; set; }
    public bool IsRemainder { get; set; }
    public bool IsOptional { get; set; }
    public object[] Values { get; set; }
    public int ChainDisplayCount { get; set; }
    public string? FlagName { get; set; }
    public string? Description { get; set; }
    public EAdminType? Permission { get; set; }
    public TranslationList? DescriptionTranslations { get; set; }
    public CommandParameter[] Parameters { get; set; } = Array.Empty<CommandParameter>();
    public CommandParameter(string name, object value)
    {
        Name = name;
        Values = new object[] { value };
    }
    public CommandParameter(string name, params object[] values)
    {
        Name = name;
        Values = values ?? Array.Empty<object>();
    }
    public string? GetDescription(string language)
    {
        if (DescriptionTranslations == null)
            return Description;

        if (DescriptionTranslations.TryGetValue(language, out string desc))
            return desc;

        return Description;
    }
}