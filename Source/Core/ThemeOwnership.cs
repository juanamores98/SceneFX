using System;
using System.Reflection;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// Cede los campos de atmósfera cuando hay un gestor de temas que los administra.
    /// </summary>
    /// <remarks>
    /// <b>Por qué existe.</b> Theme Mixer administra la atmósfera del mapa: escribe
    /// <c>m_SkyTint</c>, <c>m_Latitude</c>, <c>m_Longitude</c>, <c>m_RayleighScattering</c>,
    /// <c>m_MieScattering</c> y <c>m_Exposure</c>, y además los vuelve a leer para guardar un
    /// tema. Un mod que los pise no solo cambia lo que se ve: deja valores ajenos dentro del
    /// tema que el usuario guarde después.
    ///
    /// <b>Cómo se midió.</b> Con Theme Mixer activo, el lab registró que este mod llevaba
    /// la exposición y la posición solar a valores propios, encima de las del tema.
    ///
    /// <b>Por qué ceder y no negociar.</b> Es la misma estrategia que traían los mods clásicos
    /// a los que esta suite sustituye: si otro mod administra una propiedad y se le considera
    /// prioritario, se deja de escribirla en vez de turnarse. Turnarse produce parpadeo y
    /// resultados que dependen del orden de carga.
    /// </remarks>
    internal static class ThemeOwnership
    {
        private const string ThemeManagerAssembly = "ThemeMixer";

        private static bool _checked;
        private static bool _present;

        /// <summary>Cierto si hay un gestor de temas que administra la atmósfera.</summary>
        internal static bool AtmosphereIsManaged
        {
            get
            {
                if (_checked)
                {
                    return _present;
                }

                _checked = true;
                _present = Detect();

                if (_present)
                {
                    Debug.Log("[SceneFX] Theme Mixer administra la atmósfera: se ceden la exposición y la posición solar.");
                }

                return _present;
            }
        }

        /// <summary>Olvida lo detectado. Se llama al descargar, por si cambia la selección.</summary>
        internal static void Forget()
        {
            _checked = false;
            _present = false;
        }

        private static bool Detect()
        {
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(assembly.GetName().Name, ThemeManagerAssembly,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[SceneFX] no se pudo comprobar el gestor de temas -> " + e.Message);
            }

            return false;
        }
    }
}
