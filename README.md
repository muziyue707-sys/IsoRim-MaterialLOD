# IsoRim Material and LOD 0.3.2

作者 / Author：**水表狂魔**（GitHub: muziyue707-sys）。

**公开版本 0.3.2 / Public release 0.3.2** — [Steam 创意工坊 / Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3835669507) · [下载发布包 / Download package](packages/IsoRim-MaterialLOD-0.3.2-release.zip) · [安装说明 / Installation](API61-INSTALL.md)

工坊 IsoRim 已更新到本版适配的 API 61，DLL 与通过运行验证的版本一致。直接订阅并更新 IsoRim Waylith 和本扩展即可，无需手动安装 GitHub 版。若此前安装过本地 IsoRim，请停用重复副本。模型包仍需明确配置法线材质和 LOD；本框架不会自动替换全部建筑。

中文／English 发布素材：主 Mod 与示例包的 About 提供双语说明；[workshop/locales/](workshop/locales/) 分别维护简体中文与英文工坊标题、正文，由 Steam 按语言偏好显示；封面和详情图共用。实际工坊编号见 mod/About/PublishedFileId.txt。Separate English and Simplified Chinese Workshop metadata follows the visitor's Steam language preference. The real Workshop ID is retained in mod/About/PublishedFileId.txt; publication receipts and remote verification are recorded separately.

Independent RimWorld 1.7 addon for static building normal maps and two/three-level LOD. All material defs, IMB1 sidecar parsing, PNG loading, shaders, caches, thresholds, hysteresis and scheduling live here. Uses documented IsoRim API only: no Harmony reference, patches or private reflection.

This release supports ordinary static buildings on IsoRimZ ground and upper floors. The viewed floor uses screen-size LOD; visible lower floors default to the lowest supplied LOD (optional in Mod settings). Hidden rows suspend LOD projection/switching; peeked floors remain active. See [FLOORS.md](FLOORS.md) for policy, cache and scheduling boundaries, and [review/api61-validation.md](review/api61-validation.md) for current evidence.

Requires the author's merged **API 61** build at `peren717/IsoRim-1.7@75e80b98e19cb2b754d40cf6bb908dfd2a28277d`. The API is now in `Source/Api61.cs` and `Docs/Api.md` part 42. As rechecked on 2026-11-08, updated Workshop IsoRim now supplies API 61 with the exact runtime-tested DLL and matching runtime resources (text line endings aside). This release supports Workshop installation; no manual GitHub installation is required. See [API61-INSTALL.md](API61-INSTALL.md). The exact published host DLL hash remains enforced; mismatches warn and disable the addon.

## 0.3.2 shader fix / 着色器修复

同步 IsoRim `3a5697e` 的逐像素眼睛方向修复，避免透视和第一人称视角下侧面法线不稳定翻转造成斑点；轮廓光使用相同方向。正交视角保留原逻辑，API、参数绑定、LOD 策略和资产格式不变。

Ports IsoRim `3a5697e` to the addon shader: face-normal orientation and rim lighting use the per-fragment eye direction in perspective and first person. Orthographic behavior, API bindings, LOD policy and asset formats remain unchanged. See [validation](review/shader032-validation.md).

## Build and install

Build against the author's actual released DLL, not a separately compiled lookalike: `scripts/Build.ps1 -PrebuiltHostDll <IsoRim/1.7/Assemblies/IsoRim.dll>`. PowerShell 7/Roslyn and local game references are required. This regenerates the exact hash pin, compiles the independent addon and isolated tests. Use `-IsoRimSource` for source builds when deliberately testing another host. Game/host assemblies are never redistributed.

The 0.3.2 package targets API 61. Older 0.3.0 archives remain historical candidates. [Current validation](review/api61-validation.md) describes acceptance against the author's published binary. The host's row view is now read-only and can report invisible until its first rendered frame; the addon keeps asking on later updates.

Install the matching author-published host separately, then mod/ as a mod. example/ is the optional 44/28/12-triangle crate pack. Replace the old 0.2 addon if installed; both use packageId shuib.rimzrism.materials. tests/ is a destructive, isolated-profile-only acceptance harness, excluded from the release mod.

Configuration retains RimZrism.Materials.MaterialDef and RimZrism.Materials.LodDef. See example/Defs, MATERIALS.md and LOD.md. Normal PNGs are linear data, Blender/OpenGL +Y by default. flipNormalV controls row orientation independently of flipGreen.

## Limits and attribution

Ordinary immutable static OBJ buildings only. No automatic decimation, crossfade, texture eviction, shadow LOD, full PBR, or moving vehicles/doors/characters. LOD state is not saved; the optional lower-floor quality setting is stored in the mod configuration. VALIDATION.md records the original candidate's gates; review/floor-validation.md records the floor-support revision. Triangle reduction and skipped LOD work are not FPS results.

Custom shaders remain this addon's responsibility. IsoRim does not promise a stable internal shader ABI (vertex attributes, lighting uniforms or clipping tags). A shader update may require adaptation and revalidation. The exact host hash guard remains; mismatch and rejected asset data warn and retain native rendering rather than reporting red errors.

Shaders derive from current IsoRim IsoRimTex.vert.glsl and IsoRimSolid.frag.glsl. MIT notices are preserved in mod/LICENSE-IsoRim.txt. New code is MIT. The example reuses the project's previously baked crate; no Blender assets were changed for this refactor.
