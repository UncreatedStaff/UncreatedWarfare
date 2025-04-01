using System;
using System.Collections.Generic;
using System.Linq;

namespace Uncreated.Warfare.Translations;

/// <summary>
/// Allows all translations to share one dictionary to save on memory (dictionaries are pretty heavy). Implements <see cref="IDictionary{TKey, TValue}"/>.
/// </summary>
public class SharedTranslationDictionary : IDictionary<string, TranslationValue>
{
    private readonly IDictionary<TranslationLanguageKey, TranslationValue> _underlyingTable;
    private KeyCollection? _keyCollection;
    private ValueCollection? _valueCollection;
    public Translation Translation { get; }
    public int Count { get; private set; }
    public SharedTranslationDictionary(Translation translation, IDictionary<TranslationLanguageKey, TranslationValue> underlyingTable)
    {
        Translation = translation;
        _underlyingTable = underlyingTable;
    }
    public void Add(string? languageCode, TranslationValue value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (languageCode != null && !value.Language.Code.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Language codes are mismatched.", nameof(languageCode));

        Add(value);
    }

    /// <summary>
    /// Add a translation value to the dictionary.
    /// </summary>
    public void Add(TranslationValue value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        TranslationLanguageKey fullKey = new TranslationLanguageKey(value.Language.Code, Translation.Key);
        _underlyingTable.Add(fullKey, value);
        ++Count;
    }

    /// <summary>
    /// Add or update a translation value to the dictionary.
    /// </summary>
    public void AddOrUpdate(TranslationValue value)
    {
        this[value.Language.Code] = value;
    }

    /// <inheritdoc />
    public void Clear()
    {
        List<TranslationLanguageKey> keys = new List<TranslationLanguageKey>();
        foreach (TranslationLanguageKey key in _underlyingTable.Keys)
        {
            if (!key.TranslationKey.Equals(Translation.Key, StringComparison.Ordinal))
                continue;

            keys.Add(key);
        }

        foreach (TranslationLanguageKey key in keys)
        {
            _underlyingTable.Remove(key);
        }

        Count = 0;
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        TranslationLanguageKey fullKey = new TranslationLanguageKey(key, Translation.Key);
        if (!_underlyingTable.Remove(fullKey))
            return false;

        --Count;
        return true;
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        TranslationLanguageKey fullKey = new TranslationLanguageKey(key, Translation.Key);
        return _underlyingTable.ContainsKey(fullKey);
    }

    /// <summary>
    /// Determines whether this dictionary contains the given <paramref name="value"/>.
    /// </summary>
    public bool ContainsValue(TranslationValue value)
    {
        TranslationLanguageKey fullKey = new TranslationLanguageKey(value.Language.Code, value.Translation.Key);
        return value.Translation.Key.Equals(Translation.Key) && _underlyingTable.ContainsKey(fullKey);
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out TranslationValue value)
    {
        TranslationLanguageKey fullKey = new TranslationLanguageKey(key, Translation.Key);
        return _underlyingTable.TryGetValue(fullKey, out value);
    }

    /// <inheritdoc />
    public TranslationValue this[string key]
    {
        get
        {
            TranslationLanguageKey fullKey = new TranslationLanguageKey(key, Translation.Key);
            return _underlyingTable[fullKey];
        }
        set
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            TranslationLanguageKey fullKey = new TranslationLanguageKey(key, Translation.Key);
            bool containedKey = _underlyingTable.ContainsKey(fullKey);
            _underlyingTable[fullKey] = value;
            if (!containedKey)
                ++Count;
        }
    }

    /// <inheritdoc />
    public ICollection<string> Keys
    {
        get
        {
            _keyCollection ??= new KeyCollection(this);
            return _keyCollection;
        }
    }

    /// <inheritdoc />
    public ICollection<TranslationValue> Values
    {
        get
        {
            _valueCollection ??= new ValueCollection(this);
            return _valueCollection;
        }
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, TranslationValue>> GetEnumerator()
    {
        return _underlyingTable
            .Where(tableEntry => tableEntry.Key.TranslationKey.Equals(Translation.Key, StringComparison.Ordinal))
            .Select(static tableEntry => new KeyValuePair<string, TranslationValue>(tableEntry.Key.LanguageCode, tableEntry.Value))
            .GetEnumerator();
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<string, TranslationValue> item)
    {
        Add(item.Key, item.Value);
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<string, TranslationValue> item)
    {
        if (!item.Value.Translation.Key.Equals(Translation.Key, StringComparison.Ordinal))
            return false;

        TranslationLanguageKey fullKey = new TranslationLanguageKey(item.Key, Translation.Key);
        return _underlyingTable.TryGetValue(fullKey, out TranslationValue value) && value.Equals(item.Value);
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<string, TranslationValue> item)
    {
        if (!item.Value.Translation.Key.Equals(Translation.Key, StringComparison.Ordinal))
            return false;

        TranslationLanguageKey fullKey = new TranslationLanguageKey(item.Key, Translation.Key);
        return _underlyingTable.TryGetValue(fullKey, out TranslationValue value) && value.Equals(item.Value) && _underlyingTable.Remove(fullKey);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, TranslationValue>[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (arrayIndex + Count > array.Length)
            throw new ArgumentException($"Array not long enough for {Count} elements.", nameof(array));

        int index = arrayIndex - 1;
        foreach (KeyValuePair<TranslationLanguageKey, TranslationValue> tableEntry in _underlyingTable)
        {
            if (!tableEntry.Key.TranslationKey.Equals(Translation.Key))
                continue;

            array[++index] = new KeyValuePair<string, TranslationValue>(tableEntry.Key.LanguageCode, tableEntry.Value);
        }
    }

    #region DictionaryStuff
    bool ICollection<KeyValuePair<string, TranslationValue>>.IsReadOnly => false;
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    private class KeyCollection : ICollection<string>
    {
        private readonly SharedTranslationDictionary _instance;
        public int Count => _instance.Count;
        public bool IsReadOnly => true;
        public KeyCollection(SharedTranslationDictionary instance)
        {
            _instance = instance;
        }
        public IEnumerator<string> GetEnumerator() => _instance._underlyingTable.Keys
            .Where(tableEntry => tableEntry.TranslationKey.Equals(_instance.Translation.Key, StringComparison.Ordinal))
            .Select(static tableEntry => tableEntry.LanguageCode)
            .GetEnumerator();
        public bool Contains(string item) => _instance.ContainsKey(item);
        public void CopyTo(string[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (arrayIndex + _instance.Count > array.Length)
                throw new ArgumentException($"Array not long enough for {Count} elements.", nameof(array));

            int index = arrayIndex - 1;
            foreach (TranslationLanguageKey key in _instance._underlyingTable.Keys)
            {
                if (!key.TranslationKey.Equals(_instance.Translation.Key))
                    continue;

                array[++index] = key.LanguageCode;
            }
        }
        void ICollection<string>.Clear() => throw new NotSupportedException();
        void ICollection<string>.Add(string item) => throw new NotSupportedException();
        bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private class ValueCollection : ICollection<TranslationValue>
    {
        private readonly SharedTranslationDictionary _instance;
        public int Count => _instance.Count;
        public bool IsReadOnly => true;
        public ValueCollection(SharedTranslationDictionary instance)
        {
            _instance = instance;
        }
        public IEnumerator<TranslationValue> GetEnumerator() => _instance._underlyingTable
            .Where(tableEntry => tableEntry.Key.TranslationKey.Equals(_instance.Translation.Key, StringComparison.Ordinal))
            .Select(static tableEntry => tableEntry.Value)
            .GetEnumerator();
        public bool Contains(TranslationValue item) => _instance.ContainsValue(item);
        public void CopyTo(TranslationValue[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (arrayIndex + _instance.Count > array.Length)
                throw new ArgumentException($"Array not long enough for {Count} elements.", nameof(array));

            int index = arrayIndex - 1;
            foreach (KeyValuePair<TranslationLanguageKey, TranslationValue> tableEntry in _instance._underlyingTable)
            {
                if (!tableEntry.Key.TranslationKey.Equals(_instance.Translation.Key))
                    continue;

                array[++index] = tableEntry.Value;
            }
        }
        void ICollection<TranslationValue>.Clear() => throw new NotSupportedException();
        void ICollection<TranslationValue>.Add(TranslationValue item) => throw new NotSupportedException();
        bool ICollection<TranslationValue>.Remove(TranslationValue item) => throw new NotSupportedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    #endregion
}