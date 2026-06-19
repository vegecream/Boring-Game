using UnityEngine;

namespace BoringRun.VRInput
{
    public sealed class ForwardCityRushDemoDriver : MonoBehaviour
    {
        [SerializeField] Transform rigRoot;
        [SerializeField] Transform rushRoot;
        [SerializeField] VRRhythmActionPrototype actionSource;
        [SerializeField] float durationSeconds = 90f;
        [SerializeField] bool driveRigRoot = true;
        [SerializeField] float forwardSpeed = 7.5f;
        [SerializeField] float objectRushSpeed = 22f;
        [SerializeField] float recycleLength = 210f;
        [SerializeField] float frontDistance = 180f;
        [SerializeField] float backDistance = -28f;
        [SerializeField] float lateralBreath = 0.18f;
        [SerializeField] float yawWobbleDegrees = 1.6f;
        [SerializeField] float rollWobbleDegrees = 5.5f;
        [SerializeField] float pitchWobbleDegrees = 2.6f;
        [SerializeField] float scalePulse = 0.035f;
        [SerializeField] bool glitchRushOverTime;
        [SerializeField] Material glitchMaterial;
        [SerializeField] float glitchStartSeconds = 35f;
        [SerializeField] float glitchFullSeconds = 95f;
        [SerializeField] bool glitchByBeat;
        [SerializeField] float glitchStartBeat = 80f;
        [SerializeField] float glitchFullBeat = 170f;
        [SerializeField] bool waitForPositiveBeat = true;
        [SerializeField] bool loop = true;

        Transform[] rushObjects;
        Renderer[][] rushRenderers;
        Material[][][] originalMaterials;
        bool[] glitchApplied;
        Vector3[] offsets;
        Quaternion[] rotations;
        Vector3[] scales;
        float elapsed;
        Vector3 startPosition;

        void Awake()
        {
            if (rigRoot == null)
                rigRoot = transform;

            if (rushRoot == null)
                rushRoot = transform;

            startPosition = rigRoot.position;
            CacheRushObjects();
        }

        void Update()
        {
            if (ShouldWaitForBeatStart())
                return;

            elapsed += Time.deltaTime;
            if (loop && durationSeconds > 0.01f)
                elapsed %= durationSeconds;
            else if (durationSeconds > 0.01f)
                elapsed = Mathf.Min(elapsed, durationSeconds);

            if (rigRoot != null && driveRigRoot)
            {
                var run = startPosition;
                run.z += elapsed * forwardSpeed;
                run.x += Mathf.Sin(elapsed * 0.42f) * lateralBreath;
                rigRoot.position = run;
            }

            UpdateRushObjects();
        }

        bool ShouldWaitForBeatStart()
        {
            if (!waitForPositiveBeat || actionSource == null)
                return false;

            return !actionSource.IsGameStarted || actionSource.CurrentBeat <= 0f;
        }

        void CacheRushObjects()
        {
            var root = rushRoot != null ? rushRoot : transform;
            var children = new System.Collections.Generic.List<Transform>();
            for (var i = 0; i < root.childCount; i++)
                children.Add(root.GetChild(i));

            rushObjects = children.ToArray();
            offsets = new Vector3[rushObjects.Length];
            rotations = new Quaternion[rushObjects.Length];
            scales = new Vector3[rushObjects.Length];
            rushRenderers = new Renderer[rushObjects.Length][];
            originalMaterials = new Material[rushObjects.Length][][];
            glitchApplied = new bool[rushObjects.Length];
            for (var i = 0; i < rushObjects.Length; i++)
            {
                offsets[i] = rushObjects[i].position - startPosition;
                rotations[i] = rushObjects[i].rotation;
                scales[i] = rushObjects[i].localScale;
                rushRenderers[i] = rushObjects[i].GetComponentsInChildren<Renderer>(true);
                originalMaterials[i] = new Material[rushRenderers[i].Length][];
                for (var rendererIndex = 0; rendererIndex < rushRenderers[i].Length; rendererIndex++)
                    originalMaterials[i][rendererIndex] = rushRenderers[i][rendererIndex].sharedMaterials;
            }
        }

        void UpdateRushObjects()
        {
            if (rushObjects == null || offsets == null || rigRoot == null)
                return;

            var travel = elapsed * objectRushSpeed;
            var span = Mathf.Max(1f, recycleLength);
            for (var i = 0; i < rushObjects.Length; i++)
            {
                if (rushObjects[i] == null)
                    continue;

                var offset = offsets[i];
                var z = frontDistance - Mathf.Repeat(travel + (frontDistance - offset.z), span);
                if (z < backDistance)
                    z += span;

                var sidePulse = Mathf.Sin(elapsed * 1.7f + i * 0.63f) * 0.14f;
                rushObjects[i].position = rigRoot.position + new Vector3(offset.x + sidePulse, offset.y, z);
                var phase = elapsed + i * 0.77f;
                var yaw = Mathf.Sin(phase * 0.9f) * yawWobbleDegrees;
                var pitch = Mathf.Sin(phase * 0.63f) * pitchWobbleDegrees;
                var roll = Mathf.Sin(phase * 1.17f) * rollWobbleDegrees;
                rushObjects[i].rotation = rotations[i] * Quaternion.Euler(pitch, yaw, roll);
                var pulse = 1f + Mathf.Sin(phase * 1.35f) * scalePulse;
                rushObjects[i].localScale = scales[i] * pulse;
                UpdateGlitchState(i);
            }
        }

        void UpdateGlitchState(int index)
        {
            if (!glitchRushOverTime || glitchMaterial == null || rushRenderers == null || rushRenderers.Length == 0)
                return;

            var progress = GetGlitchProgress();
            var threshold = rushObjects.Length <= 1 ? 0f : index / (float)(rushObjects.Length - 1);
            SetGlitch(index, progress >= threshold);
        }

        float GetGlitchProgress()
        {
            if (glitchByBeat && actionSource != null)
            {
                var spanBeats = Mathf.Max(0.01f, glitchFullBeat - glitchStartBeat);
                return Mathf.Clamp01((actionSource.CurrentBeat - glitchStartBeat) / spanBeats);
            }

            var span = Mathf.Max(0.01f, glitchFullSeconds - glitchStartSeconds);
            return Mathf.Clamp01((elapsed - glitchStartSeconds) / span);
        }

        void SetGlitch(int index, bool useGlitch)
        {
            if (glitchApplied == null || index < 0 || index >= glitchApplied.Length || glitchApplied[index] == useGlitch)
                return;

            glitchApplied[index] = useGlitch;
            var renderers = rushRenderers[index];
            if (renderers == null)
                return;

            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (renderer == null)
                    continue;

                if (!useGlitch)
                {
                    renderer.sharedMaterials = originalMaterials[index][rendererIndex];
                    continue;
                }

                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = glitchMaterial;
                    continue;
                }

                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    materials[materialIndex] = glitchMaterial;
                renderer.sharedMaterials = materials;
            }
        }
    }
}
