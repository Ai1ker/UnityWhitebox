# 简约 2D 存档点

素材：`lab-checkpoint-flat-layers.png`，由内置 imagegen 生成，保留原始透明通道。左侧为银灰外壳，右侧为白色灯光；两层使用相同裁切与定位，灯光由存档点的原有状态颜色驱动。

预制体：`Assets/Whitebox/Prefabs/Mechanics/Checkpoint.prefab`。采用正面细导轨、扁平底座和菱形指示灯，减少圆形立体底座与金属厚重感。未激活为青蓝，激活为绿色，保留原有脉动、触发范围、存档位置设置。

需要重新导入时使用 Unity 菜单 `Whitebox / Art / Set Up Flat Checkpoint`。总机关美术导入菜单也会保留此版本。

## 生成提示词

Use case: stylized-concept
Asset type: TWO registered transparent sprite layers for ONE checkpoint beacon in a Unity 2D side-scrolling pixel-art laboratory game.
Primary request: replace an overly three-dimensional checkpoint with an extremely simple FLAT TWO-DIMENSIONAL front-elevation pixel sprite. A compact horizontal rectangular silver plinth at the bottom, two slender straight silver vertical guide rails with short rectangular caps, and one small hollow diamond floating above the center. A delicate translucent holographic column indicates a save point. Simple, quiet, cold futuristic laboratory aesthetic, matching silver robot casing. Readable at 40 pixels tall.
Composition: wide sheet with two equal square cells side by side. LEFT cell: ONLY the physical silver-grey plinth, thin rails and small hollow diamond casing. RIGHT cell: ONLY corresponding WHITE luminous elements: thin interrupted vertical scan lines between the rails, three short horizontal scan dashes, a tiny white diamond core inside the floating diamond. Registration aligned with left cell; no casing in right cell. Overall beacon in each cell occupies 48% cell width and 82% cell height, centered with transparent margin.
Style: extremely simplified true 2D pixel art, about 48x80 logical pixels per beacon, crisp nearest-neighbor square pixels, sharp edges, 3-tone flat silver/graphite casing, only a one-pixel highlight. Neutral white light layer so Unity can tint cyan or green. Lots of open transparent space within silhouette.
Absolutely NO perspective, NO isometric view, NO visible top faces, NO cylinders, NO ellipses, NO round 3D pedestal, NO metallic gradients, NO volumetric lighting, NO thick beveled panels, NO detailed bolts, NO photorealism, NO 3D render. No broad white haze or filled glowing rectangle. No letters, symbols except the small diamond, text, logo, floor, backdrop, drop shadow or watermark. True RGBA transparent background.
