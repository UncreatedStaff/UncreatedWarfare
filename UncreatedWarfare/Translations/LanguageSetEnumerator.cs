using System.Collections.Generic;
using SDG.Framework.Utilities;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Translations;

public ref struct LanguageSetEnumerator : IEnumerator<LanguageSet>
{
    private List<LanguageSet>? _list;
    private List<WarfarePlayer>? _players;
    private int _index;
    private int _isInitial;
    public LanguageSet Current { get; private set; }
    readonly object IEnumerator.Current => Current;
    internal LanguageSetEnumerator(List<LanguageSet> pooledList, List<WarfarePlayer> players)
    {
        _list = pooledList;
        _players = players;
        _index = -1;
        _isInitial = 1;
    }

    public bool MoveNext()
    {
        ++_index;
        if (_list == null || _index >= _list.Count)
            return false;
        
        Current = _list[_index];
        return true;
    }

    public void Reset()
    {
        _index = -1;
    }
    
    public void Dispose()
    {
        if (_players != null)
        {
            ListPool<WarfarePlayer>.release(_players);
            _players = null;
        }
        
        if (_list == null)
            return;

        ListPool<LanguageSet>.release(_list);
        _list = null;
    }

    public LanguageSetEnumerator GetEnumerator()
    {
        if (Interlocked.Exchange(ref _isInitial, 0) != 1)
        {
            LanguageSetEnumerator e = this;
            e.Reset();
            return e;
        }
        else return this;
    }
}