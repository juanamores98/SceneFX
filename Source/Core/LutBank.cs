using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ColossalFramework;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// Tablas de color propias del mod: las seis incluidas y las horneadas.
    /// </summary>
    /// <remarks>
    /// <b>Que son.</b> Cada tabla es un cubo 32x32x32 calculado por este codigo a partir de
    /// unos pocos parametros de gradacion. No hay nada extraido de terceros: se computan aqui,
    /// que es lo que permite que el mod sea MIT-0 y que sirva de algo en una instalacion sin
    /// ninguna tabla de otro autor.
    ///
    /// <b>Por que se registran en el gestor del juego.</b> La version anterior las guardaba en
    /// un diccionario aparte, con un campo propio en el estilo y su propia ruta de aplicacion.
    /// Eso significaba dos listas de tablas, dos formas de seleccionarlas y dos sitios donde se
    /// podia romper. Ahora se anaden a <c>ColorCorrectionManager.m_BuiltinLUTs</c> y se llama a
    /// su <c>UpdateItems</c>: aparecen en la misma lista que las demas, se eligen por nombre
    /// como las demas, viajan en los perfiles de suite como las demas y las valida el mismo
    /// codigo. Una tabla horneada se puede ademas seleccionar desde cualquier otro mod que lea
    /// esa lista.
    ///
    /// <b>Que se deshace al descargar.</b> Se repone el array original y se vuelve a llamar a
    /// <c>UpdateItems</c>. Las tablas se anaden siempre al final, asi que los indices que el
    /// juego ya tenia guardados no se mueven mientras el mod esta activo.
    /// </remarks>
    internal static class LutBank
    {
        private const int Size = 32;
        private const string Prefix = "SceneFX ";

        private sealed class Recipe
        {
            internal readonly string Name;
            internal readonly float Exposure, Contrast, Warmth, Saturation, Lift;

            internal Recipe(string name, float exposure, float contrast, float warmth, float saturation, float lift)
            {
                Name = name; Exposure = exposure; Contrast = contrast;
                Warmth = warmth; Saturation = saturation; Lift = lift;
            }
        }

        private static readonly Recipe[] BuiltIns =
        {
            new Recipe("Ordinary", 1.00f, 0.08f, 0.06f, 1.02f, 0.00f),
            new Recipe("Nocturne", 0.82f, 0.15f, -0.25f, 0.90f, 0.01f),
            new Recipe("Sepia", 0.95f, 0.05f, 0.45f, 0.55f, 0.02f),
            new Recipe("Cine", 0.92f, 0.22f, -0.05f, 0.95f, 0.02f),
            new Recipe("Frost", 1.02f, 0.05f, -0.40f, 0.95f, 0.00f),
            new Recipe("Ember", 1.05f, 0.12f, 0.50f, 1.05f, 0.01f),
        };

        private static Texture3DWrapper[] _originalBuiltIns;
        private static readonly List<Texture3DWrapper> Added = new List<Texture3DWrapper>();
        internal static string LastError = string.Empty;

        internal static string BakedFolder
        {
            get { return Path.Combine(StyleStore.StylesFolder, "Baked"); }
        }

        /// <summary>Anade las seis tablas incluidas a la lista del juego. Idempotente.</summary>
        internal static void RegisterBuiltIns()
        {
            foreach (var recipe in BuiltIns)
            {
                Register(Prefix + recipe.Name, Generate(recipe));
            }
        }

        /// <summary>
        /// Hornea lo que la camara esta haciendo ahora mismo en una tabla, y la registra.
        /// </summary>
        /// <remarks>
        /// Se lee el componente de tono vivo, no los campos del estilo. El tono lo escribe
        /// LumenFX desde que hay un dueño por propiedad, asi que hornear los campos de este mod
        /// congelaria unos numeros que ya no son los que se ven. Lo que la camara hace es la
        /// unica fuente honesta, la escriba quien la escriba.
        /// </remarks>
        internal static string BakeCurrentLook(string name, out string pngPath)
        {
            pngPath = null;
            LastError = string.Empty;
            if (string.IsNullOrEmpty(name)) name = "Baked " + DateTime.Now.ToString("yyyyMMdd-HHmmss");

            float gamma = 2.2f, boost = 1f, a = 0.5f, b = 0.26f, c = 0.1f, d = 0.72f, e = 0.01f, f = 0.24f, w = 11f;
            var camera = GameObject.Find("Main Camera");
            var tone = camera == null ? null : camera.GetComponent<ToneMapping>();
            if (tone != null)
            {
                gamma = tone.m_ToneMappingGamma;
                boost = tone.m_ToneMappingBoostFactor;
                a = tone.m_ToneMappingParamsFilmic.A; b = tone.m_ToneMappingParamsFilmic.B;
                c = tone.m_ToneMappingParamsFilmic.C; d = tone.m_ToneMappingParamsFilmic.D;
                e = tone.m_ToneMappingParamsFilmic.E; f = tone.m_ToneMappingParamsFilmic.F;
                w = tone.m_ToneMappingParamsFilmic.W;
            }

            float white = Filmic(w, a, b, c, d, e, f);
            if (white <= 0.0001f) white = 1f;
            float whiteScale = 1f / white;
            gamma = Mathf.Clamp(gamma, 0.1f, 8f);

            var texture = NewTexture(Prefix + name);
            var pixels = new Color[Size * Size * Size];
            int index = 0;
            for (int bi = 0; bi < Size; bi++)
                for (int gi = 0; gi < Size; gi++)
                    for (int ri = 0; ri < Size; ri++)
                    {
                        var col = new Color(ri / (Size - 1f), gi / (Size - 1f), bi / (Size - 1f), 1f);
                        col.r = Shape(col.r, boost, whiteScale, gamma, a, b, c, d, e, f);
                        col.g = Shape(col.g, boost, whiteScale, gamma, a, b, c, d, e, f);
                        col.b = Shape(col.b, boost, whiteScale, gamma, a, b, c, d, e, f);
                        pixels[index++] = col;
                    }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            string full = Prefix + name;
            Register(full, texture);
            pngPath = WriteStrip(name, pixels);
            return full;
        }

        /// <summary>Deja la lista del juego como estaba.</summary>
        internal static void Unregister()
        {
            var manager = ColorCorrectionManager.instance;
            if (manager != null && _originalBuiltIns != null)
            {
                manager.m_BuiltinLUTs = _originalBuiltIns;
                UpdateItems(manager);
            }

            foreach (var wrapper in Added)
            {
                if (wrapper == null) continue;
                if (wrapper.texture != null) UnityEngine.Object.Destroy(wrapper.texture);
                UnityEngine.Object.Destroy(wrapper);
            }

            Added.Clear();
            _originalBuiltIns = null;
        }

        private static void Register(string name, Texture3D texture)
        {
            var manager = ColorCorrectionManager.instance;
            if (manager == null || texture == null) return;
            if (manager.m_BuiltinLUTs == null) manager.m_BuiltinLUTs = new Texture3DWrapper[0];
            if (_originalBuiltIns == null) _originalBuiltIns = manager.m_BuiltinLUTs;

            foreach (var existing in manager.m_BuiltinLUTs)
                if (existing != null && existing.name == name)
                {
                    UnityEngine.Object.Destroy(texture);
                    return;
                }

            var wrapper = ScriptableObject.CreateInstance<Texture3DWrapper>();
            wrapper.name = name;
            wrapper.texture = texture;

            var grown = new Texture3DWrapper[manager.m_BuiltinLUTs.Length + 1];
            Array.Copy(manager.m_BuiltinLUTs, grown, manager.m_BuiltinLUTs.Length);
            grown[grown.Length - 1] = wrapper;
            manager.m_BuiltinLUTs = grown;
            Added.Add(wrapper);
            Localize(name);
            UpdateItems(manager);
        }

        /// <summary>Da nombre a la tabla en el idioma del juego.</summary>
        /// <remarks>
        /// El juego traduce el nombre de cada tabla incluida buscando la clave
        /// <c>BUILTIN_COLORCORRECTION</c>. Sin registrarla, cada vez que se dibuja el
        /// desplegable escribe una linea de error por tabla en el log —dieciocho por apertura
        /// en la primera medida— y ademas el nombre sale vacio. Se registra el propio nombre
        /// como su traduccion: no hay nada que traducir, pero la clave tiene que existir.
        /// </remarks>
        private static void Localize(string name)
        {
            try
            {
                if (!ColossalFramework.Globalization.LocaleManager.exists) return;
                var field = typeof(ColossalFramework.Globalization.LocaleManager).GetField(
                    "m_Locale", BindingFlags.Instance | BindingFlags.NonPublic);
                var locale = field == null
                    ? null
                    : field.GetValue(ColossalFramework.Globalization.LocaleManager.instance)
                        as ColossalFramework.Globalization.Locale;
                if (locale == null) return;
                var key = new ColossalFramework.Globalization.Locale.Key
                {
                    m_Identifier = "BUILTIN_COLORCORRECTION",
                    m_Key = name,
                    m_Index = 0
                };
                if (!locale.Exists(key)) locale.AddLocalizedString(key, name);
            }
            catch (Exception failure)
            {
                // Un nombre sin traducir no impide usar la tabla: se anota y se sigue.
                Debug.Log("[SceneFX] no se pudo nombrar la tabla " + name + ": " + failure.Message);
            }
        }

        private static void UpdateItems(ColorCorrectionManager manager)
        {
            try
            {
                var method = typeof(ColorCorrectionManager).GetMethod(
                    "UpdateItems", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (method != null) method.Invoke(manager, null);
            }
            catch (Exception failure)
            {
                LastError = "Could not refresh the LUT list: " + failure.Message;
                Debug.LogException(failure);
            }
        }

        private static Texture3D NewTexture(string name)
        {
            // Clamp y bilineal a proposito: sin eso los extremos del rango se envuelven y
            // aparecen franjas de color en las sombras y en las luces altas.
            var texture = new Texture3D(Size, Size, Size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private static Texture3D Generate(Recipe recipe)
        {
            var texture = NewTexture(Prefix + recipe.Name);
            var pixels = new Color[Size * Size * Size];
            int index = 0;
            for (int b = 0; b < Size; b++)
                for (int g = 0; g < Size; g++)
                    for (int r = 0; r < Size; r++)
                    {
                        var c = new Color(r / (Size - 1f), g / (Size - 1f), b / (Size - 1f), 1f);
                        c.r *= recipe.Exposure; c.g *= recipe.Exposure; c.b *= recipe.Exposure;
                        c.r *= 1f + 0.12f * recipe.Warmth;
                        c.b *= 1f - 0.12f * recipe.Warmth;

                        float luma = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                        c.r = Mathf.Lerp(luma, c.r, recipe.Saturation);
                        c.g = Mathf.Lerp(luma, c.g, recipe.Saturation);
                        c.b = Mathf.Lerp(luma, c.b, recipe.Saturation);

                        c.r = ContrastCurve(c.r, recipe.Contrast);
                        c.g = ContrastCurve(c.g, recipe.Contrast);
                        c.b = ContrastCurve(c.b, recipe.Contrast);

                        c.r = Mathf.Lerp(c.r, 1f, recipe.Lift * 0.5f * (1f - c.r));
                        c.g = Mathf.Lerp(c.g, 1f, recipe.Lift * 0.5f * (1f - c.g));
                        c.b = Mathf.Lerp(c.b, 1f, recipe.Lift * 0.5f * (1f - c.b));

                        pixels[index++] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                    }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>
        /// Curva propia: expansion simetrica alrededor del gris medio con un hombro suave,
        /// para que las luces altas rueden en vez de recortarse de golpe.
        /// </summary>
        private static float ContrastCurve(float v, float contrast)
        {
            float expanded = Mathf.Clamp01(0.5f + (Mathf.Clamp01(v) - 0.5f) * (1f + 2f * contrast));
            if (expanded > 0.8f)
            {
                float over = (expanded - 0.8f) / 0.2f;
                expanded = 0.8f + 0.2f * (1f - (1f - over) * (1f - over));
            }

            return expanded;
        }

        private static float Shape(float v, float boost, float whiteScale, float gamma,
            float a, float b, float c, float d, float e, float f)
        {
            float linear = Mathf.Pow(Mathf.Clamp01(v), gamma) * Mathf.Max(0f, boost);
            return Mathf.Clamp01(Mathf.Pow(Filmic(linear, a, b, c, d, e, f) * whiteScale, 1f / gamma));
        }

        /// <summary>Respuesta filmic de Hable, la misma familia de curva que usa el juego.</summary>
        private static float Filmic(float x, float a, float b, float c, float d, float e, float f)
        {
            float denominator = x * (a * x + b) + d * f;
            if (Mathf.Abs(denominator) < 1e-6f) return 0f;
            return (x * (a * x + c * b) + d * e) / denominator - e / f;
        }

        /// <summary>Escribe la tira neutra 1024x32 que otras herramientas saben leer.</summary>
        private static string WriteStrip(string name, Color[] pixels)
        {
            try
            {
                if (!Directory.Exists(BakedFolder)) Directory.CreateDirectory(BakedFolder);
                var strip = new Texture2D(Size * Size, Size, TextureFormat.RGBA32, false);
                var row = new Color[Size * Size * Size];
                for (int b = 0; b < Size; b++)
                    for (int g = 0; g < Size; g++)
                        for (int r = 0; r < Size; r++)
                            row[(Size - 1 - g) * Size * Size + b * Size + r] = pixels[b * Size * Size + g * Size + r];

                strip.SetPixels(row);
                strip.Apply(false, false);
                string path = Path.Combine(BakedFolder, StyleStore.SafeName(name) + ".png");
                File.WriteAllBytes(path, strip.EncodeToPNG());
                UnityEngine.Object.Destroy(strip);
                return path;
            }
            catch (Exception failure)
            {
                LastError = "The table was created but the PNG could not be written: " + failure.Message;
                Debug.LogException(failure);
                return null;
            }
        }
    }
}
