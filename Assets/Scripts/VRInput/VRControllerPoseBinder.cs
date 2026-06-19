using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;
using InputSystemDevice = UnityEngine.InputSystem.InputDevice;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace BoringRun.VRInput
{
    public sealed class VRControllerPoseBinder : MonoBehaviour
    {
        [SerializeField] Transform trackingSpace;
        [SerializeField] Transform leftController;
        [SerializeField] Transform rightController;

        public Transform LeftController => leftController;

        public Transform RightController => rightController;

        void Reset()
        {
            AutoFindReferences();
        }

        void Awake()
        {
            AutoFindMissingReferences();
        }

        void LateUpdate()
        {
            AutoFindMissingReferences();
            UpdateController(VRHand.Left, XRNode.LeftHand, leftController);
            UpdateController(VRHand.Right, XRNode.RightHand, rightController);
        }

        void UpdateController(VRHand hand, XRNode node, Transform controller)
        {
            if (controller == null)
                return;

            if (TryReadOVRPose(hand, out var position, out var rotation) ||
                TryReadInputSystemPose(hand, out position, out rotation) ||
                TryReadXRPose(node, out position, out rotation))
            {
                if (trackingSpace != null)
                {
                    controller.SetPositionAndRotation(
                        trackingSpace.TransformPoint(position),
                        trackingSpace.rotation * rotation);
                }
                else
                {
                    controller.SetPositionAndRotation(position, rotation);
                }
            }
        }

        static bool TryReadOVRPose(VRHand hand, out Vector3 position, out Quaternion rotation)
        {
            var controller = GetOVRController(hand);
            if (controller == OVRInput.Controller.None)
            {
                position = default;
                rotation = default;
                return false;
            }

            position = OVRInput.GetLocalControllerPosition(controller);
            rotation = OVRInput.GetLocalControllerRotation(controller);
            return true;
        }

        void AutoFindMissingReferences()
        {
            if (trackingSpace == null || leftController == null || rightController == null)
                AutoFindReferences();
        }

        void AutoFindReferences()
        {
            if (trackingSpace == null)
            {
                var camera = Camera.main;
                if (camera != null && camera.transform.parent != null)
                    trackingSpace = camera.transform.parent;
            }

            if (leftController == null)
            {
                var left = GameObject.Find("Left Controller");
                if (left != null)
                    leftController = left.transform;
            }

            if (rightController == null)
            {
                var right = GameObject.Find("Right Controller");
                if (right != null)
                    rightController = right.transform;
            }
        }

        static bool TryReadInputSystemPose(VRHand hand, out Vector3 position, out Quaternion rotation)
        {
            foreach (var device in InputSystem.devices)
            {
                if (device == null || !device.enabled || !HasHandUsage(device, hand))
                    continue;

                var positionControl = device.TryGetChildControl<Vector3Control>("devicePosition");
                if (positionControl == null)
                    positionControl = device.TryGetChildControl<Vector3Control>("position");

                var rotationControl = device.TryGetChildControl<QuaternionControl>("deviceRotation");
                if (rotationControl == null)
                    rotationControl = device.TryGetChildControl<QuaternionControl>("rotation");

                if (positionControl == null || rotationControl == null)
                    continue;

                position = positionControl.ReadValue();
                rotation = rotationControl.ReadValue();
                return true;
            }

            position = default;
            rotation = default;
            return false;
        }

        static bool TryReadXRPose(XRNode node, out Vector3 position, out Quaternion rotation)
        {
            XRInputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid ||
                !device.TryGetFeatureValue(XRCommonUsages.devicePosition, out position) ||
                !device.TryGetFeatureValue(XRCommonUsages.deviceRotation, out rotation))
            {
                position = default;
                rotation = default;
                return false;
            }

            return true;
        }

        static bool HasHandUsage(InputSystemDevice device, VRHand hand)
        {
            var expectedUsage = hand == VRHand.Left ? "LeftHand" : "RightHand";
            foreach (var usage in device.usages)
            {
                if (usage.ToString() == expectedUsage)
                    return true;
            }

            return false;
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
    }
}
