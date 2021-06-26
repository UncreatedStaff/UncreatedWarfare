using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Flags
{
    public static class ZoneDrawing
    {
        private static int overlayStep = 0;
        public static void CreateFlagTestAreaOverlay(Player player, List<Zone> zones, bool drawpath, bool drawrange, bool drawIsInTest, bool lockthreaduntildone = false, string filename = default)
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
                        SendPlayerZoneOverlay(img, player, newZones, PointsToTest, step, out done, filename);
                        step++;
                    }
                }
                else
                {
                    SendPlayerZoneOverlay(img, player, newZones, PointsToTest, 0, out _, filename);
                }
                if (drawrange)
                    SendPlayerZoneOverlay(img, player, newZones, PointsToTest, -3, out _, filename);
                if (drawpath)
                    SendPlayerZoneOverlay(img, player, newZones, PointsToTest, -2, out _, filename);
                SendPlayerZoneOverlay(img, player, newZones, PointsToTest, -1, out _, filename);
                if (player != default)
                    player.SendChat("Picture finished generating, check the Config\\Flags\\Presets folder menu.", UCWarfare.GetColor("default"));
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
                            SendPlayerZoneOverlay(img, player, newZones, PointsToTest, overlayStep, out done, filename);
                        }
                        else if (overlayStep == 0)
                        {
                            SendPlayerZoneOverlay(img, player, newZones, PointsToTest, 0, out done, filename);
                        }
                        overlayStep++;
                        yield return new WaitForSeconds(0.5f);
                        if (!done)
                            UCWarfare.I.StartCoroutine(enumerator());
                        else
                        {
                            if (drawrange)
                                SendPlayerZoneOverlay(img, player, newZones, PointsToTest, -3, out _, filename);
                            if (drawpath)
                                SendPlayerZoneOverlay(img, player, newZones, PointsToTest, -2, out _, filename);
                            SendPlayerZoneOverlay(img, player, newZones, PointsToTest, -1, out _, filename);
                            if (player != default)
                                player.SendChat("Picture finished generating, check the Config\\Flags\\Presets folder menu.", UCWarfare.GetColor("default"));
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
        internal static void SendPlayerZoneOverlay(Texture2D img, Player player, List<Zone> zones, List<Vector2> PointsToTest, int step, out bool complete, string filename)
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
                Texture2D flipped = F.FlipVertical(img);
                F.SavePhotoToDisk(Data.FlagStorage + (filename == default ? "zonearea.png" : filename + ".png"), flipped);
                UnityEngine.Object.Destroy(flipped);
                UnityEngine.Object.Destroy(img);
                complete = true;
            }
            else if (step == -2) // drawing path
            {
                for (int i = 0; i <= Data.FlagManager.FlagRotation.Count; i++)
                {
                    F.DrawLine(img, new Line(i == Data.FlagManager.FlagRotation.Count ? TeamManager.Team2Main.InverseZone.Center : Data.FlagManager.FlagRotation[i].ZoneData.InverseZone.Center,
                        i == 0 ? TeamManager.Team1Main.InverseZone.Center : Data.FlagManager.FlagRotation[i - 1].ZoneData.InverseZone.Center), Color.cyan, false, 8);
                }
                img.Apply();
            }
            else if (step == -3)
            {
                System.Random r = new System.Random();
                for (int i = 0; i < Data.FlagManager.FlagRotation.Count; i++)
                {
                    Color zonecolor = $"{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}".Hex();
                    F.DrawCircle(img, Data.FlagManager.FlagRotation[i].ZoneData.InverseZone.Center.x, Data.FlagManager.FlagRotation[i].ZoneData.InverseZone.Center.y, ObjectivePathing.FLAG_RADIUS_SEARCH * Data.FlagManager.FlagRotation[i].ZoneData.CoordinateMultiplier, zonecolor, 5, false, true);
                }
                F.DrawCircle(img, TeamManager.Team1Main.InverseZone.Center.x, TeamManager.Team1Main.InverseZone.Center.y, ObjectivePathing.MAIN_RADIUS_SEARCH * TeamManager.Team1Main.InverseZone.CoordinateMultiplier, UCWarfare.GetColor("team_1_color"), 5, false, true);
                F.DrawCircle(img, TeamManager.Team2Main.InverseZone.Center.x, TeamManager.Team2Main.InverseZone.Center.y, ObjectivePathing.MAIN_RADIUS_SEARCH * TeamManager.Team2Main.InverseZone.CoordinateMultiplier, UCWarfare.GetColor("team_2_color"), 5, false, true);
                img.Apply();
            }
        }
    }
}
