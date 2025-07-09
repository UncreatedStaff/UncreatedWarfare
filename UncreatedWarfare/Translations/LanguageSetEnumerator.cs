using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Translations;
                                          // if this is erroring ignore it, seems to be a glitch in VS as ref structs can implement interfaces
public ref struct LanguageSetEnumerator : IEnumerator<LanguageSet>
{
    private readonly bool _pooled;
    private List<LanguageSet>? _list;
    private List<WarfarePlayer>? _players;
    private int _index; // -2 means Set is single
    public LanguageSet Set;
    public LanguageSet Current => Set;
    readonly object IEnumerator.Current => Set;
    internal LanguageSetEnumerator(LanguageSet single)
    {
        Set = single;
        _index = -2;
    }
    internal LanguageSetEnumerator(WarfarePlayer player)
    {
        Set = new LanguageSet(player);
        _index = -2;
    }
    internal LanguageSetEnumerator(List<LanguageSet> pooledList, List<WarfarePlayer> players, bool pooled)
    {
        _list = pooledList;
        _players = players;
        _pooled = pooled;
        _index = -1;
    }

    public bool MoveNext()
    {
        if (_index < -1)
        {
            if (_index == -3)
                return false;

            _index = -3;
            return true;
        }

        ++_index;
        if (_list == null || _index >= _list.Count)
            return false;

        Set = _list[_index];
        return true;
    }

    public void Reset()
    {
        if (_index < -1)
        {
            _index = -2;
            return;
        }

        _index = -1;
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
        return this;
    }
    public readonly struct Cache : IEnumerable<LanguageSet>
    {
        public readonly LanguageSet[]? Sets;
        internal Cache(LanguageSet single)
        {
            Sets = [ single ];
        }

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

        public IEnumerator<LanguageSet> GetEnumerator()
        {
            return ((IEnumerable<LanguageSet>)Sets!).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Sets!.GetEnumerator();
        }
    }
}
