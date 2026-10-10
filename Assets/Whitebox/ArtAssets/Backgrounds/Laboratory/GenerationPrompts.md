# 内置 imagegen 生成记录

模式：内置 image_gen；未调用外部图像 API。所有最终 PNG 已保存于本目录，保留生成的透明通道（墙体为不透明背景）。以下是完整提示词。

## lab-background-walls.png

Use case: stylized-concept
Asset type: opaque modular background wall texture atlas for a Unity 2D side scrolling pixel-art abandoned laboratory.
Create a wide two-column sheet containing TWO equal square background wall modules edge to edge, no labels, no margin, no perspective. All surfaces FLAT front elevation, no top faces. Cold minimal silver-grey and graphite test-chamber architecture inspired by sterile futuristic laboratories, decayed and deserted. Low contrast dark background suitable BEHIND a brighter silver robot and gameplay walls.
LEFT MODULE: a quiet grid of large dark grey composite wall panels, thin seams, sparse vertical service conduits, a narrow recessed vent, occasional chipped edges and small cracks, minimal visual noise, no illuminated signage.
RIGHT MODULE: matching panel grid and same edge structure, but a large irregular broken opening offset toward lower-right; cracked panel fragments rim the hole, exposing bent vertical and horizontal steel reinforcing rods and a few torn cables against a nearly black service cavity. Hole is part of the image and fully opaque black interior, not alpha. More abandoned and damaged, but clean readable silhouette.
Pixel art: broad flat panels in about 128x128 logical pixels per module enlarged nearest neighbor, crisp square steps, restrained 5-7 tone cool grey palette, dark blue undertone, subtle sparse rust only on exposed rebar, no photorealistic lighting, no smooth 3D shading, no painted texture. Modules have matching quiet straight panel edges so they can repeat into a large wall. No characters, props, foreground floor, UI, writing, numbers, logos, watermarks, fog or vignette. No large colored lights. Full opaque background texture.

## lab-glass-layers.png

Use case: stylized-concept
Asset type: three registered transparent sprite layers for a decorative laboratory observation window in a flat 2D side scrolling pixel-art game.
Create one wide sheet with THREE equal square cells in a horizontal row. Each cell depicts matching 88%-wide by 68%-high rectangular window area centered at exactly the same position. True RGBA transparency, no grid.
LEFT: a thin cold silver-grey rectangular observation-window frame seen DIRECTLY FROM THE FRONT. Broad dark blue-grey semitransparent glass pane, a few subtle diagonal reflections, sparse scratches, two thin vertical mullions, a small damaged strip lamp along the inner top edge. Quiet abandoned lab, simple planar construction, restrained wear, no perspective or visible depth.
MIDDLE: ONLY the matching neutral WHITE luminous strip lamp at that inner top edge, plus two very faint descending shafts/reflections within the same window. No frame, no glass, transparent elsewhere. A slightly interrupted white strip to suggest an unreliable lamp. Light will be tinted and flickered in Unity.
RIGHT: ONLY soft dark graphite shadow shapes visible THROUGH that window: two hanging cables and a few diagonal industrial bars casting broken long silhouettes across the glass. Transparent empty gaps, no frame or glass. Keep shadows translucent and sparse; no creature, person or face. This layer will appear and shift subtly when the faulty lamp flashes.
Style: minimal cold silver/graphite pixel art with crisp square pixel steps, about 96x64 logical pixels per window, limited flat tones, low contrast background prop. Matches a cold silver exoskeleton robot and ruined futuristic test laboratory. No realistic 3D gloss, no photorealism, no isometric angle, no round bevels, no text/logos/arrows/numbers/watermark, no ground or backdrop. Light layer only white, shadow layer neutral dark grey.

## lab-display-layers.png

Use case: stylized-concept
Asset type: THREE transparent registered sprite layers for a wall-mounted information display in a 2D pixel-art ruined laboratory game.
Create a wide three-column sheet, three equal square cells side by side. Every cell corresponds to the exact same thin rectangular widescreen monitor, 88% cell width and 54% cell height, centered identically. Flat front elevation only, no perspective. True RGBA transparent background.
LEFT CELL: physical monitor casing only: minimal thin cold silver-grey rectangular frame with small squared corner brackets, graphite inner rim, tiny cool-grey indicator beneath screen. Empty display aperture fully transparent. NO text or cracks. Keep front flat, restrained few-tone pixel shading, slim simple frame, subtle sparse wear.
MIDDLE CELL: ONLY a transparent broken protective-glass overlay fitting the aperture: a sparse long thin branching hairline crack from the upper-left corner, a small bullet impact star with a dark puncture at upper-right corner and delicate radial spiderweb cracks there. Most of center and lower-middle remain completely clear so dynamic text stays readable. Very thin desaturated white/grey pixel lines with low opacity, no broad glow and no big opaque shards. NO casing, no words or image content.
RIGHT CELL: ONLY a subtle neutral-white electronic glitch overlay matching the aperture: a few thin horizontal scan bars, small broken pixel bands near left/right edges and one thin displaced scan streak. Mostly transparent center, no casing, no text. Will flash briefly during Warning/Error state.
Style: simple cold silver-grey pixel art, about 112x64 logical pixels per monitor enlarged nearest-neighbor, crisp square pixels, dark abandoned futuristic test lab atmosphere, consistent with a flat silver robot and laboratory mechanisms. Avoid 3D render, metallic gradients, thick beveled depth, realistic photography. Absolutely NO baked-in writing, numbers, logo, keyboard, stand, backdrop, floor, drop shadow, watermark, yellow hazard tape or red permanent status color.

