using System.Buffers;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Translations;

public struct LanguageSetEnumerator : IEnumerator<LanguageSet>
{
    // ReSharper disable once NotDisposedResource
    public static readonly LanguageSetEnumerator Empty = new LanguageSetEnumerator
    {
        _index = -4
    };

    private readonly int _count;
    private LanguageSet[]? _list;
    private WarfarePlayer[]? _players;
    private int _index; // -2 means Set is single, -3 enumerated single, -4 = empty
    
    public LanguageSet Set;

    public LanguageSet Current => Set;

    public bool IsEmpty => _index == -4;
    

    internal LanguageSetEnumerator(LanguageSet single)
    {
        Set = single;
        _count = 1;
        _index = -2;
    }
    internal LanguageSetEnumerator(WarfarePlayer player)
    {
        Set = new LanguageSet(player);
        _count = 1;
        _index = -2;
    }
    internal LanguageSetEnumerator(LanguageSet[] pooledList, int count, WarfarePlayer[] players)
    {
        _list = pooledList;
        _count = count;
        _players = players;
        _index = -1;
    }

    public bool MoveNext()
    {
        if (_index < -1)
        {
            if (_index <= -3)
                return false;

            _index = -3;
            return true;
        }

        ++_index;
        if (_list == null || _index >= _count)
            return false;

        Set = _list[_index];
        return true;
    }

    public void Reset()
    {
        if (_index < -1)
        {
            if (_index < -3)
                return;
            
            _index = -2;
            return;
        }

        _index = -1;
    }
    
    public void Dispose()
    {
        if (_players != null)
        {
            ArrayPool<WarfarePlayer>.Shared.Return(_players);
            _players = null;
        }
        
        if (_list == null)
            return;

        for (int i = 0; i < _count; ++i)
        {
            ref LanguageSet set = ref _list[i];
            set.Dispose();
        }

        ArrayPool<LanguageSet>.Shared.Return(_list);
        _list = null;
    }

    // [MustDisposeResource]
    //[HandlesResourceDisposal]
    //public LanguageSetEnumerator GetEnumerator()
    //{
    //    return this;
    //}

    readonly object IEnumerator.Current => Set;
}

public static class LanguageSetEnumeratorExtensions
{
    [MustDisposeResource]
    public static LanguageSetEnumerator GetEnumerator([HandlesResourceDisposal] this LanguageSetEnumerator enumerable)
    {
        return enumerable;
    }
}