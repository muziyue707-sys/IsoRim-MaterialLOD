# Verification, 2026-11-07

RimWorld 1.7.5000 rev1275 / Waylith, Windows. Host source base: `peren717/IsoRim-1.7@4cc784a669ef049ba081296ff94f50bf9cc4dd9d`; candidate branch `feat/static-model-extension-api`. API version remains 60 pending the maintainer's merge-time version assignment.

Tested candidate DLL SHA256: `9CC4D7678413BAAEB9DE707F82F1A9F47E959C49C3962426B5E107571E0AA959`. Baseline built from unchanged upstream: `AC0685811FBE5DE0F22D0B3C22B8E5F223E0175D098B3C8509837F403376AB5E`. Builds are deterministic in this local environment; another build may produce a different hash and must regenerate the consumer pin and repeat acceptance.

## Gates achieved

- Host, addon, acceptance harness and comparison harness compile. Addon assembly has no Harmony reference; production code has no private reflection or IsoRim internal access. Tests deliberately inspect internals to assert the boundary and uploaded attributes.
- IMB1 files match raw OBJ SHA256 and corner counts. Git preserves OBJ bytes. Shader vertex/fragment material uniform layouts match. See `review/static-validation.json`.
- Final LOD run `lod-run04-final`: real section mesh normals/tangents match vertex counts, ordinary XML/import and public LoadModel share identity, 44 -> 28 -> 12 -> 28 -> 44 triangles, two-level policy, invalid policy rejection, live camera hysteresis, selection, save/reload, roof/night/restoration, despawn pruning. Only the initial test setup forces a full rebuild; subsequent switches exercise the product's section invalidation.
- Fault injection: wrong basis lengths and NaN tangents rejected; detached snapshots cannot mutate native geometry; throwing selector and material factory each disabled after one call, native rendering retained.
- Unsupported-host run `compare-unsupported-fixed`: original upstream plus the addon logs the expected hash rejection, loads the same save and completes all three camera phases without MissingMethodException or TypeLoadException. Startup and update calls are kept behind a non-inlined gate; explicit delegate construction avoids compiler-generated static fields that the game scans even for a disabled addon. The initial failing probe is retained locally.
- Final material run `material-run03-final`: ordinary spawned Building, native unmapped material retained, selection, rotation, sun direction, indoor torch at night, fog cover/restoration, roof cover/restoration, normal play zoom, HP and rotation retained after save/reload. Blueprint/frame definitions exist; a colonist construction job was not tested. Model loader/writer have no Harmony patches.
- Visual review: baked detail on the right-hand crate changes shading under rotation and light changes; left is the strength-zero control. Roof and fog coverage were inspected. Raw captures are retained locally; selected unedited captures are in `review/`.
- Each isolated run checked 648 formal config/save files and 3040 original Workshop files. None changed or disappeared. The full hashes and raw logs remain local; no user saves/configs are published.

Raw assertion transcripts are in `review/lod-acceptance.txt` and `review/material-acceptance.txt`. The latter's legacy `uploaded` label counts material requests, not GPU uploads; the separate LOD assertions inspect actual mesh arrays.

## Reproduction

Build with `scripts/Build.ps1` (PowerShell 7/Roslyn; pass your game directory, Harmony DLL and candidate source path). The isolated launch helper uses this workstation's paths: review and adapt them before use. Run `scripts/Launch.ps1 -Mode material -RunName material-run01`, wait for `completed.txt`, then `-Mode lod -RunName lod-run01`. Never enable `tests/` in a real save. Run `scripts/VerifyProtected.ps1 -RunName <name>` after exit.

For comparisons, build unchanged upstream with `-HostOnly -HostOutput build-baseline`, then use `-Mode comparison -RunName compare-original -Baseline -HostDll <baseline DLL>`, `compare-unused -Baseline` with the candidate, and `compare-addon` with the candidate/addon. The helper copies the material test's save into each isolated profile. The comparison closes incidental test windows, uses one save, pauses simulation, sets pitch 32/yaw 0 and samples camera sizes 7/13/40 after five seconds of settling. It records 360 GameComponentUpdate frame intervals per view. These are elapsed wall-clock intervals, not GPU timings.

## Limits

Static ordinary textured buildings only. Vehicles, turret/door/character animation, generated/cut/merged copies, IsoRimZ floors, full Mod stacks, long saves and large-scene performance are not accepted by these tests. No full PBR, automatic mesh simplification, texture unloading or FPS improvement is claimed. Original geometry continues to determine shadows and gameplay footprint.

The first comparison captures were obstructed by an automatically opened debug window and are excluded from visual evidence. Their timing traces remain local, including an addon far-view spike (mean 10.6007 ms, p95 24.7451 ms). The corrected repeat and its limitations are recorded in `review/comparison.md`; no performance conclusion is drawn from either single pass.

Baseline and candidate logs both enumerate unrelated installed Workshop metadata errors (missing item 3318744782; RimMind's unsupported picture field), plus dependency metadata warnings. Deliberate failure warnings are marked EXPECTED in the LOD test. This is not an assertion that the whole game log is error-free.
