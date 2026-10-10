# 冷峻实验室环境与核心箱

使用内置 ImageGen 生成透明像素美术（墙面为不透明可平铺贴图），原始 PNG 复制到本目录，未修改像素。完整提示词见 GenerationPrompts.md。

已接入 Assets/Whitebox/Prefabs/Environment 下全部 12 个预制体，以及 Assets/Whitebox/Prefabs/GravityCube.prefab。美术接入本身不修改物理组件或关卡摆放；另按设计者要求对部分墙体接头的小凸出做端头修齐，具体记录见 WallJoinCleanupReport.md。

- 墙体、地面、天花板：银灰大面板，少量崩角、裂纹、积尘，低装饰密度。
- 单向平台：银灰横梁、冷青色短灯条，仍代表可按向下穿过。
- 地刺：银灰尖刺与红色危险灯条。
- 立方体：完整干净的银灰箱壳，中央独立发光核心，无破损。

## 核心颜色

继续修改箱子根节点原有 SpriteRenderer 的 Color，即你之前在各关卡里修改的那个颜色。旧 SpriteRenderer 只隐藏渲染、保留颜色数据，LaboratoryArt/ColorLinkedCore 会在编辑器和运行时实时读取该颜色。银灰外壳保持白色材质色，不会被一起染色。支持每个场景实例独立覆盖颜色，也支持脚本在运行时更改原 SpriteRenderer.color。

## 缩放与物理

LaboratoryArt 子节点抵消原根节点的不均匀缩放，用读取到的 BoxCollider2D 尺寸显示美术。墙面在世界尺寸中平铺，WorldAlignedPanels 材质按世界坐标统一面板接缝，重叠墙块和直角连接处的纹理连续。平台与地刺横向平铺，纵向仅缩放一排。负缩放和根旋转照常保留。可继续按原方式改根节点 Scale 来设计地形。不要给美术子节点增加碰撞体，也不要打开原碰撞体的 Auto Tiling。

旧 top edge、Drop-through dash、Spike、Cube mark 等白盒 Sprite 保留对象但隐藏渲染，不影响原层级或脚本引用。核心颜色源仍是箱子根节点。

## 源图

|文件|导入内容|
|---|---|
|lab-wall-panels.png|4×4 世界单位的可平铺面板纹理|
|lab-oneway-platform.png|切去透明边距的横梁，端帽九宫格边界，中心横向平铺|
|lab-spike-module.png|三根地刺组成的重复模块|
|lab-cube-layers.png|两层：银灰箱壳与白色可染色发光核心|

导入采用 Point、Full Rect、无压缩、无 mipmap；不生成物理形状。Whitebox > Art > Set Up Laboratory Environment And Cube 可以重新配置当前源图的切片与预制体。以后换尺寸不同的新图，请先调整对应 Sprite 切片或导入器中的矩形配置。
