# Rhythm Parkour Runtime Notes

This README records the current runtime components and public interfaces for the VR rhythm parkour demo. Keep it updated when changing chart data, input matching, timing, or prototype scene behavior.

## Current Data Flow

```text
RhythmTrackConfig
  -> RhythmActionChart
  -> RhythmActionEvent
  -> rhythm timing window
  -> VRInputSnapshot from VRInputReader or keyboard fallback
  -> VRRhythmActionSession / VRExpectedActionInputMatcher / DynamicTempoState
  -> feedback, tempo scale, distortion, failure
```

The intended VR-facing flow is:

```text
Current chart action + current beat
  -> find the action inside the judgment window
  -> read VRInputSnapshot from VRInputReader
  -> VRExpectedActionInputMatcher.Matches(expectedAction, snapshot)
  -> register success, wrong input, or miss
```

The important design direction is that chart actions ask for an expected player input. Player input should not directly choose the parkour action that happens.

## RhythmParkour Assembly

### BeatClock

Small beat utility.

Key API:

- `new BeatClock(baseBpm)`
- `GetSecondsPerBeat(tempoScale)`

Use it when converting beat-space durations to seconds. Tempo scale is clamped internally so invalid scale values cannot divide by zero.

### RhythmActionEvent

Serializable chart event for one authored parkour action.

Fields exposed through properties:

- `Beat`
- `DurationBeats`
- `ActionType`
- `Hand`
- `Direction`

Supported action types:

- `Step`: one hand press, currently used for alternating floor gaps.
- `SideGrab`: one specified hand, pressed while pointing left or right.
- `Slide`: both hands down together.
- `LongJump`: both hands up together.
- `Grapple`: one specified hand up and held for the authored duration.

### RhythmActionChart

ScriptableObject that stores a sorted list of `RhythmActionEvent`.

Key API:

- `Events`
- `SetEvents(IEnumerable<RhythmActionEvent>)`

The chart sorts by beat, then action type. Authoring should stay in beat units rather than seconds.

### RhythmTrackConfig

ScriptableObject that binds a song to chart timing.

Key fields:

- `AudioClip`
- `BaseBpm`
- `FirstBeatOffsetSeconds`
- `Chart`

Key API:

- `GetSecondsAtBeat(float beat, float tempoScale)`

Use this asset when adding songs. For a new song, set its real BPM, align `FirstBeatOffsetSeconds` to the first strong beat, and author chart events in beats.

### DynamicTempoState

Owns the tempo punishment and recovery model.

Current defaults:

- Normal scale: `1.0`
- Miss penalty: `-0.03`
- Success recovery: `+0.01`
- Distortion starts below `0.9`
- Failure starts below `0.75`

Key API:

- `Scale`
- `IsDistorted`
- `IsFailed`
- `RegisterMiss()`
- `RegisterSuccess()`
- `Reset()`

The current prototype uses `Scale` for audio pitch and beat pacing. Repeated misses slow the music; consecutive successes gradually recover toward normal speed.

### RhythmBeatManager

Optional beat event driver that binds the metronome to `DynamicTempoState`.

Key API and events:

- `Beat`
- `BeatUnityEvent`
- `TempoScaleChanged`
- `DistortionStarted`
- `FailureReached`
- `StartPlayback()`
- `StopPlayback()`
- `RegisterSuccess()`
- `RegisterMiss()`
- `ResetTempo()`

Runtime link:

```text
DynamicTempoState.Scale
  -> AudioSource.pitch
  -> BeatClock.GetSecondsPerBeat(TempoScale)
  -> next beat DSP schedule
```

This means misses slow both the audible music and future beat emission. In the current VR action prototype, beat position is read from `AudioSource.time` while `AudioSource.pitch` is driven by `VRRhythmActionSession.TempoScale`, so playback speed and chart progress stay linked there too.

### SingleButtonChartJudge

Current prototype timing judge. It checks only whether there is a chart event inside the judgment window.

Key API:

- `TryHit(float currentBeat, out RhythmActionEvent hitEvent)`
- `ConsumeMisses(float currentBeat)`
- `NextEvent`
- `SuccessCount`
- `MissCount`

Current judgment rule:

```text
hit if event.Beat - hitWindowBeats <= currentBeat <= event.Beat + hitWindowBeats
miss if currentBeat > event.Beat + hitWindowBeats
```

The current scene uses `hitWindowBeats = 0.25`, so the active window is `+/-0.25 beat`.

### SingleButtonChartSession

Prototype session wrapper around `SingleButtonChartJudge` and `DynamicTempoState`.

Key API:

- `Press(currentBeat)`
- `ConsumeMisses(currentBeat)`
- `TempoScale`
- `DistortionAmount`
- `IsDistorted`
- `IsFailed`

This class currently treats any on-window keyboard press as success. It does not yet check action type, hand, or direction.

### SingleButtonChartPrototype

Playable keyboard prototype scene driver. It is used to validate chart timing and tempo punishment before full VR action matching.

Current behavior:

- Press `Space` as the moving cue crosses each wall.
- Press `R` to restart.
- Uses `hitWindowBeats = 0.25`.
- Misses lower tempo scale.
- Successes recover tempo scale.
- Below `0.9` scale, music distortion starts.
- Below failure threshold, the session enters failure state.

This component is intentionally simple and should not be treated as final VR gameplay code.

## BoringRun.VRInput Assembly

### VRInputTypes

Shared input data types.

Important types:

- `VRHand`: `Left`, `Right`, `Both`
- `VRHandDirection`: `Unknown`, `Up`, `Down`, `Left`, `Right`, `Forward`
- `VRHandInputFrame`: one hand state for a frame
- `VRPlayerTurnFrame`: headset/player yaw turn state for a frame
- `VRInputSnapshot`: left/right hand frames plus beat conversion data
- `VRParkourInputEvent`: debug/diagnostic semantic event

`VRInputSnapshot.GetHand(VRHand.Both)` returns a combined two-hand frame:

- pressed only if both hands are pressed
- direction only if both hands share the same direction
- hold duration is the shorter hold duration

### VRInputReader

MonoBehaviour that reads controller state. The current path is Meta-first: it attempts `OVRInput` from Meta XR SDK, then falls back to Input System devices, then Unity XR `InputDevices`.

Responsibilities:

- Track left and right hand frames.
- Read trigger/grip/primary/secondary button from Meta Touch / hand tracking or the fallback XR paths.
- Detect press, release, hold duration.
- Convert controller pose or position into coarse direction.
- Detect headset/player yaw turns as one-shot left/right semantic events.
- Produce `VRInputSnapshot` through `CreateSnapshot(secondsPerBeat)`.

Useful API:

- `LeftHand`
- `RightHand`
- `GetHand(VRHand)`
- `WasPressed(VRHand)`
- `IsPressed(VRHand)`
- `WasReleased(VRHand)`
- `HoldDuration(VRHand)`
- `Direction(VRHand)`
- `PlayerTurn`
- `WasPlayerTurnedLeft()`
- `WasPlayerTurnedRight()`
- `ResetPlayerTurnBaseline()`
- `AreBothHandsPressedTogether()`
- `CreateSnapshot(float secondsPerBeat = 1f)`

### VRExpectedActionInputMatcher

Pure matching layer between an authored chart action and current VR input state.

Key API:

- `Matches(RhythmActionEvent expected, VRInputSnapshot input, float minimumHoldCompletionPercent = 1f)`
- `MatchesGrappleStart(RhythmActionEvent expected, VRInputSnapshot input)`
- `ShouldEmitGrappleUpdate(float lastUpdateBeat, float currentBeat, float updateIntervalBeats = 0.25f)`

Current matching rules:

- `Step`: expected hand must have `wasPressedThisFrame`.
- `SideGrab`: expected hand must have `wasPressedThisFrame`, and direction must be left or right as authored.
- `Slide`: both hands must be pressed together this frame, both pointing down.
- `LongJump`: both hands must be pressed together this frame, both pointing up.
- `Grapple` start: expected hand must already be pressed and pointing up during the start window.
- `Grapple` completion in the rhythm session is stateful: after start confirmation, the sustained segment immediately fails if the expected hand releases or points away from up; success is judged at the authored completion beat if the expected hand is still pressed upward. Raw button `holdDuration` is diagnostic data, not the completion source of truth.

This is the component future VR rhythm judging should call after the timing judge selects a candidate chart event.

### VRRhythmActionJudge

Pure beat-window judge for VR action input.

Key API:

- `JudgeInput(float currentBeat, VRInputSnapshot input)`
- `ConsumeMisses(float currentBeat)`
- `IsInsideWindow(float currentBeat)`
- `IsInsideJudgmentWindow(float currentBeat)`
- `TryConfirmGrappleStart(float currentBeat, VRInputSnapshot input)`
- `TryFailGrappleContinuation(float currentBeat, VRInputSnapshot input, out VRRhythmJudgmentResult result)`
- `NextEvent`
- `SuccessCount`
- `MissCount`

Current result kinds:

- `Hit`: input is inside the beat window and matches the expected chart action.
- `WrongInput`: input is inside the beat window but does not match the expected action. The expected action is consumed as a miss.
- `BadTiming`: input is outside the current action window. The next action stays available until it is hit or missed.

Grapple has two windows:

- Start window: `event.Beat +/- hitWindowBeats`. If the expected hand is not pressed upward by the end of this window, the action is missed immediately.
- Sustained segment: active only after `TryConfirmGrappleStart(...)` succeeds. During this segment, `TryFailGrappleContinuation(...)` immediately fails the action if the expected hand is released or stops pointing up.
- Completion window: the action can complete at `event.Beat + event.DurationBeats`, using the normal positive `hitWindowBeats` grace.

### VRRhythmActionSession

Session wrapper that combines `VRRhythmActionJudge` with `DynamicTempoState`.

Use this for gameplay-facing action demos. It applies tempo punishment or recovery after each judgment result:

- `Hit` registers success.
- `WrongInput`, `BadTiming`, and missed actions register miss penalties.
- Grapple miss deadlines use the start window until start is confirmed, then move to the authored hold completion point.

Key API:

- `TempoScale`
- `DistortionAmount`
- `IsDistorted`
- `IsFailed`
- `GetDropoutPulse(float distortionAmount, float phase)`
- `TryConfirmGrappleStart(float currentBeat, VRInputSnapshot input)`
- `TryFailGrappleContinuation(float currentBeat, VRInputSnapshot input, out VRRhythmJudgmentResult result)`

### VRRhythmActionPrototype

Reusable MonoBehaviour driver for action demo scenes.

Responsibilities:

- Plays the assigned `RhythmTrackConfig`.
- Maintains a `VRRhythmActionSession`.
- Reads `VRInputReader.CreateSnapshot(...)` when VR input is present.
- Falls back to timing-only keyboard input when no `VRInputReader` is available.
- Displays current beat, next action, hits, misses, tempo, and feedback.
- Applies the same tempo distortion layer as the single-button prototype: smoothed distortion, volume dropouts, and low procedural noise below `0.9` tempo scale.
- Locks into a failed state below the tempo failure threshold; press `R` to restart the prototype.
- Publishes `JudgmentResolved` for consumed input judgments and `ActionMissed` for chart events that pass their miss deadline.

Audio distortion chain:

```text
VRRhythmActionSession.TempoScale
  -> AudioSource.pitch
  -> VRRhythmActionSession.DistortionAmount
  -> AudioDistortionFilter.distortionLevel
  -> dropout pulse lowers music volume
  -> procedural noise AudioSource volume/pitch
```

`DistortionAmount` is `0` above `0.9` tempo scale and reaches `1` at the failure threshold. `VRRhythmActionPrototype` smooths this amount with `distortionSmoothingSeconds` so misses do not create abrupt audio jumps.

Keyboard fallback controls:

- `Space`: start playback when the scene is waiting at load or after reset.
- `Space`: timing hit for normal actions. If pressed inside the current action window, the action succeeds.
- `Space hold`: grapple test. For `Grapple`, the start window confirms the hold, the sustained segment fails immediately on release, and the completion window succeeds if Space is still held.
- `R`: reset back to the waiting-for-start state.

This is intended for quick action demos and tutorial prototypes. It is not final UI.

### PlayerActionAnimationDriver

Procedural visual proxy driver for testing player action animation with a simple cylinder or ball.

Responsibilities:

- Subscribe to `VRRhythmActionPrototype.JudgmentResolved` and `ActionMissed`.
- Optionally subscribe to `VRParkourInputEvents.InputRecognized` for simulator/controller animation debugging.
- Animate the assigned target transform for consumed chart actions.
- Keep `BadTiming` inputs from playing the authored action, because that result does not consume the chart event.
- Scale animation duration by `VRRhythmActionPrototype.CurrentTempoScale`.
- Tint success green and consumed misses/wrong inputs red when a renderer is assigned.
- Provide debug keys `1` through `5` for testing the five action animations without a rhythm source.
- Optionally follow the XR camera or `Camera.main` using yaw-only body follow so looking up/down does not drag the proxy through the view.

Prototype mappings:

```text
Step      -> bounce, slight side tilt, and short forward pulse
SideGrab  -> lean and shift toward the authored direction
Slide     -> squash downward
LongJump  -> upward/forward hop
Grapple   -> longer stretch/pull pulse
```

Attach it to the cylinder or to a `PlayerVisualProxy` parent. Assign `Action Source` to the object with `VRRhythmActionPrototype`, `Animated Target` to the cylinder transform, and optionally `Tint Renderer` to the cylinder renderer.

### FirstPersonActionFeedbackDriver

First-person action feedback driver for testing parkour motion without a visible player proxy.

Responsibilities:

- Subscribe to `VRRhythmActionPrototype.JudgmentResolved` and `ActionMissed`.
- Optionally subscribe to `VRParkourInputEvents.InputRecognized` for simulator/controller feedback debugging.
- Animate a camera parent transform, usually `Camera.main.transform.parent` / XR Origin `Camera Offset`.
- Move a separate locomotion root, usually `XR Origin (XR Rig)`, for accumulated first-person travel.
- Keep motion values small for VR comfort.
- Provide debug keys `1` through `5` for isolated feedback testing.

Prototype mappings:

```text
Step      -> accelerated forward travel, tiny camera bob, side sway, mild yaw/roll
SideGrab  -> side sway and slight yaw
Slide     -> staged lower/hold/recover, forward travel, low-angle upward view
LongJump  -> smooth takeoff/air/landing arc with a longer air-time plateau
Grapple   -> smooth forward pull with slight lift/look
Miss      -> short lateral shake
```

This is now preferred over the cylinder proxy for first-person testing. The cylinder proxy can remain available as a debugging tool, but it should not be treated as the final first-person body representation.

Current `SampleScene` first-person setup:

- `VR Input System` owns `FirstPersonActionFeedbackDriver`.
- `Motion Root` is `XR Origin (XR Rig) / Camera Offset`.
- `Locomotion Root` is `XR Origin (XR Rig)`.
- Keyboard keys `1` through `4` test step, side grab, slide, and long jump directly.
- Long jump uses a gentle vertical curve: eased takeoff, sustained air-time plateau, and eased landing.
- Camera orientation is applied as a small local offset after position feedback. Jump pitch is kept around a few degrees for VR comfort.
- `Ground` uses `M_SynthwaveBlock_Cyan` with shader scroll disabled.
- The scene skybox uses `M_SoftNightPanoramicSkybox`, not a flat pure-color background.

Current generated scene:

- `Assets/Scenes/VRActionPrototype.unity`
- Rebuild menu: `Rhythm Parkour/Rebuild VR Action Prototype`
- Uses `Assets/Rhythm/Tutorial_120BPM_Track.asset`
- Shows action-specific neon markers and a moving cue ball.
- Grapple markers stretch forward by `durationBeats * unitsPerBeat`, so their thickness shows how long the hold lasts.
- In the current 120 BPM tutorial chart, Grapple lasts 8 beats, which is 4 seconds and 20 scene units.

## Music VR Demo Scenes

The music-driven VR demo set is generated by:

```text
Rhythm Parkour/Rebuild Music VR Demo Scenes
```

Generated scene folder:

```text
Assets/Scenes/MusicVrDemos/
```

Generated rhythm data folder:

```text
Assets/Rhythm/MusicVrDemos/
```

All six scenes currently reuse the sample song from `Assets/Rhythm/Tutorial_120BPM_Track.asset`, including its real `BaseBpm` and `FirstBeatOffsetSeconds`. Each demo has its own `RhythmActionChart` and `RhythmTrackConfig`, so the same scene/runtime path can later be retargeted to a new song without changing the judgment code.

The generated demos use the same current VR operation stack as `SampleScene`: `VRInputReader` reads Meta SDK `OVRInput` first, then falls back to Input System / Unity XR. `BoringRun.VRInput.asmdef` references `Oculus.VR` for that Meta SDK input path.

Each scene is generated with the same XR hierarchy used by the current VR setup:

```text
XR Origin (XR Rig)
  -> Camera Offset
    -> Main Camera
    -> Left Controller
    -> Right Controller

VR Input System
  -> VRInputReader
  -> VRParkourInputEvents
  -> VRInputDebugOverlay
  -> VRControllerPoseBinder
  -> VRControllerDebugVisuals
  -> FirstPersonActionFeedbackDriver
  -> VRSpeedFeelDriver
```

`Main Camera` has an Input System `TrackedPoseDriver` and URP post processing enabled, and `XR Origin (XR Rig)` has `XROrigin` plus `InputActionManager`. `VRRhythmActionPrototype` moves the XR Origin forward according to the current beat, while `FirstPersonActionFeedbackDriver` lives on `VR Input System` like the existing VR setup and applies action feedback offsets on `Camera Offset`. Its root-travel fields are serialized to match the current driver interface, but accumulated root travel stays disabled in the music demos so beat-synchronized forward motion remains deterministic. This makes the generated demos VR-view scenes rather than desktop-only camera previews.

Each generated scene also creates:

- `Global Speed Feel Volume`: a URP global `Volume` with Lens Distortion, Chromatic Aberration, and Vignette.
- `VRSpeedFeelDriver`: binds `VRRhythmActionPrototype`, the speed volume, and the main camera.

`VRSpeedFeelDriver` increases lens distortion, chromatic aberration, vignette, and desktop FOV boost on successful or failed action judgments. It also reads `VRRhythmActionSession.DistortionAmount`, so the visual stress ramps together with the existing BPM slowdown/audio distortion pressure instead of popping abruptly.

Current demo split:

- `Demo_00_RhythmRun`: music-only forward motion. It has no scored chart actions and is used to check speed, camera motion, beat stripes, and general comfort.
- `Demo_01_Step`: alternating left/right step presses on the beat.
- `Demo_02_SideGrab`: authored hand plus left/right direction press.
- `Demo_03_Slide`: both hands down together.
- `Demo_04_LongJump`: both hands up together.
- `Demo_05_Grapple`: one hand up and held through the full authored duration.

These demos intentionally use `VRRhythmActionPrototype` rather than `VRParkourInputEvents` as the scoring path. This keeps the music-facing rule aligned with the intended design:

```text
music beat + chart action
  -> expected VRInputSnapshot
  -> VRExpectedActionInputMatcher
  -> hit / wrong input / bad timing / miss
```

Keyboard fallback is still available in the action demos for quick desktop testing:

- Start: press `Space`, or press any tracked VR controller action button.
- Normal actions: press `Space` inside the timing window.
- Grapple: hold `Space` from the start window until the completion window.
- Reset: press `R`, then start again with `Space` or a controller action button.

For VR testing, each action scene creates a `VRInputReader` and assigns it to the scene's `VRRhythmActionPrototype`. Hand direction is currently measured against the main camera transform.

### VRParkourInputEvents

Debug/diagnostic semantic input event emitter.

It recognizes inputs from `VRInputReader` and emits UnityEvents / C# events such as Step, SideGrab, Slide, LongJump, TurnLeft, TurnRight, and Grapple hold events. This is useful for debugging controller recognition, but it is not the final rhythm judgment path.

Player turn recognition is headset-yaw based. `VRInputReader` compares the current camera/player yaw against a baseline and emits one event after the yaw passes `playerTurnYawThresholdDegrees`, then waits for `playerTurnCooldownSeconds`. This detects intentional physical turning without requiring a controller button. The current rhythm matcher does not consume turn events yet; chart-level turn actions should be added separately if turns become scored gameplay.

For rhythm gameplay, prefer `VRInputReader.CreateSnapshot(...)` plus `VRExpectedActionInputMatcher.Matches(...)`.

### VRInputDebugOverlay

Simple OnGUI debug panel showing:

- left/right tracked and pressed state
- current hand directions
- player yaw turn delta
- hold durations
- slide, jump, and grapple recognition
- latest debug semantic event

### VRControllerPoseBinder and VRControllerDebugVisuals

Scene/debug helpers for visible controller transforms and controller pose display. They support development and tuning but should not own rhythm judgment.

## Current Judgment Window

Current prototype window:

```text
hitWindowBeats = 0.25
```

This is a single shared window for all current actions. At `120 BPM`, `0.25 beat` is about `125 ms`, so the full `+/-0.25 beat` window is about `250 ms`.

Recommended next step is to introduce per-action windows, for example:

```text
Step: +/-0.20 beat
SideGrab: +/-0.22 beat
Slide: +/-0.25 beat
LongJump: +/-0.25 beat
Grapple start: +/-0.25 beat; miss immediately after this if start was not confirmed
Grapple sustain: fail immediately if the expected hand is released or points away from up
Grapple hold completion: 100% of durationBeats after start confirmation
```

These values should eventually live in a ScriptableObject instead of inside scene components.

## Adding A New Song

1. Add the audio clip.
2. Create or duplicate a `RhythmTrackConfig`.
3. Set `BaseBpm` to the song BPM.
4. Set `FirstBeatOffsetSeconds` so beat `0` aligns with the first usable beat.
5. Create or duplicate a `RhythmActionChart`.
6. Author events in beat units. Use fractional beats when needed, such as `4.5` for half beat.
7. Assign the chart to the track config.
8. Test with the simple prototype before connecting full VR action matching.

## Current Gaps

- Keyboard fallback in prototype scenes is timing-only for normal actions, while `Grapple` uses the same stateful start/sustain/completion rhythm path as VR input.
- Full action matching is still available through `VRInputReader` plus `VRExpectedActionInputMatcher` for VR input.
- There is now a reusable VR rhythm judgment/session layer, but the existing wall prototype scene still uses the older single-button driver.
- There is no per-action judgment window asset yet.
- There is no final result taxonomy yet for `WrongInput` versus `BadTiming` versus `Miss`.
- Grapple hold update cadence is defined in beat-space, but no final VR rhythm adapter consumes it yet.
- The current debug semantic event layer is useful, but should not become the official rhythm matching path.
