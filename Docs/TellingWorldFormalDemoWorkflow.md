# Telling World Formal Demo Workflow

## Current Assets

Audio:

```text
Assets/Audio/TellingWorld.mp3
Assets/Audio/TellingWorld.mp3.meta
```

Generated calibration resources:

```text
Assets/Rhythm/FormalDemos/TellingWorldCalibrationDemo_Chart.asset
Assets/Rhythm/FormalDemos/TellingWorldCalibrationDemo_Track.asset
Assets/Scenes/FormalDemos/TellingWorldCalibrationDemo.unity
Assets/Settings/Volumes/FormalDemos/TellingWorldCalibrationDemo_SpeedFeelProfile.asset
```

## Audio Import Settings

`TellingWorld.mp3.meta` should match the stable music playback path:

```yaml
preloadAudioData: 1
loadInBackground: 0
ambisonic: 0
```

The project music player explicitly waits for clip loading before calling `AudioSource.Play()`, but preloading keeps the first-play path predictable for VR capture.

## Initial Calibration Values

The first generated track uses:

```text
baseBpm: 150
firstBeatOffsetSeconds: 4.57
```

These are calibration starting values, not final authored values. Tune them after testing the generated calibration scene.

The constants live in:

```text
Assets/Editor/RhythmParkour/MusicVrDemoSceneBuilder.cs
```

```csharp
const float TellingWorldInitialBpm = 150f;
const float TellingWorldInitialFirstBeatOffsetSeconds = 4.57f;
```

For generated assets, the current offset is serialized in:

```text
Assets/Rhythm/FormalDemos/TellingWorldCalibrationDemo_Track.asset
```

```yaml
firstBeatOffsetSeconds: 4.57
```

Use the builder constant as the source of truth when rebuilding the scene. Directly editing the track asset is useful for quick calibration tests.

## Calibration Scene

Open:

```text
Assets/Scenes/FormalDemos/TellingWorldCalibrationDemo.unity
```

Controls:

- `Space` or controller action button: start playback
- `Space`: desktop timing hit fallback
- `R`: reset
- `Offset(s)` input field: type an offset value in seconds; the effective judgment offset updates live
- `Reset` button: discard the temporary offset adjustment
- `Save` button: save the current effective offset back to the track asset in the Unity Editor

The first chart is step-only from beat `8` through beat `31`, alternating left/right every beat. This is intentionally simple so drift is easy to hear and see.
The calibration scene's visible step cues and beat stripes are aligned to the same `8` through `31` beat range.

Miss tempo penalty is disabled in this calibration scene, so missed inputs still show as misses but do not slow the song, cue timing, or tempo scale.

The runtime offset HUD shows:

```text
Offset: effective offset (temporary adjustment)
```

Typing in the `Offset(s)` field changes the effective offset immediately, so you can keep the scene running while fine-tuning. Press `Save` once the timing feels right.

## How To Tune

1. Start the scene and listen for whether beat stripes and Step cues align with the song.
2. If every cue is consistently early or late, adjust `TellingWorldInitialFirstBeatOffsetSeconds`.
3. If cues start aligned but drift over time, adjust `TellingWorldInitialBpm`.
4. Rebuild from Unity menu:

```text
Rhythm Parkour/Rebuild Telling World Calibration Demo
```

5. Repeat until the first 24 authored beats stay aligned.

## After Calibration

Once BPM and first beat offset are stable, replace the step-only chart with the formal mixed parkour chart:

- Step for light rhythm sections
- SideGrab on clear side accents
- Slide on low/descending phrases
- LongJump on strong upward accents
- Grapple for sustained vocal or synth phrases

Keep a copy of the calibration chart or regenerate it from the builder when retuning.
