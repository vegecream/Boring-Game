using System.Collections.Generic;
using RhythmParkour;
using UnityEditor;
using UnityEngine;

namespace RhythmParkour.Editor
{
    public sealed class RhythmParkourLevelBuildOptions
    {
        public float UnitsPerBeat { get; set; } = 2.5f;

        public float MinimumEndBeat { get; set; } = 16f;

        public bool AlignBeatStripesToEvents { get; set; }

        public bool UseTrackPositionMap { get; set; } = true;

        public float GrappleExtraUnitsPerBeat { get; set; } = RhythmTrackPositionMapper.DefaultGrappleExtraUnitsPerBeat;

        public bool CarveActionRunwayGaps { get; set; } = true;

        public float RunwayGapPaddingBeforeBeats { get; set; }

        public float RunwayGapPaddingAfterBeats { get; set; } = 0.18f;

        public float RunwayGapMergeThresholdBeats { get; set; } = 0.85f;

        public float RunwayGapStartAfterBeatOffset { get; set; } = 0.08f;
    }

    public sealed class RhythmParkourLevelTheme
    {
        public Material Runway { get; private set; }
        public Material Edge { get; private set; }
        public Material Beat { get; private set; }
        public Material Downbeat { get; private set; }
        public Material Start { get; private set; }
        public Material Action { get; private set; }
        public Material Danger { get; private set; }
        public Material SideWall { get; private set; }
        public Material GrappleAnchor { get; private set; }
        public Material Goal { get; private set; }
        public Material GoalEnergy { get; private set; }
        public Material Cue { get; private set; }
        public Material CitySilhouette { get; private set; }
        public Material WindowCyan { get; private set; }
        public Material WindowMagenta { get; private set; }
        public Material WindowRed { get; private set; }
        public Material Cloud { get; private set; }
        public Material GapMarker { get; private set; }

        public static RhythmParkourLevelTheme CreateDefault()
        {
            return new RhythmParkourLevelTheme
            {
                Runway = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_LightBridge.mat") ?? CreateMaterial("Demo_Runway_TransparentCyan", new Color(0.06f, 0.85f, 1f, 0.24f), true),
                Edge = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_Edge.mat") ?? CreateMaterial("Demo_Runway_Edge", new Color(0.35f, 1f, 1f, 0.78f), true),
                Beat = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_Beat.mat") ?? CreateMaterial("Demo_Beat_Stripe", new Color(0.22f, 0.65f, 1f, 0.55f), true),
                Downbeat = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_Downbeat.mat") ?? CreateMaterial("Demo_Downbeat_Stripe", new Color(1f, 0.24f, 0.9f, 0.72f), true),
                Start = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_WindowCyan.mat") ?? CreateMaterial("Demo_Start", new Color(0.1f, 1f, 0.62f, 0.65f), true),
                Action = LoadOrCreateActionMaterial(),
                Danger = LoadOrCreateDangerMaterial(),
                SideWall = LoadOrCreateSideWallGlitchMaterial(),
                GrappleAnchor = LoadOrCreateGrappleAnchorGlitchMaterial(),
                Goal = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_GrappleHook.mat") ?? CreateMaterial("Demo_Goal_Frame", new Color(0.18f, 0.34f, 1f, 0.9f), true),
                GoalEnergy = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_LightBridge.mat") ?? CreateMaterial("Demo_Goal_Energy", new Color(0.08f, 0.72f, 1f, 0.3f), true),
                Cue = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_CloudSheet.mat") ?? CreateMaterial("Demo_Hidden_Cue", new Color(1f, 0.84f, 0.1f, 0.2f), true),
                CitySilhouette = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_CitySilhouette.mat") ?? CreateMaterial("Demo_City_Silhouette", new Color(0.08f, 0.14f, 0.28f, 1f)),
                WindowCyan = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_WindowCyan.mat") ?? CreateMaterial("Demo_Window_Cyan", new Color(0.08f, 0.95f, 1f, 0.82f), true),
                WindowMagenta = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_WindowMagenta.mat") ?? CreateMaterial("Demo_Window_Magenta", new Color(1f, 0.18f, 0.95f, 0.82f), true),
                WindowRed = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_WindowRed.mat") ?? CreateMaterial("Demo_Window_Red", new Color(1f, 0.08f, 0.12f, 0.78f), true),
                Cloud = LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_CloudSheet.mat") ?? CreateMaterial("Demo_Cloud_Sheet", new Color(0.44f, 0.76f, 1f, 0.34f), true),
                GapMarker = LoadMaterial("Assets/Materials/Synthwave/M_SynthwaveBlock_Magenta.mat") ?? LoadMaterial("Assets/Materials/Synthwave/M_SynthwaveBlock_Cyan.mat")
            };
        }

        static Material LoadMaterial(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        static Material CreateMaterial(string name, Color color, bool transparent = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader)
            {
                name = name
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            return material;
        }

        static Material LoadOrCreateDangerMaterial()
        {
            const string path = "Assets/Materials/Synthwave/M_SynthwaveBlock_DangerRedBlue.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("RhythmParkour/Synthwave Grid Block");
            if (shader == null)
                return material ?? LoadMaterial("Assets/Materials/Synthwave/M_SynthwaveBlock_Red.mat") ?? CreateMaterial("Demo_Danger_Cue", new Color(1f, 0.08f, 0.12f, 0.7f), true);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.92f, 0.035f, 0.045f, 1f));
            material.SetColor("_GridColor", new Color(0.04f, 0.82f, 1f, 1f));
            material.SetColor("_EdgeColor", new Color(1f, 0.22f, 0.16f, 1f));
            material.SetFloat("_GridSpacing", 0.46f);
            material.SetFloat("_LineThickness", 0.026f);
            material.SetFloat("_GridIntensity", 2.15f);
            material.SetFloat("_EdgeThickness", 0.01f);
            material.SetFloat("_EdgeIntensity", 3.4f);
            material.SetFloat("_RimIntensity", 0.62f);
            material.SetFloat("_RimPower", 3.2f);
            material.SetFloat("_Pulse", 0.35f);
            material.SetFloat("_PulseStrength", 1.2f);
            material.SetFloat("_ScrollSpeedX", 0.035f);
            material.SetFloat("_ScrollSpeedY", 0.09f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material LoadOrCreateActionMaterial()
        {
            const string path = "Assets/Materials/Synthwave/M_SynthwaveBlock_ActionMagentaCyan.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("RhythmParkour/Synthwave Grid Block");
            if (shader == null)
                return material ?? LoadMaterial("Assets/Materials/Synthwave/M_SynthwaveBlock_Magenta.mat") ?? CreateMaterial("Demo_Action_Cue", new Color(1f, 0.22f, 0.88f, 0.82f), true);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(1f, 0.055f, 0.04f, 1f));
            material.SetColor("_GridColor", new Color(0.02f, 0.78f, 1f, 1f));
            material.SetColor("_EdgeColor", new Color(1f, 0.08f, 0.48f, 1f));
            material.SetFloat("_GridSpacing", 0.42f);
            material.SetFloat("_LineThickness", 0.024f);
            material.SetFloat("_GridIntensity", 2.1f);
            material.SetFloat("_EdgeThickness", 0.01f);
            material.SetFloat("_EdgeIntensity", 3.1f);
            material.SetFloat("_RimIntensity", 0.58f);
            material.SetFloat("_RimPower", 3.4f);
            material.SetFloat("_Pulse", 0.25f);
            material.SetFloat("_PulseStrength", 1.15f);
            material.SetFloat("_ScrollSpeedX", 0.045f);
            material.SetFloat("_ScrollSpeedY", 0.095f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material LoadOrCreateSideWallGlitchMaterial()
        {
            const string path = "Assets/Materials/Synthwave/M_SideGrabWall_BlueGlitch.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("RhythmParkour/URP Glitch Silhouette");
            if (shader == null)
                return material ?? LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_GlitchColossal.mat") ?? LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_WindowCyan.mat") ?? CreateMaterial("Demo_Side_Wall", new Color(0.04f, 0.9f, 1f, 0.45f), true);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.04f, 0.22f, 0.78f, 1f));
            material.SetColor("_GlitchColor", new Color(0.02f, 0.94f, 1f, 1f));
            material.SetColor("_AccentColor", new Color(1f, 0.08f, 0.72f, 1f));
            material.SetFloat("_BandDensity", 42f);
            material.SetFloat("_GlitchStrength", 0.5f);
            material.SetFloat("_PulseSpeed", 0.72f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material LoadOrCreateGrappleAnchorGlitchMaterial()
        {
            const string path = "Assets/Materials/Synthwave/M_GrappleAnchor_PurpleGlitch.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("RhythmParkour/URP Glitch Silhouette");
            if (shader == null)
                return material ?? LoadMaterial("Assets/Materials/VisualDemos/M_VisualDemo_GlitchColossal.mat") ?? LoadMaterial("Assets/Materials/Synthwave/M_SynthwaveBlock_Magenta.mat") ?? CreateMaterial("Demo_Grapple_Anchor", new Color(0.62f, 0.12f, 1f, 0.9f), true);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.78f, 0.08f, 1f, 1f));
            material.SetColor("_GlitchColor", new Color(1f, 0.36f, 1f, 1f));
            material.SetColor("_AccentColor", new Color(0.12f, 1f, 1f, 1f));
            material.SetFloat("_BandDensity", 54f);
            material.SetFloat("_GlitchStrength", 0.78f);
            material.SetFloat("_PulseSpeed", 1.05f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }

    public static class RhythmParkourLevelBuilder
    {
        public static int BuildStraightRunway(
            IReadOnlyList<RhythmActionEvent> events,
            RhythmParkourLevelTheme theme,
            RhythmParkourLevelBuildOptions options)
        {
            if (options == null)
                options = new RhythmParkourLevelBuildOptions();
            var endBeat = GetEndBeat(events, options.MinimumEndBeat);
            var trackEndZ = GetTrackZ(endBeat, events, options);
            var endZ = trackEndZ + 10f;
            var runwayStartZ = -2f;
            var runwayEndZ = endZ + 3f;
            var gaps = BuildRunwayGaps(events, options, runwayStartZ, runwayEndZ);

            CreateSegmentedRunway(runwayStartZ, runwayEndZ, gaps, theme);
            CreateRunwayGapMarkers(gaps, theme);

            var firstStripeBeat = GetFirstBeatStripe(events, options.AlignBeatStripesToEvents);
            var lastStripeBeat = GetLastBeatStripe(events, options.AlignBeatStripesToEvents, endBeat);
            for (var beat = firstStripeBeat; beat <= lastStripeBeat; beat++)
            {
                var z = GetTrackZ(beat, events, options);
                if (IsInsideRunwayGap(z, gaps))
                    continue;

                var material = beat % 4 == 0 ? theme.Downbeat : theme.Beat;
                CreateCube($"Beat Stripe {beat}", new Vector3(0f, 0.08f, z), new Vector3(2.7f, 0.05f, 0.08f), material);
            }

            CreateCube("Start Pad", new Vector3(0f, 0.04f, 0f), new Vector3(3.2f, 0.08f, 1.2f), theme.Start);
            return endBeat;
        }

        public static void CreateActionCues(
            IReadOnlyList<RhythmActionEvent> events,
            RhythmParkourLevelTheme theme,
            float unitsPerBeat,
            RhythmParkourLevelBuildOptions options = null)
        {
            if (events == null)
                return;

            if (options == null)
            {
                options = new RhythmParkourLevelBuildOptions
                {
                    UnitsPerBeat = unitsPerBeat
                };
            }

            foreach (var evt in events)
                CreateActionCue(evt, events, theme, options);
        }

        public static RhythmPortalSuccessTrigger CreateGoalPortal(RhythmParkourLevelTheme theme, float z)
        {
            CreateCube("Goal Portal Left Frame", new Vector3(-1.5f, 1.3f, z), new Vector3(0.18f, 2.6f, 0.18f), theme.Goal);
            CreateCube("Goal Portal Right Frame", new Vector3(1.5f, 1.3f, z), new Vector3(0.18f, 2.6f, 0.18f), theme.Goal);
            CreateCube("Goal Portal Top Frame", new Vector3(0f, 2.6f, z), new Vector3(3.15f, 0.18f, 0.18f), theme.Goal);
            var energy = CreateCube("Goal Portal Energy", new Vector3(0f, 1.3f, z + 0.03f), new Vector3(2.6f, 2.25f, 0.06f), theme.GoalEnergy);
            var collider = energy.GetComponent<BoxCollider>();
            if (collider != null)
                collider.isTrigger = true;

            var trigger = energy.AddComponent<RhythmPortalSuccessTrigger>();
            trigger.SetTriggerRadius(2.2f);
            return trigger;
        }

        public static IReadOnlyList<string> ValidateChart(IReadOnlyList<RhythmActionEvent> events)
        {
            var issues = new List<string>();
            if (events == null)
            {
                issues.Add("Chart has no event list.");
                return issues;
            }

            var occupiedBeats = new HashSet<float>();
            for (var i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                if (evt == null)
                {
                    issues.Add($"Event {i} is null.");
                    continue;
                }

                if (evt.Beat < 0f)
                    issues.Add($"Event {i} has negative beat {evt.Beat:0.###}.");

                if (evt.DurationBeats <= 0f)
                    issues.Add($"Event {i} at beat {evt.Beat:0.###} has non-positive duration.");

                if (!occupiedBeats.Add(evt.Beat))
                    issues.Add($"Multiple events are authored at beat {evt.Beat:0.###}.");
            }

            return issues;
        }

        public static int GetEndBeat(IReadOnlyList<RhythmActionEvent> events, float minimumEndBeat = 16f)
        {
            var endBeat = minimumEndBeat;
            if (events != null)
            {
                foreach (var evt in events)
                {
                    if (evt != null)
                        endBeat = Mathf.Max(endBeat, evt.Beat + evt.DurationBeats + 2f);
                }
            }

            return Mathf.CeilToInt(endBeat);
        }

        static void CreateActionCue(
            RhythmActionEvent evt,
            IReadOnlyList<RhythmActionEvent> events,
            RhythmParkourLevelTheme theme,
            RhythmParkourLevelBuildOptions options)
        {
            if (evt == null)
                return;

            var z = GetTrackZ(evt.Beat, events, options);
            switch (evt.ActionType)
            {
                case RhythmActionType.Step:
                {
                    var x = evt.Hand == RhythmHand.Left ? -0.6f : 0.6f;
                    CreateCube($"Step Cue {evt.Hand} Beat {evt.Beat:0.##}", new Vector3(x, 0.16f, z), new Vector3(0.78f, 0.18f, 0.85f), theme.Action);
                    break;
                }
                case RhythmActionType.SideGrab:
                {
                    var x = evt.Direction == RhythmDirection.Left ? -2.05f : 2.05f;
                    CreateCube($"Side Grab Wall {evt.Direction} Beat {evt.Beat:0.##}", new Vector3(x, 1.18f, z), new Vector3(0.18f, 1.8f, 1.15f), theme.SideWall);
                    CreateCube($"Side Grab Target {evt.Direction} Beat {evt.Beat:0.##}", new Vector3(x * 0.92f, 1.35f, z), new Vector3(0.32f, 0.45f, 0.45f), theme.Action);
                    break;
                }
                case RhythmActionType.Slide:
                {
                    var obstacleZ = z + options.UnitsPerBeat * 0.62f;
                    CreateCube($"Slide Cue Beat {evt.Beat:0.##}", new Vector3(0f, 0.13f, z), new Vector3(1.55f, 0.1f, 0.42f), theme.Action);
                    CreateCube($"Slide Low Barrier Beat {evt.Beat:0.##}", new Vector3(0f, 1.18f, obstacleZ), new Vector3(2.9f, 0.34f, 0.34f), theme.Danger);
                    break;
                }
                case RhythmActionType.LongJump:
                {
                    GetRunwayGapBeatRange(evt, options, out var gapStartBeat, out var gapEndBeat);
                    var gapStartZ = GetTrackZ(gapStartBeat, events, options);
                    var gapEndZ = GetTrackZ(gapEndBeat, events, options);
                    var gapLength = Mathf.Max(0.8f, gapEndZ - gapStartZ);
                    var takeoffZ = Mathf.Lerp(z, gapStartZ, 0.45f);
                    var landingZ = gapEndZ + 0.42f;
                    CreateCube($"Long Jump Takeoff Beat {evt.Beat:0.##}", new Vector3(0f, 0.14f, takeoffZ), new Vector3(2.2f, 0.18f, Mathf.Max(0.5f, gapStartZ - takeoffZ + 0.2f)), theme.Action);
                    CreateCube($"Long Jump Gap Beat {evt.Beat:0.##}", new Vector3(0f, -0.04f, (gapStartZ + gapEndZ) * 0.5f), new Vector3(2.4f, 0.08f, gapLength), theme.Danger);
                    CreateCube($"Long Jump Landing Beat {evt.Beat:0.##}", new Vector3(0f, 0.14f, landingZ), new Vector3(2.2f, 0.18f, 0.72f), theme.Action);
                    break;
                }
                case RhythmActionType.Grapple:
                {
                    var endZ = GetTrackZ(evt.Beat + evt.DurationBeats, events, options);
                    var length = Mathf.Max(options.UnitsPerBeat, endZ - z);
                    var handSide = evt.Hand == RhythmHand.Left ? -1f : 1f;
                    CreateCube($"Grapple Start Target Beat {evt.Beat:0.##}", new Vector3(0f, 3.35f, z), new Vector3(0.75f, 0.28f, 0.75f), theme.Action);
                    CreateCube($"Grapple End Target Beat {evt.Beat + evt.DurationBeats:0.##}", new Vector3(0f, 3.35f, z + length), new Vector3(0.9f, 0.22f, 0.9f), theme.Action);
                    CreateCube($"Grapple Hook Anchor Beat {evt.Beat:0.##}", new Vector3(handSide * 0.9f, 5.2f, z + length * 0.46f), new Vector3(0.54f, 0.26f, 0.54f), theme.GrappleAnchor);
                    CreateCube($"Grapple Landing Bridge Beat {evt.Beat + evt.DurationBeats:0.##}", new Vector3(0f, 0.04f, z + length + 0.9f), new Vector3(2.3f, 0.08f, 1.35f), theme.Runway);
                    break;
                }
            }
        }

        static float GetTrackZ(float beat, IReadOnlyList<RhythmActionEvent> events, RhythmParkourLevelBuildOptions options)
        {
            if (options == null || !options.UseTrackPositionMap)
                return beat * (options != null ? options.UnitsPerBeat : 2.5f);

            return RhythmTrackPositionMapper.GetZ(
                beat,
                events,
                options.UnitsPerBeat,
                options.GrappleExtraUnitsPerBeat);
        }

        static List<RunwayGap> BuildRunwayGaps(
            IReadOnlyList<RhythmActionEvent> events,
            RhythmParkourLevelBuildOptions options,
            float runwayStartZ,
            float runwayEndZ)
        {
            var candidates = new List<RunwayGap>();
            if (events == null || options == null || !options.CarveActionRunwayGaps)
                return candidates;

            foreach (var evt in events)
            {
                if (evt == null || !RequiresRunwayGap(evt.ActionType))
                    continue;

                GetRunwayGapBeatRange(evt, options, out var startBeat, out var endBeat);
                var startZ = Mathf.Clamp(GetTrackZ(startBeat, events, options), runwayStartZ, runwayEndZ);
                var endZ = Mathf.Clamp(GetTrackZ(endBeat, events, options), runwayStartZ, runwayEndZ);
                if (endZ - startZ < 0.6f)
                    continue;

                candidates.Add(new RunwayGap(startBeat, endBeat, startZ, endZ, evt, 1));
            }

            candidates.Sort((left, right) => left.StartBeat.CompareTo(right.StartBeat));
            return MergeRunwayGaps(candidates, events, options, runwayStartZ, runwayEndZ);
        }

        static bool RequiresRunwayGap(RhythmActionType actionType)
        {
            return actionType != RhythmActionType.Slide;
        }

        static void GetRunwayGapBeatRange(
            RhythmActionEvent evt,
            RhythmParkourLevelBuildOptions options,
            out float startBeat,
            out float endBeat)
        {
            if (evt.ActionType == RhythmActionType.Grapple)
            {
                startBeat = evt.Beat + options.RunwayGapStartAfterBeatOffset;
                endBeat = evt.Beat + Mathf.Max(evt.DurationBeats, 0.25f);
                return;
            }

            startBeat = Mathf.Max(0f, evt.Beat + options.RunwayGapStartAfterBeatOffset - options.RunwayGapPaddingBeforeBeats);
            endBeat = evt.Beat + Mathf.Max(evt.DurationBeats, 0.25f) + options.RunwayGapPaddingAfterBeats;
        }

        static List<RunwayGap> MergeRunwayGaps(
            IReadOnlyList<RunwayGap> candidates,
            IReadOnlyList<RhythmActionEvent> events,
            RhythmParkourLevelBuildOptions options,
            float runwayStartZ,
            float runwayEndZ)
        {
            var merged = new List<RunwayGap>();
            if (candidates == null || candidates.Count == 0)
                return merged;

            var current = candidates[0];
            var mergeThreshold = Mathf.Max(0f, options.RunwayGapMergeThresholdBeats);
            for (var i = 1; i < candidates.Count; i++)
            {
                var next = candidates[i];
                if (next.StartBeat <= current.EndBeat + mergeThreshold)
                {
                    current = current.Merge(next, events, options, runwayStartZ, runwayEndZ);
                    continue;
                }

                merged.Add(current);
                current = next;
            }

            merged.Add(current);
            return merged;
        }

        static void CreateSegmentedRunway(float startZ, float endZ, IReadOnlyList<RunwayGap> gaps, RhythmParkourLevelTheme theme)
        {
            var cursor = startZ;
            var segmentIndex = 0;

            foreach (var gap in gaps)
            {
                if (gap.StartZ > cursor + 0.24f)
                {
                    CreateRunwaySegment(segmentIndex++, cursor, gap.StartZ, theme);
                }

                cursor = Mathf.Max(cursor, gap.EndZ);
            }

            if (endZ > cursor + 0.24f)
                CreateRunwaySegment(segmentIndex, cursor, endZ, theme);
        }

        static void CreateRunwaySegment(int index, float startZ, float endZ, RhythmParkourLevelTheme theme)
        {
            var length = Mathf.Max(0.05f, endZ - startZ);
            var centerZ = (startZ + endZ) * 0.5f;
            var name = $"Transparent Rhythm Runway Segment {index:00}";
            CreateCube(name, new Vector3(0f, 0f, centerZ), new Vector3(2.4f, 0.06f, length), theme.Runway);
            CreateCube($"Left Edge Glow Segment {index:00}", new Vector3(-1.35f, 0.05f, centerZ), new Vector3(0.06f, 0.09f, length), theme.Edge);
            CreateCube($"Right Edge Glow Segment {index:00}", new Vector3(1.35f, 0.05f, centerZ), new Vector3(0.06f, 0.09f, length), theme.Edge);
        }

        static void CreateRunwayGapMarkers(IReadOnlyList<RunwayGap> gaps, RhythmParkourLevelTheme theme)
        {
            if (gaps == null || gaps.Count == 0)
                return;

            var material = theme.GapMarker != null ? theme.GapMarker : theme.Action;
            foreach (var gap in gaps)
            {
                var label = $"{gap.Event.ActionType} Beat {gap.StartBeat:0.##}-{gap.EndBeat:0.##}";
                var width = gap.EventCount > 1 ? 3.05f : 2.85f;
                CreateCube($"Runway Gap Entry Marker {label}", new Vector3(0f, 0.08f, gap.StartZ), new Vector3(width, 0.1f, 0.16f), material);
                CreateCube($"Runway Gap Exit Marker {label}", new Vector3(0f, 0.08f, gap.EndZ), new Vector3(width, 0.1f, 0.16f), material);
            }
        }

        static bool IsInsideRunwayGap(float z, IReadOnlyList<RunwayGap> gaps)
        {
            if (gaps == null || gaps.Count == 0)
                return false;

            foreach (var gap in gaps)
            {
                if (z >= gap.StartZ && z <= gap.EndZ)
                    return true;
            }

            return false;
        }

        static int GetFirstBeatStripe(IReadOnlyList<RhythmActionEvent> events, bool alignToEvents)
        {
            if (!alignToEvents || events == null || events.Count == 0)
                return 1;

            var firstBeat = float.MaxValue;
            foreach (var evt in events)
            {
                if (evt != null)
                    firstBeat = Mathf.Min(firstBeat, evt.Beat);
            }

            return firstBeat == float.MaxValue ? 1 : Mathf.Max(1, Mathf.FloorToInt(firstBeat));
        }

        static int GetLastBeatStripe(IReadOnlyList<RhythmActionEvent> events, bool alignToEvents, int fallbackEndBeat)
        {
            if (!alignToEvents || events == null || events.Count == 0)
                return fallbackEndBeat;

            var lastBeat = 0f;
            foreach (var evt in events)
            {
                if (evt != null)
                    lastBeat = Mathf.Max(lastBeat, evt.Beat);
            }

            return Mathf.Max(GetFirstBeatStripe(events, true), Mathf.CeilToInt(lastBeat));
        }

        static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        readonly struct RunwayGap
        {
            public RunwayGap(float startBeat, float endBeat, float startZ, float endZ, RhythmActionEvent evt, int eventCount)
            {
                StartBeat = startBeat;
                EndBeat = endBeat;
                StartZ = startZ;
                EndZ = endZ;
                Event = evt;
                EventCount = eventCount;
            }

            public float StartBeat { get; }
            public float EndBeat { get; }
            public float StartZ { get; }
            public float EndZ { get; }
            public RhythmActionEvent Event { get; }
            public int EventCount { get; }

            public RunwayGap Merge(
                RunwayGap next,
                IReadOnlyList<RhythmActionEvent> events,
                RhythmParkourLevelBuildOptions options,
                float runwayStartZ,
                float runwayEndZ)
            {
                var startBeat = Mathf.Min(StartBeat, next.StartBeat);
                var endBeat = Mathf.Max(EndBeat, next.EndBeat);
                var startZ = Mathf.Clamp(GetTrackZ(startBeat, events, options), runwayStartZ, runwayEndZ);
                var endZ = Mathf.Clamp(GetTrackZ(endBeat, events, options), runwayStartZ, runwayEndZ);
                return new RunwayGap(startBeat, endBeat, startZ, endZ, Event, EventCount + next.EventCount);
            }
        }
    }
}
