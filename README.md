# IsoRim Material and LOD 0.3.0

Independent RimWorld 1.7 addon for static building normal maps and two/three-level LOD. All material defs, IMB1 sidecar parsing, PNG loading, shaders, caches, thresholds, hysteresis and scheduling live here. Uses documented IsoRim API only: no Harmony reference, patches or private reflection.

Requires the companion static-model-extension API candidate, based on upstream 4cc784a669ef049ba081296ff94f50bf9cc4dd9d (API 60). The maintainer assigns the next version at merge. Stock API 60 is insufficient. scripts/Build.ps1 records the exact candidate DLL SHA256 in Compatibility.cs; unsupported builds log an error and disable the addon. Never remove the pin to force an update.

## Build and install

Run scripts/Build.ps1 with PowerShell 7 containing Roslyn. Parameters: -GameDir <game root> -IsoRimSource <API candidate repository> -HarmonyDll <0Harmony.dll>. It compiles the host outside its repository, generates the pin, builds the addon, then builds the separate tests. Game assemblies are never redistributed. The SDK project is an optional editor project; the verified build route is this script.

API source: [companion branch](https://github.com/muziyue707-sys/IsoRim-1.7/tree/feat/static-model-extension-api). A precompiled addon/example candidate is in [packages/](packages/), pinned to the host build recorded in [VALIDATION.md](VALIDATION.md). Build both projects together if your host binary differs. Stock Workshop IsoRim cannot use these pending APIs yet.

Install the compiled host candidate separately, then mod/ as a mod. example/ is the optional 44/28/12-triangle crate pack. Replace the old 0.2 addon if installed; both use packageId shuib.rimzrism.materials. tests/ is a destructive, isolated-profile-only acceptance harness, excluded from the release mod.

Configuration retains RimZrism.Materials.MaterialDef and RimZrism.Materials.LodDef. See example/Defs, MATERIALS.md and LOD.md. Normal PNGs are linear data, Blender/OpenGL +Y by default. flipNormalV controls row orientation independently of flipGreen.

## Limits and attribution

Ordinary immutable static OBJ buildings only. No automatic decimation, crossfade, texture eviction, shadow LOD, full PBR, moving vehicles/doors/characters or IsoRimZ floor acceptance. The addon writes no save data. VALIDATION.md records the actual gates; triangle reduction is not an FPS result.

Shaders derive from current IsoRim IsoRimTex.vert.glsl and IsoRimSolid.frag.glsl. MIT notices are preserved in mod/LICENSE-IsoRim.txt. New code is MIT. The example reuses the project's previously baked crate; no Blender assets were changed for this refactor.
