using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Reflection;

namespace Uncreated.Warfare.Translations.ValueFormatters;
public class ReflectionMemberFormatter :
    IValueFormatter<MemberInfo>,
    IValueFormatter<Type>,
    IValueFormatter<MethodBase>,
    IValueFormatter<FieldInfo>,
    IValueFormatter<PropertyInfo>,
    IValueFormatter<EventInfo>,
    IValueFormatter<ParameterInfo>,
    IValueFormatter<IMemberDefinition>,
    IValueFormatter<IVariable>
{
    public string Format(ITranslationValueFormatter formatter, MemberInfo value, in ValueFormatParameters parameters)
    {
        return value switch
        {
            Type t => Accessor.Formatter.Format(t),
            MethodBase mtd => Accessor.Formatter.Format(mtd),
            FieldInfo fld => Accessor.Formatter.Format(fld),
            PropertyInfo prop => Accessor.Formatter.Format(prop),
            EventInfo ev => Accessor.Formatter.Format(ev),
            _ => value.ToString()
        };
    }

    public string Format(ITranslationValueFormatter formatter, Type value, in ValueFormatParameters parameters)
    {
        return Accessor.Formatter.Format(value);
    }

    public string Format(ITranslationValueFormatter formatter, MethodBase value, in ValueFormatParameters parameters)
    {
        return Accessor.Formatter.Format(value);
    }

    public string Format(ITranslationValueFormatter formatter, FieldInfo value, in ValueFormatParameters parameters)
    {
        return Accessor.Formatter.Format(value);
    }

    public string Format(ITranslationValueFormatter formatter, PropertyInfo value, in ValueFormatParameters parameters)
    {
        return Accessor.Formatter.Format(value);
    }

    public string Format(ITranslationValueFormatter formatter, EventInfo value, in ValueFormatParameters parameters)
    {
        return Accessor.Formatter.Format(value);
    }

    public string Format(ITranslationValueFormatter formatter, ParameterInfo value, in ValueFormatParameters parameters)
    {
        return Accessor.Formatter.Format(value);
    }

    public string Format(ITranslationValueFormatter formatter, IMemberDefinition value, in ValueFormatParameters parameters)
    {
        return value.Format(Accessor.Formatter);
    }

    public string Format(ITranslationValueFormatter formatter, IVariable value, in ValueFormatParameters parameters)
    {
        return value.Format(Accessor.Formatter);
    }

    public string Format(ITranslationValueFormatter formatter, object value, in ValueFormatParameters parameters)
    {
        return value switch
        {
            IMemberDefinition def => def.Format(Accessor.Formatter),
            IVariable var => var.Format(Accessor.Formatter),
            MemberInfo mem => Format(formatter, mem, in parameters),
            ParameterInfo param => Format(formatter, param, in parameters),
            _ => value.ToString()
        };
    }
}
