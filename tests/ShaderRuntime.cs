using System;
using System.IO;
using System.Linq;
using IsoRim;
using RimWorld;
using UnityEngine;
using Verse;
using RimZrism.Materials;

// Isolated camera regression. Host internals are used only to drive/inspect the test, never by the addon.
public sealed class IrmeShaderAcceptance : GameComponent
{
    public IrmeShaderAcceptance(Game game) { }
    int frames, phase; bool ready, captured, failed; float next; Thing subject; Pawn eye;
    string Out => GenFilePaths.SaveDataFolderPath;
    Map Map => Find.CurrentMap;
    string[] names = { "ortho45", "perspective45", "perspective90", "perspective270", "free915", "rim90", "firstperson", "ortho_return" };
    float[] yaws = { 0, 45, 90, 270, 91.5f, 90, 0, 0 }; // Orthographic isometric starts at 45; perspective base is 0.
    void Check(bool ok, string s) { if (!ok) throw new Exception(s); File.AppendAllText(Path.Combine(Out,"shader-acceptance.txt"),"PASS "+s+"\n"); }
    void Setup()
    {
        Find.TickManager.Pause(); DebugSettings.enableStoryteller=false;
        var center=Map.Center;
        foreach(var pawn in Map.mapPawns.AllPawnsSpawned.ToList()) pawn.Destroy();
        foreach(var c in CellRect.CenteredOn(center,20).Cells){foreach(var t in c.GetThingList(Map).ToList())if(t.def.destroyable)t.Destroy();Map.roofGrid.SetRoof(c,null);Map.terrainGrid.SetTerrain(c,TerrainDefOf.Concrete);}
        Map.fogGrid.ClearAllFog();
        subject=GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("IRME_NormalCrate")),center,Map);
        GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("IRME_FlatCrate")),center+new IntVec3(4,0,0),Map);
        eye=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Faction.OfPlayer);
        GenSpawn.Spawn(eye,center+new IntVec3(1,0,-5),Map);eye.Rotation=Rot4.North;
        var tick=Find.TickManager;tick.DebugSetTicksGame(tick.TicksGame+(int)((10-GenLocalDate.HourFloat(Map))*2500));
        IsoRimMod.Settings.texturedModels=true;IsoRimMod.Settings.showRoofs=false;IsoRimMod.Settings.cutHover=false;IsoRimMod.Settings.cutRooms=false;
        Api.Dev.View3DActive=true; Map.mapDrawer.RegenerateEverythingNow();
        Check(Bootstrap.Enabled,"addon enabled");ready=true;Configure();
    }
    void Configure()
    {
        if(FirstPerson.On)FirstPerson.Leave(null);
        IsoRimMod.Settings.isometric=phase==0||phase==7;
        IsoRimMod.Settings.perspective=phase>0&&phase<7;
        IsoRimMod.Settings.isoPitch=32;IsoRimMod.Settings.turn=0;IsoRimMod.Settings.freeYaw=yaws[phase];
        IsoRimMod.Settings.rimLight=phase==5;IsoRimMod.Settings.rimStrength=1;
        Find.CameraDriver.SetRootPosAndSize((Map.Center+new IntVec3(2,0,0)).ToVector3Shifted(),5);
        if(phase==6){Find.Selector.ClearSelection();Find.Selector.Select(eye);FirstPerson.Toggle();}
        Api.InvalidateStaticModel(subject);next=Time.realtimeSinceStartup+6;captured=false;
    }
    void Evaluate()
    {
        Check(Api.ViewPerspective==(phase>0&&phase<7),names[phase]+" camera projection");
        Check((Api.FirstPersonPawn==eye)==(phase==6),names[phase]+" first-person state");
        var section=Map.mapDrawer.SectionAt(subject.Position);
        var field=typeof(Section).GetField("layers",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        int count=0;
        foreach(SectionLayer layer in (System.Collections.IEnumerable)field.GetValue(section))foreach(var sm in layer.subMeshes){
            if(sm.material?.shader!=DefDatabase<ShaderDef>.GetNamed("IRME_NormalMapped").Shader||sm.verts.Count==0)continue;
            count++;Check(sm.mesh.normals.Length==sm.verts.Count&&sm.mesh.tangents.Length==sm.verts.Count,names[phase]+" normal/tangent stream");
            Check(sm.material.GetTexture("_NormalTex")!=null,names[phase]+" normal texture bound");
        }
        Check(count>0,names[phase]+" actual custom section material");
        File.AppendAllText(Path.Combine(Out,"shader-acceptance.txt"),"VIEW "+names[phase]+" yaw="+View3D.Yaw+" squash="+View3D.Squash+" fov="+Api.FieldOfView+"\n");
    }
    public override void GameComponentUpdate()
    {
        if(!Environment.GetCommandLineArgs().Contains("-isorim-normal-shader-test")||!Out.Replace('\\','/').Contains("/shader-run")||failed)return;
        try{
            if(Current.ProgramState!=ProgramState.Playing||Map==null)return;
            if(!ready){if(++frames>80)Setup();return;}
            Find.WindowStack.Windows.ToList().ForEach(w=>w.Close(false));Api.Dev.DebrisClear();
            if(Time.realtimeSinceStartup<next)return;
            if(!captured){Evaluate();ScreenCapture.CaptureScreenshot(Path.Combine(Out,phase.ToString("D2")+"_"+names[phase]+".png"));captured=true;next=Time.realtimeSinceStartup+1;return;}
            if(++phase==names.Length){File.WriteAllText(Path.Combine(Out,"completed.txt"),"Shader camera assertions passed; visual review separate.");Application.Quit();return;}
            Configure();
        }catch(Exception e){failed=true;File.WriteAllText(Path.Combine(Out,"failed.txt"),e.ToString());Application.Quit();}
    }
}
