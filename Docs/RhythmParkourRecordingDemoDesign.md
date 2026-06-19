# Rhythm Parkour Recording Demo Design

## Goal

Create a dedicated recording scene that combines the validated Meta XR input stack, rhythm judging, music playback, and first-person parkour feedback into one short showcase flow.

The scene should be usable for VR capture, quick desktop checks, and future iteration without adding more test-only responsibilities to `SampleScene`.

## Scene

Target scene:

```text
Assets/Scenes/RecordingDemos/RhythmParkourRecordingDemo.unity
```

Generated supporting assets:

```text
Assets/Rhythm/RecordingDemos/RecordingDemo_Chart.asset
Assets/Rhythm/RecordingDemos/RecordingDemo_Track.asset
Assets/Settings/Volumes/RecordingDemos/RecordingDemo_SpeedFeelProfile.asset
```

## User Experience

The recording flow starts in a ready state. The player presses `Space` or a VR controller action button to begin music playback. The camera then moves forward with the beat while action cues appear on the runway.

The player performs a mixed sequence of six core action types:

- Step
- Side Grab
- Slide
- Long Jump
- Grapple
- Rhythm-only running between actions

Successful inputs trigger first-person parkour feedback. Missed inputs trigger the existing tempo and visual stress feedback, but the recording scene uses a wider hit window so the flow is easier to capture.

## Reused Systems

The first version reuses the current runtime components:

- `VRRhythmActionPrototype`
  Handles music playback, beat position, chart judging, tempo distortion, and hit/miss events.
- `VRInputReader`
  Reads Meta XR `OVRInput` first, then falls back to Unity Input System and XR input devices.
- `VRExpectedActionInputMatcher`
  Matches chart actions against hand, button, and direction input.
- `FirstPersonActionFeedbackDriver`
  Applies first-person parkour motion feedback to the camera/tracking space.
- `VRSpeedFeelDriver`
  Drives post-processing pressure from tempo and hit/miss state.
- `RhythmTrackConfig` and `RhythmActionChart`
  Store the music clip, BPM, beat offset, and authored action sequence.

## Scene Structure

```text
[BuildingBlock] Camera Rig
  TrackingSpace
    LeftEyeAnchor
    CenterEyeAnchor
    RightEyeAnchor
    LeftControllerAnchor
    RightControllerAnchor

VR Input System
  VRInputReader
  VRParkourInputEvents
  VRInputDebugOverlay
  VRControllerDebugVisuals
  FirstPersonActionFeedbackDriver
  VRSpeedFeelDriver

Music VR Rhythm Action Prototype
  AudioSource
  AudioDistortionFilter
  VRRhythmActionPrototype

Runway / Beat Stripes / Action Cues / Goal Portal
Global Speed Feel Volume
Demo Floating Label
```

## Chart

The initial chart is short and readable for recording:

| Beat | Action |
| ---: | --- |
| 2 | Step Left |
| 3 | Step Right |
| 4 | Side Grab Left |
| 6 | Side Grab Right |
| 8 | Slide |
| 10 | Long Jump |
| 12 | Step Left |
| 13 | Step Right |
| 16 | Grapple Right, 4 beats |
| 22 | Slide |
| 24 | Long Jump |
| 28 | Grapple Left, 4 beats |

The sequence intentionally leaves space between larger actions so the recording reads clearly and the player has time to reset posture.

## Tuning

Recording scenes prioritize clarity over strict scoring:

- `hitWindowBeats`: `0.4`
- Camera rig start position: `(0, 1, -1.5)`
- Music uses explicit audio data loading before playback.
- Keyboard fallback remains enabled for desktop capture and debugging.
- `Space` starts playback and can also hit rhythm actions in desktop testing.
- `R` resets the run.

The current action demos use stricter timing. This scene should remain more forgiving unless the recording target changes.

## Implementation Plan

1. Add a design document under `Docs`.
2. Extend the existing demo builder with a recording demo menu item.
3. Generate the recording chart and track from the validated tutorial music.
4. Generate a new scene under `Assets/Scenes/RecordingDemos`.
5. Reuse the Meta XR camera rig, rhythm prototype, input stack, first-person feedback, speed volume, runway, cues, and goal portal.
6. Verify the generated scene contains one track reference, one music `AudioSource`, one active `AudioListener`, and the mixed chart events.
7. Test in Play Mode with keyboard first, then VR controller input.

## Future Improvements

- Add turn actions once chart-level turn scoring is added.
- Add a recording-only HUD with cleaner typography and fewer debug details.
- Add scene-specific lighting and Neo City props for a stronger capture background.
- Add a short countdown after start input before the first scored beat.
