using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;
using RimWorld;
using UnityEngine;
using IsoRim;
using RimZrism.Materials;
using Api = IsoRim.Api;
using ZApi = ProjectZ.Api;

[StaticConstructorOnStartup]
public static class IrmeFloorTestSetup
{
    static IrmeFloorTestSetup()
    {
        if (Environment.GetCommandLineArgs().Contains("-isorim-normal-floor-test")
            && GenFilePaths.SaveDataFolderPath.Replace('\\','/').Contains("/floor-run")) Configure();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Configure() { ProjectZ.ProjectZMod.Settings.floors = 3; }
}

// Isolated acceptance only. ProjectZ's public API controls the real installed layout. Reflection is restricted
// to verifying the host's uploaded section meshes; production code never references ProjectZ or these internals.
public sealed class IrmeFloorAcceptance : GameComponent
{
    public IrmeFloorAcceptance(Game game) { }
    bool ready, failed, capture; int frames, phase; float next;
    static bool reload;
    Thing ground, upper, top; Vector3 center; IntVec3 root;
    int hiddenProjections, hiddenLevel, hiddenSwitches;
    string Out => GenFilePaths.SaveDataFolderPath;
    Map Map => Find.CurrentMap;
    ModelLodMap Tracker => Map.GetComponent<ModelLodMap>();
    readonly string[] names = { "ground_near", "upper_near_lower_coarse", "upper_mid", "upper_far", "upper_return",
        "upper_hidden", "hidden_camera_changes", "peek_upper", "peek_off", "top_floor", "lower_quality_option_off",
        "perspective", "rotated", "indoor_night", "upper_fog", "upper_roof", "upper_slab", "slab_hidden", "before_save", "reloaded", "hidden_despawn" };
    void Note(string s) { File.AppendAllText(Path.Combine(Out, "floor-acceptance.txt"), s + "\n"); Log.Message("[IRME-Floor-Test] " + s); }
    void Check(bool ok, string s) { if (!ok) throw new Exception("FAIL: " + s); Note("PASS: " + s); }
    void Hour(float h) { var t = Find.TickManager; t.DebugSetTicksGame(t.TicksGame + (int)((h - GenLocalDate.HourFloat(Map)) * 2500)); }
    Thing Spawn(string def, IntVec3 cell) => GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(def)), cell, Map);
    IntVec3 Cell(int x, int z, int floor) => ZApi.CellOnFloor(root + new IntVec3(x, 0, z), floor, Map);
    void Camera(float size) => Find.CameraDriver.SetRootPosAndSize(center, size);
    void Dirty() => Map.mapDrawer.RegenerateEverythingNow();
    [MethodImpl(MethodImplOptions.NoInlining)]
    void Setup()
    {
        Find.TickManager.Pause(); DebugSettings.enableStoryteller = false;
        Check(ZApi.HasFloors(Map) && ZApi.FloorCount(Map) >= 3, "real IsoRimZ three-floor map loaded");
        root = ZApi.GroundRect(Map).CenterCell; center = root.ToVector3Shifted();
        IsoRimMod.Settings.isometric = true; IsoRimMod.Settings.isoPitch = 32; IsoRimMod.Settings.freeYaw = 0;
        IsoRimMod.Settings.turn = 0; IsoRimMod.Settings.texturedModels = true; IsoRimMod.Settings.showRoofs = false;
        IsoRimMod.Settings.cutHover = false; IsoRimMod.Settings.cutRooms = false; View3D.Active = true;
        if (!reload)
        {
            foreach (var pawn in Map.mapPawns.AllPawnsSpawned.ToList()) pawn.Destroy();
            for (int f = 0; f < 3; f++) foreach (var g in CellRect.CenteredOn(root, 19).Cells)
            {
                var c = ZApi.CellOnFloor(g, f, Map);
                foreach (var t in c.GetThingList(Map).ToList()) if (t.def.destroyable) t.Destroy();
                Map.roofGrid.SetRoof(c, null);
                Map.terrainGrid.SetTerrain(c, f == 0 ? TerrainDefOf.Concrete : DefDatabase<TerrainDef>.GetNamed("PZ_OpenAir"));
            }
            // A small native slab under each upper sample; the offset in x keeps all three samples distinguishable.
            for (int f = 1; f < 3; f++) foreach (var g in CellRect.CenteredOn(root + new IntVec3(f * 3, 0, 0), 1).Cells)
                Map.terrainGrid.SetTerrain(ZApi.CellOnFloor(g, f, Map), DefDatabase<TerrainDef>.GetNamed("PZ_UpperFloor"));
            ground = Spawn("IRME_NormalCrate", Cell(-3, 0, 0)); upper = Spawn("IRME_NormalCrate", Cell(3, 0, 1));
            top = Spawn("IRME_NormalCrate", Cell(6, 0, 2));
            Spawn("IRME_FlatCrate", Cell(1, 3, 1));
            foreach (var c in CellRect.CenteredOn(Cell(1,3,1),1).Cells)
                Map.terrainGrid.SetTerrain(c, DefDatabase<TerrainDef>.GetNamed("PZ_UpperFloor"));
            // Each sample occupies 2x2 cells. Non-overlapping placements are essential: adjacent spawns would
            // destroy earlier crates and turn a claimed 256-building workload into a handful of buildings.
            for (int x = -36; x < -4; x += 2) for (int z = -36; z < -4; z += 2)
            {
                foreach (var c in new CellRect(Cell(x,z,2).x,Cell(x,z,2).z,2,2).Cells)
                    Map.terrainGrid.SetTerrain(c, DefDatabase<TerrainDef>.GetNamed("PZ_UpperFloor"));
                Spawn("IRME_NormalCrate", Cell(x,z,2));
            }
            Spawn("TorchLamp", Cell(4, 1, 1));
            Map.terrainGrid.SetTerrain(Cell(4,1,1), DefDatabase<TerrainDef>.GetNamed("PZ_UpperFloor"));
            upper.HitPoints = 137; Hour(9); LodOptions.Current.lowerFloorsUseLowest = true;
            ZApi.SetViewedFloor(0);
        }
        else
        {
            ground = Cell(-3,0,0).GetThingList(Map).Single(t => t.def.defName == "IRME_NormalCrate");
            upper = Cell(3,0,1).GetThingList(Map).Single(t => t.def.defName == "IRME_NormalCrate");
            top = Cell(6,0,2).GetThingList(Map).Single(t => t.def.defName == "IRME_NormalCrate");
            Check(upper.HitPoints == 137 && upper.Rotation == Rot4.East, "upper building HP/rotation saved and reloaded");
            phase = 19; LodOptions.Current.lowerFloorsUseLowest = true; ZApi.SetViewedFloor(1);
        }
        Map.fogGrid.ClearAllFog(); Camera(7); Dirty(); ready = true; next = Time.realtimeSinceStartup + 15;
        Check(Bootstrap.Enabled, "addon enabled with exact candidate host pin");
        Check(!typeof(Bootstrap).Assembly.GetReferencedAssemblies().Any(a => a.Name == "0Harmony" || a.Name == "IsoRimProjectZ"), "production addon references neither Harmony nor ProjectZ internals");
    }
    int HiddenProjectionTotal() => Map.listerThings.AllThings.Where(t => t.def.defName == "IRME_NormalCrate" && ZApi.FloorOf(t) > 0)
        .Sum(t => Tracker.StateFor(t)?.projections ?? 0);
    void Expect(Thing t, int level)
    {
        var s = Tracker.StateFor(t);
        Check(s != null && s.level == level && s.rendered == level, "floor " + ZApi.FloorOf(t) + " LOD=" + level + " actual=" + s?.level + "/" + s?.rendered);
    }
    void Surface(Thing t, bool faded)
    {
        var section = Map.mapDrawer.SectionAt(t.Position);
        var field = typeof(Section).GetField("layers", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        int count = 0;
        foreach (SectionLayer layer in (System.Collections.IEnumerable)field.GetValue(section)) foreach (var sm in layer.subMeshes)
        {
            if (sm.material?.shader != DefDatabase<ShaderDef>.GetNamed("IRME_NormalMapped").Shader || sm.verts.Count == 0) continue;
            count++;
            Check(sm.mesh.normals.Length == sm.verts.Count && sm.mesh.tangents.Length == sm.verts.Count, "upper/ground section uploads full normal and tangent arrays");
            if (faded)
            {
                var m = FadeFloors.For(sm.material, 1);
                Check(m != sm.material && m.shader == sm.material.shader && Math.Abs(m.GetFloat("_IsoFade") - .25f) < .001f, "custom material participates in native floor fade");
                Check(m.GetTexture("_NormalTex") == sm.material.GetTexture("_NormalTex"), "fade clone retains shared normal texture");
            }
        }
        Check(count > 0, "normal material is present on floor " + ZApi.FloorOf(t));
    }
    void Projection()
    {
        var s = Tracker.StateFor(upper); var view = Api.ReadStaticModelView(Map, upper.Position.z);
        Check(view.Visible && view.Floor == 1 && view.Offset.z == -ZApi.FloorRowShift(1, Map), "public row view matches real ProjectZ row shift");
        Check(Math.Abs(view.Offset.y - ZApi.StoreyHeight) < .0001f, "floor offset stays in true height units");
        float min=1e9f,max=-1e9f;
        var matrix = Matrix4x4.Translate(new Vector3(0, view.Offset.y / View3D.Squash, view.Offset.z));
        foreach (var raw in s.corners)
        {
            var draw = matrix.MultiplyPoint3x4(new Vector3(raw.x, raw.y / View3D.Squash, raw.z));
            var q=Api.WorldToViewport(new Vector3(draw.x,draw.y*View3D.Squash,draw.z)); min=Math.Min(min,q.y);max=Math.Max(max,q.y);
        }
        Check(Math.Abs(s.height-(max-min)) < .002f, "LOD projection agrees with native floor draw matrix in current camera mode");
    }
    void Evaluate()
    {
        Check(Api.StaticModelExtensionsActive && Api.Dev.FloorsDrawn, "static API stays active while floors are drawn");
        if (phase==0) { Expect(ground,0); Surface(ground,false); Check(Map.listerThings.AllThings.Count(t=>t.def.defName=="IRME_NormalCrate" && ZApi.FloorOf(t)>0)==258,"258 real, non-overlapping upper-floor buildings remain spawned"); Check(HiddenProjectionTotal()==0,"258 upper-floor models start hidden with zero LOD projections"); }
        if (phase==1 || phase==4 || phase==19) { Expect(upper,0); Expect(ground,2); Surface(upper,false); Projection(); }
        if (phase==2) Expect(upper,1);
        if (phase==3) Expect(upper,2);
        if (phase==5) { Check(!Api.ReadStaticModelView(Map,upper.Position.z).Visible,"upper floor hidden"); hiddenProjections=HiddenProjectionTotal();hiddenLevel=Tracker.StateFor(upper).level;hiddenSwitches=Tracker.Switches; }
        if (phase==6) { Check(HiddenProjectionTotal()==hiddenProjections,"hidden floors: ZERO extra projections while camera changes"); Check(Tracker.StateFor(upper).level==hiddenLevel,"hidden floor keeps cached mesh instead of rebuilding to a distant LOD");Note("HIDDEN rowsSkipped="+Tracker.HiddenRowsSkipped+" projectionDelta="+(HiddenProjectionTotal()-hiddenProjections)+" allVisibleSwitches="+(Tracker.Switches-hiddenSwitches)); }
        if (phase==7) { Expect(upper,0); Check(Api.ReadStaticModelView(Map,upper.Position.z).Visible,"peeked floor counts as visible");Surface(upper,true); }
        if (phase==8) Check(!Api.ReadStaticModelView(Map,upper.Position.z).Visible,"releasing peek hides upper floor again");
        if (phase==9) { Expect(top,0);Expect(upper,2);Expect(ground,2);Surface(top,false); }
        if (phase==10) { Expect(upper,0);Expect(ground,0);Check(!LodOptions.LowerFloorsUseLowest,"lower-floor quality option restores screen-size policy"); }
        if (phase==11 || phase==12) { Projection();Surface(upper,false); }
        if (phase==20) Check(Tracker.StateFor(upper)==null,"hidden despawn pruned by bounded maintenance");
        Check(Tracker.MaxSectionsPerFrame<=2,"at most two addon LOD section invalidations per frame");
        Note("PHASE "+names[phase]+" checks="+Tracker.Checks+" switches="+Tracker.Switches+" dirty="+Tracker.DirtyRequests+" rows="+Tracker.RowQueries);
    }
    void Advance()
    {
        phase++;
        if(phase>=names.Length){File.WriteAllText(Path.Combine(Out,"completed.txt"),"Floor assertions passed; visual review separate.");Application.Quit();return;}
        if(phase==1)ZApi.SetViewedFloor(1);
        if(phase==2)Camera(13);
        if(phase==3)Camera(40);
        if(phase==4)Camera(7);
        if(phase==5)ZApi.SetViewedFloor(0);
        if(phase==7){Camera(7);Api.PeekUpperFloors=true;}
        if(phase==8)Api.PeekUpperFloors=false;
        if(phase==9)ZApi.SetViewedFloor(2);
        if(phase==10)LodOptions.Current.lowerFloorsUseLowest=false;
        if(phase==11){IsoRimMod.Settings.perspective=true;IsoRimMod.Settings.isometric=false;Camera(7);ZApi.SetViewedFloor(1);}
        if(phase==12){upper.Rotation=Rot4.East;Api.InvalidateStaticModel(upper);}
        if(phase==13){IsoRimMod.Settings.perspective=false;IsoRimMod.Settings.isometric=true;Hour(0);foreach(var c in CellRect.CenteredOn(upper.Position,3).Cells)Map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);Dirty();}
        if(phase==14){Map.fogGrid.Refog(CellRect.CenteredOn(upper.Position,2));Dirty();}
        if(phase==15){Map.fogGrid.ClearAllFog();Hour(9);IsoRimMod.Settings.showRoofs=true;Dirty();}
        if(phase==16){IsoRimMod.Settings.showRoofs=false;foreach(var c in CellRect.CenteredOn(upper.Position,3).Cells)Map.roofGrid.SetRoof(c,null);foreach(var g in CellRect.CenteredOn(root+new IntVec3(3,0,0),1).Cells)Map.terrainGrid.SetTerrain(ZApi.CellOnFloor(g,2,Map),DefDatabase<TerrainDef>.GetNamed("PZ_UpperFloor"));ZApi.SetViewedFloor(2);Dirty();}
        if(phase==17)ZApi.SetViewedFloor(1);
        if(phase==18){LodOptions.Current.lowerFloorsUseLowest=true;}
        if(phase==19){GameDataSaveLoader.SaveGame("IRME_Floors");reload=true;GameDataSaveLoader.LoadGame("IRME_Floors");ready=false;frames=-100;return;}
        if(phase==20){ZApi.SetViewedFloor(0);upper.Destroy();}
        next=Time.realtimeSinceStartup+8;
    }
    public override void GameComponentUpdate()
    {
        if(!Environment.GetCommandLineArgs().Contains("-isorim-normal-floor-test") || !Out.Replace('\\','/').Contains("/floor-run") || failed)return;
        Run();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    void Run()
    {
        try
        {
            if(!ready){if(Current.ProgramState==ProgramState.Playing&&Map!=null&&++frames>80)Setup();return;}
            Api.Dev.DebrisClear();
            if(phase==6 && !capture) Camera(20+13*Mathf.Sin(Time.realtimeSinceStartup*3));
            if(Time.realtimeSinceStartup<next)return;
            if(!capture){Evaluate(); ScreenCapture.CaptureScreenshot(Path.Combine(Out,phase.ToString("D2")+"_"+names[phase]+".png"));capture=true;next=Time.realtimeSinceStartup+1;}
            else{capture=false;Advance();}
        }
        catch(Exception e){failed=true;File.WriteAllText(Path.Combine(Out,"failed.txt"),e.ToString());Note(e.ToString());Application.Quit();}
    }
}
