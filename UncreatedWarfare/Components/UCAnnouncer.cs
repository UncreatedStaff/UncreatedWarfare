using System;
using System.Collections.Generic;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Components;

public class UCAnnouncer : MonoBehaviour, IReloadableSingleton
{
    public Coroutine? coroutine;
    private bool stop = false;
    private readonly List<Translation> Messages = new List<Translation>(16);
    private bool _isLoaded = false;
    private int index;
    bool IUncreatedSingleton.IsLoaded => _isLoaded;
    string IReloadableSingleton.ReloadKey => "announcer";
    void IUncreatedSingleton.Load()
    {
        stop = false;
        Messages.Clear();
        for (int i = 0; i < T.Translations.Length; ++i)
            if (T.Translations[i].AttributeData != null && T.Translations[i].AttributeData!.IsAnnounced)
                Messages.Add(T.Translations[i]);
        index = 0;
        _isLoaded = true;
        coroutine = StartCoroutine(MessageLoop());
    }
    void IUncreatedSingleton.Unload()
    {
        _isLoaded = false;
        Messages.Clear();
        index = 0;
        OnDisable();
    }
    void IReloadableSingleton.Reload()
    {
        IUncreatedSingleton singleton = this;
        int ind = index;
        if (_isLoaded)
            singleton.Unload();
        singleton.Load();
        if (ind >= Messages.Count)
            index = Messages.Count == 0 ? 0 : Messages.Count - 1;
        else
            index = ind;
    }
    private IEnumerator MessageLoop()
    {
        while (!stop)
        {
            yield return new WaitForSecondsRealtime(UCWarfare.Config.SecondsBetweenAnnouncements);
            if (Messages.Count == 0)
                yield break;

            if (index >= Messages.Count)
                index = 0;
            if (Provider.clients.Count > 0)
                Chat.Broadcast(Messages[index]);
            ++index;
        }
    }
    void OnDisable()
    {
        stop = true;
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }
    bool IUncreatedSingleton.LoadAsynchrounously => false;
    bool IUncreatedSingleton.AwaitLoad => false;
    bool IUncreatedSingleton.IsLoading => false;
    bool IUncreatedSingleton.IsUnloading => false;
    Task IReloadableSingleton.ReloadAsync(CancellationToken token) => throw new NotImplementedException();
    Task IUncreatedSingleton.LoadAsync(CancellationToken token) => throw new NotImplementedException();
    Task IUncreatedSingleton.UnloadAsync(CancellationToken token) => throw new NotImplementedException();
}
