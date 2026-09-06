using System.Collections.Generic;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// Procedurally generated color grading tables owned by this mod. Every
    /// look is a small set of grading parameters evaluated over a 32x32x32
    /// identity cube with the mod's own response curves. Nothing here is
    /// extracted from anywhere: the tables are computed by this code.
    /// </summary>
    internal sealed class NativeLut
    {
        private const int Size = 32;

        internal readonly string Name;
        internal readonly float Exposure;
        internal readonly float Contrast;
        internal readonly float Warmth;
        internal readonly float Saturation;
        internal readonly float Lift;

        private NativeLut(string name, float exposure, float contrast, float warmth, float saturation, float lift)
        {
            Name = name;
            Exposure = exposure;
            Contrast = contrast;
            Warmth = warmth;
            Saturation = saturation;
            Lift = lift;
        }

        private static readonly Dictionary<string, NativeLut> Palette = new Dictionary<string, NativeLut>
        {
            { "ordinary", new NativeLut("ordinary", 1.00f, 0.08f, 0.06f, 1.02f, 0.00f) },
            { "nocturne", new NativeLut("nocturne", 0.82f, 0.15f, -0.25f, 0.90f, 0.01f) },
            { "sepia", new NativeLut("sepia", 0.95f, 0.05f, 0.45f, 0.55f, 0.02f) },
            { "cine", new NativeLut("cine", 0.92f, 0.22f, -0.05f, 0.95f, 0.02f) },
            { "frost", new NativeLut("frost", 1.02f, 0.05f, -0.40f, 0.95f, 0.00f) },
            { "ember", new NativeLut("ember", 1.05f, 0.12f, 0.50f, 1.05f, 0.01f) },
        };

        private static readonly Dictionary<string, Texture3D> Cache = new Dictionary<string, Texture3D>();
        private static readonly Dictionary<string, Texture3D> CustomLuts = new Dictionary<string, Texture3D>();

        internal static void RegisterCustom(string name, Texture3D texture)
        {
            if (string.IsNullOrEmpty(name) || texture == null)
            {
                return;
            }

            string key = name.ToLowerInvariant();
            Texture3D previous;
            if (CustomLuts.TryGetValue(key, out previous) && previous != null)
            {
                UnityEngine.Object.Destroy(previous);
            }

            CustomLuts[key] = texture;
        }

        internal static void ClearRuntimeTextures()
        {
            foreach (var texture in CustomLuts.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            CustomLuts.Clear();

            foreach (var texture in Cache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            Cache.Clear();
        }

        internal static bool Exists(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            string key = name.ToLowerInvariant();
            return Palette.ContainsKey(key) || CustomLuts.ContainsKey(key);
        }

        internal static List<string> Names()
        {
            var list = new List<string>(Palette.Keys);
            foreach (string k in CustomLuts.Keys)
            {
                if (!list.Contains(k))
                {
                    list.Add(k);
                }
            }

            return list;
        }

        internal static bool TryGet(string name, out Texture3D texture)
        {
            texture = null;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            string key = name.ToLowerInvariant();
            Texture3D custom;
            if (CustomLuts.TryGetValue(key, out custom) && custom != null)
            {
                texture = custom;
                return true;
            }

            NativeLut lut;
            if (!Palette.TryGetValue(key, out lut))
            {
                return false;
            }

            Texture3D cached;
            if (Cache.TryGetValue(lut.Name, out cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            texture = lut.Generate();
            Cache[lut.Name] = texture;
            return true;
        }

        private Texture3D Generate()
        {
            var texture = new Texture3D(Size, Size, Size, TextureFormat.RGBA32, false);
            texture.name = "SceneFX." + Name;

            var pixels = new Color[Size * Size * Size];
            int index = 0;
            for (int b = 0; b < Size; b++)
            {
                for (int g = 0; g < Size; g++)
                {
                    for (int r = 0; r < Size; r++)
                    {
                        Color c = new Color(r / (Size - 1f), g / (Size - 1f), b / (Size - 1f), 1f);

                        // Exposure.
                        c.r *= Exposure;
                        c.g *= Exposure;
                        c.b *= Exposure;

                        // Warmth: opposing R/B channel gain.
                        c.r *= 1f + 0.12f * Warmth;
                        c.b *= 1f - 0.12f * Warmth;

                        // Saturation around luminance.
                        float luma = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                        c.r = Mathf.Lerp(luma, c.r, Saturation);
                        c.g = Mathf.Lerp(luma, c.g, Saturation);
                        c.b = Mathf.Lerp(luma, c.b, Saturation);

                        // Contrast as an S-curve around mid gray.
                        c.r = ContrastCurve(c.r);
                        c.g = ContrastCurve(c.g);
                        c.b = ContrastCurve(c.b);

                        // Lift (fogged blacks).
                        c.r = Mathf.Lerp(c.r, 1f, Lift * 0.5f * (1f - c.r));
                        c.g = Mathf.Lerp(c.g, 1f, Lift * 0.5f * (1f - c.g));
                        c.b = Mathf.Lerp(c.b, 1f, Lift * 0.5f * (1f - c.b));

                        pixels[index++] = new Color(
                            Mathf.Clamp01(c.r),
                            Mathf.Clamp01(c.g),
                            Mathf.Clamp01(c.b),
                            255);
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>
        /// Own response curve: symmetric expansion around mid gray with a soft
        /// shoulder so highlights roll off instead of clipping hard.
        /// </summary>
        private float ContrastCurve(float v)
        {
            v = Mathf.Clamp01(v);
            float centered = v - 0.5f;
            float scaled = centered * (1f + 2f * Contrast);
            float expanded = 0.5f + scaled;
            expanded = Mathf.Clamp01(expanded);

            // Soft shoulder for the top range.
            if (expanded > 0.8f)
            {
                float over = (expanded - 0.8f) / 0.2f;
                expanded = 0.8f + 0.2f * (1f - (1f - over) * (1f - over));
            }

            return expanded;
        }
    }
}
