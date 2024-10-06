namespace Uncreated.Warfare.Logging.Formatting;

/// <summary>
/// Represents a value equal to a formatting argument that is out of range of the given values.
/// </summary>
/// <remarks>Singleton object available at <see cref="Value"/>.</remarks>
public sealed class OutOfRange
{
    /// <summary>
    /// Represents a value equal to a formatting argument that is out of range of the given values.
    /// </summary>
    public static readonly OutOfRange Value = new OutOfRange();
    private OutOfRange() { }
}
