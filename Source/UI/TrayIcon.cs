using UnityEngine;

namespace SceneFX.UI
{
    /// <summary>
    /// Procedurally drawn tray icon: a four-quadrant grading wheel.
    /// </summary>
    internal static class TrayIcon
    {
        internal static Texture2D Make()
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                name = "SceneFX.tray"
            };

            var center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x, y);
                    float distance = Vector2.Distance(p, center);
                    Color c = new Color(0f, 0f, 0f, 0f);

                    if (distance < 22f)
                    {
                        Vector2 dir = p - center;
                        bool right = dir.x >= 0f;
                        bool top = dir.y >= 0f;

                        if (top && right) c = new Color32(214, 116, 60, 255);   // warm
                        else if (top) c = new Color32(70, 130, 190, 255);       // cool
                        else if (right) c = new Color32(120, 78, 160, 255);     // cinematic
                        else c = new Color32(58, 140, 110, 255);                // natural
                    }

                    if (Mathf.Abs(distance - 14f) < 1.5f)
                    {
                        c = new Color(1f, 1f, 1f, 1f); // rim
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply(false, true);
            return tex;
        }
    }
}
