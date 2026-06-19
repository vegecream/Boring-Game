using RhythmParkour;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BoringRun.VRInput
{
    public sealed class VRSpeedFeelDriver : MonoBehaviour
    {
        [SerializeField] VRRhythmActionPrototype actionSource;
        [SerializeField] Volume speedVolume;
        [SerializeField] Camera targetCamera;

        [Header("Post Processing")]
        [SerializeField, Range(-0.25f, 0f)] float baseLensDistortion = -0.025f;
        [SerializeField, Range(-0.35f, 0f)] float actionLensDistortion = -0.095f;
        [SerializeField, Range(0f, 0.4f)] float baseChromaticAberration = 0.035f;
        [SerializeField, Range(0f, 0.6f)] float actionChromaticAberration = 0.18f;
        [SerializeField, Range(0f, 0.6f)] float baseVignette = 0.18f;
        [SerializeField, Range(0f, 0.75f)] float actionVignette = 0.32f;
        [SerializeField, Range(0f, 18f)] float desktopFovBoost = 7f;

        [Header("Response")]
        [SerializeField] float pulseDecaySpeed = 3.4f;

        [Header("Failure Edge")]
        [SerializeField] Color baseVignetteColor = new Color(0.02f, 0.01f, 0.05f, 1f);
        [SerializeField] Color failureVignetteColor = new Color(1f, 0.025f, 0.015f, 1f);
        [SerializeField, Range(0f, 1f)] float failureVignette = 0.68f;
        [SerializeField] float failurePulseDecaySpeed = 3.8f;

        LensDistortion lensDistortion;
        ChromaticAberration chromaticAberration;
        Vignette vignette;
        float pulse;
        float failurePulse;
        float baseFov;

        void Awake()
        {
            ResolveReferences();
            ConfigureVolume();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void Update()
        {
            ResolveReferences();

            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * pulseDecaySpeed);
            failurePulse = Mathf.MoveTowards(failurePulse, 0f, Time.deltaTime * failurePulseDecaySpeed);
            var stress = actionSource != null ? actionSource.SessionDistortion01() : 0f;
            var amount = Mathf.Clamp01(0.35f + pulse * 0.65f + stress * 0.25f);

            ApplyPostProcessing(amount, failurePulse);
        }

        void ResolveReferences()
        {
            if (actionSource == null)
                actionSource = FindObjectOfType<VRRhythmActionPrototype>();

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (speedVolume == null)
                speedVolume = GetComponentInChildren<Volume>();

            if (targetCamera != null && baseFov <= 0f)
                baseFov = targetCamera.fieldOfView;
        }

        void ConfigureVolume()
        {
            if (speedVolume == null)
                return;

            var profile = speedVolume.profile;
            if (profile == null)
                return;

            profile.TryGet(out lensDistortion);
            profile.TryGet(out chromaticAberration);
            profile.TryGet(out vignette);

            if (lensDistortion != null)
            {
                lensDistortion.active = true;
                lensDistortion.intensity.overrideState = true;
                lensDistortion.scale.overrideState = true;
                lensDistortion.xMultiplier.overrideState = true;
                lensDistortion.yMultiplier.overrideState = true;
                lensDistortion.scale.value = 1.02f;
                lensDistortion.xMultiplier.value = 1f;
                lensDistortion.yMultiplier.value = 1f;
            }

            if (chromaticAberration != null)
            {
                chromaticAberration.active = true;
                chromaticAberration.intensity.overrideState = true;
            }

            if (vignette != null)
            {
                vignette.active = true;
                vignette.intensity.overrideState = true;
                vignette.smoothness.overrideState = true;
                vignette.color.overrideState = true;
                vignette.color.value = baseVignetteColor;
                vignette.smoothness.value = 0.58f;
            }
        }

        void Subscribe()
        {
            if (actionSource == null)
                actionSource = FindObjectOfType<VRRhythmActionPrototype>();

            if (actionSource == null)
                return;

            actionSource.JudgmentResolved += HandleJudgmentResolved;
            actionSource.ActionMissed += HandleActionMissed;
        }

        void Unsubscribe()
        {
            if (actionSource == null)
                return;

            actionSource.JudgmentResolved -= HandleJudgmentResolved;
            actionSource.ActionMissed -= HandleActionMissed;
        }

        void HandleJudgmentResolved(VRRhythmJudgmentResult result)
        {
            if (result.Event == null || result.Kind == VRRhythmJudgmentKind.BadTiming)
                return;

            pulse = Mathf.Max(pulse, result.Kind == VRRhythmJudgmentKind.Hit ? 1f : 0.8f);
            if (result.Kind != VRRhythmJudgmentKind.Hit)
                failurePulse = 1f;
        }

        void HandleActionMissed(RhythmActionEvent evt)
        {
            if (evt != null)
            {
                pulse = Mathf.Max(pulse, 0.65f);
                failurePulse = Mathf.Max(failurePulse, 0.9f);
            }
        }

        void ApplyPostProcessing(float amount, float failureAmount)
        {
            if (lensDistortion != null)
                lensDistortion.intensity.value = Mathf.Lerp(baseLensDistortion, actionLensDistortion, amount);

            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(baseChromaticAberration, actionChromaticAberration, amount);

            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Max(
                    Mathf.Lerp(baseVignette, actionVignette, amount),
                    Mathf.Lerp(baseVignette, failureVignette, failureAmount));
                vignette.color.value = Color.Lerp(baseVignetteColor, failureVignetteColor, failureAmount);
            }

            if (targetCamera != null && baseFov > 0f)
                targetCamera.fieldOfView = baseFov + desktopFovBoost * amount;
        }
    }

    static class VRRhythmActionPrototypeSpeedExtensions
    {
        public static float SessionDistortion01(this VRRhythmActionPrototype prototype)
        {
            return prototype != null && prototype.Session != null
                ? prototype.Session.DistortionAmount
                : 0f;
        }
    }
}
