# Rhythm Parkour Level Generation Design

## Goal

Build formal rhythm parkour demos from chart data instead of hand-placing scene objects.

The chart is the timing source. The level generator turns chart events into a straight runway, beat stripes, action cues, and the goal portal.

## Data Model

`RhythmActionChart` remains the authored chart asset.

Each `RhythmActionEvent` describes only gameplay timing:

```text
beat
durationBeats
actionType
hand
direction
```

Scene objects are generated from this data. They should not be edited as the source of truth.

## Generation Layers

### Chart

The chart answers: "What happens on which beat?"

Examples:

```text
8   Step      Left
9   Step      Right
12  Slide     Both Down
16  LongJump  Both Up
24  Grapple   Right Up duration 4
```

### Theme

`RhythmParkourLevelTheme` owns reusable materials for generated objects:

```text
runway
beat stripe
downbeat stripe
action cue
danger cue
side wall
goal portal
hidden cue
```

The first version creates transient editor materials matching the current demo look. Later versions can load shared material assets or prefab sets.

### Builder

`RhythmParkourLevelBuilder` owns placement rules:

```text
z = beat * unitsPerBeat
Step hand -> left/right lane
SideGrab direction -> side wall lane
Slide -> low gate
LongJump -> takeoff/gap/landing
Grapple duration -> hold path length
```

It also creates:

```text
runway
left/right edge glow
beat stripes
action cues
goal portal
```

### Validator

The builder includes a lightweight chart validator for obvious authoring mistakes:

```text
null events
negative duration
multiple actions on the same beat
events authored before beat 0
```

This is intentionally conservative. Comfort and difficulty checks can be added once formal TellingWorld sections are authored.

## TellingWorld Workflow

Keep two separate scenes:

```text
TellingWorldCalibrationDemo
TellingWorldFormalDemo
```

The calibration scene remains simple:

```text
Step-only beat 8 through 31
runtime offset input enabled
miss tempo penalty disabled
```

The formal scene should be generated from a separate chart after offset is stable.

The manual source of truth for the formal chart is:

```text
Assets/Rhythm/FormalDemos/TellingWorldFormalDemo_Chart.tsv
```

Edit this TSV file to control beat-to-action mapping. The generated
`TellingWorldFormalDemo_Chart.asset` and `TellingWorldFormalDemo.unity` should
be rebuilt from the TSV instead of edited by hand.

TSV columns:

```text
beat
durationBeats
actionType
hand
direction
note
```

Allowed values:

```text
actionType: Step, SideGrab, Slide, LongJump, Grapple
hand:       None, Left, Right, Both
direction:  None, Up, Down, Left, Right
```

Rows starting with `#` are comments. Blank rows are ignored. If a row has an
invalid value, the editor logs a warning and skips that row.

Current first-pass formal section structure:

```text
8-17:   simple step intro plus first side grabs
20-40:  slide and long-jump introduction
44-52:  grapple hold section
56-76:  mixed steps, slide, jump, side grabs, and left grapple
80-96:  final step, slide, jump, and right grapple phrase
```

This chart is intentionally readable for demo recording. It uses the reusable
placement rules rather than scene-authored blocks, so changing a beat in the
chart updates both gameplay timing and the visible obstacle/cue position.

## Implementation Status

Implemented first:

```text
Assets/Editor/RhythmParkour/RhythmParkourLevelBuilder.cs
Assets/Editor/RhythmParkour/MusicVrDemoSceneBuilder.cs integration
```

The current generated scenes still use primitive cubes, but the placement logic is now reusable and chart-driven.

Implemented second:

```text
TellingWorldFormalDemo generation entry
TellingWorldFormalDemo chart definition using Step, SideGrab, Slide, LongJump, Grapple
Request-file rebuild hook for the formal demo scene
```

Implemented third:

```text
Formal demo theme now reuses VisualDemos/VFX material assets
TellingWorldFormalDemo uses the updated visual skybox
Generated formal scene set dressing adds side city silhouettes, window light strips, upper cloud sheets, and far neon rails
Calibration scene remains visually simple for offset testing
```

Implemented fourth:

```text
TellingWorldFormalDemo chart is loaded from a manually editable TSV file
Initial full-length manual chart covers beat 8 through beat 196
Regenerating the formal demo updates both Chart asset data and scene obstacle placement
```

Next steps:

```text
Add richer validation for comfort and impossible chains
Optionally replace primitive generation with prefab-set generation
Tune the formal chart against the actual song phrases after recording review
```
