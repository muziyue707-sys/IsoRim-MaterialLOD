## 给自己的模型接入

1. 在 Blender 完成低模、UV、三角化和高→低烘焙；烘焙以后不要改变低模三角化／顶点法线。
2. 用 `scripts/export_basis.py` 导出**同一个烘焙低模**。此导出器处理一个静态物体、一个材质；多材质资产先分件或合图。它不负责生成贴图或烘焙，不会替你自动减少面数。
3. 把 OBJ、MTL、同名 `.obj.irbasis` 放到模型包的 `Models`，底色／法线 PNG 放到该包 `Textures` 下。MTL 的 `map_Kd` 是相对 Textures 的路径。
4. 按 IsoRim 原规则写 `IsoRim.ModelDef`，再为相同模型添加下列定义。

```xml
<RimZrism.Materials.MaterialDef>
  <defName>MyCrateMaterial</defName>
  <model>Models/MyCrate.obj</model>
  <materialName>Crate</materialName>
  <normalTexture>MyPack/MyCrate_Normal</normalTexture>
  <normalStrength>1</normalStrength>
  <flipGreen>false</flipGreen>
  <flipNormalV>true</flipNormalV>
</RimZrism.Materials.MaterialDef>
```

`model` 相对**此定义所在的 Mod 根目录**；`materialName` 精确对应 MTL 名称；`normalTexture` 相对该 Mod 的 Textures，省略 `.png`。底色仍走 IsoRim 原加载器与 MTL Kd 色彩规则。法线通过线性数据纹理加载，默认 Blender/OpenGL +Y。`flipGreen` 用于相反的绿通道约定；`flipNormalV` 是当前 Waylith 原始 PNG 行方向修正，和绿通道翻转是两个不同操作。强度范围 0–4，0 为几何顶点法线。

PowerShell 调用例子（自行替换路径）：

```powershell
& 'D:\Steam\steamapps\common\Blender\blender.exe' -b 'D:\Assets\Crate.blend' --python-exit-code 1 -P '<addon repository>\scripts\export_basis.py' -- --object Crate_low --out 'D:\MyModelPack\Models\MyCrate.obj' --texture MyPack/MyCrate_BaseColor
```

导出器默认 Kd 白色，`--tint R G B` 可指定乘色。坐标和 Blender MikkTSpace 切线一起转换到游戏空间；边车文件携带 OBJ SHA256，错配、缺失、损坏时拒绝启用并显示原材质。每个启用材质在该模型里必须使用唯一固定的 map_Kd 路径，不支持 `{stuff}` 动态路径。配置不改变其他模型的材质请求。
