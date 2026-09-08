using System;
using System.Collections.Generic;
using System.Globalization;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace SceneFX.UI
{
    /// <summary>A narrow native panel that can also be mounted inside another mod's UI.</summary>
    public sealed class PanelView : IDisposable
    {
        public UIPanel Root { get; private set; }
        private readonly List<UIScrollablePanel> _pages = new List<UIScrollablePanel>();
        private readonly List<UIButton> _tabs = new List<UIButton>();
        private readonly List<Action> _refresh = new List<Action>();
        private readonly List<Action<float>> _resize = new List<Action<float>>();
        private readonly Func<string> _statusText;
        private readonly UILabel _status;
        private readonly UILabel _title;
        private readonly UIButton _vanilla, _optimized, _close;
        private bool _refreshing;
        private int _selected;
        private string _error = string.Empty;

        public PanelView(string title, UIComponent parent, float width, float height,
            Action vanilla, Action optimized, Func<string> status)
        {
            bool embedded = parent != null;
            Root = parent != null ? parent.AddUIComponent<UIPanel>()
                : UIView.GetAView().AddUIComponent(typeof(UIPanel)) as UIPanel;
            Root.name = title + "Panel";
            Root.backgroundSprite = "MenuPanel";
            Root.clipChildren = true;
            _statusText = status;
            _title = Root.AddUIComponent<UILabel>();
            _title.text = title;
            _title.textScale = 1.05f;
            _title.relativePosition = new Vector3(10f, 8f);
            _title.autoSize = false;
            _title.height = 24f;
            _close = Button(Root, "X", () => Root.isVisible = false);
            _close.isVisible = !embedded;
            _close.size = new Vector2(26f, 24f);
            if (!embedded)
            {
                var drag = Root.AddUIComponent<UIDragHandle>();
                drag.target = Root;
                drag.relativePosition = Vector3.zero;
                _resize.Add(w => drag.size = new Vector2(w - 44f, 32f));
            }
            _vanilla = Button(Root, "VANILLA", vanilla);
            _optimized = Button(Root, "OPTIMIZED", optimized);
            _vanilla.tooltip = "Release this mod's changes and keep that mode across cities.";
            _optimized.tooltip = "Apply this mod's part of Render It Plus / Default.";
            _status = Root.AddUIComponent<UILabel>();
            _status.textScale = 0.75f;
            _status.autoSize = false;
            _status.wordWrap = true;
            SetSize(width, height);
        }

        public UIScrollablePanel AddPage(string label)
        {
            int index = _pages.Count;
            var page = Root.AddUIComponent<UIScrollablePanel>();
            page.clipChildren = true;
            page.autoLayout = true;
            page.autoLayoutDirection = LayoutDirection.Vertical;
            page.autoLayoutPadding = new RectOffset(0, 0, 0, 6);
            page.scrollWheelDirection = UIOrientation.Vertical;
            page.builtinKeyNavigation = true;
            _pages.Add(page);
            _tabs.Add(Button(Root, label, () => Select(index)));
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
            width = Mathf.Max(280f, width);
            height = Mathf.Max(260f, height);
            Root.size = new Vector2(width, height);
            _title.width = width - 50f;
            _close.relativePosition = new Vector3(width - 34f, 6f);
            float half = (width - 24f) / 2f;
            _vanilla.size = _optimized.size = new Vector2(half, 28f);
            _vanilla.relativePosition = new Vector3(8f, 36f);
            _optimized.relativePosition = new Vector3(16f + half, 36f);
            float tabWidth = _pages.Count == 0 ? 0f : (width - 16f) / _pages.Count;
            for (int i = 0; i < _pages.Count; i++)
            {
                _tabs[i].size = new Vector2(tabWidth - 3f, 26f);
                _tabs[i].relativePosition = new Vector3(8f + i * tabWidth, 70f);
                _pages[i].size = new Vector2(width - 16f, height - 150f);
                _pages[i].relativePosition = new Vector3(8f, 102f);
            }
            _status.size = new Vector2(width - 20f, 40f);
            _status.relativePosition = new Vector3(10f, height - 42f);
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
            var row = Row(page, 24f);
            var label = row.AddUIComponent<UILabel>();
            label.text = text;
            label.textScale = 0.86f;
            label.textColor = new Color32(79, 195, 247, 255);
            label.relativePosition = new Vector3(0f, 4f);
        }

        public void Action(UIComponent page, string text, Action action)
        {
            var row = Row(page, 28f);
            var button = Button(row, text, action);
            button.height = 28f;
            _resize.Add(w => button.width = w);
            button.width = row.width;
        }

        private UIButton Button(UIComponent parent, string text, Action action)
        {
            var button = parent.AddUIComponent<UIButton>();
            button.text = text;
            button.textScale = 0.85f;
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.eventClicked += (c, p) => Run(action);
            return button;
        }

        public void Check(UIComponent page, string label, Func<bool> read, Action<bool> write)
        {
            var row = Row(page, 34f);
            var box = (UICheckBox)new UIHelper(row).AddCheckbox(label, read(), value =>
            {
                if (!_refreshing) Run(() => write(value));
            });
            box.relativePosition = Vector3.zero;
            box.label.autoSize = false;
            box.label.wordWrap = true;
            box.label.height = 32f;
            box.label.textScale = 0.84f;
            _resize.Add(w => { box.width = w; box.label.width = w - 28f; });
            _refresh.Add(() => box.isChecked = read());
        }

        public void Number(UIComponent page, string label, Func<float> read, Action<float> write,
            float min, float max, float step)
        {
            var row = Row(page, 70f);
            var title = row.AddUIComponent<UILabel>();
            title.text = label;
            title.textScale = 0.84f;
            title.autoSize = false;
            title.height = 40f;
            title.wordWrap = true;
            var field = row.AddUIComponent<UITextField>();
            field.normalBgSprite = "TextFieldPanel";
            field.focusedBgSprite = "TextFieldPanelHovered";
            field.textScale = 0.85f;
            field.height = 24f;
            field.width = 92f;
            field.padding = new RectOffset(4, 4, 3, 2);
            field.builtinKeyNavigation = true;
            var slider = row.AddUIComponent<UISlider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.height = 18f;
            slider.relativePosition = new Vector3(4f, 48f);
            var track = slider.AddUIComponent<UISlicedSprite>();
            track.spriteName = "ScrollbarTrack";
            track.height = 12f;
            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.spriteName = "ScrollbarThumb";
            thumb.size = new Vector2(12f, 18f);
            slider.thumbObject = thumb;
            _resize.Add(w =>
            {
                title.width = w - 98f;
                field.relativePosition = new Vector3(w - 92f, 0f);
                slider.width = w - 8f;
                track.width = slider.width;
            });
            slider.eventValueChanged += (c, value) => { if (!_refreshing) Run(() => write(value)); };
            field.eventTextSubmitted += (c, text) =>
            {
                if (_refreshing) return;
                Run(() =>
                {
                    float value;
                    if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                        || float.IsNaN(value) || float.IsInfinity(value) || value < min || value > max)
                        throw new ArgumentException("Enter a number between " + min + " and " + max + ".");
                    write(value);
                });
            };
            _refresh.Add(() => { float value = read(); slider.value = value; field.text = value.ToString("0.######", CultureInfo.InvariantCulture); });
        }

        public void Choice(UIComponent page, string label, Func<string[]> items, Func<int> selected, Action<int> write)
        {
            var row = Row(page, 60f);
            var dropdown = (UIDropDown)new UIHelper(row).AddDropdown(label, items(), selected(), value =>
            {
                if (!_refreshing) Run(() => write(value));
            });
            _resize.Add(w => { dropdown.width = w; dropdown.listWidth = (int)w; if (dropdown.parent != null) dropdown.parent.width = w; });
            _refresh.Add(() => { dropdown.items = items(); dropdown.selectedIndex = selected(); });
        }

        public void Text(UIComponent page, string label, Func<string> read, Action<string> write)
        {
            var row = Row(page, 60f);
            var field = (UITextField)new UIHelper(row).AddTextfield(label, read(), value => { if (!_refreshing) write(value); }, null);
            _resize.Add(w => { field.width = w; if (field.parent != null) field.parent.width = w; });
            _refresh.Add(() => field.text = read());
        }

        private void Run(Action action)
        {
            try { action(); _error = string.Empty; }
            catch (Exception e) { _error = e.Message; Debug.LogException(e); }
            Refresh();
        }

        public void Refresh()
        {
            if (Root == null) return;
            _refreshing = true;
            try
            {
                foreach (var refresh in _refresh) refresh();
                _status.text = string.IsNullOrEmpty(_error) ? _statusText() : _error;
                SetSize(Root.width, Root.height);
            }
            finally { _refreshing = false; }
        }

        public void Dispose()
        {
            if (Root != null) UnityEngine.Object.Destroy(Root.gameObject);
            Root = null;
            _refresh.Clear();
            _resize.Clear();
        }
    }
}
