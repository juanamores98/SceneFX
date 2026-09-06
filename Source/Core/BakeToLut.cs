using System;
using System.IO;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// Bakes the current visual style grade (exposure, warmth, contrast, brightness, filmic curve, gamma)
    /// into a 32x32x32 Texture3D, registers it in NativeLut, and exports a 1024x32 neutral LUT strip PNG.
    /// Clean-room implementation using the Uncharted 2 / Hable filmic response curve.
    /// </summary>
    internal static class BakeToLut
    {
        private const int Size = 32;

        public static string BakedFolder
        {
            get
            {
                return Path.Combine(StyleStore.StylesFolder, "Baked");
            }
        }

        public static Texture3D Bake(StyleData style, string name, out string exportedPngPath)
        {
            exportedPngPath = null;
            if (style == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "Custom_Baked";
            }

            float contrast = Mathf.Clamp(style.Contrast, -1f, 1f);
            float A = 0.5f + 0.18f * contrast;
            float B = 0.26f - 0.14f * contrast;
            float C = 0.1f - 0.008f * contrast;
            float D = 0.72f + 0.18f * contrast;
            float E = 0.01f;
            float F = 0.24f - 0.11f * contrast;
            float W = 11f + 2.2f * contrast;
            float boost = 1f + 0.5f * Mathf.Clamp(style.Brightness, -1f, 1f);
            float gamma = Mathf.Clamp(style.Gamma, 1.2f, 3f);
            float exposure = Mathf.Clamp(style.Exposure, 0.5f, 1.5f);
            float warmth = Mathf.Clamp(style.Warmth, -1f, 1f);

            float whiteScale = 1.0f / Filmic(W, A, B, C, D, E, F);

            var tex3D = new Texture3D(Size, Size, Size, TextureFormat.RGBA32, false);
            tex3D.name = "SceneFX.Baked." + name;

            var pixels3D = new Color[Size * Size * Size];
            var pixels2D = new Color[Size * Size * Size]; // for 1024x32 strip

            int index3D = 0;
            for (int b = 0; b < Size; b++)
            {
                for (int g = 0; g < Size; g++)
                {
                    for (int r = 0; r < Size; r++)
                    {
                        // Normalized identity input
                        float inR = r / (Size - 1f);
                        float inG = g / (Size - 1f);
                        float inB = b / (Size - 1f);

                        // 1. Exposure
                        inR *= exposure;
                        inG *= exposure;
                        inB *= exposure;

                        // 2. Warmth
                        inR *= (1f + 0.15f * warmth);
                        inB *= (1f - 0.15f * warmth);

                        // 3. Filmic tone mapping
                        float outR = Filmic(inR * boost, A, B, C, D, E, F) * whiteScale;
                        float outG = Filmic(inG * boost, A, B, C, D, E, F) * whiteScale;
                        float outB = Filmic(inB * boost, A, B, C, D, E, F) * whiteScale;

                        // 4. Gamma correction
                        outR = Mathf.Pow(Mathf.Clamp01(outR), 1f / gamma);
                        outG = Mathf.Pow(Mathf.Clamp01(outG), 1f / gamma);
                        outB = Mathf.Pow(Mathf.Clamp01(outB), 1f / gamma);

                        var c = new Color(outR, outG, outB, 1f);
                        pixels3D[index3D++] = c;

                        // Place in 2D strip (width 1024 = 32 * 32, height 32)
                        // x = b * 32 + r, y = g
                        int px = b * Size + r;
                        int py = g;
                        pixels2D[py * (Size * Size) + px] = c;
                    }
                }
            }

            tex3D.SetPixels(pixels3D);
            tex3D.Apply(false, true);

            // Register in NativeLut
            NativeLut.RegisterCustom(name, tex3D);

            var backup = style.Clone();
            backup.Name = style.Name + "_prebake_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            StyleStore.SaveStyle(backup);

            style.Gamma = 2.2f;
            style.Brightness = 0f;
            style.Contrast = 0f;
            style.Warmth = 0f;
            style.Exposure = 1f;

            Debug.Log("[SceneFX] Baked '" + name + "' to LUT; grade reset to neutral, previous look saved as '" + backup.Name + "'");

            // Export PNG
            try
            {
                if (!Directory.Exists(BakedFolder))
                {
                    Directory.CreateDirectory(BakedFolder);
                }

                var tex2D = new Texture2D(Size * Size, Size, TextureFormat.RGB24, false);
                tex2D.SetPixels(pixels2D);
                tex2D.Apply();

                byte[] pngBytes = tex2D.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex2D);

                string safeName = StyleStore.SafeName(name);
                string outPath = Path.Combine(BakedFolder, safeName + ".png");
                File.WriteAllBytes(outPath, pngBytes);
                exportedPngPath = outPath;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return tex3D;
        }

        private static float Filmic(float v, float A, float B, float C, float D, float E, float F)
        {
            v = Mathf.Max(0f, v);
            return ((v * (A * v + C * B) + D * E) / (v * (A * v + B) + D * F)) - (E / F);
        }
    }
}
