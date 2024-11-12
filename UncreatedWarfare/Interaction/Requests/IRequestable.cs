namespace Uncreated.Warfare.Interaction.Requests;

/// <summary>
/// Represents an object that can be requested using an <see cref="IRequestHandler{T}"/>.
/// </summary>
public interface IRequestable<out TRequestObject>;