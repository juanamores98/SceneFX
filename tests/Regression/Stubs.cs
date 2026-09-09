// Offline doubles: storage, component lookup and scalar state only. No Unity rendering,
// game scheduler, real UI, Harmony patching, Steam access or native texture generation.
using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
 public class Object {
  public static readonly List<Object> Registry = new List<Object>();
  public Object() { Registry.Add(this); }
  public static T FindObjectOfType<T>() where T: Object { return Registry.OfType<T>().FirstOrDefault(); }
  public static void Destroy(Object o) { Registry.Remove(o); }
  public static void DestroyImmediate(Object o) { Destroy(o); }
  public static implicit operator bool(Object o) { return o != null; }
 }
 public class Behaviour: Object {public bool enabled=true;}
 public class MonoBehaviour: Behaviour { public int GetInstanceID() { return GetHashCode(); } }
 public class GameObject: Object {
  public string name; public GameObject(string n) { name=n; }
  public static GameObject Find(string n) { return Registry.OfType<GameObject>().FirstOrDefault(x=>x.name==n); }
  public T AddComponent<T>() where T:new() { return new T(); }
  public T GetComponent<T>() where T:Object { return FindObjectOfType<T>(); }
 }
 public class Texture3D: Object { public string name; }
 public class Texture2D: Object { }
 public struct Color {
  public float r,g,b,a; public Color(float x,float y,float z,float w=1) { r=x;g=y;b=z;a=w; }
  public static Color white => new Color(1,1,1,1);
  public static Color clear => new Color(0,0,0,0);
  public static Color Lerp(Color x,Color y,float t) { return new Color(Mathf.Lerp(x.r,y.r,t),Mathf.Lerp(x.g,y.g,t),Mathf.Lerp(x.b,y.b,t),Mathf.Lerp(x.a,y.a,t)); }
  public static bool operator ==(Color x,Color y) { return x.Equals(y); }
  public static bool operator !=(Color x,Color y) { return !x.Equals(y); }
  public override bool Equals(object o) { return o is Color c && r==c.r && g==c.g && b==c.b && a==c.a; }
  public override int GetHashCode() { return r.GetHashCode(); }
 }
 public struct Color32 {
  public byte r,g,b,a; public Color32(byte x,byte y,byte z,byte w){r=x;g=y;b=z;a=w;}
  public static implicit operator Color(Color32 c){return new Color(c.r/255f,c.g/255f,c.b/255f,c.a/255f);}
 }
 public struct Vector3 {
  public float x,y,z; public Vector3(float a,float b,float c=0){x=a;y=b;z=c;}
  public static Vector3 zero=>new Vector3();
  public static Vector3 Lerp(Vector3 a,Vector3 b,float t){return new Vector3(Mathf.Lerp(a.x,b.x,t),Mathf.Lerp(a.y,b.y,t),Mathf.Lerp(a.z,b.z,t));}
  public static bool operator ==(Vector3 a,Vector3 b){return a.Equals(b);}
  public static bool operator !=(Vector3 a,Vector3 b){return !a.Equals(b);}
  public override bool Equals(object o){return o is Vector3 v&&x==v.x&&y==v.y&&z==v.z;}
  public override int GetHashCode(){return x.GetHashCode();}
 }
 public struct Vector4 {
  public float x,y,z,w; public Vector4(float a,float b,float c=0,float d=0){x=a;y=b;z=c;w=d;}
 }
 public struct GradientColorKey { public Color color; public float time; public GradientColorKey(Color c,float t){color=c;time=t;} }
 public struct GradientAlphaKey { public float alpha,time; public GradientAlphaKey(float a,float t){alpha=a;time=t;} }
 public class Gradient {
  private GradientColorKey[] _colorKeys={new GradientColorKey(new Color(.5f,.5f,.5f),0),new GradientColorKey(new Color(.5f,.5f,.5f),1)};
  private GradientAlphaKey[] _alphaKeys={new GradientAlphaKey(1,0),new GradientAlphaKey(1,1)};
  public GradientColorKey[] colorKeys {get{return (GradientColorKey[])_colorKeys.Clone();}set{_colorKeys=(GradientColorKey[])value.Clone();}}
  public GradientAlphaKey[] alphaKeys {get{return (GradientAlphaKey[])_alphaKeys.Clone();}set{_alphaKeys=(GradientAlphaKey[])value.Clone();}}
  public Color Evaluate(float t){return colorKeys[0].color; } // no visual/gradient assertions
 }
 public enum ShadowQuality { Disable,HardOnly,All }
 public static class QualitySettings { public static ShadowQuality shadows=ShadowQuality.All; }
 public static class Mathf {
  public static float Clamp(float v,float a,float b){return v<a?a:v>b?b:v;}
  public static int Clamp(int v,int a,int b){return v<a?a:v>b?b:v;}
  public static float Clamp01(float v){return Clamp(v,0,1);}
  public static float Repeat(float v,float n){return v-(float)Math.Floor(v/n)*n;}
  public static float Lerp(float a,float b,float t){return a+(b-a)*Clamp01(t);}
  public static float Abs(float v){return Math.Abs(v);}
  public static float Min(float a,float b){return Math.Min(a,b);}
  public static int Max(int a,int b){return Math.Max(a,b);}
  public static int Min(int a,int b){return Math.Min(a,b);}
  public static float Max(float a,float b){return Math.Max(a,b);}
  public static bool Approximately(float a,float b){return Math.Abs(a-b)<.000001f;}
 }
 public static class Time {public static float fixedDeltaTime=.016666667f; public static float realtimeSinceStartup=10,timeScale=1,unscaledDeltaTime=.016f; }
 public static class Debug { public static List<string> Logs=new List<string>(); public static void Log(object o){Logs.Add(o.ToString());} public static void LogWarning(object o){Log(o);} public static void LogException(Exception e){Log(e);} }
 public static class Application { public static void OpenURL(string url) {throw new Exception("External access blocked in audit");} }
 public enum KeyCode { F10,F11,LeftControl,LeftAlt,L }
 public static class Input { public static bool GetKeyDown(KeyCode k){return false;} public static bool GetKey(KeyCode k){return false;} }
 public static class Shader { public static void SetGlobalVector(string name, Vector4 value) {} public static void SetGlobalVector(int name, Vector4 value) {} }
}
namespace ColossalFramework {
 public class ColorCorrectionLut:UnityEngine.Behaviour {}
 public class ToneMapping:UnityEngine.Behaviour {
  public float m_ToneMappingGamma=2.2f,m_ToneMappingBoostFactor=1,m_Luminance=.1f;
  public Filmic m_ToneMappingParamsFilmic=new Filmic();
  public class Filmic {public float A=.5f,B=.25f,C=.1f,D=.7f,E=.01f,F=.25f,W=11.2f;}
 }
 public class Texture3DWrapper {public string name;}
 public static class Singleton<T> where T:new(){public static T instance=new T();}
}
namespace ColossalFramework.IO { public static class DataLocation { public static string localApplicationData=System.IO.Path.Combine(AppContext.BaseDirectory,"isolated-settings-"+Guid.NewGuid().ToString("N")); } }
namespace ColossalFramework.PlatformServices {
 public struct PublishedFileId {}
 public static class PlatformService {public static Workshop workshop=new Workshop();}
 public class Workshop {public PublishedFileId[] GetSubscribedItems(){return new PublishedFileId[0];} public string GetSubscribedItemPath(PublishedFileId id){return null;} }
}
namespace ICities {
 public interface IUserMod{} public enum LoadMode{NewGame,LoadGame} public interface IThreading{}
 public class LoadingExtensionBase {public virtual void OnLevelLoaded(LoadMode m){} public virtual void OnLevelUnloading(){} }
 public class ThreadingExtensionBase {public virtual void OnCreated(IThreading e){} public virtual void OnUpdate(float a,float b){} }
 public class UIHelperBase {
  public UIHelperBase AddGroup(string s){return this;}
  public void AddCheckbox(string s,bool b,Action<bool> a){}
  public void AddButton(string s,Action a){}
 }
}
namespace Harmony { public class HarmonyInstance {public static HarmonyInstance Create(string id){return new HarmonyInstance();} public void PatchAll(System.Reflection.Assembly a){} public void UnpatchAll(string id){} } }
public class DayNightProperties:UnityEngine.Object {
 public static DayNightProperties instance;
 public bool m_Tonemapping=true;
 public float m_Exposure=1,m_SunIntensity=1,m_MoonIntensity=.5f,m_RayleighScattering=1,m_MieScattering=1,m_Latitude=36,m_Longitude=88,m_TimeOfDay=12f;
 public UnityEngine.Gradient m_LightColor=new UnityEngine.Gradient();
 public AmbientColor m_AmbientColor=new AmbientColor();
 public UnityEngine.Color m_SkyTint=new UnityEngine.Color(.5f,.5f,.5f);
 public UnityEngine.Vector3 m_WaveLengths=new UnityEngine.Vector3(680,550,440);
 public float normalizedTimeOfDay=>m_TimeOfDay / 24f;
 public UnityEngine.Color currentLightColor=>new UnityEngine.Color(1,1,1);
 public void Refresh() {}
 public class AmbientColor {private UnityEngine.Gradient m_SkyColor=new UnityEngine.Gradient(),m_EquatorColor=new UnityEngine.Gradient(),m_GroundColor=new UnityEngine.Gradient();}
}
public class FogProperties:UnityEngine.Object {public float m_ColorDecay=.2f,m_FogDensity=.00223f,m_NoiseContribution=1,m_WindSpeed=.001f,m_FogHeight=1000,m_HorizonHeight=800,m_FogStart=194;public bool m_edgeFog=true;}
public class FogEffect:UnityEngine.Object {public bool enabled,m_edgeFog=true,m_UseVolumeFog;public float m_FogHeight=5000,m_3DFogStart,m_3DFogDistance=10,m_edgeFogDistance;}
public class DayNightFogEffect:UnityEngine.Object {public bool enabled=true;}
public class RenderProperties:UnityEngine.Object {public bool m_useVolumeFog=true; public float m_fogHeight=5000,m_volumeFogDensity,m_volumeFogDistance=4800,m_edgeFogDistance; public float m_inscatteringExponent=1.7f,m_inscatteringIntensity=1.72f,m_volumeFogStart;public UnityEngine.Color m_inscatteringColor,m_volumeFogColor;public ColossalFramework.Texture3DWrapper m_ColorCorrectionLUT;}
public class SimulationManager {public static SimulationManager instance=ColossalFramework.Singleton<SimulationManager>.instance; public bool m_isNightTime,m_enableDayNight=true,SimulationPaused; public const float SUNRISE_HOUR=5, SUNSET_HOUR=20; public const uint DAYTIME_FRAMES=65536; public int SelectedSimulationSpeed=1; public float m_currentDayTimeHour=12; public uint m_dayTimeOffsetFrames, m_referenceFrameIndex, m_currentFrameIndex;}
public class WeatherProperties {public bool m_rainIsSnow;}
public class WeatherManager {public static WeatherManager instance=new WeatherManager();public bool m_enableWeather=true;public WeatherProperties m_properties=new WeatherProperties();public float m_currentRain,m_targetRain,m_currentFog,m_targetFog,m_currentCloud,m_targetCloud,m_currentNorthernLights,m_targetNorthernLights,m_currentRainbow,m_targetRainbow,m_groundWetness,m_targetTemperature,m_currentTemperature,m_targetDirection,m_windDirection;}
public class NetManager {public static NetManager instance=new NetManager();public bool m_treatWetAsSnow;}
public class ColorCorrectionManager {public static ColorCorrectionManager instance=new ColorCorrectionManager();public string[] items={"Original","Other","1539181199.Relight2Average"};public int lastSelection; public int currentSelection {set {lastSelection=value;}}public ColossalFramework.Texture3DWrapper[] m_BuiltinLUTs=new ColossalFramework.Texture3DWrapper[0];public void SetLUT(UnityEngine.Texture3D t){} }
namespace AtmosphereFX.Runtime {public class AtmosphereEngine:UnityEngine.MonoBehaviour {public static void OpenWindow(){} } }
namespace AtmosphereFX.Options {internal static class OptionsPanel {internal static void Build(ICities.UIHelperBase h){} } }
namespace AtmosphereFX.UI {public static class UuiButton {public static void Register(string n,string d,UnityEngine.Texture2D t,Action<bool>a){} public static void Unregister(){} } public static class TrayIcon{public static UnityEngine.Texture2D Make(){return null;}} }
namespace LumenFX.Shadows {public static class AdaptiveBias {public static void ClearCache(){} } }
namespace LumenFX.UI {public class TunerWindow {public TunerWindow(LumenFX.Core.LightState s,Action a){} public void Draw(int id){} } }
namespace LumenFX.UI {public static class UuiButton {public static void Register(string n,string d,UnityEngine.Texture2D t,Action<bool>a){} public static void Unregister(){} } public static class TrayIcon{public static UnityEngine.Texture2D Make(){return null;}} }
namespace ClassicLightFX.Core {public static class LutLibrary {public static string GetEnvironment(){return "Europe";} public static ColossalFramework.Texture3DWrapper Synthesize(string n){return new ColossalFramework.Texture3DWrapper{name=n};}} }
namespace SceneFX.Core {
 internal static class SkyMood {internal static void Apply(int i){} internal static void Restore(){} }
 internal static class NativeLut {internal static bool TryGet(string n,out UnityEngine.Texture3D t){t=null;return false;}internal static void ClearRuntimeTextures(){} }
 internal static class LutCompat {internal static bool TryGet(string n,out UnityEngine.Texture3D t){t=null;return false;} }
 internal static class BorderlessMode {internal static void Apply(){} internal static void Restore(){} }
}
namespace SceneFX.UI {
 public class NativePanel {public void Show(){} public void Toggle(){} }
 public class StylePanel {public StylePanel(Action a){} public void Draw(int i){} }
 public static class UuiButton {public static void Register(string n,string d,UnityEngine.Texture2D t,Action<bool>a){} public static void Unregister(){} }
 public static class TrayIcon{public static UnityEngine.Texture2D Make(){return null;}}
}

public class RainParticleProperties: UnityEngine.Object {public bool ForceRainMotionBlur;}
namespace UnityStandardAssets.ImageEffects {public class Bloom:UnityEngine.Behaviour {}}
namespace ClassicLightFX.Options {internal static class OptionsPanel {internal static void Build(ICities.UIHelperBase h) {}}}
namespace ClassicLightFX.Core {public class ClassicEngine:UnityEngine.MonoBehaviour {public static void OpenWindow(){}}}
namespace ClassicLightFX.UI {public static class UuiButton {public static void Register(string n,string d,UnityEngine.Texture2D t,Action<bool>a){} public static void Unregister(){} } public static class TrayIcon{public static UnityEngine.Texture2D Make(){return null;}} }




