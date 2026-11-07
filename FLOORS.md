# 楼层与 LOD / Floor-aware LOD

本扩展通过 IsoRim 公开 API 处理普通静态建筑，不直接依赖 IsoRimZ 的程序集，不添加 Harmony 补丁或内部反射。IsoRim 负责真实楼层布局、绘制变换、裁切、淡出和阴影；子 Mod 负责材质和 LOD 策略。

| 状态 / State | 默认策略 / Default |
| --- | --- |
| 当前查看层 / Viewed floor | 按屏幕占比和滞回选择 / Screen-size LOD with hysteresis |
| 可见下层 / Visible lower floors | 使用已配置的最远档；可在设置中关闭 / Lowest supplied LOD; optional in Mod settings |
| 隐藏层 / Hidden floors | 暂停投影与档位切换，保留已有缓存 / Suspend projection and switching, keep caches |
| 窥视淡出层 / Peeked or faded floors | 属于可见层，继续按屏幕大小更新 / Visible; continue screen-size LOD |

三档模型的最远档是 **LOD2（第三档）**，不是新增第四档 LOD3。两档模型使用 LOD1。没有配置 LOD 的建筑不会被自动减面。

## 调度与缓存 / Scheduling and caches

按实际地图行归组，每帧只为每个已占用行读取一次楼层状态。隐藏行不进入逐建筑 LOD 循环；可见下层达到固定最远档后，也会整行休眠。这个小规模显隐查询仍然存在，不是“隐藏层完全零 CPU”。每秒最多检查 128 个跟踪对象的存活状态，逐步释放被销毁或移出地图的条目。

楼层重新显示、成为当前层、偏移改变或用户切换下层选项时，行状态变化会取消旧的检查等待时间，重新选择档位。每帧最多处理 128 个候选状态，每次常规投影检查间隔 0.25 秒；最多请求两个不同区块失效。大量建筑重新显示时按预算逐步恢复，可能短暂保留原档位，不承诺整层同一帧完成切换。

Rows are queried once per update and hidden rows skip per-building LOD work entirely. Settled lower rows sleep until their view/policy changes. Bounded maintenance checks up to 128 object lifetimes per second. Visibility, offset or policy changes make affected rows eligible immediately, while the 128-state / two-section-per-frame limits still apply. Large reveals converge over subsequent frames rather than forcing a full-map rebuild.

完全隐藏的层不为了“降到最低档”额外重建；游戏因正常原因首次打印隐藏建筑时，可直接采用最远档。共享模型、材质、法线图不因单层隐藏而卸载，避免误释放其他可见层仍在使用的资源与反复加载。IsoRim 仍可绘制隐藏层的原生阴影，游戏逻辑、模拟与寻路继续运行。没有显存或整局 FPS 收益承诺。

Hidden layers keep their cached mesh level. First hidden prints can use the lowest level without projection. Shared textures/materials/models remain cached; native shadow behavior and gameplay simulation are unchanged. This reduces specific LOD work, not necessarily total frame time or VRAM.

## 坐标与材质 / Coordinates and materials

保存模型的原始地图行坐标，通过 `ReadStaticModelView` 获取当前真实高度与行偏移，再调用 `WorldToViewport`。不缓存可能过期的楼层绘制偏移，也不把压缩高度混入真实坐标。`LocalToWorld` 可直接返回含楼层偏移的位置；持续跟踪使用 `LocalToMap` 加当前偏移。

API 61 的视图查询只读取渲染器已准备好的状态，不会主动准备楼层；首次绘制前可能返回不可见。子 Mod 保留对象并在后续帧再次查询，出现可见状态后按预算恢复。The API 61 view query is read-only and may be invisible before the map's first rendered frame; the addon keeps polling and resumes within its normal budget.

法线材质通过 `ConfigureStaticMaterial(..., supportsFloorFade: true)` 明确声明实现 `_IsoFade` / `_IsoFadeFrom`。IsoRim 创建其原生淡出副本，保留共享法线贴图。顶点属性、光照参数和裁切标记属于 IsoRim 内部着色器约定，**不保证稳定**；更新后由本子 Mod 适配和复验。版本不匹配、资产或基底数据拒绝时回退原生画法，最多普通日志／警告。

Raw geometry and live true-space floor offsets are kept separate. Custom materials explicitly opt into native floor fade. The consumer owns its shader; internal renderer shader conventions are not a stable public ABI. Invalid assets or a mismatched host retain native rendering with warnings, never intentional red error logs from the addon.

验证与限制见 [楼层验收记录](review/floor-validation.md)。不包含车辆、活动炮塔、门叶、角色动画、自动生成的模型副本、完整大型 Mod 栈或长期存档验收。
