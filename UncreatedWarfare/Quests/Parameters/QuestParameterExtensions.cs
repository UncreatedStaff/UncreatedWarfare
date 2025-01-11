namespace Uncreated.Warfare.Quests.Parameters;

public static class QuestParameterExtensions
{
    public static bool IsWildcardInclusive<TValueType>(this QuestParameterValue<TValueType> choice)
    {
        return choice is null or { ValueType: ParameterValueType.Wildcard, SelectionType: ParameterSelectionType.Inclusive };
    }
}