using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Uncreated.Warfare.Gamemodes.Flags
{
    public static class ZoneDrawing
    {
        private static int overlayStep = 0;
        public static void CreateFlagTestAreaOverlay(FlagGamemode gamemode, Player player, List<Zone> zones, bool drawpath, bool drawrange, bool drawIsInTest, bool drawsearchangles, bool lockthreaduntildone = false, string filename = default)
        {
            if (lockthreaduntildone)
            {
                List<Zone> newZones = zones;
                newZones.Sort(delegate (Zone a, Zone b)
                {
                    return b.BoundsArea.CompareTo(a.BoundsArea);
                });
                Texture2D img = new Texture2D(Level.size, Level.size);
                List<Vector2> PointsToTest = new List<Vector2>();
                for (int i = -1 * img.width / 2; i < img.width / 2; i += 1)
                {
                    for (int j = -1 * img.height / 2; j < img.height / 2; j += 1)
                    {
                        PointsToTest.Add(new Vector2(i, j));
                    }
                }
                if (drawIsInTest)
                {
                    int step = 0;
                    bool done = false;
                    while (!done)
                    {
                        SendPlayerZoneOverlay(gamemode, img, player, newZones, PointsToTest, step, out done, filename, false);
                        step++;
                    }
                }
                else
                {
                    SendPlayerZoneOverlay(gamemode, img, player, newZones, PointsToTest, 0, out _, filename, false);
                }
                if (drawrange)
                    SendPlayerZoneOverlay(gamemode, img, player, newZones, PointsToTest, -3, out _, filename, drawsearchangles);
                if (drawpath)
                    SendPlayerZoneOverlay(gamemode, img, player, newZones, PointsToTest, -2, out _, filename, false);
                SendPlayerZoneOverlay(gamemode, img, player, newZones, PointsToTest, -1, out _, filename, false);
                if (player != default)
                    player.SendChat("Picture finished generating, check the Config\\Maps\\Flags folder menu.", UCWarfare.GetColor("default"));
            }
            else
            {
                if (overlayStep == 0)
                {
                    List<Zone> newZones = zones;
                    newZones.Sort(delegate (Zone a, Zone b)
                    {
                        return b.BoundsArea.CompareTo(a.BoundsArea);
                    });
                    Texture2D img = new Texture2D(Level.size, Level.size);
                    List<Vector2> PointsToTest = new List<Vector2>();
                    for (int i = -1 * img.width / 2; i < img.width / 2; i += 1)
                    {
                        for (int j = -1 * img.height / 2; j < img.height / 2; j += 1)
                        {
                            PointsToTest.Add(new Vector2(i, j));
                        }
                    }
                    UCWarfare.I.StartCoroutine(enumerator());
                    IEnumerator<WaitForSeconds> enumerator()
                    {
                        bool done = !drawIsInTest;
                        if (drawIsInTest)
                        {
                            SendPlayerZoneOverlay(gamemode, img, player, newZones, PointsToTest, overlayStep, out done, filename, false);
                        }
                        else if (overlayStep == 0)
                        {
                            SendPlayerZoneOverlay(gamemode, img, player, newZones, PointsToTest, 0, out done, filename, false);
                        }
                        overlayStep++;
                        yield return new WaitForSeconds(0.5f);
                        if (!done)
                            UCWarfare.I.StartCoroutine(enumerator());
                        else
                        {
                            if (drawrange)
                                SendPlayerZoneOverlay(gamemode, img, player, newZones, PointsToTest, -3, out _, filename, drawsearchangles);
                            if (drawpath)
                                SendPlayerZoneOverlay(gamemode, img, player, newZones, PointsToTest, -2, out _, filename, false);
                            SendPlayerZoneOverlay(gamemode, img, player, newZones, PointsToTest, -1, out _, filename, false);
                            if (player != default)
                                player.SendChat("Picture finished generating, check the Config\\Maps\\Flags folder menu.", UCWarfare.GetColor("default"));
                            overlayStep = 0;
                        }
                    }
                }
                else
                {
                    if (player != default)
                        player.SendChat("A player is already running this procedure, try again in a few minutes.", UCWarfare.GetColor("default"));
                }
            }
        }
        internal static void SendPlayerZoneOverlay(FlagGamemode gamemode, Texture2D img, Player player, List<Zone> zones, List<Vector2> PointsToTest, int step, out bool complete, string filename, bool drawAngles)
        {
            complete = false;
            F.Log("STEP " + step.ToString(Data.Locale));
            if (step == 0)
            {
                if (File.Exists(Level.info.path + @"\Map.png"))
                {
                    byte[] fileData = File.ReadAllBytes(Level.info.path + @"\Map.png");
                    img.LoadImage(fileData, false);
                }
                img.Apply();
            }
            else if (step == 1)
            {
                foreach (Zone zone in zones)
                {
                    if (zone.GetType() == typeof(PolygonZone))
                    {
                        PolygonZone pzone = (PolygonZone)zone;
                        for (int i = 0; i < pzone.PolygonInverseZone.Lines.Length; i++)
                        {
                            F.DrawLine(img, pzone.PolygonInverseZone.Lines[i], Color.black, false);
                        }
                    }
                    else if (zone.GetType() == typeof(CircleZone))
                    {
                        CircleZone czone = (CircleZone)zone;
                        F.FillCircle(img, czone.InverseZone.Center.x + img.width / 2, czone.InverseZone.Center.y + img.height / 2, czone.CircleInverseZone.Radius, Color.black, false);
                    }
                    else if (zone.GetType() == typeof(RectZone))
                    {
                        RectZone rzone = (RectZone)zone;
                        for (int i = 0; i < rzone.RectInverseZone.lines.Length; i++)
                        {
                            F.DrawLine(img, rzone.RectInverseZone.lines[i], Color.black, false);
                        }
                    }
                }
                //player.SendChat("Completed step 2", UCWarfare.GetColor("default"));
                img.Apply();
            }
            else if (step > 1)
            {
                int z = (step - 2) * 3;    //0
                int next = (step - 1) * 3; //3
                if (zones.Count <= next) complete = true;
                System.Random r = new System.Random();
                for (int e = z; e < (zones.Count > next ? next : zones.Count); e++)
                {
                    Color zonecolor = $"{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}".Hex();
                    for (int i = 0; i < PointsToTest.Count; i++)
                    {
                        if (zones[e].InverseZone.IsInside(new Vector2(PointsToTest[i].x, PointsToTest[i].y)))
                        {
                            img.SetPixelClamp((int)Math.Round(PointsToTest[i].x + img.width / 2), (int)Math.Round(PointsToTest[i].y + img.height / 2), zonecolor);
                        }
                    }
                }
                //player.SendChat("Completed step " + (step + 1).ToString(Data.Locale), UCWarfare.GetColor("default"));
                img.Apply();
            }
            else if (step == -1) // finalizing image
            {
                img.Apply();
                F.SavePhotoToDisk(filename == default ? Data.FlagStorage + "zonearea.png" : filename + ".png", img);
                UnityEngine.Object.Destroy(img);
                complete = true;
            }
            else if (step == -2) // drawing path
            {
                for (int i = 0; i <= gamemode.Rotation.Count; i++)
                {
                    F.DrawLine(img, new Line(i == gamemode.Rotation.Count ? TeamManager.Team2Main.InverseZone.Center : gamemode.Rotation[i].ZoneData.InverseZone.Center,
                        i == 0 ? TeamManager.Team1Main.InverseZone.Center : gamemode.Rotation[i - 1].ZoneData.InverseZone.Center), Color.cyan, false, 8);
                }
                img.Apply();
            }
            else if (step == -3)
            {
                System.Random r = new System.Random();
                for (int i = 0; i < zones.Count; i++)
                {
                    Color zonecolor = $"{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}".Hex();
                    F.DrawCircle(img, zones[i].InverseZone.Center.x, zones[i].InverseZone.Center.y, ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier, zonecolor, 5, false, true);
                    if (drawAngles)
                    {
                        F.DrawLine(img,
                            new Line(
                                new Vector2(zones[i].InverseZone.Center.x - Mathf.Cos(ObjectivePathing.SIDE_ANGLE_LEFT_END) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier,
                                zones[i].InverseZone.Center.y - Mathf.Sin(ObjectivePathing.SIDE_ANGLE_LEFT_END) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier),
                                new Vector2(zones[i].InverseZone.Center.x + Mathf.Cos(ObjectivePathing.SIDE_ANGLE_LEFT_END) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier,
                                zones[i].InverseZone.Center.y + Mathf.Sin(ObjectivePathing.SIDE_ANGLE_LEFT_END) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier)),
                            zonecolor, false, 3);
                        F.DrawLine(img,
                            new Line(
                                new Vector2(zones[i].InverseZone.Center.x - Mathf.Cos(ObjectivePathing.SIDE_ANGLE_RIGHT_END) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier,
                                zones[i].InverseZone.Center.y - Mathf.Sin(ObjectivePathing.SIDE_ANGLE_RIGHT_END) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier),
                                new Vector2(zones[i].InverseZone.Center.x + Mathf.Cos(ObjectivePathing.SIDE_ANGLE_RIGHT_END) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier,
                                zones[i].InverseZone.Center.y + Mathf.Sin(ObjectivePathing.SIDE_ANGLE_RIGHT_END) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier)),
                            zonecolor, false, 3);
                        F.DrawLine(img,
                            new Line(
                                new Vector2(zones[i].InverseZone.Center.x - Mathf.Cos(ObjectivePathing.SIDE_ANGLE_LEFT_START) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier,
                                zones[i].InverseZone.Center.y - Mathf.Sin(ObjectivePathing.SIDE_ANGLE_LEFT_START) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier),
                                new Vector2(zones[i].InverseZone.Center.x + Mathf.Cos(ObjectivePathing.SIDE_ANGLE_LEFT_START) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier,
                                zones[i].InverseZone.Center.y + Mathf.Sin(ObjectivePathing.SIDE_ANGLE_LEFT_START) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier)),
                            zonecolor, false, 3);
                        F.DrawLine(img,
                            new Line(
                                new Vector2(zones[i].InverseZone.Center.x - Mathf.Cos(ObjectivePathing.SIDE_ANGLE_RIGHT_START) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier,
                                zones[i].InverseZone.Center.y - Mathf.Sin(ObjectivePathing.SIDE_ANGLE_RIGHT_START) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier),
                                new Vector2(zones[i].InverseZone.Center.x + Mathf.Cos(ObjectivePathing.SIDE_ANGLE_RIGHT_START) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier,
                                zones[i].InverseZone.Center.y + Mathf.Sin(ObjectivePathing.SIDE_ANGLE_RIGHT_START) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier)),
                            zonecolor, false, 3);
                        F.DrawLine(img,
                             new Line(
                                 zones[i].InverseZone.Center,
                                 new Vector2(zones[i].InverseZone.Center.x + Mathf.Cos(ObjectivePathing.MAIN_BASE_ANGLE_OFFSET) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier,
                                 zones[i].InverseZone.Center.y + Mathf.Sin(ObjectivePathing.MAIN_BASE_ANGLE_OFFSET) * ObjectivePathing.FLAG_RADIUS_SEARCH * zones[i].CoordinateMultiplier)),
                             zonecolor, false, 3);
                    }
                }
                F.DrawCircle(img, TeamManager.Team1Main.InverseZone.Center.x, TeamManager.Team1Main.InverseZone.Center.y, ObjectivePathing.MAIN_SEARCH_RADIUS * TeamManager.Team1Main.InverseZone.CoordinateMultiplier, UCWarfare.GetColor("team_1_color"), 5, false, true);
                F.DrawCircle(img, TeamManager.Team2Main.InverseZone.Center.x, TeamManager.Team2Main.InverseZone.Center.y, ObjectivePathing.MAIN_SEARCH_RADIUS * TeamManager.Team2Main.InverseZone.CoordinateMultiplier, UCWarfare.GetColor("team_2_color"), 5, false, true);
                img.Apply();
            }
        }
        public static void DrawZoneMap(FlagGamemode gamemode, string filename)
        {
            Color multidimensionalcolor = new Color(1, 1, 0);
            Color multidimensionalcolorpath = new Color(1, 0.25f, 0);
            Color color1 = new Color(0.1f, 1, 0.15f);
            Color color2 = new Color(0.15f, 0.2f, 0.15f);
            Color color2path = new Color(0.15f, 0, 0);
            Color color1path = new Color(1, 0, 0);
            Color color1missingpath = new Color(0, 0, 0.15f);
            Color color2missingpath = new Color(0, 0, 1);
            int thickness = 8;
            Texture2D img = new Texture2D(Level.size, Level.size);
            if (File.Exists(Level.info.path + @"\Map.png"))
            {
                byte[] fileData = File.ReadAllBytes(Level.info.path + @"\Map.png");
                img.LoadImage(fileData, false);
            }
            Dictionary<Flag, float> flags = new Dictionary<Flag, float>();
            if (gamemode is TeamCTF.TeamCTF ctf)
            {
                flags = ObjectivePathing.InstantiateFlags(ctf.Config.team1adjacencies, ctf.AllFlags, null, null);
                foreach (KeyValuePair<Flag, float> t1mainarrow in flags)
                    DrawLineGradient(new Line(TeamManager.Team1Main.InverseZone.Center, t1mainarrow.Key.ZoneData.InverseZone.Center), thickness, img, TeamManager.Team1Color,
                        gamemode.Rotation.Count > 0 && gamemode.Rotation[0].ID == t1mainarrow.Key.ID ? color1path : color2, false);
                flags = ObjectivePathing.InstantiateFlags(ctf.Config.team2adjacencies, ctf.AllFlags, null, null);
                foreach (KeyValuePair<Flag, float> t2mainarrow in flags)
                    DrawLineGradient(new Line(t2mainarrow.Key.ZoneData.InverseZone.Center, TeamManager.Team2Main.InverseZone.Center), thickness, img,
                        gamemode.Rotation.Count > 0 && gamemode.Rotation.Last().ID == t2mainarrow.Key.ID ? color1path : color1, TeamManager.Team2Color, false);
            }
            List<int> drewPaths = new List<int>();
            List<KeyValuePair<int, int>> drawnLines = new List<KeyValuePair<int, int>>();
            foreach (Flag flag in gamemode.AllFlags)
            {
                flags = ObjectivePathing.InstantiateFlags(flag.Adjacencies, gamemode.AllFlags, null, null);
                int i = gamemode.Rotation.FindIndex(x => x.ID == flag.ID);
                foreach (KeyValuePair<Flag, float> flagarrow in flags)
                {
                    if (drawnLines.Exists(x => (x.Value == flagarrow.Key.ID && x.Key == flag.ID) || (x.Value == flag.ID && x.Key == flagarrow.Key.ID))) // multi-directional
                    {
                        Color c1 = multidimensionalcolor;
                        if (i != -1 && ((gamemode.Rotation.Count > i + 1 && gamemode.Rotation[i + 1].ID == flagarrow.Key.ID) || (i != 0 && gamemode.Rotation[i - 1].ID == flagarrow.Key.ID)))
                        {
                            c1 = multidimensionalcolorpath;
                            drewPaths.Add(flag.ID);
                        }
                        DrawLineGradient(new Line(flag.ZoneData.InverseZone.Center, flagarrow.Key.ZoneData.InverseZone.Center), thickness, img, c1, c1, false);
                    }
                    else
                    {
                        Color c1 = color1;
                        Color c2 = color2;
                        if (i != -1 && gamemode.Rotation.Count > i + 1 && gamemode.Rotation[i + 1].ID == flagarrow.Key.ID)
                        {
                            c1 = color1path;
                            c2 = color2path;
                            drewPaths.Add(flag.ID);
                        }
                        drawnLines.Add(new KeyValuePair<int, int>(flag.ID, flagarrow.Key.ID));
                        DrawLineGradient(new Line(flag.ZoneData.InverseZone.Center, flagarrow.Key.ZoneData.InverseZone.Center), thickness, img, c1, c2, false);
                    }
                }
            }
            for (int i = 0; i <= gamemode.Rotation.Count; i++)
            {
                if (i == 0 || i == gamemode.Rotation.Count || drewPaths.Contains(gamemode.Rotation[i - 1].ID)) continue; // line already drawn
                Line line;
                if (i == gamemode.Rotation.Count)
                {
                    line = new Line(gamemode.Rotation[i - 1].ZoneData.InverseZone.Center, TeamManager.Team2Main.InverseZone.Center);
                }
                else if (i == 0)
                {
                    line = new Line(TeamManager.Team1Main.InverseZone.Center, gamemode.Rotation[i].ZoneData.InverseZone.Center);
                }
                else
                {
                    line = new Line(gamemode.Rotation[i - 1].ZoneData.InverseZone.Center, gamemode.Rotation[i].ZoneData.InverseZone.Center);
                }
                DrawLineGradient(line, thickness / 2, img, color1missingpath, color2missingpath, false);
            }
            img.Apply();
            F.SavePhotoToDisk(filename == default ? Data.FlagStorage + "zonemap.png" : filename + ".png", img);
            UnityEngine.Object.Destroy(img);
        }

        public static void DrawArrow(Texture2D Texture, Line line, Color color, bool apply = true, float thickness = 1, float arrowHeadLength = 12)
        {
            F.DrawLine(Texture, line, color, false, thickness);
            float mult = line.length / arrowHeadLength;
            Vector2 d = (line.pt1 - line.pt2) * mult;
            Vector2 endLeft = line.pt1 - d;
            Vector2 endRight = line.pt1 + d;
            Line left = new Line(line.pt1, endLeft);
            Line right = new Line(line.pt1, endRight);
            F.DrawLine(Texture, left, color, false, thickness);
            F.DrawLine(Texture, right, color, false, thickness);
            if (apply) Texture.Apply();
        }
        public static void DrawLineGradient(Line line, float thickness, Texture2D texture, Color color1, Color color2, bool apply = true)
        {
            if (thickness == 0) return;
            Vector2 point1 = new Vector2(line.pt1.x + texture.width / 2, line.pt1.y + texture.height / 2);
            Vector2 point2 = new Vector2(line.pt2.x + texture.width / 2, line.pt2.y + texture.height / 2);
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
    }
}
