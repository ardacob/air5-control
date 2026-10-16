using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Air5Control {
 public static class Startup {
  const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
  const string SettingsKey=@"Software\Air5Control";
  public static string Command(string path) {
   if(String.IsNullOrWhiteSpace(path) || path.Contains("\"")) throw new ArgumentException("Invalid executable path");
   return "\""+path+"\" --tray";
  }
  public static bool Preferred() {
   using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(SettingsKey)) return key==null || Convert.ToInt32(key.GetValue("StartupEnabled",1))!=0;
  }
  public static void Set(bool enabled) {
   using(var key=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKey)) {
    if(enabled) key.SetValue("Air5Control",Command(System.Reflection.Assembly.GetExecutingAssembly().Location));
    else key.DeleteValue("Air5Control",false);
   }
   using(var key=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(SettingsKey)) key.SetValue("StartupEnabled",enabled?1:0,Microsoft.Win32.RegistryValueKind.DWord);
  }
 }
 public sealed class ReconnectSchedule {
  public DateTime NextAttempt { get; private set; }
  public bool Due(DateTime now) { return now>=NextAttempt; }
  public void Failed(DateTime now) { NextAttempt=now.AddSeconds(15); }
  public void Connected() { NextAttempt=DateTime.MinValue; }
 }
 public sealed class Earbuds : IDisposable {
  BluetoothLEDevice device; GattDeviceService service; GattCharacteristic tx, rx;
  readonly ConcurrentQueue<byte[]> packets = new ConcurrentQueue<byte[]>();
  bool handlerAttached, connectionHandlerAttached, ready; volatile bool disconnected;
  public bool Ready { get { return ready && !disconnected; } private set { ready=value; if(value) disconnected=false; } }
  void OnConnectionStatus(BluetoothLEDevice sender,object args) { try { if(Object.ReferenceEquals(device,sender) && sender.ConnectionStatus==BluetoothConnectionStatus.Disconnected) disconnected=true; } catch {} }
  static readonly Guid ServiceId = new Guid("00001100-d102-11e1-9b23-00025b00a5a5");
  public static byte[] GetPacket(int code) {
   if(code!=6 && code!=7 && code!=9 && code!=16 && code!=35) throw new ArgumentException("GET not allowed");
   return new byte[]{0,10,3,(byte)code};
  }
  public static byte[] ModePacket(int mode) {
   if(mode<0 || mode>2) throw new ArgumentException("Mode not allowed");
   return new byte[]{0,10,3,17,(byte)mode};
  }
  public static void Validate(byte[] p) {
   if(p==null || p.Length<4 || p[0]!=0 || p[1]!=10 || p[2]!=3) throw new ArgumentException("Packet blocked");
   if(p.Length==4 && (p[3]==6 || p[3]==7 || p[3]==9 || p[3]==16 || p[3]==35)) return;
   if(p.Length==5 && p[3]==17 && p[4]<=2) return;
   throw new ArgumentException("Packet blocked");
  }
  public static bool Matches(byte[] p,int code) { return p!=null && p.Length>=5 && p[0]==0 && p[1]==10 && p[2]==131 && p[3]==code; }
  public static int? Scalar(byte[] p,int code,int max) {
   return Matches(p,code) && p.Length==6 && p[4]==0 && p[5]<=max ? (int?)p[5] : null;
  }
  static async Task<T> Wait<T>(IAsyncOperation<T> operation) {
   var until=DateTime.UtcNow.AddSeconds(12);
   while(operation.Status==AsyncStatus.Started) { if(DateTime.UtcNow>until) { try { operation.Cancel(); } catch {} throw new TimeoutException("Bluetooth işlemi zaman aşımına uğradı."); } await Task.Delay(40); }
   return operation.GetResults();
  }
  public async Task Connect() {
   Dispose();
   var devices=await Wait(DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector()));
   DeviceInformation target=null;
   foreach(var d in devices) if(String.Equals(d.Name,"SOUNDPEATS Air5 Pro",StringComparison.OrdinalIgnoreCase)) { if(target!=null) throw new Exception("Birden fazla Air5 Pro bulundu."); target=d; }
   if(target==null) throw new Exception("Air5 Pro bulunamadı. Önce Windows Bluetooth ayarlarından eşleştir.");
   device=await Wait(BluetoothLEDevice.FromIdAsync(target.Id));
   if(device==null) throw new Exception("Bluetooth erişimi alınamadı. Kulaklığı yeniden bağlayıp tekrar dene.");
   var services=await Wait(device.GetGattServicesForUuidAsync(ServiceId,BluetoothCacheMode.Uncached));
   if(services.Status!=GattCommunicationStatus.Success || services.Services.Count!=1) throw new Exception("Kontrol servisine ulaşılamadı. PeatsAudio'yu kapatıp tekrar dene.");
   service=services.Services[0];
   var chars=await Wait(service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached));
   if(chars.Status!=GattCommunicationStatus.Success) throw new Exception("Bluetooth kanalları okunamadı.");
   foreach(var c in chars.Characteristics) {
    if(c.Uuid==new Guid("00001101-d102-11e1-9b23-00025b00a5a5")) tx=c;
    if(c.Uuid==new Guid("00001102-d102-11e1-9b23-00025b00a5a5")) rx=c;
   }
   if(tx==null || rx==null) throw new Exception("Kontrol kanalları eksik.");
   rx.ValueChanged+=OnValue; handlerAttached=true;
   var status=await Wait(rx.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify));
   if(status!=GattCommunicationStatus.Success) throw new Exception("Bildirim bağlantısı kurulamadı.");
   await Task.Delay(700); Ready=true;
   device.ConnectionStatusChanged+=OnConnectionStatus; connectionHandlerAttached=true;
   if(device.ConnectionStatus==BluetoothConnectionStatus.Disconnected) { disconnected=true; throw new Exception("Kulaklık bağlantısı kesildi."); }
  }
  void OnValue(GattCharacteristic sender,GattValueChangedEventArgs args) {
   try { using(var reader=DataReader.FromBuffer(args.CharacteristicValue)) { var b=new byte[reader.UnconsumedBufferLength]; reader.ReadBytes(b); packets.Enqueue(b); } } catch {}
  }
  void Drain() { byte[] b; while(packets.TryDequeue(out b)) {} }
  async Task Send(byte[] p) {
   Validate(p); if(!Ready) throw new Exception("Önce kulaklığa bağlan.");
   GattCommunicationStatus status;
   using(var writer=new DataWriter()) { writer.WriteBytes(p); status=await Wait(tx.WriteValueAsync(writer.DetachBuffer(),GattWriteOption.WriteWithResponse)); }
   if(status!=GattCommunicationStatus.Success) throw new Exception("Bluetooth gönderimi başarısız: "+status);
  }
  public async Task<byte[]> Query(int code) {
   for(int i=0;i<2;i++) {
    Drain(); await Send(GetPacket(code)); var until=DateTime.UtcNow.AddMilliseconds(1800);
    while(DateTime.UtcNow<until) {
     byte[] p; while(packets.TryDequeue(out p)) if(Matches(p,code)) return p;
     await Task.Delay(60);
    }
   }
   return null;
  }
  public async Task<bool> SetMode(int mode) {
   Drain(); await Send(ModePacket(mode)); // Never retry a SET.
   await Task.Delay(450);
   var actual=Scalar(await Query(16),16,2);
   return actual.HasValue && actual.Value==mode;
  }
  public void Dispose() {
   Ready=false;
   if(device!=null && connectionHandlerAttached) { try { device.ConnectionStatusChanged-=OnConnectionStatus; } catch {} }
   connectionHandlerAttached=false;
   if(rx!=null && handlerAttached) { try { rx.ValueChanged-=OnValue; } catch {} }
   handlerAttached=false; tx=null; rx=null;
   if(service!=null) { try { service.Dispose(); } catch {} service=null; }
   if(device!=null) { try { device.Dispose(); } catch {} device=null; }
   Drain();
  }
 }

 public sealed class Controller {
  readonly Window window; readonly Earbuds buds=new Earbuds(); readonly DispatcherTimer timer=new DispatcherTimer();
  bool busy, closing, exitRequested; DateTime lastRead=DateTime.MinValue;
  readonly ReconnectSchedule reconnect=new ReconnectSchedule();
  string lastFailure;
  bool connectionConfirmed; int connectionAttempts;
  void ConnectionLog(string kind,string detail) {
   if(testMode) return;
   try {
    string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"connection.log");
    if(File.Exists(path) && new FileInfo(path).Length>262144) File.Copy(path,path+".previous",true);
    if(File.Exists(path) && new FileInfo(path).Length>262144) File.WriteAllText(path,"");
    File.AppendAllText(path,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")+" | "+kind+" | "+detail.Replace('\r',' ').Replace('\n',' ')+Environment.NewLine);
   } catch {}
  }
  void LostConnection(string reason) {
   if(connectionConfirmed) ConnectionLog("DISCONNECTED",reason);
   connectionConfirmed=false;
  }
  Forms.NotifyIcon tray; Forms.ContextMenuStrip trayMenu;
  Forms.ToolStripMenuItem trayStatus, trayLeft, trayRight, trayCase, trayRefresh, trayConnect, trayStartup;
  readonly bool testMode;
  readonly Forms.ToolStripMenuItem[] trayModes=new Forms.ToolStripMenuItem[3];
  System.Drawing.Icon trayIcon;
  void InitTray() {
   trayMenu=new Forms.ContextMenuStrip();
   trayMenu.Items.Add("Air5 Control'ü aç",null,delegate { ShowWindow(); });
   trayMenu.Items.Add(new Forms.ToolStripSeparator());
   trayStatus=new Forms.ToolStripMenuItem("Bağlantı bekleniyor"){Enabled=false}; trayMenu.Items.Add(trayStatus);
   trayLeft=new Forms.ToolStripMenuItem(){Enabled=false}; trayRight=new Forms.ToolStripMenuItem(){Enabled=false}; trayCase=new Forms.ToolStripMenuItem(){Enabled=false};
   trayMenu.Items.AddRange(new Forms.ToolStripItem[]{trayLeft,trayRight,trayCase,new Forms.ToolStripSeparator()});
   string[] labels={"Normal","ANC · Gürültü engelleme","Şeffaflık"};
   foreach(int m in new[]{1,2,0}) {
    int mode=m; var item=new Forms.ToolStripMenuItem(labels[m]); trayModes[m]=item;
    item.Click+=async delegate { await Run(delegate { return Change(mode); }); }; trayMenu.Items.Add(item);
   }
   trayMenu.Items.Add(new Forms.ToolStripSeparator());
   trayRefresh=new Forms.ToolStripMenuItem("Pil ve durumu yenile"); trayRefresh.Click+=async delegate { await Run(ReadAll); }; trayMenu.Items.Add(trayRefresh);
   trayConnect=new Forms.ToolStripMenuItem("Bağlan / Yeniden bağlan"); trayConnect.Click+=async delegate { await Run(Connect); }; trayMenu.Items.Add(trayConnect);
   trayMenu.Items.Add(new Forms.ToolStripSeparator());
   trayStartup=new Forms.ToolStripMenuItem("Windows ile başlat"){Enabled=!testMode};
   trayStartup.Click+=delegate { try { bool enabled=!trayStartup.Checked; Startup.Set(enabled); trayStartup.Checked=enabled; } catch(Exception ex) { Log(ex); tray.ShowBalloonTip(6000,"Otomatik başlatma ayarlanamadı",ex.Message,Forms.ToolTipIcon.Warning); } };
   trayMenu.Items.Add(trayStartup);
   trayMenu.Items.Add("Çıkış",null,delegate { RequestExit(); });
   trayIcon=System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
   tray=new Forms.NotifyIcon(){Icon=trayIcon,ContextMenuStrip=trayMenu,Text="Air5 Control",Visible=true};
   tray.DoubleClick+=delegate { ShowWindow(); };
   UpdateTray();
  }
  void UpdateTray() {
   if(tray==null || closing) return;
   trayLeft.Text="Sol kulaklık: "+Find<TextBlock>("LeftValue").Text;
   trayRight.Text="Sağ kulaklık: "+Find<TextBlock>("RightValue").Text;
   trayCase.Text="Şarj kutusu: "+Find<TextBlock>("CaseValue").Text;
   trayStatus.Text=Find<TextBlock>("ConnectionText").Text+" · "+Find<TextBlock>("ModeText").Text;
   string tip="Air5 Pro | Sol "+Find<TextBlock>("LeftValue").Text+" | Sağ "+Find<TextBlock>("RightValue").Text+" | Kutu "+Find<TextBlock>("CaseValue").Text;
   tray.Text=tip.Length>63?tip.Substring(0,63):tip;
  }
  void ShowWindow() { window.Opacity=1; window.ShowInTaskbar=true; window.ShowActivated=true; window.WindowState=WindowState.Normal; window.Show(); window.Activate(); }
  void HideWindow() { window.Hide(); }
  void RequestExit() { exitRequested=true; HideWindow(); if(!busy) window.Close(); }
  void Cleanup() { ConnectionLog("EXIT","Uygulama kapatıldı"); closing=true; timer.Stop(); buds.Dispose(); tray.Visible=false; tray.Dispose(); trayMenu.Dispose(); if(trayIcon!=null) trayIcon.Dispose(); }
  T Find<T>(string name) where T:class { return window.FindName(name) as T; }
  static Brush Brush(string hex) { return (Brush)new BrushConverter().ConvertFromString(hex); }
  void Text(string name,string value) { Find<TextBlock>(name).Text=value; }
  void Message(string value) { Text("Message",value); }
  void Status(string value,bool connected) { Find<Button>("ConnectButton").Content=connected?"Yeniden bağlan":"Bağlan"; Text("ConnectionText",value); Find<System.Windows.Shapes.Ellipse>("StatusDot").Fill=Brush(connected?"#6ED8EE":"#8492A6"); UpdateTray(); }
  void Enable() {
   Find<Button>("ConnectButton").IsEnabled=!busy; Find<Button>("RefreshButton").IsEnabled=!busy && buds.Ready;
   foreach(string id in new[]{"AncButton","TransparencyButton","NormalButton"}) Find<Button>(id).IsEnabled=!busy && buds.Ready;
   trayConnect.Enabled=!busy && !exitRequested; trayRefresh.Enabled=!busy && buds.Ready && !exitRequested;
   foreach(var item in trayModes) item.Enabled=!busy && buds.Ready && !exitRequested;
  }
  void Battery(string prefix,int? value) {
   Text(prefix+"Value",value.HasValue?value+"%":"—");
   Find<ProgressBar>(prefix+"Bar").Value=value??0;
   Find<TextBlock>(prefix+"Value").ToolTip=value.HasValue?"Kulaklığın bildirdiği pil seviyesi":"Pil bilgisi alınamadı. Kutu pili her bağlantıda sunulmayabilir.";
   UpdateTray();
  }
  void Mode(int? mode) {
   Text("ModeText",mode==1?"Gürültü engelleme açık":mode==2?"Şeffaflık açık":mode==0?"Normal mod":"Durum okunamadı");
   string[] ids={"NormalButton","AncButton","TransparencyButton"};
   for(int i=0;i<ids.Length;i++) {
    trayModes[i].Checked=mode.HasValue && mode.Value==i;
    var b=Find<Button>(ids[i]); bool selected=mode.HasValue && mode.Value==i;
    b.Background=Brush(selected?"#193B48":"#131D2A"); b.BorderBrush=Brush(selected?"#6ED8EE":"#253245");
   }
   UpdateTray();
  }
  void ResetValues() { Battery("Left",null); Battery("Right",null); Battery("Case",null); Mode(null); Text("Detail","Pil bilgileri henüz alınmadı."); }
  void Log(Exception ex) { try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"air5.log"),DateTime.Now.ToString("s")+" "+ex+Environment.NewLine); } catch {} }
  async Task Run(Func<Task> action) {
   if(busy || closing || exitRequested) return; busy=true; Enable();
   try { await action(); }
   catch(Exception ex) { LostConnection(ex.Message); ConnectionLog("ERROR",ex.Message); buds.Dispose(); ResetValues(); Status("Kulaklık bekleniyor",false); Message(ex.Message+" Otomatik olarak yeniden denenecek."); if(lastFailure!=ex.Message) Log(ex); lastFailure=ex.Message; }
   finally {
    if(buds.Ready) { reconnect.Connected(); lastFailure=null; } else reconnect.Failed(DateTime.UtcNow);
    busy=false; if(exitRequested && !closing) window.Close(); else if(!closing) Enable();
   }
  }
  async Task Maintain() {
   if(busy || closing || exitRequested) return;
   if(!buds.Ready) {
    LostConnection("Bluetooth bağlantı durumu kesildi");
    ResetValues(); Status("Kulaklık bekleniyor",false); Enable();
    if(reconnect.Due(DateTime.UtcNow)) await Run(Connect);
   } else if(DateTime.Now-lastRead>=TimeSpan.FromSeconds(30)) await Run(ReadAll);
  }
  async Task ReadAll() {
   var l=Earbuds.Scalar(await buds.Query(6),6,100);
   var r=Earbuds.Scalar(await buds.Query(7),7,100);
   var c=Earbuds.Scalar(await buds.Query(35),35,100);
   var mode=Earbuds.Scalar(await buds.Query(16),16,2);
   Battery("Left",l); Battery("Right",r); Battery("Case",c); Mode(mode);
   if(!l.HasValue && !r.HasValue && !mode.HasValue) {
    LostConnection("Pil ve mod sorgularına yanıt alınamadı"); ConnectionLog("NO_RESPONSE","Pil/mod okunamadı; yeniden denenecek"); buds.Dispose(); ResetValues(); Status("Kulaklık bekleniyor",false); Message("Yanıt alınamadı. Otomatik olarak yeniden bağlanılacak."); return;
   }
   Status("Bağlı",true); lastRead=DateTime.Now;
   Text("Detail","Son okuma "+lastRead.ToString("HH:mm")+(c.HasValue?" · Pil değerleri kulaklığın bildirdiği son seviyelerdir.":" · Kutu pil bilgisi alınamıyor."));
  }
  async Task Connect() {
   connectionAttempts++; ConnectionLog("CONNECT_ATTEMPT","Deneme "+connectionAttempts);
   ResetValues(); Status("Bağlanıyor…",false); Message("Air5 Pro ile kontrol bağlantısı kuruluyor…");
   await buds.Connect();
   var fw=await buds.Query(9);
   if(Earbuds.Matches(fw,9) && fw[4]==0 && fw.Length>5) Text("VersionText","AIR5 CONTROL  /  1.5.1   ·   "+System.Text.Encoding.ASCII.GetString(fw,5,fw.Length-5).Trim('\0'));
   await ReadAll(); if(buds.Ready) {
    connectionConfirmed=true;
    ConnectionLog("CONNECTED","Deneme "+connectionAttempts+"; Sol="+Find<TextBlock>("LeftValue").Text+"; Sağ="+Find<TextBlock>("RightValue").Text+"; Kutu="+Find<TextBlock>("CaseValue").Text+"; Mod="+Find<TextBlock>("ModeText").Text);
    Message("Hazır. Dinleme modunu aşağıdan değiştirebilirsin.");
   }
  }
  async Task Change(int mode) {
   Message("Dinleme modu değiştiriliyor…");
   bool success=await buds.SetMode(mode);
   Mode(Earbuds.Scalar(await buds.Query(16),16,2));
   Message(success?"Dinleme modu kulaklıktan yeniden okunarak doğrulandı.":"Mod değişikliği doğrulanamadı. Durum yeniden okundu.");
  }
  public Controller(Window w,bool startConnection=true,bool startHidden=false) {
   window=w; testMode=!startConnection;
   ConnectionLog("START","Air5 Control 1.5.1; yeniden deneme aralığı 15 saniye");
   if(startHidden) { window.Opacity=0; window.ShowInTaskbar=false; window.ShowActivated=false; }
   InitTray(); Enable();
   if(!testMode) { try { bool enabled=Startup.Preferred(); Startup.Set(enabled); trayStartup.Checked=enabled; } catch(Exception ex) { Log(ex); tray.ShowBalloonTip(6000,"Otomatik başlatma ayarlanamadı",ex.Message,Forms.ToolTipIcon.Warning); } }
   Find<Button>("ConnectButton").Click+=async delegate { await Run(Connect); };
   Find<Button>("RefreshButton").Click+=async delegate { await Run(async delegate { Message("Pil ve mod bilgileri okunuyor…"); await ReadAll(); if(buds.Ready) Message("Bilgiler güncellendi."); }); };
   Find<Button>("AncButton").Click+=async delegate { await Run(delegate { return Change(1); }); };
   Find<Button>("TransparencyButton").Click+=async delegate { await Run(delegate { return Change(2); }); };
   Find<Button>("NormalButton").Click+=async delegate { await Run(delegate { return Change(0); }); };
   Find<Button>("Minimize").Click+=delegate { HideWindow(); };
   Find<Button>("Close").Click+=delegate { window.Close(); };
   Find<Grid>("TitleDrag").MouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e) { if(e.OriginalSource is TextBlock || e.OriginalSource==Find<Grid>("TitleDrag")) window.DragMove(); };
   window.StateChanged+=delegate { if(window.WindowState==WindowState.Minimized) HideWindow(); };
   window.Closing+=delegate(object sender,System.ComponentModel.CancelEventArgs e) { if(!exitRequested) { e.Cancel=true; HideWindow(); } else Cleanup(); };
   Application.Current.SessionEnding+=delegate { exitRequested=true; };
   bool loaded=false;
   window.Loaded+=async delegate { if(loaded) return; loaded=true; if(startHidden) { HideWindow(); window.Opacity=1; } if(startConnection) await Run(Connect); };
   timer.Interval=TimeSpan.FromSeconds(3);
   timer.Tick+=async delegate { await Maintain(); };
   if(startConnection) timer.Start();
  }
  public void TestTray() {
   if(window.IsVisible || window.ShowInTaskbar || trayStartup.Enabled) throw new Exception("Hidden startup failed");
   Battery("Left",90); Battery("Right",80); Battery("Case",70); Mode(1);
   if(trayLeft.Text!="Sol kulaklık: 90%" || !tray.Text.Contains("Kutu 70%")) throw new Exception("Tray battery display failed");
   foreach(int m in new[]{0,1,2}) { Mode(m); for(int i=0;i<3;i++) if(trayModes[i].Checked!=(i==m)) throw new Exception("Tray mode selection failed"); }
   if(trayRefresh.Enabled || trayModes[0].Enabled || !trayConnect.Enabled) throw new Exception("Disconnected menu state failed");
   window.WindowState=WindowState.Minimized; if(window.IsVisible) throw new Exception("Minimize did not hide");
   ShowWindow(); if(!window.IsVisible || window.WindowState!=WindowState.Normal) throw new Exception("Restore failed");
   window.Close(); if(window.IsVisible || closing) throw new Exception("Close did not hide to tray");
   ShowWindow(); Battery("Case",null); if(!trayCase.Text.Contains("—")) throw new Exception("Unavailable battery display failed");
   reconnect.Failed(DateTime.UtcNow); Maintain().GetAwaiter().GetResult();
   if(Find<TextBlock>("ConnectionText").Text!="Kulaklık bekleniyor" || !window.IsVisible) throw new Exception("Disconnected maintenance failed");
   bool ran=false; busy=true;
   Run(delegate { ran=true; return Task.FromResult(0); }).GetAwaiter().GetResult(); busy=false;
   if(ran) throw new Exception("Concurrent operation allowed");
   Run(delegate { throw new InvalidOperationException("SIMULATED DISCONNECT TEST"); }).GetAwaiter().GetResult();
   if(closing || !window.IsVisible || reconnect.Due(DateTime.UtcNow) || trayModes[0].Enabled) throw new Exception("Failure recovery failed");
   RequestExit(); if(!closing || tray.Visible || timer.IsEnabled) throw new Exception("Exit cleanup failed");
   File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tray-test.txt"),"PASS: hidden startup without registry changes; tray batteries/tooltip; three mode selection states; disconnected maintenance; concurrent operations blocked; simulated disconnect schedules retry without closing; minimize; restore; close to tray; unavailable battery; explicit exit cleanup. No Bluetooth operations.");
  }
 }
 public static class Program {
  [STAThread] public static int Main(string[] args) {
   try {
    if(args.Length>0 && args[0]=="--self-test") { Test(); return 0; }
    var app=new Application();
    if(args.Length>0 && args[0]=="--connection-test") { ConnectionTest().GetAwaiter().GetResult(); return 0; }
    var resource=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Air5UI");
    Window window; using(var stream=resource) window=(Window)XamlReader.Load(stream);
    if(args.Length>0 && args[0]=="--preview") {
     var root=(FrameworkElement)window.Content; ((Border)root).Background=window.Background; root.Measure(new System.Windows.Size(960,730)); root.Arrange(new System.Windows.Rect(0,0,960,730)); root.UpdateLayout();
     var image=new RenderTargetBitmap(960,730,96,96,PixelFormats.Pbgra32); image.Render(root);
     var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
     using(var output=File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Onizleme.png"))) encoder.Save(output);
     return 0;
    }
    bool trayTest=args.Length>0 && args[0]=="--tray-test";
    bool startHidden=!(args.Length>0 && args[0]=="--show");
    var controller=new Controller(window,!trayTest,startHidden);
    if(trayTest) window.Loaded+=delegate { window.Dispatcher.BeginInvoke(new Action(delegate { try { controller.TestTray(); } catch(Exception ex) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tray-test-error.txt"),ex.ToString()); app.Shutdown(1); } })); };
    app.Run(window); return 0;
   } catch(Exception ex) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"startup-error.txt"),ex.ToString()); if(args.Length==0) MessageBox.Show(ex.Message,"Air5 Control",MessageBoxButton.OK,MessageBoxImage.Error); return 1; }
  }
  static async Task ConnectionTest() {
   var path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"connection-test.txt");
   using(var b=new Earbuds()) {
    try {
     await b.Connect(); File.WriteAllText(path,"Connected\r\n");
     foreach(int code in new[]{9,6,7,35,16}) { var reply=await b.Query(code); File.AppendAllText(path,code+": "+(reply==null?"No reply":BitConverter.ToString(reply))+"\r\n"); }
    } catch(Exception ex) { File.WriteAllText(path,ex.ToString()); }
   }
  }
  static void Test() {
   var retry=new ReconnectSchedule(); var now=new DateTime(2026,10,17,0,0,0,DateTimeKind.Utc);
   if(!retry.Due(now)) throw new Exception("Initial connection not due");
   for(int i=0;i<4;i++) { retry.Failed(now); if(retry.Due(now.AddSeconds(14)) || !retry.Due(now.AddSeconds(15))) throw new Exception("Retry timing failed"); now=now.AddSeconds(15); }
   retry.Connected(); if(!retry.Due(now)) throw new Exception("Reconnect reset failed");
   if(Startup.Command(@"C:\Air5 Test\Air5-Control.exe")!="\"C:\\Air5 Test\\Air5-Control.exe\" --tray") throw new Exception("Startup command quoting failed");
   foreach(int code in new[]{6,7,9,16,35}) Earbuds.Validate(Earbuds.GetPacket(code));
   foreach(int mode in new[]{0,1,2}) {
    var packet=Earbuds.ModePacket(mode); Earbuds.Validate(packet);
    if(BitConverter.ToString(packet)!="00-0A-03-11-0"+mode) throw new Exception("Incorrect mode packet");
    if(Earbuds.Scalar(new byte[]{0,10,131,16,0,(byte)mode},16,2)!=mode) throw new Exception("Mode decode failed");
   }
   foreach(int mode in new[]{-1,3,256}) {
    bool rejected=false; try { Earbuds.ModePacket(mode); } catch(ArgumentException) { rejected=true; }
    if(!rejected) throw new Exception("Invalid mode allowed");
   }
   foreach(var bad in new[]{new byte[]{0,29,14,1,17},new byte[]{0,10,3,5},new byte[]{0,10,3,17,3}}) {
    bool rejected=false; try { Earbuds.Validate(bad); } catch(ArgumentException) { rejected=true; } if(!rejected) throw new Exception("Forbidden packet accepted");
   }
   if(Earbuds.Scalar(new byte[]{0,10,131,35,0,255},35,100)!=null) throw new Exception("Unknown battery decoded as percentage");
   if(Earbuds.Scalar(new byte[]{0,10,131,6,0,90},6,100)!=90) throw new Exception("Battery decode failed");
   if(Earbuds.Scalar(new byte[]{0,10,131,16,0,2},16,2)!=2) throw new Exception("Mode decode failed");
   if(Earbuds.Scalar(new byte[]{0,10,131,16,1,2},16,2)!=null) throw new Exception("Error accepted");
   File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test.txt"),"PASS: initial/repeated 15-second retry schedule and reset; quoted startup command; exact Normal/ANC/transparency packets; all three mode replies; invalid modes blocked; EQ/reset blocked; battery/status validation.");
  }
 }
}


