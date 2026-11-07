# IsoRim Material and LOD 0.3.0

作者 / Author：**水表狂魔**（GitHub: muziyue707-sys）。

中文／English 发布素材：主 Mod 与示例包的 About 已提供双语说明；[workshop/](workshop/) 包含双语工坊标题、BBCode 正文和封面。这些是本地发布素材，不代表已发布 Steam 工坊条目。The About files and Workshop copy are bilingual; publishing assets do not imply a live Steam listing.

Independent RimWorld 1.7 addon for static building normal maps and two/three-level LOD. All material defs, IMB1 sidecar parsing, PNG loading, shaders, caches, thresholds, hysteresis and scheduling live here. Uses documented IsoRim API only: no Harmony reference, patches or private reflection.

The floor-support candidate includes ordinary static buildings on IsoRimZ ground and upper floors. The viewed floor uses screen-size LOD; visible lower floors default to the lowest supplied LOD (optional in Mod settings). Hidden rows suspend LOD projection/switching; peeked floors remain active. See [FLOORS.md](FLOORS.md) for policy, cache and scheduling boundaries, and [review/floor-validation.md](review/floor-validation.md) for current evidence.

Requires the companion static-model-extension API candidate, based on upstream 4cc784a669ef049ba081296ff94f50bf9cc4dd9d (API 60). The maintainer assigns the next version at merge. Stock API 60 is insufficient. scripts/Build.ps1 records the exact candidate DLL SHA256 in Compatibility.cs; unsupported builds log a warning and disable the addon. Never remove the pin to force an update.

## Build and install

Run scripts/Build.ps1 with PowerShell 7 containing Roslyn. Parameters: -GameDir <game root> -IsoRimSource <API candidate repository> -HarmonyDll <0Harmony.dll>. It compiles the host outside its repository, generates the pin, builds the addon, then builds the separate tests. Game assemblies are never redistributed. The SDK project is an optional editor project; the verified build route is this script.

API source: [companion branch](https://github.com/muziyue707-sys/IsoRim-1.7/tree/feat/static-model-extension-api). A precompiled addon/example candidate is in [packages/](packages/), pinned to the host build recorded in [VALIDATION.md](VALIDATION.md). Build both projects together if your host binary differs. Stock Workshop IsoRim cannot use these pending APIs yet.

The current `-floors.zip` candidate includes floor support and three bilingual detail panels. The older `-bilingual.zip` is retained as a historical metadata-only package.

Install the compiled host candidate separately, then mod/ as a mod. example/ is the optional 44/28/12-triangle crate pack. Replace the old 0.2 addon if installed; both use packageId shuib.rimzrism.materials. tests/ is a destructive, isolated-profile-only acceptance harness, excluded from the release mod.

Configuration retains RimZrism.Materials.MaterialDef and RimZrism.Materials.LodDef. See example/Defs, MATERIALS.md and LOD.md. Normal PNGs are linear data, Blender/OpenGL +Y by default. flipNormalV controls row orientation independently of flipGreen.

## Limits and attribution

Ordinary immutable static OBJ buildings only. No automatic decimation, crossfade, texture eviction, shadow LOD, full PBR, or moving vehicles/doors/characters. LOD state is not saved; the optional lower-floor quality setting is stored in the mod configuration. VALIDATION.md records the original candidate's gates; review/floor-validation.md records the floor-support revision. Triangle reduction and skipped LOD work are not FPS results.

Custom shaders remain this addon's responsibility. IsoRim does not promise a stable internal shader ABI (vertex attributes, lighting uniforms or clipping tags). A shader update may require adaptation and revalidation. The exact host hash guard remains; mismatch and rejected asset data warn and retain native rendering rather than reporting red errors.

Shaders derive from current IsoRim IsoRimTex.vert.glsl and IsoRimSolid.frag.glsl. MIT notices are preserved in mod/LICENSE-IsoRim.txt. New code is MIT. The example reuses the project's previously baked crate; no Blender assets were changed for this refactor.
