using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MemePets;

class MediaTests {
    [STAThread] static int Main(string[] args) {
        int count=0,failures=0;
        foreach(string name in new[]{"SunPet","ShaolanPet","GoodThingPet"}) {
            string dir=Path.Combine(args[0],"assets",name);
            var image=new BitmapImage(new Uri(Path.Combine(dir,"face.png")));
            Console.WriteLine(name+" image "+image.PixelWidth+"x"+image.PixelHeight);
            var clips=Files.Read<List<Clip>>(Path.Combine(dir,"clips.json"));
            foreach(var clip in clips) {
                bool ok=false;string error="timeout";
                var player=new MediaPlayer();var frame=new DispatcherFrame();
                var timeout=new DispatcherTimer {Interval=TimeSpan.FromSeconds(6)};
                timeout.Tick+=(s,e)=>frame.Continue=false;
                player.MediaOpened+=(s,e)=> {ok=player.NaturalDuration.HasTimeSpan&&player.NaturalDuration.TimeSpan.TotalSeconds>0;error="no duration";frame.Continue=false;};
                player.MediaFailed+=(s,e)=>{error=e.ErrorException.Message;frame.Continue=false;};
                player.Volume=0;player.Open(new Uri(Path.Combine(dir,"Clips",clip.File)));timeout.Start();Dispatcher.PushFrame(frame);timeout.Stop();player.Close();
                if(!ok){failures++;Console.WriteLine("FAIL "+name+"/"+clip.File+" "+error);}else count++;
            }
        }
        Console.WriteLine("Audio decoder: "+count+" passed, "+failures+" failed");
        return failures==0&&count==76?0:1;
    }
}
