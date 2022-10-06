using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

namespace Uncreated.Warfare.Actions
{

    public class Action
    {
        public readonly UCPlayer Caller;
        public readonly JsonAssetReference<EffectAsset> ViewerEffect;
        public readonly JsonAssetReference<EffectAsset>? CallerEffect;
        public readonly IEnumerable<UCPlayer> Viewers;
        public readonly IEnumerable<UCPlayer> ToastReceivers;
        public readonly EActionOrigin Origin;
        public readonly Vector3? InitialPosition;
        protected readonly int LifeTime;
        protected readonly float UpdateFrequency;
        protected bool SquadWide;
        private ActionComponent _component;

        private readonly Translation<Color> ChatMessage;
        private readonly Translation<string> Toast;

        public Action(UCPlayer caller, JsonAssetReference<EffectAsset> viewerEffect, JsonAssetReference<EffectAsset>? callerEffect, IEnumerable<UCPlayer> viewers, float updateFrequency, int lifeTime, EActionOrigin origin, Translation<Color> chatMessage, Translation<string> toast, bool squadWide = false)
            : this(caller, viewerEffect, callerEffect, viewers, viewers, updateFrequency, lifeTime, origin, chatMessage, toast, squadWide) { }
        public Action(UCPlayer caller, JsonAssetReference<EffectAsset> viewerEffect, JsonAssetReference<EffectAsset>? callerEffect, IEnumerable<UCPlayer> viewers, IEnumerable<UCPlayer> toastReceivers, float updateFrequency, int lifeTime, EActionOrigin origin, Translation<Color> chatMessage, Translation<string> toast, bool squadWide = false)
        {
            Caller = caller;
            ViewerEffect = viewerEffect;
            CallerEffect = callerEffect;
            LifeTime = lifeTime;
            Viewers = viewers;
            ToastReceivers = toastReceivers;
            UpdateFrequency = updateFrequency;
            SquadWide = squadWide;
            Origin = origin;
            ChatMessage = chatMessage;
            Toast = toast;

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

            var existing = Caller.Player.transform.GetComponents<Action>();
            foreach (var component in existing)
            {
                if (component.ViewerEffect.Guid == viewerEffect.Guid)
                    component.Cancel();
            }

            _component = Caller.Player.transform.gameObject.AddComponent<ActionComponent>();
        }
        public void Start()
        {
            if (_component == null || InitialPosition == null)
                return;

            if (CheckValid != null && !CheckValid())
                return;

            _component.Initialize(this);
            
            if (!CooldownManager.HasCooldown(Caller, ECooldownType.ACTION_ANNOUNCE, out _, ViewerEffect.Guid))
            {
                Announce();
                CooldownManager.StartCooldown(Caller, ECooldownType.ACTION_ANNOUNCE, 5, ViewerEffect.Guid);
            }
           
        }
        public void Cancel()
        {
            _component?.Destroy();
        }
        public void Complete()
        {
            OnComplete?.Invoke();
            _component?.Destroy();
        }
        private void Announce()
        {
            if (SquadWide && Caller.Squad != null)
            {
                foreach (var player in ToastReceivers)
                    Tips.TryGiveTip(player, 5, Toast, Caller.Squad.Name);
            }
            else
            {
                foreach (var player in ToastReceivers)
                    Tips.TryGiveTip(player, 5, Toast, Caller.NickName);
            }

            ulong t = Caller.GetTeam();
            Color t1 = Teams.TeamManager.GetTeamColor(t);

            foreach (LanguageSet set in LanguageSet.OnTeam(t))
            {
                string t2 = ChatMessage.Translate(set.Language, t1);
                while (set.MoveNext())
                    ChatManager.serverSendMessage(t2, Palette.AMBIENT, Caller.SteamPlayer, set.Next.SteamPlayer, EChatMode.SAY, null, true);
            }
        }
        public delegate bool CheckValidHandler();
        public delegate bool CheckCompleteHandler();
        public delegate void OnFinishedHandler();
        public delegate void OnCompleteHandler();

        public CheckValidHandler CheckValid;
        public CheckCompleteHandler LoopCheckComplete;
        public OnFinishedHandler OnFinished;
        public OnCompleteHandler OnComplete;

        public class ActionComponent : MonoBehaviour
        {
            private Action _action;

            public void Initialize(Action action)
            {
                _action = action;
                StartCoroutine(Loop());
            }
            private void SendMarkers()
            {
                Vector3 position = _action.Origin == EActionOrigin.FOLLOW_CALLER ? transform.position : _action.InitialPosition!.Value;

                foreach (var player in _action.Viewers)
                {
                    EffectManager.sendEffect(_action.ViewerEffect.Id, player.Connection, position);

                    if (_action.CallerEffect != null)
                        EffectManager.sendEffect(_action.CallerEffect.Id, _action.Caller.Connection, player.Position);
                }
            }
            public void Destroy()
            {
                if (_action.OnFinished != null)
                    _action.OnFinished();
                Destroy(this);
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
                            _action.Complete();
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
}
