using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IsoRim;
using UnityEngine;
using Verse;

public sealed class IRMEComparison : GameComponent
{
    public IRMEComparison(Game game) { }
    private static bool loaded;
    private bool ready, captured;
    private int frames, phase;
    private float start;
    private long last;
    private readonly List<double> ms = new List<double>();
    private readonly float[] zooms = { 7, 13, 40 };
    private string Out => GenFilePaths.SaveDataFolderPath;
    public override void GameComponentUpdate()
    {
        if (!Environment.GetCommandLineArgs().Contains("-isorim-normal-comparison-test") || !Out.Replace('\\', '/').Contains("/compare-")) return;
        if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null) return;
        if (!loaded) { loaded = true; GameDataSaveLoader.LoadGame("Comparison"); return; }
        if (++frames < 80) return;
        Find.WindowStack.Windows.ToList().ForEach(w => w.Close(false));
        if (!ready)
        {
            Find.TickManager.Pause(); DebugSettings.enableStoryteller = false;
            IsoRimMod.Settings.isometric = true; IsoRimMod.Settings.isoPitch = 32;
            IsoRimMod.Settings.freeYaw = 0; IsoRimMod.Settings.turn = 0;
            IsoRimMod.Settings.texturedModels = true; IsoRimMod.Settings.showRoofs = false;
            Api.Dev.View3DActive = true; Api.Dev.DebrisClear();
            Find.CameraDriver.SetRootPosAndSize(Find.CurrentMap.Center.ToVector3Shifted(), zooms[phase]);
            if (phase == 0) Find.CurrentMap.mapDrawer.RegenerateEverythingNow();
            start = Time.realtimeSinceStartup; last = System.Diagnostics.Stopwatch.GetTimestamp(); ready = true;
            return;
        }
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        double elapsed = (now - last) * 1000.0 / System.Diagnostics.Stopwatch.Frequency; last = now;
        if (Time.realtimeSinceStartup - start < 5) return;
        if (!captured) { ScreenCapture.CaptureScreenshot(Path.Combine(Out, "compare-" + phase + ".png")); captured = true; return; }
        if (ms.Count < 360) { ms.Add(elapsed); return; }
        ms.Sort();
        File.AppendAllText(Path.Combine(Out, "frames.csv"), phase + "," + zooms[phase] + "," + ms.Count + "," + ms.Average().ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "," + ms[ms.Count / 2].ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "," + ms[(int)(ms.Count * .95)].ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "\n");
        if (++phase == zooms.Length) { File.WriteAllText(Path.Combine(Out, "completed.txt"), "Same-save fixed-camera frame intervals sampled; not GPU timings or a performance claim."); Application.Quit(); return; }
        ms.Clear(); ready = false; captured = false;
    }
}
