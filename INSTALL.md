# 安装 / Installation — 0.4.0

作者 / Author: 水表狂魔。0.4.0 已完成独立及载具联合验收，见 [acceptance](review/dynamic040-validation.md)。

使用配套工坊 IsoRim 和本扩展，无需手动安装另一个 GitHub 版 IsoRim。停用同 packageId 的重复本地副本。顺序：Harmony → IsoRim → 本扩展 → 主动接入的模型包；使用楼层时加 IsoRimZ。

本版针对 IsoRim API77 / FigureShaderContract 1，精确 DLL SHA256：`D59E865930817D4D386F7CF3D582DFBC39E0046EDDF74F9775F01D0A2896B199`。不匹配时警告并停用增强；不删除校验强行运行。兼容载具包在扩展缺失或不可用时继续基础迷彩 3D、固定 LOD0。

Use the matching Workshop IsoRim and this addon. Disable duplicate local copies; no separate GitHub host installation is required. Load Harmony, IsoRim, this addon, then integrated model packs. Add IsoRimZ only when using floors. Exact host hash and figure shader contract are checked. An unavailable enhancement service must preserve the compatible vehicle pack's basic camouflage 3D at LOD0.

Subscribe through Workshop, or install `mod/` locally. Do not enable both local and subscribed copies. The separate `example/` is optional and does not install vehicle models. Normal/LOD assets and an explicit adapter are required.
