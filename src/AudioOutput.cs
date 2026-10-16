using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Air5Control {
 public sealed class AudioSnapshot {
  public string EndpointId="", Name="", Detail="", Error="";
  public bool Ready;
 }
 public sealed class AudioGate {
  string endpoint=""; DateTime since;
  public void Reset() { endpoint=""; since=DateTime.MinValue; }
  public bool Observe(AudioSnapshot state,DateTime now) {
   if(!state.Ready || String.IsNullOrEmpty(state.EndpointId)) { Reset(); return false; }
   if(endpoint!=state.EndpointId) { endpoint=state.EndpointId; since=now; }
   return now-since>=TimeSpan.FromSeconds(8);
  }
 }
 public static class AudioOutput {
  public static bool IsTarget(string name) {
   return name!=null && Regex.IsMatch(name,@"\bAir5\s+Pro\b(?!\s*(?:\+|Plus\b))",RegexOptions.IgnoreCase);
  }
  public static bool IsUsable(string name,uint state) { return state==1 && IsTarget(name); }
  static void Release(object value) { if(value!=null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value); }
  static void Check(int hr) { Marshal.ThrowExceptionForHR(hr); }
  public static AudioSnapshot Read() {
   var result=new AudioSnapshot(); IEnum enumerator=null; ICollection devices=null;
   try {
    enumerator=(IEnum)new Enumerator(); Check(enumerator.EnumAudioEndpoints(0,15,out devices));
    uint count; Check(devices.GetCount(out count)); var details=new List<string>();
    for(uint i=0;i<count;i++) {
     IDevice device=null; IProperties properties=null; Variant value=new Variant();
     try {
      Check(devices.Item(i,out device)); uint state; Check(device.GetState(out state));
      Check(device.OpenPropertyStore(0,out properties));
      var key=new PropertyKey(){Format=new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),Id=14};
      Check(properties.GetValue(ref key,out value));
      string name=value.Type==31?Marshal.PtrToStringUni(value.Pointer):"";
      if(IsTarget(name)) {
       details.Add(name+" [state="+state+"]");
       if(IsUsable(name,state) && !result.Ready) { string id; Check(device.GetId(out id)); result.Ready=true; result.EndpointId=id; result.Name=name; }
      }
     } finally { PropVariantClear(ref value); Release(properties); Release(device); }
    }
    result.Detail=details.Count==0?"Air5 Pro ses çıkışı bulunamadı":String.Join("; ",details.ToArray());
   } catch(Exception ex) { result.Ready=false; result.EndpointId=""; result.Error=ex.Message; result.Detail="Ses çıkışı okunamadı: "+ex.Message; }
   finally { Release(devices); Release(enumerator); }
   return result;
  }
  [DllImport("ole32.dll")] static extern int PropVariantClear(ref Variant value);
  [StructLayout(LayoutKind.Sequential)] struct PropertyKey { public Guid Format; public uint Id; }
  [StructLayout(LayoutKind.Explicit,Size=24)] struct Variant { [FieldOffset(0)] public ushort Type; [FieldOffset(8)] public IntPtr Pointer; }
  [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class Enumerator {}
  [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IEnum {
   [PreserveSig] int EnumAudioEndpoints(int flow,uint states,out ICollection devices);
  }
  [ComImport,Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface ICollection {
   [PreserveSig] int GetCount(out uint count);
   [PreserveSig] int Item(uint index,out IDevice device);
  }
  [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IDevice {
   [PreserveSig] int Activate(ref Guid iid,uint context,IntPtr parameters,[MarshalAs(UnmanagedType.IUnknown)] out object instance);
   [PreserveSig] int OpenPropertyStore(uint access,out IProperties properties);
   [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
   [PreserveSig] int GetState(out uint state);
  }
  [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IProperties {
   [PreserveSig] int GetCount(out uint count);
   [PreserveSig] int GetAt(uint index,out PropertyKey key);
   [PreserveSig] int GetValue(ref PropertyKey key,out Variant value);
  }
 }
}
