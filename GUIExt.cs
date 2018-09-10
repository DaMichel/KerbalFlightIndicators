using UnityEngine;

namespace KerbalFlightIndicators
{
    public static class GUIExt
    {
        public static void DrawTextureCentered(Vector3 position, Texture2D tex, float width, float height)
        {
            GUI.DrawTexture(new Rect(position.x - width * 0.5f,
                    position.y - height * 0.5f,
                    width, height),
                tex);
        }

        public static void DrawTextureCentered(Vector3 position, Texture2D tex)
        {
            DrawTextureCentered(position, tex, tex.width, tex.height);
        }

        public static void DrawTextureCentered(Vector3 position, Texture2D tex, float width, float height, Rect texCoords)
        {
            GUI.DrawTextureWithTexCoords(new Rect(position.x - width * 0.5f,
                    position.y - height * 0.5f,
                    width, height),
                tex, texCoords);
        }
    }
}
