# Dynamic service handoff 2026-11-14

Status: 0.4.0 PUBLISHED to original Workshop3835669507; Public. Native english/schinese title and full description verified. Source, release package and evidence are synchronized together in the GitHub commit containing this handoff; see Git history for its SHA.

## Publication receipt

Content submit OK, dependency retained3819577715, three gallery images retained, cover unchanged, owner matches, banned=false. Bytes2932821. `dist/workshop-040-20261114/submit-result.json` and `remote-verification.json`; language receipts `dist/workshop-localization-040-20261114/`; public evidence `review/dynamic040-provenance.json` and `review/workshop-localization-040-20261114.json`. Both localized descriptions match exactly and visibility remainsPublic. Author水表狂魔.

Release `packages/IsoRim-MaterialLOD-0.4.0-release.zip`, 2934529bytes, SHA256688FF99886886A71093D6CC5446169DAE385E0BE5C53F2FE4BD864AC805896F9, CRC PASS, DLL matches the tested and uploaded DLL. No host/Vehicles DLL or saves included. Production IRME.dll D99D19497CCA36DA0B0B86DF40FFE697E6ED9788AC2E7D3B29AFB4095B3127BF unchanged from joint enhanced01. Exact source and all shader hashes in dynamic040-provenance.json.

Contract ready: [dynamic-service-contract-20261114.md](dynamic-service-contract-20261114.md). Vehicle chat can implement optional delegate bridge against public `RimZrism.Materials.DynamicService`, Version 1. Minimal API is Material clone/create, release, LOD selector and collection. Vehicle retains mesh loading/cache, base camouflage material and all VF interpretation. The shared service owns enhanced shader, cached normal/AO textures and private enhanced material lifetime.

Host verified file hash: D59E865930817D4D386F7CF3D582DFBC39E0046EDDF74F9775F01D0A2896B199. API77 source, figure contract1. Current shader diff from API61 adds per-owner-floor lighting and textured round flags in addition to already ported ToEye. Will adapt both static and dynamic shader copies as applicable, retain exact hash gate.

0.3.2 preserved in `dist/baseline-032-20261114/`: release ZIP SHA256 C1F4B99C724C47BA229E0CF9C9708FCDB145A0CD0ABD5B422A88542EA23440F5; committed source archive SHA256 066B1C28B86C4253BC322BDE63F50110564FF925250AF810A5A9A291FF7C181A.

Vehicle integration requirements: enhanced material receives paint parameters; submit once with Api.SubmitFigureMesh LightPass, no manual mark pass; release service-owned material on transition, no direct Destroy; no-IRME keeps base 3D at LOD0. See contract for full details. No files in vehicle-pack changed.

Build: IRME.dll, IRME.Tests.dll, IRME.Comparison.dll PASS against exact author DLL. DynamicService v1 signatures match contract. Candidate DLL: `mod/Assemblies/IRME.dll`; dynamic shaders `mod/Shaders/IRME_Dynamic.vert.glsl` and `.frag.glsl`; ShaderDef `IRME_DynamicNormalMapped`.

`dynamic-run040-01` completed and exited, all service assertions PASS: no VF/Harmony assembly reference, private material/paint isolation, shared normal cache, missing/relative input fallback, 64 create/release cycles, immediate host mark removal, >=3-frame original destruction and borrowed-resource preservation. Three raw screenshots viewed: two separate paint colors and baked top detail in orthographic/perspective90, correct red flash on second object; no large speckled patches. This is a crate service fixture, NOT vehicle acceptance. Protection PASS: 732 formal files +3171 original Workshop files unchanged. Expected two warning cases, ambient unrelated missing Workshop errors remain.

All standalone processes exited. `lod-run040-01`:13 phases PASS; `floor-run040-01`:21 phases PASS. Each protection check PASS732+3171. Near/mid/far static captures and upper/peek/perspective/night/reloaded floor captures visually reviewed. Joint vehicle `base02`294 checks /`enhanced01`302 checks PASS, each5837 protected files unchanged. Verified all five joint runtime DLL/shader hashes match production, including IRME.dll D99D19497CCA36DA0B0B86DF40FFE697E6ED9788AC2E7D3B29AFB4095B3127BF. No production code/shader changes since those runs. Full report: dynamic040-validation.md. Vehicle chat explicitly notified joint acceptance and requested this mod's original Public item3835669507 update; this chat owns that upload only.

## API77 shader patches for vehicle BASIC shader (vehicle chat applies)

Native vertex shader API61 to current API77 has no content diff. Native fragment changes: ToEye (already present in vehicle 0.2.3), textured round flags (irrelevant for the custom vehicle pipeline which uses its own explicit normals), owner-floor night lighting (apply), and existing `_IsoFlash` draw support (missing from the old vehicle shader; add).

Replace the single old floor-glow line:
```glsl
xz.y += clamp(floor((p.y + 0.01) / fg.y), 0.0, fg.x) * fg.z;
```
with:
```glsl
float k = uMaterial._IsoOwnerFloor > 0.5
    ? clamp(uMaterial._IsoOwnerFloor - 1.0, 0.0, fg.x)
    : clamp(floor((p.y + 0.01) / fg.y), 0.0, fg.x);
xz.y += k * fg.z;
```
This keeps a tall moving model in its owner's light cells, rather than sampling the floor above. The host's own light-pass shader already has this change; custom fragment copy should stay aligned.

Add `float _IsoFlash; // @default = 0.0` to BOTH vertex/fragment MaterialUniforms at the identical position (ours inserts it before _NormalParams). After the lightPass early return, immediately before `float mode = uMaterial._LightMode;`, add `color.gb *= 1.0 - clamp(uMaterial._IsoFlash, 0.0, 1.0);`. Pass optional flash to SubmitFigureMesh only if vehicle adapter has a real flash value. Keep ToEye and matching vertex/fragment uniform order.

Resource detail: `Collect` automatically runs during GameComponentUpdate and notices a changed Current.Game. Shared normal/AO textures persist across transient zero references. On a new game session they are queued for destruction with the same 3-frame grace. Main-menu immediate texture eviction is not claimed; consumers can call Collect after their teardown if desired. All service materials must be released by consumer on despawn/upgrade/LOD transitions.
