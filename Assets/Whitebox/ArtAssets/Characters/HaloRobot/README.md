# 核心外骨骼机器人

玩家外观已替换为球形核心、开放机械支架和细机械四肢的像素机器人。

- 玩家预制体：`Assets/Whitebox/Prefabs/Characters/Player.prefab`。
- 共用外观预制体：`Assets/Whitebox/Resources/HaloRobotVisual.prefab`。编辑这里可以更新所有关卡的机器人动画与外观。
- 原始图集：本目录下 `halo-robot-atlas.png`，透明背景，1024×1536，4 列 6 行，共 24 帧。
- 动画：待机 4 帧、跑动 8 帧、上升/下落各 1 帧、技能 1/2 释放各 5 帧。技能 1 为青色方向箭头，技能 2 为琥珀色重力场。
- 死亡动画：`halo-robot-death.png`，8 帧，外骨骼断裂、核心脱落、触地轻弹后熄光；死亡时保留原来的朝向。复活时恢复正常机器人。

在外观预制体的 `RobotPlayerVisual` 组件里可以调整各组帧、播放速度和 `Visual Size`（外观大小）。`Reference Body Height` 用于校准图中的实际身体高度，当前为 203/160，不含透明留白。角色的碰撞、质量、移动、重力与惯性仍由原玩家根对象管理。

`Death Frames Per Second` 控制死亡动画速度，默认每秒 12 帧；`Death Hold Seconds` 控制最后碎片姿势的停留时间，默认 0.15 秒。复活等待至少 0.85 秒，若把死亡动画调慢，则会自动等到动画播完再复活。暂停菜单会同时暂停动画与复活计时。

`Whitebox > Art > Set Up Robot Death Animation` 可单独重新导入死亡图集。死亡图集按照碎片落地位置校准，不跟随核心下落重新居中。生成提示词见 `DeathGenerationPrompt.md`。

现有图集使用 Point 过滤，无压缩、无 mipmap；每帧按照核心和脚底位置设置独立 pivot，避免生成图集的排版偏差造成动画抖动。更换为新图集时，请按新素材重新设置切片和 pivot。

`Whitebox > Art > Set Up Core Robot Player` 可重新导入当前这张图集并生成外观预制体。这个工具的尺寸、帧顺序和校准数据只对应当前图集，不适用于任意新图；它不会保存或重建关卡。

第一关的旧场景玩家在运行时自动挂接这套外观，其余关卡继承玩家预制体。以后新摆放玩家时，直接使用 `Player.prefab`。

图集使用内置 imagegen 生成；完整生成提示词见 `GenerationPrompt.md`。
