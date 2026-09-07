using System;
using ColossalFramework.UI;
using UnityEngine;

namespace SceneFX.UI
{
    /// <summary>
    /// Tarjeta contenedora colapsable con cabecera estilizada, fondo oscuro suave y borde.
    /// Permite agrupar controles afines en bloques visuales sin desordenar la pantalla.
    /// </summary>
    public sealed class UICard : UIPanel
    {
        private UIPanel _header;
        private UILabel _titleLabel;
        private UIButton _toggleButton;
        private UIPanel _content;
        private bool _isCollapsed;

        public UIPanel Content
        {
            get { return _content; }
        }

        public bool IsCollapsed
        {
            get { return _isCollapsed; }
        }

        public void SetWidth(float value)
        {
            width = value;
            if (_header != null)
            {
                _header.width = value - 16f;
                if (_toggleButton != null)
                {
                    _toggleButton.relativePosition = new Vector3(_header.width - 24f, 2f);
                }
                if (_titleLabel != null)
                {
                    _titleLabel.width = _header.width - 28f;
                }
            }
            if (_content != null)
            {
                _content.width = value - 16f;
            }
        }

        public static UICard Create(UIComponent parent, string name, string title, bool startCollapsed = false)
        {
            UICard card = parent.AddUIComponent<UICard>();
            card.name = name;
            card.width = parent.width - 12f;
            card.autoLayout = true;
            card.autoLayoutDirection = LayoutDirection.Vertical;
            card.autoFitChildrenVertically = true;
            card.backgroundSprite = "GenericPanel";
            card.color = new Color32(24, 26, 33, 235);
            card.padding = new RectOffset(6, 6, 6, 6);

            // Cabecera de la tarjeta
            card._header = card.AddUIComponent<UIPanel>();
            card._header.width = card.width - 12f;
            card._header.height = 26f;
            card._header.autoLayout = false;

            card._titleLabel = card._header.AddUIComponent<UILabel>();
            card._titleLabel.text = title;
            card._titleLabel.textScale = 0.88f;
            card._titleLabel.textColor = new Color32(79, 195, 247, 255); // #4FC3F7
            card._titleLabel.relativePosition = new Vector3(4f, 4f);

            card._toggleButton = card._header.AddUIComponent<UIButton>();
            card._toggleButton.normalBgSprite = "ButtonMenu";
            card._toggleButton.hoveredBgSprite = "ButtonMenuHovered";
            card._toggleButton.width = 22f;
            card._toggleButton.height = 22f;
            card._toggleButton.text = "▼";
            card._toggleButton.textScale = 0.7f;
            card._toggleButton.relativePosition = new Vector3(card._header.width - 24f, 2f);

            // Contenedor de contenido
            card._content = card.AddUIComponent<UIPanel>();
            card._content.width = card.width - 12f;
            card._content.autoLayout = true;
            card._content.autoLayoutDirection = LayoutDirection.Vertical;
            card._content.autoFitChildrenVertically = true;
            card._content.padding = new RectOffset(2, 2, 4, 4);

            card._isCollapsed = startCollapsed;
            card.UpdateCollapsedState();

            // Clic en la cabecera o botón colapsa/expande
            MouseEventHandler flip = (c, p) =>
            {
                card._isCollapsed = !card._isCollapsed;
                card.UpdateCollapsedState();
                p.Use();
            };

            card._toggleButton.eventClick += flip;
            card._header.eventClick += flip;

            return card;
        }

        public void SetTitle(string title)
        {
            if (_titleLabel != null)
            {
                _titleLabel.text = title;
            }
        }

        private void UpdateCollapsedState()
        {
            if (_content != null && _toggleButton != null)
            {
                _content.isVisible = !_isCollapsed;
                _toggleButton.text = _isCollapsed ? "►" : "▼";
            }
        }
    }
}
