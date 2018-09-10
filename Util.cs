using System;
using UnityEngine;

namespace KerbalFlightIndicators
{
    public static class Util
    {
        public static Vector3 x = Vector3.right;
        public static Vector3 y = Vector3.up;
        public static Vector3 z = Vector3.forward;

        public static float RAD2DEGf = 180f / Mathf.PI;
        public static double RAD2DEG = 180.0 / Math.PI;

        // angle need to rotate the x-axis so that it is parallel with (x,y); in Radians
        public static float PolarAngle(Vector2 v)
        {
            float n = v.magnitude;
            float a = Mathf.Acos(v.x / n);
            if (v.y < 0f) a = Mathf.PI * 2f - a;
            return a;
        }

        public static Vector3 PerspectiveProjection(Camera cam, Vector3 p)
        {
            return cam.projectionMatrix.MultiplyPoint(p);
        }

        public static Vector3 CameraToViewport(Camera cam, Vector3 v)
        {
            Vector3 q = cam.projectionMatrix.MultiplyPoint(v);
            // After projection, coordinates go from -1 to 1.
            // We need to transform to proper viewport space which goes from 0 to 1.
            // The bottom left is viewport-(0,0), according to the Unity 3d reference.
            // For some odd reason the x axis in camera space seems reversed. 
            q.y = (q.y + 1.0f) * 0.5f;
            q.x = (1.0f - q.x) * 0.5f;
            return q;
        }

        public static Vector3 CameraToScreen(Camera cam, Vector3 v)
        {
            return cam.ViewportToScreenPoint(CameraToViewport(cam, v));
        }

        public static Texture2D LoadTexture(string filename)
        {
            byte[] data = KSP.IO.File.ReadAllBytes<KerbalFlightIndicators>(filename);
            // we can just create a new texture object with a blank 2x2 image. 
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data); // Resizes the texture to hold the image data. Sets width and height attribute according to the loaded image.
            tex.anisoLevel = 0;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        public static Color ColorFromString(string s)
        {
            Color result = new Color();
            var strvalues = s.Split(',');
            for (int i = 0; i < strvalues.Length; ++i)
                result[i] = float.Parse(strvalues[i].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            return result;
        }

        public static string ColorToString(Color c)
        {
            string[] s = new string[4];
            for (int i = 0; i < 4; ++i)
                s[i] = c[i].ToString(System.Globalization.CultureInfo.InvariantCulture);
            return string.Join(",", s);
        }


        public static Rect[] ComputeTexCoords(int tex_width, int tex_height, RectOffset[] atlas)
        {
            Rect[] result = new Rect[atlas.Length];
            float sx = 1f / tex_width;
            float sy = 1f / tex_height;
            for (int i = 0; i < atlas.Length; ++i)
            {
                result[i] = new Rect(
                    atlas[i].left * sx,
                    1f - (atlas[i].bottom * sy),
                    (atlas[i].right - atlas[i].left) * sx,
                    (atlas[i].bottom - atlas[i].top) * sy
                );
            }
            return result;
        }

        public static RectOffset RectOffsetFromString(string s)
        {
            int left, right, bottom, top;
            var strvalues = s.Split(',');
            left = int.Parse(strvalues[0]);
            right = int.Parse(strvalues[1]);
            top = int.Parse(strvalues[2]);
            bottom = int.Parse(strvalues[3]);
            return new RectOffset(left, right, top, bottom);
        }
    }
}
