using DanielWillett.ReflectionTools;
using System;

namespace Uncreated.Warfare.Exceptions;

/// <summary>
/// Thrown when configuration data for a quest is invalid.
/// </summary>
public class QuestConfigurationException : GameConfigurationException
{
    public Type? QuestTemplateType { get; }

    public QuestConfigurationException(Type templateType, string reason) : base($"There was an error reading the quest configuration for the state of quest {Accessor.ExceptionFormatter.Format(templateType)}. {reason}")
    {
        QuestTemplateType = templateType;
    }

    public QuestConfigurationException(string message) : base(message) { }
}