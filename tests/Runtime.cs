using System;
using System.IO;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using IsoRim;
using RimZrism.Materials;
using NormalMaterialStore = RimZrism.Materials.Materials;


public class IsoRimNormalLodAcceptance:GameComponent {
 public IsoRimNormalLodAcceptance(Game game){}
 bool ready,failed,pending;int frames,phase,stableSwitches;float next,boundaryZoom,jitterMin=1,jitterMax;Thing subject,control;Vector3 center;
 static bool reload;
 string Out=>GenFilePaths.SaveDataFolderPath;
 string[] names={"01_near_LOD0","02_medium_LOD1","03_far_LOD2","04_return_LOD1","05_return_LOD0","06_hysteresis_hold","07_far_save","08_reload_LOD2","09_near_reload","10_medium_night","11_medium_roof","12_medium_restored","13_destroyed"};
 float[] zooms={7,13,40,13,7,9,40,40,7,13,13,13,13};
 int[] expected={0,1,2,1,0,0,2,2,0,1,1,1,-1};
 int failingSelectorCalls,failingSurfaceCalls;
 bool IsRun=>Environment.GetCommandLineArgs().Contains("-isorim-normal-lod-test") && Out.Replace('\\','/').Contains("/lod-run");
 ModelLodMap Tracker=>Find.CurrentMap.GetComponent<ModelLodMap>();
 void Note(string s){File.AppendAllText(Path.Combine(Out,"lod-acceptance.txt"),s+"\n");Log.Message("[IRME-LOD-Test] "+s);}
 void Check(bool v,string s){if(!v)throw new Exception("FAIL: "+s);Note("PASS: "+s);}
 void Hour(float hour){var t=Find.TickManager;t.DebugSetTicksGame(t.TicksGame+(int)((hour-GenLocalDate.HourFloat(Find.CurrentMap))*2500));}
 Thing Spawn(string name,IntVec3 at){return GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(name)),at,Find.CurrentMap);}
 void PolicyTests(){
  Check(Bootstrap.Enabled,"API-only addon initialized");
  var definition=DefDatabase<ModelDef>.GetNamed("IRME_NormalModel");
  var loaded=Api.LoadModel(definition.modContentPack,definition.model);
  var hidden=typeof(ModelHandle).GetField("model",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(loaded);
  Check(object.ReferenceEquals(hidden,Forms.For(subject.def).model.variants[0]),"public load and gameplay model share one cache identity");
  Api.AddStaticModelSelector(ctx=>{if(ctx.Thing==control){failingSelectorCalls++;throw new Exception("EXPECTED selector fault injection");}return null;});
  Check(!typeof(Bootstrap).Assembly.GetReferencedAssemblies().Any(a=>a.Name=="0Harmony"),"addon has no Harmony reference");
  var mod=DefDatabase<LodDef>.GetNamed("IRME_CrateLod").modContentPack;
  var handle=Api.LoadModel(mod,"Models/Crate_LOD2.obj");
  var snap=Api.ReadStaticModel(handle);var original=snap.Positions[0];snap.Positions[0]=new Vector3(999,999,999);
  Check(Api.ReadStaticModel(handle).Positions[0]==original,"geometry snapshot cannot mutate host model");
  bool rejected=false;try{Api.RegisterStaticSurface(handle,new Vector3[1],new Vector4[1],ctx=>null);}catch(ArgumentException){rejected=true;}
  Check(rejected,"API rejects wrong corner array lengths");
  rejected=false;try{var ns=Enumerable.Repeat(Vector3.up,snap.Positions.Length).ToArray();var ts=Enumerable.Repeat(new Vector4(float.NaN,0,0,1),ns.Length).ToArray();Api.RegisterStaticSurface(handle,ns,ts,ctx=>null);}catch(ArgumentException){rejected=true;}
  Check(rejected,"API rejects nonfinite tangent data");
  Api.RegisterStaticSurface(handle,Enumerable.Repeat(Vector3.up,snap.Positions.Length).ToArray(),Enumerable.Repeat(new Vector4(1,0,0,1),snap.Positions.Length).ToArray(),ctx=>{failingSurfaceCalls++;throw new Exception("EXPECTED surface fault injection");});
  var d=DefDatabase<LodDef>.GetNamed("IRME_CrateLod");var c=new LodChain{def=d,models=new ModelHandle[3]};
  Check(c.Choose(.2f,-1)==0&&c.Choose(.08f,-1)==1&&c.Choose(.02f,-1)==2,"initial screen-size selection covers all three levels");
  Check(c.Choose(.11f,0)==0&&c.Choose(.13f,1)==1,"15 percent hysteresis retains both sides inside dead band");
  Check(c.Choose(.10f,0)==1&&c.Choose(.14f,1)==0&&c.Choose(.03f,1)==2&&c.Choose(.06f,2)==1,"both threshold crossings work in both directions");
  Check(c.Choose(float.NaN,2)==0,"invalid projected size falls back to full detail");
  var two=new LodChain{def=new LodDef{model="test",levels=new System.Collections.Generic.List<LodLevel>{new LodLevel{model="far",belowScreenHeight=.1f}}},models=new ModelHandle[2]};
  Check(two.Choose(.2f,-1)==0&&two.Choose(.05f,-1)==1&&two.Choose(.2f,1)==0,"two-level configuration supports downgrade and upgrade");
  Check(new LodDef{model="test",levels=new System.Collections.Generic.List<LodLevel>{new LodLevel{model="bad",belowScreenHeight=float.NaN}}}.ConfigErrors().Any(),"invalid threshold rejected by definition validation");
 }
 void Setup(){
  if(!IsRun)throw new Exception("Refusing non-LOD isolated profile");
  var map=Find.CurrentMap;Find.TickManager.Pause();DebugSettings.enableStoryteller=false;center=map.Center.ToVector3Shifted();
  IsoRimMod.Settings.isometric=true;IsoRimMod.Settings.isoPitch=32;IsoRimMod.Settings.freeYaw=0;IsoRimMod.Settings.turn=0;IsoRimMod.Settings.texturedModels=true;IsoRimMod.Settings.showRoofs=false;View3D.Active=true;
  if(!reload){
   foreach(var p in map.mapPawns.AllPawnsSpawned.ToList())p.Destroy();
   foreach(var c in CellRect.CenteredOn(map.Center,20).Cells){foreach(var t in c.GetThingList(map).ToList())if(t.def.destroyable)t.Destroy();map.roofGrid.SetRoof(c,null);map.terrainGrid.SetTerrain(c,TerrainDefOf.Concrete);}
   map.fogGrid.ClearAllFog();subject=Spawn("IRME_NormalCrate",map.Center+new IntVec3(2,0,-2));control=Spawn("IRME_FlatCrate",map.Center+new IntVec3(-2,0,2));Spawn("TorchLamp",map.Center+new IntVec3(5,0,0));Hour(9);subject.HitPoints=139;
   PolicyTests();
  }else{
   phase=7;subject=map.listerThings.AllThings.Single(t=>t.def.defName=="IRME_NormalCrate");control=map.listerThings.AllThings.Single(t=>t.def.defName=="IRME_FlatCrate");Check(subject.HitPoints==139,"save/load retains normal building data");
  }
  Find.CameraDriver.SetRootPosAndSize(center,zooms[phase]);map.mapDrawer.RegenerateEverythingNow(); // initial scene only; zoom tests must rely on product LOD dirtying.
  ready=true;next=Time.realtimeSinceStartup+12;Note("READY chains="+ModelLods.ChainsLoaded);
 }
 void Advance(){
  phase++;var map=Find.CurrentMap;
  if(phase>=names.Length){Check(Tracker.MaxSectionsPerFrame<=2,"at most two sections dirtied by LOD per frame");File.WriteAllText(Path.Combine(Out,"completed.txt"),"LOD runtime assertions passed. Visual review recorded separately.");Note("COMPLETE switches="+Tracker.Switches+" dirty="+Tracker.DirtyRequests+" checks="+Tracker.Checks);Application.Quit();return;}
  if(phase==5){stableSwitches=Tracker.Switches;boundaryZoom=zooms[4]*Tracker.StateFor(subject).height/.12f;}
  if(phase==7){GameDataSaveLoader.SaveGame("IRME_LOD_Acceptance");reload=true;GameDataSaveLoader.LoadGame("IRME_LOD_Acceptance");ready=false;frames=-100;return;}
  if(phase==9){Hour(0);foreach(var c in CellRect.CenteredOn(map.Center,7).Cells)map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);map.mapDrawer.RegenerateEverythingNow();}
  if(phase==10){IsoRimMod.Settings.showRoofs=true;IsoRimMod.Settings.cutHover=false;IsoRimMod.Settings.cutRooms=false;map.mapDrawer.RegenerateEverythingNow();}
  if(phase==11){IsoRimMod.Settings.showRoofs=false;Hour(9);map.mapDrawer.RegenerateEverythingNow();}
  if(phase==12){subject.Destroy();}
  Find.CameraDriver.SetRootPosAndSize(center,zooms[phase]);
 }
 void CheckBasisUpload(){
  var sec=Find.CurrentMap.mapDrawer.SectionAt(subject.Position);
  var field=typeof(Section).GetField("layers",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
  var layers=(System.Collections.IEnumerable)field.GetValue(sec);
  int found=0;
  foreach(SectionLayer layer in layers)foreach(var mesh in layer.subMeshes){
   if(mesh.material?.shader!=DefDatabase<ShaderDef>.GetNamed("IRME_NormalMapped").Shader || mesh.verts.Count==0)continue;
   Check(mesh.mesh.tangents.Length==mesh.verts.Count && mesh.mesh.normals.Length==mesh.verts.Count,"actual GPU mesh arrays have normals and tangents for every corner");found++;
  }
  Note("basis diagnostic requests="+NormalMaterialStore.MaterialRequests+" state="+(Tracker.StateFor(subject)?.level.ToString()??"none"));
  Check(found>0,"custom surface is present in regular section mesh");
 }
 public override void GameComponentUpdate(){
  if(!IsRun||failed)return;
  try{
   if(!ready){if(Current.ProgramState==ProgramState.Playing&&Find.CurrentMap!=null&&++frames>50)Setup();return;}
   Api.Dev.DebrisClear();
   if(phase==5){ // oscillate around the threshold, remaining inside the hysteresis band.
    float z=boundaryZoom+.25f*Mathf.Sin(Time.realtimeSinceStartup*8);Find.CameraDriver.SetRootPosAndSize(center,z);
    if(Time.realtimeSinceStartup>next-4){float h=Tracker.StateFor(subject).height;jitterMin=Mathf.Min(jitterMin,h);jitterMax=Mathf.Max(jitterMax,h);}
   }
   if(Time.realtimeSinceStartup<next)return;
   if(!pending){
    if(phase==0 || phase==1 || phase==8)CheckBasisUpload();
    if(!reload && phase==3){Check(failingSelectorCalls==1,"throwing selector disabled after one call");Check(failingSurfaceCalls==1,"throwing surface disabled after one call");}
    var s=Tracker.StateFor(subject);
    if(phase<12){
     Check(s!=null&&s.level==expected[phase]&&s.rendered==expected[phase],names[phase]+" applied="+(s==null?"none":s.level+" rendered="+s.rendered+" size="+s.height.ToString("F4")+" tris="+ModelLods.Corners(s.chain.models[s.level])/3));
     Check(Tracker.StateFor(control)==null,"unconfigured control stays outside LOD tracker");
     Find.Selector.Select(subject);Check(Find.Selector.SelectedObjects.Contains(subject),"LOD building remains selectable");Find.Selector.ClearSelection();
    }else Check(s==null,"despawned building removed from LOD tracker");
    if(phase==5){Check(jitterMin<.12f&&jitterMax>.12f,"live camera actually crosses nominal threshold: "+jitterMin+".."+jitterMax);Check(Tracker.Switches==stableSwitches,"camera boundary jitter produces zero LOD switches");}
    ScreenCapture.CaptureScreenshot(Path.Combine(Out,names[phase]+".png"));Note("CAPTURE "+names[phase]+" switches="+Tracker.Switches+" dirty="+Tracker.DirtyRequests);pending=true;next=Time.realtimeSinceStartup+2;
   }else{pending=false;next=Time.realtimeSinceStartup+(phase==4?9:6);Advance();}
  }catch(Exception e){failed=true;File.WriteAllText(Path.Combine(Out,"failed.txt"),e.ToString());Note(e.ToString());Application.Quit();}
 }
 public override void GameComponentOnGUI(){if(!IsRun||!ready)return;Text.Font=GameFont.Small;Widgets.Label(new Rect(20,18,1700,30),"IRME LOD | left: 44-triangle control | right: automatic 44 / 28 / 12 | "+names[Math.Min(phase,names.Length-1)]);}
}

public class IsoRimNormalMaterialAcceptance:GameComponent {
 public IsoRimNormalMaterialAcceptance(Game game){}
 bool ready,failed,pending;int frames,phase;float next;Thing normal,flat;Vector3 center;
 string Out=>GenFilePaths.SaveDataFolderPath;
 static bool reloading;
 string[] names={"01_day_close","02_rotated","03_evening","04_indoor_night","05_fogged","06_restored","07_reloaded","08_play_zoom","09_roof_occlusion","10_roof_hidden"};
 void Note(string text){File.AppendAllText(Path.Combine(Out,"acceptance.txt"),text+"\n");Log.Message("[IRME-Test] "+text);}
 void Check(bool value,string text){if(!value)throw new Exception("FAIL: "+text);Note("PASS: "+text);}
 Thing Spawn(string def,IntVec3 cell,Rot4 rot){return GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(def)),cell,Find.CurrentMap,rot);}
 void Hour(float hour){var tm=Find.TickManager;int delta=(int)((hour-GenLocalDate.HourFloat(Find.CurrentMap))*2500);tm.DebugSetTicksGame(tm.TicksGame+delta);}
 void Dirty(){Find.CurrentMap.mapDrawer.RegenerateEverythingNow();}
 void Setup(){
  if(!Out.Replace('\\','/').Contains("/material-run"))throw new Exception("Refusing non-isolated profile");
  var map=Find.CurrentMap;Find.TickManager.Pause();DebugSettings.enableStoryteller=false;
  center=map.Center.ToVector3Shifted();
  if(!reloading){
   foreach(var p in map.mapPawns.AllPawnsSpawned.ToList())p.Destroy();
   foreach(var cell in CellRect.CenteredOn(map.Center,18).Cells){foreach(var t in cell.GetThingList(map).ToList())if(t.def.destroyable)t.Destroy();map.roofGrid.SetRoof(cell,null);map.terrainGrid.SetTerrain(cell,TerrainDefOf.Concrete);}
   map.fogGrid.ClearAllFog();
   flat=Spawn("IRME_FlatCrate",map.Center+new IntVec3(-2,0,2),Rot4.North);
   normal=Spawn("IRME_NormalCrate",map.Center+new IntVec3(2,0,-2),Rot4.North);
   Spawn("TorchLamp",map.Center+new IntVec3(5,0,0),Rot4.North);
   Hour(9);
  }else{
   normal=map.listerThings.AllThings.Single(t=>t.def.defName=="IRME_NormalCrate");flat=map.listerThings.AllThings.Single(t=>t.def.defName=="IRME_FlatCrate");phase=6;
   Check(normal.HitPoints==137&&normal.Rotation==Rot4.East,"save/load preserves normal building HP and rotation");
  }
  Api.Dev.DebrisClear();IsoRimMod.Settings.isometric=true;IsoRimMod.Settings.isoPitch=32;IsoRimMod.Settings.freeYaw=0;IsoRimMod.Settings.turn=0;IsoRimMod.Settings.texturedModels=true;View3D.Active=true;
  Find.CameraDriver.SetRootPosAndSize(center,7f);Dirty();ready=true;next=Time.realtimeSinceStartup+12;
  Check(Api.Version==60,"API 60 + pending extension candidate loaded");
  Check(Bootstrap.Enabled && !typeof(Bootstrap).Assembly.GetReferencedAssemblies().Any(a=>a.Name=="0Harmony"),"API-only addon enabled and has no Harmony assembly reference");
  Check(HarmonyLib.Harmony.GetPatchInfo(HarmonyLib.AccessTools.Method(typeof(ModelBaker),"Write"))==null && HarmonyLib.Harmony.GetPatchInfo(HarmonyLib.AccessTools.Method(typeof(ObjLoader),"Load"))==null,"native model loader and writer have no Harmony patches");
  Check(normal is Building&&normal.Spawned,"normal sample is a regular spawned Building");
  Check(normal.def.blueprintDef!=null&&normal.def.frameDef!=null,"normal building has construction blueprint and frame definitions");
  Check(R3DMaterials.LitTexture("IRME/Crate_BaseColor",Color.white).shader!=DefDatabase<ShaderDef>.GetNamed("IRME_NormalMapped").Shader,"unmapped/native material requests retain original shader");
  Find.Selector.Select(normal);Check(Find.Selector.SelectedObjects.Contains(normal),"building can be selected");Find.Selector.ClearSelection();
  Note("READY loadedModels="+NormalMaterialStore.LoadedModels+" baked="+NormalMaterialStore.MaterialRequests+" uploaded="+NormalMaterialStore.MaterialRequests);
 }
 void Advance(){
  phase++;var map=Find.CurrentMap;
  if(phase==1){normal.Rotation=Rot4.East;flat.Rotation=Rot4.East;normal.HitPoints=137;Dirty();}
  if(phase==2){Hour(17);Dirty();}
  if(phase==3){foreach(var c in CellRect.CenteredOn(map.Center,7).Cells)map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);Hour(0);Dirty();}
  if(phase==4){foreach(var c in CellRect.CenteredOn(map.Center,7).Cells)map.roofGrid.SetRoof(c,null);map.fogGrid.Refog(CellRect.CenteredOn(normal.Position,3));Dirty();}
  if(phase==5){map.fogGrid.ClearAllFog();Hour(9);Dirty();}
  if(phase==6){
   Check(NormalMaterialStore.LoadedModels>=2&&NormalMaterialStore.MaterialRequests>0&&NormalMaterialStore.MaterialRequests>0,"extension loaded basis and uploaded regular section meshes");
   GameDataSaveLoader.SaveGame("IRME_Acceptance");Check(File.Exists(Path.Combine(Out,"Saves","IRME_Acceptance.rws")),"isolated save created");
   reloading=true;GameDataSaveLoader.LoadGame("IRME_Acceptance");ready=false;frames=-100;return;
  }
  if(phase==7)Find.CameraDriver.SetRootPosAndSize(center,22f);
  if(phase==8){Find.CameraDriver.SetRootPosAndSize(center,7f);foreach(var c in CellRect.CenteredOn(map.Center,7).Cells)map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);IsoRimMod.Settings.showRoofs=true;IsoRimMod.Settings.cutHover=false;IsoRimMod.Settings.cutRooms=false;Dirty();}
  if(phase==9){IsoRimMod.Settings.showRoofs=false;Dirty();}
  if(phase>=names.Length){File.WriteAllText(Path.Combine(Out,"completed.txt"),"Runtime assertions passed. Visual acceptance is recorded separately.");Note("COMPLETE");Application.Quit();}
 }
 public override void GameComponentUpdate(){
  if(!Environment.GetCommandLineArgs().Contains("-isorim-normal-material-test") || !Out.Replace('\\','/').Contains("/material-run"))return;
  if(failed)return;
  try{
   if(!ready){if(Current.ProgramState==ProgramState.Playing&&Find.CurrentMap!=null&&++frames>50)Setup();return;}
   Api.Dev.DebrisClear();
   if(Time.realtimeSinceStartup<next)return;
   if(!pending){ScreenCapture.CaptureScreenshot(Path.Combine(Out,names[phase]+".png"));Note("CAPTURE "+names[phase]+" hour="+GenLocalDate.HourFloat(Find.CurrentMap)+" sun="+R3DMaterials.SunDir);pending=true;next=Time.realtimeSinceStartup+2;}
   else{pending=false;next=Time.realtimeSinceStartup+5;Advance();}
  }catch(Exception e){failed=true;File.WriteAllText(Path.Combine(Out,"failed.txt"),e.ToString());Note(e.ToString());Application.Quit();}
 }
 public override void GameComponentOnGUI(){if(!Environment.GetCommandLineArgs().Contains("-isorim-normal-material-test")||!ready)return;Text.Font=GameFont.Small;GUI.color=Color.white;Widgets.Label(new Rect(20,18,1600,30),"IRME native building acceptance | LEFT: normal strength 0 | RIGHT: baked normal strength 1 | "+names[Math.Min(phase,names.Length-1)]);}
}
