using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public class SpottedComponent : MonoBehaviour
    {
        public Guid EffectGUID { get; private set; }
        public ushort EffectID { get; private set; }
        public ulong Team { get; private set; }
        public bool IsActive { get => _coroutine != null; }
        private float _frequency;
        private int _defaultTimer;
        private Coroutine? _coroutine;
        public void Initialize(Guid effectGUID, int defaultTimer, ulong team = 0, float frequency = 0.5f)
        {
            EffectGUID = effectGUID;
            Team = team;
            _defaultTimer = defaultTimer;
            _frequency = frequency;

            if (Assets.find(EffectGUID) is EffectAsset effect)
            {
                EffectID = effect.id;
            }
            else
                L.LogWarning("SpottedComponent could not initialize: Effect asset not found: " + effectGUID);
        }
        public static void MarkTarget(Transform transform)
        {
            L.Log("Spotting...");
            if (transform.TryGetComponent(out SpottedComponent spotted))
            {
                L.Log("Spot successful");

                if (transform.TryGetComponent(out InteractableVehicle vehicle))
                {
                    L.Log("     VEHICLE SPOTTED");
                }
                else if (transform.TryGetComponent(out Player player))
                {
                    L.Log("     PLAYER SPOTTED");
                }
                else if (transform.TryGetComponent(out BarricadeDrop barricade))
                {
                    L.Log("     FORTIFICATION SPOTTED");
                }
            }
        }

        public void Destroy()
        {
            Destroy(this);
        }
        public void Activate() => Activate(_defaultTimer);
        public void Activate(int seconds)
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _coroutine = StartCoroutine(MarkerLoop(seconds));
        }
        private void SendMarkers()
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                var player = PlayerManager.OnlinePlayers[i];

                if (player.GetTeam() == Team)
                    EffectManager.sendEffect(EffectID, player.Connection, transform.position);
            }
        }
        private IEnumerator<WaitForSeconds> MarkerLoop(int seconds)
        {
            int counter = 0;

            while (counter < seconds * (1 / _frequency))
            {
                SendMarkers();

                counter++;
                yield return new WaitForSeconds(_frequency);
            }
            _coroutine = null;
        }
    }
}
