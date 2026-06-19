using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;
using InputSystemDevice = UnityEngine.InputSystem.InputDevice;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace BoringRun.VRInput
{
    public sealed class VRInputReader : MonoBehaviour
    {
        enum DirectionMode
        {
            PositionFromReference,
            ControllerForward,
            ControllerUp
        }

        public static VRInputReader Instance { get; private set; }

        [Header("Button")]
        [SerializeField] VRInputButton actionButton = VRInputButton.Trigger;
        [SerializeField, Range(0.1f, 0.95f)] float analogPressThreshold = 0.55f;

        [Header("Direction")]
        [SerializeField] Transform directionReference;
        [SerializeField] DirectionMode directionMode = DirectionMode.PositionFromReference;
        [SerializeField, Min(0.02f)] float verticalPositionThreshold = 0.18f;
        [SerializeField, Range(0.2f, 0.95f)] float directionMinDot = 0.5f;

        [Header("Timing")]
        [SerializeField, Range(0.02f, 0.5f)] float simultaneousPressTolerance = 0.25f;

        [Header("Player Turn")]
        [SerializeField] bool recognizePlayerTurn = true;
        [SerializeField] Transform playerTurnReference;
        [SerializeField, Range(10f, 120f)] float playerTurnYawThresholdDegrees = 35f;
        [SerializeField, Range(0.05f, 1.5f)] float playerTurnCooldownSeconds = 0.35f;

        VRHandInputFrame leftHand;
        VRHandInputFrame rightHand;
        VRPlayerTurnFrame playerTurn;
        float leftPressStartedAt = -1f;
        float rightPressStartedAt = -1f;
        float playerTurnBaselineYaw;
        float lastPlayerTurnAt = -1000f;
        bool hasPlayerTurnBaseline;

        public VRInputButton ActionButton => actionButton;
        public float SimultaneousPressTolerance => simultaneousPressTolerance;
        public VRHandInputFrame LeftHand => leftHand;
        public VRHandInputFrame RightHand => rightHand;
        public VRPlayerTurnFrame PlayerTurn => playerTurn;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Update()
        {
            leftHand = ReadHand(VRHand.Left, XRNode.LeftHand, leftHand, ref leftPressStartedAt);
            rightHand = ReadHand(VRHand.Right, XRNode.RightHand, rightHand, ref rightPressStartedAt);
            playerTurn = ReadPlayerTurn();
        }

        public VRHandInputFrame GetHand(VRHand hand)
        {
            switch (hand)
            {
                case VRHand.Left:
                    return leftHand;
                case VRHand.Right:
                    return rightHand;
                case VRHand.Both:
                    return CreateSnapshot().GetHand(VRHand.Both);
                default:
                    return default;
            }
        }

        public bool WasPressed(VRHand hand)
        {
            return GetHand(hand).wasPressedThisFrame;
        }

        public bool IsPressed(VRHand hand)
        {
            return GetHand(hand).isPressed;
        }

        public bool WasReleased(VRHand hand)
        {
            return GetHand(hand).wasReleasedThisFrame;
        }

        public float HoldDuration(VRHand hand)
        {
            return GetHand(hand).holdDuration;
        }

        public VRHandDirection Direction(VRHand hand)
        {
            return GetHand(hand).direction;
        }

        public bool IsAlternatingStepInput(VRHand expectedHand)
        {
            return WasPressed(expectedHand);
        }

        public bool IsSideGrabInput(VRHand hand, VRHandDirection side)
        {
            return (side == VRHandDirection.Left || side == VRHandDirection.Right)
                && WasPressed(hand)
                && Direction(hand) == side;
        }

        public bool IsSlideInput()
        {
            return AreBothHandsPressedTogether()
                && Direction(VRHand.Left) == VRHandDirection.Down
                && Direction(VRHand.Right) == VRHandDirection.Down;
        }

        public bool IsLongJumpInput()
        {
            return AreBothHandsPressedTogether()
                && Direction(VRHand.Left) == VRHandDirection.Up
                && Direction(VRHand.Right) == VRHandDirection.Up;
        }

        public bool IsGrappleHoldInput(VRHand hand, float requiredHoldSeconds)
        {
            var frame = GetHand(hand);
            return frame.isPressed
                && frame.holdDuration >= requiredHoldSeconds
                && frame.direction == VRHandDirection.Up;
        }

        public bool AreBothHandsPressedTogether()
        {
            if (!leftHand.isPressed || !rightHand.isPressed)
                return false;

            if (leftHand.wasPressedThisFrame || rightHand.wasPressedThisFrame)
                return Mathf.Abs(leftPressStartedAt - rightPressStartedAt) <= simultaneousPressTolerance;

            return Mathf.Abs(leftPressStartedAt - rightPressStartedAt) <= simultaneousPressTolerance;
        }

        public bool WasPlayerTurnedLeft()
        {
            return playerTurn.wasTurnedThisFrame && playerTurn.direction == VRHandDirection.Left;
        }

        public bool WasPlayerTurnedRight()
        {
            return playerTurn.wasTurnedThisFrame && playerTurn.direction == VRHandDirection.Right;
        }

        public void ResetPlayerTurnBaseline()
        {
            if (TryGetPlayerTurnYaw(out var yaw))
            {
                playerTurnBaselineYaw = yaw;
                hasPlayerTurnBaseline = true;
            }
        }

        public VRInputSnapshot CreateSnapshot(float secondsPerBeat = 1f)
        {
            return VRInputSnapshot.FromHands(leftHand, rightHand, AreBothHandsPressedTogether(), secondsPerBeat);
        }

        VRHandInputFrame ReadHand(VRHand hand, XRNode node, VRHandInputFrame previous, ref float pressStartedAt)
        {
            var ovrFrame = ReadOVRHand(hand, previous, ref pressStartedAt);
            if (ovrFrame.isTracked)
                return ovrFrame;

            var inputSystemFrame = ReadInputSystemHand(hand, previous, ref pressStartedAt);
            if (inputSystemFrame.isTracked)
                return inputSystemFrame;

            return ReadXRHand(node, previous, ref pressStartedAt);
        }

        VRHandInputFrame ReadOVRHand(VRHand hand, VRHandInputFrame previous, ref float pressStartedAt)
        {
            var controller = GetOVRController(hand);
            var frame = new VRHandInputFrame
            {
                isTracked = controller != OVRInput.Controller.None,
                direction = VRHandDirection.Unknown
            };

            if (!frame.isTracked)
                return frame;

            frame.isPressed = ReadButton(controller);
            frame.wasPressedThisFrame = frame.isPressed && !previous.isPressed;
            frame.wasReleasedThisFrame = !frame.isPressed && previous.isPressed;

            if (frame.wasPressedThisFrame)
                pressStartedAt = Time.time;

            frame.holdDuration = frame.isPressed && pressStartedAt >= 0f ? Time.time - pressStartedAt : 0f;
            frame.direction = ReadDirection(controller);
            return frame;
        }

        VRHandInputFrame ReadInputSystemHand(VRHand hand, VRHandInputFrame previous, ref float pressStartedAt)
        {
            InputSystemDevice device = null;
            foreach (var candidate in InputSystem.devices)
            {
                if (candidate != null && candidate.enabled && HasHandUsage(candidate, hand))
                {
                    device = candidate;
                    break;
                }
            }

            var frame = new VRHandInputFrame
            {
                isTracked = device != null,
                direction = VRHandDirection.Unknown
            };

            if (device == null)
                return frame;

            frame.isPressed = ReadButton(device);
            frame.wasPressedThisFrame = frame.isPressed && !previous.isPressed;
            frame.wasReleasedThisFrame = !frame.isPressed && previous.isPressed;

            if (frame.wasPressedThisFrame)
                pressStartedAt = Time.time;

            frame.holdDuration = frame.isPressed && pressStartedAt >= 0f ? Time.time - pressStartedAt : 0f;
            frame.direction = ReadDirection(device);
            return frame;
        }

        bool HasHandUsage(InputSystemDevice device, VRHand hand)
        {
            var expectedUsage = hand == VRHand.Left ? "LeftHand" : "RightHand";
            foreach (var usage in device.usages)
            {
                if (usage.ToString() == expectedUsage)
                    return true;
            }

            return false;
        }

        VRHandInputFrame ReadXRHand(XRNode node, VRHandInputFrame previous, ref float pressStartedAt)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            var frame = new VRHandInputFrame
            {
                isTracked = device.isValid,
                direction = VRHandDirection.Unknown
            };

            if (!device.isValid)
                return frame;

            if (device.TryGetFeatureValue(XRCommonUsages.isTracked, out bool isTracked))
                frame.isTracked = isTracked;

            frame.isPressed = ReadButton(device);
            frame.wasPressedThisFrame = frame.isPressed && !previous.isPressed;
            frame.wasReleasedThisFrame = !frame.isPressed && previous.isPressed;

            if (frame.wasPressedThisFrame)
                pressStartedAt = Time.time;

            frame.holdDuration = frame.isPressed && pressStartedAt >= 0f ? Time.time - pressStartedAt : 0f;
            frame.direction = ReadDirection(device);
            return frame;
        }

        bool ReadButton(XRInputDevice device)
        {
            switch (actionButton)
            {
                case VRInputButton.Grip:
                    return ReadBoolOrAxis(device, XRCommonUsages.gripButton, XRCommonUsages.grip);
                case VRInputButton.PrimaryButton:
                    return device.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool primary) && primary;
                case VRInputButton.SecondaryButton:
                    return device.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bool secondary) && secondary;
                case VRInputButton.Trigger:
                default:
                    return ReadBoolOrAxis(device, XRCommonUsages.triggerButton, XRCommonUsages.trigger);
            }
        }

        bool ReadButton(InputSystemDevice device)
        {
            switch (actionButton)
            {
                case VRInputButton.Grip:
                    return ReadButtonOrAxis(device, "gripButton", "grip");
                case VRInputButton.PrimaryButton:
                    return ReadButtonControl(device, "primaryButton");
                case VRInputButton.SecondaryButton:
                    return ReadButtonControl(device, "secondaryButton");
                case VRInputButton.Trigger:
                default:
                    return ReadButtonOrAxis(device, "triggerButton", "trigger");
            }
        }

        bool ReadButton(OVRInput.Controller controller)
        {
            switch (actionButton)
            {
                case VRInputButton.Grip:
                    return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, controller)
                        || OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller) >= analogPressThreshold;
                case VRInputButton.PrimaryButton:
                    return OVRInput.Get(OVRInput.Button.One, controller);
                case VRInputButton.SecondaryButton:
                    return OVRInput.Get(OVRInput.Button.Two, controller);
                case VRInputButton.Trigger:
                default:
                    return OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controller)
                        || OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller) >= analogPressThreshold;
            }
        }

        bool ReadBoolOrAxis(XRInputDevice device, InputFeatureUsage<bool> buttonUsage, InputFeatureUsage<float> axisUsage)
        {
            if (device.TryGetFeatureValue(buttonUsage, out bool button) && button)
                return true;

            return device.TryGetFeatureValue(axisUsage, out float axis) && axis >= analogPressThreshold;
        }

        bool ReadButtonOrAxis(InputSystemDevice device, string buttonName, string axisName)
        {
            if (ReadButtonControl(device, buttonName))
                return true;

            var axis = device.TryGetChildControl<AxisControl>(axisName);
            return axis != null && axis.ReadValue() >= analogPressThreshold;
        }

        bool ReadButtonControl(InputSystemDevice device, string buttonName)
        {
            var button = device.TryGetChildControl<ButtonControl>(buttonName);
            return button != null && button.ReadValue() >= analogPressThreshold;
        }

        VRHandDirection ReadDirection(XRInputDevice device)
        {
            if (directionMode == DirectionMode.PositionFromReference &&
                device.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 position))
                return DirectionFromTrackingOffset(position - GetReferenceTrackingPosition());

            if (!device.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion rotation))
                return VRHandDirection.Unknown;

            return DirectionFromRotation(rotation, directionMode);
        }

        VRHandDirection ReadDirection(InputSystemDevice device)
        {
            if (directionMode == DirectionMode.PositionFromReference)
            {
                var positionControl = device.TryGetChildControl<Vector3Control>("devicePosition");
                if (positionControl == null)
                    positionControl = device.TryGetChildControl<Vector3Control>("position");

                if (positionControl != null)
                    return DirectionFromTrackingOffset(positionControl.ReadValue() - GetReferenceTrackingPosition());
            }

            var rotationControl = device.TryGetChildControl<QuaternionControl>("deviceRotation");
            if (rotationControl == null)
                rotationControl = device.TryGetChildControl<QuaternionControl>("rotation");

            if (rotationControl == null)
                return VRHandDirection.Unknown;

            return DirectionFromRotation(rotationControl.ReadValue(), directionMode);
        }

        VRHandDirection ReadDirection(OVRInput.Controller controller)
        {
            var position = OVRInput.GetLocalControllerPosition(controller);
            if (directionMode == DirectionMode.PositionFromReference)
                return DirectionFromTrackingOffset(position - GetReferenceTrackingPosition());

            var rotation = OVRInput.GetLocalControllerRotation(controller);
            return DirectionFromRotation(rotation, directionMode);
        }

        static OVRInput.Controller GetOVRController(VRHand hand)
        {
            var connected = OVRInput.GetConnectedControllers();
            var touch = hand == VRHand.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
            if ((connected & touch) != 0)
                return touch;

            var handTracking = hand == VRHand.Left ? OVRInput.Controller.LHand : OVRInput.Controller.RHand;
            if ((connected & handTracking) != 0)
                return handTracking;

            return OVRInput.Controller.None;
        }

        Vector3 GetReferenceTrackingPosition()
        {
            var reference = directionReference != null ? directionReference : Camera.main != null ? Camera.main.transform : null;
            return reference != null ? reference.localPosition : Vector3.zero;
        }

        VRPlayerTurnFrame ReadPlayerTurn()
        {
            var frame = new VRPlayerTurnFrame
            {
                direction = VRHandDirection.Unknown
            };

            if (!recognizePlayerTurn || !TryGetPlayerTurnYaw(out var yaw))
                return frame;

            frame.isTracked = true;

            if (!hasPlayerTurnBaseline)
            {
                playerTurnBaselineYaw = yaw;
                hasPlayerTurnBaseline = true;
                return frame;
            }

            var delta = Mathf.DeltaAngle(playerTurnBaselineYaw, yaw);
            frame.yawDeltaDegrees = delta;

            if (Time.time - lastPlayerTurnAt < playerTurnCooldownSeconds)
                return frame;

            if (Mathf.Abs(delta) < playerTurnYawThresholdDegrees)
                return frame;

            frame.wasTurnedThisFrame = true;
            frame.direction = delta > 0f ? VRHandDirection.Right : VRHandDirection.Left;
            playerTurnBaselineYaw = yaw;
            lastPlayerTurnAt = Time.time;
            return frame;
        }

        bool TryGetPlayerTurnYaw(out float yaw)
        {
            var reference = playerTurnReference != null
                ? playerTurnReference
                : Camera.main != null ? Camera.main.transform : null;

            if (reference == null)
            {
                yaw = 0f;
                return false;
            }

            yaw = reference.eulerAngles.y;
            return true;
        }

        VRHandDirection DirectionFromRotation(Quaternion rotation, DirectionMode mode)
        {
            var localVector = mode == DirectionMode.ControllerUp ? rotation * Vector3.up : rotation * Vector3.forward;
            return DirectionFromLocalVector(localVector);
        }

        VRHandDirection DirectionFromTrackingOffset(Vector3 localOffset)
        {
            if (localOffset.y >= verticalPositionThreshold)
                return VRHandDirection.Up;

            if (localOffset.y <= -verticalPositionThreshold)
                return VRHandDirection.Down;

            return DirectionFromLocalVector(localOffset);
        }

        VRHandDirection DirectionFromLocalVector(Vector3 localVector)
        {
            var normalized = localVector.normalized;
            var x = normalized.x;
            var y = normalized.y;
            var z = normalized.z;
            var absX = Mathf.Abs(x);
            var absY = Mathf.Abs(y);
            var absZ = Mathf.Abs(z);

            if (absY >= absX && absY >= absZ && absY >= directionMinDot)
                return y > 0f ? VRHandDirection.Up : VRHandDirection.Down;

            if (absX >= absY && absX >= absZ && absX >= directionMinDot)
                return x > 0f ? VRHandDirection.Right : VRHandDirection.Left;

            if (z >= directionMinDot)
                return VRHandDirection.Forward;

            return VRHandDirection.Unknown;
        }
    }
}
