namespace SceneFX.UI
{
    using System;
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    /// Registers the mod's button in the Unified UI tray when that mod is
    /// installed, through its public helper discovered by reflection. The
    /// tray is optional: the mod keeps its own hotkeys (F10 native, F11 IMGUI) as fallback.
    /// </summary>
    internal static class UuiButton
    {
        private static object _button;

        internal static bool Register(string title, string tooltip, Texture2D icon, Action<bool> onToggle)
        {
            if (_button != null)
            {
                return true;
            }

            try
            {
                Type helpers = FindType("UnifiedUI.Helpers.UUIHelpers");
                if (helpers == null)
                {
                    return false;
                }

                MethodInfo register = null;
                foreach (MethodInfo candidate in helpers.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (candidate.Name != "RegisterCustomButton")
                    {
                        continue;
                    }

                    ParameterInfo[] p = candidate.GetParameters();
                    if (p.Length >= 5 && p[0].ParameterType == typeof(string) && p[3].ParameterType == typeof(Texture2D))
                    {
                        register = candidate;
                        break;
                    }
                }

                if (register == null)
                {
                    return false;
                }

                ParameterInfo[] wanted = register.GetParameters();
                object[] args = new object[wanted.Length];
                args[0] = title;
                args[1] = null;
                args[2] = tooltip;
                args[3] = icon;
                args[4] = onToggle;
                for (int i = 5; i < wanted.Length; i++)
                {
                    args[i] = wanted[i].DefaultValue == DBNull.Value ? null : wanted[i].DefaultValue;
                }

                _button = register.Invoke(null, args);
                return _button != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SceneFX] Unified UI registration failed: " + e.Message);
                return false;
            }
        }

        internal static void Unregister()
        {
            try
            {
                if (_button == null)
                {
                    return;
                }

                MethodInfo destroy = _button.GetType().GetMethod("Destroy") ?? _button.GetType().GetMethod("Dispose");
                if (destroy != null)
                {
                    destroy.Invoke(_button, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SceneFX] UUI unregister: " + e.Message);
            }
            finally
            {
                _button = null;
            }
        }

        private static Type FindType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type found = assembly.GetType(name, false);
                    if (found != null)
                    {
                        return found;
                    }
                }
                catch
                {
                    // Uninspectable assemblies are not a reason to stop.
                }
            }

            return null;
        }
    }
}
