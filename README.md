# IsoRim Material and LOD 0.4.0

作者 / Author：**水表狂魔**（GitHub: muziyue707-sys）。

**0.4.0 — dynamic material/LOD service and API77 compatibility.** [Current acceptance](review/dynamic040-validation.md) · [Release package](packages/IsoRim-MaterialLOD-0.4.0-release.zip). The 0.3.2 baseline remains in [its historical package](packages/IsoRim-MaterialLOD-0.3.2-release.zip).

[Steam 创意工坊 / Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3835669507) · [安装 / Installation](INSTALL.md) · [动态服务 / Dynamic service](DYNAMIC.md)

为普通静态建筑提供烘焙法线与两／三档 LOD，并为主动接入的动态模型包提供通用法线／局部 AO 材质、缓存、私有材质生命周期和 LOD 选择服务。本 Mod 不依赖 Vehicles.dll，不解释车辆迷彩、炮塔、后坐或升级；这些仍由载具适配包处理。

兼容载具包可独立显示基础迷彩 3D 模型；启用本 Mod 后才使用它提供的烘焙法线效果与自动 LOD。未接入的模型不会自动获得增强效果。本框架不生成模型／法线贴图，不自动减面。

Independent RimWorld 1.7 addon for static building normal maps and two/three-level LOD, plus an optional dynamic material/LOD service for explicitly integrated model packs. No Vehicles.dll dependency, Harmony patches or private reflection in production. Vehicle paint interpretation, motion, upgrades and gameplay remain in the consuming adapter. Without this addon, a compatible vehicle pack retains its own base camouflage 3D renderer at LOD0; with it, baked normal effects and automatic LOD are provided through the optional service.

Static buildings support IsoRimZ ground/upper floors: viewed-floor screen-size LOD, optional lowest supplied LOD on visible lower floors, no hidden-floor projection/switching, and active peeked-floor handling. Dynamic consumers handle their own visibility and lifecycle; the static section scheduler is not used for vehicles.

## Compatibility and build

Current reviewed host: Workshop IsoRim **API77**, DLL SHA256 `D59E865930817D4D386F7CF3D582DFBC39E0046EDDF74F9775F01D0A2896B199`, figure shader contract **1**. Static integration uses API61; dynamic material/drawing uses API62 with API64 release. The exact DLL guard remains. Do not remove it to force a new host update.

Build against the author-published binary:

```powershell
./scripts/Build.ps1 -IsoRimSource <Workshop-IsoRim-root> -PrebuiltHostDll <Workshop-IsoRim-root>/1.7/Assemblies/IsoRim.dll
```

Build.ps1 regenerates the reviewed host pin and compiles the addon and isolated tests. Do not run it against an unreviewed host as a substitute for compatibility validation. No game/host DLL is distributed.

Install `mod/`; `example/` is the optional 44/28/12-triangle crate. `tests/` is an isolated-profile-only harness and is excluded from the production mod. See [MATERIALS.md](MATERIALS.md), [LOD.md](LOD.md), [FLOORS.md](FLOORS.md), and [DYNAMIC.md](DYNAMIC.md). Existing static MaterialDef/LodDef names are unchanged.

## Scope and evidence

0.4.0 service contract and current acceptance: [handoff](review/dynamic-service-handoff-20261114.md). Earlier runtime evidence is historical: [0.3.2 camera/floor regression](review/shader032-validation.md), [API61 acceptance](review/api61-validation.md). Earlier tests do not prove this candidate or a full vehicle integration.

No automatic decimation, full PBR, LOD crossfade, shadow LOD or promised FPS/VRAM gains. Dynamic textures are shared per session, not evicted on every LOD switch. Each private enhanced material is unregistered and safely retired after release. The dynamic draw API does not batch submissions.

Custom shaders remain our responsibility. The public figure shader contract stabilizes its listed names/bindings; copied lighting and clipping code still requires review after host updates. Mismatches and asset rejection warn and preserve the consumer's fallback. Shader MIT notices remain in `mod/LICENSE-IsoRim.txt`; new code is MIT. Dynamic rigid-basis/paint/AO code derives from this project's vehicle-pack 0.2.3, author 水表狂魔.

English and Simplified Chinese Workshop metadata lives in `workshop/locales/`; Steam selects language by visitor preference. Cover/gallery are retained. Publication receipts and exact content hashes are recorded in the handoff.
