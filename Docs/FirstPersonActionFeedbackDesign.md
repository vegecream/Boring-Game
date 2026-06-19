# First Person Action Feedback Design

## Purpose

The cylinder proxy was useful for checking action states, but it is not the final first-person expression. The next prototype uses subtle camera-parent motion to show parkour actions from the player's point of view.

The goal is not to simulate a full body. The goal is to make the player feel the action through camera motion, hand/controller effects, environment motion, and later VFX/audio.

## Runtime Boundary

`FirstPersonActionFeedbackDriver` listens to the same sources as the proxy driver:

```text
VRRhythmActionPrototype.JudgmentResolved / ActionMissed
VRParkourInputEvents.InputRecognized for debug input testing
keyboard keys 1-5 for isolated feedback testing
```

Rhythm result events should remain the final gameplay source. Recognized input events are only for debugging simulator/controller recognition.

## Motion Roots

Do not animate the tracked XR camera transform directly. Use a parent transform, normally the XR Origin `Camera Offset`.

Default auto-binding:

```text
Camera.main.transform.parent -> Motion Root
```

This keeps the effect compatible with XR tracking, because the headset pose still updates inside the animated parent.

The prototype now separates temporary first-person pose from actual player travel:

```text
Motion Root     -> XR Origin / Camera Offset, for temporary bob, lean, lower, and look offsets
Locomotion Root -> XR Origin (XR Rig), for accumulated world-space travel
```

The motion root always returns to its rest pose after an action. The locomotion root does not reset; it is moved forward by successful actions so the player actually advances through the scene.

## Feedback Mapping

Initial values are intentionally small for VR comfort:

```text
Step      -> tiny up bob, side sway, short forward pulse, mild yaw/roll
SideGrab  -> side sway and slight yaw
Slide     -> staged lower/hold/recover motion, low-angle upward view
LongJump  -> staged takeoff/air/landing motion with a smooth air-time plateau
Grapple   -> smooth forward pull with slight lift
Miss      -> short lateral shake layered on the action
```

These are prototypes. If a motion feels uncomfortable, reduce the serialized values first instead of removing the action path.

## Root Locomotion

Successful actions can move the locomotion root:

```text
Step      -> accelerated forward travel
Slide     -> staged forward travel matched to lower/hold/recover
LongJump  -> staged forward travel matched to takeoff/air/landing
Grapple   -> smooth forward pull over action duration
SideGrab  -> currently pose-only, no root travel
Miss      -> pose feedback only by default
```

Step movement intentionally ignores left/right for travel direction. Left and right steps both advance forward; the hand only changes the body sway and camera roll side. This keeps rhythm stepping readable while still creating alternating body motion.

## Camera Orientation

Camera orientation is authored as a small local offset on the motion root, not on the tracked headset transform. It is applied after positional feedback so the same action can combine travel, bob, and view direction.

Current defaults are deliberately conservative:

```text
Step      -> about 2 degrees roll plus sub-degree yaw
Slide     -> about 8 degrees upward look while lowered
LongJump  -> about -2.5 to +2 degrees pitch across takeoff, air, and landing
Grapple   -> about 3 degrees upward look
```

Long jump uses a softer vertical curve than the first prototype. Takeoff eases toward the top, air time holds near the peak with a subtle float, and landing eases back down. This avoids the previous steep jump curve and reduces abrupt view changes in VR.

## Current Scene Setup

`Assets/Scenes/SampleScene.unity` is the current first-person test scene.

Scene bindings:

```text
VR Input System / FirstPersonActionFeedbackDriver
Motion Root     = XR Origin (XR Rig) / Camera Offset
Locomotion Root = XR Origin (XR Rig)
```

Scene rendering:

```text
Ground material = M_SynthwaveBlock_Cyan
Ground shader   = RhythmParkour/Synthwave Grid Block
Ground scroll   = disabled
Skybox          = M_SoftNightPanoramicSkybox
```

Only the ground should use the special synthwave grid pattern. The skybox should remain a non-solid panoramic background so the whole scene does not look like it is tinted by a flat color.

## Test Controls

```text
1 -> Step
2 -> SideGrab
3 -> Slide
4 -> LongJump
5 -> Grapple
Left Shift + key -> miss version
```

Simulator/controller recognized actions also trigger feedback when `Play Recognized Input Events` is enabled.
