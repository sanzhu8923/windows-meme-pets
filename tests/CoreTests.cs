using System;
using System.Collections.Generic;
using System.Linq;
using MemePets;

class CoreTests {
    static int count;
    static void Check(bool good, string title) { if (!good) throw new Exception(title); count++; Console.WriteLine("PASS " + title); }
    static int Main() {
        try {
            var clips = new List<Clip> { new Clip { Id=1 }, new Clip { Id=2 }, new Clip { Id=3 } };
            var picker = new ClipPicker(clips, new Random(5));
            Check(new HashSet<int>(Enumerable.Range(0,3).Select(x=>picker.Next(false,0).Id)).Count==3,"each clip once per round");
            picker.SetExcluded(new[]{1,2});
            Check(Enumerable.Range(0,6).All(x=>picker.Next(false,0).Id==3),"excluded clips never play");
            picker.SetExcluded(new[]{1,2,3});
            Check(picker.Next(false,0)!=null,"all excluded recovers");
            Check(picker.NextEnabled()==null,"automatic replies never revive disabled short clips");
            Check(picker.Next(true,99)!=null,"missing pinned clip recovers");
            Check(picker.Next(true,2).Id==2,"fixed clip selection");
            Check(ReplyRules.Accept(Role.Sun,Role.Shaolan,Role.Shaolan,0),"shaolan starts sun reply");
            Check(ReplyRules.Accept(Role.Good,Role.Sun,Role.Shaolan,1),"sun continues to good");
            Check(ReplyRules.Accept(Role.Sun,Role.Good,Role.Shaolan,2),"good continues to final sun");
            Check(!ReplyRules.Accept(Role.Good,Role.Sun,Role.Shaolan,3),"four-sentence chain terminates");
            Check(!ReplyRules.Accept(Role.Good,Role.Sun,Role.Sun,0),"sun click cannot start good");
            Check(ReplyRules.Accept(Role.Sun,Role.Good,Role.Good,0),"manual good gets one sun reply");
            Check(!ReplyRules.Accept(Role.Shaolan,Role.Sun,Role.Shaolan,1),"shaolan never replies");
            Check(!ReplyRules.Accept(Role.Sun,Role.Sun,Role.Sun,0),"no self reply");
            Check(!ReplyRules.Accept(Role.Sun,Role.Good,Role.Shaolan,-1),"reject malformed hop");
            Check(PositionRules.Clamp(double.NaN,0,100)==0,"invalid position recovery");
            Check(PositionRules.Clamp(10000,0,100)==100,"off-screen clamp");
            Console.WriteLine("PASS: " + count + " checks"); return 0;
        } catch(Exception e) { Console.Error.WriteLine("FAIL " + e.Message); return 1; }
    }
}
