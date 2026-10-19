# Audio bible

Status: Work Order 16 integration complete; perceptual phone/headphone review remains an owner gate.

Use section 5 and Part E of the master plan as the source of truth. The 26 original WAV files remain unchanged beneath `Assets/_Project/Audio/SFX`. `JellyRiotAudioSettings` is the sole clip/mix adapter and `AudioManager` owns a fixed ten-source 2D SFX pool plus one dormant music source.

## Runtime mapping

- `UI_Tap_01..03`: rack selection and UI buttons; quiet random variant.
- `UI_Invalid`: rejected placement, including a full column. A rejected move never queues drop or landing audio.
- `Jelly_Drop_01..04`: authoritative `JellyPlaced` event at fall start.
- `Jelly_Land_01..03`: the presentation's first contact frame; also one restrained landing haptic.
- `Jelly_Pop_01..05`: once per cleared connected group, capped to three audible groups per wave.
- `Cascade_02..05`: resolution waves two to five. Later waves reuse `Cascade_05` with restrained pitch variation.
- `Riot_Stinger`: first wave three-or-later event in one action only, with a short mix duck and RIOT haptic.
- `Score_Tick`: at most one scheduled tick per scored wave, spaced from the wave cue.
- `Row_Rise`: `PressureRowApplied`.
- `Game_Over`: `RunFinalised`.
- `Best_Score`: `PersonalBest` presentation cue.
- `Daily_Complete`: a completed Daily run reaching results.

Variant choice and pitch variation use a private `System.Random`; they never consume `IGameRandom`. Core event sequence numbers suppress duplicate playback. Pause, focus loss and ad interruption discard delayed gameplay sounds and stop pooled voices so stale bursts cannot occur on resume.

## Import and mix

All short clips are mono, Vorbis quality 0.76, decompress-on-load, optimised sample rate and preloaded on default and Android targets. No clip streams. Runtime AudioSources use `spatialBlend = 0`, no Doppler and no distance attenuation. Default relative levels are UI 0.34, drop 0.56, landing 0.62, pop 0.74, cascade 0.70, RIOT 0.82 and results 0.68. Major cascade/RIOT cues temporarily duck non-major SFX and the optional music voice.

Use `Jelly Riot > Audio > Configure SFX` to rebuild the settings asset/import configuration. In Play Mode, `Jelly Riot > Audio > Open SFX Preview` previews every individual clip through the production pool.

## Manual acceptance checklist

- Listen to every clip on phone speakers at low device volume and on headphones; score clarity, squishiness, distinctiveness, repeatability and clipping from 1–5.
- Verify one legal drop produces one drop and one landing; a full column produces only invalid feedback.
- Trigger large multi-group waves and a 3+ chain; confirm controlled pops, ascending cascades and one RIOT stinger/haptic.
- Pause mid-fall, background/foreground, then simulate an ad with `SetAdvertisementInterruption`; confirm no stale burst and no stuck mute/duck.
- Restart and reload Main; confirm mute, Master/SFX/Music volumes and haptics persist.
- Complete Endless with and without a new best, then complete Daily; confirm result cues do not clip.
- Play with sound and haptics disabled and confirm every gameplay state remains understandable.

Phone/headphone listening, Android haptic feel and real advertisement focus behaviour are **NOT RUN** in the automated environment and must not be called final until checked on hardware.

