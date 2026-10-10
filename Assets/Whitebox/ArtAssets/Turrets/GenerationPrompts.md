# 图片生成记录

生成工具：内置 ImageGen；透明背景。原图复制入项目，未改动原始像素。Unity 中仅配置切片和枢轴。

## 固定款（首版下半部分）

Create a production 2D pixel-art enemy TURRET animation atlas matching the pixel-art laboratory escape robot in the reference, with a colder, more severe military-science feel. Use SILVER GREY ceramic/metal shell plates, exposed charcoal mechanical framework, blue-grey metal shadows and RED optics/red status lights (never cyan). Original Aperture Science / Portal-like clean experimental hardware. Strict crisp chunky pixel art and dark outlines matching the reference, not smooth illustration, no antialiasing or painterly texture.
ONE transparent PNG atlas, SQUARE canvas ideally 1536x1536. Exactly SIX columns x SIX rows = 36 equal square cells, each ideally256x256. No grid, labels, text, background, floor, shadows, logo or watermark. Every cell contains exactly one turret animation frame, consistent scale and centered alignment. Same invisible ground/base baseline y=224 in EVERY cell, machine top near y=40, main circular red sensor/weapon bearing at x=128,y=128. Body approximately185px high, keep generous transparent padding, no cross-cell pixels. Do not include a long projecting gun barrel: a separate aimable gun sprite will be attached to the central bearing by the engine. A tiny barrel socket is fine.
TWO visibly different turret designs:
A PHYSICS STANDING TURRET (rows1-3): upright compact round/ovoid exposed red sensor core in slim silver armoured ribs, supported by THREE thin articulated black-and-silver mechanical legs, splayed stable feet. Thin legs must be clearly visible, equipment looks freestanding and movable, same exposed-core + exoskeleton design language as the player but clearly a hostile machine, not another walking humanoid. Cold symmetrical laboratory instrument.
B FIXED MOUNT TURRET (rows4-6): circular red sensor core in a heavier angular SILVER GREY armoured socket with thick external braces, connected to a broad flat bolted mounting/base plate at bottom y=224. A rigid integrated bracket/casing instead of legs. NO walking legs and NO tripod feet. Looks permanently mounted to a floor or wall and can be rotated as a unit by a level designer. Its plate and mechanical core structure make it unmistakably different from the standing model. Maintain main bearing at x=128,y=128 and comparable185px overall height.
Animation order TOP TO BOTTOM:
Row1: six standing turret IDLE frames, very restrained sensor scan and dim red indicator pulse, fixed position, feet never move.
Row2: six standing turret LOCKING frames, red optic/intensity gradually rises, small armour shutters progressively open exposing a red inner bearing, mechanical readiness increases. No aim-direction changes.
Row3: six standing turret FIRING/recovery frames, tiny white-red internal firing flash in central bearing then subtle recoil of body/shutters and return. Keep feet on same baseline, recoil only few pixels. Do not add long muzzle flame (separate sprite).
Row4: six fixed mount IDLE frames, restrained red indicator sweep/pulse. Base plate never moves.
Row5: six fixed mount LOCKING frames, central optic gradually turns intense red and segmented armour iris opens. Base stays rigid.
Row6: six fixed mount FIRING/recovery frames, compact white-red core flash, short mechanical recoil of upper socket and return, base never moves.
Six sequential frames each row, real readable changes without changing design, proportions or camera. All frames face generally RIGHT in a 2D side view/very slight 3/4 view with a clearly visible circular red optic. Metallic cold silver, not warm cream. RED effects distinguish enemies from cyan player. Transparent alpha background, no transparent-looking checkerboard painted into image.

## 立式款（最终版）

Generate a NEW standalone PHYSICS STANDING ENEMY TURRET animation atlas. The user liked the FIXED MOUNT model in the BOTTOM HALF of the reference, but rejected the STANDING model in the TOP HALF because it resembled the spherical-core humanoid player too much. Do NOT repeat that rejected anatomy. Keep the same crisp pixel art rendering, cold SILVER GREY armour, charcoal mechanics and RED lighting as the reference. This new standing design must have an unmistakably different silhouette from the player.
NEW DESIGN: a slender upright angular CAPSULE / SENSOR TOWER chassis with beveled silver-grey segmented shell panels, a narrow VERTICAL RED SENSOR SLIT down the front, two compact symmetrical side CANNON PODS/armoured wing panels. The body is a tall cold precision security instrument. Support it on THREE splayed straight thin mechanical TRIPOD STRUTS ending in tiny rubber contact pads. The struts are rigid rods, NOT articulated humanoid legs, NOT digitigrade legs and NOT boot-like feet. NO arms, NO hands, NO big spherical core, NO humanoid head or torso. It should read as an automated tripod sentry appliance, not a robot character or a walking person. The upper central body is elongated, with a red vertical eye slit rather than a round red eyeball. Small dark vent gaps and asymmetrical technical panel details, no logos. Main weapon swivel/aiming socket near center x=128,y=128, no long protruding cannon barrel because the game will attach an aimable gun separately.
ONE transparent PNG atlas, landscape 2:1 ideally1536x768, EXACTLY SIX columns by THREE rows =18 equal256x256 square cells. No text/labels/grid/background/floor/shadow/watermark. Same machine identity, scale, perspective and alignment in all18frames. Machine top around y=40 and tripod contact baseline y=224 in EACH cell, overall about185px tall, centeredx128. No pixels cross cell borders; generous transparent padding. Mostly side-view with slight 3/4 reading of the slit/pods, all generally facing RIGHT.
ROW1: SIX IDLE frames. Capsule closed, dim red slit slowly scans/pulses, quiet machine, no changes to tripod positions.
ROW2: SIX LOCK-ON frames. Red slit gradually brightens; side armour wings/cannon pod shutters progressively open a few pixels, exposing red mechanism inside. Distinct escalating readiness, same rigid tripod base. Do not morph into a sphere.
ROW3: SIX FIRING + RECOVERY frames. Sharp brief white-red internal weapon flash and a tiny red pixel burst from the central swivel, slight body recoil2-3pixels, then vents dim and panels return. No long muzzle flame (separate sprite), no moving the tripod feet. Six actual distinct sequential frames.
Pixel art MUST match the reference's chunky outlines, metal shading and tiny red pixel highlights. Cold silver instead of warm ivory. Strong readable tall narrow silhouette with simple tripod struts distinguishes it from both spherical-core PLAYER and heavy base-mounted turret. No human anatomy, no arms, no humanoid joints. Transparent alpha background.

## 枪管与爆炸

Create a companion PIXEL ART gun-and-explosion sprite atlas matching the attached silver-grey/red laboratory turret atlas. EXACT matching cold silver metal, charcoal mechanics, intense RED optics, crisp chunky pixels and dark outline, no cyan. Original clean malfunctioning laboratory hardware. Output ONE transparent PNG, portrait4:3 canvas ideally1024x768. EXACTLY FOUR equal columns by THREE equal rows, 12 square cells ideally256x256. No labels, text, grid lines, background, checkerboard, ground, shadows or logos. Real alpha transparency.
TOP ROW (four reusable aimable gun attachment sprites, no turret body or legs):
One horizontal side-view cannon barrel facing RIGHT, consistent design and scale in all4cells. The barrel's rear bearing starts at x=48,y=128 and serves as the ROTATION PIVOT. Main silver/graphite angular barrel extends from x=48 to x=212, centered at y=128, about34px high; a small red energy ring at the muzzle. It should look like a compact laboratory emitter/tube, not an organic weapon. No hand, no body, no stand. Keep rear bearing exactlyaligned in allfourcells.
Frame0gun idle: dark red muzzle, neutral cold silver body.
Frame1gun locked/charged: brighter red ring and small red side indicator.
Frame2gun shot: short sharp RED-WHITE muzzle flash from x=212 to x=242, fitsinsidecell; barrel recoils3pixels maximum but rearbearingdoesnotmove.
Frame3gun cooldown: little red vent sparks and dimming tip, same shape.
BOTTOM TWO ROWS (eight consecutive detached explosion animation frames, NO intact turret, NO lasting corpse):
All explosion frames centered at x=128,y=128 insideeachcell, same scale/worldcamera, generous padding, maximumradius106pixels. This effect will replace either turret model on destruction. Clearly a fast red reactor overload with sparse mechanical fragments.
Frame0 compact white-red core flash, small bright red ring and a few tiny dark-silver shell fragments.
Frame1 rapid red-white blast expansion with jagged pixel star silhouette.
Frame2 bright peak white center, RED and a little orange outer pixels, thin expanding ring; a few tiny silver/dark mechanical shards begin to fly away.
Frame3 larger broken red energy ring, white center shrinking, fragments scatter.
Frame4 fading red-orange explosion with chunky dark smoke puffs, no solid machine.
Frame5 fewer red embers, smaller dark-grey dissipating smoke clusters.
Frame6 tiny sparse fading red sparks and faint charcoal pixels.
Frame7 only a few tiny very dim red/grey pixels about to disappear; mostly transparent. No remains that look like a turret or pile of scrap. End disappears completely in engine.
Each of12cells filled in exactly described order, no cross-cell pixels or clipping. Gun spritesonlyinrow1, explosion effectsonlyinrows2and3. Strict pixelart: no blur, no gradients, no photorealism, no painterly rendering.

## 能量球

Create a game-ready PIXEL ART hostile ENERGY ORB PROJECTILE animation sprite sheet matching the silver-grey/red laboratory sentry gun and red explosion in the attached reference. The user wants the old plain tiny ball replaced by a glowing energy sphere. This is a projectile, no gun, no mechanical turret body, no metal shell or physical round ball.
ONE transparent PNG, landscape4:1 canvas ideally1024x256, exactly FOUR equal256x256 square cells in a single horizontal row. NO text, labels, grid, background, checkerboard, floor, shadows or logos. Every frame contains the SAME centered spherical red energy projectile at x128,y128 with a fixed visible core diameter about120pixels and outer aura about180pixels. Generous transparent padding, no cross-cell pixels. Consistent scale and center in allfourframes.
A small round WHITE-HOT central energy core, surrounded by saturated crimson/red plasma, a thin broken red circular energy ring and a few very small red orbiting square sparks. Clearly a luminous ball of unstable energy, a strong circular silhouette from all directions. Four seamless pulse-loop frames: inner core grows slightly, ring rotates/changes four cardinal pixel sparks, glowbrightens then returns; do not translate the sphere or add a one-sided long trail. Hostile RED distinguishes it from the CYAN player. A few orange pixels may bridge white tored but red must dominate. Strict crisp chunky pixelart, dark red outline where needed and hard-edged luminous pixels, limited palette, no blurry glow, no smooth vector edges, no painterly gradients. Transparent alpha background. Source asset for Unity 2D with small circle collision; preserve readable brightcenter at small on-screen size.


