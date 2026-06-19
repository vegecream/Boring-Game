using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;
using InputSystemDevice = UnityEngine.InputSystem.InputDevice;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace BoringRun.VRInput
{
    public sealed class VRControllerDebugVisuals : MonoBehaviour
    {
        [SerializeField] bool showVisuals = true;
        [SerializeField] Color leftColor = new Color(0.05f, 0.9f, 1f, 1f);
        [SerializeField] Color rightColor = new Color(1f, 0.2f, 0.85f, 1f);
        [SerializeField, Range(0.25f, 2f)] float visualScale = 1f;
        [SerializeField] Transform trackingSpace;

        HandVisual leftVisual;
        HandVisual rightVisual;

        void Awake()
        {
            leftVisual = CreateVisual("Left Controller Visual", leftColor);
            rightVisual = CreateVisual("Right Controller Visual", rightColor);
        }

        void Update()
        {
            if (trackingSpace == null)
                trackingSpace = FindTrackingSpace();

            UpdateVisual(VRHand.Left, XRNode.LeftHand, leftVisual);
            UpdateVisual(VRHand.Right, XRNode.RightHand, rightVisual);
        }

        void OnDestroy()
        {
            if (leftVisual.root != null)
                Destroy(leftVisual.root);

            if (rightVisual.root != null)
                Destroy(rightVisual.root);
        }

        void UpdateVisual(VRHand hand, XRNode node, HandVisual visual)
        {
            if (visual.root == null)
                return;

            if (!showVisuals)
            {
                visual.root.SetActive(false);
                return;
            }

            if (TryReadInputSystemPose(hand, out var position, out var rotation) ||
                TryReadXRPose(node, out position, out rotation))
            {
                visual.root.SetActive(true);
                ApplyTrackingPose(visual.root.transform, position, rotation);
                visual.root.transform.localScale = Vector3.one * visualScale;
                return;
            }

            visual.root.SetActive(false);
        }

        void ApplyTrackingPose(Transform visualTransform, Vector3 trackingPosition, Quaternion trackingRotation)
        {
            if (trackingSpace == null)
            {
                visualTransform.SetPositionAndRotation(trackingPosition, trackingRotation);
                return;
            }

            visualTransform.SetPositionAndRotation(
                trackingSpace.TransformPoint(trackingPosition),
                trackingSpace.rotation * trackingRotation);
        }

        static Transform FindTrackingSpace()
        {
            var camera = Camera.main;
            if (camera == null || camera.transform.parent == null)
                return null;

            return camera.transform.parent;
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

        static HandVisual CreateVisual(string name, Color color)
        {
            var root = new GameObject(name);
            root.SetActive(false);

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", color * 1.2f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Controller Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.08f, 0.05f, 0.16f);
            body.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(body.GetComponent<Collider>());

            var forwardRay = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            forwardRay.name = "Forward Ray";
            forwardRay.transform.SetParent(root.transform, false);
            forwardRay.transform.localPosition = new Vector3(0f, 0f, 0.36f);
            forwardRay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            forwardRay.transform.localScale = new Vector3(0.012f, 0.28f, 0.012f);
            forwardRay.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(forwardRay.GetComponent<Collider>());

            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "Forward Tip";
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0f, 0.66f);
            tip.transform.localRotation = Quaternion.identity;
            tip.transform.localScale = Vector3.one * 0.04f;
            tip.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(tip.GetComponent<Collider>());

            return new HandVisual { root = root };
        }

        struct HandVisual
        {
            public GameObject root;
        }
    }
}
