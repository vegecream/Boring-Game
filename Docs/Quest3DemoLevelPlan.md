# Quest3 Demo Level Plan

## Goal

Build a short Quest3-ready demo level that proves the current OpenXR/XRI setup, VR input recognition, and first-person rhythm movement loop work together in headset.

## Scope

- Use `Assets/Scenes/SampleScene.unity` as the playable baseline.
- Keep one active XR stack: Unity OpenXR + XRI `XR Origin`.
- Keep the Meta building-block camera rig disabled or remove it after headset validation.
- Keep the build target focused on Android ARM64, OpenXR, and Quest3.

## Level Beat

Create a 30-45 second tutorial run with clear action beats:

- Warm-up: alternating left/right step inputs.
- Direction check: side grab left and side grab right.
- Body motion check: slide under a low obstacle.
- Air-time check: long jump across a visible gap.
- View tracking check: physical player turn left/right recognized by HMD yaw.
- Hold check: grapple hold or sustained upward-hand action.

## Scene Layout

- Start pad with player facing forward.
- Ground lane with visible floor pattern only on the ground.
- Non-solid skybox/background so headset orientation is easy to verify.
- One scene should contain all onboarding guide locations. Each location owns a `VRGuideLevelMarker`, a visible interaction marker, and a floating `TextMesh` sign.
- A single `VRGuideLevelController` advances through `VRGuideSequence` levels and toggles the matching marker active.
- Color-coded action gates:
  - step gates: left/right lane markers
  - slide gate: low arch
  - jump gate: gap or raised platform
  - turn gate: arrow signage at the side
  - grapple gate: vertical or forward pull marker

## Onboarding Guide Locations

Use one `VRGuideLevelDefinition` per operation and place its marker in the same scene:

- `Guide_Step_Left` and `Guide_Step_Right`: two floor pads near the start lane.
- `Guide_SideGrab_Left` and `Guide_SideGrab_Right`: side wall markers at shoulder height.
- `Guide_Slide`: a low arch with a floor arrow leading under it.
- `Guide_LongJump`: a short gap or raised platform with a landing pad.
- `Guide_Turn_Left` and `Guide_Turn_Right`: arrow signs placed to the left and right of the player lane.
- `Guide_Grapple_Hold`: an overhead/forward marker that requires a sustained upward hand hold.

Marker setup in the scene:

1. Create an empty object at the interaction location, for example `Guide_Slide_Marker`.
2. Add `VRGuideLevelMarker`.
3. Assign the matching `VRGuideLevelDefinition`.
4. Put the gate mesh, arrow, pad, or obstacle in `Active Only Objects`.
5. Add a child object above eye level with a `TextMesh`; assign it to `Sign Text`.
6. Keep `Face Main Camera` enabled so the sign turns toward the player.
7. Assign the same `VRGuideLevelController` used by the scene.

## Implementation Tasks

- Add or configure `VRGuideLevelController` with a `VRGuideSequence` containing all operation guide levels.
- Place one `VRGuideLevelMarker` per operation location in `Assets/Scenes/SampleScene.unity`.
- Add lightweight obstacle visuals that match `RhythmActionEvent` type, hand, direction, and duration.
- Add a debug overlay toggle that can show last recognized action in headset builds.
- Add scene reset/recenter controls for fast headset iteration.
- Add a build validation checklist in docs once the first headset pass is complete.

## Acceptance Checks

- Build and Run installs to Quest3 without Android SDK/JDK/Gradle errors.
- In headset, sky is above and ground is below after launch and after recentering.
- Only one active `MainCamera` is present.
- Step, side grab, slide, long jump, grapple, and turn recognition are visible in debug feedback.
- First-person motion advances the XR Origin without unwanted constant drift.
- The level remains comfortable: small camera offsets, smooth jump arc, no abrupt roll/pitch spikes.
