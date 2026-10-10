# 炮塔与能量球美术

冷银灰机身、红色光效，与青色核心机器人区分。所有图片为 ImageGen 生成的透明 PNG。

## 已接入

- `Prefabs/Mechanics/Turret.prefab`：现有场景沿用此预制体，自动根据根节点是否存在 Rigidbody2D 选择外观。
- `Turret_Physics.prefab`：三脚立式安防炮塔，有动态 Rigidbody2D。
- `Turret_Fixed.prefab`：带固定底座的炮塔，无 Rigidbody2D，可以旋转后嵌入墙面。
- `Resources/TurretArtVisual.prefab`：两款各 6 帧待机、6 帧锁定、6 帧发射，另有独立转向枪管。
- `Resources/TurretExplosion.prefab`：8 帧爆炸后自行销毁，无残骸。炮塔的 Explosion Prefab 仍可自行替换。
- `Prefabs/Bullet.prefab`：4 帧红白能量球循环；碰撞体、质量、速度、重力和技能交互维持原设置。

## 可调整项

炮塔子节点 TurretArt 的 Style 默认 Automatic。根节点有 Rigidbody2D 时使用 Physics Standing，反之 Fixed Mount；也可以手动覆盖外观。Idle Frames Per Second / Fire Frames Per Second 控制动画速率，Gun World Length 控制枪管显示长度。不要为调整美术而改变子弹根节点 Scale，它影响碰撞体。

美术子节点补偿原有非等比 Scale，不改变炮塔的 BoxCollider2D 或物理参数。激光和实际发射方向沿用原逻辑。锁定动画对应真实锁定进度，发射动画仅在实际发射时触发。暂停和技能慢放同样作用于动画。

## 源图与导入

|源图|使用内容|
|---|---|
|turret-standing-atlas.png|6 列 3 行，立式待机/锁定/发射|
|turret-mounted-atlas.png|6 列 6 行，仅下三行用于固定款；上三行为早期弃用设计，不导入|
|turret-effects-atlas.png|4 列 3 行，第一行枪管，其余 8 帧爆炸|
|energy-orb-atlas.png|4 列单行，4 帧能量球|

图片按实际输出尺寸切片，Point 采样、无压缩、无 mipmap、保留透明通道。身体帧按脚底对齐，能量球按白热内核定位。可从 Whitebox > Art > Set Up Turrets And Energy Orbs 重新导入；正常换美术直接替换 Sprite 数组即可。重跑导入不会覆盖已存在的两款变体的设计参数。

