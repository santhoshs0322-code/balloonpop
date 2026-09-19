using System;
using System.Collections.Generic;
// Isolated persistence/math adapter: exercises production LevelManager without a Unity license.
namespace UnityEngine {
    public static class PlayerPrefs {
        static readonly Dictionary<string,int> values = new Dictionary<string,int>();
        public static int GetInt(string key, int fallback=0) { int v; return values.TryGetValue(key,out v) ? v : fallback; }
        public static void SetInt(string key,int value) { values[key]=value; }
        public static void Save() {}
    }
    public static class Mathf {
        public static int Clamp(int v,int lo,int hi) { return Math.Max(lo,Math.Min(v,hi)); }
        public static float Exp(float v) { return (float)Math.Exp(v); }
        public static int RoundToInt(float v) { return (int)Math.Round(v); }
    }
}
class ProgressionChecks {
    static int checks;
    static void Check(bool value,string message) { if(!value) throw new Exception(message); checks++; }
    static void Main() {
        Check(LevelManager.IsUnlocked(1),"First level open");
        Check(!LevelManager.IsUnlocked(2),"Second level locked");
        LevelManager.SelectLevel(500);
        Check(LevelManager.CurrentLevel==1,"Cannot select locked level");
        LevelManager.SaveResult(500,99999);
        Check(!LevelManager.IsUnlocked(501),"Cannot unlock via locked result");
        for(int level=1;level<=1000;level++) {
            int target=LevelManager.GetClearScore(level);
            Check(target<=LevelManager.GetStar3Score(level),"Ordered thresholds");
            LevelManager.SaveResult(level,target-1);
            Check(!LevelManager.IsUnlocked(level+1),"Failure must not unlock");
            LevelManager.SaveResult(level,target);
            Check(LevelManager.GetStars(level)==1,"Clear grants star");
            Check(LevelManager.IsUnlocked(level+1),"Clear unlocks next");
            LevelManager.SelectLevel(level+1);
            Check(LevelManager.CurrentLevel==level+1,"Selection beyond ten");
            LevelManager.SaveResult(level,LevelManager.GetStar3Score(level));
            LevelManager.SaveResult(level,0);
            Check(LevelManager.GetStars(level)==3,"Replay preserves best stars");
        }
        foreach(var size in new[]{new[]{320,568},new[]{1080,2400},new[]{2048,2732},new[]{1920,1080},new[]{720,720}}) {
            float safeWidth=size[0]-32, safeHeight=size[1]-90;
            float scale=Math.Min(safeWidth/1080f,safeHeight/1920f);
            Check(1080*scale<=safeWidth+0.01f && 1920*scale<=safeHeight+0.01f,"Design fits safe area");
        }
        Console.WriteLine("PASS: "+checks+" progression and layout checks");
    }
}
