# Art bible

Status: supplied production art integrated for Work Orders 09 and 10.

Use the palette, sprite sizes and glossy rounded-square jelly language in section 4 of the master plan. Source artwork belongs in `Assets/_Project/Art/Source`; imported sprites belong in `Assets/_Project/Art/Sprites`. Keep every asset original or accompanied by a verified licence record. Do not treat concept art as a gameplay screenshot.

## Runtime mapping

- `Coral`, `Lemon`, `Aqua`, `Lilac` and `Mint` map directly to the same-named sprites in `Art/Sprites`.
- `Jelly_Shadow` is a separate underlay; faces, blinks and glints remain optional presentation assets for later animation work orders.
- `button_panel.png` is authoritatively sliced as `CellHole`, `RackFive`, `PillButton`, `ScorePanel` and `InformationPanel`.
- `buttons.png` is authoritatively sliced as `Pause`, `Replay`, `Sound`, `Haptics`, `Home`, `Daily`, `Info`, `SelectionArrow` and `SelectionHighlight`.
- The production rack is reconstructed from three cell-hole sprites because the source rack illustration contains five cavities; gameplay remains exactly three slots.
- Ordinary UI and character textures use RGBA sprites, 100 pixels per unit, bilinear filtering, no mipmaps or runtime read/write, and ASTC 6x6 Android overrides.

The static Work Order 10 presentation intentionally has no animation-driven gameplay. Drop, blink, squash, pop and RIOT motion remain Work Orders 11-14.

