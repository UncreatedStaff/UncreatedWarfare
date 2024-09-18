#if false
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Actions;

public class Action
{
    private readonly ActionManager _actionManager;
    private readonly ITranslationService _translationService;
    private readonly TipService _tipService;

    private readonly ActionComponent _component;
    private readonly EffectAsset? _viewerEffect;
    private readonly EffectAsset? _callerEffect;
    private readonly TrackingWhereEnumerable<WarfarePlayer> _viewers;
    private readonly TrackingWhereEnumerable<WarfarePlayer> _toastReceivers;
    private readonly ActionOrigin _origin;
    private readonly ActionType _type;
    private readonly Vector3? _initialPosition;
    protected readonly int LifeTime;
    protected readonly float UpdateFrequency;
    protected bool SquadWide;

    private readonly Translation<Color>? _chatMessage;
    private readonly Translation? _toast;
    public WarfarePlayer Caller { get; }
    public Vector3? InitialPosition => _initialPosition;
    public Action(ActionManager actionManager, IServiceProvider serviceProvider, WarfarePlayer caller, IAssetLink<EffectAsset> viewerEffect, IAssetLink<EffectAsset>? callerEffect, TrackingWhereEnumerable<WarfarePlayer> viewers, float updateFrequency, int lifeTime, ActionOrigin origin, ActionType type, Translation<Color>? chatMessage, Translation? toast, bool squadWide = false)
        : this(actionManager, serviceProvider, caller, viewerEffect, callerEffect, viewers, viewers, updateFrequency, lifeTime, origin, type, chatMessage, toast, squadWide) { }
    public Action(ActionManager actionManager, IServiceProvider serviceProvider, WarfarePlayer caller, IAssetLink<EffectAsset> viewerEffect, IAssetLink<EffectAsset>? callerEffect, TrackingWhereEnumerable<WarfarePlayer> viewers, TrackingWhereEnumerable<WarfarePlayer> toastReceivers, float updateFrequency, int lifeTime, ActionOrigin origin, ActionType type, Translation<Color>? chatMessage, Translation? toast, bool squadWide = false)
    {
        _actionManager = actionManager;
        _translationService = serviceProvider.GetRequiredService<ITranslationService>();
        _tipService = serviceProvider.GetRequiredService<TipService>();
        Caller = caller;

        if (!viewerEffect.TryGetAsset(out _viewerEffect))
        {
            L.LogWarning($"Unable to find viewer effect for action {_type} {_origin} for {Caller}.");
        }

        if (callerEffect != null && !callerEffect.TryGetAsset(out _callerEffect))
        {
            L.LogWarning($"Unable to find caller effect for action {_type} {_origin} for {Caller}.");
        }

        LifeTime = lifeTime;
        _viewers = viewers;
        _toastReceivers = toastReceivers;
        UpdateFrequency = updateFrequency;
        SquadWide = squadWide;
        _origin = origin;
        _type = type;
        _chatMessage = chatMessage;
        _toast = toast;

        switch (_origin)
        {
            case ActionOrigin.AtCallerLookTarget:
                Transform look = caller.UnturnedPlayer.look.aim;
                _initialPosition = Physics.Raycast(look.position, look.forward, out RaycastHit hit, 800) ? hit.point : null;
                break;

            case ActionOrigin.AtCallerPosition:
                _initialPosition = caller.Position;
                break;

            case ActionOrigin.AtCallerWaypoint:
                Vector3 marker = caller.UnturnedPlayer.quests.markerPosition;
                _initialPosition = new Vector3(marker.x, F.GetTerrainHeightAt2DPoint(marker.x, marker.z), marker.z);
                break;

            default:
                _initialPosition = null;
                break;
        }

        ActionComponent[] existing = caller.UnturnedPlayer.transform.gameObject.GetComponents<ActionComponent>();
        L.LogDebug("Existing actions: " + existing.Length);
        foreach (ActionComponent component in existing)
        {
            if (component.Action == null || component.Action._type != type)
                continue;
            
            L.LogDebug($"     Attempting to cancel {type} action...");
            component.Action.Cancel();
        }

        _component = caller.UnturnedPlayer.transform.gameObject.AddComponent<ActionComponent>();
    }
    public void Start()
    {
        if (_component == null)
            return;

        if (_origin != ActionOrigin.FollowCaller && _initialPosition == null)
            return;
        
        if (CheckValid != null && !CheckValid())
            return;
        
        _component.Initialize(this);

        if (_viewerEffect != null && CooldownManager.HasCooldown(Caller, CooldownType.AnnounceAction, out _, _viewerEffect.GUID))
            return;

        Announce();
        if (_viewerEffect != null)
            CooldownManager.StartCooldown(Caller, CooldownType.AnnounceAction, 5, _viewerEffect.GUID);
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
        _actionManager.SayTeam(Caller, _chatMessage);

        if (_toast is null)
            return;

        foreach (WarfarePlayer? player in _toastReceivers)
        {
            if (_toast is Translation<string> t) // TODO: better way to do account for different types of translations / clean up
                _tipService.TryGiveTip(player, 5, t, SquadWide && Caller.Squad != null ? Caller.Squad.Name : Caller.NickName);
            else
                _tipService.TryGiveTip(player, 5, _toast);
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
        private WaitForSecondsRealtime _waitObj;
        public Action Action => _action;
        public void Initialize(Action action)
        {
            _action = action;
            StartCoroutine(Loop());
            _waitObj = new WaitForSecondsRealtime(action.UpdateFrequency);
        }
        private void SendMarkers()
        {
            Vector3 position = _action._origin == ActionOrigin.FollowCaller ? transform.position : _action._initialPosition!.Value;
            if (_action._viewerEffect != null)
                EffectUtility.TriggerEffect(_action._viewerEffect, Data.GetPooledTransportConnectionList(_action._viewers.Select(x => x.Connection), Provider.clients.Count / 2), position, true);

            if (_action._callerEffect == null)
                return;

            foreach (WarfarePlayer player in _action._viewers)
            {
                EffectUtility.TriggerEffect(_action._callerEffect, player.Connection, player.Position, true);
            }
        }
        private IEnumerator Loop()
        {
            int counter = 0;

            float updatesPerSecond = 1 / _action.UpdateFrequency;
            int totalCounts = Mathf.CeilToInt(_action.LifeTime * updatesPerSecond);

            while (counter < totalCounts)
            {
                SendMarkers();

                if (_action.LoopCheckComplete != null && _action.LoopCheckComplete())
                {
                    _action.CompleteAction();
                    yield break;
                }

                counter++;
                yield return _waitObj;
            }

            Destroy();
        }
        public void Destroy()
        {
            _action.Finished?.Invoke();
            Destroy(this);
            L.LogDebug("DESTROYED ACTION");
        }
    }
}
public enum ActionOrigin
{
    FollowCaller,
    AtCallerLookTarget,
    AtCallerPosition,
    AtCallerWaypoint
}
public enum ActionType
{
    Order,
    SimpleRequest,
    SquadleaderRequest,
    Emote
}
#endif