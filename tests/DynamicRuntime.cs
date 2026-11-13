using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IsoRim;
using RimWorld;
using UnityEngine;
using Verse;
using RimZrism.Materials;

// Isolated service test without Vehicles.dll. Host internals below are inspection-only tests.
public sealed class IrmeDynamicAcceptance : GameComponent
{
    public IrmeDynamicAcceptance(Game game) { }
    int frames, phase, releasedAt; bool ready, failed; float next;
    Mesh mesh; Material source, first, second, disposed; Texture2D color, mask;
    Vector3 center;
    string Out => GenFilePaths.SaveDataFolderPath;
    string AssetRoot => LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "shuib.rimzrism.materials.example").RootDir;
    void Check(bool value, string text)
    {
        if (!value) throw new Exception(text);
        File.AppendAllText(Path.Combine(Out, "dynamic-acceptance.txt"), "PASS " + text + "\n");
    }
    Texture2D Pixel(Color c)
    {
        var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t;
    }
    Material Make(Material original, string path = null) => DynamicService.CreateMaterial(original,
        path ?? Path.Combine(AssetRoot, "Textures/IRME/Crate_Normal.png"), null, 1, false, true, true, .55f);
    void Policy()
    {
        Check(DynamicService.Version == 1 && DynamicService.Available, "contract v1 / exact host gate active");
        Check(!typeof(DynamicService).Assembly.GetReferencedAssemblies().Any(a => a.Name == "Vehicles" || a.Name == "0Harmony"), "production assembly has no Vehicles or Harmony reference");
        Func<float,int,bool,int,int> pick = (h,c,i,max) => DynamicService.SelectLod(h,c,i,max,.12f,.045f,.15f);
        Check(pick(.2f,0,false,2)==0 && pick(.08f,0,false,2)==1 && pick(.02f,0,false,2)==2, "initial three LOD levels");
        Check(pick(.11f,0,true,2)==0 && pick(.1f,0,true,2)==1, "near downward hysteresis");
        Check(pick(.13f,1,true,2)==1 && pick(.14f,1,true,2)==0, "near upward hysteresis");
        Check(pick(.04f,1,true,2)==1 && pick(.03f,1,true,2)==2 && pick(.05f,2,true,2)==2 && pick(.06f,2,true,2)==1, "far hysteresis both directions");
        Check(pick(.001f,0,false,0)==0 && pick(.001f,0,false,1)==1, "one / two level clamp");
        Check(pick(float.NaN,2,true,2)==0 && pick(float.PositiveInfinity,2,true,2)==0 && pick(-1,2,true,2)==0, "invalid projection safe LOD0");
        Check(DynamicService.SelectLod(.1f,2,true,2,.04f,.12f,.15f)==0, "invalid thresholds safe LOD0");
        Check(DynamicService.SelectLod(.1f,2,true,2,.12f,.04f,.5f)==0, "invalid hysteresis safe LOD0");
    }
    void LoadMesh()
    {
        using var r = new BinaryReader(File.OpenRead(Path.Combine(AssetRoot,"Models/Crate_Normal.obj.irbasis")));
        Check(r.ReadUInt32()==0x31424D49,"IMB1 test fixture");r.ReadBytes(32);int count=r.ReadInt32();
        var ps=new Vector3[count];var ns=new Vector3[count];var ts=new Vector4[count];var uvs=new Vector2[count];
        var tags=new List<Vector3>();var ids=new int[count];var cs=new Color32[count];
        for(int i=0;i<count;i++){
            ps[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
            ns[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
            uvs[i]=new Vector2(r.ReadSingle(),r.ReadSingle());
            ts[i]=new Vector4(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
            tags.Add(Api.MovingMeshTag);ids[i]=i;cs[i]=new Color32(255,255,255,255);
        }
        mesh=new Mesh();mesh.vertices=ps;mesh.normals=ns;mesh.tangents=ts;mesh.uv=uvs;mesh.colors32=cs;mesh.SetUVs(1,tags);mesh.triangles=ids;mesh.RecalculateBounds();
    }
    void Setup()
    {
        Policy();Find.TickManager.Pause();DebugSettings.enableStoryteller=false;
        var map=Find.CurrentMap;center=map.Center.ToVector3Shifted();
        foreach(var c in CellRect.CenteredOn(map.Center,12).Cells){foreach(var t in c.GetThingList(map).ToList())if(t.def.destroyable)t.Destroy();map.roofGrid.SetRoof(c,null);map.terrainGrid.SetTerrain(c,TerrainDefOf.Concrete);}
        map.fogGrid.ClearAllFog();
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame+(int)((10-GenLocalDate.HourFloat(map))*2500));
        Api.View3DActive=true;IsoRimMod.Settings.texturedModels=true;IsoRimMod.Settings.showRoofs=false;
        IsoRimMod.Settings.cutHover=false;IsoRimMod.Settings.cutRooms=false;IsoRimMod.Settings.isometric=true;
        Find.CameraDriver.SetRootPosAndSize(center,5);
        LoadMesh();color=Pixel(Color.white);mask=Pixel(Color.red);
        source=new Material(DefDatabase<ShaderDef>.GetNamed("IRME_NormalMapped").Shader){name="dynamic_test_base",mainTexture=color,color=Color.white,renderQueue=2001};
        source.SetTexture("_PaintMask",mask);source.SetTexture("_PatternTex",Texture2D.whiteTexture);
        source.SetColor("_PaintOne",new Color(.2f,.65f,.3f));source.SetColor("_PaintTwo",Color.white);source.SetColor("_PaintThree",Color.white);
        source.SetVector("_PaintSpace",new Vector4(4,8,0,0));source.SetFloat("_PaintMode",0);
        first=Make(source);second=Make(source);
        Check(first!=null&&second!=null,"two enhanced materials created");
        Check(first!=source&&second!=source&&first!=second,"private material instances");
        Check(first.shader==DefDatabase<ShaderDef>.GetNamed("IRME_DynamicNormalMapped").Shader,"main mod owns dynamic shader");
        Check(source.shader!=first.shader&&source.mainTexture==color,"source shader and texture unchanged");
        Check(first.mainTexture==color&&first.GetTexture("_PaintMask")==mask,"borrowed base and paint textures preserved across shader switch");
        Check(first.GetColor("_PaintOne")==source.GetColor("_PaintOne"),"paint color preserved across shader switch");
        Check(first.GetTexture("_NormalTex")==second.GetTexture("_NormalTex")&&DynamicService.CachedTextures==1,"shared normal cache");
        Check(first.GetTexture("_ReliefAOTex")==Texture2D.whiteTexture,"optional AO white fallback");
        second.SetColor("_PaintOne",new Color(.75f,.25f,.12f));
        Check(first.GetColor("_PaintOne")!=second.GetColor("_PaintOne"),"two instances do not share paint state");
        Check(IsoGlow.MarkFor(first)!=null&&IsoGlow.MarkFor(second)!=null,"public configure creates separate host light passes");
        Check(Make(source,Path.Combine(Out,"missing.png"))==null,"missing normal returns null");
        Check(Make(source,"relative.png")==null,"relative normal path rejected quietly");
        Check(DynamicService.LiveMaterials==2,"failed material constructions not retained");
        for(int i=0;i<64;i++){
            var transient=Make(source);Check(transient!=null,"create/release cycle "+i);
            DynamicService.ReleaseMaterial(transient);DynamicService.ReleaseMaterial(transient);
            Check(IsoGlow.MarkFor(transient)==null,"cycle light registration released "+i);
        }
        Check(DynamicService.LiveMaterials==2&&DynamicService.CachedTextures==1,"64 cycles preserve bounded live materials and shared cache");
        DynamicService.ReleaseMaterial(source);Check(source!=null,"unowned source release is no-op");
        map.mapDrawer.RegenerateEverythingNow();ready=true;next=Time.realtimeSinceStartup+6;
    }
    public override void GameComponentUpdate()
    {
        if(!Environment.GetCommandLineArgs().Contains("-isorim-normal-dynamic-test")||!Out.Replace('\\','/').Contains("/dynamic-run")||failed)return;
        try{
            if(Current.ProgramState!=ProgramState.Playing||Find.CurrentMap==null)return;
            if(!ready){if(++frames>100)Setup();return;}
            Find.WindowStack.Windows.ToList().ForEach(w=>w.Close(false));DynamicService.Collect();
            if(phase<3){
                if(first!=null)Api.SubmitFigureMesh(mesh,first,Matrix4x4.TRS(center+new Vector3(-1.4f,0,0),Quaternion.Euler(0,45,0),Vector3.one));
                if(second!=null)Api.SubmitFigureMesh(mesh,second,Matrix4x4.TRS(center+new Vector3(1.4f,0,0),Quaternion.Euler(0,45,0),Vector3.one),null,FigureMeshFlags.LightPass,0,phase==2?.5f:0);
            }
            if(Time.realtimeSinceStartup<next)return;
            if(phase==0){ScreenCapture.CaptureScreenshot(Path.Combine(Out,"01_private_paint_ortho.png"));phase++;IsoRimMod.Settings.isometric=false;IsoRimMod.Settings.perspective=true;IsoRimMod.Settings.freeYaw=90;next=Time.realtimeSinceStartup+6;}
            else if(phase==1){ScreenCapture.CaptureScreenshot(Path.Combine(Out,"02_perspective90.png"));phase++;next=Time.realtimeSinceStartup+6;}
            else if(phase==2){ScreenCapture.CaptureScreenshot(Path.Combine(Out,"03_flash.png"));disposed=first;DynamicService.ReleaseMaterial(first);DynamicService.ReleaseMaterial(first);first=null;releasedAt=Time.frameCount;Check(IsoGlow.MarkFor(disposed)==null,"release removes host light registration immediately");Check(disposed!=null,"original retained until queued draws finish");phase++;next=Time.realtimeSinceStartup+1;}
            else{
                Check(Time.frameCount-releasedAt>=3&&disposed==null,"original destroyed after frame grace period");
                Check(DynamicService.PendingDestruction==0,"all previous deferred objects collected");
                DynamicService.ReleaseMaterial(second);second=null;
                Check(DynamicService.LiveMaterials==0,"all private materials released");
                Check(source!=null&&color!=null&&mask!=null,"borrowed resources remain alive");
                Api.RetireMesh(mesh);UnityEngine.Object.Destroy(source);UnityEngine.Object.Destroy(color);UnityEngine.Object.Destroy(mask);
                File.WriteAllText(Path.Combine(Out,"completed.txt"),"Dynamic service assertions PASS. No Vehicles loaded; visual review separate.");Application.Quit();
            }
        }catch(Exception e){failed=true;File.WriteAllText(Path.Combine(Out,"failed.txt"),e.ToString());Application.Quit();}
    }
}
