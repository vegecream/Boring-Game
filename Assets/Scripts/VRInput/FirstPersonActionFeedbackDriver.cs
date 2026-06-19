using RhythmParkour;
using UnityEngine;

namespace BoringRun.VRInput
{
    public sealed class FirstPersonActionFeedbackDriver : MonoBehaviour
    {
        [SerializeField] VRRhythmActionPrototype actionSource;
        [SerializeField] VRParkourInputEvents debugInputSource;
        [SerializeField] Transform motionRoot;
        [SerializeField] Transform locomotionRoot;

        [Header("Source")]
        [SerializeField] bool playRecognizedInputEvents = true;

        [Header("Timing")]
        [SerializeField] float baseActionDurationSeconds = 0.42f;
        [SerializeField] float grappleSecondsPerBeat = 0.25f;

        [Header("Camera Motion")]
        [SerializeField] float stepBob = 0.22f;
        [SerializeField] float stepForward = 0.34f;
        [SerializeField] float stepSideSway = 0.045f;
        [SerializeField] float stepLandingDip = 0.08f;
        [SerializeField] float sideGrabLift = 0.34f;
        [SerializeField] float sideGrabSway = 0.22f;
        [SerializeField] float sideGrabForward = 0.34f;
        [SerializeField] float sideGrabLandingDip = 0.08f;
        [SerializeField] float sideGrabYawDegrees = 5f;
        [SerializeField] float slideLowering = 0.42f;
        [SerializeField] float slideForward = 0.5f;
        [SerializeField] float longJumpLift = 0.9f;
        [SerializeField] float longJumpForward = 1.35f;
        [SerializeField] float longJumpLandingDip = 0.16f;
        [SerializeField] float gravityFallPower = 1.55f;
        [SerializeField] float grappleForward = 0.26f;
        [SerializeField] float grappleLift = 0.04f;
        [SerializeField] bool usePendulumGrappleMotion = true;
        [SerializeField] float grapplePendulumBaseRadius = 4.2f;
        [SerializeField] float grapplePendulumRadiusPerBeat = 0.38f;
        [SerializeField] float grapplePendulumAngleDegrees = 56f;
        [SerializeField] float grapplePendulumRollDegrees = 10f;

        [Header("Camera Orientation")]
        [SerializeField] float stepRollDegrees = 3.8f;
        [SerializeField] float stepYawDegrees = 1.6f;
        [SerializeField] float slideLookUpDegrees = 8f;
        [SerializeField] float slideRollDegrees = 1.2f;
        [SerializeField] float longJumpTakeoffLookUpDegrees = 10f;
        [SerializeField] float longJumpAirLookDownDegrees = 6.5f;
        [SerializeField] float longJumpLandingRollDegrees = 3.2f;
        [SerializeField] float grappleLookUpDegrees = 6f;

        [Header("Root Locomotion")]
        [SerializeField] bool moveRootOnSuccessfulActions = true;
        [SerializeField] bool moveRootOnMisses;
        [SerializeField] float stepTravelDistance = 0.85f;
        [SerializeField] float stepAccelerationExponent = 1.75f;
        [SerializeField] float slideTravelDistance = 1.25f;
        [SerializeField, Range(0.05f, 0.45f)] float slideDropStage = 0.2f;
        [SerializeField, Range(0.1f, 0.9f)] float slideHoldStage = 0.74f;
        [SerializeField] float longJumpTravelDistance = 1.65f;
        [SerializeField, Range(0.05f, 0.45f)] float longJumpTakeoffStage = 0.2f;
        [SerializeField, Range(0.1f, 0.85f)] float longJumpFloatStage = 0.85f;
        [SerializeField] float grappleTravelDistance = 1.9f;

        [Header("Miss Feedback")]
        [SerializeField] float missShakeAmount = 0.025f;
        [SerializeField] float missShakeFrequency = 42f;

        [Header("Debug Keys")]
        [SerializeField] bool enableKeyboardDebug = true;
        [SerializeField] KeyCode debugStepKey = KeyCode.Alpha1;
        [SerializeField] KeyCode debugSideGrabKey = KeyCode.Alpha2;
        [SerializeField] KeyCode debugSlideKey = KeyCode.Alpha3;
        [SerializeField] KeyCode debugLongJumpKey = KeyCode.Alpha4;
        [SerializeField] KeyCode debugGrappleKey = KeyCode.Alpha5;
        [SerializeField] KeyCode debugMissModifierKey = KeyCode.LeftShift;

        [Header("Grapple Visual")]
        [SerializeField] Transform leftRopeEndPoint;
        [SerializeField] Transform rightRopeEndPoint;
        [SerializeField] Material grappleRopeMaterial;
        [SerializeField] Material grappleClawMaterial;
        [SerializeField] float grappleHookForward = 6.6f;
        [SerializeField] float grappleHookHeight = 6.2f;
        [SerializeField] float grappleHookSideOffset = 0.72f;
        [SerializeField] float grappleHookAnchorVerticalOffset = 1.25f;
        [SerializeField] float grappleBodyVerticalOffset = 1.05f;
        [SerializeField] float grappleRopeRadius = 0.075f;
        [SerializeField] Vector3 grappleClawScale = new Vector3(0.64f, 0.32f, 0.64f);

        VRRhythmActionPrototype subscribedActionSource;
        VRParkourInputEvents subscribedInputSource;
        RhythmActionEvent activeEvent;
        Vector3 restLocalPosition;
        Quaternion restLocalRotation;
        Vector3 activeRootStartPosition;
        Vector3 activeRootDirection;
        float activeRootDistance;
        bool activeMovesRoot;
        bool activeIsMiss;
        bool debugAlternateSide;
        float activeTimer;
        float activeDuration;
        Transform runtimeGrappleRope;
        Transform runtimeGrappleClaw;
        Vector3 activeGrappleHookPosition;
        float activeGrappleRadius;
        RhythmHand activeGrappleHand;

        void Awake()
        {
            ResolveMotionRoot();
            ResolveLocomotionRoot();
            ResolveGrappleEndPoints();
            CacheRestPose();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void Start()
        {
            ResolveMotionRoot();
            ResolveLocomotionRoot();
            ResolveGrappleEndPoints();
            CacheRestPose();

            if (subscribedActionSource == null && subscribedInputSource == null)
                Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
            RestoreRestPose();
        }

        void Update()
        {
            if (enableKeyboardDebug)
                PollKeyboardDebug();

            if (motionRoot == null)
                return;

            if (activeEvent == null)
            {
                DestroyGrappleVisual();
                RestoreRestPose();
                return;
            }

            activeTimer += Time.deltaTime;
            var normalized = activeDuration <= 0f || !IsFinite(activeDuration)
                ? 1f
                : Safe01(activeTimer / activeDuration);
            ApplyRootMotion(activeEvent, normalized);
            ApplyFeedback(activeEvent, normalized, activeIsMiss);

            if (normalized >= 1f)
            {
                if (activeEvent.ActionType == RhythmActionType.Grapple)
                    DestroyGrappleVisual();
                activeEvent = null;
                RestoreRestPose();
            }
        }

        void Subscribe()
        {
            Unsubscribe();

            if (actionSource == null)
                actionSource = FindObjectOfType<VRRhythmActionPrototype>();

            if (actionSource != null)
            {
                subscribedActionSource = actionSource;
                subscribedActionSource.JudgmentResolved += HandleJudgmentResolved;
                subscribedActionSource.ActionMissed += HandleActionMissed;
                subscribedActionSource.GrappleStarted += HandleGrappleStarted;
            }

            if (!playRecognizedInputEvents)
                return;

            if (debugInputSource == null)
                debugInputSource = FindObjectOfType<VRParkourInputEvents>();

            if (debugInputSource != null)
            {
                subscribedInputSource = debugInputSource;
                subscribedInputSource.InputRecognized += HandleRecognizedInput;
            }
        }

        void Unsubscribe()
        {
            if (subscribedActionSource != null)
            {
                subscribedActionSource.JudgmentResolved -= HandleJudgmentResolved;
                subscribedActionSource.ActionMissed -= HandleActionMissed;
                subscribedActionSource.GrappleStarted -= HandleGrappleStarted;
                subscribedActionSource = null;
            }

            if (subscribedInputSource != null)
            {
                subscribedInputSource.InputRecognized -= HandleRecognizedInput;
                subscribedInputSource = null;
            }
        }

        void HandleJudgmentResolved(VRRhythmJudgmentResult result)
        {
            if (result.Event == null || result.Kind == VRRhythmJudgmentKind.BadTiming)
                return;

            if (result.Event.ActionType == RhythmActionType.Grapple)
                return;

            Play(result.Event, result.Kind != VRRhythmJudgmentKind.Hit);
        }

        void HandleActionMissed(RhythmActionEvent evt)
        {
            if (evt != null)
                Play(evt, true);
        }

        void HandleGrappleStarted(RhythmActionEvent evt)
        {
            if (evt != null)
                Play(evt, false);
        }

        void HandleRecognizedInput(VRParkourInputEvent inputEvent)
        {
            var evt = CreateDebugActionEvent(inputEvent);
            if (evt != null)
                Play(evt, false);
        }

        void PollKeyboardDebug()
        {
            var isMiss = NewInputKeyboard.IsPressed(debugMissModifierKey);

            if (NewInputKeyboard.WasPressedThisFrame(debugStepKey))
            {
                debugAlternateSide = !debugAlternateSide;
                var hand = debugAlternateSide ? RhythmHand.Left : RhythmHand.Right;
                Play(new RhythmActionEvent(0f, 1f, RhythmActionType.Step, hand, RhythmDirection.None), isMiss);
            }

            if (NewInputKeyboard.WasPressedThisFrame(debugSideGrabKey))
            {
                debugAlternateSide = !debugAlternateSide;
                var hand = debugAlternateSide ? RhythmHand.Left : RhythmHand.Right;
                var direction = debugAlternateSide ? RhythmDirection.Left : RhythmDirection.Right;
                Play(new RhythmActionEvent(0f, 1f, RhythmActionType.SideGrab, hand, direction), isMiss);
            }

            if (NewInputKeyboard.WasPressedThisFrame(debugSlideKey))
                Play(new RhythmActionEvent(0f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down), isMiss);

            if (NewInputKeyboard.WasPressedThisFrame(debugLongJumpKey))
                Play(new RhythmActionEvent(0f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up), isMiss);

            if (NewInputKeyboard.WasPressedThisFrame(debugGrappleKey))
                Play(new RhythmActionEvent(0f, 4f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up), isMiss);
        }

        void Play(RhythmActionEvent evt, bool isMiss)
        {
            activeEvent = evt;
            activeIsMiss = isMiss;
            activeTimer = 0f;
            activeDuration = Mathf.Max(0.05f, SafeFloat(GetDuration(evt), baseActionDurationSeconds));
            ConfigureRootMotion(evt, isMiss);
            ConfigureGrappleVisual(evt);
        }

        float GetDuration(RhythmActionEvent evt)
        {
            var tempoScale = SafeFloat(subscribedActionSource != null ? subscribedActionSource.CurrentTempoScale : 1f, 1f);
            var durationBeats = SafeFloat(evt.DurationBeats, evt.ActionType == RhythmActionType.Grapple ? 4f : 1f);
            var duration = Mathf.Max(0.05f, baseActionDurationSeconds);
            var durationIncludesTempoScale = false;

            if (subscribedActionSource != null)
            {
                var secondsPerBeat = SafeFloat(subscribedActionSource.CurrentSecondsPerBeat, grappleSecondsPerBeat);
                duration = Mathf.Max(duration, durationBeats * Mathf.Max(0.05f, secondsPerBeat) * GetDurationBeatMultiplier(evt));
                durationIncludesTempoScale = true;
            }
            else if (evt.ActionType == RhythmActionType.Grapple)
            {
                var secondsPerBeat = Mathf.Max(0.05f, grappleSecondsPerBeat);
                duration = Mathf.Max(duration, durationBeats * Mathf.Max(0.05f, secondsPerBeat));
            }

            if (durationIncludesTempoScale)
                return duration;

            return duration / Mathf.Max(0.25f, tempoScale);
        }

        static float GetDurationBeatMultiplier(RhythmActionEvent evt)
        {
            switch (evt.ActionType)
            {
                case RhythmActionType.LongJump:
                    return 1.35f;
                case RhythmActionType.Step:
                case RhythmActionType.SideGrab:
                    return 1.15f;
                case RhythmActionType.Slide:
                    return 1.65f;
                case RhythmActionType.Grapple:
                default:
                    return 1f;
            }
        }

        void ApplyFeedback(RhythmActionEvent evt, float normalized, bool isMiss)
        {
            normalized = Safe01(normalized);
            var pulse = Mathf.Sin(normalized * Mathf.PI);
            var smooth = Mathf.SmoothStep(0f, 1f, normalized);
            var offset = Vector3.zero;
            var lookEuler = Vector3.zero;

            switch (evt.ActionType)
            {
                case RhythmActionType.Step:
                    ApplyStep(evt, normalized, pulse, ref offset, ref lookEuler);
                    break;
                case RhythmActionType.SideGrab:
                    ApplySideGrab(evt, normalized, pulse, ref offset, ref lookEuler);
                    break;
                case RhythmActionType.Slide:
                    ApplySlide(normalized, pulse, ref offset, ref lookEuler);
                    break;
                case RhythmActionType.LongJump:
                    ApplyLongJump(normalized, pulse, ref offset, ref lookEuler);
                    break;
                case RhythmActionType.Grapple:
                    ApplyGrapple(evt, smooth, pulse, normalized, ref offset, ref lookEuler);
                    break;
            }

            if (isMiss)
            {
                var shake = Mathf.Sin(activeTimer * missShakeFrequency) * missShakeAmount * (1f - normalized);
                offset.x += shake;
            }

            offset = SafeVector3(offset, Vector3.zero);
            lookEuler = SafeVector3(lookEuler, Vector3.zero);
            motionRoot.localPosition = restLocalPosition + offset;
            motionRoot.localRotation = restLocalRotation * Quaternion.Euler(lookEuler.x, lookEuler.y, lookEuler.z);
        }

        Vector3 GetCameraLookEuler(RhythmActionEvent evt, float normalized)
        {
            normalized = Safe01(normalized);
            var pulse = Mathf.Sin(normalized * Mathf.PI);
            var lookEuler = Vector3.zero;

            switch (evt.ActionType)
            {
                case RhythmActionType.Step:
                {
                    var side = evt.Hand == RhythmHand.Left ? -1f : 1f;
                    lookEuler.y += side * stepYawDegrees * pulse;
                    lookEuler.z -= side * stepRollDegrees * pulse;
                    break;
                }
                case RhythmActionType.SideGrab:
                {
                    var side = evt.Direction == RhythmDirection.Left ? -1f : 1f;
                    lookEuler.y += side * sideGrabYawDegrees * pulse;
                    break;
                }
                case RhythmActionType.Slide:
                {
                    var lowered = StageHeight(normalized, slideDropStage, slideHoldStage);
                    lookEuler.x -= slideLookUpDegrees * lowered;
                    lookEuler.z += slideRollDegrees * Mathf.Sin(normalized * Mathf.PI * 2f);
                    break;
                }
                case RhythmActionType.LongJump:
                {
                    var jumpArc = StageJumpArc(normalized, longJumpTakeoffStage, longJumpFloatStage);
                    lookEuler.x += StageJumpLookPitch(normalized, longJumpTakeoffStage, longJumpFloatStage);
                    lookEuler.z += longJumpLandingRollDegrees * Mathf.Sin(normalized * Mathf.PI) * (1f - jumpArc);
                    break;
                }
                case RhythmActionType.Grapple:
                    lookEuler.x -= grappleLookUpDegrees * pulse;
                    if (usePendulumGrappleMotion)
                        lookEuler.z -= grapplePendulumRollDegrees * pulse * GetGrappleBodyLeanSide(evt);
                    break;
            }

            return SafeVector3(lookEuler, Vector3.zero);
        }

        void ConfigureRootMotion(RhythmActionEvent evt, bool isMiss)
        {
            activeMovesRoot = false;
            activeRootDistance = 0f;

            if (evt == null || locomotionRoot == null || !moveRootOnSuccessfulActions)
                return;

            if (isMiss && !moveRootOnMisses)
                return;

            activeRootStartPosition = locomotionRoot.position;
            activeRootDirection = GetPlanarForward();

            switch (evt.ActionType)
            {
                case RhythmActionType.Step:
                    activeRootDistance = stepTravelDistance;
                    break;
                case RhythmActionType.Slide:
                    activeRootDistance = slideTravelDistance;
                    break;
                case RhythmActionType.LongJump:
                    activeRootDistance = longJumpTravelDistance;
                    break;
                case RhythmActionType.Grapple:
                    activeRootDistance = Mathf.Max(grappleTravelDistance, evt.DurationBeats * stepTravelDistance);
                    break;
                case RhythmActionType.SideGrab:
                default:
                    activeRootDistance = 0f;
                    break;
            }

            activeMovesRoot = activeRootDistance > 0f;
        }

        void ApplyRootMotion(RhythmActionEvent evt, float normalized)
        {
            if (!activeMovesRoot || locomotionRoot == null || evt == null)
                return;

            var progress = Safe01(GetRootMotionProgress(evt, normalized));
            var nextPosition = activeRootStartPosition + activeRootDirection * (activeRootDistance * progress);
            if (IsFinite(nextPosition))
                locomotionRoot.position = nextPosition;
        }

        float GetRootMotionProgress(RhythmActionEvent evt, float normalized)
        {
            normalized = Safe01(normalized);

            switch (evt.ActionType)
            {
                case RhythmActionType.Step:
                    return Mathf.Pow(normalized, Mathf.Max(0.25f, stepAccelerationExponent));
                case RhythmActionType.Slide:
                    return StageProgress(normalized, slideDropStage, slideHoldStage, 0.22f, 0.86f);
                case RhythmActionType.LongJump:
                    return StageProgress(normalized, longJumpTakeoffStage, longJumpFloatStage, 0.12f, 0.78f);
                case RhythmActionType.Grapple:
                    return Mathf.SmoothStep(0f, 1f, normalized);
                default:
                    return 0f;
            }
        }

        static float StageProgress(float normalized, float firstStageEnd, float secondStageEnd, float firstStageDistance, float secondStageDistance)
        {
            normalized = Safe01(normalized);
            firstStageEnd = Mathf.Clamp(firstStageEnd, 0.05f, 0.9f);
            secondStageEnd = Mathf.Clamp(secondStageEnd, firstStageEnd + 0.05f, 0.95f);
            firstStageDistance = Mathf.Clamp01(firstStageDistance);
            secondStageDistance = Mathf.Clamp(secondStageDistance, firstStageDistance, 1f);

            if (normalized <= firstStageEnd)
            {
                var t = normalized / firstStageEnd;
                return Mathf.Lerp(0f, firstStageDistance, t * t);
            }

            if (normalized <= secondStageEnd)
            {
                var t = (normalized - firstStageEnd) / (secondStageEnd - firstStageEnd);
                return Mathf.Lerp(firstStageDistance, secondStageDistance, Mathf.SmoothStep(0f, 1f, t));
            }

            var landingDuration = 1f - secondStageEnd;
            var landingT = landingDuration <= 0f ? 1f : (normalized - secondStageEnd) / landingDuration;
            return Mathf.Lerp(secondStageDistance, 1f, 1f - Mathf.Pow(1f - Mathf.Clamp01(landingT), 2f));
        }

        static float ForwardLeanArc01(float normalized, float apexTime)
        {
            normalized = Safe01(normalized);
            return Mathf.Pow(Mathf.Max(0f, Mathf.Sin(normalized * Mathf.PI)), 0.72f)
                * Mathf.Lerp(0.82f, 1f, Safe01(apexTime));
        }

        static float LandingCompression(float normalized)
        {
            var t = Safe01((normalized - 0.64f) / 0.36f);
            var hit = Mathf.Sin(t * Mathf.PI);
            return Mathf.Pow(Mathf.Max(0f, hit), 1.35f);
        }

        static float GravityArc01(float normalized, float apexTime, float fallPower)
        {
            normalized = Safe01(normalized);
            apexTime = Mathf.Clamp(apexTime, 0.18f, 0.82f);
            fallPower = Mathf.Max(0.35f, fallPower);

            if (normalized <= apexTime)
            {
                var t = normalized / apexTime;
                return 1f - (1f - t) * (1f - t);
            }

            var fallT = (normalized - apexTime) / Mathf.Max(0.001f, 1f - apexTime);
            return Mathf.Max(0f, 1f - Mathf.Pow(fallT, fallPower));
        }

        static float LongJumpForwardPush(float normalized)
        {
            normalized = Safe01(normalized);
            var launch = Mathf.SmoothStep(0f, 1f, Safe01(normalized / 0.22f)) * 0.25f;
            var air = Mathf.SmoothStep(0f, 1f, Safe01((normalized - 0.22f) / 0.58f)) * 0.58f;
            var landing = Mathf.SmoothStep(0f, 1f, Safe01((normalized - 0.8f) / 0.2f)) * 0.17f;
            var travelShape = launch + air + landing;
            return travelShape * ForwardLeanArc01(normalized, 0.56f);
        }

        static float ActionPlateau01(float normalized)
        {
            normalized = Safe01(normalized);
            var inT = Mathf.SmoothStep(0f, 1f, Safe01(normalized / 0.16f));
            var outT = 1f - Mathf.SmoothStep(0f, 1f, Safe01((normalized - 0.84f) / 0.16f));
            return Mathf.Min(inT, outT);
        }

        void ApplyStep(RhythmActionEvent evt, float normalized, float pulse, ref Vector3 offset, ref Vector3 lookEuler)
        {
            var side = evt.Hand == RhythmHand.Left ? -1f : 1f;
            var landing = LandingCompression(normalized);
            var gravityArc = GravityArc01(normalized, 0.46f, gravityFallPower);
            offset.x += side * stepSideSway * pulse;
            offset.y += stepBob * gravityArc;
            offset.y -= stepLandingDip * landing;
            offset.z += stepForward * ForwardLeanArc01(normalized, 0.42f);
            lookEuler.y += side * stepYawDegrees * pulse;
            lookEuler.z -= side * stepRollDegrees * (pulse * 0.75f + landing * 0.25f);
        }

        void ApplySideGrab(RhythmActionEvent evt, float normalized, float pulse, ref Vector3 offset, ref Vector3 lookEuler)
        {
            var side = evt.Direction == RhythmDirection.Left ? -1f : 1f;
            var landing = LandingCompression(normalized);
            var gravityArc = GravityArc01(normalized, 0.42f, gravityFallPower);
            var pull = Mathf.SmoothStep(0f, 1f, normalized);
            offset.x += side * sideGrabSway * (pulse * 0.75f + pull * 0.35f);
            offset.y += sideGrabLift * gravityArc;
            offset.y -= sideGrabLandingDip * landing;
            offset.z += sideGrabForward * ForwardLeanArc01(normalized, 0.48f);
            lookEuler.y += side * sideGrabYawDegrees * (pulse + landing * 0.35f);
            lookEuler.z -= side * stepRollDegrees * 0.32f * pulse;
        }

        void ApplySlide(float normalized, float pulse, ref Vector3 offset, ref Vector3 lookEuler)
        {
            var lowered = StageHeight(normalized, slideDropStage, slideHoldStage);
            var slidePush = StageProgress(normalized, slideDropStage, slideHoldStage, 0.28f, 0.82f);
            offset.y -= slideLowering * lowered;
            offset.z += slideForward * slidePush;
            lookEuler.x -= slideLookUpDegrees * lowered;
            lookEuler.z += slideRollDegrees * Mathf.Sin(normalized * Mathf.PI * 2f) * (0.35f + lowered * 0.65f);
        }

        void ApplyLongJump(float normalized, float pulse, ref Vector3 offset, ref Vector3 lookEuler)
        {
            var jumpArc = GravityArc01(normalized, 0.5f, gravityFallPower);
            var landing = LandingCompression(normalized);
            offset.y += longJumpLift * jumpArc;
            offset.y -= longJumpLandingDip * landing;
            offset.z += longJumpForward * LongJumpForwardPush(normalized);
            lookEuler.x += StageJumpLookPitch(normalized, longJumpTakeoffStage, longJumpFloatStage);
            lookEuler.z += longJumpLandingRollDegrees * Mathf.Sin(normalized * Mathf.PI) * (1f - jumpArc + landing * 0.4f);
        }

        void ApplyGrapple(RhythmActionEvent evt, float smooth, float pulse, float normalized, ref Vector3 offset, ref Vector3 lookEuler)
        {
            if (usePendulumGrappleMotion)
            {
                var radius = Mathf.Max(0.1f, activeGrappleRadius);
                var angle = Mathf.Lerp(-grapplePendulumAngleDegrees, grapplePendulumAngleDegrees, smooth) * Mathf.Deg2Rad;
                var startAngle = grapplePendulumAngleDegrees * Mathf.Deg2Rad;
                var leanSide = GetGrappleBodyLeanSide(evt);
                var swingPulse = Mathf.Sin(normalized * Mathf.PI);
                var swingDepth = Mathf.Cos(angle) - Mathf.Cos(startAngle);

                offset.x += leanSide * grappleHookSideOffset * swingPulse * 0.42f;
                offset.z += Mathf.Sin(angle) * radius * 0.34f * swingPulse;
                offset.y += grappleBodyVerticalOffset * ActionPlateau01(normalized);
                offset.y -= Mathf.Max(0f, swingDepth) * radius * 0.62f;
                offset.y += grappleLift * swingPulse;
                lookEuler.z -= leanSide * grapplePendulumRollDegrees * swingPulse;
                UpdateGrappleVisual(normalized);
            }
            else
            {
                offset.z += grappleForward * ForwardLeanArc01(normalized, 0.5f);
                offset.y += grappleLift * pulse;
            }

            lookEuler.x -= grappleLookUpDegrees * pulse;
        }

        void ConfigureGrappleVisual(RhythmActionEvent evt)
        {
            if (evt == null || evt.ActionType != RhythmActionType.Grapple)
            {
                DestroyGrappleVisual();
                return;
            }

            ResolveGrappleEndPoints();
            activeGrappleHand = evt.Hand;
            activeGrappleRadius = Mathf.Max(
                0.1f,
                grapplePendulumBaseRadius + Mathf.Max(0f, SafeFloat(evt.DurationBeats, 0f)) * grapplePendulumRadiusPerBeat);

            activeGrappleHookPosition = SafeVector3(ResolveFixedGrappleAnchor(evt), transform.position + Vector3.up * grappleHookHeight + Vector3.forward * grappleHookForward);

            EnsureGrappleClaw();
        }

        Vector3 ResolveFixedGrappleAnchor(RhythmActionEvent evt)
        {
            var anchor = FindGrappleAnchor(evt);
            if (anchor != null)
                return anchor.position + Vector3.up * grappleHookAnchorVerticalOffset;

            var reference = Camera.main != null ? Camera.main.transform : motionRoot != null ? motionRoot : transform;
            var forward = reference != null ? reference.forward : Vector3.forward;
            var up = reference != null ? reference.up : Vector3.up;
            var right = reference != null ? reference.right : Vector3.right;
            var side = GetGrappleSide(evt);
            var ropeEnd = GetRopeEndPoint(evt.Hand);
            var origin = ropeEnd != null
                ? ropeEnd.position
                : motionRoot != null ? motionRoot.position : transform.position;

            return origin
                + forward.normalized * grappleHookForward
                + up.normalized * (grappleHookHeight + grappleHookAnchorVerticalOffset)
                + right.normalized * side * grappleHookSideOffset;
        }

        static Transform FindGrappleAnchor(RhythmActionEvent evt)
        {
            if (evt == null)
                return null;

            var exact = FindTransformByName($"Grapple Hook Anchor Beat {evt.Beat:0.##}");
            if (exact != null)
                return exact;

            var rounded = Mathf.RoundToInt(evt.Beat);
            return FindTransformByName($"Grapple Hook Anchor Beat {rounded}");
        }

        Transform GetRopeEndPoint(RhythmHand hand)
        {
            if (hand == RhythmHand.Left)
                return leftRopeEndPoint != null ? leftRopeEndPoint : rightRopeEndPoint;

            return rightRopeEndPoint != null ? rightRopeEndPoint : leftRopeEndPoint;
        }

        float GetGrappleSide(RhythmActionEvent evt)
        {
            var hand = evt != null ? evt.Hand : activeGrappleHand;
            return hand == RhythmHand.Left ? -1f : 1f;
        }

        float GetGrappleBodyLeanSide(RhythmActionEvent evt)
        {
            return -GetGrappleSide(evt);
        }

        void UpdateGrappleVisual(float normalized)
        {
            var ropeEnd = GetRopeEndPoint(activeGrappleHand);
            if (ropeEnd == null)
                return;

            EnsureGrappleRope();
            EnsureGrappleClaw();

            if (runtimeGrappleClaw != null)
                runtimeGrappleClaw.position = activeGrappleHookPosition;

            if (runtimeGrappleRope == null)
                return;

            var end = ropeEnd.position;
            var midpoint = (activeGrappleHookPosition + end) * 0.5f;
            var direction = end - activeGrappleHookPosition;
            var length = direction.magnitude;
            runtimeGrappleRope.position = midpoint;
            runtimeGrappleRope.rotation = length > 0.001f
                ? Quaternion.FromToRotation(Vector3.up, direction.normalized)
                : Quaternion.identity;
            runtimeGrappleRope.localScale = new Vector3(grappleRopeRadius, Mathf.Max(0.001f, length * 0.5f), grappleRopeRadius);
        }

        void EnsureGrappleRope()
        {
            if (runtimeGrappleRope != null)
                return;

            var rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rope.name = "Runtime Grapple Cable";
            var renderer = rope.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = ResolveGrappleRopeMaterial();

            var collider = rope.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            runtimeGrappleRope = rope.transform;
        }

        void EnsureGrappleClaw()
        {
            if (runtimeGrappleClaw != null)
                return;

            var claw = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            claw.name = "Runtime Grapple Claw";
            claw.transform.position = activeGrappleHookPosition;
            claw.transform.localScale = grappleClawScale;
            var renderer = claw.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = grappleClawMaterial != null ? grappleClawMaterial : ResolveGrappleRopeMaterial();

            var collider = claw.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            runtimeGrappleClaw = claw.transform;
        }

        Material ResolveGrappleRopeMaterial()
        {
            if (grappleRopeMaterial != null)
                return grappleRopeMaterial;

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.name = "Runtime Red Grapple Cable Material";
            material.color = new Color(1f, 0.04f, 0.08f, 0.92f);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", new Color(1f, 0.04f, 0.08f, 0.92f));
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", new Color(2.2f, 0.04f, 0.08f, 1f));

            grappleRopeMaterial = material;
            return grappleRopeMaterial;
        }

        void DestroyGrappleVisual()
        {
            if (runtimeGrappleRope != null)
            {
                Destroy(runtimeGrappleRope.gameObject);
                runtimeGrappleRope = null;
            }

            if (runtimeGrappleClaw != null)
            {
                Destroy(runtimeGrappleClaw.gameObject);
                runtimeGrappleClaw = null;
            }
        }

        void ResolveGrappleEndPoints()
        {
            var poseBinder = FindObjectOfType<VRControllerPoseBinder>();
            if (poseBinder != null)
            {
                if (poseBinder.LeftController != null)
                    leftRopeEndPoint = poseBinder.LeftController;
                if (poseBinder.RightController != null)
                    rightRopeEndPoint = poseBinder.RightController;
            }

            if (leftRopeEndPoint == null)
                leftRopeEndPoint = FindTransformByName("LeftControllerAnchor");
            if (rightRopeEndPoint == null)
                rightRopeEndPoint = FindTransformByName("RightControllerAnchor");
        }

        static Transform FindTransformByName(string transformName)
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var result = FindTransformByName(roots[i].transform, transformName);
                if (result != null)
                    return result;
            }

            return null;
        }

        static Transform FindTransformByName(Transform root, string transformName)
        {
            if (root.name == transformName)
                return root;

            foreach (Transform child in root)
            {
                var result = FindTransformByName(child, transformName);
                if (result != null)
                    return result;
            }

            return null;
        }

        void ResolveMotionRoot()
        {
            if (motionRoot != null)
                return;

            if (Camera.main != null && Camera.main.transform.parent != null)
                motionRoot = Camera.main.transform.parent;
            else if (Camera.main != null)
                motionRoot = Camera.main.transform;
        }

        void ResolveLocomotionRoot()
        {
            if (locomotionRoot != null)
                return;

            if (motionRoot != null)
            {
                var candidate = motionRoot;
                while (candidate.parent != null)
                    candidate = candidate.parent;

                locomotionRoot = candidate;
                return;
            }

            if (Camera.main == null)
                return;

            var root = Camera.main.transform;
            while (root.parent != null)
                root = root.parent;

            locomotionRoot = root;
        }

        Vector3 GetPlanarForward()
        {
            var reference = Camera.main != null ? Camera.main.transform : motionRoot != null ? motionRoot : locomotionRoot;
            var forward = reference != null ? reference.forward : Vector3.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                forward = locomotionRoot != null ? locomotionRoot.forward : Vector3.forward;

            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        static float StageHeight(float normalized, float dropEnd, float holdEnd)
        {
            normalized = Safe01(normalized);
            dropEnd = Mathf.Clamp(dropEnd, 0.05f, 0.9f);
            holdEnd = Mathf.Clamp(holdEnd, dropEnd + 0.05f, 0.95f);

            if (normalized <= dropEnd)
                return Mathf.SmoothStep(0f, 1f, normalized / dropEnd);

            if (normalized <= holdEnd)
                return 1f;

            var recoverDuration = 1f - holdEnd;
            var recoverT = recoverDuration <= 0f ? 1f : (normalized - holdEnd) / recoverDuration;
            return 1f - Mathf.SmoothStep(0f, 1f, recoverT);
        }

        static float StageJumpArc(float normalized, float takeoffEnd, float floatEnd)
        {
            normalized = Safe01(normalized);
            takeoffEnd = Mathf.Clamp(takeoffEnd, 0.05f, 0.9f);
            floatEnd = Mathf.Clamp(floatEnd, takeoffEnd + 0.05f, 0.95f);

            if (normalized <= takeoffEnd)
            {
                var t = Mathf.SmoothStep(0f, 1f, normalized / takeoffEnd);
                return Mathf.Lerp(0f, 0.92f, t);
            }

            if (normalized <= floatEnd)
            {
                var t = (normalized - takeoffEnd) / (floatEnd - takeoffEnd);
                return 0.92f + Mathf.Sin(t * Mathf.PI) * 0.08f;
            }

            var landingDuration = 1f - floatEnd;
            var landingT = landingDuration <= 0f ? 1f : (normalized - floatEnd) / landingDuration;
            return Mathf.Lerp(0.92f, 0f, Mathf.SmoothStep(0f, 1f, landingT));
        }

        float StageJumpLookPitch(float normalized, float takeoffEnd, float floatEnd)
        {
            normalized = Safe01(normalized);
            takeoffEnd = Mathf.Clamp(takeoffEnd, 0.05f, 0.9f);
            floatEnd = Mathf.Clamp(floatEnd, takeoffEnd + 0.05f, 0.95f);

            if (normalized <= takeoffEnd)
            {
                var t = Mathf.SmoothStep(0f, 1f, normalized / takeoffEnd);
                return Mathf.Lerp(0f, -longJumpTakeoffLookUpDegrees, t);
            }

            if (normalized <= floatEnd)
            {
                var t = Mathf.SmoothStep(0f, 1f, (normalized - takeoffEnd) / (floatEnd - takeoffEnd));
                return Mathf.Lerp(-longJumpTakeoffLookUpDegrees, longJumpAirLookDownDegrees, t);
            }

            var landingDuration = 1f - floatEnd;
            var landingT = landingDuration <= 0f ? 1f : (normalized - floatEnd) / landingDuration;
            return Mathf.Lerp(longJumpAirLookDownDegrees, 0f, Mathf.SmoothStep(0f, 1f, landingT));
        }

        void CacheRestPose()
        {
            if (motionRoot == null)
                return;

            restLocalPosition = motionRoot.localPosition;
            restLocalRotation = motionRoot.localRotation;
        }

        void RestoreRestPose()
        {
            if (motionRoot == null)
                return;

            motionRoot.localPosition = SafeVector3(restLocalPosition, Vector3.zero);
            motionRoot.localRotation = restLocalRotation;
        }

        static float Safe01(float value)
        {
            if (!IsFinite(value))
                return 0f;

            return Mathf.Clamp01(value);
        }

        static float SafeFloat(float value, float fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        static Vector3 SafeVector3(Vector3 value, Vector3 fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        static RhythmActionEvent CreateDebugActionEvent(VRParkourInputEvent inputEvent)
        {
            switch (inputEvent.actionType)
            {
                case VRParkourActionType.Step:
                    return new RhythmActionEvent(0f, 1f, RhythmActionType.Step, ToRhythmHand(inputEvent.hand), ToRhythmDirection(inputEvent.direction));
                case VRParkourActionType.SideGrab:
                    return new RhythmActionEvent(0f, 1f, RhythmActionType.SideGrab, ToRhythmHand(inputEvent.hand), ToRhythmDirection(inputEvent.direction));
                case VRParkourActionType.Slide:
                    return new RhythmActionEvent(0f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down);
                case VRParkourActionType.LongJump:
                    return new RhythmActionEvent(0f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up);
                case VRParkourActionType.GrappleHoldStarted:
                    return new RhythmActionEvent(0f, 4f, RhythmActionType.Grapple, ToRhythmHand(inputEvent.hand), RhythmDirection.Up);
                case VRParkourActionType.GrappleHoldUpdated:
                case VRParkourActionType.GrappleHoldEnded:
                default:
                    return null;
            }
        }

        static RhythmHand ToRhythmHand(VRHand hand)
        {
            switch (hand)
            {
                case VRHand.Left:
                    return RhythmHand.Left;
                case VRHand.Right:
                    return RhythmHand.Right;
                case VRHand.Both:
                    return RhythmHand.Both;
                default:
                    return RhythmHand.None;
            }
        }

        static RhythmDirection ToRhythmDirection(VRHandDirection direction)
        {
            switch (direction)
            {
                case VRHandDirection.Up:
                    return RhythmDirection.Up;
                case VRHandDirection.Down:
                    return RhythmDirection.Down;
                case VRHandDirection.Left:
                    return RhythmDirection.Left;
                case VRHandDirection.Right:
                    return RhythmDirection.Right;
                default:
                    return RhythmDirection.None;
            }
        }
    }
}
