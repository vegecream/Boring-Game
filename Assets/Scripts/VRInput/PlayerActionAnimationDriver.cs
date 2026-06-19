using RhythmParkour;
using UnityEngine;

namespace BoringRun.VRInput
{
    public sealed class PlayerActionAnimationDriver : MonoBehaviour
    {
        [SerializeField] VRRhythmActionPrototype actionSource;
        [SerializeField] VRParkourInputEvents debugInputSource;
        [SerializeField] Transform animatedTarget;
        [SerializeField] Renderer tintRenderer;

        [Header("Timing")]
        [SerializeField] float baseActionDurationSeconds = 0.35f;
        [SerializeField] float grappleSecondsPerBeat = 0.25f;

        [Header("View Follow")]
        [SerializeField] Transform followTarget;
        [SerializeField] bool followMainCameraWhenUnassigned = true;
        [SerializeField] bool followTargetYawOnly = true;
        [SerializeField] Vector3 followLocalOffset = new Vector3(0f, -1.2f, 0.65f);

        [Header("Motion")]
        [SerializeField] float stepHeight = 0.25f;
        [SerializeField] float stepSideOffset = 0.18f;
        [SerializeField] float stepForwardOffset = 0.35f;
        [SerializeField] float sideGrabLeanDegrees = 18f;
        [SerializeField] float sideGrabOffset = 0.35f;
        [SerializeField] float slideScaleY = 0.45f;
        [SerializeField] float slideLowering = 0.35f;
        [SerializeField] float longJumpHeight = 0.85f;
        [SerializeField] float longJumpForwardOffset = 0.7f;
        [SerializeField] float grappleStretchY = 1.35f;
        [SerializeField] float grappleForwardOffset = 0.45f;

        [Header("Feedback")]
        [SerializeField] float missShakeAmount = 0.08f;
        [SerializeField] Color successTint = new Color(0.25f, 1f, 0.45f, 1f);
        [SerializeField] Color missTint = new Color(1f, 0.2f, 0.15f, 1f);

        [Header("Debug")]
        [SerializeField] bool enableKeyboardDebug = true;
        [SerializeField] bool playRecognizedInputEvents = true;
        [SerializeField] KeyCode debugStepKey = KeyCode.Alpha1;
        [SerializeField] KeyCode debugSideGrabKey = KeyCode.Alpha2;
        [SerializeField] KeyCode debugSlideKey = KeyCode.Alpha3;
        [SerializeField] KeyCode debugLongJumpKey = KeyCode.Alpha4;
        [SerializeField] KeyCode debugGrappleKey = KeyCode.Alpha5;
        [SerializeField] KeyCode debugMissModifierKey = KeyCode.LeftShift;

        Transform target;
        VRRhythmActionPrototype subscribedSource;
        VRParkourInputEvents subscribedInputSource;
        RhythmActionEvent activeEvent;
        Vector3 startLocalPosition;
        Quaternion startLocalRotation;
        Vector3 startLocalScale;
        Color startColor = Color.white;
        bool hasStartColor;
        bool activeIsMiss;
        bool debugAlternateSide;
        float activeTimer;
        float activeDuration;

        void Reset()
        {
            animatedTarget = transform;
            tintRenderer = GetComponentInChildren<Renderer>();
        }

        void Awake()
        {
            target = animatedTarget != null ? animatedTarget : transform;
            CacheStartTransform();
            CacheStartColor();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void Start()
        {
            ResolveFollowTarget();

            if (subscribedSource == null)
                Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
            RestoreVisualState();
        }

        void Update()
        {
            if (enableKeyboardDebug)
                PollKeyboardDebug();

            if (target == null)
                return;

            if (activeEvent == null)
            {
                ApplyRestPose();
                return;
            }

            activeTimer += Time.deltaTime;
            var normalized = activeDuration <= 0f ? 1f : Mathf.Clamp01(activeTimer / activeDuration);
            ApplyActionPose(activeEvent, normalized, activeIsMiss);

            if (normalized >= 1f)
            {
                activeEvent = null;
                RestoreVisualState();
            }
        }

        void Subscribe()
        {
            Unsubscribe();

            if (actionSource == null)
                actionSource = FindObjectOfType<VRRhythmActionPrototype>();

            if (actionSource != null)
            {
                subscribedSource = actionSource;
                subscribedSource.JudgmentResolved += HandleJudgmentResolved;
                subscribedSource.ActionMissed += HandleActionMissed;
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
            if (subscribedSource != null)
            {
                subscribedSource.JudgmentResolved -= HandleJudgmentResolved;
                subscribedSource.ActionMissed -= HandleActionMissed;
                subscribedSource = null;
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

            Play(result.Event, result.Kind != VRRhythmJudgmentKind.Hit);
        }

        void HandleActionMissed(RhythmActionEvent evt)
        {
            if (evt == null)
                return;

            Play(evt, true);
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

        static RhythmActionEvent CreateDebugActionEvent(VRParkourInputEvent inputEvent)
        {
            switch (inputEvent.actionType)
            {
                case VRParkourActionType.Step:
                    return new RhythmActionEvent(
                        0f,
                        1f,
                        RhythmActionType.Step,
                        ToRhythmHand(inputEvent.hand),
                        ToRhythmDirection(inputEvent.direction));

                case VRParkourActionType.SideGrab:
                    return new RhythmActionEvent(
                        0f,
                        1f,
                        RhythmActionType.SideGrab,
                        ToRhythmHand(inputEvent.hand),
                        ToRhythmDirection(inputEvent.direction));

                case VRParkourActionType.Slide:
                    return new RhythmActionEvent(0f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down);

                case VRParkourActionType.LongJump:
                    return new RhythmActionEvent(0f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up);

                case VRParkourActionType.GrappleHoldStarted:
                    return new RhythmActionEvent(
                        0f,
                        4f,
                        RhythmActionType.Grapple,
                        ToRhythmHand(inputEvent.hand),
                        RhythmDirection.Up);

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

        void Play(RhythmActionEvent evt, bool isMiss)
        {
            activeEvent = evt;
            activeIsMiss = isMiss;
            activeTimer = 0f;
            activeDuration = GetDuration(evt);

            SetTint(isMiss ? missTint : successTint);
        }

        float GetDuration(RhythmActionEvent evt)
        {
            var tempoScale = subscribedSource != null ? subscribedSource.CurrentTempoScale : 1f;
            var duration = Mathf.Max(0.05f, baseActionDurationSeconds);

            if (evt.ActionType == RhythmActionType.Grapple)
                duration = Mathf.Max(duration, evt.DurationBeats * Mathf.Max(0.05f, grappleSecondsPerBeat));

            return duration / Mathf.Max(0.25f, tempoScale);
        }

        void ApplyActionPose(RhythmActionEvent evt, float normalized, bool isMiss)
        {
            var pulse = Mathf.Sin(normalized * Mathf.PI);
            var position = startLocalPosition;
            var rotation = startLocalRotation;
            var scale = startLocalScale;

            switch (evt.ActionType)
            {
                case RhythmActionType.Step:
                    ApplyStep(evt, pulse, ref position, ref rotation);
                    break;
                case RhythmActionType.SideGrab:
                    ApplySideGrab(evt, pulse, ref position, ref rotation);
                    break;
                case RhythmActionType.Slide:
                    ApplySlide(pulse, ref position, ref scale);
                    break;
                case RhythmActionType.LongJump:
                    ApplyLongJump(pulse, ref position, ref scale);
                    break;
                case RhythmActionType.Grapple:
                    ApplyGrapple(normalized, pulse, ref position, ref scale);
                    break;
            }

            if (isMiss)
            {
                var shake = Mathf.Sin(activeTimer * 70f) * missShakeAmount * (1f - normalized);
                position.x += shake;
            }

            ApplyPose(position, rotation);
            target.localScale = scale;
        }

        void ApplyStep(RhythmActionEvent evt, float pulse, ref Vector3 position, ref Quaternion rotation)
        {
            var side = evt.Hand == RhythmHand.Left ? -1f : 1f;
            position.x += side * stepSideOffset * pulse;
            position.y += stepHeight * pulse;
            position.z += stepForwardOffset * pulse;
            rotation *= Quaternion.Euler(0f, 0f, -side * 8f * pulse);
        }

        void ApplySideGrab(RhythmActionEvent evt, float pulse, ref Vector3 position, ref Quaternion rotation)
        {
            var side = evt.Direction == RhythmDirection.Left ? -1f : 1f;
            position.x += side * sideGrabOffset * pulse;
            rotation *= Quaternion.Euler(0f, 0f, -side * sideGrabLeanDegrees * pulse);
        }

        void ApplySlide(float pulse, ref Vector3 position, ref Vector3 scale)
        {
            var squash = Mathf.Lerp(1f, slideScaleY, pulse);
            scale.y *= squash;
            scale.x *= Mathf.Lerp(1f, 1.2f, pulse);
            scale.z *= Mathf.Lerp(1f, 1.15f, pulse);
            position.y -= slideLowering * pulse;
        }

        void ApplyLongJump(float pulse, ref Vector3 position, ref Vector3 scale)
        {
            position.y += longJumpHeight * pulse;
            position.z += longJumpForwardOffset * pulse;
            scale.y *= Mathf.Lerp(1f, 1.2f, pulse);
        }

        void ApplyGrapple(float normalized, float pulse, ref Vector3 position, ref Vector3 scale)
        {
            position.z += grappleForwardOffset * Mathf.SmoothStep(0f, 1f, normalized);
            scale.y *= Mathf.Lerp(1f, grappleStretchY, pulse);
            scale.x *= Mathf.Lerp(1f, 0.85f, pulse);
            scale.z *= Mathf.Lerp(1f, 0.85f, pulse);
        }

        void CacheStartTransform()
        {
            if (target == null)
                return;

            startLocalPosition = target.localPosition;
            startLocalRotation = target.localRotation;
            startLocalScale = target.localScale;
        }

        void CacheStartColor()
        {
            if (tintRenderer == null)
                return;

            startColor = tintRenderer.sharedMaterial != null ? tintRenderer.sharedMaterial.color : Color.white;
            hasStartColor = true;
        }

        void RestoreVisualState()
        {
            if (target != null)
            {
                ApplyRestPose();
                target.localScale = startLocalScale;
            }

            if (hasStartColor)
                SetTint(startColor);
        }

        void SetTint(Color color)
        {
            if (tintRenderer == null || tintRenderer.material == null || !tintRenderer.material.HasProperty("_Color"))
                return;

            tintRenderer.material.color = color;
        }

        void ResolveFollowTarget()
        {
            if (followTarget != null || !followMainCameraWhenUnassigned || Camera.main == null)
                return;

            followTarget = Camera.main.transform;
        }

        void ApplyRestPose()
        {
            ApplyPose(startLocalPosition, startLocalRotation);
            target.localScale = startLocalScale;
        }

        void ApplyPose(Vector3 localPosition, Quaternion localRotation)
        {
            if (followTarget == null)
            {
                target.localPosition = localPosition;
                target.localRotation = localRotation;
                return;
            }

            var actionOffset = localPosition - startLocalPosition;
            var actionRotation = Quaternion.Inverse(startLocalRotation) * localRotation;
            var followRotation = GetFollowRotation();
            target.position = followTarget.position + followRotation * (followLocalOffset + actionOffset);
            target.rotation = followRotation * actionRotation;
        }

        Quaternion GetFollowRotation()
        {
            if (!followTargetYawOnly)
                return followTarget.rotation;

            return Quaternion.Euler(0f, followTarget.eulerAngles.y, 0f);
        }
    }
}
