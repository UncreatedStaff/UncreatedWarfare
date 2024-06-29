using System;
using UnityEngine;

namespace Uncreated.Warfare.Events.Components;

#pragma warning disable IDE0051
public class ThrowableComponent : MonoBehaviour
{
    internal ulong Owner;
    internal Guid Throwable;
    internal int UnityInstanceID;
    internal bool IsExplosive;
    private void Start() => UnityInstanceID = gameObject.GetInstanceID();
    private void OnDestroy()
    {
        EventDispatcher.InvokeOnThrowableDespawning(this);
    }
}

#pragma warning restore IDE0051