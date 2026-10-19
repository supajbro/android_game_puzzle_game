# Jelly animation sheet preparation

Run from the repository root:

```powershell
py -3 -m pip install --user Pillow==12.0.0
py -3 Tools/Art/prepare_jelly_animation_sheets.py
```

Pillow is an Editor/source-art tool dependency only; it is not added to the Unity player. The
validated version requires Python 3.10+ and declares the permissive MIT-CMU licence in its package
metadata.

The script reads the five `Assets/_Project/Art/Animations/Jelly_*_Sheet.png` sources, calculates
proportional 5-by-4 crop boundaries, removes only disconnected/edge debris through alpha geometry,
and exports 100 transparent 256-by-256 RGBA frames. It does not colour-key the art. Outputs are
organised by colour and `Falling`, `Landing`, `SquashStretch`, or `Blink` beneath `Frames/`.

Contact sheets are written to `Tools/Art/Previews/`. The JSON report records every crop and flags
any frame where conservative cleanup fell back to the source crop. Unity import settings and the
`JellyAnimationLibrary` asset are rebuilt via `Jelly Riot/Visuals/Configure Animation Frames`.
