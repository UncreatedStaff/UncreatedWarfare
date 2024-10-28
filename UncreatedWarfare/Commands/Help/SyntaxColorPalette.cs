using System;
using System.Collections.Generic;
using System.Linq;
using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;
public static class SyntaxColorPalette
{
    public const char LookAtTargetInGameSymbol = 'ʘ';
    public const string LookAtTargetInGamePrefix = "ʘ ";
    public const string LookAtTargetInGameOptionalPrefix = "[ʘ] ";
    
    public const char LookAtTargetTerminalSymbol = '⌖';
    public const string LookAtTargetTerminalPrefix = "⌖ ";
    public const string LookAtTargetTerminalOptionalPrefix = "[⌖] ";

    public static readonly Color32 Punctuation = new Color32(153, 153, 153, 255);
    public static readonly Color32 QuoteMarks = new Color32(214, 157, 133, 255);
    public static readonly Color32 DefaultValueTypeColor = new Color32(191, 191, 191, 255);
    public static readonly Color32 DefaultReferenceTypeColor = new Color32(236, 198, 217, 255);
    public static readonly Color32 VerbatimColor = new Color32(221, 221, 221, 255);

    private static readonly Dictionary<Type, Color32> TypeColors = new Dictionary<Type, Color32>
    {
        { typeof(object), DefaultReferenceTypeColor },
        { typeof(string), new Color32(102, 179, 255, 255) },
        { typeof(char), new Color32(77, 166, 255, 255) },
        { typeof(IPlayer), new Color32(179, 255, 224, 255) },
        { typeof(int), new Color32(255, 221, 153, 255) },
        { typeof(uint), new Color32(255, 221, 153, 255) },
        { typeof(long), new Color32(255, 221, 153, 255) },
        { typeof(ulong), new Color32(255, 221, 153, 255) },
        { typeof(short), new Color32(255, 221, 153, 255) },
        { typeof(ushort), new Color32(255, 221, 153, 255) },
        { typeof(byte), new Color32(255, 221, 153, 255) },
        { typeof(sbyte), new Color32(255, 221, 153, 255) },
        { typeof(float), new Color32(255, 187, 153, 255) },
        { typeof(double), new Color32(255, 187, 153, 255) },
        { typeof(decimal), new Color32(255, 187, 153, 255) },
        { typeof(bool), new Color32(255, 179, 179, 255) },
        { typeof(TimeSpan), new Color32(179, 179, 255, 255) },
        { typeof(DateTime), new Color32(179, 179, 255, 255) },
        { typeof(DateTimeOffset), new Color32(179, 179, 255, 255) },
        { typeof(Type), new Color32(204, 102, 255, 255) },
        { typeof(Asset), new Color32(77, 255, 184, 255) }
    };

    public static Color32 GetColor(CommandSyntaxFormatter.TagType type)
    {
        return type switch
        {
            CommandSyntaxFormatter.TagType.Caller => new Color32(255, 204, 102, 255),
            CommandSyntaxFormatter.TagType.Target => new Color32(255, 102, 102, 255),
            CommandSyntaxFormatter.TagType.ParameterName => new Color32(204, 204, 255, 255),
            CommandSyntaxFormatter.TagType.FlagName => new Color32(255, 204, 255, 255),
            _ => new Color32(153, 153, 102, 255)
        };
    }

    public static Color32 GetColor(IReadOnlyList<Type> types)
    {
        if (types.Count == 0 || types.Contains(typeof(VerbatimParameterType)))
        {
            return VerbatimColor;
        }

        Type type = types[0];

        Color32 clr = type.IsValueType ? DefaultValueTypeColor : DefaultReferenceTypeColor;
        type.ForEachBaseType((type, _) =>
        {
            if (!TypeColors.TryGetValue(type, out Color32 color))
                return true;
            
            clr = color;
            return false;
        });

        return clr;
    }
}
