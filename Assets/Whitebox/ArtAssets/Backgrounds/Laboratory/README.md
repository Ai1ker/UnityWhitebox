# 实验室背景与双语文字

素材由内置 imagegen 生成。美术提示词完整记录在 [GenerationPrompts.md](GenerationPrompts.md)。

## 自行摆放背景

预制体位于 `Assets/Whitebox/Prefabs/Backgrounds/`，直接拖进场景即可。它们不含碰撞体或技能目标，不参与解谜判定。本次没有自动放进任何已设计的关卡。

| 预制体 | 用途 |
| --- | --- |
| BackgroundWall_Panels | 暗银灰残墙、面板接缝、通风口和线管，可平铺 |
| BackgroundWall_BrokenRebar | 破洞、裸露钢筋和断线，可盖在普通背景墙上 |
| LaboratoryGlass_Flicker | 玻璃观察窗，故障灯光闪烁与工业阴影轻微移动 |
| LaboratoryScreen_Cracked | 裂纹、弹孔保护玻璃和自定义双语信息屏 |

墙体的 `LaboratoryBackgroundWall / 背景尺寸` 控制覆盖范围；也可缩放根对象，图案继续平铺。默认背景排序 -100，破洞模块 -99，玻璃 -80，屏幕约 -70，均位于现有地形和角色后方。需要重叠装饰时可调整各子物体 SpriteRenderer 的 Sorting Order。

墙体贴图使用 Trilinear 和 mipmap，减少平滑镜头移动、缩远时细线的像素跳闪；角色和前景贴图的采样保持原样。

## 动态玻璃

`LaboratoryGlassFlicker` 可调：

- Flicker Interval Seconds：两次故障闪烁之间的随机间隔。
- Flicker Duration Seconds：每次闪烁持续时间。
- Flicker Intensity：灯光忽明忽暗的强度。
- Shadow Jitter Distance：阴影轻微横移的幅度。
- Enable Flicker：关闭后恢复稳定灯光。

每个实例使用独立随机相位。默认暂停时停止，技能慢放期间按真实秒计时。

## 屏幕内容

选中屏幕根对象，在 `LaboratoryDisplayScreen` 填写 `Chinese Content` 和 `English Content`。支持多行与自动缩小字号，英文留空时回退中文。Font Size 控制字号，Custom Font 可指定正式字体。

Enable Diagnostics 控制是否偶尔出现警报。Glitch Interval Seconds 与 Glitch Duration Seconds 分别控制间隔和时长；默认每次警报持续 2.2–3.2 秒。警报显示大号 WARNING / ERROR（中文模式附带“警告 / 错误”）、三角叹号、上下红条与暗红底色，之后恢复正文。Diagnostic Pulse Seconds 控制缓慢亮暗脉冲的周期，默认 1.1 秒；Diagnostic Font Scale 控制警报字号倍率，Diagnostic Panel Color 控制警报底色。文字保持可读，不会快速闪灭。Crack Color 可调裂纹透明度，确保正文可读。屏幕不含碰撞体。

## 所有文字的语言选择

默认跟随玩家系统语言：中文系统使用中文，其他系统使用英文。主菜单或 ESC 设置中可切换“跟随系统 / 中文 / English”，偏好会保存。

- 场景提示框：原 `SceneHintText.content` 保留中文，新增 `contentEnglish` 填英文。
- 主菜单与结束菜单：在每个文字对象的 `LocalizedUiText` 中填写 Chinese / English。
- 现有 HUD、教学、技能操作、暂停菜单、关卡选择、死亡和提示消息已提供中英文。

本版本采用离线双语，不会联网自动翻译。已有游戏文案已填写英文；新写的屏幕、提示框和菜单内容请填写对应英文，缺失时显示原中文，不会让文字消失。

## 素材文件

- `lab-background-walls.png`：普通 / 破洞墙体两切片。
- `lab-glass-layers.png`：玻璃、灯光、阴影三层。
- `lab-display-layers.png`：外框、裂纹、电子干扰三层。

重新生成预制体：Unity 菜单 `Whitebox / Art / Set Up Laboratory Backgrounds`。此菜单会重设这四个背景预制体的默认外观和参数；设计时应直接修改场景实例或自建变体。
