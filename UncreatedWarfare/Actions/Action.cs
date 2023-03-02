using SDG.Unturned;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Players;
using UnityEngine;

namespace Uncreated.Warfare.Actions;

public class Action
{
    public readonly UCPlayer Caller;
    public readonly JsonAssetReference<EffectAsset> ViewerEffect;
    public readonly JsonAssetReference<EffectAsset>? CallerEffect;
    public readonly List<UCPlayer> Viewers;
    public readonly List<UCPlayer> ToastReceivers;
    public readonly EActionOrigin Origin;
    public readonly EActionType Type;
    public readonly Vector3? InitialPosition;
    protected readonly int LifeTime;
    protected readonly float UpdateFrequency;
    protected bool SquadWide;
    private readonly ActionComponent _component;

    private readonly Translation<Color>? _chatMessage;
    private readonly Translation? _toast;

    public Action(UCPlayer caller, JsonAssetReference<EffectAsset> viewerEffect, JsonAssetReference<EffectAsset>? callerEffect, IEnumerable<UCPlayer> viewers, float updateFrequency, int lifeTime, EActionOrigin origin, EActionType type, Translation<Color>? chatMessage, Translation? toast, bool squadWide = false)
        : this(caller, viewerEffect, callerEffect, viewers, viewers, updateFrequency, lifeTime, origin, type, chatMessage, toast, squadWide) { }
    public Action(UCPlayer caller, JsonAssetReference<EffectAsset> viewerEffect, JsonAssetReference<EffectAsset>? callerEffect, IEnumerable<UCPlayer> viewers, IEnumerable<UCPlayer> toastReceivers, float updateFrequency, int lifeTime, EActionOrigin origin, EActionType type, Translation<Color>? chatMessage, Translation? toast, bool squadWide = false)
    {
        Caller = caller;
        ViewerEffect = viewerEffect;
        CallerEffect = callerEffect;
        LifeTime = lifeTime;
        Viewers = viewers as List<UCPlayer> ?? viewers.ToList();
        ToastReceivers = toastReceivers as List<UCPlayer> ?? toastReceivers.ToList();
        UpdateFrequency = updateFrequency;
        SquadWide = squadWide;
        Origin = origin;
        Type = type;
        _chatMessage = chatMessage;
        _toast = toast;

        switch (Origin)
        {
            case EActionOrigin.CALLER_MARKER:
                InitialPosition = new Vector3(caller.Player.quests.markerPosition.x, F.GetTerrainHeightAt2DPoint(caller.Player.quests.markerPosition.x, caller.Player.quests.markerPosition.z), caller.Player.quests.markerPosition.z);
                break;
            case EActionOrigin.CALLER_POSITION:
                InitialPosition = caller.Position;
                break;
            case EActionOrigin.CALLER_LOOK:
                InitialPosition = Physics.Raycast(caller.Player.look.aim.position, caller.Player.look.aim.forward, out RaycastHit hit, 800) ? hit.point : null;
                break;
            default:
                InitialPosition = null;
                break;
        }


        if (!ViewerEffect.Exists)
            L.LogWarning("Action could not start: Effect asset not found: " + ViewerEffect.Guid);
        if (CallerEffect is not null && !CallerEffect.Exists)
            L.LogWarning("Action could not start: Effect asset not found: " + CallerEffect.Guid);

        ActionComponent[] existing = Caller.Player.transform.gameObject.GetComponents<ActionComponent>();
        L.Log("Existing actions: " + existing.Length);
        foreach (ActionComponent component in existing)
        {
            if (component.Action != null && Type == component.Action.Type)
            {
                L.Log("     Attempting to cancel action action...");
                component.Action.Cancel();
            }
        }

        _component = Caller.Player.transform.gameObject.AddComponent<ActionComponent>();
    }
    public void Start()
    {
        L.Log("BREAKPOINT 0");

        if (_component == null)
            return;

        if (Origin != EActionOrigin.FOLLOW_CALLER && InitialPosition == null)
            return;

        L.Log("BREAKPOINT 1");

        if (CheckValid != null && !CheckValid())
            return;

        L.Log("BREAKPOINT 2");

        _component.Initialize(this);

        if (!CooldownManager.HasCooldown(Caller, CooldownType.AnnounceAction, out _, ViewerEffect.Guid))
        {
            L.Log("BREAKPOINT 3");
            Announce();
            CooldownManager.StartCooldown(Caller, CooldownType.AnnounceAction, 5, ViewerEffect.Guid);
        }
           
    }
    public void Cancel()
    {
        if (_component != null)
            _component.Destroy();
    }
    public void CompleteAction()
    {
        Complete?.Invoke();
        Cancel();
    }
    private void Announce()
    {
        SayTeam(Caller, _chatMessage);

        if (_toast is null)
            return;

        if (SquadWide && Caller.Squad != null)
        {
            foreach (var player in ToastReceivers)
            {
                if (_toast is Translation<string> t) // TODO: better way to do account for different types of translations / clean up
                    Tips.TryGiveTip(player, 5, t, Caller.Squad.Name);
                else
                    Tips.TryGiveTip(player, 5, _toast);
            }
        }
        else
        {
            foreach (var player in ToastReceivers)
            {
                if (_toast is Translation<string> t)
                    Tips.TryGiveTip(player, 5, t, Caller.NickName);
                else
                    Tips.TryGiveTip(player, 5, _toast);
            }
        }
    }
    public static void SayTeam(UCPlayer caller, Translation<Color>? chatMessage)
    {
        if (chatMessage is null)
            return;

        ulong t = caller.GetTeam();
        Color t1 = Teams.TeamManager.GetTeamColor(t);

        foreach (LanguageSet set in LanguageSet.OnTeam(t))
        {
            string t2 = chatMessage.Translate(set.Language, t1);
            while (set.MoveNext())
                ChatManager.serverSendMessage(t2, Palette.AMBIENT, caller.SteamPlayer, set.Next.SteamPlayer, EChatMode.SAY, null, true);
        }
    }
    public delegate bool CheckValidHandler();
    public delegate bool CheckCompleteHandler();
    public delegate void FinishedHandler();
    public delegate void CompleteHandler();

    public CheckValidHandler? CheckValid;
    public CheckCompleteHandler? LoopCheckComplete;
    public FinishedHandler? Finished;
    public CompleteHandler? Complete;

    public class ActionComponent : MonoBehaviour
    {
        private Action _action;
        public Action Action => _action;

        public void Initialize(Action action)
        {
            _action = action;
            StartCoroutine(Loop());
        }
        private void SendMarkers()
        {
            Vector3 position = _action.Origin == EActionOrigin.FOLLOW_CALLER ? transform.position : _action.InitialPosition!.Value;
            _action.CallerEffect.ValidReference(out EffectAsset? callerEffect);
            if (_action.ViewerEffect.ValidReference(out EffectAsset viewerEffect))
            {
                F.TriggerEffectReliable(viewerEffect, _action.Viewers.Select(x => x.Connection), position);
                if (callerEffect != null)
                    foreach (UCPlayer player in _action.Viewers)
                        F.TriggerEffectReliable(callerEffect, player.Connection, player.Position);
            }
        }
        public void Destroy()
        {
            if (_action.Finished != null)
                _action.Finished();
            Destroy(this);
            L.Log("DESTROYED ACTION");
        }

        private IEnumerator<WaitForSeconds> Loop()
        {
            int counter = 0;

            while (counter < _action.LifeTime * (1 / _action.UpdateFrequency))
            {
                SendMarkers();

                if (counter % 3 * (1 / _action.UpdateFrequency) == 0) // every 3 seconds
                {
                    if (_action.LoopCheckComplete != null && _action.LoopCheckComplete())
                    {
                        _action.CompleteAction();
                        yield break;
                    }
                }

                counter++;
                yield return new WaitForSeconds(_action.UpdateFrequency);
            }

            Destroy();
        }
    }
}
public enum EActionOrigin
{
    FOLLOW_CALLER,
    CALLER_LOOK,
    CALLER_POSITION,
    CALLER_MARKER
}
public enum EActionType
{
    ORDER,
    SIMPLE_REQUEST,
    SQUADLEADER_REQUEST,
    EMOTE
}