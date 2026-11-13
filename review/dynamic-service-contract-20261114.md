# Dynamic material / LOD service contract v1 (2026-11-14)

Status: implemented and jointly accepted for 0.4.0; publication state in the handoff. Owner: Material & LOD chat. Vehicle chat owns vehicle-pack 0.2.4; do not modify each other's files.

Assembly `IRME.dll`, public type `RimZrism.Materials.DynamicService`. No reference to Vehicles.dll. Main-thread only. Vehicle pack remains independently loadable without IRME: resolve this type only after checking the enabled mod/assembly, then bind delegates; never put an IRME type in the always-loaded base renderer. Do not redistribute IRME.dll in the vehicle pack.

```csharp
public static int Version { get; } // 1: this service contract, NOT IsoRim API version
public static bool Available { get; } // exact reviewed host hash, API >=64 and FigureShaderContract==1
public static Material CreateMaterial(Material source, string normalPng, string aoPng,
    float normalStrength, bool flipGreen, bool flipV, bool continuousLight, float aoStrength);
public static void ReleaseMaterial(Material material);
public static int SelectLod(float projectedHeight, int current, bool initialized,
    int maxLod, float nearFraction, float farFraction, float hysteresis);
public static void Collect(); // drain deferred destruction; also called by our GameComponent
```

`CreateMaterial` returns a NEW PRIVATE configured material, or null on unavailable host, host materials not ready, invalid/missing normal data or construction failure. Source is read-only and NEVER owned/destroyed/registered by us. Paths are absolute PNG paths; normal required; AO null/empty means neutral white, supplied unreadable AO fails quietly. Normal/AO PNGs are cached by canonical path + linear usage; assets immutable for this game session. Textures created by service are owned by it; source base colour, paint mask and pattern textures remain caller-owned.

The clone uses `IRME_DynamicNormalMapped` shader, then `Api.ConfigureFigureMaterial(..., Textured)`. It preserves source paint/base settings; set vehicle paint parameters on the RETURNED material whenever VF changes. Normal properties and local AO are service-owned. Caller owns mesh and tags it via `Api.MovingMeshTag`, retaining its explicit normal/tangent data. Caller submits through `Api.SubmitFigureMesh` with LightPass, true-space matrix and optional flash. Do not add a separate IsoGlow mark draw. Shader is main-mod-owned; no Vehicle shader dependency.

Paint contract retained from 0.2.3: `_MainTex`, `_Color`, `_PaintMask`, `_PatternTex`, `_PaintOne`, `_PaintTwo`, `_PaintThree`, `_PaintTransform`, `_PaintSpace`, `_PaintMode`. Vehicle interprets VF PatternData and writes those. Dynamic shader uses bindings 6=normal, 7=paint mask, 8=pattern, 9=local AO; IsoRim owns 0..5. Main manages `_NormalTex`, `_NormalParams` (strength, green sign, flipV, continuous light), `_ReliefAOTex`, `_ReliefAOParams`.

`ReleaseMaterial` is idempotent for service-owned instances; calls `Api.ReleaseFigureMaterial` immediately and queues original material destruction after at least 3 frames (already-queued draws remain valid). Never call Unity Destroy on a service material yourself. Call on LOD switch, upgrade/retexture, despawn, disabling adapter and map/game teardown. Do not mutate/draw it after release. Shared normal/AO cache is retained across transient zero users; Collect retires it when it observes a changed Current.Game. The component invokes Collect during play, not while sitting in the main menu. No immediate menu eviction or VRAM-saving claim.

`SelectLod`: maxLod 0..2, near > far > 0, hysteresis 0..0.5 exclusive. Invalid inputs return LOD0 (no red errors). Initializes by near/far; thereafter uses symmetric hysteresis. Result clamped to maxLod. Unavailable service returns 0. Vehicle supplies projection using public Api.WorldToViewport and correct true-world floor offset; skips projection for hidden/fogged vehicles. This service does not query VF or mark static sections dirty.

Required adapter behavior:
- Without available IRME: its own BASIC camouflage material, LOD0 only, no baked normal/AO effect or automatic LOD. Keep base material available for null-return fallback.
- With available IRME: use SelectLod and CreateMaterial; same geometry/transforms/paint rules. Missing enhanced material falls back to BASIC 3D (not invisible). If a lower LOD fails, retry the complete LOD0 part set before native 2D fallback.
- Resolve all required parts before submitting any; avoid partial duplicate draw on failure.
- Vehicle stays responsible for hull/turret/recoil, upgrades, muzzle, gameplay and lifetime triggers.

Reviewed host: Workshop 3819577715 DLL SHA256 `D59E865930817D4D386F7CF3D582DFBC39E0046EDDF74F9775F01D0A2896B199`, source API 77, FigureShaderContract 1. Configure/Submit/Visible/MovingMeshTag/RetireMesh from API62; ReleaseFigureMaterial from API64. No new upstream API requested for this contract. DLL gate retained, shader comparison and runtime acceptance recorded separately.

Implementation/progress/acceptance: `review/dynamic-service-handoff-20261114.md`. Joint vehicle runtime and final publication owned/coordinated by vehicle chat; no standalone Steam upload before joint acceptance.
