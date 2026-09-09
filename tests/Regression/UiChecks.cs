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

        // El desplegable se construia con UIHelper y salia en blanco: la fila reservaba el
        // hueco y no se dibujaba nada. Ahora se arma a mano, asi que hay que comprobar dos
        // cosas: que existe con geometria util, y que mover la seleccion llega al mod.
        FreshWorld();
        using(var atmo=AtmosphereFX.FxModule.CreatePanel(null))
        {
            UIDropDown scatter=null;
            foreach(var c in Descendants(atmo.Root))
                if(c is UILabel label && label.text=="Scatter colour")
                    foreach(var sibling in label.parent.children) if(sibling is UIDropDown d) scatter=d;

            Check("UI08 a choice row builds a usable dropdown",
                scatter!=null && scatter.items.Length==3 && scatter.width>60f && scatter.triggerButton!=null,
                scatter==null ? "no dropdown next to its label"
                    : "items="+scatter.items.Length+" width="+scatter.width+" trigger="+(scatter.triggerButton!=null));

            AtmosphereFX.Config.ModConfig.ScatterColorMode = 0;
            scatter.Pick(2);
            Check("UI09 changing the selection reaches the mod",
                AtmosphereFX.Config.ModConfig.ScatterColorMode==2,
                "picked index 2, mod now has "+AtmosphereFX.Config.ModConfig.ScatterColorMode);

            UILabel first=null;
            foreach(var c in Descendants(atmo.Root)) if(c is UILabel l && l.text=="Fog start (m)") first=l;
            Check("UI10 the label column leaves room for long names",
                first!=null && first.width>=114f,
                first==null ? "label not found" : "label column = "+first.width+" px (was fixed at 125 minus padding)");
        }
        LumenFX.LumenFXMod.NotifyStateChanged();
    }
}
