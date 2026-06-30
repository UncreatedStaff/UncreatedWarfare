using System;

namespace Uncreated.Warfare.Translations.Storage;

/// <summary>
/// Defines the implementation of <see cref="ITranslationStorage"/> to use for all collections.
/// </summary>
public interface ITranslationStorageFactory
{
    /// <summary>
    /// Create a storage implementation for the given collection.
    /// </summary>
    ITranslationStorage Create(TranslationCollection collection);

    /// <summary>
    /// Create a storage implementation for the given <typeparamref name="TEnum"/> type.
    /// </summary>
    /// <typeparam name="TEnum">The type of <see cref="Enum"/> to format.</typeparam>
    IEnumTranslationStorage<TEnum> CreateEnumStorage<TEnum>() where TEnum : unmanaged, Enum;
}