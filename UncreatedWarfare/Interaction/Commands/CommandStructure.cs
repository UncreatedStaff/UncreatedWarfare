using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Interaction.Commands;
public sealed class CommandStructure
{
    private const string NoTypeColor = "ddd";
    private const string DefaultValueTypeColor = "bfbfbf";
    private const string DefaultClassTypeColor = "ecc6d9";
    private const string QuoteColor = "d69d85";
    public IExecutableCommand Command { get; internal set; }
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
    public PermissionLeaf? Permission { get; set; }
    public TranslationList? DescriptionTranslations { get; set; }
    public CommandParameter[] Parameters { get; set; } = Array.Empty<CommandParameter>();
    public string? GetDescription(LanguageInfo? language)
    {
        if (DescriptionTranslations == null)
            return Description;

        if (language != null && DescriptionTranslations.TryGetValue(language.Code, out string desc))
            return desc;

        return Description;
    }
    /// <exception cref="BaseCommandContext"/>
    public async UniTask OnHelpCommand(CommandContext ctx, CommandInfo cmd)
    {
        if (string.IsNullOrEmpty(Description) && DescriptionTranslations == null && Parameters is not { Length: > 0 })
            return;

        /* todo
        if (!cmd.CheckPermission(ctx))
        {
            throw ctx.SendNoPermission();
        }
        */

        string? desc = GetDescription(ctx.Language);
#if false
        StringBuilder builder = new StringBuilder(ctx.IsConsole ? "/" : (ctx.IMGUI ? "/<color=#ffffff>" : "/<#fff>"), Chat.MaxMessageSize);
        builder.Append(cmd.CommandName.ToLowerInvariant());
        if (!ctx.IsConsole)
            builder.Append("</color>");
        if (Parameters is { Length: > 0 })
        {
            builder.Append(' ');
            string? desc2 = await FormatParameters(Parameters, builder, ctx, colors: !ctx.IsConsole);
            await UniTask.SwitchToMainThread();
            if (desc2 != null)
                desc = desc2;
            if (builder.Length > Chat.MaxMessageSize - 64) // retry without colors
            {
                if (ctx.IsConsole)
                    builder.Clear().Append('/').Append(cmd.CommandName.ToLowerInvariant()).Append(' ');
                else
                    builder.Clear().Append("<#fff>").Append(ctx.IMGUI ? "/<color=#ffffff>" : "/<#fff>")
                    .Append(cmd.CommandName.ToLowerInvariant()).Append("</color>").Append(' ');

                await FormatParameters(Parameters, builder, ctx, colors: false);
                await UniTask.SwitchToMainThread();
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
#endif
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
    public PermissionLeaf? Permission { get; set; }
    public string[] Aliases { get; set; } = Array.Empty<string>();
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
    public string? GetDescription(LanguageInfo? language)
    {
        return DescriptionTranslations == null ? Description : DescriptionTranslations.Translate(language, Description);
    }
}