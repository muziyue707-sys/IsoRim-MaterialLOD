# Dynamic materials and LOD (service v1)

0.4.0 adds a generic dynamic enhancement service alongside the existing static-building API. It has no Vehicle Framework dependency and uses IsoRim's documented API62/64 figure submission/material contract. This candidate targets the reviewed API77 DLL; joint vehicle acceptance and publication are tracked in `review/dynamic-service-handoff-20261114.md`.

The canonical callable signatures and parameter ownership are in [the v1 contract](review/dynamic-service-contract-20261114.md).

## Integration

1. Keep a basic renderer in the consuming model pack: its own camouflage/base material, mesh cache and LOD0. Do not reference IRME in its always-loaded types; an optional adapter may resolve the public service and bind delegates only when its assembly is available. Do not ship another copy of IRME.dll.
2. Check `DynamicService.Version == 1` and `Available`. Use the shared `SelectLod` policy only when the service is available. Feed projected true-world size after applying the correct floor offset. Hidden objects should skip projection and drawing; policy choices for visible lower floors belong to the consumer.
3. Pass a read-only base material and absolute normal/AO PNG paths to `CreateMaterial`. A null result means keep the base 3D rendering. Missing required parts must be resolved before any submission; fall back to complete LOD0 if a selected lower level is unavailable.
4. The returned material is private to that object/part. Apply per-object camouflage parameters to it. The source material, base texture, paint masks and pattern textures remain consumer-owned. Normal/AO textures are shared, linear and service-owned.
5. Meshes must supply matching explicit normals/tangents, UVs and the public `Api.MovingMeshTag`. Submit using `Api.SubmitFigureMesh` with true-space matrices and `LightPass`. The host submits its light pass: do not draw a second mark manually.
6. Release every enhanced material via `ReleaseMaterial` on LOD/asset changes, despawn, teardown or adapter disable. It unregisters host light/fade copies immediately and destroys the original after at least three frames. Never destroy a returned material directly. The consumer retains responsibility for its meshes (`Api.RetireMesh`) and base materials (`Api.ReleaseFigureMaterial` then safe delayed destruction if configured).

There is no VF paint/upgrade/turret interpretation here. Body/turret/recoil matrices, muzzle positions, transitions, gameplay and lifecycle notifications stay in the consuming adapter. Static selectors continue to use map-section rebuilding; dynamic LOD does not invalidate static sections.

## Ownership and limits

Normal/AO cache keys are canonical absolute paths. Files are immutable during a game session. Transient zero users do not cause texture eviction, avoiding LOD-switch reload churn. `Collect` drains retired objects and clears the previous session's textures when it observes a different `Current.Game`. A GameComponent calls it during play; consumers may also call it after teardown. Immediate main-menu texture eviction is not promised.

`LiveMaterials`, `CachedTextures` and `PendingDestruction` expose counters for diagnostics; they do not imply measured GPU memory. Fewer triangles, shared code or shared textures are not FPS claims. SubmitFigureMesh currently never batches these draws. No automatic simplification, full PBR, shadow LOD or crossfade is added.

Compatibility requires the reviewed DLL hash, API >=64 and `FigureShaderContract == 1`. API77 is the current reviewed build. The contract stabilizes listed bindings, not copied lighting/clipping code; shader source differences must still be reviewed on updates.

## Shader provenance

The dynamic shader's rigid normal/tangent, triplanar paint and local AO implementation derives from this project's vehicle-pack 0.2.3 shader (author 水表狂魔), itself based on IsoRim under MIT. The original IsoRim notice is retained. 0.4.0 synchronizes owner-floor lighting, keeps ToEye perspective normals, and adds the public `_IsoFlash` value to matching vertex/fragment uniform layouts. Static shader receives applicable API77 round-texture/owner-floor corrections. Base camouflage shaders remain the consuming pack's responsibility.
