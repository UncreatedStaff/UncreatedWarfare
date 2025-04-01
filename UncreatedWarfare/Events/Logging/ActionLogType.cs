namespace Uncreated.Warfare.Events.Logging;

public sealed class ActionLogType(string displayName, string logName, ushort id)
{
    public string DisplayName { get; } = displayName;
    public string LogName { get; } = logName;
    public ushort Id { get; } = id;

    /// <inheritdoc />
    public override string ToString()
    {
        return DisplayName;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Id;
    }
}