
//#define CONSIDER_LEAN_OBSTRUCTION

#if DEBUG
#define SPAWN_LOOK_DEBUG_PARTICLE
#endif

using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Utilities;
using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction;

/// <summary>
/// Unified location for checking what the player is looking at so multiple services don't have to check at once.
/// </summary>
//[PlayerComponent]
public class PlayerLookComponent : MonoBehaviour, IPlayerComponent
{
    private IPlayerService _playerService;

    private const float PositionTolerance = 0.0001f;
    private const float RotationTolerance = 0.000001f;

#if SPAWN_LOOK_DEBUG_PARTICLE
    private float _lastDebugParticleSpawned;
    private EffectAsset? _debugParticleAsset;
#endif

    private Ray _ray;
    private Ray _lastUpdatedRay;

    public required WarfarePlayer Player { get; init; }


    public ref Ray CrosshairRay => ref _ray;

    public bool IsLooking { get; private set; }

    public void Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();

#if SPAWN_LOOK_DEBUG_PARTICLE
        _debugParticleAsset = Assets.find<EffectAsset>(new Guid("6093290a7ce049b8a418be7fd79e89a0"));
#endif

        if (!isOnJoin)
            return;

        //PlayerInput.onPluginKeyTick += OnPluginKeyTick;
    }

    private void OnPluginKeyTick(Player player, uint simulation, byte key, bool state)
    {
        if (key != 0 || player != Player.UnturnedPlayer)
            return;

        //Tick();
    }

    private int c = 0;
    private float _leanAlpha;
#if CONSIDER_LEAN_OBSTRUCTION
    private int _lastObstructionCheck;
    private byte _leanObstructionMask;
    private static readonly Collider?[] WorkingLeanHits = new Collider?[1];
#endif

    // todo: maybe try lateupdate or earlyupdate
    private void Update()
    {
        bool log = (++c % 50) == 0;

        const int obstructionCheckEveryTicks = 4;

#if SPAWN_LOOK_DEBUG_PARTICLE
        Vector3 left = Vector3.left;
#endif
        if (Player.IsInMenu)
        {
            IsLooking = false;
            OnLookMoved();
        }
        else
        {
            Player gamePlayer = Player.UnturnedPlayer;

            InteractableVehicle currentVehicle = gamePlayer.movement.getVehicle();
            Vector3 pos, fwd;
            Transform aim = gamePlayer.look.aim;
            if (currentVehicle is null)
            {

                //PlayerKeyComponent kcomp = Player.Component<PlayerKeyComponent>();
                int lean;
                //if (gamePlayer.stance.stance is EPlayerStance.CLIMB or EPlayerStance.SPRINT or EPlayerStance.DRIVING or EPlayerStance.SITTING)
                //{
                    lean = 0;
                    _leanAlpha = 0;
                //}
                //else
                //{
                //    lean = (gamePlayer.input.keys[(int)PlayerKey.LeanLeft] ? 1 : 0) - (gamePlayer.input.keys[(int)PlayerKey.LeanRight] ? 1 : 0);
                //    if (Math.Abs(lean - _leanAlpha) > 0.01f)
                //    {
                //        _leanAlpha = Mathf.Lerp(_leanAlpha, lean, 4.0f * Time.deltaTime);
                //    }
                //    else
                //    {
                //        _leanAlpha = lean;
                //    }
                //}

                float yaw = gamePlayer.look.yaw;
#if !SPAWN_LOOK_DEBUG_PARTICLE
                Vector3
#endif
                    left = -gamePlayer.transform.right;
#if CONSIDER_LEAN_OBSTRUCTION
                retryAtZero:
#endif
                //if (_leanAlpha != 0)
                //{
                    //Vector3 playerPos = gamePlayer.transform.position;
                    //Vector3 lclOffset = aim.position - playerPos;

#if CONSIDER_LEAN_OBSTRUCTION
                    ++_lastObstructionCheck;
                    if (_lastObstructionCheck % obstructionCheckEveryTicks == 0)
                    {
                        CheckLeanHit(lean, playerPos, left);
                    }

                    if (IsLeanObstructed(lean))
                    {
                        _leanAlpha = 0;
                        goto retryAtZero;
                    }
#endif

                    //Vector3 playerFwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Sin((90f - gamePlayer.look.pitch) * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));
                    //
                    //Quaternion leanAngle = Quaternion.AngleAxis(_leanAlpha * HumanAnimator.LEAN, playerFwd);
                    //pos = leanAngle * lclOffset + playerPos;
                    //fwd = playerFwd;
                    ////if (log)
                    //WarfareModule.Singleton.GlobalLogger.LogTrace($"Lean: {lean}, pos: {pos:F1}, yaw: {yaw:F1}, fwd: {fwd:F3}, leanAngle: {leanAngle.eulerAngles}.");
                //}
                //else
                //{
#if CONSIDER_LEAN_OBSTRUCTION
                    _lastObstructionCheck = obstructionCheckEveryTicks - 1;
#endif
                    pos = aim.position;
                    fwd = aim.forward;
                    if (log)
                        WarfareModule.Singleton.GlobalLogger.LogTrace($"Lean: {lean}, pos: {pos:F1}, yaw: {yaw:F1}, fwd: {fwd:F3}.");
                //}
                SetRay(pos, fwd);
            }
            else
            {
                byte seat = gamePlayer.movement.getSeat();
                bool isLocked = seat == 0 && currentVehicle.asset.hasLockMouse;
                if (isLocked)
                {
                    pos = aim.position;
                    fwd = aim.forward;
                    SetRay(pos, fwd);
                }
                else
                {
                    Transform seatTransform = currentVehicle.passengers[seat].seat;
                    Vector3 localPosition = seatTransform.InverseTransformPoint(aim.position);
                    Quaternion relativeFwd = Quaternion.Euler(gamePlayer.look.pitch - 90f, gamePlayer.look.yaw, 0f);
                    Quaternion actualRotation = seatTransform.rotation * relativeFwd;

                    fwd = actualRotation * Vector3.forward;
                    pos = localPosition;

                    // accounts for the camera offset when turning fully around
                    if (gamePlayer.look.yaw > 0)
                    {
                        pos -= Vector3.left * gamePlayer.look.yaw / 360f;
                    }
                    else
                    {
                        pos -= Vector3.left * gamePlayer.look.yaw / 240f;
                    }

                    pos = seatTransform.TransformPoint(pos);

                    SetRay(pos, fwd);
                    if (log)
                        WarfareModule.Singleton.GlobalLogger.LogTrace($"Pos: {pos:F1}, yaw: {gamePlayer.look.yaw:F1}, pitch: {gamePlayer.look.pitch:F1}, relativeFwd: {relativeFwd.eulerAngles:F1}, actualRot: {actualRotation.eulerAngles:F1}.");
                }
            }
        }

#if SPAWN_LOOK_DEBUG_PARTICLE
        if (!Player.IsInMenu && _debugParticleAsset != null && Time.realtimeSinceStartup - _lastDebugParticleSpawned > 0.0875f && !Player.IsKeyDown(PlayerKey.PluginKey4))
        {
            EffectManager.ClearEffectByGuid(_debugParticleAsset.GUID, Player.Connection);
            Vector3 dir = CrosshairRay.direction;
            TriggerEffectParameters p = new TriggerEffectParameters(_debugParticleAsset)
            {
                position = CrosshairRay.GetPoint(1.5f),
                reliable = false
            };
            p.SetRotation(Quaternion.LookRotation(dir, Vector3.Cross(left, dir)));
            p.SetRelevantPlayer(Player.Connection);
            EffectManager.triggerEffect(p);
            p.position = CrosshairRay.origin;
            p.SetUniformScale(0.25f);
            EffectManager.triggerEffect(p);
            _lastDebugParticleSpawned = Time.realtimeSinceStartup;
        }
#endif
    }

#if CONSIDER_LEAN_OBSTRUCTION
    private bool IsLeanObstructed(int lean)
    {
        int mask = 1 << (lean != 1 ? 1 : 0);
        return (_leanObstructionMask & mask) != 0;
    }

    private void CheckLeanHit(int lean, Vector3 playerPos, Vector3 leanDir)
    {
        if (lean < 0)
            leanDir = -leanDir;

        playerPos.y += Player.UnturnedPlayer.look.heightLook;
        float testRadius = PlayerStance.RADIUS;
        float testDistance = 1.2f - testRadius;
        Vector3 endPosition = playerPos + leanDir * testDistance;
        int hitCount = Physics.OverlapCapsuleNonAlloc(playerPos, endPosition, testRadius, WorkingLeanHits, RayMasks.BLOCK_LEAN);
        int mask = 1 << (lean != 1 ? 1 : 0);
        if (hitCount == 0)
        {
            _leanObstructionMask &= (byte)~mask;
        }
        else
        {
            _leanObstructionMask |= (byte)mask;
            WorkingLeanHits[0] = null;
        }
    }
#endif

    private void SetRay(Vector3 position, Vector3 forward)
    {
        Ray newRay = new Ray(position, forward);
        _ray = newRay;
        if (position.IsNearlyEqual(_lastUpdatedRay.origin, PositionTolerance)
            && forward.IsNearlyEqual(_lastUpdatedRay.direction, RotationTolerance))
        {
            return;
        }

        _lastUpdatedRay = newRay;
        OnLookMoved();
    }

    private void OnLookMoved()
    {
        
    }

    private void OnDestroy()
    {
        //PlayerInput.onPluginKeyTick -= OnPluginKeyTick;
    }
}