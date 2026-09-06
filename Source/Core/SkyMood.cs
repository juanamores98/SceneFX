using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// Procedurally generated sky cubemaps owned by this mod. Each mood is a
    /// small gradient palette evaluated over the six cube faces with the
    /// mod's own math; the generated cubemaps replace the game's environment
    /// and outer-space cubemaps until restored. Nothing is extracted from
    /// third parties.
    /// </summary>
    internal static class SkyMood
    {
        private const int FaceSize = 64;

        private sealed class Palette
        {
            internal readonly string Name;
            internal readonly Color Horizon;
            internal readonly Color Zenith;
            internal readonly float WarmBand;

            internal Palette(string name, float hr, float hg, float hb, float zr, float zg, float zb, float warmBand)
            {
                Name = name;
                Horizon = new Color(hr, hg, hb, 1f);
                Zenith = new Color(zr, zg, zb, 1f);
                WarmBand = warmBand;
            }
        }

        private static readonly List<Palette> Palettes = new List<Palette>
        {
            new Palette("Ordinary", 0.62f, 0.74f, 0.86f, 0.28f, 0.46f, 0.78f, 0.05f),
            new Palette("Dusk", 0.86f, 0.52f, 0.34f, 0.24f, 0.26f, 0.48f, 0.35f),
            new Palette("Night", 0.10f, 0.13f, 0.24f, 0.02f, 0.03f, 0.09f, 0.0f),
            new Palette("Pastel", 0.88f, 0.84f, 0.86f, 0.58f, 0.66f, 0.86f, 0.10f),
        };

        private static readonly Dictionary<string, Cubemap> Cache = new Dictionary<string, Cubemap>(StringComparer.OrdinalIgnoreCase);

        private static Cubemap _originalMain;
        private static Cubemap _originalOuter;
        private static Texture _originalSkybox;
        private static bool _applied;

        internal static IReadOnlyList<string> Names
        {
            get
            {
                var names = new List<string> { "Keep" };
                foreach (Palette p in Palettes)
                {
                    names.Add(p.Name);
                }

                return names;
            }
        }

        internal static void Apply(int index)
        {
            if (index <= 0 || index > Palettes.Count)
            {
                Restore();
                return;
            }

            Snapshot();
            Cubemap cubemap = GetOrCreate(Palettes[index - 1]);

            var renderProps = UnityEngine.Object.FindObjectOfType<RenderProperties>();
            if (renderProps != null)
            {
                renderProps.m_cubemap = cubemap;
            }

            var dayNight = UnityEngine.Object.FindObjectOfType<DayNightProperties>();
            if (dayNight != null)
            {
                dayNight.m_OuterSpaceCubemap = cubemap;

                var skyboxField = typeof(DayNightProperties).GetField("m_SkyboxMaterial", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var material = skyboxField != null ? skyboxField.GetValue(dayNight) as Material : null;
                if (material != null)
                {
                    material.mainTexture = cubemap;
                }
            }

            _applied = true;
        }

        internal static void Restore()
        {
            if (!_applied)
            {
                return;
            }

            var renderProps = UnityEngine.Object.FindObjectOfType<RenderProperties>();
            if (renderProps != null && _originalMain != null)
            {
                renderProps.m_cubemap = _originalMain;
            }

            var dayNight = UnityEngine.Object.FindObjectOfType<DayNightProperties>();
            if (dayNight != null)
            {
                if (_originalOuter != null)
                {
                    dayNight.m_OuterSpaceCubemap = _originalOuter;
                }

                var skyboxField = typeof(DayNightProperties).GetField("m_SkyboxMaterial", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var material = skyboxField != null ? skyboxField.GetValue(dayNight) as Material : null;
                if (material != null && _originalSkybox != null)
                {
                    material.mainTexture = _originalSkybox;
                }
            }

            _applied = false;
        }

        private static void Snapshot()
        {
            if (_applied)
            {
                return;
            }

            var renderProps = UnityEngine.Object.FindObjectOfType<RenderProperties>();
            _originalMain = renderProps != null ? renderProps.m_cubemap : null;

            var dayNight = UnityEngine.Object.FindObjectOfType<DayNightProperties>();
            if (dayNight != null)
            {
                _originalOuter = dayNight.m_OuterSpaceCubemap;
                var skyboxField = typeof(DayNightProperties).GetField("m_SkyboxMaterial", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var material = skyboxField != null ? skyboxField.GetValue(dayNight) as Material : null;
                _originalSkybox = material != null ? material.mainTexture : null;
            }
        }

        private static Cubemap GetOrCreate(Palette palette)
        {
            Cubemap cached;
            if (Cache.TryGetValue(palette.Name, out cached) && cached != null)
            {
                return cached;
            }

            var cubemap = new Cubemap(FaceSize, TextureFormat.ARGB32, false)
            {
                name = "SceneFX.Sky." + palette.Name
            };

            Color[] facePixels = new Color[FaceSize * FaceSize];
            for (int face = 0; face < 6; face++)
            {
                for (int y = 0; y < FaceSize; y++)
                {
                    for (int x = 0; x < FaceSize; x++)
                    {
                        Vector3 dir = FaceDirection(face, x, y);
                        float t = Mathf.Clamp01(dir.y * 0.5f + 0.5f);

                        Color c = Color.Lerp(palette.Horizon, palette.Zenith, Mathf.SmoothStep(0.35f, 0.95f, t));

                        // Warm band hugging the horizon on side faces.
                        if (palette.WarmBand > 0f && Math.Abs(dir.y) < 0.25f)
                        {
                            float band = Mathf.Clamp01(1f - Math.Abs(dir.y) / 0.25f);
                            c = Color.Lerp(c, new Color(0.95f, 0.62f, 0.36f, 1f), band * palette.WarmBand);
                        }

                        facePixels[y * FaceSize + x] = c;
                    }
                }

                cubemap.SetPixels(facePixels, (CubeMapFace)face);
            }

            cubemap.Apply(false, true);
            Cache[palette.Name] = cubemap;
            return cubemap;
        }

        /// <summary>
        /// Own mapping of cube-face UV to a world direction, enough for smooth
        /// vertical gradients (exact cube mapping is not required here).
        /// </summary>
        private static Vector3 FaceDirection(int face, int x, int y)
        {
            float u = (x / (FaceSize - 1f)) * 2f - 1f;
            float v = (y / (FaceSize - 1f)) * 2f - 1f;

            switch (face)
            {
                case 0: return new Vector3(1f, v, -u);
                case 1: return new Vector3(-1f, v, u);
                case 2: return new Vector3(u, 1f, -v);
                case 3: return new Vector3(u, -1f, v);
                case 4: return new Vector3(u, v, 1f);
                default: return new Vector3(-u, v, -1f);
            }
        }
    }
}
