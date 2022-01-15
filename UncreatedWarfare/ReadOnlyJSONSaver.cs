using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Uncreated.Warfare;

public class ReadOnlyListSaver<T> : IReadOnlyList<T> where T : new()
{
    protected string directory;
    private readonly SemaphoreSlim _threadLocker = new SemaphoreSlim(1, 1);
    private readonly List<T> _list;
    private CustomDeserializer _deserializer;
    private bool useDeserializer;

    public delegate T CustomDeserializer(ref Utf8JsonReader reader);

    public int Count => _list.Count;
    public T this[int index] => _list[index];


    public ReadOnlyListSaver(string _directory) : this()
    {
        directory = _directory;
        useDeserializer = false;
    }

    private ReadOnlyListSaver()
    {
        _list = new List<T>();
        CreateFileIfNotExists(LoadDefaults());
        Reload();
    }

    public ReadOnlyListSaver(string _directory, CustomDeserializer deserializer) : this()
    {
        directory = _directory;
        _deserializer = deserializer;
        useDeserializer = deserializer != null;
    }

    public void Reload()
    {
        _threadLocker.Wait();
        if (useDeserializer)
        {
            FileStream rs = null;
            try
            {
                using (rs = new FileStream(directory, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long len = rs.Length;
                    if (len > int.MaxValue)
                    {
                        L.LogError("File " + directory + " is too large.");
                        return;
                    }
                    byte[] buffer = new byte[len];
                    rs.Read(buffer, 0, (int)len);
                    Utf8JsonReader reader = new Utf8JsonReader(buffer.AsSpan(), JsonEx.readerOptions);
                    if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
                    {
                        _list.Clear();
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                T next = _deserializer.Invoke(ref reader);
                                _list.Add(next);
                                L.Log("read " + next.ToString());
                                while (reader.Read())
                                    if (reader.TokenType == JsonTokenType.EndObject)
                                        break;
                            }
                        }
                    }

                    rs.Close();
                    rs.Dispose();
                }

                _threadLocker.Release();
                return;
            }
            catch (Exception e)
            {
                L.LogError("Failed to run custom deserializer for " + typeof(T).Name);
                L.LogError(e);
                if (rs != null)
                {
                    rs.Close();
                    rs.Dispose();
                }
            }
        }

        bool clsd = false;
        StreamReader r = null;
        try
        {
            r = File.OpenText(directory);
            string json = r.ReadToEnd();
            r.Close();
            r.Dispose();
            clsd = true;
            _threadLocker.Release();
            T[] vals = JsonSerializer.Deserialize<T[]>(json, JsonEx.serializerSettings);
            if (vals != null)
            {
                _list.Clear();
                _list.AddRange(vals);
            }
        }
        catch (Exception ex)
        {
            if (r != null && !clsd)
            {
                r.Close();
                r.Dispose();
            }

            _threadLocker.Release();
            throw new JSONSaver<T>.JSONReadException(directory, ex);
        }
    }

    protected List<T> GetObjectsWhereAsList(Func<T, bool> predicate) => _list.Where(predicate).ToList();
    protected IEnumerable<T> GetObjectsWhere(Func<T, bool> predicate) => _list.Where(predicate);
    protected T GetObject(Func<T, bool> predicate, bool readFile = false) => _list.FirstOrDefault(predicate);
    protected bool ObjectExists(Func<T, bool> match, out T item)
    {
        item = GetObject(match);
        return item != null;
    }

    protected virtual string LoadDefaults() => "[]";

    protected void CreateFileIfNotExists(string text = "[]")
    {
        if (!File.Exists(directory))
        {
            StreamWriter creator = File.CreateText(directory);
            creator.WriteLine(text);
            creator.Close();
            creator.Dispose();
        }
    }

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
}