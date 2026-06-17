namespace Uncreated.Warfare.Zones;

/// <summary>
/// Defines the behaivor of the current layout's electrical grid.
/// </summary>
public interface IElectricalGridHandler
{
    /// <summary>
    /// Whether or not electrical grid handling is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Defines whether or not an object should be powered.
    /// </summary>
    bool IsPowered(LevelObject @object);

    /// <summary>
    /// Defines whether or not a barricade should be powered.
    /// </summary>
    bool IsPowered(InteractablePower otherInteractable);

    /// <summary>
    /// Invoked on layout start.
    /// </summary>
    void Start();

    /// <summary>
    /// Invoked on layout end.
    /// </summary>
    void Stop();
}

/// <summary>
/// An implementation of <see cref="IElectricalGridHandler"/> that disables electricity.
/// </summary>
public sealed class DisabledElectricalGridHandler : IElectricalGridHandler
{
    public bool IsEnabled => true;
    public bool IsPowered(LevelObject @object) => false;
    public bool IsPowered(InteractablePower otherInteractable) => false;
    public void Start() { }
    public void Stop() { }
}