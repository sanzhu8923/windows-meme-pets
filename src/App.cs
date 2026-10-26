using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace MemePets {
    [DataContract] public class Preferences {
        [DataMember] public double Size=200;
        [DataMember] public double Volume=0.7;
        [DataMember] public double X=double.NaN;
        [DataMember] public double Y=double.NaN;
        [DataMember] public bool Link=true;
        [DataMember] public bool Fixed;
        [DataMember] public int Pinned;
        [DataMember] public int[] Excluded;
    }
    public static class Files {
        public static T Read<T>(string path) {
            using(var stream=File.OpenRead(path)) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
        }
        public static void Write<T>(string path,T value) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary=path+".tmp";
            using(var stream=File.Create(temporary)) new DataContractJsonSerializer(typeof(T)).WriteObject(stream,value);
            if(File.Exists(path)) File.Replace(temporary,path,null); else File.Move(temporary,path);
        }
    }
    public static class Native {
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern uint RegisterWindowMessage(string name);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string className,string title);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr window,int index);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr window,int index,int value);
        public static readonly uint Spoke=RegisterWindowMessage("MemePets.Windows.1.Spoken");
        public static readonly uint Control=RegisterWindowMessage("MemePets.Windows.1.Control");
        public static readonly IntPtr Broadcast=new IntPtr(0xffff);
        public static void Send(Role target,uint message,IntPtr w,IntPtr l) {
            var handle=FindWindow(null,Program.Name(target)+" · Windows 桌宠");
            if(handle!=IntPtr.Zero)PostMessage(handle,message,w,l);
        }
        public static void SendAll(uint message,IntPtr w,IntPtr l) {
            foreach(Role r in Enum.GetValues(typeof(Role)))Send(r,message,w,l);
        }
        public static string MutexName(Role role) { return @"Local\MemePets.Windows.1."+role; }
        public static bool IsRunning(Role role) {
            try { using(var mutex=Mutex.OpenExisting(MutexName(role))) return true; }
            catch(WaitHandleCannotBeOpenedException) { return false; }
        }
    }
    public static class Program {
        public static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
        public static string Arg(string[] args,string key) { int i=Array.IndexOf(args,key); return i>=0 && i+1<args.Length ? args[i+1] : null; }
        public static string Name(Role role) { return role==Role.Sun ? "太对了" : role==Role.Shaolan ? "薄肌" : "好事"; }
        public static string AssetName(Role role) { return role==Role.Sun?"SunPet":role==Role.Shaolan?"ShaolanPet":"GoodThingPet"; }
        public static string AssetDir(Role role) { return Path.Combine(Root,"assets",AssetName(role)); }
        public static Role GetRole(string[] args) {
            string value=Arg(args,"--role") ?? Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
            return value.IndexOf("shaolan",StringComparison.OrdinalIgnoreCase)>=0 ? Role.Shaolan : value.IndexOf("good",StringComparison.OrdinalIgnoreCase)>=0 ? Role.Good : Role.Sun;
        }
        [STAThread] public static int Main(string[] args) {
            try {
                string stem=Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
                if(stem=="一起启动" || args.Contains("--all")) {
                    foreach(Role r in Enum.GetValues(typeof(Role))) Process.Start(new ProcessStartInfo(Path.Combine(Root,AssetName(r)+".exe")) { WorkingDirectory=Root });
                    return 0;
                }
                var role=GetRole(args);
                string command=args.FirstOrDefault(a=>new[]{"--say","--stop","--quit","--quit-all","--reset","--menu"}.Contains(a));
                if(command!=null) {
                    int action=command=="--say"?1:command=="--stop"?2:command=="--reset"?3:command=="--menu"?5:4;
                    if(command=="--quit-all")Native.SendAll(Native.Control,IntPtr.Zero,new IntPtr(action));
                    else Native.Send(role,Native.Control,new IntPtr((int)role),new IntPtr(action));
                    return 0;
                }
                bool fresh;
                using(var mutex=new Mutex(true,Native.MutexName(role),out fresh)) {
                    if(!fresh) { Native.Send(role,Native.Control,new IntPtr((int)role),new IntPtr(3)); return 0; }
                    var app=new Application { ShutdownMode=ShutdownMode.OnMainWindowClose };
                    app.DispatcherUnhandledException+=(sender,e)=> { MessageBox.Show(e.Exception.Message,"桌宠运行错误");e.Handled=true;app.Shutdown(1); };
                    var pet=new PetWindow(role,args);
                    app.Run(pet);
                    GC.KeepAlive(mutex);
                }
                return 0;
            } catch(Exception e) { MessageBox.Show(e.Message,"桌宠无法启动",MessageBoxButton.OK,MessageBoxImage.Error);return 1; }
        }
    }
    public class PetWindow : Window {
        readonly Role role;
        readonly List<Clip> clips;
        readonly ClipPicker picker;
        readonly ClipPicker replies;
        readonly Preferences settings;
        readonly string settingsPath;
        readonly string tracePath;
        readonly bool muted;
        readonly Image face;
        readonly TextBlock caption;
        readonly Border bubble;
        readonly MediaPlayer player=new MediaPlayer();
        readonly Forms.NotifyIcon tray;
        readonly DispatcherTimer response=new DispatcherTimer();
        readonly DispatcherTimer watchdog=new DispatcherTimer();
        readonly BitmapSource bitmap;
        readonly byte[] pixels;
        readonly int stride;
        readonly ScaleTransform scale=new ScaleTransform(1,1);
        Clip active;
        int activeHop;
        Role activeRoot;
        bool playing, closed, startedAudio, dragging;
        int playGeneration;
        Point down, origin;
        Action pending;
        HwndSource source;

        public PetWindow(Role role,string[] args) {
            this.role=role; muted=args.Contains("--mute");
            string data=Program.Arg(args,"--data-dir") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MemePetsWindows");
            Directory.CreateDirectory(data);
            settingsPath=Path.Combine(data,role+".json");
            tracePath=args.Contains("--trace")?Path.Combine(data,role+".log"):null;
            clips=Files.Read<List<Clip>>(Path.Combine(Program.AssetDir(role),"clips.json"));
            if(clips.Count==0 || clips.Any(c=>!File.Exists(AudioPath(c)))) throw new FileNotFoundException("语音素材不完整。请先解压整个压缩包，再打开 exe；不要只移动 exe 文件。");
            try { settings=Files.Read<Preferences>(settingsPath); } catch { settings=new Preferences {Size=role==Role.Sun?180:200}; }
            settings.Size=PositionRules.Clamp(settings.Size,120,400);
            settings.Volume=PositionRules.Clamp(settings.Volume,0,1);
            if(settings.Excluded==null) settings.Excluded=clips.Where(c=>c.DefaultOff).Select(c=>c.Id).ToArray();
            if(!clips.Any(c=>c.Id==settings.Pinned)) {settings.Fixed=false;settings.Pinned=clips[0].Id;}
            picker=new ClipPicker(clips,new Random());picker.SetExcluded(settings.Excluded);
            int[] shortIds={1,2,4,11,13,14};
            var pool=clips.Where(c=>role==Role.Sun?shortIds.Contains(c.Id):c.Duration<1.6).ToList();
            replies=new ClipPicker(pool.Count>0?pool:clips,new Random());replies.SetExcluded(settings.Excluded);
            Title=Program.Name(role)+" · Windows 桌宠";
            WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.NoResize; AllowsTransparency=true; Background=null;Topmost=true; ShowInTaskbar=false;ShowActivated=false;
            bitmap=new BitmapImage(new Uri(Path.Combine(Program.AssetDir(role),"face.png")));
            var bgra=new FormatConvertedBitmap(bitmap,PixelFormats.Bgra32,null,0);
            stride=bgra.PixelWidth*4;pixels=new byte[stride*bgra.PixelHeight];bgra.CopyPixels(pixels,stride,0);
            var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition {Height=new GridLength(42)});
            face=new Image {Source=bitmap,Stretch=Stretch.Fill,Margin=new Thickness(10,0,10,0),RenderTransformOrigin=new Point(.5,.85),RenderTransform=scale,Cursor=Cursors.Hand,ToolTip="单击说话 · 按住拖动 · 右键设置 · 滚轮缩放"};
            caption=new TextBlock {Text=Program.Name(role),FontSize=13,FontFamily=new FontFamily("Microsoft YaHei UI"),TextAlignment=TextAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Center,Foreground=Brushes.White};
            bubble=new Border {Background=new SolidColorBrush(Color.FromArgb(210,30,31,35)),CornerRadius=new CornerRadius(12),Padding=new Thickness(9,4,9,4),Margin=new Thickness(4,3,4,3),Child=caption,VerticalAlignment=VerticalAlignment.Center};
            layout.Children.Add(face);Grid.SetRow(bubble,1);layout.Children.Add(bubble);Content=layout;
            UpdateSize();
            tray=new Forms.NotifyIcon {Text=Title,Visible=true};
            using(var image=new System.Drawing.Bitmap(Path.Combine(Program.AssetDir(role),"face.png")))
            using(var thumb=new System.Drawing.Bitmap(image,32,32)) {
                IntPtr handle=thumb.GetHicon();
                using(var icon=System.Drawing.Icon.FromHandle(handle)) tray.Icon=(System.Drawing.Icon)icon.Clone();
                DestroyIcon(handle);
            }
            tray.MouseClick+=(sender,e)=>Dispatcher.BeginInvoke(new Action(()=> { if(e.Button==Forms.MouseButtons.Right) OpenMenu(); else ResetPosition(); }));
            face.MouseLeftButtonDown+=OnFaceDown;face.MouseMove+=OnFaceMove;face.MouseLeftButtonUp+=OnFaceUp;
            face.LostMouseCapture+=(sender,e)=>{dragging=false;};
            MouseRightButtonUp+=(sender,e)=>{e.Handled=true;OpenMenu();};
            MouseWheel+=(sender,e)=>{settings.Size=PositionRules.Clamp(settings.Size+(e.Delta>0?20:-20),120,400);UpdateSize();ClampPosition();Save();e.Handled=true;};
            response.Tick+=(sender,e)=> {response.Stop();var action=pending;pending=null;if(action!=null)action();};
            watchdog.Tick+=(sender,e)=> {watchdog.Stop();StopPlayback(false);};
            player.MediaOpened+=(sender,e)=>OnOpened();
            player.MediaEnded+=(sender,e)=>StopPlayback(false);
            player.MediaFailed+=(sender,e)=> {Log("ERROR audio "+e.ErrorException.Message);StopPlayback(false);caption.Text="音频播放失败，右键可重试";};
            SourceInitialized+=(sender,e)=> {
                source=(HwndSource)PresentationSource.FromVisual(this);source.AddHook(Hook);
                var handle=new WindowInteropHelper(this).Handle;
                Native.SetWindowLong(handle,-20,Native.GetWindowLong(handle,-20)|0x08000000|0x00000080);
                if(double.IsNaN(settings.X)||double.IsNaN(settings.Y)) ResetPosition();else {Left=settings.X;Top=settings.Y;ClampPosition();}
                Log("READY "+role);
            };
            Closed+=(sender,e)=> {closed=true;StopPlayback(true);Save();player.Close();tray.Visible=false;tray.Dispose();if(source!=null)source.RemoveHook(Hook);Log("CLOSED");};
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=OnDisplayChanged;
            Closed+=(sender,e)=>Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=OnDisplayChanged;
        }
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
        void OnDisplayChanged(object sender,EventArgs e) { if(!closed) Dispatcher.BeginInvoke(new Action(()=>{ClampPosition();Save();})); }
        string AudioPath(Clip clip) {return Path.Combine(Program.AssetDir(role),"Clips",clip.File);}
        void Log(string message) { if(tracePath!=null) try {File.AppendAllText(tracePath,DateTime.UtcNow.ToString("O")+" "+message+Environment.NewLine);}catch(IOException){} }
        void Save() {settings.X=Left;settings.Y=Top;try{Files.Write(settingsPath,settings);}catch(Exception e){Log("ERROR settings "+e.Message);} }
        void UpdateSize() {Width=settings.Size;Height=(settings.Size-20)*bitmap.PixelHeight/bitmap.PixelWidth+42;}
        void ClampPosition() {
            var transform=source==null?Matrix.Identity:source.CompositionTarget.TransformToDevice;
            var center=transform.Transform(new Point(double.IsNaN(Left)?0:Left+Width/2,double.IsNaN(Top)?0:Top+Height/2));
            var screen=Forms.Screen.FromPoint(new System.Drawing.Point((int)center.X,(int)center.Y));
            var from=transform;from.Invert();
            var min=from.Transform(new Point(screen.WorkingArea.Left,screen.WorkingArea.Top));
            var max=from.Transform(new Point(screen.WorkingArea.Right,screen.WorkingArea.Bottom));
            Left=PositionRules.Clamp(Left,min.X,max.X-Width);Top=PositionRules.Clamp(Top,min.Y,max.Y-Height);
        }
        void ResetPosition() {
            var area=SystemParameters.WorkArea;
            Left=area.Right-Width-20-((int)role-1)*220;Top=area.Bottom-Height-20;ClampPosition();Save();
            Show();Topmost=true;
        }
        void OnFaceDown(object sender,MouseButtonEventArgs e) {
            down=PointToScreen(e.GetPosition(this));origin=new Point(Left,Top);dragging=false;face.CaptureMouse();e.Handled=true;
        }
        void OnFaceMove(object sender,MouseEventArgs e) {
            if(!face.IsMouseCaptured || e.LeftButton!=MouseButtonState.Pressed) return;
            var now=PointToScreen(e.GetPosition(this));var delta=now-down;
            if(!dragging && delta.Length<5) return;dragging=true;
            var t=source.CompositionTarget.TransformFromDevice;var d=t.Transform(delta);
            Left=origin.X+d.X;Top=origin.Y+d.Y;
        }
        void OnFaceUp(object sender,MouseButtonEventArgs e) {
            if(!face.IsMouseCaptured)return;bool wasDragging=dragging;face.ReleaseMouseCapture();
            if(wasDragging){ClampPosition();Save();Log("DRAG "+Left+","+Top);}else PlayManual();e.Handled=true;
        }
        void PlayManual() {CancelPending();if(playing)return;Play(picker.Next(settings.Fixed,settings.Pinned),0,role);}
        void CancelPending() {response.Stop();pending=null;}
        void StopPlayback(bool cancelResponse) {
            if(cancelResponse){CancelPending();playGeneration++;}watchdog.Stop();player.Stop();playing=false;startedAudio=false;active=null;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,null);scale.BeginAnimation(ScaleTransform.ScaleYProperty,null);
            caption.Text=Program.Name(role);
        }
        void Play(Clip clip,int hop,Role root) {
            if(playing||closed)return;
            active=clip;activeHop=hop;activeRoot=root;playing=true;startedAudio=false;playGeneration++;
            player.Volume=muted?0:settings.Volume;
            caption.Text=clip.Label;caption.ToolTip=clip.Label;
            player.Open(new Uri(AudioPath(clip)));
            watchdog.Interval=TimeSpan.FromSeconds(Math.Max(10,clip.Duration+5));watchdog.Start();
        }
        void OnOpened() {
            if(!playing||active==null||startedAudio)return;startedAudio=true;
            double duration=player.NaturalDuration.HasTimeSpan?player.NaturalDuration.TimeSpan.TotalSeconds:active.Duration;
            player.Play();
            var pulse=new DoubleAnimation(1,1.035,TimeSpan.FromMilliseconds(130)) {AutoReverse=true,RepeatBehavior=new RepeatBehavior(Math.Max(1,duration/.26))};
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,pulse);scale.BeginAnimation(ScaleTransform.ScaleYProperty,pulse);
            Log("PLAY role="+role+" clip="+active.Id+" hop="+activeHop+" root="+activeRoot+" duration="+duration.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture));
            if(settings.Link) {
                long packed=(int)role|((int)activeRoot<<8)|(activeHop<<16);
                foreach(Role other in Enum.GetValues(typeof(Role)))if(other!=role)Native.Send(other,Native.Spoke,new IntPtr(packed),new IntPtr((long)(duration*1000)));
            }
        }
        IntPtr Hook(IntPtr hwnd,int message,IntPtr w,IntPtr l,ref bool handled) {
            if((uint)message==Native.Control) {
                if(w.ToInt64()!=0 && w.ToInt64()!=(int)role)return IntPtr.Zero;
                handled=true;switch(l.ToInt32()) {case 1:PlayManual();break;case 2:StopPlayback(true);Log("STOP");break;case 3:ResetPosition();break;case 4:Close();break;case 5:OpenMenu();break;}
            } else if((uint)message==Native.Spoke) {
                int packed=w.ToInt32();var from=(Role)(packed&255);var root=(Role)((packed>>8)&255);int hop=(packed>>16)&255;
                int duration=l.ToInt32();
                if(settings.Link && ReplyRules.Accept(role,from,root,hop) && duration>0 && duration<120000) {
                    CancelPending();int generation=playGeneration;
                    var clip=replies.NextEnabled();if(clip==null)return IntPtr.Zero;
                    pending=()=>{if(!playing&&!closed&&settings.Link&&generation==playGeneration&&Native.IsRunning(from))Play(clip,hop+1,root);};
                    response.Interval=TimeSpan.FromMilliseconds(Math.Max(80,duration-(hop==1?200:300)));response.Start();
                    Log("SCHEDULE from="+from+" hop="+(hop+1));
                }
            } else if(message==0x0084 && !face.IsMouseCaptured) {
                long coordinate=l.ToInt64();int x=(short)(coordinate&0xffff),y=(short)((coordinate>>16)&0xffff);
                Point p=face.PointFromScreen(new Point(x,y));
                if(p.X>=0&&p.Y>=0&&p.X<face.ActualWidth&&p.Y<face.ActualHeight) {
                    int px=Math.Min(bitmap.PixelWidth-1,(int)(p.X/face.ActualWidth*bitmap.PixelWidth));
                    int py=Math.Min(bitmap.PixelHeight-1,(int)(p.Y/face.ActualHeight*bitmap.PixelHeight));
                    if(pixels[py*stride+px*4+3]<16){handled=true;return new IntPtr(-1);}
                }
            }
            return IntPtr.Zero;
        }
        MenuItem Item(string title,Action action,bool? check=null) {
            var item=new MenuItem {Header=title};if(check.HasValue){item.IsCheckable=true;item.IsChecked=check.Value;}
            item.Click+=(sender,e)=>{e.Handled=true;action();};return item;
        }
        void OpenMenu() {
            var menu=new ContextMenu();
            menu.Items.Add(new MenuItem {Header=Program.Name(role)+" · "+clips.Count+" 段原声",IsEnabled=false});
            var peers=Enum.GetValues(typeof(Role)).Cast<Role>().Where(r=>r!=role&&Native.IsRunning(r)).Select(Program.Name).ToArray();
            menu.Items.Add(new MenuItem {Header="其他桌宠："+(peers.Length==0?"未启动":string.Join("、",peers)),IsEnabled=false});
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("随机说一句",()=>{StopPlayback(true);Play(picker.Next(false,0),0,role);}));
            menu.Items.Add(Item("停止说话 / 取消接话",()=>StopPlayback(true)));
            var mode=new MenuItem {Header="播放方式"};
            mode.Items.Add(Item("随机轮播（每轮不重复）",()=>{settings.Fixed=false;Save();},!settings.Fixed));
            mode.Items.Add(Item("固定台词（在下面选一句）",()=>{settings.Fixed=true;Save();},settings.Fixed));menu.Items.Add(mode);
            var choose=new MenuItem {Header="挑一句（点击试听，并设为固定台词）",MaxHeight=540};
            foreach(var c in clips) {var clip=c;choose.Items.Add(Item(clip.Id.ToString("00")+" · "+clip.Label,()=>{settings.Pinned=clip.Id;settings.Fixed=true;Save();StopPlayback(true);Play(clip,0,role);},settings.Fixed&&settings.Pinned==clip.Id));}menu.Items.Add(choose);
            var enabled=new MenuItem {Header="随机台词范围（勾选表示启用）",MaxHeight=540};
            enabled.Items.Add(Item("全部启用",()=>{SetExcluded(new int[0]);}));
            enabled.Items.Add(Item("恢复原版默认",()=>SetExcluded(clips.Where(c=>c.DefaultOff).Select(c=>c.Id).ToArray())));
            foreach(var c in clips) {var clip=c;bool on=!settings.Excluded.Contains(clip.Id);enabled.Items.Add(Item(clip.Id.ToString("00")+" · "+clip.Label,()=> {
                var ids=new HashSet<int>(settings.Excluded);if(on)ids.Add(clip.Id);else ids.Remove(clip.Id);
                if(ids.Count>=clips.Count){MessageBox.Show("请至少保留一句台词。",Program.Name(role));return;}SetExcluded(ids.ToArray());
            },on));}menu.Items.Add(enabled);
            menu.Items.Add(Item("三个桌宠互相接话",()=>{settings.Link=!settings.Link;if(!settings.Link)CancelPending();Save();},settings.Link));
            var volume=new MenuItem {Header="音量"};foreach(int n in new[]{0,25,50,75,100}) {int value=n;volume.Items.Add(Item(n+"%",()=>{settings.Volume=value/100.0;player.Volume=muted?0:settings.Volume;Save();},Math.Abs(settings.Volume-n/100.0)<.02));}menu.Items.Add(volume);
            var size=new MenuItem {Header="人物大小（也可以滚轮调整）"};foreach(int n in new[]{120,160,180,200,240,280,320,400}){int value=n;size.Items.Add(Item(n+" 像素",()=>{settings.Size=value;UpdateSize();ClampPosition();Save();},settings.Size==n));}menu.Items.Add(size);
            menu.Items.Add(Item("移回右下角",()=>ResetPosition()));
            menu.Items.Add(Item("启动另外两个",()=>{foreach(Role r in Enum.GetValues(typeof(Role)))if(r!=role&&!Native.IsRunning(r))Process.Start(Path.Combine(Program.Root,Program.AssetName(r)+".exe"));}));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("打开使用说明",()=>Process.Start(Path.Combine(Program.Root,"使用说明.txt"))));
            menu.Items.Add(Item("退出这个桌宠",()=>Close()));
            menu.Items.Add(Item("全部退出",()=>Native.SendAll(Native.Control,IntPtr.Zero,new IntPtr(4))));
            menu.PlacementTarget=this;menu.Placement=System.Windows.Controls.Primitives.PlacementMode.MousePoint;menu.IsOpen=true;
        }
        void SetExcluded(int[] ids) {settings.Excluded=ids;picker.SetExcluded(ids);replies.SetExcluded(ids);Save();}
    }
}
