using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SDG.NetPak;
using SDG.NetTransport;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Uncreated.Warfare.Gamemodes.Flags;

public static class ZoneDrawing
{
    private static readonly Color Multidimensionalcolor = new Color(1, 1, 0, 0.8f);
    private static readonly Color Color1 = new Color(0.1f, 1, 0.15f, 0.8f);
    private static readonly Color Color2 = new Color(0.15f, 0.2f, 0.15f, 0.8f);
    private static readonly Color Color2Path = new Color(0.15f, 0, 0, 0.8f);
    private static readonly Color Color1Path = new Color(1, 0, 0, 0.8f);
    public static IEnumerator CreateFlagOverlay(CommandInteraction ctx, string? fileName = null, bool openOutput = false)
    {
        bool includeUnloadedZones = !ctx.MatchFlag("rot", "rotation", "noAll");
        bool extraZones = !ctx.MatchFlag("flags", "noExtra", "noMains");
        bool drawCurrentPath = ctx.MatchFlag("path", "pathing", "drawPath");
        bool drawIn = !ctx.MatchFlag("noFill", "noArea");
        bool drawAdjacencies = !ctx.MatchFlag("noAdj");
        bool chart = ctx.MatchFlag("chart");
        bool amcDamageWeights = ctx.MatchFlag("dmgMult");
        string msg = "Generating overlay with options: ";
        if (includeUnloadedZones)
            msg += "allZones";
        else
            msg += "rot";

        if (extraZones)
            msg += ", extraZones";
        else
            msg += ", noExtra";

        if (drawCurrentPath)
            msg += ", path";
        else
            msg += ", noPath";

        if (drawIn)
            msg += ", fill";
        else
            msg += ", noFill";

        if (drawAdjacencies)
            msg += ", adj";
        else
            msg += ", noAdj";

        if (chart)
            msg += ", chart";
        else
            msg += ", gps";

        if (amcDamageWeights)
            msg += ", dmgMult";
        else
            msg += ", noDmgMult";
        msg += ".";
        ctx.ReplyString(msg);

        ZoneList? list = Data.Singletons.GetSingleton<ZoneList>();
        if (list == null)
        {
            ctx.SendGamemodeError();
            yield break;
        }
        List<Zone> zones = new List<Zone>(16);
        if (!includeUnloadedZones && Data.Is(out IFlagRotation rotationGm))
            zones.AddRange(rotationGm.Rotation.Select(x => x.ZoneData.Item!).Where(x => x != null));
        else if (includeUnloadedZones)
        {
            list.WriteWait();
            try
            {
                zones.AddRange(list.Items.Where(x => x.Item is { Data.UseCase: ZoneUseCase.Flag }).Select(x => x.Item!).Where(x => x != null));
            }
            finally
            {
                list.WriteRelease();
            }
        }
        else if (!extraZones)
        {
            ctx.SendGamemodeError();
            yield break;
        }

        if (extraZones)
        {
            list.WriteWait();
            try
            {
                zones.AddRange(list.Items.Where(x => x.Item is { Data.UseCase: not ZoneUseCase.Flag }).Select(x => x.Item!).Where(x => x != null));
            }
            finally
            {
                list.WriteRelease();
            }
        }

        yield return null;
        
        Texture2D img = new Texture2D(GridLocation.ImageSize.X, GridLocation.ImageSize.Y);
        string levelMap = Path.Combine(Level.info.path, chart ? "Chart.png" : "Map.png");
        if (File.Exists(levelMap))
        {
            byte[] fileData = File.ReadAllBytes(levelMap);
            img.LoadImage(fileData, false);
        }
        
        if (drawAdjacencies)
        {
            for (int i = 0; i < zones.Count; ++i)
            {
                Zone zone = zones[i];
                if (zone.Data.UseCase is not ZoneUseCase.Flag and not ZoneUseCase.Team1Main and not ZoneUseCase.Team2Main)
                    continue;
                AdjacentFlagData[] adjs = zone.Data.Adjacencies;
                Vector2 c = zone.Center;
                c = GridLocation.WorldCoordsToMapCoords(new Vector3(c.x, 0, c.y));
                (float x1, float y1) = (c.x, c.y);
                foreach (AdjacentFlagData adj in adjs)
                {
                    if (adj.PrimaryKey == zone.PrimaryKey)
                        continue;
                    Zone? zone2 = zones.FirstOrDefault(x => x.PrimaryKey == adj.PrimaryKey);
                    if (zone2 == null)
                        continue;
                    c = zone2.Center;
                    c = GridLocation.WorldCoordsToMapCoords(new Vector3(c.x, 0, c.y));
                    (float x2, float y2) = (c.x, c.y);
                    Color c1 = Color1;
                    Color c2 = Color2;
                    if (Array.Exists(zone2.Data.Adjacencies, x => x.PrimaryKey == zone.PrimaryKey))
                    {
                        c1 = Multidimensionalcolor;
                        c2 = Multidimensionalcolor;
                    }
                    DrawLineGradient(new Line(x1, y1, x2, y2), 3f, img, c1, c2, false);
                }
            }

            yield return null;
        }
        if (drawCurrentPath && Data.Is(out IFlagRotation gm))
        {
            // draw path
            List<Flag> rot = gm.Rotation;
            float x2, y2;
            Vector2 c = TeamManager.Team1Main.Center;
            c = GridLocation.WorldCoordsToMapCoords(new Vector3(c.x, 0, c.y));
            (float x1, float y1) = (c.x, c.y);
            if (rot.Count == 0)
            {
                c = TeamManager.Team2Main.Center;
                c = GridLocation.WorldCoordsToMapCoords(new Vector3(c.x, 0, c.y));
                (x2, y2) = (c.x, c.y);
                DrawLineGradient(new Line(x1, y1, x2, y2), 4f, img, TeamManager.Team1Color, TeamManager.Team2Color, false);
            }
            else
            {
                list.WriteWait();
                try
                {
                    List<Zone> zones2 = rot.Select(x => x.ZoneData?.Item!).Where(x => x != null).ToList();
                    for (int i = 0; i < zones2.Count; i++)
                    {
                        Zone flag = zones2[i];
                        c = flag.Center;
                        c = GridLocation.WorldCoordsToMapCoords(new Vector3(c.x, 0, c.y));
                        (x2, y2) = (c.x, c.y);
                        Color c1 = i == 0 ? Color1Path : TeamManager.Team1Color;
                        Color c2 = Color2Path;
                        DrawLineGradient(new Line(x1, y1, x2, y2), 2f, img, c1, c2, false);
                        c = zones2.Count > i + 1 ? zones2[i + 1].Center : TeamManager.Team2Main.Center;
                        (x1, y1) = (c.x, c.y);
                    }

                    c = TeamManager.Team2Main.Center;
                    c = GridLocation.WorldCoordsToMapCoords(new Vector3(c.x, 0, c.y));
                    (x2, y2) = (c.x, c.y);
                    DrawLineGradient(new Line(x1, y1, x2, y2), 2f, img, Color1Path, TeamManager.Team2Color, false);
                }
                finally
                {
                    list.WriteRelease();
                }
            }

            yield return null;
        }

        foreach (Zone zone in zones.OrderByDescending(x => x.BoundsArea))
        {
            Color color = new Color(UnityEngine.Random.value * 0.5f + 0.25f, UnityEngine.Random.value * 0.5f + 0.25f, UnityEngine.Random.value * 0.5f + 0.25f, 0.5f);
            switch (zone)
            {
                case CircleZone circle:
                    float radx = GridLocation.WorldDistanceToMapDistanceX(circle.Radius);
                    float rady = GridLocation.WorldDistanceToMapDistanceX(circle.Radius);
                    Vector2 c = GridLocation.WorldCoordsToMapCoords(new Vector3(circle.Center.x, 0f, circle.Center.y));
                    (float x, float y) = (c.x, c.y);

                    if (drawIn)
                        FillCircle(img, x, y, radx, rady, color, false);
                    DrawCircle(img, x, y, Mathf.Max(radx, rady), Color.yellow, 1f, false);
                    DrawLine(img, new Line(new Vector2(x + radx, y + rady), new Vector2(x - radx, y + rady)), Color.red with { a = 0.33f });
                    DrawLine(img, new Line(new Vector2(x - radx, y + rady), new Vector2(x - radx, y - rady)), Color.red with { a = 0.33f });
                    DrawLine(img, new Line(new Vector2(x - radx, y - rady), new Vector2(x + radx, y - rady)), Color.red with { a = 0.33f });
                    DrawLine(img, new Line(new Vector2(x + radx, y - rady), new Vector2(x + radx, y + rady)), Color.red with { a = 0.33f });
                    break;
                case RectZone rect:
                    c = GridLocation.WorldCoordsToMapCoords(new Vector3(rect.Center.x, 0f, rect.Center.y));
                    (x, y) = (c.x, c.y);
                    c = GridLocation.WorldCoordsToMapCoords(new Vector3(rect.Center.x + rect.Size.x, 0f, rect.Center.y + rect.Size.y));
                    (float x1, float y1) = (c.x, c.y);

                    if (drawIn)
                        FillRectangle(img, x, y, x1 - x, y1 - y, color, false);
                    x -= (x1 - x);
                    y -= (y1 - y);
                    x1 -= (x1 - x);
                    y1 -= (y1 - y);
                    DrawLine(img, new Line(new Vector2(x, y), new Vector2(x1, y)), Color.red with { a = 0.33f });
                    DrawLine(img, new Line(new Vector2(x1, y), new Vector2(x1, y1)), Color.red with { a = 0.33f });
                    DrawLine(img, new Line(new Vector2(x1, y1), new Vector2(x, y1)), Color.red with { a = 0.33f });
                    DrawLine(img, new Line(new Vector2(x, y1), new Vector2(x, y)), Color.red with { a = 0.33f });
                    break;
                case PolygonZone polygon:
                    Vector4 bounds = polygon.Bounds;
                    c = GridLocation.WorldCoordsToMapCoords(new Vector3(bounds.x, 0f, bounds.y));
                    (x, y) = (c.x, c.y);
                    c = GridLocation.WorldCoordsToMapCoords(new Vector3(bounds.z, 0f, bounds.w));
                    (x1, y1) = (c.x, c.y);
                    
                    if (drawIn)
                    {
                        for (float x2 = x; x2 < x1; ++x2)
                        {
                            for (float y2 = y; y2 < y1; ++y2)
                            {
                                Vector3 wpos = GridLocation.MapCoordsToWorldCoords(new Vector2(x2, y2));
                                if (polygon.IsInside(new Vector2(wpos.x, wpos.z)))
                                    img.SetPixelClamp(Mathf.RoundToInt(x2), Mathf.RoundToInt(y2), color);
                            }
                        }
                    }

                    Vector2[] pts = polygon.Data.ZoneData.Points;
                    for (int i = 0; i < pts.Length; ++i)
                    {
                        int i2 = (i + 1) % pts.Length;
                        c = GridLocation.WorldCoordsToMapCoords(new Vector3(pts[i].x, 0f, pts[i].y));
                        (float px1, float py1) = (c.x, c.y);
                        c = GridLocation.WorldCoordsToMapCoords(new Vector3(pts[i2].x, 0f, pts[i2].y));
                        (float px2, float py2) = (c.x, c.y);
                        DrawLine(img, new Line(new Vector2(px1, py1), new Vector2(px2, py2)), Color.yellow with { a = 0.75f });
                    }

                    DrawLine(img, new Line(new Vector2(x, y), new Vector2(x1, y)), Color.red with { a = 0.33f });
                    DrawLine(img, new Line(new Vector2(x1, y), new Vector2(x1, y1)), Color.red with { a = 0.33f });
                    DrawLine(img, new Line(new Vector2(x1, y1), new Vector2(x, y1)), Color.red with { a = 0.33f });
                    DrawLine(img, new Line(new Vector2(x, y1), new Vector2(x, y)), Color.red with { a = 0.33f });
                    break;
            }

            yield return null;
        }

        if (amcDamageWeights)
        {
            for (int x = 0; x < img.width; x++)
            {
                for (int y = 0; y < img.width; y++)
                {
                    if (x % 2 == 0 && y % 2 == 0)
                        continue;

                    Vector3 coords = GridLocation.MapCoordsToWorldCoords(new Vector2(x, y));

                    float amc = Mathf.Min(TeamManager.GetAMCDamageMultiplier(1ul, coords), TeamManager.GetAMCDamageMultiplier(2ul, coords));
                    Color color = Color.Lerp(Color.red, Color.green, amc) with { a = 0.5f };
                    img.SetPixelClamp(x, y, color);
                }

                if (x % 16 == 0)
                    yield return null;
            }
        }
        img.Apply();
        if (fileName != null && !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            fileName += ".png";
        fileName ??= Path.Combine(Data.Paths.FlagStorage, "zonearea.png");
        yield return null;
        F.SavePhotoToDisk(fileName, img);
        yield return null;
        img.Reinitialize(img.width / 8, img.height / 8);
        if (!ctx.IsConsole && Data.SendScreenshotDestination != null)
        {
            byte[] jpeg = img.EncodeToJPG(75);
            L.LogDebug(jpeg.Length.ToString());
            if (jpeg.Length > 30000)
            {
                jpeg = img.EncodeToJPG(33);
                L.LogDebug(jpeg.Length.ToString());
            }
            if (jpeg.Length <= 30000)
            {
                Data.SendScreenshotDestination.Invoke(ctx.Caller.Player.GetNetId(), ENetReliability.Reliable, ctx.Caller.Connection, writer =>
                {
                    writer.WriteUInt16((ushort)jpeg.Length);
                    writer.WriteBytes(jpeg);
                });
            }
        }
        yield return null;
        UnityEngine.Object.Destroy(img);
#if DEBUG
        if (openOutput)
        {
            try
            {
                Process.Start(fileName);
            }
            catch (Exception ex)
            {
                L.Log("Unable to open output photo.");
                L.LogError(ex, method: "ZONE DRAWING");
            }
        }
#endif
    }
    public static void DrawLineGradient(Line line, float thickness, Texture2D texture, Color color1, Color color2, bool apply = true)
    {
        if (thickness == 0) return;
        Vector2 point1 = new Vector2(line.Point1.x, line.Point1.y);
        Vector2 point2 = new Vector2(line.Point2.x, line.Point2.y);
        Vector2 t = point1;
        float frac = 1 / Mathf.Sqrt(Mathf.Pow(point2.x - point1.x, 2) + Mathf.Pow(point2.y - point1.y, 2));
        float ctr = 0;

        while ((int)t.x != (int)point2.x || (int)t.y != (int)point2.y)
        {
            t = Vector2.Lerp(point1, point2, ctr);
            Color color = Color.Lerp(color1, color2, ctr);
            ctr += frac;
            texture.SetPixelClamp((int)t.x, (int)t.y, color);
            if (thickness > 1)
            {
                float distance = thickness / 2f;
                for (float i = -distance; i <= distance; i += 0.5f)
                    texture.SetPixelClamp(Mathf.RoundToInt(t.x + i), Mathf.RoundToInt(t.y + i), color);
            }
        }
        if (apply)
            texture.Apply();
    }
    // https://answers.unity.com/questions/244417/create-line-on-a-texture.html
    public static void DrawLine(Texture2D texture, Line line, Color color, bool apply = true, float thickness = 1)
    {
        if (thickness == 0) return;
        Vector2 point1 = line.Point1;
        Vector2 point2 = line.Point2;
        Vector2 t = point1;
        float frac = 1 / Mathf.Sqrt(Mathf.Pow(point2.x - point1.x, 2) + Mathf.Pow(point2.y - point1.y, 2));
        float ctr = 0;

        while ((int)t.x != (int)point2.x || (int)t.y != (int)point2.y)
        {
            t = Vector2.Lerp(point1, point2, ctr);
            ctr += frac;
            texture.SetPixelClamp((int)t.x, (int)t.y, color);
            if (thickness > 1)
            {
                float distance = thickness / 2f;
                for (float i = -distance; i <= distance; i += 0.5f)
                    texture.SetPixelClamp(Mathf.RoundToInt(t.x + i), Mathf.RoundToInt(t.y + i), color);
            }
        }
        if (apply)
            texture.Apply();
    }
    // https://stackoverflow.com/questions/30410317/how-to-draw-circle-on-texture-in-unity
    public static void FillCircle(Texture2D texture, float x, float y, float radiusX, float radiusY, Color color, bool apply = true)
    {
        float rSquared = Mathf.Pow(Mathf.Max(radiusX, radiusY), 2);
        
        for (float u = x - radiusX; u < x + radiusX + 1; u++)
        for (float v = y - radiusY; v < y + radiusY + 1; v++)
            if ((x - u) * (x - u) + (y - v) * (y - v) < rSquared)
                texture.SetPixelClamp((int)Math.Round(u), (int)Math.Round(v), color);
        if (apply)
            texture.Apply();
    }
    public static void FillRectangle(Texture2D texture, float x, float y, float sizeX, float sizeY, Color color, bool apply = true)
    {
        sizeX = Mathf.Abs(sizeX) / 2f;
        sizeY = Mathf.Abs(sizeY) / 2f;
        for (float x2 = x - sizeX; x2 < x + sizeX; ++x2)
        {
            for (float y2 = y - sizeY; y2 < y + sizeY; ++y2)
            {
                texture.SetPixelClamp(Mathf.RoundToInt(x2), Mathf.RoundToInt(y2), color);
            }
        }

        if (apply)
            texture.Apply();
    }
    public static void SetPixelClamp(this Texture2D texture, int x, int y, Color color)
    {
        if (x <= texture.width && x >= 0 && y <= texture.height && y >= 0)
        {
            if (color.a < 1f)
            {
                Color old = texture.GetPixel(x, y);
                color = (old * ((1f - color.a) * old.a) + color * color.a) with { a = 1f };
            }
            texture.SetPixel(x, y, color);
        }
    }
    public static void DrawCircle(Texture2D texture, float x, float y, float radius, Color color, float thickness = 1, bool apply = true, bool drawLineToOutside = false, float polygonResolutionScale = 1f)
    {
        if (thickness == 0) return;
        float sidesRadians = (Mathf.PI / 180) * polygonResolutionScale;
        float increment = (Mathf.PI * 2) * sidesRadians;
        int x1 = Mathf.RoundToInt(x + radius);
        int y1 = Mathf.RoundToInt(y);
        for (float r = 0; r < sidesRadians; r += increment)
        {
            Vector2 p = GetPositionOnCircle(r, radius);
            int x2 = Mathf.RoundToInt(p.x + x);
            int y2 = Mathf.RoundToInt(p.y + y);
            DrawLine(texture, new Line(new Vector2(x1, y1), new Vector2(x2, y2)), color, false, thickness);
            x1 = x2;
            y1 = y2;
        }
        if (drawLineToOutside)
        {
            DrawLine(texture, new Line(new Vector2(x, y), new Vector2(x + radius, y)), color, false, thickness);
            DrawLine(texture, new Line(new Vector2(x, y), new Vector2(x - radius, y)), color, false, thickness);
            DrawLine(texture, new Line(new Vector2(x, y), new Vector2(x, y + radius)), color, false, thickness);
            DrawLine(texture, new Line(new Vector2(x, y), new Vector2(x, y - radius)), color, false, thickness);
        }
        if (apply)
            texture.Apply();
    }
    public static Vector2 GetPositionOnCircle(float radians, float radius = 1) => new Vector2(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius);
}