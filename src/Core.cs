using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
namespace MemePets {
    public enum Role { Sun=1, Shaolan=2, Good=3 }
    [DataContract] public class Clip {
        [DataMember(Name="id")] public int Id;
        [DataMember(Name="label")] public string Label;
        [DataMember(Name="file")] public string File;
        [DataMember(Name="duration")] public double Duration;
        [DataMember(Name="defaultOff")] public bool DefaultOff;
    }
    public class ClipPicker {
        readonly List<Clip> clips;
        readonly Random random;
        readonly Queue<Clip> bag = new Queue<Clip>();
        HashSet<int> excluded = new HashSet<int>();
        int last = -1;
        public ClipPicker(List<Clip> clips, Random random) {
            if(clips==null || clips.Count==0) throw new ArgumentException("No audio clips");
            this.clips=clips; this.random=random;
        }
        public void SetExcluded(IEnumerable<int> ids) { excluded = new HashSet<int>(ids); bag.Clear(); }
        public Clip NextEnabled() { return clips.Any(c=>!excluded.Contains(c.Id))?Next(false,0):null; }
        public Clip Next(bool fixedMode,int pinned) {
            if(fixedMode) { var chosen=clips.Find(c=>c.Id==pinned); if(chosen!=null) return chosen; }
            if(bag.Count==0) {
                var pool=clips.Where(c=>!excluded.Contains(c.Id)).ToList();
                if(pool.Count==0) pool=new List<Clip>(clips);
                for(int i=pool.Count-1;i>0;i--) { int j=random.Next(i+1); var t=pool[i];pool[i]=pool[j];pool[j]=t; }
                if(pool.Count>1 && pool[0].Id==last) { var t=pool[0];pool[0]=pool[1];pool[1]=t; }
                foreach(var clip in pool) bag.Enqueue(clip);
            }
            var result=bag.Dequeue(); last=result.Id; return result;
        }
    }
    public static class ReplyRules {
        public static bool Accept(Role me,Role from,Role root,int hop) {
            if(hop<0 || hop>=3 || me==from || !Enum.IsDefined(typeof(Role),root)) return false;
            if(me==Role.Sun) return from==Role.Shaolan || from==Role.Good;
            return me==Role.Good && from==Role.Sun && root==Role.Shaolan;
        }
    }
    public static class PositionRules {
        public static double Clamp(double value,double minimum,double maximum) {
            if(double.IsNaN(value) || double.IsInfinity(value)) return minimum;
            return Math.Max(minimum,Math.Min(Math.Max(minimum,maximum),value));
        }
    }
}
