# API 61 安装说明 / Installation

Historical 0.3.2 installation record. For the 0.4.0 API77 candidate use [INSTALL.md](INSTALL.md).

作者 / Author: **水表狂魔**. Addon version **0.3.2**.

## 中文

本条目已公开发布，可在创意工坊订阅。2026-11-08 已核对更新后的 IsoRim 工坊版：API 61，DLL 与通过运行验证的作者发布版完全一致。**现在可直接使用工坊版，无需手动安装 GitHub 版。**

1. 订阅并更新 [IsoRim Waylith](https://steamcommunity.com/sharedfiles/filedetails/?id=3819577715) 和[本扩展](https://steamcommunity.com/sharedfiles/filedetails/?id=3835669507)，等待 Steam 下载完成后重新启动游戏。
2. 启用更新后的工坊 IsoRim。如果此前手动安装过 GitHub 版，停用同 packageId 的本地副本；不要同时启用两份 IsoRim。
3. 确认已安装 Harmony。
4. 顺序：Harmony → 工坊 IsoRim API 61 → 本扩展 → 已适配的模型包。IsoRimZ 是可选项，使用楼层时加载它。
5. 本扩展是框架，默认不会替换所有建筑。箱体示例和模型包格式见源码仓库。

本版校验作者发布的 IsoRim.dll SHA256：`3A22565D697E78A5D15210C7B861F0FEBFD80C62C8D9D409D6529E2F990B1861`。其他构建即使也标 API 61，仍可能因为散列不匹配停用。不要删除校验来强行启用；作者以后更新着色器或 DLL 后，需要重新适配和验证。停用只保留原生画法并记录警告。

## English

This item is publicly available on Steam Workshop. On 2026-11-08, updated Workshop IsoRim was verified as API 61, with a DLL identical to the author's runtime-tested release. **The Workshop version is now supported; manual GitHub installation is no longer required.**

Subscribe to and update [IsoRim Waylith](https://steamcommunity.com/sharedfiles/filedetails/?id=3819577715) and [this addon](https://steamcommunity.com/sharedfiles/filedetails/?id=3835669507). Wait for Steam downloads to finish, then restart the game. Enable Workshop IsoRim. If you previously installed a local GitHub copy, disable that duplicate; do not enable both copies.

Load Harmony, Workshop IsoRim API 61, this addon, then compatible model packs. IsoRimZ is optional. The addon is a framework and does not automatically replace all buildings. Its exact host-DLL hash is listed above; mismatches warn and retain native drawing. Do not remove the compatibility check. A later host/shader update requires adaptation and revalidation.
