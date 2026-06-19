using UnityEngine;

namespace BoringRun.VRInput
{
    public sealed class SlowColossalRotationDriver : MonoBehaviour
    {
        [SerializeField] Vector3 angularVelocityDegrees = new Vector3(1.2f, 4f, 0.8f);
        [SerializeField] float hoverAmplitude = 0.8f;
        [SerializeField] float hoverFrequency = 0.08f;
        [SerializeField] float phase;

        Vector3 startPosition;

        void Awake()
        {
            startPosition = transform.position;
        }

        void Update()
        {
            transform.Rotate(angularVelocityDegrees * Time.deltaTime, Space.Self);

            if (hoverAmplitude <= 0f || hoverFrequency <= 0f)
                return;

            var position = startPosition;
            position.y += Mathf.Sin((Time.time + phase) * Mathf.PI * 2f * hoverFrequency) * hoverAmplitude;
            transform.position = position;
        }
    }
}
