# Remote Visual Asset Troubleshooting

This note is for checking why another workspace cannot see the skybox, clouds, or Neo City models that are visible locally.

## Local Baseline

The local workspace is on `/main` at `cs:43`. The following visual assets exist locally and are version-controlled:

- Skybox material used by the formal/visual demos:
  - `Assets/Materials/VisualDemos/M_VisualDemo_Skybox.mat`
  - GUID: `ccf9812a7e4e32c488a25a3e2d33b6e0`
  - Added in `cs:40`
- Shared synthwave skybox shader:
  - `Assets/OpenSource/UnitySkyboxShaders/KeijiroHorizontalSkyboxURP.shader`
  - Added earlier and last updated in `cs:41`
- URP renderer with volumetric clouds renderer feature:
  - `Assets/Settings/URP/VR_URP_Renderer.asset`
  - Renderer feature material: `Assets/OpenSource/UnityVolumetricCloudsURP/VolumetricClouds/VolumetricClouds.mat`
  - Renderer updated in `cs:41`
- Neo City FBX models:
  - `Assets/External/KitBash3D/NeoCity/neocity/KB3D_NEC_BldgLG_A.fbx`
  - `Assets/External/KitBash3D/NeoCity/neocity/KB3D_NEC_BldgLG_B.fbx`
  - `Assets/External/KitBash3D/NeoCity/neocity/KB3D_NEC_BldgLG_C.fbx`
  - `Assets/External/KitBash3D/NeoCity/neocity/KB3D_NEC_BldgMD_A.fbx`
  - `Assets/External/KitBash3D/NeoCity/neocity/KB3D_NEC_BldgMD_B.fbx`
  - `Assets/External/KitBash3D/NeoCity/neocity/KB3D_NEC_BldgMD_C.fbx`
  - `Assets/External/KitBash3D/NeoCity/neocity/KB3D_NEC_BldgSM_A.fbx`
  - `Assets/External/KitBash3D/NeoCity/neocity/KB3D_NEC_BldgSM_B.fbx`
  - `Assets/External/KitBash3D/NeoCity/neocity/KB3D_NEC_BldgSM_C.fbx`
  - Added in `cs:36`
- Neo City 2k textures:
  - `Assets/External/KitBash3D/NeoCity/2k/*.png`
  - Added in `cs:36`
- Neo City imported materials:
  - `Assets/External/KitBash3D/NeoCity/neocity/Materials/*.mat`
  - Added in `cs:36`

## Scenes To Check

Open these scenes first:

- `Assets/Scenes/VisualDemos/ForwardCityRushVisualDemo.unity`
  - Expected: gradient synthwave skybox, volumetric cloud layer, rhythm platform, and Neo City building prefabs passing around the path.
  - Uses `M_VisualDemo_Skybox.mat`.
  - References Neo City FBX GUIDs from `Assets/External/KitBash3D/NeoCity/neocity`.
- `Assets/Scenes/FormalDemos/TellingWorldFormalDemo.unity`
  - Expected: gradient skybox and generated visual city blocks.
  - Important: most "Visual City Block" objects in this scene are generated primitive blocks with VisualDemo materials, not raw KitBash FBX instances.
- `Assets/Scenes/NeoCityAllAssetImportTest.unity`
  - Expected: raw KitBash Neo City FBX models are visible with their imported materials.
  - Use this to isolate whether the problem is asset import or a demo scene setup.

## Quick Remote Checks

Run these in the Unity Version Control workspace root:

```powershell
& "C:\Users\fisan\AppData\Roaming\UnityHub\external-modules\plastic\client\cm.exe" status
& "C:\Users\fisan\AppData\Roaming\UnityHub\external-modules\plastic\client\cm.exe" update
& "C:\Users\fisan\AppData\Roaming\UnityHub\external-modules\plastic\client\cm.exe" ls "Assets\Materials\VisualDemos"
& "C:\Users\fisan\AppData\Roaming\UnityHub\external-modules\plastic\client\cm.exe" ls "Assets\External\KitBash3D\NeoCity\neocity"
& "C:\Users\fisan\AppData\Roaming\UnityHub\external-modules\plastic\client\cm.exe" ls "Assets\External\KitBash3D\NeoCity\2k"
```

The workspace should be at least `cs:43` on `/main`. If `Assets/External/KitBash3D/NeoCity` is missing or empty, the workspace did not download the large model/texture assets.

## Unity Checks

In Unity, check these items:

- `Project Settings > Graphics`
  - The custom render pipeline should reference `Assets/Settings/URP/VR_URP_Pipeline.asset`.
- `Project Settings > Quality`
  - The active quality level should not override the render pipeline with an empty or different pipeline asset.
- `Assets/Settings/URP/VR_URP_Renderer.asset`
  - It should include the `VolumetricCloudsURP` renderer feature.
- Scene `RenderSettings`
  - `ForwardCityRushVisualDemo` and `TellingWorldFormalDemo` should use `M_VisualDemo_Skybox`.
- Main camera
  - Clear Flags should be `Skybox`.
  - Far clip plane should be large enough for the city/path objects.

## Common Failure Modes

- Workspace is not updated to head.
  - Fix: run `cm update` and confirm `/main ... (cs:43 - head)` or newer.
- Large assets were cloaked or partially downloaded.
  - Symptom: scene has missing prefab/model references, or `Assets/External/KitBash3D/NeoCity` is absent.
  - Fix: remove cloaking/partial rules for `Assets/External/KitBash3D`, then force update.
- Unity has not imported the large FBX/texture assets yet.
  - Symptom: files exist on disk, but scene shows missing meshes/materials immediately after update.
  - Fix: wait for import, or use `Assets > Reimport All` for `Assets/External/KitBash3D/NeoCity`.
- URP pipeline is not active.
  - Symptom: URP materials, custom skybox, or volumetric cloud rendering look wrong.
  - Fix: set Graphics/Quality to `VR_URP_Pipeline.asset`.
- Scene-specific setup is the problem rather than the asset package.
  - Test: open `NeoCityAllAssetImportTest.unity`. If the raw FBX models are visible there, the package is present and imported; inspect the specific demo scene camera/lighting/material overrides.

## Local Conclusion

On the local machine, the skybox material, skybox shader, URP renderer feature, Neo City FBX files, imported materials, and 2k texture files are present and version-controlled. If a remote workspace cannot see them after updating to `cs:43` or newer, the most likely causes are incomplete workspace update, cloaked/partial asset download, inactive URP pipeline, or Unity import not completed.
