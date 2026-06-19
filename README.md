# Rhythm Parkour

Rhythm Parkour is a Unity VR rhythm-runner prototype that combines beat-based music gameplay with first-person parkour actions. The player follows a linear floating track and performs hand inputs on the beat. Correct inputs keep the run stable; missed or wrong inputs slow the music, distort the scene, and push the world toward failure.

The project was developed as a short VR music parkour demo: one fixed route, five core action types, world-space rhythm prompts, first-person motion feedback, and a dynamic tempo punishment system.

## Demo Video

<video src="Docs/rhythm_parkour.mp4" controls width="100%"></video>

If the embedded player is not shown, open the demo video directly:

[Watch rhythm_parkour.mp4](Docs/rhythm_parkour.mp4)

## 分工合作

| 模块 | 负责人 | 主要工作 |
| --- | --- | --- |
| 节奏与音乐系统 | 张博瑞、倪煜晖 | 根据 BPM 和首拍偏移生成节拍判定区间，维护动态速度倍率、命中恢复和失误惩罚 |
| VR 输入与动作判定 | 邓建斌 | 读取左右手控制器输入与方向，将玩家动作映射为 Step、Slide、Jump、Side Grab、Grapple 等事件 |
| 单动作 Demo 与教程 | 张博瑞、邓建斌 | 制作五类基础动作的小 Demo，验证每种动作的节奏窗口、输入方式和反馈表现 |
| 第一人称反馈 | 张博瑞、邓建斌 | 设计镜头父节点的小幅运动曲线、命中反馈、失误震动、速度感后处理和 VR 舒适性参数 |
| 场景与关卡生成 | 张博瑞 | 根据人工谱面顺序摆放交互物体，生成线性跑酷路线、视觉预览和终点传送门 |
| 美术与视觉规范 | 张博瑞 | 制作开放宇宙背景、悬浮荧光轨道、蓝紫红空间渐变、发光材质和失稳视觉效果 |
| 测试与调参 | 倪煜晖 | 编写 EditMode 测试，验证节奏判定、动态 BPM、谱面配置和 VR 动作匹配逻辑 |
| 汇报与展示 | 倪煜晖、邓建斌 | 准备课程汇报 PPT、录制实机演示视频、整理项目 README 和说明文档 |

## Core Gameplay

The game loop is built around three connected systems:

- **Beat Interval**: split the song timeline by BPM and first-beat offset to produce rhythm judgment windows.
- **Action Event**: map authored chart events to expected VR hand inputs such as step, slide, jump, side grab, and grapple.
- **Scene Gen**: place interaction objects and route modules from the authored chart to assemble the final level.

At runtime, the music starts, beat prompts appear along the route, and the player performs the expected VR action inside the timing window. Hits trigger parkour motion and speed feedback. Misses still allow the route performance to continue, but they reduce the tempo scale and increase audio/visual instability.

## VR Actions

| Action | Player Input | In-Game Movement | Purpose |
| --- | --- | --- | --- |
| Step | Alternate left/right hand button presses | Step across floating platforms | Establishes the basic rhythm-run pattern |
| Side Grab | Press the specified hand while pointing left or right | Wall-run or side-grab movement | Adds directional hand judgment |
| Slide | Press both hands while pointing down | Slide under low obstacles | Adds body posture and synchronized input |
| Long Jump | Press both hands while pointing up | Leap across a large gap | Creates a strong-beat visual climax |
| Grapple | Hold one specified hand upward | Grapple pull along a light line | Adds sustained input and long-note timing |

## Dynamic Tempo And Failure

The failure model is designed to feel like the player is pulling the whole song and world out of rhythm:

- Normal tempo scale starts at `1.0`.
- A miss reduces tempo, currently by about `0.03`.
- A success gradually restores tempo, currently by about `0.01`.
- Below `0.9`, music distortion and visual instability begin.
- Below the failure threshold, the run enters a failed state.

This makes mistakes recoverable, while repeated mistakes create escalating pressure through slower BPM, distorted audio, camera feedback, red warning visuals, and unstable space effects.

## Project Structure

| Path | Description |
| --- | --- |
| `Assets/Scripts/RhythmParkour/` | Beat timing, chart data, tempo state, rhythm track config, and single-button prototype logic |
| `Assets/Scripts/VRInput/` | VR input reading, action matching, rhythm action session, first-person feedback, menus, and debug tools |
| `Assets/Editor/RhythmParkour/` | Unity editor builders for demo scenes, visual previews, generated levels, and project setup |
| `Assets/Shaders/` | URP shaders for rhythm bridges, ribbons, grid blocks, and glitch effects |
| `Assets/Tests/EditMode/` | EditMode tests for tempo state, chart config, rhythm judgment, and VR input matching |
| `Docs/` | Design notes, demo plans, workflow notes, and the demo video |
| `Packages/` | Unity package manifest and lock file |
| `ProjectSettings/` | Unity project configuration |

## Requirements

- Unity `2022.3.62f3`
- Universal Render Pipeline `14.0.12`
- XR Interaction Toolkit `2.6.5`
- OpenXR `1.14.3`
- Meta XR SDK `201.0.0`
- A VR headset supported by the configured OpenXR / Meta XR path, such as Meta Quest 3

## Running The Project

1. Open the repository root in Unity `2022.3.62f3`.
2. Let Unity restore packages from `Packages/manifest.json`.
3. Open one of the generated demo scenes under `Assets/Scenes/` if the full asset checkout is available.
4. For desktop testing, use the keyboard fallback in the action prototypes:
   - `Space`: start playback or hit normal actions.
   - Hold `Space`: test grapple sustain.
   - `R`: reset the prototype.
5. For VR testing, use controller button input and hand direction; the current input path tries Meta `OVRInput` first, then falls back to Unity Input System and XR input devices.

## Team Work Table

| Work Package | Main Deliverables | Related Files |
| --- | --- | --- |
| Rhythm system | BPM timing, beat windows, chart events, tempo scale, dynamic recovery and punishment | `Assets/Scripts/RhythmParkour/` |
| VR input system | Controller input snapshots, hand direction detection, action matching, keyboard fallback | `Assets/Scripts/VRInput/VRInputReader.cs`, `Assets/Scripts/VRInput/VRExpectedActionInputMatcher.cs` |
| Action prototypes | Step, side grab, slide, long jump, and grapple demo behavior | `Assets/Scripts/VRInput/VRRhythmActionPrototype.cs`, `Docs/Demo.md` |
| First-person feedback | Camera-parent motion curves, speed feel, hit/miss feedback, VR comfort tuning | `Assets/Scripts/VRInput/FirstPersonActionFeedbackDriver.cs`, `Assets/Scripts/VRInput/VRSpeedFeelDriver.cs` |
| Scene and level generation | Generated rhythm demo scenes, route modules, portal and visual preview builders | `Assets/Editor/RhythmParkour/` |
| Visual design | Floating neon track, open-space background, blue-purple-red danger gradient, URP effects | `Assets/Shaders/`, `Docs/RhythmParkourLevelGenerationDesign.md` |
| Testing and validation | EditMode tests for timing, tempo, chart config, and VR judgment rules | `Assets/Tests/EditMode/` |
| Presentation and demo | Course presentation, recorded gameplay video, project README | `Docs/rhythm_parkour.mp4`, `README.md` |

## Current Scope

This is a demo-scale prototype. It focuses on verifying whether VR hand input, rhythm timing, first-person parkour feedback, and dynamic music instability can form a coherent short experience. It intentionally does not include free movement, a level editor, a full player avatar, complex scoring, or physically simulated climbing.

## Acknowledgements

Thanks to Chen Qi for music selection, classmates who provided offline playtest feedback, and Codex for assisted implementation and documentation support.
