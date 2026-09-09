using System;
using System.Collections.Generic;
using System.Globalization;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace SceneFX.UI
{
    /// <summary>A compact native panel inspired by Relight row layout and Arrebol flat styling.</summary>
    public sealed class PanelView : IDisposable
    {
        public UIPanel Root { get; private set; }
        private readonly List<UIScrollablePanel> _pages = new List<UIScrollablePanel>();
        private readonly List<UIButton> _tabs = new List<UIButton>();
        private readonly List<Action> _refresh = new List<Action>();
        private readonly List<Action<float>> _resize = new List<Action<float>>();
        private readonly Func<string> _statusText;
        private readonly Func<string> _modeText;
        private readonly UILabel _status;
        private readonly UILabel _title;
        private readonly UIButton _vanilla, _optimized, _undoButton, _close;
        private Func<string> _capture;
        private Func<string, bool> _apply;
        private string _undo;
        private bool _refreshing;
        private readonly List<Action> _unsubscribe = new List<Action>();
        private int _selected;
        private string _error = string.Empty;

        private static readonly Color32 TitleColor = new Color32(230, 237, 239, 255);
        private static readonly Color32 AccentColor = new Color32(31, 168, 224, 255);
        private static readonly Color32 DimTextColor = new Color32(156, 175, 182, 255);

        public PanelView(string title, UIComponent parent, float width, float height,
            Action vanilla, Action optimized, Func<string> status, Func<string> mode = null)
        {
            bool embedded = parent != null;
            Root = parent != null ? parent.AddUIComponent<UIPanel>()
                : UIView.GetAView().AddUIComponent(typeof(UIPanel)) as UIPanel;
            Root.name = title + "Panel";
            Root.backgroundSprite = "MenuPanel";
            Root.clipChildren = true;
            _statusText = status;
            _modeText = mode;
            _title = Root.AddUIComponent<UILabel>();
            _title.text = title;
            _title.textScale = 1.0f;
            _title.textColor = TitleColor;
            _title.relativePosition = new Vector3(12f, 8f);
            _title.autoSize = false;
            _title.height = 24f;
            _close = Button(Root, "✕", () => Root.isVisible = false);
            _close.isVisible = !embedded;
            _close.size = new Vector2(24f, 22f);
            _close.textScale = 0.85f;
            if (!embedded)
            {
                var drag = Root.AddUIComponent<UIDragHandle>();
                drag.target = Root;
                drag.relativePosition = Vector3.zero;
                _resize.Add(w => drag.size = new Vector2(w - 30f, 32f));
            }
            _vanilla = Button(Root, "Vanilla", vanilla);
            _optimized = Button(Root, "Optimized", optimized);
            _vanilla.tooltip = UiText.Get("Revert to base game visual settings (no mod alterations)");
            _optimized.tooltip = UiText.Get("Apply the author's recommended visual preset");
            var ownType = typeof(PanelView).Assembly.GetType("SceneFX.SceneFXMod");
            var readMethod = ownType.GetMethod("ExportSuiteSection");
            var applyMethod = ownType.GetMethod("ApplySuiteSection", new[] { typeof(string) });
            _capture = () => (string)readMethod.Invoke(null, null);
            _apply = xml => (bool)applyMethod.Invoke(null, new object[] { xml });
            _undoButton = Button(Root, "Undo", () => {
                if (_undo != null && !_apply(_undo)) throw new InvalidOperationException("Could not restore previous settings");
                _undo = null;
            });
            _undoButton.tooltip = UiText.Get("Undo the last change made during this session");
            _status = Root.AddUIComponent<UILabel>();
            _status.textScale = 0.76f;
            _status.textColor = DimTextColor;
            _status.autoSize = false;
            // Dos renglones: en uno solo se cortaba a media frase justo cuando mas importa,
            // que es cuando explica por que un ajuste no ha quedado aplicado.
            _status.wordWrap = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (string module in new[] { "SceneFX", "LumenFX", "AtmosphereFX", "ClassicLightFX" })
                {
                    var type = assembly.GetType(module + "." + module + "Mod", false);
                    var changed = type == null ? null : type.GetEvent("StateChanged");
                    if (changed == null) continue;
                    Action handler = () => { if (Root != null && Root.isVisible) Refresh(); };
                    changed.AddEventHandler(null, handler);
                    _unsubscribe.Add(() => changed.RemoveEventHandler(null, handler));
                }
            SetSize(width, height);
        }

        public UIScrollablePanel AddPage(string label)
        {
            int index = _pages.Count;
            var page = Root.AddUIComponent<UIScrollablePanel>();
            page.clipChildren = true;
            page.autoLayout = true;
            page.autoLayoutDirection = LayoutDirection.Vertical;
            page.autoLayoutPadding = new RectOffset(0, 0, 0, 4);
            page.scrollWheelDirection = UIOrientation.Vertical;
            page.builtinKeyNavigation = true;
            _pages.Add(page);
            _tabs.Add(Button(Root, UiText.Get(label), () => Select(index)));
            SetSize(Root.width, Root.height);
            Select(_selected);
            return page;
        }

        private void Select(int index)
        {
            _selected = index;
            for (int i = 0; i < _pages.Count; i++)
            {
                _pages[i].isVisible = i == index;
                _tabs[i].normalBgSprite = i == index ? "ButtonMenuFocused" : "ButtonMenu";
            }
        }

        public void SetSize(float width, float height)
        {
            width = Mathf.Max(320f, width);
            height = Mathf.Max(300f, height);
            Root.size = new Vector2(width, height);
            _title.width = width - 40f;
            _close.relativePosition = new Vector3(width - 28f, 6f);

            float btnW = (width - 24f) / 3f;
            _vanilla.size = _optimized.size = _undoButton.size = new Vector2(btnW, 26f);
            _vanilla.relativePosition = new Vector3(8f, 34f);
            _optimized.relativePosition = new Vector3(10f + btnW, 34f);
            _undoButton.relativePosition = new Vector3(12f + 2f * btnW, 34f);
            _undoButton.isEnabled = _undo != null;

            float tabWidth = _pages.Count == 0 ? 0f : (width - 16f) / _pages.Count;
            for (int i = 0; i < _pages.Count; i++)
            {
                _tabs[i].size = new Vector2(tabWidth - 2f, 26f);
                _tabs[i].relativePosition = new Vector3(8f + i * tabWidth, 64f);
                _pages[i].size = new Vector2(width - 16f, height - 94f - 42f);
                _pages[i].relativePosition = new Vector3(8f, 94f);
            }

            _status.size = new Vector2(width - 20f, 36f);
            _status.relativePosition = new Vector3(10f, height - 39f);

            foreach (var resize in _resize) resize(width - 26f);
        }

        private UIPanel Row(UIComponent page, float height)
        {
            var row = page.AddUIComponent<UIPanel>();
            row.height = height;
            row.width = Root.width - 26f;
            _resize.Add(w => row.width = w);
            return row;
        }

        public void Heading(UIComponent page, string text)
        {
            var row = Row(page, 22f);
            var label = row.AddUIComponent<UILabel>();
            label.text = UiText.Get(text);
            label.textScale = 0.84f;
            label.textColor = AccentColor;
            label.relativePosition = new Vector3(4f, 3f);
            label.autoSize = true;
        }

        public void Action(UIComponent page, string text, Action action)
        {
            var row = Row(page, 28f);
            var button = Button(row, text, action);
            button.height = 26f;
            button.relativePosition = new Vector3(4f, 1f);
            button.width = row.width - 8f;
            _resize.Add(w => {
                button.width = w - 8f;
                button.relativePosition = new Vector3(4f, 1f);
            });
        }

        private UIButton Button(UIComponent parent, string text, Action action)
        {
            var button = parent.AddUIComponent<UIButton>();
            button.text = UiText.Get(text);
            button.textScale = 0.82f;
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.eventClicked += (c, p) => Run(action);
            return button;
        }

        public void Check(UIComponent page, string label, Func<bool> read, Action<bool> write)
        {
            var row = Row(page, 24f);
            var box = (UICheckBox)new UIHelper(row).AddCheckbox(UiText.Get(label), read(), value =>
            {
                if (!_refreshing) Run(() => write(value));
            });
            box.relativePosition = new Vector3(4f, 1f);
            box.height = 22f;
            box.label.autoSize = false;
            box.label.wordWrap = false;
            box.label.height = 22f;
            box.label.textScale = 0.82f;
            _resize.Add(w => {
                box.width = w - 8f;
                box.label.width = w - 36f;
            });
            _refresh.Add(() => box.isChecked = read());
        }

        public void Number(UIComponent page, string label, Func<float> read, Action<float> write,
            float min, float max, float step, bool log = false, Func<bool> enabled = null, float? reset = null)
        {
            var row = Row(page, 26f);
            float initial = reset ?? read();

            var title = row.AddUIComponent<UILabel>();
            title.text = UiText.Get(label);
            title.tooltip = UiText.Get(label);
            title.textScale = 0.80f;
            title.autoSize = false;
            title.wordWrap = false;
            title.height = 22f;
            title.width = LabelWidth(Root.width - 26f) - 6f;
            title.relativePosition = new Vector3(2f, 2f);

            var slider = row.AddUIComponent<UISlider>();
            slider.height = 16f;
            slider.relativePosition = new Vector3(LabelWidth(Root.width - 26f), 5f);
            slider.builtinKeyNavigation = true;

            var track = slider.AddUIComponent<UISlicedSprite>();
            track.spriteName = "ScrollbarTrack";
            track.height = 8f;
            track.relativePosition = new Vector3(0f, 4f);

            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.spriteName = "ScrollbarThumb";
            thumb.size = new Vector2(10f, 16f);
            slider.thumbObject = thumb;

            double scale = Math.Max(step, 0.0000001f);
            double span = Math.Log(1d + (max - min) / scale);
            Func<float, float> toValue = t => log ? (float)(min + scale * (Math.Exp(t * span) - 1d)) : t;
            Func<float, float> toSlider = v => log ? (float)(Math.Log(1d + Math.Max(0d, v - min) / scale) / span) : v;
            slider.minValue = log ? 0f : min;
            slider.maxValue = log ? 1f : max;
            slider.stepSize = log ? 0.001f : step;

            var field = row.AddUIComponent<UITextField>();
            field.normalBgSprite = "TextFieldPanel";
            field.focusedBgSprite = "TextFieldPanelHovered";
            field.textScale = 0.80f;
            field.height = 22f;
            field.width = 54f;
            field.padding = new RectOffset(2, 2, 4, 2);
            field.builtinKeyNavigation = true;

            var resetButton = Button(row, "R", () => write(initial));
            resetButton.tooltip = reset.HasValue ? UiText.Get("Reset this setting") : UiText.Get("Restore value at panel opening");
            resetButton.size = new Vector2(20f, 22f);
            resetButton.textScale = 0.75f;

            _resize.Add(w =>
            {
                float labelWidth = LabelWidth(w);
                title.width = labelWidth - 6f;
                float sliderWidth = Mathf.Max(50f, w - labelWidth - 82f);
                slider.relativePosition = new Vector3(labelWidth, 5f);
                slider.width = sliderWidth;
                track.width = sliderWidth;
                field.relativePosition = new Vector3(w - 78f, 2f);
                resetButton.relativePosition = new Vector3(w - 22f, 2f);
            });

            slider.eventValueChanged += (c, value) => { if (!_refreshing) Run(() => write(Mathf.Clamp(toValue(value), min, max))); };
            field.eventTextSubmitted += (c, text) =>
            {
                if (_refreshing) return;
                Run(() =>
                {
                    float value;
                    if (!float.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                        || float.IsNaN(value) || float.IsInfinity(value) || value < min || value > max)
                        throw new ArgumentException("Enter a number between " + min + " and " + max + ".");
                    write(value);
                });
            };

            _refresh.Add(() =>
            {
                bool active = enabled == null || enabled();
                field.isEnabled = slider.isEnabled = resetButton.isEnabled = active;
                row.tooltip = active ? string.Empty : UiText.Get("Controlled by the selected mode; switch to Manual to edit");
                float value = read();
                slider.value = toSlider(value);
                if (!field.containsFocus) field.text = FormatNumber(value, step);
            });
        }

        private static string FormatNumber(float value, float step)
        {
            if (step >= 1f) return value.ToString("0", CultureInfo.InvariantCulture);
            if (step >= 0.1f) return value.ToString("0.#", CultureInfo.InvariantCulture);
            if (step >= 0.01f) return value.ToString("0.##", CultureInfo.InvariantCulture);
            if (step >= 0.001f) return value.ToString("0.###", CultureInfo.InvariantCulture);
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        public void OptionalNumber(UIComponent page, string label, Func<float> read, Action<float> write,
            float min, float max, float step, bool log = false)
        {
            float manual = read() >= 0f ? read() : min;
            Choice(page, label + " mode", () => new[] { "Game", "Manual" }, () => read() < 0f ? 0 : 1,
                mode => { if (read() >= 0f) manual = read(); write(mode == 0 ? -1f : manual); });
            Number(page, label, () => read() < 0f ? manual : read(), v => { manual = v; write(v); }, min, max, step,
                log, () => read() >= 0f, min);
        }

        public void Info(UIComponent page, Func<string> text)
        {
            var row = Row(page, 28f);
            var label = row.AddUIComponent<UILabel>();
            label.autoSize = false;
            label.wordWrap = true;
            label.textScale = 0.75f;
            label.textColor = DimTextColor;
            label.relativePosition = new Vector3(4f, 2f);
            label.height = 24f;
            _resize.Add(w => label.width = w - 8f);
            _refresh.Add(() => label.text = UiText.Get(text()));
        }

        /// <summary>Una fila con etiqueta y desplegable.</summary>
        /// <remarks>
        /// <b>Por que no usa UIHelper.</b> Su AddDropdown clona una plantilla del menu de
        /// opciones y devuelve el control dentro de un contenedor con su propia disposicion.
        /// Dentro de estas filas salia en blanco: el hueco se reservaba y no se dibujaba nada.
        /// Las casillas por UIHelper si funcionan, asi que el problema es esa plantilla y no
        /// el ayudante. Aqui se arma igual que el deslizador, con las piezas puestas a mano,
        /// que es el camino que ya se ve funcionando en el resto del panel.
        /// </remarks>
        public void Choice(UIComponent page, string label, Func<string[]> items, Func<int> selected, Action<int> write)
        {
            var row = Row(page, 26f);

            var title = row.AddUIComponent<UILabel>();
            title.text = UiText.Get(label);
            title.tooltip = UiText.Get(label);
            title.textScale = 0.80f;
            title.autoSize = false;
            title.wordWrap = false;
            title.height = 22f;
            title.relativePosition = new Vector3(2f, 4f);

            var dropdown = row.AddUIComponent<UIDropDown>();
            dropdown.height = 22f;
            dropdown.itemHeight = 20;
            dropdown.itemPadding = new RectOffset(6, 6, 3, 3);
            dropdown.textFieldPadding = new RectOffset(8, 6, 4, 0);
            dropdown.textScale = 0.78f;
            dropdown.listHeight = 180;
            dropdown.normalBgSprite = "ButtonMenu";
            dropdown.hoveredBgSprite = "ButtonMenuHovered";
            dropdown.focusedBgSprite = "ButtonMenu";
            dropdown.disabledBgSprite = "ButtonMenuDisabled";
            dropdown.listBackground = "GenericPanelLight";
            dropdown.itemHover = "ListItemHover";
            dropdown.itemHighlight = "ListItemHighlight";
            dropdown.popupColor = new Color32(45, 52, 61, 255);
            dropdown.popupTextColor = new Color32(220, 226, 232, 255);
            dropdown.zOrder = 1;
            dropdown.verticalAlignment = UIVerticalAlignment.Middle;
            dropdown.horizontalAlignment = UIHorizontalAlignment.Left;
            dropdown.items = UiText.Items(items());
            dropdown.selectedIndex = Mathf.Clamp(selected(), 0, Mathf.Max(0, dropdown.items.Length - 1));

            // Sin boton disparador el desplegable no se abre al pulsarlo.
            var trigger = dropdown.AddUIComponent<UIButton>();
            trigger.text = string.Empty;
            trigger.relativePosition = Vector3.zero;
            dropdown.triggerButton = trigger;

            _resize.Add(w =>
            {
                float labelWidth = LabelWidth(w);
                title.width = labelWidth - 6f;
                dropdown.relativePosition = new Vector3(labelWidth, 2f);
                dropdown.width = Mathf.Max(60f, w - labelWidth - 4f);
                dropdown.listWidth = (int)dropdown.width;
                trigger.size = dropdown.size;
            });

            dropdown.eventSelectedIndexChanged += (c, value) => { if (!_refreshing) Run(() => write(value)); };
            _refresh.Add(() =>
            {
                dropdown.items = UiText.Items(items());
                dropdown.selectedIndex = Mathf.Clamp(selected(), 0, Mathf.Max(0, dropdown.items.Length - 1));
            });
        }

        /// <summary>
        /// Ancho de la columna de etiquetas, proporcional a la ventana.
        /// </summary>
        /// <remarks>
        /// Era fijo en 125 px y los nombres largos —«Sun power (0 = map)», «Sky red
        /// wavelength (0 = map)»— salian cortados a media palabra. Con la proporcion, una
        /// ventana mas ancha da mas sitio a la etiqueta en vez de solo al deslizador.
        /// </remarks>
        private static float LabelWidth(float rowWidth)
        {
            return Mathf.Clamp(rowWidth * 0.44f, 120f, 210f);
        }

        public void Text(UIComponent page, string label, Func<string> read, Action<string> write)
        {
            var row = Row(page, 50f);
            var field = (UITextField)new UIHelper(row).AddTextfield(UiText.Get(label), read(), value => { if (!_refreshing) write(value); }, null);
            field.size = new Vector2(row.width - 8f, 24f);
            field.relativePosition = new Vector3(4f, 20f);
            _resize.Add(w => {
                field.width = w - 8f;
                if (field.parent != null) field.parent.width = w;
            });
            _refresh.Add(() => { if (!field.containsFocus) field.text = read(); });
        }

        private void Run(Action action)
        {
            try
            {
                // El aviso de cambio externo se quedaba pegado en la barra de estado para
                // siempre, tapando la confirmacion de lo que acabas de hacer. La barra dice
                // que ha pasado en esta accion, asi que se limpia al empezarla.
                Infrastructure.PropertyLedger.LastWarning = string.Empty;
                Infrastructure.FxStorage.LastNote = string.Empty;
                string before = _capture == null ? null : _capture();
                action();
                if (_capture != null && before != _capture())
                {
                    _undo = before;
                    _undoButton.isEnabled = true;
                }
                _error = string.Empty;
            }
            catch (Exception e)
            {
                _error = e.Message;
                Debug.LogException(e);
            }
            Refresh();
        }

        public void Refresh()
        {
            if (Root == null) return;
            _refreshing = true;
            try
            {
                foreach (var refresh in _refresh) refresh();

                if (_modeText != null)
                {
                    string m = _modeText();
                    bool isVanilla = string.Equals(m, "VANILLA", StringComparison.OrdinalIgnoreCase);
                    bool isOptimized = string.Equals(m, "OPTIMIZED", StringComparison.OrdinalIgnoreCase);

                    _vanilla.normalBgSprite = isVanilla ? "ButtonMenuFocused" : "ButtonMenu";
                    _vanilla.textColor = isVanilla ? AccentColor : TitleColor;
                    _vanilla.text = isVanilla ? "Vanilla ✓" : "Vanilla";

                    _optimized.normalBgSprite = isOptimized ? "ButtonMenuFocused" : "ButtonMenu";
                    _optimized.textColor = isOptimized ? AccentColor : TitleColor;
                    _optimized.text = isOptimized ? "Optimized ✓" : "Optimized";
                }

                _status.text = string.IsNullOrEmpty(_error) ? _statusText() : ("Error: " + _error);
                _status.tooltip = _status.text;
                _undoButton.isEnabled = _undo != null;
                SetSize(Root.width, Root.height);
            }
            finally { _refreshing = false; }
        }

        public void Dispose()
        {
            foreach (var unsubscribe in _unsubscribe) unsubscribe();
            _unsubscribe.Clear();
            if (Root != null) UnityEngine.Object.Destroy(Root.gameObject);
            Root = null;
            _refresh.Clear();
            _resize.Clear();
        }
    }
}
