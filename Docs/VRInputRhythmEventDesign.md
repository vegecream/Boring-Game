# VR Input and Rhythm Event Design

## Purpose

This document defines the boundary between VR input recognition and rhythm/gameplay systems for the VR rhythm parkour demo.

The design goal is that gameplay and beat systems consume clean rhythm input events. They should not read XR devices, controller poses, simulator state, button thresholds, or raw Unity input directly.

## Current Context

The design in `Demo.txt` defines five player actions:

- Alternating step
- Side grab
- Slide
- Long jump
- Grapple hold

The current VR input implementation already has two useful layers:

- `VRInputReader`: reads raw controller state, button state, hold duration, and coarse hand direction.
- `VRParkourInputEvents`: recognizes semantic parkour inputs from those raw frames.

The incoming rhythm prototype appears to use chart assets with:

- `beat`
- `durationBeats`
- `actionType`
- `hand`
- `direction`

It also uses timing concepts such as `hitWindowBeats`, dynamic tempo scale, miss penalties, and distortion/failure thresholds.

The missing piece is a stable adapter layer between recognized VR input and the rhythm judge.

## System Layers

### 1. Raw VR Input

Owner: VR input system.

Responsibilities:

- Poll XR/Input System devices.
- Track left and right controller state.
- Read button press/release state.
- Read hold duration.
- Convert controller pose or position into coarse directions: up, down, left, right, forward, unknown.
- Apply low-level thresholds such as trigger analog threshold and direction thresholds.

Example component:

- `VRInputReader`

This layer is hardware-facing and simulator-facing. It should not know about beats, charts, scoring, or gameplay outcomes.

### 2. VR Input Recognition

Owner: VR input system.

Responsibilities:

- Convert raw controller frames into semantic player intent.
- Recognize the five parkour action types.
- Emit semantic action events without judging beat timing.

Example component:

- `VRParkourInputEvents`

Example recognized actions:

- Step left/right
- Side grab left/right
- Slide
- Long jump
- Grapple hold started/updated/ended

This layer should answer: "What did the player try to do?"

It should not answer: "Was this on beat?"

### 3. Rhythm Input Adapter

Owner: integration boundary between input and rhythm systems.

Responsibilities:

- Subscribe to recognized VR input events.
- Stamp each event with timing data.
- Convert the VR input event into a chart-compatible rhythm input event.
- Own public timing abstraction for gameplay and beat systems.
- Optionally buffer a short input history for early input handling.
- Expose one clean input event stream.

Proposed component:

- `VRRhythmInputAdapter`

This layer should answer: "What did the player try to do, and when did it happen in song/beat time?"

### 4. Rhythm Judge

Owner: beat controller / rhythm system.

Responsibilities:

- Read the current chart target.
- Receive rhythm input events from the adapter.
- Compare input beat time against chart beat time.
- Decide `Perfect`, `Good`, `Bad`, `Miss`, `WrongInput`, or equivalent result.
- Notify gameplay systems of result.
- Apply tempo scale changes through the dynamic tempo system.

This layer should not read XR devices or `VRInputReader`.

## Public Event Contract

The adapter should publish a rhythm-level event, not the raw VR event.

Suggested structure:

```csharp
public enum RhythmInputPhase
{
    Started,
    Performed,
    Updated,
    Completed,
    Canceled
}

public struct RhythmInputEvent
{
    public int sequenceId;

    public VRParkourActionType actionType;
    public VRHand hand;
    public VRHandDirection direction;
    public RhythmInputPhase phase;

    public float gameTime;
    public float songTime;
    public float beatTime;

    public float holdSeconds;
    public float holdBeats;
}
```

The exact enum and type names can change, but the data categories should stay stable.

### Timing Fields

- `gameTime`: Unity gameplay time when the input was recognized.
- `songTime`: current song time after audio offset and tempo handling.
- `beatTime`: current chart beat time.
- `holdSeconds`: hold duration in real/game seconds.
- `holdBeats`: hold duration converted into beat units.

The adapter owns these conversions so downstream systems can work in beat-space.

## Timing Ownership

The rhythm input adapter should wrap timing.

Gameplay and beat systems should not independently call `Time.time` for input timing. They should consume the adapter's `beatTime`.

Recommended public timing unit: beats.

Reason:

- Chart events are authored in beats.
- Tolerance windows are easier to tune musically.
- Dynamic tempo scaling naturally changes seconds-per-beat, while beat-space windows remain consistent with musical feel.

Example judgment:

```text
target beat: 13.00
input beat: 13.08
delta beat: +0.08
result: Perfect
```

## Judgment Windows

Use beat-space configuration.

Suggested initial values:

```text
perfectWindowBeats = 0.08
goodWindowBeats = 0.18
missWindowBeats = 0.30
```

At 120 BPM:

- `0.08 beats` is about 40 ms.
- `0.18 beats` is about 90 ms.
- `0.30 beats` is about 150 ms.

These values should be configurable in a ScriptableObject, not hardcoded.

## Config Assets

### VRInputRecognitionProfile

Purpose: tune how physical/simulated VR input becomes semantic action intent.

Suggested fields:

```text
analogPressThreshold
verticalPositionThreshold
directionMinDot
simultaneousPressToleranceSeconds
grappleHoldStartSeconds
grappleUpdateIntervalSeconds
```

This profile belongs near the VR input layer.

### RhythmJudgementProfile

Purpose: tune how rhythm input events are judged against chart events.

Suggested fields:

```text
perfectWindowBeats
goodWindowBeats
missWindowBeats
earlyInputBufferBeats
minimumHoldCompletionPercent
wrongInputPenaltyEnabled
badTimingPenaltyEnabled
missPenaltyEnabled
```

This profile belongs near the rhythm judge.

### DynamicTempoProfile

Purpose: tune tempo recovery/failure behavior.

Suggested fields:

```text
normalTempoScale = 1.0
missPenalty = 0.03
successRecovery = 0.01
distortionThreshold = 0.90
failureThreshold = 0.75
```

The current tests already imply this behavior.

## Chart Contract

The chart should describe expected player actions in beat-space.

Suggested event fields:

```csharp
public struct RhythmActionEvent
{
    public float beat;
    public float durationBeats;

    public VRParkourActionType actionType;
    public VRHand hand;
    public VRHandDirection direction;

    public int lane;
    public int segment;
    public string moduleId;
}
```

Required gameplay fields:

- `beat`
- `durationBeats`
- `actionType`
- `hand`
- `direction`

Optional grid/module fields:

- `lane`
- `segment`
- `moduleId`

The chart should not care about raw controller buttons. It should care about expected parkour action intent.

## Grid and World Mapping

Use a small beat-space grid for level generation and gameplay alignment.

Suggested mapping:

```text
worldZ = beat * unitsPerBeat
worldX = lane * laneWidth
lengthZ = durationBeats * unitsPerBeat
```

Recommended initial values:

```text
unitsPerBeat = 2.5
laneWidth = 2.0
```

Grid fields should describe where the expected action appears in the route, while beat fields describe when the input is judged.

Example chart row:

```text
beat: 13.0
durationBeats: 1.0
actionType: Slide
hand: Both
direction: Down
lane: 0
segment: 8
moduleId: LowBeam
```

## Long Press and Streaming Input

Long press actions need phases. Grapple should not be treated as a single tap.

Recommended phase behavior:

```text
Started   -> valid hold first crosses start threshold
Updated   -> repeated while hold remains valid
Completed -> hold lasted long enough for chart requirement
Canceled  -> hold released or pose became invalid too early
```

For grapple:

- `Started` is judged near the chart start beat.
- `Updated` drives rope, beam, progress UI, sound, and visual feedback.
- `Completed` confirms the hold requirement was satisfied.
- `Canceled` can become a miss if released before minimum completion.

Hold completion should be judged by percentage of chart duration, not exact end-frame timing.

Suggested initial rule:

```text
minimumHoldCompletionPercent = 0.80
```

Example:

```text
chart beat: 17.0
durationBeats: 2.0
minimum valid hold: 1.6 beats
```

## Tap Action Behavior

Tap-like actions emit one `Performed` event:

- Step
- SideGrab
- Slide
- LongJump

The rhythm judge compares the event against the nearest unresolved chart event.

For two-hand actions:

- The recognition layer decides whether the two-hand input is valid.
- The rhythm judge only sees `Slide` or `LongJump`.
- The chart still records `hand = Both` and direction for validation/readability.

## Input Buffering

The adapter may keep a short input buffer.

Purpose:

- Preserve very early inputs until a matching beat event enters the judge window.
- Avoid losing a player input that happened just before the target became active.

Suggested buffer:

```text
earlyInputBufferBeats = 0.20
```

Buffered events should expire after they are too old to match any pending chart event.

This should remain in the adapter or rhythm judge, not in `VRInputReader`.

## Result Flow

Suggested runtime flow:

```text
VRInputReader
  -> VRParkourInputEvents
  -> VRRhythmInputAdapter
  -> RhythmJudge
  -> Gameplay feedback / tempo state / level flow
```

Result events from rhythm judge should include:

```text
chart event id
input sequence id
judgement result
delta beats
tempo scale after result
```

Example result:

```text
eventId: 8
inputSequenceId: 42
result: Good
deltaBeats: -0.12
tempoScale: 0.98
```

## Wrong Input Policy

Initial recommendation:

- Wrong action inside an active window: count as wrong input and apply penalty.
- Input too early but within buffer: hold for possible later match.
- Input too early outside buffer: ignore or soft-fail, depending on playtest feel.
- Input too late after miss window: miss.
- Missing an expected chart event: miss.

For the demo, wrong input and bad timing can both use the same tempo penalty first. More detailed scoring can come later.

## Integration API

The beat controller should expose one method:

```csharp
public void SubmitInput(RhythmInputEvent inputEvent);
```

The adapter should expose one event:

```csharp
public event Action<RhythmInputEvent> InputSubmitted;
```

The bridge between them can be either:

- direct serialized reference, or
- a scene-level event channel.

For the current project size, a serialized reference is enough.

## Naming Recommendation

Use "rhythm input" for adapter outputs.

Use "VR input" only for hardware/recognition layers.

This keeps the boundary clear:

- `VRInputReader`: hardware state
- `VRParkourInputEvents`: recognized physical intent
- `VRRhythmInputAdapter`: timed rhythm input
- `RhythmJudge`: chart comparison and scoring

## Implementation Notes

1. Keep `VRInputReader` independent from music.
2. Keep `VRParkourInputEvents` independent from chart timing.
3. Add `VRRhythmInputAdapter` as the only place that reads both recognized input and rhythm clock state.
4. Add tests for tap matching, wrong action, early buffer, hold completion, and hold cancel.
5. Keep tolerance windows in beats.
6. Keep recognition thresholds in seconds/dots/meters.

## Open Questions

- Should `beatTime` come from an audio DSP clock or a game-time clock adjusted by tempo scale?
- Should failed inputs still trigger parkour animation immediately, or only after the chart event resolves?
- Should wrong input during no active chart window penalize tempo, or only inputs near expected events?
- Should hold `Completed` require release after the hold, or simply reaching enough duration?
- Should chart events use `VRHand.Both`, or should rhythm-facing hand enum be separate from VR-facing hand enum?

## Immediate Next Step

Create a minimal `VRRhythmInputAdapter` prototype with:

- serialized reference to `VRParkourInputEvents`
- serialized reference to a rhythm clock provider
- `InputSubmitted` event
- conversion from recognized input to rhythm input
- basic phase mapping for tap and grapple events

Then update the rhythm prototype so it consumes adapter events instead of `KeyCode.Space`.
