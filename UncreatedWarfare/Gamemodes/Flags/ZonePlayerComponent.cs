using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
    private ZoneBuilder? _currentBuilder;
    private bool _currentBuilderIsExisting = false;
    private List<Vector2>? _currentPoints;
    private float _lastZonePreviewRefresh = 0f;
    private static readonly List<ZonePlayerComponent> _builders = new List<ZonePlayerComponent>(2);
    internal static EffectAsset? _airdrop = null ;
    internal static EffectAsset  _center  = null!;
    internal static EffectAsset  _corner  = null!;
    internal static EffectAsset  _side    = null!;
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
#pragma warning disable IDE0051
    private void Update()
    {
        if (_currentBuilder is not null && Time.time - _lastZonePreviewRefresh > 55f)
        {
            RefreshPreview();
        }
    }
#pragma warning restore IDE0051
    internal void UtilCommand(string[] args)
    {
        ThreadUtil.assertIsGameThread();
        if (args.Length < 2)
        {
            player.SendChat("util_zone_syntax");
            return;
        }
        string operation = args[1];
        if (operation.Equals("location", StringComparison.OrdinalIgnoreCase) || operation.Equals("position", StringComparison.OrdinalIgnoreCase) || operation.Equals("pos", StringComparison.OrdinalIgnoreCase))
        {
            player.SendChat("util_zone_location",
                player.Player.transform.position.x.ToString("F2", Data.Locale),
                player.Player.transform.position.y.ToString("F2", Data.Locale),
                player.Player.transform.position.z.ToString("F2", Data.Locale),
                player.Player.transform.rotation.eulerAngles.y.ToString("F1", Data.Locale));
            return;
        }
    }
    internal void CreateCommand(string[] args)
    {
        ThreadUtil.assertIsGameThread();
        if (args.Length < 3)
        {
            player.SendChat("create_zone_syntax");
            return;
        }
        string typestr = args[1];
        EZoneType type = EZoneType.INVALID;
        if (typestr.Equals("rect", StringComparison.OrdinalIgnoreCase) || typestr.Equals("rectangle", StringComparison.OrdinalIgnoreCase) || typestr.Equals("square", StringComparison.OrdinalIgnoreCase))
        {
            type = EZoneType.RECTANGLE;
        }
        else if (typestr.Equals("circle", StringComparison.OrdinalIgnoreCase) || typestr.Equals("oval", StringComparison.OrdinalIgnoreCase) || typestr.Equals("ellipse", StringComparison.OrdinalIgnoreCase))
        {
            type = EZoneType.CIRCLE;
        }
        else if (typestr.Equals("polygon", StringComparison.OrdinalIgnoreCase) || typestr.Equals("poly", StringComparison.OrdinalIgnoreCase) || typestr.Equals("shape", StringComparison.OrdinalIgnoreCase))
        {
            type = EZoneType.POLYGON;
        }
        if (type == EZoneType.INVALID)
        {
            player.SendChat("create_zone_syntax");
            return;
        }
        string name = args.Length == 3 ? args[2] : string.Join(" ", args, 2, args.Length - 2);
        for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
        {
            if (Data.ZoneProvider.Zones[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                player.SendChat("create_zone_name_taken", Data.ZoneProvider.Zones[i].Name);
                return;
            }
        }
        for (int i = _builders.Count - 1; i >= 0; --i)
        {
            ZoneBuilder? zb = _builders[i]._currentBuilder;
            if (zb == null)
            {
                _builders.RemoveAt(i);
                continue;
            }
            if (zb.Name != null && zb.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                player.SendChat("create_zone_name_taken_2", zb.Name, _builders[i].player.Player.channel.owner.playerID.characterName);
                return;
            }
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
            Id = nextId
        };
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
        string text = Translation.TranslateEnum(type, player.Steam64);
        if (_edit != null)
        {
            Data.SendEffectClearAll.InvokeAndLoopback(ENetReliability.Reliable, new ITransportConnection[] { player.Player.channel.owner.transportConnection });
            EffectManager.sendUIEffect(_edit.id, EDIT_KEY, tc, true);
            player.HasUIHidden = true;
            EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Name", name);
            EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Type", text);
            EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Header", Translation.Translate("edit_zone_ui_suggested_commands", player.Steam64));
        }
        player.SendChat("create_zone_success", name, text);
        CheckType(type, true);
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
    internal void EditCommand(string[] args)
    {
        ThreadUtil.assertIsGameThread();
        if (args.Length < 2)
        {
            player.SendChat("edit_zone_syntax");
            return;
        }
        Vector3 pos = player.Position;
        string operation = args[1];
        if (operation.Equals("existing", StringComparison.OrdinalIgnoreCase) || operation.Equals("open", StringComparison.OrdinalIgnoreCase))
        {
            if (_currentBuilder != null)
            {
                player.SendChat("edit_zone_existing_in_progress");
                return;
            }
            Zone? zone = null;
            if (args.Length < 3)
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
                {
                    player.SendChat("edit_zone_existing_badvalue");
                    return;
                }
            }
            else
            {
                string name = string.Join(" ", args, 2, args.Length - 2);
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
            {
                player.SendChat("edit_zone_existing_badvalue");
                return;
            }
            _currentBuilderIsExisting = true;
            _currentBuilder = zone.Builder;
            _currentPoints?.Clear();
            if (_currentBuilder.ZoneType == EZoneType.POLYGON && _currentBuilder.ZoneData.Points != null)
            {
                if (_currentPoints == null)
                    _currentPoints = new List<Vector2>(_currentBuilder.ZoneData.Points);
                else
                    _currentPoints.AddRange(_currentBuilder.ZoneData.Points);
            }
            _builders.Add(this);
            ITransportConnection tc = player.Player.channel.owner.transportConnection;
            string text = Translation.TranslateEnum(_currentBuilder.ZoneType, player.Steam64);
            if (_edit != null)
            {
                Data.SendEffectClearAll.InvokeAndLoopback(ENetReliability.Reliable, new ITransportConnection[] { player.Player.channel.owner.transportConnection });
                EffectManager.sendUIEffect(_edit.id, EDIT_KEY, tc, true);
                player.HasUIHidden = true;
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Name", _currentBuilder.Name);
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Type", text);
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Header", Translation.Translate("edit_zone_ui_suggested_commands", player!.Steam64));
            }
            player.SendChat("edit_zone_existing_success", _currentBuilder.Name!, text);
            CheckType(_currentBuilder.ZoneType, true);
            RefreshPreview();
        }
        else
        {
            if (_currentBuilder == null)
            {
                player.SendChat("edit_zone_not_started");
                return;
            }
            else if (operation.Equals("maxheight", StringComparison.OrdinalIgnoreCase))
            {
                float mh;
                if (args.Length > 2)
                {
                    if (!(float.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out float arg3) && !float.IsNaN(arg3) && !float.IsInfinity(arg3)))
                    {
                        player.SendChat("edit_zone_maxheight_badvalue");
                        return;
                    }
                    else mh = arg3;
                }
                else
                {
                    mh = pos.y;
                    if (float.IsNaN(mh) || float.IsInfinity(mh))
                    {
                        player.SendChat("edit_zone_maxheight_badvalue");
                        return;
                    }
                }
                _currentBuilder.MaxHeight = mh;
                UpdateHeights();
                player.SendChat("edit_zone_maxheight_success", mh.ToString("F2", Data.Locale));
            }
            else if (operation.Equals("type") || operation.Equals("shape"))
            {
                if (args.Length > 2)
                {
                    EZoneType type = EZoneType.INVALID;
                    string v = args[2];
                    if (v.Equals("rect", StringComparison.OrdinalIgnoreCase) || v.Equals("rectangle", StringComparison.OrdinalIgnoreCase) || v.Equals("square", StringComparison.OrdinalIgnoreCase))
                    {
                        type = EZoneType.RECTANGLE;
                        if (float.IsNaN(_currentBuilder.ZoneData.SizeX))
                            _currentBuilder.ZoneData.SizeX = 10f;
                        if (float.IsNaN(_currentBuilder.ZoneData.SizeZ))
                            _currentBuilder.ZoneData.SizeZ = 10f;
                    }
                    else if (v.Equals("circle", StringComparison.OrdinalIgnoreCase) || v.Equals("oval", StringComparison.OrdinalIgnoreCase) || v.Equals("ellipse", StringComparison.OrdinalIgnoreCase))
                    {
                        type = EZoneType.CIRCLE;
                        if (float.IsNaN(_currentBuilder.ZoneData.Radius))
                            _currentBuilder.ZoneData.Radius = 5f;
                    }
                    else if (v.Equals("polygon", StringComparison.OrdinalIgnoreCase) || v.Equals("poly", StringComparison.OrdinalIgnoreCase) || v.Equals("shape", StringComparison.OrdinalIgnoreCase))
                    {
                        type = EZoneType.POLYGON;
                    }
                    if (type == EZoneType.INVALID)
                    {
                        player.SendChat("edit_zone_type_badvalue");
                        return;
                    }
                    if (type == _currentBuilder.ZoneType)
                    {
                        player.SendChat("edit_zone_type_already_set", type.ToString().ToLower());
                        return;
                    }
                    CheckType(type);
                    player.SendChat("edit_zone_type_success", type.ToString().ToLower());
                }
            }
            else if (operation.Equals("minheight", StringComparison.OrdinalIgnoreCase))
            {
                float mh;
                if (args.Length > 2)
                {
                    if (!(float.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out float arg3) && !float.IsNaN(arg3) && !float.IsInfinity(arg3)))
                    {
                        player.SendChat("edit_zone_minheight_badvalue");
                        return;
                    }
                    else mh = arg3;
                }
                else
                {
                    mh = pos.y;
                    if (float.IsNaN(mh) || float.IsInfinity(mh))
                    {
                        player.SendChat("edit_zone_minheight_badvalue");
                        return;
                    }
                }
                _currentBuilder.MinHeight = mh;
                UpdateHeights();
                player.SendChat("edit_zone_minheight_success", mh.ToString("F2", Data.Locale));
                RefreshPreview();
            }
            else if (operation.Equals("finalize", StringComparison.OrdinalIgnoreCase) || operation.Equals("complete", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (_currentBuilder.UseCase == EZoneUseCase.OTHER || _currentBuilder.UseCase > EZoneUseCase.LOBBY)
                    {
                        player.SendChat("edit_zone_finalize_use_case");
                        return;
                    }
                    int replIndex = -1;
                    for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                    {
                        if (Data.ZoneProvider.Zones[i].Id == _currentBuilder.Id)
                        {
                            if (!_currentBuilderIsExisting)
                            {
                                player.SendChat("edit_zone_finalize_exists", _currentBuilder.Name!);
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
                        player.SendChat("edit_zone_finalize_failure", ex1.Message);
                        return;
                    }
                    catch (ZoneReadException ex2)
                    {
                        player.SendChat("edit_zone_finalize_failure", ex2.Message);
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
                    player.SendChat(@new ? "edit_zone_finalize_success" : "edit_zone_finalize_success_overwrite", _currentBuilder.Name!);
                    _currentPoints = null;
                    _currentBuilder = null;
                    if (_edit != null)
                        EffectManager.askEffectClearByID(_edit.id, player.Player.channel.owner.transportConnection);
                    player.HasUIHidden = false;
                    UCWarfare.I.UpdateLangs(player.Player.channel.owner);
                    _currentBuilderIsExisting = false;
                    RefreshPreview();
                }
                catch (Exception ex)
                {
                    player.SendChat("edit_zone_finalize_error", ex.Message);
                }
            }
            else if (operation.Equals("cancel", StringComparison.OrdinalIgnoreCase) || operation.Equals("discard", StringComparison.OrdinalIgnoreCase) || operation.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                player.SendChat("edit_zone_cancel_success", _currentBuilder.Name ?? "null");
                _currentBuilder = null;
                _currentPoints = null;
                _builders.Remove(this);
                if (_edit != null)
                    EffectManager.askEffectClearByID(_edit.id, player.Player.channel.owner.transportConnection);
                RefreshPreview();
            }
            else if (operation.Equals("addpoint", StringComparison.OrdinalIgnoreCase) || operation.Equals("addpt", StringComparison.OrdinalIgnoreCase))
            {
                float x;
                float z;
                if (args.Length > 3)
                {
                    if (!(float.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out float arg3) && !float.IsNaN(arg3) && !float.IsInfinity(arg3)) ||
                        !(float.TryParse(args[3], System.Globalization.NumberStyles.Any, Data.Locale, out float arg4) && !float.IsNaN(arg4) && !float.IsInfinity(arg4)))
                    {
                        player.SendChat("edit_zone_addpoint_badvalues");
                        return;
                    }
                    else
                    {
                        x = arg3;
                        z = arg4;
                    }
                }
                else
                {
                    x = pos.x;
                    z = pos.z;
                    if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(z) || float.IsInfinity(z))
                    {
                        player.SendChat("edit_zone_addpoint_badvalues");
                        return;
                    }
                }

                Vector2 v = new Vector2(x, z);
                if (_currentPoints != null)
                    _currentPoints.Add(v);
                else
                    _currentPoints = new List<Vector2>(8) { v };
                string txt = FromV2(v);
                if (!CheckType(EZoneType.POLYGON))
                {
                    ITransportConnection tc = player.Player.channel.owner.transportConnection;
                    EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + (_currentPoints.Count - 1), txt);
                    EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + (_currentPoints.Count - 1), true);
                    RefreshPreview();
                }
                player.SendChat("edit_zone_addpoint_success", _currentPoints.Count.ToString(Data.Locale), txt);
            }
            else if (operation.Equals("delpoint", StringComparison.OrdinalIgnoreCase) || operation.Equals("delpt", StringComparison.OrdinalIgnoreCase) ||
                     operation.Equals("deletepoint", StringComparison.OrdinalIgnoreCase) || operation.Equals("removepoint", StringComparison.OrdinalIgnoreCase) ||
                     operation.Equals("deletept", StringComparison.OrdinalIgnoreCase) || operation.Equals("removept", StringComparison.OrdinalIgnoreCase))
            {
                CheckType(EZoneType.POLYGON);
                float x;
                float z;
                ITransportConnection tc;
                if (args.Length > 3)
                {
                    if (!(float.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out float arg3) && !float.IsNaN(arg3) && !float.IsInfinity(arg3)) ||
                        !(float.TryParse(args[3], System.Globalization.NumberStyles.Any, Data.Locale, out float arg4) && !float.IsNaN(arg4) && !float.IsInfinity(arg4)))
                    {
                        player.SendChat("edit_zone_delpoint_badvalues");
                        return;
                    }
                    else
                    {
                        x = arg3;
                        z = arg4;
                    }
                }
                else if (args.Length > 2)
                {
                    if (int.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out int index))
                    {
                        if (_currentPoints == null || _currentPoints.Count < index || index < 0)
                        {
                            player.SendChat("edit_zone_point_number_not_point", index.ToString(Data.Locale));
                            return;
                        }
                        --index;
                        Vector2 pt = _currentPoints[index];
                        _currentPoints.RemoveAt(index);
                        if (!CheckType(EZoneType.POLYGON))
                        {
                            tc = player.Player.channel.owner.transportConnection;
                            for (int i = index; i < _currentPoints.Count; ++i)
                            {
                                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + i, FromV2(pt));
                            }
                            EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + _currentPoints.Count, false);
                            RefreshPreview();
                        }
                        player.SendChat("edit_zone_delpoint_success", (index + 1).ToString(Data.Locale), FromV2(pt));
                        return;
                    }
                    else
                    {
                        player.SendChat("edit_zone_delpoint_badvalues");
                        return;
                    }
                }
                else
                {
                    x = pos.x;
                    z = pos.z;
                    if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(z) || float.IsInfinity(z))
                    {
                        player.SendChat("edit_zone_delpoint_badvalues");
                        return;
                    }
                }
                Vector2 v = new Vector2(x, z);
                if (_currentPoints == null || _currentPoints.Count == 0)
                {
                    player.SendChat("edit_zone_point_none_nearby", FromV2(v));
                    return;
                }
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
                    player.SendChat("edit_zone_point_none_nearby", FromV2(v));
                    return;
                }
                Vector2 pt2 = _currentPoints[ind];
                _currentPoints.RemoveAt(ind);
                if (!CheckType(EZoneType.POLYGON))
                {
                    tc = player.Player.channel.owner.transportConnection;
                    for (int i = ind; i < _currentPoints.Count; ++i)
                    {
                        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + i, FromV2(pt2));
                    }
                    EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + _currentPoints.Count, false);
                    RefreshPreview();
                }
                player.SendChat("edit_zone_delpoint_success", (ind + 1).ToString(Data.Locale), FromV2(pt2));
                return;
            }
            else if (operation.Equals("clearpoints", StringComparison.OrdinalIgnoreCase) || operation.Equals("clearpts", StringComparison.OrdinalIgnoreCase) ||
                     operation.Equals("clrpoints", StringComparison.OrdinalIgnoreCase) || operation.Equals("clrpts", StringComparison.OrdinalIgnoreCase))
            {
                if (_currentPoints != null)
                    _currentPoints.Clear();
                if (!CheckType(EZoneType.POLYGON))
                {
                    ITransportConnection tc = player.Player.channel.owner.transportConnection;
                    for (int i = 0; i < POINT_ROWS; ++i)
                    {
                        EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + i, false);
                    }
                    RefreshPreview();
                }
                player.SendChat("edit_zone_clearpoints_success");
                RefreshPreview();
            }
            else if (operation.Equals("setpoint", StringComparison.OrdinalIgnoreCase) || operation.Equals("setpt", StringComparison.OrdinalIgnoreCase) ||
                     operation.Equals("movepoint", StringComparison.OrdinalIgnoreCase) || operation.Equals("movept", StringComparison.OrdinalIgnoreCase))
            {

                ITransportConnection tc;
                if (args.Length == 6) // <nearby src x> <nearby src z> <dest x> <dest z>
                {
                    if (!(float.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out float arg1) && !float.IsNaN(arg1) && !float.IsInfinity(arg1)) ||
                        !(float.TryParse(args[3], System.Globalization.NumberStyles.Any, Data.Locale, out float arg2) && !float.IsNaN(arg2) && !float.IsInfinity(arg2)) ||
                        !(float.TryParse(args[4], System.Globalization.NumberStyles.Any, Data.Locale, out float arg3) && !float.IsNaN(arg3) && !float.IsInfinity(arg3)) ||
                        !(float.TryParse(args[5], System.Globalization.NumberStyles.Any, Data.Locale, out float arg4) && !float.IsNaN(arg4) && !float.IsInfinity(arg4)))
                    {
                        player.SendChat("edit_zone_setpoint_badvalues");
                        return;
                    }
                    else
                    {
                        Vector2 v = new Vector2(arg1, arg2);
                        if (_currentPoints == null || _currentPoints.Count == 0)
                        {
                            player.SendChat("edit_zone_point_none_nearby", FromV2(v));
                            return;
                        }
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
                            player.SendChat("edit_zone_point_none_nearby", FromV2(v));
                            return;
                        }
                        Vector2 old = _currentPoints[ind];
                        Vector2 @new = new Vector2(arg3, arg4);
                        _currentPoints[ind] = @new;
                        tc = player.Player.channel.owner.transportConnection;
                        string v2 = FromV2(@new);
                        if (!CheckType(EZoneType.POLYGON))
                        {
                            EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + ind, v2);
                            EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + ind, true);
                            RefreshPreview();
                        }
                        player.SendChat("edit_zone_setpoint_success", (ind + 1).ToString(Data.Locale), FromV2(old), v2);
                        return;
                    }
                }
                else if (args.Length == 5) // <pt num> <dest x> <dest z>
                {
                    if (!int.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out int index) ||
                        !(float.TryParse(args[3], System.Globalization.NumberStyles.Any, Data.Locale, out float arg2) && !float.IsNaN(arg2) && !float.IsInfinity(arg2)) ||
                        !(float.TryParse(args[4], System.Globalization.NumberStyles.Any, Data.Locale, out float arg3) && !float.IsNaN(arg3) && !float.IsInfinity(arg3)))
                    {
                        player.SendChat("edit_zone_setpoint_badvalues");
                        return;
                    }
                    else
                    {
                        if (_currentPoints == null || _currentPoints.Count < index || index < 0)
                        {
                            player.SendChat("edit_zone_point_number_not_point", index.ToString(Data.Locale));
                            return;
                        }
                        --index;
                        Vector2 old = _currentPoints[index];
                        Vector2 @new = new Vector2(arg2, arg3);
                        _currentPoints[index] = @new;
                        tc = player.Player.channel.owner.transportConnection;
                        string v2 = FromV2(@new);
                        if (!CheckType(EZoneType.POLYGON))
                        {
                            EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + index, v2);
                            EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + index, true);
                            RefreshPreview();
                        }
                        player.SendChat("edit_zone_setpoint_success", (index + 1).ToString(Data.Locale), FromV2(old), v2);
                        return;

                    }
                }
                else if (args.Length == 4) // <src x> <src z>
                {
                    if (!(float.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out float arg1) && !float.IsNaN(arg1) && !float.IsInfinity(arg1)) ||
                        !(float.TryParse(args[3], System.Globalization.NumberStyles.Any, Data.Locale, out float arg2) && !float.IsNaN(arg2) && !float.IsInfinity(arg2)))
                    {
                        player.SendChat("edit_zone_setpoint_badvalues");
                        return;
                    }
                    else
                    {
                        Vector2 v = new Vector2(arg1, arg2);
                        if (_currentPoints == null || _currentPoints.Count == 0)
                        {
                            player.SendChat("edit_zone_point_none_nearby", FromV2(v));
                            return;
                        }
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
                            player.SendChat("edit_zone_point_none_nearby", FromV2(v));
                            return;
                        }
                        Vector2 old = _currentPoints[ind];
                        Vector2 @new = new Vector2(pos.x, pos.z);
                        _currentPoints[ind] = @new;
                        tc = player.Player.channel.owner.transportConnection;
                        string v2 = FromV2(@new);
                        if (!CheckType(EZoneType.POLYGON))
                        {
                            EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + ind, v2);
                            EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + ind, true);
                            RefreshPreview();
                        }
                        player.SendChat("edit_zone_setpoint_success", (ind + 1).ToString(Data.Locale), FromV2(old), v2);
                        return;
                    }
                }
                else if (args.Length == 3) // <pt num>
                {
                    if (!int.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out int index))
                    {
                        player.SendChat("edit_zone_setpoint_badvalues");
                        return;
                    }
                    else
                    {
                        if (_currentPoints == null || _currentPoints.Count < index || index < 0)
                        {
                            player.SendChat("edit_zone_point_number_not_point", index.ToString(Data.Locale));
                            return;
                        }
                        --index;
                        Vector2 old = _currentPoints[index];
                        Vector2 @new = new Vector2(pos.x, pos.z);
                        _currentPoints[index] = @new;
                        tc = player.Player.channel.owner.transportConnection;
                        string v2 = FromV2(@new);
                        if (!CheckType(EZoneType.POLYGON))
                        {
                            EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + index, v2);
                            EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "Polygon_Point_Num_" + index, true);
                            RefreshPreview();
                        }
                        player.SendChat("edit_zone_setpoint_success", (index + 1).ToString(Data.Locale), FromV2(old), v2);
                        return;
                    }
                }
                else
                {
                    player.SendChat("edit_zone_setpoint_badvalues");
                    return;
                }
            }
            else if (operation.Equals("orderpoint", StringComparison.OrdinalIgnoreCase) || operation.Equals("orderpt", StringComparison.OrdinalIgnoreCase))
            {
                if (!CheckType(EZoneType.POLYGON))
                    RefreshPreview();
                // todo
            }
            else if (operation.Equals("radius", StringComparison.OrdinalIgnoreCase))
            {
                float radius;
                if (args.Length == 2)
                {
                    radius = (new Vector2(pos.x, pos.z) - new Vector2(_currentBuilder.X, _currentBuilder.Z)).magnitude;
                }
                else if (!(float.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out radius) && !float.IsNaN(radius) && !float.IsInfinity(radius)))
                {
                    player.SendChat("edit_zone_radius_badvalue");
                    return;
                }
                _currentBuilder.ZoneData.Radius = radius;
                if (!CheckType(EZoneType.CIRCLE))
                {
                    EffectManager.sendUIEffectText(EDIT_KEY, player.Player.channel.owner.transportConnection, true, "Circle_Radius", _currentBuilder.ZoneData.Radius.ToString("F2", Data.Locale) + "m");
                    RefreshPreview();
                }
                player.SendChat("edit_zone_radius_success", radius.ToString("F2", Data.Locale));
                return;
            }
            else if (operation.Equals("sizex", StringComparison.OrdinalIgnoreCase) || operation.Equals("width", StringComparison.OrdinalIgnoreCase) || operation.Equals("length", StringComparison.OrdinalIgnoreCase))
            {
                float sizex;
                if (args.Length == 2)
                {
                    sizex = Mathf.Abs(pos.x - _currentBuilder.X) * 2;
                }
                else if (!(float.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out sizex) && !float.IsNaN(sizex) && !float.IsInfinity(sizex)))
                {
                    player.SendChat("edit_zone_sizex_badvalue");
                    return;
                }
                _currentBuilder.ZoneData.SizeX = sizex;
                if (!CheckType(EZoneType.RECTANGLE))
                {
                    EffectManager.sendUIEffectText(EDIT_KEY, player.Player.channel.owner.transportConnection, true, "Rect_SizeX", _currentBuilder.ZoneData.SizeX.ToString("F2", Data.Locale) + "m");
                    RefreshPreview();
                }
                player.SendChat("edit_zone_sizex_success", sizex.ToString("F2", Data.Locale));
                return;
            }
            else if (operation.Equals("sizez", StringComparison.OrdinalIgnoreCase) || operation.Equals("height", StringComparison.OrdinalIgnoreCase) || operation.Equals("depth", StringComparison.OrdinalIgnoreCase))
            {
                float sizez;
                if (args.Length == 2)
                {
                    sizez = Mathf.Abs(pos.z - _currentBuilder.Z) * 2;
                }
                else if (!(float.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out sizez) && !float.IsNaN(sizez) && !float.IsInfinity(sizez)))
                {
                    player.SendChat("edit_zone_sizez_badvalue");
                    return;
                }
                _currentBuilder.ZoneData.SizeZ = sizez;
                if (!CheckType(EZoneType.RECTANGLE))
                {
                    EffectManager.sendUIEffectText(EDIT_KEY, player.Player.channel.owner.transportConnection, true, "Rect_SizeZ", _currentBuilder.ZoneData.SizeZ.ToString("F2", Data.Locale) + "m");
                    RefreshPreview();
                }
                player.SendChat("edit_zone_sizez_success", sizez.ToString("F2", Data.Locale));
                return;
            }
            else if (operation.Equals("center", StringComparison.OrdinalIgnoreCase) || operation.Equals("position", StringComparison.OrdinalIgnoreCase))
            {
                float x;
                float z;
                if (args.Length == 2)
                {
                    x = pos.x;
                    z = pos.z;
                }
                else if (!(float.TryParse(args[2], System.Globalization.NumberStyles.Any, Data.Locale, out x) && !float.IsNaN(x) && !float.IsInfinity(x))
                      || !(float.TryParse(args[3], System.Globalization.NumberStyles.Any, Data.Locale, out z) && !float.IsNaN(z) && !float.IsInfinity(z)))
                {
                    player.SendChat("edit_zone_center_badvalue");
                    return;
                }
                _currentBuilder.X = x;
                _currentBuilder.Z = z;
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
                player.SendChat("edit_zone_center_success", FromV2(x, z));
                EffectManager.sendUIEffectText(EDIT_KEY, player.Player.channel.owner.transportConnection, true, "Name", _currentBuilder.ZoneData.SizeZ.ToString("F2", Data.Locale) + "m");
                RefreshPreview();
            }
            else if (operation.Equals("name", StringComparison.OrdinalIgnoreCase) || operation.Equals("title", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 3)
                {
                    player.SendChat("edit_zone_name_badvalue");
                    return;
                }
                string name = args.Length == 3 ? args[2] : string.Join(" ", args, 2, args.Length - 2);
                _currentBuilder.Name = name;
                EffectManager.sendUIEffectText(EDIT_KEY, player.Player.channel.owner.transportConnection, true, "Name", name);
                player.SendChat("edit_zone_name_success", name);
            }
            else if (operation.Equals("use", StringComparison.OrdinalIgnoreCase) || operation.Equals("usecase", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 3)
                {
                    player.SendChat("edit_zone_use_case_badvalue");
                    return;
                }

                bool w2ic = args[2].Equals("case", StringComparison.OrdinalIgnoreCase);
                if (args.Length == 3 && w2ic)
                {
                    player.SendChat("edit_zone_use_case_badvalue");
                    return;
                }
                if (Enum.TryParse(w2ic ? args[3] : args[2], true, out EZoneUseCase uc))
                {
                    _currentBuilder.UseCase = uc;
                    player.SendChat("edit_zone_use_case_success", uc.ToString().ToLower().Replace('_', ' '));
                }
                else
                {
                    player.SendChat("edit_zone_use_case_badvalue");
                }
            }
            else if (operation.Equals("shortname", StringComparison.OrdinalIgnoreCase) || operation.Equals("short", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 3)
                {
                    player.SendChat("edit_zone_name_badvalue");
                    return;
                }
                int st = args.Length > 3 && args[2].Equals("name", StringComparison.OrdinalIgnoreCase) ? 3 : 2;
                string name = args.Length == st + 1 ? args[st] : string.Join(" ", args, st, args.Length - st);
                _currentBuilder.ShortName = name;
                player.SendChat("edit_zone_name_success", name);
            }
        }
    }

    internal void ReloadLang()
    {
        if (_currentBuilder == null) return;
        CheckType(_currentBuilder.ZoneType, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FromV2(Vector2 v2) => $"({v2.x.ToString("F2", Data.Locale)}, {v2.y.ToString("F2", Data.Locale)})";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FromV2(float x, float y) => $"({x.ToString("F2", Data.Locale)}, {y.ToString("F2", Data.Locale)})";
    private bool CheckType(EZoneType type, bool overwrite = false)
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
        _currentBuilder.ZoneType = type;
        UpdateHeights();
        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Type", Translation.TranslateEnum(type, player.Steam64));
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
                        EffectManager.sendUIEffectText(EDIT_KEY, tc, true, "Polygon_Point_Value_" + i, FromV2(_currentPoints[i]));
                }
                EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, "PolygonParent", true);
                break;
            default: return false;
        }
        UpdateSuggestedCommands();
        RefreshPreview();
        return true;
    }
    private void UpdateSuggestedCommands()
    {
        if (_currentBuilder == null) return;
        ThreadUtil.assertIsGameThread();
        string[] cmds = new string[11];
        int pos = -1;
        cmds[++pos] = "edit_zone_ui_suggested_command_3";
        cmds[++pos] = "edit_zone_ui_suggested_command_1";
        cmds[++pos] = "edit_zone_ui_suggested_command_2";
        if (_currentBuilder.ZoneType == EZoneType.CIRCLE)
            cmds[++pos] = "edit_zone_ui_suggested_command_9_c";
        else if (_currentBuilder.ZoneType == EZoneType.RECTANGLE)
        {
            cmds[++pos] = "edit_zone_ui_suggested_command_10_r";
            cmds[++pos] = "edit_zone_ui_suggested_command_11_r";
        }
        else if (_currentBuilder.ZoneType == EZoneType.POLYGON)
        {
            cmds[++pos] = "edit_zone_ui_suggested_command_5_p";
            cmds[++pos] = "edit_zone_ui_suggested_command_6_p";
            cmds[++pos] = "edit_zone_ui_suggested_command_7_p";
            cmds[++pos] = "edit_zone_ui_suggested_command_8_p";
            cmds[++pos] = "edit_zone_ui_suggested_command_14_p";
        }
        cmds[++pos] = "edit_zone_ui_suggested_command_4";
        cmds[++pos] = "edit_zone_ui_suggested_command_13";
        cmds[++pos] = "edit_zone_ui_suggested_command_12";
        ++pos;
        ITransportConnection tc = player.Player.channel.owner.transportConnection;
        for (int i = 0; i < HELP_ROWS; ++i)
        {
            bool a = i < pos;
            string b = "CommandHelp (" + i + ")";
            EffectManager.sendUIEffectVisibility(EDIT_KEY, tc, true, b, a);
            if (a)
                EffectManager.sendUIEffectText(EDIT_KEY, tc, true, b, Translation.Translate(cmds[i], player.Steam64));
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
            Translation.Translate("edit_zone_ui_y_limits", player.Steam64,
            float.IsNaN(_currentBuilder.MinHeight) ? Translation.Translate("edit_zone_ui_y_limits_infinity", player.Steam64) : _currentBuilder.MinHeight.ToString("F2", Data.Locale),
            float.IsNaN(_currentBuilder.MaxHeight) ? Translation.Translate("edit_zone_ui_y_limits_infinity", player.Steam64) : _currentBuilder.MaxHeight.ToString("F2", Data.Locale)));
        }
    }
}
