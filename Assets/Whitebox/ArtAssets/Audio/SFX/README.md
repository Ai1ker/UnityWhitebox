# 机械与技能音效 / Mechanical and skill sounds

以 CC0 实物录音剪辑、叠加和滤波为主体。仅技能 1、2 叠加低音量、柔和起落的电子能量脉冲，不使用尖锐的高频扫音。原始录音及授权见 `../Source/CC0/SOURCES.md`。

| 文件 | 声音设计 |
| --- | --- |
| Footstep_01–06 | 0.17–0.186 秒轻巧落脚，削减低频共振，关节与擦地只留微弱细节，6 个变体 |
| Jump | 0.21 秒轻起跳声，削减低频压力感，擦地与关节只留短而微弱的细节，默认 Volume 0.30 |
| SkillVelocity | 短机械拨动与柔和冲击，叠加短促 410–434 Hz 电子脉冲，比机械层 RMS 低 16 dB |
| SkillGravity | 机构移动与双段落锁，叠加较低的柔和双脉冲，比机械层 RMS 低 15 dB |
| PlayerDeath | 实录金属、薄板和零件依次掉落，加低沉的核心落地声 |
| TurretAim | 很轻的瞄准机构摩擦，去掉激光电子音；4.6 秒连续循环 |
| TurretFire | 短促机械击发及闷实冲击 |
| BulletHitPlayer / Wall | 较闷的外壳命中 / 较干的硬质表面命中 |
| Checkpoint | 柔和的两次卡扣确认，去掉原先三音阶提示 |
| PressurePlate / BulletSwitch | 较沉的压板落位 / 较短的开关卡扣 |
| DoorOpen / Close | 实际拖擦、解除锁扣和落锁，开关门力度不同 |
| Portal | 柔和的压力释放、宽频摩擦与闷实机械开合，无电子扫频 |
| CrateScrape | 硬质物体拖过粗糙表面的实际摩擦，5.8 秒交叉衔接循环，不混入电机音调 |

全部为 44.1 kHz、单声道、PCM16 WAV。脚步和跳跃不做饱和增厚；脚步峰值 0.26、默认 Volume 0.22，跳跃峰值 0.31、默认 Volume 0.30。技能柔化高频，保留少量电子反馈。单次声音首尾淡入淡出，持续声音由播放器做 40 ms 渐入 / 80 ms 渐出。推箱只随速度调整响度，不随速度升降音调。各项音量可在 Resources/LaboratoryAudio/SoundBank.asset 调节。

python Tools/generate_laboratory_sfx.py 可离线重复生成，使用 Python 标准库及项目内的实录素材。加 --only Footstep SkillVelocity SkillGravity 可只生成脚步和两种技能。FoleyProcessing.json 记录时长、峰值、RMS、循环接缝检查及技能电子层比例。
