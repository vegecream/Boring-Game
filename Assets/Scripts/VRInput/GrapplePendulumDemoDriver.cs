using RhythmParkour;
using UnityEngine;

namespace BoringRun.VRInput
{
    public sealed class GrapplePendulumDemoDriver : MonoBehaviour
    {
        [SerializeField] Transform rigRoot;
        [SerializeField] Transform motionRoot;
        [SerializeField] Transform hookPoint;
        [SerializeField] Transform ropeEndPoint;
        [SerializeField] Transform leftRopeEndPoint;
        [SerializeField] Transform rightRopeEndPoint;
        [SerializeField] Transform ropeVisual;
        [SerializeField] Material ropeMaterial;
        [SerializeField] Transform clawVisual;
        [SerializeField] RhythmHand grappleHand = RhythmHand.Right;
        [SerializeField] bool configureMotionFromGrapple = true;
        [SerializeField] float grappleDurationSeconds = 4f;
        [SerializeField] float approachSeconds = 3f;
        [SerializeField] float swingSeconds = 8f;
        [SerializeField] float exitSeconds = 4f;
        [SerializeField] float approachDistance = 6f;
        [SerializeField] float baseSwingRadius = 8.2f;
        [SerializeField] float swingRadiusPerSecond = 1.35f;
        [SerializeField] float swingRadius = 6.4f;
        [SerializeField] float swingAngleDegrees = 64f;
        [SerializeField, Range(0f, 0.35f)] float swingMidSpeedBoost = 0f;
        [SerializeField] float baseExitDistance = 10f;
        [SerializeField] float exitDistancePerSecond = 3f;
        [SerializeField] float exitDistance = 8f;
        [SerializeField] float cameraRollDegrees = 7f;
        [SerializeField] bool loop = true;

        Vector3 initialRigPosition;
        Quaternion initialMotionRotation;
        float elapsed;

        void Awake()
        {
            if (rigRoot == null)
                rigRoot = transform;

            if (motionRoot == null && Camera.main != null)
                motionRoot = Camera.main.transform.parent;

            CacheRopeMaterialFromExistingVisual();
            ResolveConfiguredMotion();
            initialRigPosition = rigRoot.position;
            initialMotionRotation = motionRoot != null ? motionRoot.localRotation : Quaternion.identity;
        }

        void CacheRopeMaterialFromExistingVisual()
        {
            if (ropeMaterial != null || ropeVisual == null)
                return;

            var renderer = ropeVisual.GetComponent<Renderer>();
            if (renderer != null)
                ropeMaterial = renderer.sharedMaterial;
        }

        void ResolveConfiguredMotion()
        {
            if (ropeEndPoint == null)
                ropeEndPoint = SelectRopeEndPoint();

            if (!configureMotionFromGrapple)
                return;

            var duration = Mathf.Max(0.1f, grappleDurationSeconds);
            swingSeconds = duration;
            swingRadius = Mathf.Max(0.1f, baseSwingRadius + duration * swingRadiusPerSecond);
            exitDistance = Mathf.Max(0f, baseExitDistance + duration * exitDistancePerSecond);
        }

        Transform SelectRopeEndPoint()
        {
            if (grappleHand == RhythmHand.Left)
                return leftRopeEndPoint != null ? leftRopeEndPoint : rightRopeEndPoint;

            return rightRopeEndPoint != null ? rightRopeEndPoint : leftRopeEndPoint;
        }

        void Update()
        {
            var totalSeconds = Mathf.Max(0.1f, approachSeconds + swingSeconds + exitSeconds);
            elapsed += Time.deltaTime;
            if (loop)
                elapsed %= totalSeconds;
            else
                elapsed = Mathf.Min(elapsed, totalSeconds);

            if (hookPoint == null || rigRoot == null)
                return;

            var hook = hookPoint.position;
            var swingStartAngle = -swingAngleDegrees;
            var swingEndAngle = swingAngleDegrees;
            var startAttach = AttachPointAtAngle(hook, swingStartAngle);
            var endAttach = AttachPointAtAngle(hook, swingEndAngle);
            var landing = endAttach + Vector3.forward * exitDistance;
            Vector3 attachPoint;
            float normalizedSwing = 0f;
            var ropeVisible = false;

            if (elapsed < approachSeconds)
            {
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, approachSeconds));
                var approachStart = initialRigPosition + Vector3.back * approachDistance;
                attachPoint = Vector3.Lerp(approachStart, startAttach, t);
            }
            else if (elapsed < approachSeconds + swingSeconds)
            {
                ropeVisible = true;
                normalizedSwing = (elapsed - approachSeconds) / Mathf.Max(0.01f, swingSeconds);
                var eased = SwingProgress01(normalizedSwing);
                var angle = Mathf.Lerp(swingStartAngle, swingEndAngle, eased);
                attachPoint = AttachPointAtAngle(hook, angle);
            }
            else
            {
                var t = Mathf.Clamp01((elapsed - approachSeconds - swingSeconds) / Mathf.Max(0.01f, exitSeconds));
                attachPoint = Vector3.Lerp(endAttach, landing, t);
            }

            rigRoot.position = attachPoint;
            var roll = Mathf.Sin(normalizedSwing * Mathf.PI) * cameraRollDegrees;
            if (motionRoot != null)
                motionRoot.localRotation = initialMotionRotation * Quaternion.Euler(0f, 0f, -roll);

            UpdateRope(hook, attachPoint, ropeVisible);
        }

        Vector3 AttachPointAtAngle(Vector3 hook, float angleDegrees)
        {
            var radians = angleDegrees * Mathf.Deg2Rad;
            return hook + new Vector3(0f, -Mathf.Cos(radians) * swingRadius, Mathf.Sin(radians) * swingRadius);
        }

        void UpdateRope(Vector3 hook, Vector3 attachPoint, bool visible)
        {
            if (clawVisual != null)
                clawVisual.position = hook;

            if (!visible)
            {
                DestroyRuntimeRope();
                return;
            }

            EnsureRuntimeRope();
            if (ropeVisual == null)
                return;

            var ropeEnd = ropeEndPoint != null ? ropeEndPoint.position : attachPoint;
            var midpoint = (hook + ropeEnd) * 0.5f;
            var direction = ropeEnd - hook;
            var length = direction.magnitude;
            ropeVisual.position = midpoint;
            ropeVisual.rotation = length > 0.001f
                ? Quaternion.FromToRotation(Vector3.up, direction.normalized)
                : Quaternion.identity;
            ropeVisual.localScale = new Vector3(0.045f, length * 0.5f, 0.045f);
        }

        void EnsureRuntimeRope()
        {
            if (ropeVisual != null)
            {
                ropeVisual.gameObject.SetActive(true);
                return;
            }

            var rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rope.name = "Runtime Grapple Cable";
            rope.transform.SetParent(null, false);
            var renderer = rope.GetComponent<Renderer>();
            if (renderer != null && ropeMaterial != null)
                renderer.sharedMaterial = ropeMaterial;
            var collider = rope.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            ropeVisual = rope.transform;
        }

        void DestroyRuntimeRope()
        {
            if (ropeVisual == null)
                return;

            Destroy(ropeVisual.gameObject);
            ropeVisual = null;
        }

        float SwingProgress01(float value)
        {
            var t = Mathf.Clamp01(value);
            var boost = Mathf.Clamp(swingMidSpeedBoost, 0f, 0.35f);
            var endpointSafeBias = t * t * (1f - t) * (1f - t) * (2f * t - 1f);
            return Mathf.Clamp01(t + boost * endpointSafeBias);
        }

    }
}
