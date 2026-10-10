# 核心掉落死亡动画

使用内置 imagegen，以 halo-robot-atlas.png 为角色造型参考，生成八帧透明背景死亡图集。原始输出 1774×887，4 列 2 行；导入工具按实际尺寸切片，保持与原角色相同的像素比例，并以脚底/碎片底部校准地面基线，不跟随下落核心重新居中。

## 生成提示词

+Create a NEW companion DEATH ANIMATION SPRITE ATLAS for the EXACT same pixel-art spherical-core exoskeleton robot shown in the attached reference atlas. The existing robot identity must remain unmistakable: one ivory ceramic spherical core with cyan circular eye, separated white shell petals around exposed charcoal inner sphere and open graphite metal cage, thin mechanical arms and digitigrade legs, tiny white armor plates, visible joints, broad small feet. It has no separate humanoid head and chest. This is the player robot from a 2D laboratory escape game.
Output ONE transparent PNG, WIDE 2:1 canvas exactly 1024x512 pixels. Precisely 4 columns by 2 rows, 8 equal 256x256 cells. NO visible grid, no text, no labels, no background, no ground line, no shadow, no logos, no border. All eight animation frames use the SAME camera, character scale, canvas anchor and floor baseline. Strict crisp chunky pixel art matching reference color palette, outlines and resolution; hard edges, no blur or gradients. Keep all debris and sparks within each cell with transparent padding.
IMPORTANT SCALE/ANCHOR: in the first frame the intact robot spans approximately y=42 to y=245 within its 256x256 cell, width approximately130px, spherical core center at x=142,y=106, cyan eye looks RIGHT and is centered near x=177,y=105. The feet touch an invisible fixed floor at y=245. Maintain this same invisible floor at y=245 in ALL frames. As it breaks, the core must visibly DROP DOWN across the animation, NOT remain centered by automatic layout. Keep the sphere the SAME size (about90px diameter) even when lying on the floor. The robot core and every detached mechanical piece must stay recognizable and consistent. Maximum debris spread x=20 to x=235.
Frame sequence left-to-right TOP ROW then left-to-right BOTTOM ROW, one readable action per cell:
Frame0: lethal impact reaction, robot still upright at reference anchor; bent cage, a few tiny cyan-white electrical pixels, visible new cracks in ivory armor.
Frame1: damaged open exoskeleton support struts snap, one arm detaches, white shell fragment pops outward; the central spherical core disengages from the cage but stays near y=108. No fireball.
Frame2: skeletal legs buckle and thin arms fall outward; the now free core drops to center about x=151,y=136, slightly rotated clockwise so its blue iris points diagonally down-right.
Frame3: broken cage collapses into scattered thin struts; free core falls to center x=164,y=184, nearly reaching the invisible floor. Ivory armor chips and a small severed arm beside it.
Frame4: core contacts the floor at bottom y=245 (sphere center about x=173,y=200); tiny impact pixel sparks. The exoskeleton is now a LOW pile of detached arms, feet, joints and bent rods to its left, NOT a second intact robot.
Frame5: short gentle rebound, same sphere center about x=179,y=187, rotates another small amount clockwise, eye dimmer cyan; low broken pieces settle.
Frame6: core lands again at center about x=184,y=200, no more sparks; scattered white plates and dark skeletal limbs lie on floor, cyan eye very faint.
Frame7: hold final wreckage pose, core at center about x=184,y=200, dead dim blue/off eye; completely disassembled exoskeleton forms LOW debris to the left. No tall remaining legs or intact body. This must read as an intact spherical personality core fallen out of a destroyed exoskeleton, not the entire robot shrinking or a human corpse.
All eight cells filled, no clipping/crossing cell boundaries. Genuinely transparent alpha background.
