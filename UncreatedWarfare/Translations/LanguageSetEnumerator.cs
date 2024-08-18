using System;
using SDG.Framework.Utilities;
using System.Collections.Generic;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Translations;
                                          // if this is erroring ignore it, seems to be a glitch in VS as ref structs can implement interfaces
public ref struct LanguageSetEnumerator : IEnumerator<LanguageSet>
{
    private readonly bool _pooled;
    private List<LanguageSet>? _list;
    private List<WarfarePlayer>? _players;
    private int _index;
    private int _isInitial;
    public LanguageSet Set;
    public LanguageSet Current => Set;
    readonly object IEnumerator.Current => Set;
    internal LanguageSetEnumerator(List<LanguageSet> pooledList, List<WarfarePlayer> players, bool pooled)
    {
        _list = pooledList;
        _players = players;
        _pooled = pooled;
        _index = -1;
        _isInitial = 1;
    }

    public bool MoveNext()
    {
        ++_index;
        if (_list == null || _index >= _list.Count)
            return false;

        Set = _list[_index];
        return true;
    }

    public void Reset()
    {
        _index = -1;
        _isInitial = 1;
    }
    
    public void Dispose()
    {
        if (_players != null)
        {
            if (_pooled)
                ListPool<WarfarePlayer>.release(_players);
            _players = null;
        }
        
        if (_list == null)
            return;

        if (_pooled)
            ListPool<LanguageSet>.release(_list);
        _list = null;
    }

    public Cache ToCache()
    {
        return new Cache(in this);
    }

    public LanguageSetEnumerator GetEnumerator()
    {
        if (Interlocked.Exchange(ref _isInitial, 0) == 1)
        {
            return this;
        }

        LanguageSetEnumerator e = this;
        e.Reset();
        return e;
    }
    public struct Cache
    {
        public readonly LanguageSet[]? Sets;
        internal Cache(in LanguageSetEnumerator enumerator)
        {
            Sets = enumerator._list?.ToArray();
            WarfarePlayer[]? players = enumerator._players?.ToArray();

            if (Sets == null || players == null)
                return;

            for (int i = 0; i < Sets.Length; ++i)
            {
                ref LanguageSet s = ref Sets[i];
                s.SetPlayers(new ArraySegment<WarfarePlayer>(players, s.StartIndex, s.Count));
            }
        }
    }
}
