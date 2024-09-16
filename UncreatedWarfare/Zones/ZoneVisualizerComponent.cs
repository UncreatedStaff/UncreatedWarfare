using SDG.NetTransport;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Zones;

/// <summary>
/// Handles showing effects around an active zone.
/// </summary>
public class ZoneVisualizerComponent : IPlayerComponent
{
    private class SpawnRoundInfo
    {
        public float StartTime;
        public Vector2[] Points, Corners;
        public Vector2 Center;
        public EffectAsset? CenterEffect, CornerEffect, SideEffect, AirdropEffect;
        public float MinHeight;
    }

    private readonly List<SpawnRoundInfo> _spawns = new List<SpawnRoundInfo>(0);
    public WarfarePlayer Player { get; private set; }

    public void Init(IServiceProvider serviceProvider) { }

    public int SpawnPoints(Zone zone)
    {
        GameThread.AssertCurrent();

        ReadOnlySpan<byte> centerId = [ 0xfc, 0xd4, 0x15, 0x18, 0xe8, 0x66, 0x82, 0x4e, 0xa7, 0x0a, 0x59, 0x85, 0x34, 0xd8, 0xc3, 0x19 ];
        ReadOnlySpan<byte> cornerId = [ 0x08, 0x7c, 0x63, 0xe8, 0xd5, 0xf4, 0xd6, 0x4a, 0x86, 0x50, 0xc1, 0x25, 0x0b, 0x0c, 0x57, 0xa1 ];
        ReadOnlySpan<byte> sideId   = [ 0xee, 0x10, 0xde, 0x00, 0x89, 0x40, 0x10, 0x4e, 0x81, 0xe4, 0x3d, 0x1b, 0x86, 0x3d, 0x70, 0x37 ];

        EffectAsset? centerEffect  = Assets.find<EffectAsset>(new Guid(centerId));
        EffectAsset? cornerEffect  = Assets.find<EffectAsset>(new Guid(cornerId));
        EffectAsset? sideEffect    = Assets.find<EffectAsset>(new Guid(sideId));
        EffectAsset? airdropEffect = null;

        if (centerEffect == null || cornerEffect == null || sideEffect == null)
        {
            centerId = [ 0x81, 0x4d, 0xbb, 0x0b, 0x01, 0x38, 0xa8, 0x48, 0x8a, 0xef, 0x45, 0x3b, 0x3c, 0x51, 0x58, 0xbd ];
            cornerId = [ 0xfc, 0x58, 0x36, 0x56, 0x33, 0x7a, 0xbc, 0x4d, 0x8c, 0x0b, 0x9e, 0x32, 0x2a, 0xac, 0x96, 0xb9 ];
            sideId   = [ 0xab, 0x0f, 0x82, 0xd9, 0x17, 0xf8, 0xd5, 0x4e, 0x80, 0x7d, 0xc4, 0x45, 0x93, 0x80, 0x04, 0x06 ];
            ReadOnlySpan<byte> airdropId =
                       [ 0xd0, 0xfb, 0x17, 0x2c, 0xce, 0xf0, 0xae, 0x49, 0xb3, 0xbc, 0x46, 0x37, 0xb6, 0x88, 0x09, 0xa2 ];

            centerEffect  = Assets.find<EffectAsset>(new Guid(centerId));
            cornerEffect  = Assets.find<EffectAsset>(new Guid(cornerId));
            sideEffect    = Assets.find<EffectAsset>(new Guid(sideId));
            airdropEffect = Assets.find<EffectAsset>(new Guid(airdropId));
        }

        GetParticleSpawnPoints(zone, out Vector2[] sidePoints, out Vector2[] corners, out Vector2 center);

        SpawnRoundInfo info = new SpawnRoundInfo
        {
            StartTime = Time.realtimeSinceStartup,
            Center = center,
            Corners = corners,
            Points = sidePoints,
            CenterEffect = centerEffect,
            CornerEffect = cornerEffect,
            SideEffect = sideEffect,
            AirdropEffect = airdropEffect,
            MinHeight = zone.GetMinHeight()
        };

        Spawn(info);

        _spawns.Add(info);

        Player.UnturnedPlayer.StartCoroutine(CleanUpSpawns());

        return sidePoints.Length + corners.Length + 1;
    }

    private IEnumerator CleanUpSpawns()
    {
        yield return new WaitForSeconds(61f);

        ITransportConnection connection = Player.Connection;
        HashSet<Guid> removed = new HashSet<Guid>(4);
        bool needsRespawn = false;
        for (int i = 0; i < _spawns.Count; ++i)
        {
            SpawnRoundInfo info = _spawns[i];
            if (Time.realtimeSinceStartup - info.StartTime < 60)
                break;

            if (info.CenterEffect != null && removed.Add(info.CenterEffect.GUID))
            {
                EffectManager.ClearEffectByGuid(info.CenterEffect.GUID, connection);
            }
            if (info.CornerEffect != null && removed.Add(info.CornerEffect.GUID))
            {
                EffectManager.ClearEffectByGuid(info.CornerEffect.GUID, connection);
            }
            if (info.SideEffect != null && removed.Add(info.SideEffect.GUID))
            {
                EffectManager.ClearEffectByGuid(info.SideEffect.GUID, connection);
            }
            if (info.AirdropEffect != null && removed.Add(info.AirdropEffect.GUID))
            {
                EffectManager.ClearEffectByGuid(info.AirdropEffect.GUID, connection);
            }

            _spawns.RemoveAt(i);
            --i;
            needsRespawn = true;
        }

        if (!needsRespawn)
            yield break;

        // respawn active effects
        for (int i = _spawns.Count - 1; i >= 0; --i)
        {
            SpawnRoundInfo info = _spawns[i];
            if (info.CenterEffect != null && removed.Contains(info.CenterEffect.GUID)
                || info.CornerEffect != null && removed.Contains(info.CornerEffect.GUID)
                || info.SideEffect != null && removed.Contains(info.SideEffect.GUID)
                || info.AirdropEffect != null && removed.Contains(info.AirdropEffect.GUID))
            {
                Spawn(info);
            }
        }
    }

    private void Spawn(SpawnRoundInfo info)
    {
        bool hasCommonZonesWorkshopInstalled = info.AirdropEffect == null;
        ITransportConnection channel = Player.Connection;

        // Border
        foreach (Vector2 point in info.Points)
        {
            Vector3 pointPos = new Vector3(point.x, 0f, point.y);
            pointPos.y = TerrainUtility.GetHighestPoint(pointPos, info.MinHeight);

            F.TriggerEffectReliable(info.SideEffect!, channel, pointPos);
            if (!hasCommonZonesWorkshopInstalled)
            {
                F.TriggerEffectReliable(info.AirdropEffect!, channel, pointPos);
            }
        }

        // Corners
        foreach (Vector2 point in info.Corners)
        {
            Vector3 cornerPos = new Vector3(point.x, 0f, point.y);
            cornerPos.y = TerrainUtility.GetHighestPoint(cornerPos, info.MinHeight);
            F.TriggerEffectReliable(info.CornerEffect!, channel, cornerPos);
            if (!hasCommonZonesWorkshopInstalled)
            {
                F.TriggerEffectReliable(info.AirdropEffect!, channel, cornerPos);
            }
        }

        // Center
        Vector3 centerPos = new Vector3(info.Center.x, 0f, info.Center.y);
        centerPos.y = TerrainUtility.GetHighestPoint(centerPos, info.MinHeight);
        F.TriggerEffectReliable(info.CenterEffect!, channel, centerPos);
        if (!hasCommonZonesWorkshopInstalled)
        {
            F.TriggerEffectReliable(info.AirdropEffect!, channel, centerPos);
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    public void GetParticleSpawnPoints(Zone zone, out Vector2[] sidePoints, out Vector2[] corners, out Vector2 center)
    {
        center = zone.Center;
        switch (zone.Shape)
        {
            case ZoneShape.Cylinder or ZoneShape.Sphere when zone.CircleInfo != null:
                const float tau = 2f * Mathf.PI;
                float spacing = 18f;

                float circumference = tau * zone.CircleInfo.Radius;
                float answer = circumference / spacing;
                int remainder = (int)Mathf.Round((answer - Mathf.Floor(answer)) * spacing);
                int canfit = (int)Mathf.Floor(answer);
                if (remainder != 0)
                {
                    if (remainder < spacing / 2)            // extend all others
                        spacing = circumference / canfit;
                    else                                    // add one more and subtend all others
                        spacing = circumference / ++canfit;
                }

                float angleRad = spacing / zone.CircleInfo.Radius;
                Vector2[] pts = new Vector2[canfit];
                int index = -1;
                for (float i = 0; i < tau; i += angleRad)
                {
                    pts[++index] = new Vector2(center.x + Mathf.Cos(i) * zone.CircleInfo.Radius, center.y + Mathf.Sin(i) * zone.CircleInfo.Radius);
                }
                sidePoints = pts;
                corners = Array.Empty<Vector2>();
                break;

            case ZoneShape.AABB when zone.AABBInfo != null:
                spacing = 10f;
                Vector2 size = new Vector2(zone.AABBInfo.Size.x, zone.AABBInfo.Size.z);
                List<Vector2> rtnSpawnPoints = new List<Vector2>(64);
            
                corners =
                [
                    new Vector2(center.x - size.x / 2, center.y - size.y / 2), // tl
                    new Vector2(center.x + size.x / 2, center.y - size.y / 2), // tr
                    new Vector2(center.x + size.x / 2, center.y + size.y / 2), // br
                    new Vector2(center.x - size.x / 2, center.y + size.y / 2)  // bl
                ];

                for (int i1 = 0; i1 < 4; i1++)
                {
                    ref readonly Vector2 p1 = ref corners[i1];
                    ref readonly Vector2 p2 = ref corners[(i1 + 1) % 4];

                    float length = (p2 - p1).magnitude;
                    if (length == 0)
                        continue;

                    float distance = NormalizeSpacing(length, spacing);
                    if (distance == 0) // prevent infinite loops
                        continue;
                    
                    for (float i = distance; i < length; i += distance)
                    {
                        rtnSpawnPoints.Add(Vector2.Lerp(p1, p2, i / length));
                    }
                }

                sidePoints = rtnSpawnPoints.ToArray();
                break;

            case ZoneShape.Polygon when zone.PolygonInfo != null:
                spacing = 10f;

                Vector2[] relCorners = zone.PolygonInfo.Points;
                rtnSpawnPoints = new List<Vector2>(64);

                corners = new Vector2[relCorners.Length];
                for (int i = 0; i < relCorners.Length; ++i)
                {
                    ref readonly Vector2 relCorner = ref relCorners[i];
                    corners[i] = new Vector2(relCorner.x + center.x, relCorner.y + center.y);
                }

                for (int i1 = 0; i1 < corners.Length; i1++)
                {
                    ref readonly Vector2 p1 = ref corners[i1];
                    ref readonly Vector2 p2 = ref corners[(i1 + 1) % corners.Length];

                    float length = (p2 - p1).magnitude;
                    if (length == 0)
                        continue;

                    float distance = NormalizeSpacing(length, spacing);
                    if (distance == 0) // prevent infinite loops
                        continue;

                    for (float i = distance; i < length; i += distance)
                    {
                        rtnSpawnPoints.Add(Vector2.Lerp(p1, p2, i / length));
                    }
                }

                sidePoints = rtnSpawnPoints.ToArray();
                break;

            default:
                sidePoints = Array.Empty<Vector2>();
                corners = Array.Empty<Vector2>();
                break;
        }
    }

    private static float NormalizeSpacing(float length, float baseSpacing)
    {
        float answer = length / baseSpacing;
        int remainder = Mathf.RoundToInt((answer - Mathf.Floor(answer)) * baseSpacing);
        int canfit = Mathf.FloorToInt(answer);

        if (remainder == 0)
            return baseSpacing;

        if (remainder < baseSpacing / 2)     // extend all others
            return length / canfit;

        //add one more and subtend all others
        return length / (canfit + 1);
    }
}