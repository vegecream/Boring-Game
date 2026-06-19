# Player Action Animation Design

## Purpose

The current demo is first-person, but it still needs visible action feedback while we develop rhythm gameplay. A simple ground plane plus a cylinder or ball is suitable for this stage. The object acts as a visual proxy for the player action, not as the real XR player body.

## Runtime Boundary

The animation system should react to rhythm gameplay results, not raw controller input.

Current flow:

```text
RhythmActionChart
  -> VRInputReader / keyboard fallback
  -> VRRhythmActionSession
  -> VRRhythmJudgmentResult or missed chart action
  -> PlayerActionAnimationDriver
```

This keeps the animation aligned with the authored chart. It also matches the game design in `Demo.txt`: when the player misses, the parkour action can still play, while tempo, audio, and world feedback carry the penalty.

## Scene Setup

Recommended hierarchy:

```text
XR Origin
  Camera Offset
    Main Camera
    Left Controller
    Right Controller

Ground Plane

PlayerVisualProxy
  Cylinder
  PlayerActionAnimationDriver
```

The camera rig should stay independent from the visual proxy during prototype work. Moving the camera with every squash, bounce, or lean can be uncomfortable in VR. Later we can choose which action animations should affect the camera.

For current debug testing, the visual proxy can follow the camera as a visible first-person reference. `PlayerActionAnimationDriver` supports this by using `Camera.main` when `Follow Target` is empty. By default it follows the camera's world position and yaw only, so looking up or down does not make the body proxy float vertically through the view.

## Component Contract

`VRRhythmActionPrototype` publishes:

- `JudgmentResolved`: a chart action was judged from player input.
- `ActionMissed`: a chart action passed its miss deadline without a valid input.
- `CurrentTempoScale`: the current tempo multiplier used to scale animation duration.

`PlayerActionAnimationDriver` consumes those events and animates a target transform.

For isolated input debugging, it can also subscribe to `VRParkourInputEvents.InputRecognized`. This path is only for checking that recognized simulator/controller actions visibly move the proxy. Final rhythm gameplay should still prefer the chart/judgment result events.

## Prototype Motion Mapping

Initial procedural mappings:

```text
Step      -> small bounce and left/right tilt
SideGrab  -> lean and shift toward the authored side
Slide     -> squash downward and widen briefly
LongJump  -> upward and forward hop
Grapple   -> stretched forward pulse over a longer duration
```

Success and miss both play the authored action. A miss adds red tint and a short shake, so the user can see that the performance continued but the timing/input was bad.

## Implementation Notes

- Add `PlayerActionAnimationDriver` to the cylinder or to a parent object.
- Assign `Action Source` to the scene object that owns `VRRhythmActionPrototype`.
- For debug-only input testing, assign `Debug Input Source` to the scene object that owns `VRParkourInputEvents`, or leave it empty to auto-find one.
- Assign `Animated Target` to the cylinder transform. If left empty, the driver animates its own transform.
- Assign `Tint Renderer` to the cylinder renderer if color feedback is wanted.
- Leave `Follow Target` empty to follow `Camera.main`, or assign the XR camera explicitly.
- Keep `Follow Target Yaw Only` enabled for a body proxy. Disable it only for object-like props that should follow full view pitch and roll.
- Tune `Follow Local Offset` if the cylinder is too close, too low, or outside the player's view.
- Keep the driver in the VR input assembly for now because it consumes `VRRhythmActionPrototype` events directly.

## Debug Controls

`PlayerActionAnimationDriver` has keyboard debug controls so a plane plus cylinder scene can test animation without the full rhythm prototype source:

```text
1 -> Step
2 -> SideGrab
3 -> Slide
4 -> LongJump
5 -> Grapple
Left Shift + key -> play the miss version with red tint and shake
```

These controls only exercise the visual proxy. They do not judge rhythm input, update score, or change tempo.

If `Play Recognized Input Events` is enabled, recognized VR input also triggers the proxy:

```text
left/right step input -> Step animation
side grab input -> SideGrab animation
slide input -> Slide animation
long jump input -> LongJump animation
grapple hold started -> Grapple animation
```

Expected debug behavior:

- The cylinder stays near the player's body position instead of rotating up/down with the player's gaze.
- `1` alternates left/right step animation and adds a short forward pulse.
- `3` squashes the cylinder downward for slide.
- `4` hops upward/forward for long jump.

This is intentionally procedural. Animator clips can replace the transform tweens later without changing the rhythm result event surface.
