using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Networking;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;
internal class ZonePlayerComponent : MonoBehaviour
{
    private const float NEARBY_POINT_DISTANCE_SQR = 25f;
    private const int POINT_ROWS = 30;
    private const int HELP_ROWS = 11;
    private UCPlayer player = null!;
    private static EffectAsset? _edit = null!;
    private const short EDIT_KEY = 25432;
    private const string ZONE_EDIT_USAGE = "/zone edit <existing|maxheight|minheight|finalize|cancel|use case|addpoint|delpoint|clearpoints|setpoint|orderpoint|radius|sizex|sizez|center|name|short name|type|> [value]";
    private ZoneBuilder? _currentBuilder;
    private bool _currentBuilderIsExisting = false;
    private List<Vector2>? _currentPoints;
    private float _lastZonePreviewRefresh = 0f;
    private static readonly List<ZonePlayerComponent> _builders = new List<ZonePlayerComponent>(2);
    internal static EffectAsset? _airdrop = null ;
    internal static EffectAsset  _center  = null!;
    internal static EffectAsset  _corner  = null!;
    internal static EffectAsset  _side    = null!;
    private int _closestPoint = -1;
    private int _lastPtCheck = -4;
    private readonly List<Transaction> UndoBuffer = new List<Transaction>(16);
    private readonly List<Transaction> RedoBuffer = new List<Transaction>(4);
    internal static void UIInit()
    {
        _edit           =  Assets.find<EffectAsset>(new Guid("503fed1019db4c7e9c365bf6e108b43f"));
        _center         =  Assets.find<EffectAsset>(new Guid("1815d4fc66e84e82a70a598534d8c319"));
        _corner         =  Assets.find<EffectAsset>(new Guid("e8637c08f4d54ad68650c1250b0c57a1"));
        _side           =  Assets.find<EffectAsset>(new Guid("00de10ee40894e1081e43d1b863d7037"));
        _airdrop = null;
        if (_center == null || _corner == null || _side == null)
        {
            _airdrop    = Assets.find<EffectAsset>(new Guid("2c17fbd0f0ce49aeb3bc4637b68809a2"))!;
            _center     = Assets.find<EffectAsset>(new Guid("0bbb4d81380148a88aef453b3c5158bd"))!;
            _corner     = Assets.find<EffectAsset>(new Guid("563658fc7a334dbc8c0b9e322aac96b9"))!;
            _side       = Assets.find<EffectAsset>(new Guid("d9820fabf8174ed5807dc44593800406"))!;
        }
    }
    internal void Init(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        this.player = player;
        Update();
    }
    private void Update()
    {
        if (_currentBuilder is not null)
        {
            float t = Time.time;
            if (t - _lastZonePreviewRefresh > 55f)
                RefreshPreview();
            else if (_currentBuilder.ZoneType == EZoneType.POLYGON && (_closestPoint == -1 || t - _lastPtCheck > 2f) && _currentPoints is not null && _currentPoints.Count > 0)
            {
                Vector3 pos = player.Position;
                if (pos == default)
                    return;
                Vector2 v = new Vector2(pos.x, pos.z);
                float min = 0f;
                int ind = -1;
                for (int i = 0; i < _currentPoints.Count; ++i)
                {
                    float sqrdist = (v - _currentPoints[i]).sqrMagnitude;
                    if (ind == -1 || sqrdist < min)
                    {
                        min = sqrdist;
                        ind = i;
                    }
                }
                if (_closestPoint != ind)
                    UpdateClosestPoint(ind);
            }
        }
    }
    private void UpdateClosestPoint(int newInd)
    {
        int old = _closestPoint;
        _closestPoint = newInd;
        ITransportConnection tc = player.Connection;
        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + newInd, PointText(newInd));
        if (old > -1 && old < _currentPoints!.Count)
            EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + old, PointText(old));
    }
    internal void UtilCommand(CommandInteraction ctx)
    {
        ThreadUtil.assertIsGameThread();
        if (!ctx.HasArgs(1))
        {
            throw ctx.SendCorrectUsage("/zone util <location>");
        }
        if (ctx.MatchParameter(0, "location", "position", "loc", "pos"))
        {
            Vector3 p = player.Player.transform.position;
            throw ctx.Reply(T.ZoneUtilLocation,
                p.x,
                p.y,
                p.z,
                player.Player.transform.rotation.eulerAngles.y);
        }
    }

    internal void DeleteCommand(CommandInteraction ctx)
    {
        ThreadUtil.assertIsGameThread();
        Zone zone;
        if (!ctx.HasArgs(1))
        {
            Vector3 pos = player.Position;
            if (pos == default) return;
            List<int> t = new List<int>(2);
            for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
            {
                if (Data.ZoneProvider.Zones[i].IsInside(pos))
                {
                    t.Add(i);
                }
            }
            if (t.Count == 1)
            {
                zone = Data.ZoneProvider.Zones[t[0]];
            }
            else
            {
                ctx.Reply(T.ZoneDeleteZoneNotInZone);
                return;
            }
        }
        else
        {
            if (!ctx.TryGetRange(0, out string name))
            {
                ctx.Reply(T.ZoneDeleteZoneNotFound, Translation.Null(T.ZoneDeleteZoneNotFound.Flags));
                return;
            }
            zone = null!;
            if (int.TryParse(name, System.Globalization.NumberStyles.Any, Data.Locale, out int id) && id > -1)
            {
                for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                {
                    if (Data.ZoneProvider.Zones[i].Id == id)
                    {
                        zone = Data.ZoneProvider.Zones[i];
                        break;
                    }
                }
            }
            if (zone is null)
            {
                for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                {
                    if (Data.ZoneProvider.Zones[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        zone = Data.ZoneProvider.Zones[i];
                        break;
                    }
                }
                if (zone is null)
                {
                    for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                    {
                        if (Data.ZoneProvider.Zones[i].Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            zone = Data.ZoneProvider.Zones[i];
                            break;
                        }
                    }
                }
            }
            if (zone is null)
            {
                ctx.Reply(T.ZoneDeleteZoneNotFound, name);
                return;
            }
        }
        ctx.Reply(T.ZoneDeleteZoneConfirm, zone);
        Task.Run(async () =>
        {
            if (await CommandWaiter.WaitAsync(player, "confirm", 10000))
            {
                await UCWarfare.ToUpdate();

                for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                {
                    if (Data.ZoneProvider.Zones[i].Name.Equals(zone.Name, StringComparison.Ordinal))
                    {
                        int id = Data.ZoneProvider.Zones[i].Id;
                        Data.ZoneProvider.Zones.RemoveAt(i);
                        Data.ZoneProvider.Save();
                        for (int j = 0; j < _builders.Count; ++j)
                        {
                            ZonePlayerComponent b = _builders[j];
                            if (b._currentBuilder is not null && b._currentBuilderIsExisting && b._currentBuilder!.Id == id)
                                b.OnDeleted();
                        }
                        ctx.Reply(T.ZoneDeleteZoneSuccess, zone);
                        return;
                    }
                }
                ctx.Reply(T.ZoneDeleteZoneNotFound, zone.Name);
            }
            else
                ctx.Reply(T.ZoneDeleteDidNotConfirm, zone);
        });
        ctx.Defer();
    }

    private void OnDeleted()
    {
        player.SendChat(T.ZoneDeleteEditingZoneDeleted);
        _currentBuilderIsExisting = false;

        int nextId = Data.ZoneProvider.NextFreeID();
        for (int i = _builders.Count - 1; i >= 0; --i)
        {
            ZoneBuilder? zb = _builders[i]._currentBuilder;
            if (zb == null)
            {
                _builders.RemoveAt(i);
                continue;
            }
            if (zb.Id >= nextId)
            {
                nextId = zb.Id + 1;
            }
        }
        _currentBuilder!.Id = nextId;
    }

    internal void CreateCommand(CommandInteraction ctx)
    {
        ThreadUtil.assertIsGameThread();
        if (ctx.HasArgs(2))
            throw ctx.SendCorrectUsage("/zone create <polygon|rectange|circle> <name>");

        EZoneType type = EZoneType.INVALID;
        if (ctx.MatchParameter(0, "rect", "rectangle", "square", "sqr"))
        {
            type = EZoneType.RECTANGLE;
        }
        if (ctx.MatchParameter(0, "circle", "oval", "ellipse"))
        {
            type = EZoneType.CIRCLE;
        }
        if (ctx.MatchParameter(0, "polygon", "poly", "shape"))
        {
            type = EZoneType.POLYGON;
        }
        if (type == EZoneType.INVALID)
            throw ctx.SendCorrectUsage("Invalid Type - /zone create <polygon|rectange|circle> <name>");

        string name = ctx.GetRange(1)!;

        for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
            if (Data.ZoneProvider.Zones[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                throw ctx.Reply(T.ZoneCreateNameTaken, Data.ZoneProvider.Zones[i].Name);
        
        for (int i = _builders.Count - 1; i >= 0; --i)
        {
            ZoneBuilder? zb = _builders[i]._currentBuilder;
            if (zb == null)
            {
                _builders.RemoveAt(i);
                continue;
            }
            if (zb.Name != null && zb.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                throw ctx.Reply(T.ZoneCreateNameTakenEditing, zb.Name, _builders[i].player);
        }

        int nextId = Data.ZoneProvider.NextFreeID();
        for (int i = _builders.Count - 1; i >= 0; --i)
        {
            ZoneBuilder? zb = _builders[i]._currentBuilder;
            if (zb == null)
            {
                _builders.RemoveAt(i);
                continue;
            }
            if (zb.Id >= nextId)
            {
                nextId = zb.Id + 1;
            }
        }

        Vector3 pos = player.Position;
        _currentBuilder = new ZoneBuilder()
        {
            Name = name,
            UseMapCoordinates = false,
            X = pos.x,
            Z = pos.z,
            ZoneType = type,
            Id = nextId,
            Adjacencies = Array.Empty<AdjacentFlagData>()
        };
        UndoBuffer.Clear();
        RedoBuffer.Clear();
        _currentBuilderIsExisting = false;
        switch (type)
        {
            case EZoneType.RECTANGLE:
                _currentBuilder.ZoneData.SizeX = 10f;
                _currentBuilder.ZoneData.SizeZ = 10f;
                break;
            case EZoneType.CIRCLE:
                _currentBuilder.ZoneData.Radius = 5f;
                break;
        }
        _builders.Add(this);
        ITransportConnection tc = player.Player.channel.owner.transportConnection;
        string text = Localization.TranslateEnum(type, player.Steam64);
        if (_edit != null)
        {
            Data.SendEffectClearAll.InvokeAndLoopback(ENetReliability.Reliable, new ITransportConnection[] { player.Player.channel.owner.transportConnection });
            EffectManager.sendUIEffect(_edit.id, EDIT_KEY, tc, true);
            player.HasUIHidden = true;
            EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Name", name);
            EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Type", text);
            EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Header", T.ZoneEditSuggestedCommandsHeader.Translate(player));
        }
        ctx.Reply(T.ZoneCreated, name, type);
        CheckType(type, true, @implicit: false);
        RefreshPreview();
    }
    private void RefreshPreview()
    {
        ThreadUtil.assertIsGameThread();
        _lastZonePreviewRefresh = Time.time;
        ITransportConnection channel = player.Player.channel.owner.transportConnection;
        if (_airdrop != null)
            EffectManager.askEffectClearByID(_airdrop.id, channel);
        EffectManager.askEffectClearByID(_side.id, channel);
        EffectManager.askEffectClearByID(_corner.id, channel);
        EffectManager.askEffectClearByID(_center.id, channel);
        if (_currentBuilder == null) return;
        Vector3 pos = new Vector3(_currentBuilder.X, 0f, _currentBuilder.Z);
        pos.y = F.GetHeight(pos, _currentBuilder.MinHeight);
        F.TriggerEffectReliable(_center, channel, pos); // purple paintball splatter
        if (_airdrop != null)
            F.TriggerEffectReliable(_airdrop, channel, pos); // airdrop
        switch (_currentBuilder.ZoneType)
        {
            case EZoneType.CIRCLE:
                if (!float.IsNaN(_currentBuilder.ZoneData.Radius))
                {
                    CircleZone.CalculateParticleSpawnPoints(out Vector2[] points, _currentBuilder.ZoneData.Radius, new Vector2(_currentBuilder.X, _currentBuilder.Z));
                    for (int i = 0; i < points.Length; i++)
                    {
                        ref Vector2 point = ref points[i];
                        pos = new Vector3(point.x, 0f, point.y);
                        pos.y = F.GetHeight(pos, _currentBuilder.MinHeight);
                        F.TriggerEffectReliable(_side, channel, pos); // yellow paintball splatter
                        if (_airdrop != null)
                            F.TriggerEffectReliable(_airdrop, channel, pos); // airdrop
                    }
                }
                break;
            case EZoneType.RECTANGLE:
                if (!float.IsNaN(_currentBuilder.ZoneData.SizeX) && !float.IsNaN(_currentBuilder.ZoneData.SizeZ))
                {
                    RectZone.CalculateParticleSpawnPoints(out Vector2[] points, out Vector2[] corners,
                        new Vector2(_currentBuilder.ZoneData.SizeX, _currentBuilder.ZoneData.SizeZ), new Vector2(_currentBuilder.X, _currentBuilder.Z));
                    for (int i = 0; i < points.Length; i++)
                    {
                        ref Vector2 point = ref points[i];
                        pos = new Vector3(point.x, 0f, point.y);
                        pos.y = F.GetHeight(pos, _currentBuilder.MinHeight);
                        F.TriggerEffectReliable(_side, channel, pos); // yellow paintball splatter
                        if (_airdrop != null)
                            F.TriggerEffectReliable(_airdrop, channel, pos); // airdrop
                    }
                    for (int i = 0; i < corners.Length; i++)
                    {
                        ref Vector2 point = ref corners[i];
                        pos = new Vector3(point.x, 0f, point.y);
                        pos.y = F.GetHeight(pos, _currentBuilder.MinHeight);
                        F.TriggerEffectReliable(_corner, channel, pos); // red paintball splatter
                        if (_airdrop != null)
                            F.TriggerEffectReliable(_airdrop, channel, pos); // airdrop
                    }
                }
                break;
            case EZoneType.POLYGON:
                if (_currentPoints != null && _currentPoints.Count > 2)
                {
                    PolygonZone.CalculateParticleSpawnPoints(out Vector2[] points, _currentPoints);
                    for (int i = 0; i < points.Length; i++)
                    {
                        ref Vector2 point = ref points[i];
                        pos = new Vector3(point.x, 0f, point.y);
                        pos.y = F.GetHeight(pos, _currentBuilder.MinHeight);
                        F.TriggerEffectReliable(_side, channel, pos); // yellow paintball splatter
                        if (_airdrop != null)
                            F.TriggerEffectReliable(_airdrop, channel, pos); // airdrop
                    }
                    for (int i = 0; i < _currentPoints.Count; i++)
                    {
                        Vector2 point = _currentPoints[i];
                        pos = new Vector3(point.x, 0f, point.y);
                        pos.y = F.GetHeight(pos, _currentBuilder.MinHeight);
                        F.TriggerEffectReliable(_corner, channel, pos); // red paintball splatter
                        if (_airdrop != null)
                            F.TriggerEffectReliable(_airdrop, channel, pos); // airdrop
                    }
                }
                break;
        }
    }
    internal void EditCommand(CommandInteraction ctx)
    {
        ThreadUtil.assertIsGameThread();
        if (!ctx.HasArgs(1))
            throw ctx.SendCorrectUsage(ZONE_EDIT_USAGE);

        Vector3 pos = player.Position;
        if (pos == default)
            return;
        ctx.Defer();
        if (ctx.MatchParameter(0, "existing", "open", "current"))
        {
            if (_currentBuilder != null)
                throw ctx.Reply(T.ZoneEditExistingInProgress);

            Zone? zone = null;
            if (ctx.HasArgsExact(1))
            {
                List<int> t = new List<int>(2);
                for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                {
                    if (Data.ZoneProvider.Zones[i].IsInside(pos))
                    {
                        t.Add(i);
                    }
                }
                if (t.Count == 1)
                {
                    zone = Data.ZoneProvider.Zones[t[0]];
                }
                else
                    throw ctx.Reply(T.ZoneEditExistingInvalid);
            }
            else
            {
                string name = ctx.GetRange(1)!;
                if (int.TryParse(name, System.Globalization.NumberStyles.Any, Data.Locale, out int id) && id > -1)
                {
                    for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                    {
                        if (Data.ZoneProvider.Zones[i].Id == id)
                        {
                            zone = Data.ZoneProvider.Zones[i];
                            break;
                        }
                    }
                }
                if (zone == null)
                {
                    for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                    {
                        if (Data.ZoneProvider.Zones[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            zone = Data.ZoneProvider.Zones[i];
                            break;
                        }
                    }
                    if (zone == null)
                    {
                        for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                        {
                            if (Data.ZoneProvider.Zones[i].Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                zone = Data.ZoneProvider.Zones[i];
                                break;
                            }
                        }
                    }
                }
            }

            if (zone == null)
                throw ctx.Reply(T.ZoneEditExistingInvalid);

            _currentBuilderIsExisting = true;
            _currentBuilder = zone.Builder;
            _currentPoints?.Clear();
            UndoBuffer.Clear();
            RedoBuffer.Clear();
            if (_currentBuilder.ZoneType == EZoneType.POLYGON && _currentBuilder.ZoneData.Points != null)
            {
                if (_currentPoints == null)
                    _currentPoints = new List<Vector2>(_currentBuilder.ZoneData.Points);
                else
                    _currentPoints.AddRange(_currentBuilder.ZoneData.Points);
            }
            _builders.Add(this);
            ITransportConnection tc = player.Player.channel.owner.transportConnection;
            string text = Localization.TranslateEnum(_currentBuilder.ZoneType, player.Steam64);
            if (_edit != null)
            {
                Data.SendEffectClearAll.InvokeAndLoopback(ENetReliability.Reliable, new ITransportConnection[] { player.Player.channel.owner.transportConnection });
                EffectManager.sendUIEffect(_edit.id, EDIT_KEY, tc, true);
                player.HasUIHidden = true;
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Name", _currentBuilder.Name);
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Type", text);
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Header", T.ZoneEditSuggestedCommandsHeader.Translate(player));
            }
            ctx.Reply(T.ZoneEditExistingSuccess, _currentBuilder.Name!, _currentBuilder.ZoneType);
            CheckType(_currentBuilder.ZoneType, true, @implicit: false);
            RefreshPreview();
        }
        else
        {
            if (_currentBuilder == null)
                throw ctx.Reply(T.ZoneEditNotStarted);
            if (ctx.MatchParameter(0, "maxheight", "maxy", "max-y"))
            {
                float mh;
                if (ctx.HasArgsExact(2))
                {
                    if (!ctx.TryGet(1, out mh))
                    {
                        ctx.Reply(T.ZoneEditMaxHeightInvalid);
                        return;
                    }
                }
                else
                {
                    mh = pos.y;
                }
                AddTransaction(new SetFloatTransaction(_currentBuilder.MaxHeight, mh, SetFloatTransaction.EFloatType.MAX_Y));
                SetMaxHeight(mh);
            }
            else if (ctx.MatchParameter(0, "minheight", "miny", "min-y"))
            {
                float mh;
                if (ctx.HasArgsExact(2))
                {
                    if (!ctx.TryGet(1, out mh))
                    {
                        ctx.Reply(T.ZoneEditMinHeightInvalid);
                        return;
                    }
                }
                else
                {
                    mh = pos.y;
                }
                AddTransaction(new SetFloatTransaction(_currentBuilder.MinHeight, mh, SetFloatTransaction.EFloatType.MIN_Y));
                SetMinHeight(mh);
            }
            else if (ctx.MatchParameter(0, "type", "shape", "mode"))
            {
                if (ctx.HasArgs(2))
                {
                    EZoneType type = EZoneType.INVALID;
                    if (ctx.MatchParameter(1, "rect", "rectangle", "square"))
                    {
                        type = EZoneType.RECTANGLE;
                        if (float.IsNaN(_currentBuilder.ZoneData.SizeX))
                            _currentBuilder.ZoneData.SizeX = 10f;
                        if (float.IsNaN(_currentBuilder.ZoneData.SizeZ))
                            _currentBuilder.ZoneData.SizeZ = 10f;
                    }
                    if (ctx.MatchParameter(1, "circle", "oval", "ellipse"))
                    {
                        type = EZoneType.CIRCLE;
                        if (float.IsNaN(_currentBuilder.ZoneData.Radius))
                            _currentBuilder.ZoneData.Radius = 5f;
                    }
                    if (ctx.MatchParameter(1, "polygon", "poly", "shape", "custom"))
                    {
                        type = EZoneType.POLYGON;
                    }
                    if (type == EZoneType.INVALID)
                    {
                        ctx.Reply(T.ZoneEditTypeInvlaid);
                        return;
                    }
                    if (type == _currentBuilder.ZoneType)
                    {
                        ctx.Reply(T.ZoneEditTypeAlreadySet, type);
                        return;
                    }
                    CheckType(type, @implicit: false, transact: true);
                    ctx.Reply(T.ZoneEditTypeSuccess, type);
                }
                else
                {
                    ctx.Reply(T.ZoneEditTypeInvlaid);
                    return;
                }
            }
            else if (ctx.MatchParameter(0, "finalize", "complete", "confirm", "save"))
            {
                try
                {
                    if (_currentBuilder.UseCase == EZoneUseCase.OTHER || _currentBuilder.UseCase > EZoneUseCase.LOBBY)
                    {
                        ctx.Reply(T.ZoneEditFinalizeUseCaseUnset);
                        return;
                    }
                    int replIndex = -1;
                    for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                    {
                        if (Data.ZoneProvider.Zones[i].Id == _currentBuilder.Id)
                        {
                            if (!_currentBuilderIsExisting)
                            {
                                ctx.Reply(T.ZoneEditFinalizeExists);
                                return;
                            }
                            else
                            {
                                replIndex = i;
                                break;
                            }
                        }
                    }
                    if (_currentBuilder.ZoneType == EZoneType.POLYGON && _currentPoints != null)
                        _currentBuilder.Points = _currentPoints.ToArray();
                    ZoneModel mdl;
                    try
                    {
                        mdl = _currentBuilder.Build();
                    }
                    catch (ZoneAPIException ex1)
                    {
                        ctx.Reply(T.ZoneEditFinalizeFailure, ex1.Message);
                        return;
                    }
                    catch (ZoneReadException ex2)
                    {
                        ctx.Reply(T.ZoneEditFinalizeFailure, ex2.Message);
                        return;
                    }
                    Zone zone = mdl.GetZone();
                    bool @new;
                    if (replIndex == -1)
                    {
                        replIndex = Data.ZoneProvider.Zones.Count;
                        Data.ZoneProvider.Zones.Add(zone);
                        @new = true;
                    }
                    else
                    {
                        Data.ZoneProvider.Zones[replIndex] = zone;
                        @new = false;
                    }
                    Data.ZoneProvider.Save();
                    _builders.Remove(this);
                    ctx.Reply(@new ? T.ZoneEditFinalizeSuccess : T.ZoneEditFinalizeOverwrote, zone);
                    _currentPoints = null;
                    _currentBuilder = null;
                    UndoBuffer.Clear();
                    RedoBuffer.Clear();
                    if (_edit != null)
                        EffectManager.askEffectClearByID(_edit.id, player.Player.channel.owner.transportConnection);
                    player.HasUIHidden = false;
                    UCWarfare.I.UpdateLangs(player);
                    _currentBuilderIsExisting = false;
                    RefreshPreview();
                }
                catch (Exception ex)
                {
                    ctx.Reply(T.ZoneEditFinalizeFailure, ex.Message);
                }
            }
            else if (ctx.MatchParameter(0, "cancel", "discard", "exit"))
            {
                ctx.Reply(T.ZoneEditCancelled, _currentBuilder.Name ?? Translation.Null(T.ZoneEditCancelled.Flags));
                _currentBuilder = null;
                _currentPoints = null;
                UndoBuffer.Clear();
                RedoBuffer.Clear();
                _builders.Remove(this);
                if (_edit != null)
                    EffectManager.askEffectClearByID(_edit.id, player.Player.channel.owner.transportConnection);
                player.HasUIHidden = false;
                UCWarfare.I.UpdateLangs(player);
                RefreshPreview();
            }
            else if (ctx.MatchParameter(0, "addpt", "addpoint", "newpt"))
            {
                float x;
                float z;
                int index = _currentPoints is null ? 0 : _currentPoints.Count;
                if ((ctx.HasArgsExact(2) || ctx.HasArgsExact(4)) && !(ctx.TryGetRef(1, ref index) && index > -1)) // <insert index> [<x> <z>]
                    throw ctx.Reply(T.ZoneEditAddPointInvalid);

                if (ctx.HasArgsExact(3)) // <x> <z>
                {
                    if (!ctx.TryGet(2, out z) || !ctx.TryGet(1, out x))
                        throw ctx.Reply(T.ZoneEditAddPointInvalid);
                }
                else if (ctx.HasArgsExact(4)) // <insert index> <x> <z>
                {
                    if (!ctx.TryGet(3, out z) || !ctx.TryGet(2, out x))
                        throw ctx.Reply(T.ZoneEditAddPointInvalid);
                }
                else
                {
                    x = pos.x;
                    z = pos.z;
                }

                if (index < 0 || (_currentPoints is not null && _currentPoints.Count < index))
                    throw ctx.Reply(T.ZoneEditAddPointInvalid);

                Vector2 v = new Vector2(x, z);
                if (_currentPoints != null)
                    _currentPoints.Insert(index, v);
                else
                {
                    index = 0;
                    _currentPoints = new List<Vector2>(8) { v };
                }
                if (!CheckType(EZoneType.POLYGON))
                {
                    ITransportConnection tc = player.Player.channel.owner.transportConnection;
                    EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + (_currentPoints.Count - 1), PointText(_currentPoints.Count - 1));
                    EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + (_currentPoints.Count - 1), true);
                    RefreshPreview();
                }
                ctx.Reply(T.ZoneEditAddPointSuccess, _currentPoints.Count, v);
                AddTransaction(new AddDelPointTransaction(index, v, false));
            }
            else if (ctx.MatchParameter(0, "delpoint", "delpt", "deletepoint", "removepoint", "deletept", "removept"))
            {
                float x;
                float z;
                ITransportConnection tc;
                if (ctx.HasArgsExact(3))
                {
                    if (!ctx.TryGet(3, out z) || !ctx.TryGet(2, out x))
                        throw ctx.Reply(T.ZoneEditDeletePointInvalid);
                }
                else if (ctx.HasArgsExact(2))
                {
                    if (ctx.TryGet(1, out int index))
                    {
                        if (_currentPoints == null || _currentPoints.Count < index || index < 0)
                            throw ctx.Reply(T.ZoneEditPointNotDefined, index);

                        --index;
                        Vector2 pt = _currentPoints[index];
                        _currentPoints.RemoveAt(index);
                        if (!CheckType(EZoneType.POLYGON))
                        {
                            tc = player.Player.channel.owner.transportConnection;
                            for (int i = index; i < _currentPoints.Count; ++i)
                            {
                                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + i, PointText(i));
                            }
                            EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + _currentPoints.Count, false);
                            RefreshPreview();
                        }
                        ctx.Reply(T.ZoneEditDeletePointSuccess, index + 1, pt);
                        AddTransaction(new AddDelPointTransaction(index, pt, true));
                        return;
                    }
                    else
                        throw ctx.Reply(T.ZoneEditDeletePointInvalid);
                }
                else
                {
                    x = pos.x;
                    z = pos.z;
                }
                Vector2 v = new Vector2(x, z);
                if (_currentPoints == null || _currentPoints.Count == 0)
                    throw ctx.Reply(T.ZoneEditPointNotNearby, v);

                float min = 0f;
                int ind = -1;
                for (int i = 0; i < _currentPoints.Count; ++i)
                {
                    float sqrdist = (v - _currentPoints[i]).sqrMagnitude;
                    if (ind == -1 || sqrdist < min)
                    {
                        min = sqrdist;
                        ind = i;
                    }
                }
                if (ind == -1 || min > NEARBY_POINT_DISTANCE_SQR) // must be within 5 meters
                    throw ctx.Reply(T.ZoneEditPointNotNearby, v);

                Vector2 pt2 = _currentPoints[ind];
                _currentPoints.RemoveAt(ind);
                if (!CheckType(EZoneType.POLYGON))
                {
                    tc = player.Player.channel.owner.transportConnection;
                    for (int i = ind; i < _currentPoints.Count; ++i)
                    {
                        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + i, PointText(i));
                    }
                    EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + _currentPoints.Count, false);
                    RefreshPreview();
                }
                ctx.Reply(T.ZoneEditDeletePointSuccess, ind + 1, pt2);
                AddTransaction(new AddDelPointTransaction(ind, pt2, true));
            }
            else if (ctx.MatchParameter(0, "clearpoints", "clearpts", "clrpts", "clrpoints"))
            {
                if (_currentPoints != null)
                {
                    AddTransaction(new ClearPointsTransaction(_currentPoints));
                    _currentPoints.Clear();
                }
                if (!CheckType(EZoneType.POLYGON))
                {
                    ITransportConnection tc = player.Player.channel.owner.transportConnection;
                    for (int i = 0; i < POINT_ROWS; ++i)
                    {
                        EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + i, false);
                    }
                    RefreshPreview();
                }
                ctx.Reply(T.ZoneEditClearSuccess);
                RefreshPreview();
            }
            else if (ctx.MatchParameter(0, "setpoint", "movepoint", "setpt", "movept"))
            {
                ITransportConnection tc;
                if (ctx.HasArgsExact(5)) // <nearby src x> <nearby src z> <dest x> <dest z>
                {
                    if (!ctx.TryGet(1, out float srcX) || !ctx.TryGet(2, out float srcZ) || !ctx.TryGet(3, out float dstX) || !ctx.TryGet(4, out float dstZ))
                        throw ctx.Reply(T.ZoneEditSetPointInvalid);

                        Vector2 v = new Vector2(srcX, srcZ);
                    if (_currentPoints == null || _currentPoints.Count == 0)
                        throw ctx.Reply(T.ZoneEditPointNotNearby, v);

                    float min = 0f;
                    int ind = -1;
                    for (int i = 0; i < _currentPoints.Count; ++i)
                    {
                        float sqrdist = (v - _currentPoints[i]).sqrMagnitude;
                        if (ind == -1 || sqrdist < min)
                        {
                            min = sqrdist;
                            ind = i;
                        }
                    }
                    if (ind == -1 || min > NEARBY_POINT_DISTANCE_SQR) // must be within 5 meters
                        throw ctx.Reply(T.ZoneEditPointNotNearby, v);

                    Vector2 old = _currentPoints[ind];
                    Vector2 @new = new Vector2(dstX, dstZ);
                    _currentPoints[ind] = @new;
                    tc = player.Player.channel.owner.transportConnection;
                    if (!CheckType(EZoneType.POLYGON))
                    {
                        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + ind, PointText(ind));
                        EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + ind, true);
                        RefreshPreview();
                    }
                    AddTransaction(new SetPointTransaction(ind, old, @new));
                    ctx.Reply(T.ZoneEditSetPointSuccess, ind + 1, old, @new);
                }
                else if (ctx.HasArgsExact(4)) // <pt num> <dest x> <dest z>
                {
                    if (!ctx.TryGet(1, out int index) || !ctx.TryGet(2, out float dstX) || !ctx.TryGet(3, out float dstZ))
                        throw ctx.Reply(T.ZoneEditSetPointInvalid);

                    if (_currentPoints == null || _currentPoints.Count < index || index < 1)
                        throw ctx.Reply(T.ZoneEditPointNotDefined, index);

                    --index;
                    Vector2 old = _currentPoints[index];
                    Vector2 @new = new Vector2(dstX, dstZ);
                    _currentPoints[index] = @new;
                    tc = player.Player.channel.owner.transportConnection;
                    if (!CheckType(EZoneType.POLYGON))
                    {
                        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + index, PointText(index));
                        EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + index, true);
                        RefreshPreview();
                    }
                    AddTransaction(new SetPointTransaction(index, old, @new));
                    ctx.Reply(T.ZoneEditSetPointSuccess, index + 1, old, @new);
                }
                else if (ctx.HasArgsExact(3)) // <src x> <src z>
                {
                    if (!ctx.TryGet(2, out float dstX) || !ctx.TryGet(3, out float dstZ))
                        throw ctx.Reply(T.ZoneEditSetPointInvalid);

                    Vector2 v = new Vector2(dstX, dstZ);
                    if (_currentPoints == null || _currentPoints.Count == 0)
                        throw ctx.Reply(T.ZoneEditPointNotNearby, v);

                    float min = 0f;
                    int ind = -1;
                    for (int i = 0; i < _currentPoints.Count; ++i)
                    {
                        float sqrdist = (v - _currentPoints[i]).sqrMagnitude;
                        if (ind == -1 || sqrdist < min)
                        {
                            min = sqrdist;
                            ind = i;
                        }
                    }
                    if (ind == -1 || min > NEARBY_POINT_DISTANCE_SQR) // must be within 5 meters
                        throw ctx.Reply(T.ZoneEditPointNotNearby, v);

                    Vector2 old = _currentPoints[ind];
                    Vector2 @new = new Vector2(pos.x, pos.z);
                    _currentPoints[ind] = @new;
                    tc = player.Player.channel.owner.transportConnection;
                    if (!CheckType(EZoneType.POLYGON))
                    {
                        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + ind, PointText(ind));
                        EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + ind, true);
                        RefreshPreview();
                    }
                    AddTransaction(new SetPointTransaction(ind, old, @new));
                    ctx.Reply(T.ZoneEditSetPointSuccess, ind + 1, old, @new);
                }
                else if (ctx.HasArgsExact(2)) // <pt num>
                {
                    if (!ctx.TryGet(1, out int index))
                        throw ctx.Reply(T.ZoneEditSetPointInvalid);

                    if (_currentPoints == null || _currentPoints.Count < index || index < 1)
                        throw ctx.Reply(T.ZoneEditPointNotDefined, index);

                    --index;
                    Vector2 old = _currentPoints[index];
                    Vector2 @new = new Vector2(pos.x, pos.z);
                    _currentPoints[index] = @new;
                    tc = player.Player.channel.owner.transportConnection;
                    if (!CheckType(EZoneType.POLYGON))
                    {
                        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + index, PointText(index));
                        EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + index, true);
                        RefreshPreview();
                    }
                    AddTransaction(new SetPointTransaction(index, old, @new));
                    ctx.Reply(T.ZoneEditSetPointSuccess, index + 1, old, @new);
                }
                else if (ctx.HasArgsExact(1))
                {
                    Vector2 v = new Vector2(pos.x, pos.z);
                    if (_currentPoints == null || _currentPoints.Count == 0)
                        throw ctx.Reply(T.ZoneEditPointNotNearby, v);

                    float min = 0f;
                    int ind = -1;
                    for (int i = 0; i < _currentPoints.Count; ++i)
                    {
                        float sqrdist = (v - _currentPoints[i]).sqrMagnitude;
                        if (ind == -1 || sqrdist < min)
                        {
                            min = sqrdist;
                            ind = i;
                        }
                    }
                    if (ind == -1 || min > NEARBY_POINT_DISTANCE_SQR) // must be within 5 meters
                    {
                        ctx.Reply(T.ZoneEditPointNotNearby, v);
                        return;
                    }
                    Vector2 old = _currentPoints[ind];
                    _currentPoints[ind] = v;
                    tc = player.Player.channel.owner.transportConnection;
                    if (!CheckType(EZoneType.POLYGON))
                    {
                        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + ind, PointText(ind));
                        EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + ind, true);
                        RefreshPreview();
                    }
                    AddTransaction(new SetPointTransaction(ind, old, v));
                    ctx.Reply(T.ZoneEditSetPointSuccess, ind + 1, old, v);
                }
                else
                    throw ctx.Reply(T.ZoneEditSetPointInvalid);
            }
            else if (ctx.MatchParameter(0, "orderpoint", "orderpt", "setindex", "ptindex"))
            {
                if (_currentPoints == null || _currentPoints.Count == 0)
                    throw ctx.Reply(T.ZoneEditPointNotNearby, new Vector2(pos.x, pos.z));

                int from;
                int to;
                if (ctx.HasArgsExact(2)) // <to-index>
                {
                    if (!ctx.TryGet(1, out to))
                    {
                        if (ctx.MatchParameter(1, "end", "bottom"))
                            to = _currentPoints.Count;
                        else if (ctx.MatchParameter(1, "start", "first", "top"))
                            to = 1;
                        else
                            throw ctx.Reply(T.ZoneEditOrderPointInvalid);
                    }
                    if (_currentPoints.Count < to || to < 1)
                        throw ctx.Reply(T.ZoneEditPointNotDefined, to);

                    float min = 0f;
                    from = -1;
                    Vector2 v = new Vector2(pos.x, pos.z);
                    for (int i = 0; i < _currentPoints.Count; ++i)
                    {
                        float sqrdist = (v - _currentPoints[i]).sqrMagnitude;
                        if ((from == -1 || sqrdist < min) && i != to)
                        {
                            min = sqrdist;
                            from = i;
                        }
                    }
                    if (from == -1 || min > NEARBY_POINT_DISTANCE_SQR) // must be within 5 meters
                    {
                        ctx.Reply(T.ZoneEditPointNotNearby, v);
                        return;
                    }
                    --to;
                }
                else if (ctx.HasArgsExact(3)) // <from-index> <to-index>
                {
                    if (!ctx.TryGet(1, out from))
                    {
                        if (ctx.MatchParameter(1, "end", "bottom"))
                            from = _currentPoints.Count;
                        else if (ctx.MatchParameter(1, "start", "first", "top"))
                            from = 1;
                        else
                            throw ctx.Reply(T.ZoneEditOrderPointInvalid);
                    }
                    if (!ctx.TryGet(2, out to))
                    {
                        if (ctx.MatchParameter(2, "end", "bottom"))
                            to = _currentPoints.Count;
                        else if (ctx.MatchParameter(2, "start", "first", "top"))
                            to = 1;
                        else
                            throw ctx.Reply(T.ZoneEditOrderPointInvalid);
                    }
                    if (to == from)
                        throw ctx.Reply(T.ZoneEditOrderPointInvalid);
                    if (_currentPoints.Count < to || to < 1)
                        throw ctx.Reply(T.ZoneEditPointNotDefined, to);

                    if (_currentPoints.Count < from || from < 1)
                        throw ctx.Reply(T.ZoneEditPointNotDefined, from);

                    --to;
                    --from;
                }
                else if (ctx.HasArgsExact(4))
                {
                    if (!ctx.TryGet(1, out float srcX) || !ctx.TryGet(2, out float srcZ))
                        throw ctx.Reply(T.ZoneEditOrderPointInvalid);

                    if (!ctx.TryGet(2, out to))
                    {
                        if (ctx.MatchParameter(2, "end", "bottom"))
                            to = _currentPoints.Count;
                        else if (ctx.MatchParameter(2, "start", "first", "top"))
                            to = 1;
                        else
                            throw ctx.Reply(T.ZoneEditOrderPointInvalid);
                    }
                    if (_currentPoints.Count < to || to < 1)
                        throw ctx.Reply(T.ZoneEditPointNotDefined, to);

                    --to;
                    float min = 0f;
                    from = -1;
                    Vector2 v = new Vector2(srcX, srcZ);
                    for (int i = 0; i < _currentPoints.Count; ++i)
                    {
                        float sqrdist = (v - _currentPoints[i]).sqrMagnitude;
                        if (from == -1 || sqrdist < min)
                        {
                            min = sqrdist;
                            from = i;
                        }
                    }
                    if (from == -1 || min > NEARBY_POINT_DISTANCE_SQR) // must be within 5 meters
                        throw ctx.Reply(T.ZoneEditPointNotNearby, v);

                    if (to == from)
                        throw ctx.Reply(T.ZoneEditOrderPointInvalid);
                }
                else
                    throw ctx.Reply(T.ZoneEditOrderPointInvalid);
                Vector2 old = _currentPoints[from];
                _currentPoints.RemoveAt(from);
                _currentPoints.Insert(to, old);
                ITransportConnection tc = player.Player.channel.owner.transportConnection;
                if (!CheckType(EZoneType.POLYGON))
                {
                    int ind2 = Math.Min(from, to);
                    for (int i = ind2; i < _currentPoints.Count; ++i)
                    {
                        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + i, PointText(i));
                        EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + i, true);
                    }
                    RefreshPreview();
                }

                AddTransaction(new SwapPointsTransaction(from, to));
                ctx.Reply(T.ZoneEditOrderPointSuccess, from, to);
            }
            else if (ctx.MatchParameter(0, "radius"))
            {
                float radius;
                if (!ctx.HasArgs(2))
                    radius = (new Vector2(pos.x, pos.z) - new Vector2(_currentBuilder.X, _currentBuilder.Z)).magnitude;
                else if (!ctx.TryGet(1, out radius))
                    throw ctx.Reply(T.ZoneEditRadiusInvalid);

                AddTransaction(new SetFloatTransaction(_currentBuilder.ZoneData.Radius, radius, SetFloatTransaction.EFloatType.RADIUS));
                SetRadius(radius);
            }
            else if (ctx.MatchParameter(0, "sizex", "width", "length"))
            {
                float sizex;
                if (!ctx.HasArgs(2))
                    sizex = Mathf.Abs(pos.x - _currentBuilder.X) * 2;
                else if (!ctx.TryGet(1, out sizex))
                    throw ctx.Reply(T.ZoneEditSizeXInvalid);

                AddTransaction(new SetFloatTransaction(_currentBuilder.ZoneData.SizeX, sizex, SetFloatTransaction.EFloatType.SIZE_X));
                SetSizeX(sizex);
            }
            else if (ctx.MatchParameter(0, "sizez", "height", "depth"))
            {
                float sizez;
                if (!ctx.HasArgs(2))
                    sizez = Mathf.Abs(pos.z - _currentBuilder.Z) * 2;
                else if (!ctx.TryGet(1, out sizez))
                    throw ctx.Reply(T.ZoneEditSizeZInvalid);

                AddTransaction(new SetFloatTransaction(_currentBuilder.ZoneData.SizeZ, sizez, SetFloatTransaction.EFloatType.SIZE_Z));
                SetSizeZ(sizez);
            }
            else if (ctx.MatchParameter(0, "center", "position", "origin"))
            {
                float x;
                float z;
                if (!ctx.HasArgs(3))
                {
                    x = pos.x;
                    z = pos.z;
                }
                else if (!ctx.TryGet(1, out x) || !ctx.TryGet(2, out z))
                    throw ctx.Reply(T.ZoneEditCenterInvalid);

                AddTransaction(new SetCenterTransaction(_currentBuilder.X, _currentBuilder.Z, x, z));
                SetCenter(x, z);
            }
            else if (ctx.MatchParameter(0, "name", "title", "longname"))
            {
                if (!ctx.HasArgs(2))
                    throw ctx.Reply(T.ZoneEditNameInvalid);

                string name = ctx.GetRange(1)!;
                if (!string.IsNullOrEmpty(_currentBuilder.Name))
                    AddTransaction(new SetStringTransaction(_currentBuilder.Name!, name, SetStringTransaction.EFloatType.NAME));
                SetName(name);
            }
            else if (ctx.MatchParameter(0, "use", "use-case", "usecase"))
            {
                if (!ctx.HasArgs(2))
                    throw ctx.Reply(T.ZoneEditUseCaseInvalid);

                bool w2ic = ctx.MatchParameter(1, "case");
                if (!ctx.HasArgsExact(3) && w2ic)
                    throw ctx.Reply(T.ZoneEditUseCaseInvalid);

                if (Enum.TryParse(ctx.GetRange(w2ic ? 2 : 1)!.Replace(' ', '_'), true, out EZoneUseCase uc))
                {
                    AddTransaction(new SetUseTransaction(_currentBuilder!.UseCase, uc));
                    SetUseCase(uc);
                }
                else
                    throw ctx.Reply(T.ZoneEditUseCaseInvalid);
            }
            else if (ctx.MatchParameter(0, "shortname", "short", "abbreviation", "abbr"))
            {
                if (!ctx.HasArgs(2))
                    throw ctx.Reply(T.ZoneEditShortNameInvalid);

                int st = ctx.MatchParameter(1, "name") ? 2 : 1;
                string? name = ctx.GetRange(st);
                if (name is null)
                    throw ctx.Reply(T.ZoneEditShortNameInvalid);

                AddTransaction(new SetStringTransaction(_currentBuilder.ShortName!, name, SetStringTransaction.EFloatType.SHORT_NAME));
                SetShortName(name);
            }
            else if (ctx.MatchParameter(0, "undo"))
            {
                if (UndoBuffer.Count > 0)
                    Undo();
                else
                    ctx.Reply(T.ZoneEditUndoEmpty);
            }
            else if (ctx.MatchParameter(0, "redo"))
            {
                if (RedoBuffer.Count > 0)
                    Redo();
                else
                    ctx.Reply(T.ZoneEditRedoEmpty);
            }
            else
            {
                ctx.SendCorrectUsage(ZONE_EDIT_USAGE);
            }
        }
    }

    private void SetUseCase(EZoneUseCase uc)
    {
        _currentBuilder!.UseCase = uc;
        player.SendChat(T.ZoneEditUseCaseSuccess, uc);
    }

    private void SetName(string name)
    {
        _currentBuilder!.Name = name;
        EffectManager.sendUIEffectText(EDIT_KEY, player.Player.channel.owner.transportConnection, true, "Name", name);
        player.SendChat(T.ZoneEditNameSuccess, name);
    }
    private void SetShortName(string shortName)
    {
        _currentBuilder!.ShortName = shortName;
        if (!string.IsNullOrEmpty(shortName))
            player.SendChat(T.ZoneEditShortNameSuccess, shortName);
        else
            player.SendChat(T.ZoneEditShortNameRemoved);
    }

    private void SetRadius(float radius)
    {
        _currentBuilder!.ZoneData.Radius = radius;
        if (!CheckType(EZoneType.CIRCLE))
        {
            EffectManager.sendUIEffectText(EDIT_KEY, player.Player.channel.owner.transportConnection, true, "Circle_Radius", _currentBuilder.ZoneData.Radius.ToString("F2", Data.Locale) + "m");
            RefreshPreview();
        }
        player.SendChat(T.ZoneEditRadiusSuccess, radius);
    }

    private void SetMaxHeight(float mh)
    {
        _currentBuilder!.MaxHeight = mh;
        UpdateHeights();
        player.SendChat(T.ZoneEditMaxHeightSuccess, mh);
    }

    private void SetMinHeight(float mh)
    {
        _currentBuilder!.MinHeight = mh;
        UpdateHeights();
        player.SendChat(T.ZoneEditMinHeightSuccess, mh);
        RefreshPreview();
    }

    internal void ReloadLang()
    {
        if (_currentBuilder == null) return;
        CheckType(_currentBuilder.ZoneType, true);
    }

    private void SetSizeX(float sizex)
    {
        _currentBuilder!.ZoneData.SizeX = sizex;
        if (!CheckType(EZoneType.RECTANGLE))
        {
            EffectManager.sendUIEffectText(EDIT_KEY, player.Player.channel.owner.transportConnection, true, "Rect_SizeX", _currentBuilder.ZoneData.SizeX.ToString("F2", Data.Locale) + "m");
            RefreshPreview();
        }
        player.SendChat(T.ZoneEditSizeXSuccess, sizex);
    }
    private void SetSizeZ(float sizez)
    {
        _currentBuilder!.ZoneData.SizeZ = sizez;
        if (!CheckType(EZoneType.RECTANGLE))
        {
            EffectManager.sendUIEffectText(EDIT_KEY, player.Player.channel.owner.transportConnection, true, "Rect_SizeZ", _currentBuilder.ZoneData.SizeZ.ToString("F2", Data.Locale) + "m");
            RefreshPreview();
        }
        player.SendChat(T.ZoneEditSizeZSuccess, sizez);
    }
    private void SetCenter(float x, float z)
    {
        _currentBuilder!.X = x;
        _currentBuilder!.Z = z;
        ITransportConnection tc = player.Player.channel.owner.transportConnection;
        switch (_currentBuilder.ZoneType)
        {
            case EZoneType.RECTANGLE:
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Rect_Position", FromV2(_currentBuilder.X, _currentBuilder.Z));
                break;
            case EZoneType.CIRCLE:
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Circle_Position", FromV2(_currentBuilder.X, _currentBuilder.Z));
                break;
            case EZoneType.POLYGON:
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Position", FromV2(_currentBuilder.X, _currentBuilder.Z));
                break;
        }

        float rot = player.Player.transform.rotation.eulerAngles.y;
        for (int i = 1; i < 5; ++i)
        {
            if (rot > 90 * i - 5f && rot < 90 * i + 5f)
                rot = 90 * i;
        }
        bool srot = false;
        switch (_currentBuilder.UseCase)
        {
            case EZoneUseCase.T1_MAIN:
                Teams.TeamManager.Config.Team1SpawnYaw.SetCurrentMapValue(rot);
                Teams.TeamManager.SaveConfig();
                srot = true;
                break;
            case EZoneUseCase.T2_MAIN:
                Teams.TeamManager.Config.Team2SpawnYaw.SetCurrentMapValue(rot);
                Teams.TeamManager.SaveConfig();
                srot = true;
                break;
            case EZoneUseCase.LOBBY:
                Teams.TeamManager.Config.LobbySpawnpointYaw.SetCurrentMapValue(rot);
                Teams.TeamManager.SaveConfig();
                srot = true;
                break;
        }

        if (srot)
            player.SendChat(T.ZoneEditCenterSuccessRotation, new Vector2(x, z), rot);
        else
            player.SendChat(T.ZoneEditCenterSuccess, new Vector2(x, z));
        RefreshPreview();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FromV2(Vector2 v2) => $"({v2.x.ToString("F2", Data.Locale)}, {v2.y.ToString("F2", Data.Locale)})";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FromV2(float x, float y) => $"({x.ToString("F2", Data.Locale)}, {y.ToString("F2", Data.Locale)})";
    private bool CheckType(EZoneType type, bool overwrite = false, bool @implicit = true, bool transact = true)
    {
        if (_currentBuilder == null || (!overwrite && _currentBuilder.ZoneType == type)) return false;
        ThreadUtil.assertIsGameThread();
        ITransportConnection tc = player.Player.channel.owner.transportConnection;
        switch (_currentBuilder.ZoneType)
        {
            case EZoneType.CIRCLE:
                EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "CircleImage", false);
                break;
            case EZoneType.RECTANGLE:
                EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "RectImage", false);
                break;
            case EZoneType.POLYGON:
                EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "PolygonParent", false);
                break;
        }
        if (!overwrite && transact)
            AddTransaction(new SetTypeTransaction(_currentBuilder.ZoneType, type, @implicit));
        _currentBuilder.ZoneType = type;
        UpdateHeights();
        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Type", Localization.TranslateEnum(type, player.Steam64));
        switch (type)
        {
            case EZoneType.CIRCLE:
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Circle_Radius", _currentBuilder.ZoneData.Radius.ToString("F2", Data.Locale) + "m");
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Circle_Position", FromV2(_currentBuilder.X, _currentBuilder.Z));
                EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "CircleImage", true);
                break;
            case EZoneType.RECTANGLE:
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Rect_SizeX", _currentBuilder.ZoneData.SizeX.ToString("F2", Data.Locale) + "m");
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Rect_SizeZ", _currentBuilder.ZoneData.SizeZ.ToString("F2", Data.Locale) + "m");
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Rect_Position", FromV2(_currentBuilder.X, _currentBuilder.Z));
                EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "RectImage", true);
                break;
            case EZoneType.POLYGON:
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Position", FromV2(_currentBuilder.X, _currentBuilder.Z));
                if (_currentPoints == null)
                    _currentPoints = new List<Vector2>(8);
                for (int i = 0; i < POINT_ROWS; ++i)
                {
                    bool a = _currentPoints.Count > i;
                    EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + i, a);
                    if (a)
                        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + i, PointText(i));
                }
                EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "PolygonParent", true);
                break;
            default: return false;
        }
        UpdateSuggestedCommands();
        RefreshPreview();
        return true;
    }
    private string PointText(int i) => _closestPoint == i ? FromV2(_currentPoints![i]).Colorize("dbffdc") : FromV2(_currentPoints![i]);
    private void UpdateSuggestedCommands()
    {
        if (_currentBuilder == null) return;
        ThreadUtil.assertIsGameThread();
        Translation[] cmds = new Translation[11];
        int pos = -1;
        cmds[++pos] = T.ZoneEditSuggestedCommand3;
        cmds[++pos] = T.ZoneEditSuggestedCommand1;
        cmds[++pos] = T.ZoneEditSuggestedCommand2;
        if (_currentBuilder.ZoneType == EZoneType.CIRCLE)
            cmds[++pos] = T.ZoneEditSuggestedCommand9;
        else if (_currentBuilder.ZoneType == EZoneType.RECTANGLE)
        {
            cmds[++pos] = T.ZoneEditSuggestedCommand10;
            cmds[++pos] = T.ZoneEditSuggestedCommand11;
        }
        else if (_currentBuilder.ZoneType == EZoneType.POLYGON)
        {
            cmds[++pos] = T.ZoneEditSuggestedCommand5;
            cmds[++pos] = T.ZoneEditSuggestedCommand6;
            cmds[++pos] = T.ZoneEditSuggestedCommand7;
            cmds[++pos] = T.ZoneEditSuggestedCommand8;
            cmds[++pos] = T.ZoneEditSuggestedCommand14;
        }
        cmds[++pos] = T.ZoneEditSuggestedCommand4;
        cmds[++pos] = T.ZoneEditSuggestedCommand13;
        cmds[++pos] = T.ZoneEditSuggestedCommand12;
        ++pos;
        ITransportConnection tc = player.Player.channel.owner.transportConnection;
        for (int i = 0; i < HELP_ROWS; ++i)
        {
            bool a = i < pos;
            string b = "CommandHelp (" + i + ")";
            EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, b, a);
            if (a)
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, b, Localization.Translate(cmds[i], player.Steam64));
        }
    }
    private void UpdateHeights()
    {
        if (_currentBuilder != null)
        {
            ThreadUtil.assertIsGameThread();
            EffectManager.sendUIEffectText(EDIT_KEY, player.Player.channel.owner.transportConnection, true, 
                _currentBuilder.ZoneType switch
                {
                    EZoneType.CIRCLE => "Circle_YLimit",
                    EZoneType.RECTANGLE => "Rect_YLimit",
                    EZoneType.POLYGON => "Polygon_YLimit",
                    _ => string.Empty
                },
                T.ZoneEditUIYLimits.Translate(player,
            float.IsNaN(_currentBuilder.MinHeight) ? T.ZoneEditUIYLimitsInfinity.Translate(player) : _currentBuilder.MinHeight.ToString("0.##", Data.Locale),
            float.IsNaN(_currentBuilder.MaxHeight) ? T.ZoneEditUIYLimitsInfinity.Translate(player) : _currentBuilder.MaxHeight.ToString("0.##", Data.Locale)));
        }
    }
    private void AddTransaction(Transaction t)
    {
        UndoBuffer.Add(t);
        RedoBuffer.Clear();
    }
    private void Undo()
    {
        bool imp = true;
        while (UndoBuffer.Count > 0 && imp)
        {
            int end = UndoBuffer.Count - 1;
            Transaction t = UndoBuffer[end];
            UndoBuffer.RemoveAt(end);
            t.Undo(this);
            imp = t.Implicit;
            RedoBuffer.Add(t);
        }
    }
    private void Redo()
    {
        if (RedoBuffer.Count == 0) return;
        do
        {
            int end = RedoBuffer.Count - 1;
            Transaction t = RedoBuffer[end];
            RedoBuffer.RemoveAt(end);
            t.Redo(this);
            UndoBuffer.Add(t);
        }
        while (RedoBuffer.Count > 0 && RedoBuffer[RedoBuffer.Count - 1].Implicit);
    }
    private abstract class Transaction
    {
        public readonly bool Implicit;
        public Transaction(bool @implicit)
        {
            this.Implicit = @implicit;
        }
        public abstract void Undo(ZonePlayerComponent component);
        public abstract void Redo(ZonePlayerComponent component);
    }
    private class SetCenterTransaction : Transaction
    {
        private readonly float OldX;
        private readonly float OldZ;
        private readonly float NewX;
        private readonly float NewZ;
        public SetCenterTransaction(float oldX, float oldZ, float newX, float newZ) : base(false)
        {
            OldX = oldX;
            OldZ = oldZ;
            NewX = newX;
            NewZ = newZ;
        }
        public override void Redo(ZonePlayerComponent component)
        {
            component.SetCenter(NewX, NewZ);
        }
        public override void Undo(ZonePlayerComponent component)
        {
            component.SetCenter(OldX, OldZ);
        }
    }
    private class SetUseTransaction : Transaction
    {
        private readonly EZoneUseCase Old;
        private readonly EZoneUseCase New;
        public SetUseTransaction(EZoneUseCase old, EZoneUseCase @new) : base(false)
        {
            Old = old;
            New = @new;
        }
        public override void Redo(ZonePlayerComponent component)
        {
            component.SetUseCase(New);
        }
        public override void Undo(ZonePlayerComponent component)
        {
            component.SetUseCase(Old);
        }
    }
    private class SetTypeTransaction : Transaction
    {
        private readonly EZoneType Old;
        private readonly EZoneType New;
        public SetTypeTransaction(EZoneType old, EZoneType @new, bool @implicit) : base(@implicit)
        {
            Old = old;
            New = @new;
        }
        public override void Redo(ZonePlayerComponent component)
        {
            component.CheckType(New, transact: false);
            if (!Implicit)
                component.player.SendChat(T.ZoneEditTypeSuccess, New);
        }
        public override void Undo(ZonePlayerComponent component)
        {
            component.CheckType(Old, transact: false);
            if (!Implicit)
                component.player.SendChat(T.ZoneEditTypeSuccess, Old);
        }
    }

    private class SetFloatTransaction : Transaction
    {
        private readonly float Old;
        private readonly float New;
        private readonly EFloatType Type;
        public SetFloatTransaction(float old, float @new, EFloatType type) : base(false)
        {
            Old = old;
            New = @new;
            Type = type;
        }
        public override void Redo(ZonePlayerComponent component)
        {
            switch (Type)
            {
                case EFloatType.MIN_Y:
                    component._currentBuilder!.MinHeight = New;
                    break;
                case EFloatType.MAX_Y:
                    component._currentBuilder!.MaxHeight = New;
                    break;
                case EFloatType.RADIUS:
                    component._currentBuilder!.ZoneData.Radius = New;
                    break;
                case EFloatType.SIZE_X:
                    component.SetSizeX(New);
                    break;
                case EFloatType.SIZE_Z:
                    component.SetSizeZ(New);
                    break;
            }
        }
        public override void Undo(ZonePlayerComponent component)
        {
            switch (Type)
            {
                case EFloatType.MIN_Y:
                    component._currentBuilder!.MinHeight = Old;
                    break;
                case EFloatType.MAX_Y:
                    component._currentBuilder!.MaxHeight = Old;
                    break;
                case EFloatType.RADIUS:
                    component._currentBuilder!.ZoneData.Radius = Old;
                    break;
                case EFloatType.SIZE_X:
                    component.SetSizeX(Old);
                    break;
                case EFloatType.SIZE_Z:
                    component.SetSizeZ(Old);
                    break;
            }
        }

        public enum EFloatType : byte
        {
            MIN_Y,
            MAX_Y,
            RADIUS,
            SIZE_X,
            SIZE_Z
        }
    }
    private class SetStringTransaction : Transaction
    {
        private readonly string Old;
        private readonly string New;
        private readonly EFloatType Type;
        public SetStringTransaction(string old, string @new, EFloatType type) : base(false)
        {
            Old = old;
            New = @new;
            Type = type;
        }
        public override void Redo(ZonePlayerComponent component)
        {
            switch (Type)
            {
                case EFloatType.NAME:
                    component.SetName(New);
                    break;
                case EFloatType.SHORT_NAME:
                    component.SetShortName(New);
                    break;
            }
        }
        public override void Undo(ZonePlayerComponent component)
        {
            switch (Type)
            {
                case EFloatType.NAME:
                    component.SetName(Old);
                    break;
                case EFloatType.SHORT_NAME:
                    component.SetShortName(Old);
                    break;
            }
        }

        public enum EFloatType : byte
        {
            NAME,
            SHORT_NAME
        }
    }
    private class AddDelPointTransaction : Transaction
    {
        public int Index;
        public readonly Vector2 Position;
        public readonly bool IsDeleteOp;
        public AddDelPointTransaction(int newIndex, Vector2 position, bool delete) : base(false)
        {
            Index = newIndex;
            Position = position;
            IsDeleteOp = delete;
        }
        private void Add(ZonePlayerComponent component)
        {
            if (component._currentPoints != null)
                component._currentPoints.Insert(Index, Position);
            else
                component._currentPoints = new List<Vector2>(8) { Position };
            if (!component.CheckType(EZoneType.POLYGON, transact: false))
            {
                ITransportConnection tc = component.player.Player.channel.owner.transportConnection;
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + (component._currentPoints.Count - 1), component.PointText(component._currentPoints.Count - 1));
                EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + (component._currentPoints.Count - 1), true);
                component.RefreshPreview();
            }
            component.player.SendChat(T.ZoneEditAddPointSuccess, component._currentPoints.Count, Position);
        }
        private void Delete(ZonePlayerComponent component)
        {
            if (component._currentPoints is null || component._currentPoints.Count <= Index) return;
            Vector2 pt2 = component._currentPoints![Index];
            component._currentPoints.RemoveAt(Index);
            if (!component.CheckType(EZoneType.POLYGON, transact: false))
            {
                ITransportConnection tc = component.player.Player.channel.owner.transportConnection;
                for (int i = Index; i < component._currentPoints.Count; ++i)
                {
                    EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + i, component.PointText(i));
                }
                EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + component._currentPoints.Count, false);
                component.RefreshPreview();
            }
            component.player.SendChat(T.ZoneEditDeletePointSuccess, Index + 1, pt2);
        }
        public override void Redo(ZonePlayerComponent component)
        {
            if (IsDeleteOp)
                Delete(component);
            else
                Add(component);
        }
        public override void Undo(ZonePlayerComponent component)
        {
            if (IsDeleteOp)
                Add(component);
            else
                Delete(component);
        }
    }
    private class SetPointTransaction : Transaction
    {
        public readonly int Index;
        public readonly Vector2 Old;
        public readonly Vector2 New;
        public SetPointTransaction(int index, Vector2 old, Vector2 @new) : base(false)
        {
            Index = index;
            Old = old;
            New = @new;
        }
        private void Set(ZonePlayerComponent component, Vector2 @new, Vector2 old)
        {
            component._currentPoints![Index] = @new;
            if (!component.CheckType(EZoneType.POLYGON, transact: false))
            {
                ITransportConnection tc = component.player.Player.channel.owner.transportConnection;
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + Index, component.PointText(Index));
                EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + Index, true);
                component.RefreshPreview();
            }
            component.player.SendChat(T.ZoneEditSetPointSuccess, Index + 1, old, @new);
        }
        public override void Redo(ZonePlayerComponent component)
        {
            Set(component, New, Old);
        }
        public override void Undo(ZonePlayerComponent component)
        {
            Set(component, Old, New);
        }
    }
    private class ClearPointsTransaction : Transaction
    {
        public readonly List<Vector2>? Old;
        public ClearPointsTransaction(List<Vector2> points) : base(false)
        {
            Old = points?.ToList();
        }
        public override void Redo(ZonePlayerComponent component)
        {
            if (component._currentPoints != null)
                component._currentPoints.Clear();
            if (!component.CheckType(EZoneType.POLYGON, transact: false))
            {
                ITransportConnection tc = component.player.Player.channel.owner.transportConnection;
                for (int i = 0; i < POINT_ROWS; ++i)
                {
                    EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + i, false);
                }
                component.RefreshPreview();
            }
            component.player.SendChat(T.ZoneEditClearSuccess);
        }
        public override void Undo(ZonePlayerComponent component)
        {
            if (Old is not null)
            {
                component._currentPoints = Old.ToList();
                if (!component.CheckType(EZoneType.POLYGON, transact: false))
                {
                    ITransportConnection tc = component.player.Player.channel.owner.transportConnection;
                    for (int i = 0; i < component._currentPoints.Count; ++i)
                    {
                        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + i, component.PointText(i));
                        EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + i, false);
                    }
                    component.RefreshPreview();
                }
                component.player.SendChat(T.ZoneEditUnclearedSuccess, component._currentPoints.Count, component._currentPoints.Count.S());
            }
            else
                component.player.SendChat(T.ZoneEditUnclearedSuccess, 0, string.Empty);
        }
    }
    private class SwapPointsTransaction : Transaction
    {
        public readonly int From;
        public readonly int To;
        public SwapPointsTransaction(int from, int to) : base(false)
        {
            From = from;
            To = to;
        }
        private void Swap(ZonePlayerComponent component, int from, int to)
        {
            Vector2 old = component._currentPoints![from];
            component._currentPoints.RemoveAt(from);
            component._currentPoints.Insert(to, old);
            ITransportConnection tc = component.player.Player.channel.owner.transportConnection;
            if (!component.CheckType(EZoneType.POLYGON))
            {
                int ind2 = Math.Min(from, to);
                for (int i = ind2; i < component._currentPoints.Count; ++i)
                {
                    EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + i, component.PointText(i));
                    EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + i, true);
                }
                component.RefreshPreview();
            }
            component.player.SendChat(T.ZoneEditOrderPointSuccess, from, to);
        }
        public override void Redo(ZonePlayerComponent component)
        {
            Swap(component, From, To);
        }
        public override void Undo(ZonePlayerComponent component)
        {
            Swap(component, To, From);
        }
    }
}
