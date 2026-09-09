using System;
using System.Collections.Generic;
using ColossalFramework.UI;
partial class Program
{
    static IEnumerable<UIComponent> Descendants(UIComponent root)
    {foreach(var child in root.children){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
    static void UiChecks()
    {
        FreshWorld();
        using(var scene=SceneFX.FxModule.CreatePanel(null))
        using(var lumen=LumenFX.FxModule.CreatePanel(null))
        using(var atmo=AtmosphereFX.FxModule.CreatePanel(null))
        using(var classic=ClassicLightFX.FxModule.CreatePanel(null))
        {
            Check("UI01 all four panels construct and refresh", scene.Root!=null&&lumen.Root!=null&&atmo.Root!=null&&classic.Root!=null,"actual PanelView/FxModule source linked with UI event doubles");
            UITextField field=null;
            foreach(var c in Descendants(lumen.Root))
                if(c is UILabel label && label.text=="Gamma") foreach(var sibling in label.parent.children) if(sibling is UITextField f)field=f;
            field.Submit("2,87");
            Check("UI02 comma input updates exact value", Equal(LumenFX.Runtime.TunerRuntime.CurrentState.Gamma,2.87f),"invariant output XML, localized input");
            field.containsFocus=true;field.text="2,";
            LumenFX.LumenFXMod.ApplySuiteSection("<lumenfx><gamma>3</gamma></lumenfx>");
            Check("UI03 external notification preserves focused text",field.text=="2,","in-progress editing is not destroyed");
            field.containsFocus=false;LumenFX.LumenFXMod.NotifyStateChanged();
            Check("UI04 unfocused field reflects external update",field.text.StartsWith("3"),"panel shows current state");
            foreach(var c in Descendants(lumen.Root)) if(c is UIButton button && (button.text=="Undo" || button.text.Contains("Undo") || button.text.Contains("Deshacer")))button.Click();
            Check("UI05 Undo restores prior module snapshot",Equal(LumenFX.Runtime.TunerRuntime.CurrentState.Gamma,2.2f),"undo the last local edit, not merely the label");
            ColossalFramework.Globalization.LocaleManager.instance.language="es";
            Check("UI06 Spanish labels resolve",AtmosphereFX.UI.UiText.Get("Volume Density")=="Densidad volumétrica","native UI labels use game language");
            ColossalFramework.Globalization.LocaleManager.instance.language="en";
        }
        using(var scene=SceneFX.FxModule.CreatePanel(null))
        {
            SceneFX.FxModule.Release();
            scene.Refresh();
            UIButton vanillaBtn=null, optBtn=null;
            foreach(var c in scene.Root.children)
            {
                if(c is UIButton b)
                {
                    if(b.text.Contains("Vanilla")) vanillaBtn=b;
                    if(b.text.Contains("Optimized")) optBtn=b;
                }
            }
            bool vCheck = vanillaBtn!=null && vanillaBtn.text.Contains("✓") && vanillaBtn.normalBgSprite=="ButtonMenuFocused";
            SceneFX.FxModule.ApplyOptimized();
            scene.Refresh();
            bool oCheck = optBtn!=null && optBtn.text.Contains("✓") && optBtn.normalBgSprite=="ButtonMenuFocused" && !vanillaBtn.text.Contains("✓");
            Check("UI07 active mode button highlighted with checkmark", vCheck && oCheck, "Vanilla and Optimized buttons reflect active configuration");
        }
        LumenFX.LumenFXMod.NotifyStateChanged();
    }
}
