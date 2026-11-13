# 0.4.0 dynamic service and API77 acceptance — 2026-11-14

RimWorld 1.7.5005 rev1082. Exact author host DLL SHA256 D59E865930817D4D386F7CF3D582DFBC39E0046EDDF74F9775F01D0A2896B199, API77, FigureShaderContract1. Production IRME.dll SHA256 D99D19497CCA36DA0B0B86DF40FFE697E6ED9788AC2E7D3B29AFB4095B3127BF. No production code/shader changes after these tests.

## Main-mod tests

- `dynamic-run040-01`: separate game with no VF. Service availability/signatures, no Vehicles/Harmony assembly reference, private material and paint isolation, immutable source, shared linear normal cache, neutral absent AO, missing/relative path fallback, 64 create/release cycles, immediate host light unregistration, idempotent release, original destruction after >=3 frames, deferred queue drained, borrowed resources survive. Three raw captures reviewed: separate colors, baked details, perspective90 and draw flash. This is a crate fixture, not vehicle motion proof.
- `lod-run040-01`: all 13 static phases passed, 44/28/12 geometry and reverse changes, hysteresis, native control, selection, normals/tangents, warning fallback cases, night/roof, save/reload and despawn. 162 tracker checks reported after reload. Near/mid/far raw captures reviewed; expected loss of distant detail, no missing mesh.
- `floor-run040-01`: all 21 three-floor phases passed, 258 upper buildings, hidden-floor projection increment0, lower-floor coarse option, peek fade, actual perspective/floor offset, rotation, night lighting, fog/roof/slab, save/reload and hidden despawn. Raw upper-near, peek, perspective, night and reloaded captures reviewed; patterned fade is intentional. Static update budget still <=2 section invalidations/frame.
- Each run independently verified 732 formal configuration/save files and 3171 original Workshop files unchanged. All game processes exited. Ambient missing-Workshop errors are unrelated; no claim that the entire log is error-free. Deliberate missing/relative texture tests emit warnings only.

## Joint vehicle validation (vehicle chat owns this fixture)

`vehicle-pack-r024-20261114/evidence/workshop-ready.json`: base02 PASS294 checks; enhanced01 PASS302; 5837 protected files unchanged in each. Five appearances, three levels, paint isolation, upgrades, turret/muzzle/recoil/fire, save/reload, eight cameras, automatic near/far selection and despawn material/queue cleanup. Basic shader has no normal sampler and remains LOD0 without IRME. Enhanced materials are IRME-owned. Runtime snapshot DLL and all shader hashes match the main-mod production files.

Vehicle chat reviewed enhanced ZTZ99A/ZBD04A close views and reported retained camouflage/normal details without the old white cannon-root or broad speckling. This main-mod chat directly reviewed its own crate/static/floor captures, and verified the other chat's input manifest and assertion records. Do not extend this evidence to every vehicle mod, dynamic cross-floor motion, full mod stacks, long saves, FPS or VRAM savings.

## Ownership and compatibility

Public service v1 is documented in dynamic-service-contract-20261114.md. Main mod owns enhancement shader, normal/AO cache and private material release. Vehicle adapter owns meshes/base camouflage, VF paint interpretation, transforms/upgrades/gameplay and notifications. Uses only documented IsoRim public APIs; no upstream changes. Version/hash/figure-contract gating retained. Shared cached images are session resources, not a promise of immediate menu eviction.

API77 shader review: native vertex shader unchanged from prior host; applicable fragment owner-floor lighting synchronized, static textured-round handling synchronized, perspective ToEye retained. Dynamic vertex/fragment blocks both include `_IsoFlash`; normal/paint/AO use reserved consumer bindings6..9. Original shader attribution retained.

Build and joint runtime establish this exact candidate, not later rebuilds. Publishing receipts and content hashes are recorded separately in dynamic040-provenance.json and the handoff.
