# IsoRimZ floor candidate — local validation

Tested 2026-11-07 on RimWorld 1.7.5000 rev1275 / Waylith / Windows. This supersedes the original candidate's exclusion of IsoRimZ for ordinary opaque static buildings. Dynamic models, vehicles, pawns, moving doors and translucent/mirrored geometry are not accepted by this test.

## Implementation and ownership

The independent addon consumes public model/material APIs and `ReadStaticModelView`. Its production assembly has neither a Harmony nor a ProjectZ reference. The host supplies row visibility, viewed floor, true-space floor offset and explicit per-material fade opt-in. The addon owns shader code, asset/basis validation, LOD policy and scheduling. Internal shader attributes, lighting uniforms and clipping tags are not a stable API; the exact host DLL compatibility gate remains enabled. The maintainer assigns the API version and final file/document organization at merge.

Viewed-floor buildings use screen projection and hysteresis. Visible lower floors default to their last configured LOD (LOD2 for the three-level example); the user can disable this policy. Hidden rows skip per-building LOD projections and switches, retaining caches. Peek/fade-visible floors stay eligible. Native hidden-floor shadows remain unchanged. There is no texture eviction, zero-total-CPU claim, GPU timing or FPS claim.

## Final runs

- `floor-run04-final`: all 21 phases passed on an actual three-floor ProjectZ map. The fixture verifies **258 non-overlapping upper-floor buildings** remain spawned. Hidden upper buildings initially have zero projections; camera changes while hidden produce **projectionDelta=0**. Ground and upper normal/tangent arrays, native floor offset/projection agreement, near/mid/far/return, lower-quality toggle, actual perspective camera, rotation, torch/night, fog, roof/slab cover and restoration, fade clone/normal-texture retention, HP/rotation save/reload, and hidden despawn cleanup passed. [Raw assertions](floors/floor-acceptance.txt).
- `lod-run05-floors-regression`: ordinary no-floor LOD regression passed, including 44/28/12 triangles, return/jitter, callback fault handling, invalid basis rejection, selection, save/reload, cover/restoration and despawn. Addon section invalidations remained at most two per frame. [Raw assertions](floors/lod-acceptance.txt).
- `compare-unsupported-fresh`: all three camera phases completed on a fresh map. Exactly one unsupported-host warning, no MissingMethodException/TypeLoadException and no IRME error; the configured crate rendered natively and was visually inspected. [Summary](floors/unsupported-host.txt). The earlier historical-save attempt is excluded below.

Scheduler limits: 128 candidate visits per frame, at most two distinct addon-requested dirty sections per frame, 0.25-second ordinary rechecks, and up to 128 lifetime checks per second. These are addon limits, not limits on native game/host redraws. Row visibility is still queried each frame; large reveals settle over budgeted frames. Visible lower rows stop per-building scheduling once settled.

## Visual review and artifacts

Final raw screenshots were inspected for upper/ground placement and detail, peek dithering, fog coverage, slab occlusion and restoration. Selected PNGs in `floors/` are unedited game captures. Full captures/logs remain in the local isolated run directories. Night/perspective captures were also reviewed during the preceding run; the final run independently asserts the perspective flag and coordinate agreement. This is a bounded static-building fixture, not full-stack or long-term gameplay acceptance.

The three bilingual `workshop/details` panels and the cover are AI-edited/generated concept illustrations, not mesh-topology evidence or unedited screenshots. Sample 44/28/12 triangle counts describe the crate only. Prompts and PNG masters are retained; JPEG copies in `mod/About/Details` are publishing assets, not an automatic in-game gallery.

## Protection and excluded exploratory runs

`floor-run04-final`: all 648 formal config/save hashes and 3040 original Workshop files unchanged. During the subsequent no-floor regression the formal save directory changed (37 missing paths, five changed Autosaves); the user confirmed switching Steam accounts. The isolated acceptance save was created under the project validation profile. No restoration or write to formal saves was performed by this task. Workshop hashes remained unchanged. Subsequent protection snapshots use the new account state; private file lists and saves are not published.

Exploratory runs are not substituted for final evidence: run01 had a two-floor fixture; run02's closely spaced crates destroyed one another and its camera was not actually perspective; run03 corrected these but exposed too-slow hidden-object maintenance at 16 checks/second. The final build uses 128 checks/second and passed the corrected fixture. `compare-unsupported-floors` emitted the intended single unsupported-host warning, but loading an older acceptance save failed on unavailable DLC definitions; that run is excluded from rendering acceptance. A fresh isolated map avoids that historical-save dependency.

Ambient Workshop metadata/config errors exist in the environment; this report does not claim the whole game log contains zero errors. Extension failures deliberately retain native drawing with warnings, not `Log.Error`.

## Reproduce

Adapt local game/Workshop paths, then run `scripts/Build.ps1`. Run `scripts/Launch.ps1 -Mode floor -RunName <unique-name>` and wait for `completed.txt`; inspect `floor-acceptance.txt` and screenshots. Repeat with `-Mode lod`. For unsupported-host smoke use `-Mode comparison -FreshComparison -HostDll <unmodified-upstream-DLL> -RunName compare-<unique-name>`. Run `scripts/VerifyProtected.ps1` after each exit. Never enable the test harness in a real save.

DLL hashes and archive provenance: [floor-provenance.json](floor-provenance.json).
