using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Flags;
using UnityEngine;

namespace UncreatedWarfare.Flags
{
    public abstract class Zone
    {
        public const float EffectiveImageScalingSize = 1920;
        public const float FullImageSize = 2048;
        internal float Multiplier = EffectiveImageScalingSize / FullImageSize;
        public Vector2 Center;
        internal bool SucessfullyParsed = false;
        public abstract bool IsInside(Vector2 location);
        public abstract bool IsInside(Vector3 location);
        public Zone(Vector2 Center)
        {
            this.Center = new Vector2(Center.x * Multiplier, Center.y * Multiplier);
        }
    }
    public class RectZone : Zone
    {
        public Vector2 Size;
        public RectZone(Vector2 Center, ZoneData data) : base(Center)
        {
            string[] size = data.data.Split(',');
            if(size.Length != 2)
            {
                CommandWindow.LogError($"Zone at ({Center.x}, {Center.y}) has invalid rectangle data. Format is: \"size x,size y\".");
                return;
            } else
            {
                if(float.TryParse(size[0], out float SizeX) && float.TryParse(size[1], out float SizeY))
                {
                    Size = new Vector2(SizeX * Multiplier, SizeY * Multiplier);
                    SucessfullyParsed = true;
                } else
                {
                    CommandWindow.LogError($"Zone at ({Center.x}, {Center.y}) has invalid rectangle data. Format is: \"size x,size y\".");
                    return;
                }
            }

        }
        public override bool IsInside(Vector2 location) => location.x > Center.x - Size.x / 2 && location.x < Center.x + Size.x / 2 && location.y > Center.y - Size.y / 2 && location.y < Center.y + Size.y / 2;
        public override bool IsInside(Vector3 location) => location.x > Center.x - Size.x / 2 && location.x < Center.x + Size.x / 2 && location.z > Center.y - Size.y / 2 && location.z < Center.y + Size.y / 2;
    }
    public class CircleZone : Zone
    {
        public float Radius;
        public CircleZone(Vector2 Center, ZoneData data) : base(Center)
        {
            string[] size = data.data.Split(',');
            if (size.Length != 1)
            {
                CommandWindow.LogError($"Zone at ({Center.x}, {Center.y}) has invalid circle data. Format is: \"radius\".");
                return;
            }
            else
            {
                if (float.TryParse(size[0], out float Rad))
                {
                    this.Radius = Rad * Multiplier;
                    SucessfullyParsed = true;
                }
                else
                {
                    CommandWindow.LogError($"Zone at ({Center.x}, {Center.y}) has invalid circle data. Format is: \"radius\".");
                    return;
                }
            }
        }
        public override bool IsInside(Vector2 location)
        {
            float difX = location.x - Center.x;
            float difY = location.y - Center.y;
            float sqrDistance = (difX * difX) + (difY * difY);
            return sqrDistance <= Radius * Radius;
        }
        public override bool IsInside(Vector3 location)
        {
            float difX = location.x - Center.x;
            float difY = location.z - Center.y;
            float sqrDistance = (difX * difX) + (difY * difY);
            return sqrDistance <= Radius * Radius;
        }
    }
    public class Line
    {
        public Vector2 pt1;
        public Vector2 pt2;
        public float m; // slope 
        public float b; // y-inx
        public Line(Vector2 pt1, Vector2 pt2)
        {
            this.pt1 = pt1;
            this.pt2 = pt2;
            this.m = (pt1.y - pt2.y) / (pt1.x - pt2.x);
            this.b = -1 * ((m * pt1.x) - pt1.y);
        }
        public bool IsIntersecting(float playerY, float playerX)
        {
            if (playerY < Math.Min(pt1.y, pt2.y) || playerY >= Math.Max(pt1.y, pt2.y)) return false; // if input value is out of vertical range of line
            if (pt1.x == pt2.x) return pt1.x >= playerX; // checks for undefined sloped (a completely vertical line)
            float x = GetX(playerY); // solve for y
            return x >= playerX; // if output value is in front of player
        }
        public float GetX(float y) => (y - b) / m; // inverse slope function
        public static Vector2 AvgAllPoints(Vector2[] Points)
        {
            float xTotal = 0;
            float yTotal = 0;
            int i;
            for (i = 0; i < Points.Length; i++)
            {
                xTotal += Points[i].x;
                yTotal += Points[i].y;
            }
            return new Vector2(xTotal / i, yTotal / i);
        }
    }
    public class PolygonZone : Zone
    {
        public Vector2[] Points;
        public Line[] Lines;
        public float Width;
        public PolygonZone(Vector2 Center, ZoneData data) : base(Center)
        {
            string[] size = data.data.Split(',');
            if (size.Length < 6 || size.Length % 2 == 1)
            {
                CommandWindow.LogError($"Zone at ({Center.x}, {Center.y}) has invalid polygon data. Format is: \"x1,y1,x2,y2,x3,y3,...\" with at least 3 points.");
                return;
            }
            Points = new Vector2[size.Length / 2];
            for (int i = 0; i < size.Length; i += 2)
            {
                if (float.TryParse(size[i], out float x) && float.TryParse(size[i + 1], out float y))
                {
                    Points[i / 2] = new Vector2(x * Multiplier, y * Multiplier);
                }
                else
                {
                    CommandWindow.LogError($"Zone at ({Center.x}, {Center.y}) has invalid polygon data. Format is: \"x1,y1,x2,y2,x3,y3,...\" with at least 3 points.\n{data.data}");
                    CommandWindow.LogError($"Couldn't parse ({size[i]}, {size[i + 1]}).");
                    return;
                }
            }
            Lines = new Line[Points.Length];
            for (int i = 0; i < Points.Length; i++)
                Lines[i] = new Line(Points[i], Points[i == Points.Length - 1 ? 0 : i + 1]);
        }
        public override bool IsInside(Vector2 location)
        {
            int intersects = 0;
            for (int i = 0; i < Lines.Length; i++)
            {
                if (Lines[i].IsIntersecting(location.y, location.x)) intersects++;
                if (intersects == 2) return false;
            }
            if (intersects == 1) return true;
            else return false;
        }
        public override bool IsInside(Vector3 location)
        {
            int intersects = 0;
            for (int i = 0; i < Lines.Length; i++)
            {
                if (Lines[i].IsIntersecting(location.z, location.x)) intersects++;
                if (intersects == 2) return false;
            }
            if (intersects == 1) return true;
            else return false;
        }
    }
}
