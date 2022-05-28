using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Point;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public class SpottedComponent : MonoBehaviour
    {
        public Guid EffectGUID { get; private set; }
        public ESpotted Type { get; private set; }
        public ushort EffectID { get; private set; }
        public Player? CurrentSpotter { get; private set; }
        public bool IsActive { get => _coroutine != null; }
        private float _frequency;
        private int _defaultTimer;
        private Coroutine? _coroutine;
        public static readonly Guid LaserDesignatorGUID = new Guid("3879d9014aca4a17b3ed749cf7a9283e");

        public static readonly HashSet<SpottedComponent> ActiveMarkers = new HashSet<SpottedComponent>();


        public void Initialize(ESpotted type)
        {
            Type = type;
            CurrentSpotter = null;

            switch (type)
            {
                case ESpotted.INFANTRY:
                    EffectGUID = new Guid("70f75e38a90e481190ba147f25bd6e24");
                    _defaultTimer = 8;
                    _frequency = 0.5f;
                    break;
                case ESpotted.LIGHT_VEHICLE:
                    EffectGUID = new Guid("34fea0ab821141bd935b001ee82a7049");
                    _defaultTimer = 12;
                    _frequency = 0.5f;
                    break;
                case ESpotted.ARMOR:
                    EffectGUID = new Guid("1a25daa6f506441282cd30be48d27883");
                    _defaultTimer = 45;
                    _frequency = 0.5f;
                    break;
                case ESpotted.AIRCRAFT:
                    EffectGUID = new Guid("0e90e68eff624456b76fee28a4875d14");
                    _defaultTimer = 10;
                    _frequency = 0.5f;
                    break;
                case ESpotted.EMPLACEMENT:
                    EffectGUID = new Guid("0e90e68eff624456b76fee28a4875d14");
                    _defaultTimer = 10;
                    _frequency = 0.5f;
                    break;
                case ESpotted.FOB:
                    EffectGUID = new Guid("de142d979e12442fb9d44baf8f520751");
                    _defaultTimer = 90;
                    _frequency = 2;
                    break;
            }

            if (Assets.find(EffectGUID) is EffectAsset effect)
            {
                EffectID = effect.id;
            }
            else
                L.LogWarning("SpottedComponent could not initialize: Effect asset not found: " + EffectGUID);
        }
        public static void MarkTarget(Transform transform, Player spotter)
        {
            if (transform.TryGetComponent(out InteractableVehicle vehicle) && vehicle.lockedGroup.m_SteamID != spotter.GetTeam())
            {
                if (vehicle.transform.gameObject.TryGetComponent(out SpottedComponent spotted))
                {
                    if (vehicle.transform.TryGetComponent(out VehicleComponent vc))
                    {
                        spotted.TryAnnounce(spotter, Translation.Translate(vc.Data.Type.ToString().ToLower(), JSONMethods.DEFAULT_LANGUAGE).Colorize("f2a172"));
                    }

                    spotted.Activate(spotter);
                }
            }
            else if (transform.TryGetComponent(out Player player) && player.GetTeam() != spotter.GetTeam())
            {
                if (player.transform.gameObject.TryGetComponent(out SpottedComponent spotted))
                {
                    spotted.TryAnnounce(spotter, "contact");

                    spotted.Activate(spotter);
                }
            }
            else if (transform.TryGetComponent(out BarricadeDrop barricade) && barricade.GetServersideData().group != spotter.GetTeam())
            {
                if (barricade.model.gameObject.gameObject.TryGetComponent(out SpottedComponent spotted))
                {
                    spotted.TryAnnounce(spotter, "FOB".Colorize("ff7e5e"));
                    spotted.Activate(spotter);
                }
            }
        }

        public void OnTargetKilled(int assistXP)
        {
            if (CurrentSpotter != null)
            {
                Points.AwardXP(CurrentSpotter, assistXP, Translation.Translate("xp_spotted_assist", CurrentSpotter));
            }
        }

        private void OnDestroy()
        {
            L.Log("Spotted component was successfully destroyed");
            ActiveMarkers.Remove(this);

            if (_coroutine != null)
                StopCoroutine(_coroutine);
        }
        public void Activate(Player spotter) => Activate(spotter, _defaultTimer);
        public void Activate(Player spotter, int seconds)
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            CurrentSpotter = spotter;

            _coroutine = StartCoroutine(MarkerLoop(seconds));
        }
        private void SendMarkers()
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                var player = PlayerManager.OnlinePlayers[i];

                if (player.GetTeam() == CurrentSpotter!.GetTeam())
                    EffectManager.sendEffect(EffectID, player.Connection, transform.position);
            }
        }
        private void TryAnnounce(Player spotter, string targetName)
        {
            if (IsActive)
                return;

            ToastMessage.QueueMessage(spotter, new ToastMessage(Translation.Translate("spotted", spotter), EToastMessageSeverity.MINI), true);

            foreach (var player in PlayerManager.OnlinePlayers)
            {
                if (player.GetTeam() == spotter.GetTeam())
                    ChatManager.serverSendMessage($"[T] <color=#{Teams.TeamManager.GetTeamHexColor(spotter.GetTeam())}>%SPEAKER%</color>: Enemy {targetName} spotted!",
                Color.white, spotter.channel.owner, player, EChatMode.SAY, null, true);
            }
        }
        private IEnumerator<WaitForSeconds> MarkerLoop(int seconds)
        {
            ActiveMarkers.Add(this);

            int counter = 0;

            while (counter < seconds * (1 / _frequency))
            {
                SendMarkers();

                counter++;
                yield return new WaitForSeconds(_frequency);
            }
            _coroutine = null;
            CurrentSpotter = null;

            ActiveMarkers.Remove(this);
        }
        public enum ESpotted
        {
            INFANTRY,
            LIGHT_VEHICLE,
            ARMOR,
            AIRCRAFT,
            EMPLACEMENT,
            FOB
        }
    }
}
