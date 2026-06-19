# Quest3 Onboarding Scene Setup

This setup keeps all beginner guide interactions in one Unity scene.

## Runtime Pieces

- `VRGuideLevelDefinition`: one operation lesson, for example left step, slide, or turn right.
- `VRGuideSequence`: ordered list of lessons.
- `VRGuideLevelController`: scene controller that listens to `VRParkourInputEvents` and advances the guide sequence.
- `VRGuideLevelMarker`: scene marker for one physical interaction location and its floating sign.

Default assets live in:

`Assets/GuideLevels/Quest3Onboarding/`

Use `Quest3_Onboarding_Guide_Sequence.asset` as the first guide sequence.

## Scene Setup

Fast path:

1. In Unity, run `Rhythm Parkour/Rebuild Quest3 Onboarding Demo In SampleScene`.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Find `Quest3 Onboarding Demo` in the Hierarchy.
4. Adjust the generated cube markers, gates, and floating signs as needed.

Manual setup:

1. Create an empty object named `Guide System`.
2. Add `VRGuideLevelController`.
3. Assign `Sequence` to `Assets/GuideLevels/Quest3Onboarding/Quest3_Onboarding_Guide_Sequence.asset`.
4. Assign `Input Events` to the scene object that owns `VRParkourInputEvents`.
5. Keep `Auto Start` enabled.

For each interaction location:

1. Create an empty object, for example `Guide_Slide_Marker`.
2. Put it at the physical tutorial location.
3. Add `VRGuideLevelMarker`.
4. Assign `Controller` to `Guide System`.
5. Assign `Level` to the matching guide level asset.
6. Add visible objects such as pads, arrows, arches, side walls, or grapple markers.
7. Assign those objects to `Active Only Objects` if they should appear only during this lesson.
8. Add a child object above the marker named `Floating Sign`.
9. Add a `TextMesh` component to `Floating Sign`.
10. Assign that `TextMesh` to `Sign Text`.
11. Keep `Face Main Camera` enabled.

## Recommended Locations

- `Guide_Step_Left`: left floor pad, near the start.
- `Guide_Step_Right`: right floor pad, one step after the left pad.
- `Guide_SideGrab_Left`: left wall marker at shoulder height.
- `Guide_SideGrab_Right`: right wall marker at shoulder height.
- `Guide_Slide`: low gate or arch with the sign above the gate.
- `Guide_LongJump`: short gap with a clear landing pad.
- `Guide_Turn_Left`: left arrow sign, placed slightly to the side of the lane.
- `Guide_Turn_Right`: right arrow sign, placed slightly to the side of the lane.
- `Guide_Grapple_Hold`: overhead or forward target marker.

## Floating Sign Rules

- Use large `TextMesh` text and keep it about 1.8-2.2 meters above the floor.
- Keep the sign 1.5-3 meters from the player path so it does not block the camera.
- Use `Hide Sign When Inactive` when only the current lesson should be visible.
- Disable `Hide Sign When Inactive` if you want the whole tutorial route visible from the start.
- Put meshes or arrows in `Inactive Only Objects` if you want preview markers before the lesson becomes active.

## Debug Controls

In editor or keyboard fallback testing:

- `R`: restart current guide level.
- `N`: skip to the next guide level.

The controller also shows a desktop `OnGUI` overlay. For Quest headset UI, connect `Message Changed` to a world-space text object or keep using each marker's `Sign Text`.
