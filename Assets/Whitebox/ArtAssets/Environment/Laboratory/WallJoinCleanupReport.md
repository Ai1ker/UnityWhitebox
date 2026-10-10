# Laboratory wall join cleanup

仅修整审计名单中 14 个墙地块碰撞体的长轴端头；只修改 BoxCollider2D.size / offset，并同步刷新实验室美术。Root Transform 经逐项精确校验保持不变，不修改物理材质、机关、通道高度或其他对象。首次运行前的原场景备份位于 `Temp/LaboratoryWallJoinBackup`，再次运行不会覆盖备份。已对齐的对象仅验证并跳过；候选边界与审计旧值偏差达到 0.003，或邻接锚点缺失、锚点对应边与审计目标偏差达到 0.003 时，记录 SkippedChangedByDesigner 及原因并保留现状。所有候选在整批预检及修改后验证通过后才保存各自场景。

| Scene | Root object | Anchor | Edge | Before | After | Trim | Result | Reason |
| --- | --- | --- | --- | ---: | ---: | ---: | --- | --- |
| Assets/Whitebox/Scenes/Level02_Blank.unity | Ground_Long | Wall_Left (1) | maxX | 19.390000 | 19.360000 | 0.029999 | Trimmed |  |
| Assets/Whitebox/Scenes/Level02_Blank.unity | Ground_Long (1) | Wall_Left (1) | minX | 18.269450 | 18.360000 | 0.090548 | Trimmed |  |
| Assets/Whitebox/Scenes/Level02_Blank.unity | Ground_Long (2) | Wall_Right | minX | 27.851940 | 27.930000 | 0.078056 | Trimmed |  |
| Assets/Whitebox/Scenes/Level02_Blank.unity | Ceiling (2) | Wall_Right (1) | maxX | 73.940000 | 73.900000 | 0.040001 | Trimmed |  |
| Assets/Whitebox/Scenes/Level02_Blank.unity | Ceiling (3) | Wall_Right (1) | maxX | 74.035000 | 73.899990 | 0.135002 | Trimmed |  |
| Assets/Whitebox/Scenes/Level03_Blank.unity | Ground_Long | Wall_Left | minX | -12.952500 | -12.885050 | 0.067448 | Trimmed |  |
| Assets/Whitebox/Scenes/Level03_Blank.unity | Wall_Left | Ceiling | maxY | 8.547500 | 8.491394 | 0.056106 | Trimmed |  |
| Assets/Whitebox/Scenes/Level03_Blank.unity | Ground_Long (1) | Wall_Left (1) | maxX | 64.643800 | 64.590000 | 0.053802 | Trimmed |  |
| Assets/Whitebox/Scenes/Level03_Blank.unity | Ground_Long (3) | Wall_Right (1) | minX | 84.209990 | 84.229160 | 0.019173 | Trimmed |  |
| Assets/Whitebox/Scenes/Level03_Blank.unity | Ground_Spawn | Wall_Left (1) | minX | 63.540000 | 63.590000 | 0.049995 | Trimmed |  |
| Assets/Whitebox/Scenes/Level04_Blank.unity | Ground_Long | Ground_Long (2) | maxX | 8.436632 | 8.410002 | 0.026630 | Trimmed |  |
| Assets/Whitebox/Scenes/Level04_Blank.unity | Ceiling (2) | Wall_Right | maxX | 91.257490 | 91.200000 | 0.057495 | Trimmed |  |
| Assets/Whitebox/Scenes/Level05_Blank.unity | Ceiling | Wall_Right | maxX | 32.575000 | 32.540000 | 0.035000 | Trimmed |  |
| Assets/Whitebox/Scenes/Level06_Blank.unity | Ceiling (5) | Wall_Left (1) | minX | -46.411170 | -46.370000 | 0.041176 | Trimmed |  |
