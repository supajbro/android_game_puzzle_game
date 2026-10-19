# Art bible

Status: supplied production art integrated through Work Order 11; animation frames prepared for Work Order 13.

Use the palette, sprite sizes and glossy rounded-square jelly language in section 4 of the master plan. Source artwork belongs in `Assets/_Project/Art/Source`; imported sprites belong in `Assets/_Project/Art/Sprites`. Keep every asset original or accompanied by a verified licence record. Do not treat concept art as a gameplay screenshot.

## Runtime mapping

- `Coral`, `Lemon`, `Aqua`, `Lilac` and `Mint` map directly to the same-named sprites in `Art/Sprites`.
- `Jelly_Shadow` is a separate underlay; faces, blinks and glints remain optional presentation assets for later animation work orders.
- `button_panel.png` is authoritatively sliced as `CellHole`, `RackFive`, `PillButton`, `ScorePanel` and `InformationPanel`.
- `buttons.png` is authoritatively sliced as `Pause`, `Replay`, `Sound`, `Haptics`, `Home`, `Daily`, `Info`, `SelectionArrow` and `SelectionHighlight`.
- The production rack is reconstructed from three cell-hole sprites because the source rack illustration contains five cavities; gameplay remains exactly three slots.
- Ordinary UI and character textures use RGBA sprites, 100 pixels per unit, bilinear filtering, no mipmaps or runtime read/write, and ASTC 6x6 Android overrides.

The static Work Order 10 presentation intentionally has no animation-driven gameplay. Drop, blink, squash, pop and RIOT motion remain Work Orders 11-14.

## Prepared animation frames

- Untouched source sheets are `Assets/_Project/Art/Animations/Jelly_<Colour>_Sheet.png`.
- Each 1402x1122 source is divided using proportional boundaries rather than assumed integer cells.
- Runtime-ready frames live under `Art/Animations/Frames/<Colour>/<Category>/` as transparent
  256x256 RGBA PNGs. Categories are `Falling`, `Landing`, `SquashStretch` and `Blink`, with five
  frames numbered `01` through `05` in each category.
- Cleanup is alpha-geometry based and does not key or replace jelly colours. It keeps the central
  jelly component and removes disconnected/edge debris; source sheets remain available for revision.
- `JellyAnimationLibrary` provides ordered lookup by `JellyColour` and `JellyAnimationType`. Static
  sprites remain in `Art/Sprites` and are restored whenever the presenter settles.
- Unity imports frames as single centred sprites at 100 PPU, bilinear, clamp, no mipmaps/read-write,
  max 256, with an ASTC 6x6 Android override. Use `Jelly Riot/Visuals/Configure Animation Frames`
  to rebuild imports and the library asset.
- Checkerboard contact sheets are in `Tools/Art/Previews`; the reproducible crop/cleanup record is
  `Tools/Art/animation_cleanup_report.json`.

