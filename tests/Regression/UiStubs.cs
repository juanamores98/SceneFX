// Test-only event/focus doubles; not a simulation of native rendering or layout.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} }
    public class RectOffset { public RectOffset(int a,int b,int c,int d){} }
}
namespace ColossalFramework.Globalization
{
    public class LocaleManager { public static bool exists=true; public static LocaleManager instance=new LocaleManager(); public string language="en"; }
}
namespace ColossalFramework.UI
{
    using UnityEngine;
    public enum LayoutDirection {Vertical}
    public enum UIOrientation {Vertical}
    public class UIComponent : UnityEngine.Object
    {
        public string name,tooltip; public float width,height; public Vector2 size { get{return new Vector2(width,height);} set{width=value.x;height=value.y;} }
        public Vector3 relativePosition; public bool isVisible=true,isEnabled=true,clipChildren,builtinKeyNavigation,containsFocus;
        public UIComponent parent; public GameObject gameObject = new GameObject("UI");
        public List<UIComponent> children = new List<UIComponent>();
        public event Action<UIComponent,Vector3> eventPositionChanged;
        public T AddUIComponent<T>() where T:UIComponent,new(){var item=new T();item.parent=this;children.Add(item);return item;}
        public UIComponent AddUIComponent(Type type){var item=(UIComponent)Activator.CreateInstance(type);item.parent=this;children.Add(item);return item;}
    }
    public class UIPanel:UIComponent {public string backgroundSprite;}
    public class UIScrollablePanel:UIPanel {public bool autoLayout;public LayoutDirection autoLayoutDirection;public RectOffset autoLayoutPadding;public UIOrientation scrollWheelDirection;}
    public class UILabel:UIComponent {public string text;public float textScale;public bool autoSize,wordWrap;public Color32 textColor;}
    public class UIButton:UIComponent {public string text,normalBgSprite,hoveredBgSprite,focusedBgSprite;public float textScale;public event Action<UIComponent,object> eventClicked; public void Click(){if(isEnabled)eventClicked?.Invoke(this,null);}}
    public class UIDragHandle:UIComponent {public UIComponent target;}
    public class UITextField:UIComponent
    {
        public string normalBgSprite,focusedBgSprite,text;public float textScale;public RectOffset padding;
        public event Action<UIComponent,string> eventTextSubmitted;
        public void Submit(string value){text=value;eventTextSubmitted?.Invoke(this,value);}
    }
    public class UISlider:UIComponent
    {
        public float minValue,maxValue,stepSize;private float _value;public UIComponent thumbObject;
        public float value {get{return _value;}set{if(_value!=value){_value=value;eventValueChanged?.Invoke(this,value);}}}
        public event Action<UIComponent,float> eventValueChanged;
    }
    public class UISlicedSprite:UIComponent {public string spriteName;}
    public class UICheckBox:UIComponent {public bool isChecked;public UILabel label=new UILabel();}
    public class UIDropDown:UIComponent {public string[] items;public int selectedIndex,listWidth;}
    public class UIView:UIPanel {public float fixedWidth=1920,fixedHeight=1080;private static UIView view=new UIView();public static UIView GetAView(){return view;}}
    public class UIHelper
    {
        private UIComponent parent;public UIHelper(UIComponent p){parent=p;}
        public object AddCheckbox(string text,bool value,Action<bool> change){var box=parent.AddUIComponent<UICheckBox>();box.label.text=text;box.isChecked=value;return box;}
        public object AddDropdown(string label,string[] items,int selected,Action<int> change){var box=parent.AddUIComponent<UIDropDown>();box.items=items;box.selectedIndex=selected;return box;}
        public object AddTextfield(string label,string value,Action<string> change,Action<string> submit){var field=parent.AddUIComponent<UITextField>();field.text=value;return field;}
    }
}
