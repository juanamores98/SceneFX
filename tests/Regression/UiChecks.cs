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
            foreach(var c in Descendants(lumen.Root)) if(c is UIButton button && button.text=="Undo")button.Click();
            Check("UI05 Undo restores prior module snapshot",Equal(LumenFX.Runtime.TunerRuntime.CurrentState.Gamma,2.2f),"undo the last local edit, not merely the label");
            ColossalFramework.Globalization.LocaleManager.instance.language="es";
            Check("UI06 Spanish labels resolve",AtmosphereFX.UI.UiText.Get("Volume Density")=="Densidad volumétrica","native UI labels use game language");
            ColossalFramework.Globalization.LocaleManager.instance.language="en";
        }
        LumenFX.LumenFXMod.NotifyStateChanged();
    }
}
