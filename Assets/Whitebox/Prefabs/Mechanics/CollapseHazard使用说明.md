# 塌方机关

将同文件夹中的 `CollapseHazard.prefab` 拖入场景，顶板放在玩家路线的上方。

- 根物体的 **Box Collider 2D → Size / Offset** 是玩家触发区域，橙色编辑器线框仅用于摆放，不会显示在游戏中。
- **Delay Seconds**：玩家进入后，到第一块顶板掉下来的时间。
- **Segment Interval**：从左到右相邻顶板的掉落间隔。
- **Warning Seconds**：掉落前震动和红灯预警，包含在延迟内。
- **Fall Acceleration / Maximum Fall Speed**：坠落加速度和最大速度。
- **Maximum Fall Distance**：下方没有地面时，坠落超过此距离会停止并淡出，默认 30。
- **Settled Lifetime**：落地后保留时间，再淡出。
- **Reset On Respawn**：默认开启，玩家复活或脱离卡死后恢复顶板。

六块顶板可以在层级中单独移动、复制或删除；增删后更新根物体 **Segments** 数组，也可以清空数组以自动收集子物体。触发时按世界坐标从左到右排序，整体缩放、旋转后仍沿世界向下坠落。顶板预警时没有伤害，只有正在坠落的顶板接触玩家才造成 100 点伤害。它们不阻挡角色、箱子、子弹，也不会改变原有地面碰撞，不接受方向技能和边界重力。

如预制体尚未生成，在编辑器选择 **Whitebox → Create Collapse Hazard Prefab**。已有预制体不会被此菜单重建，以保留设计者修改。
