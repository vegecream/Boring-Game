using UnityEngine;

namespace BoringRun.VRInput
{
    public sealed class VRInputDebugOverlay : MonoBehaviour
    {
        [SerializeField] VRInputReader inputReader;
        [SerializeField] bool showOverlay = true;
        [SerializeField, Min(0.1f)] float eventMessageSeconds = 1.25f;
        [SerializeField] int eventMessageFontSize = 28;
        [SerializeField] Vector2 panelSize = new Vector2(560f, 220f);

        VRParkourInputEvent lastInputEvent;
        bool hasLastInputEvent;
        string lastInputEventText;
        float lastInputEventTime = -999f;

        void Reset()
        {
            inputReader = GetComponent<VRInputReader>();
        }

        void OnEnable()
        {
            var events = GetComponent<VRParkourInputEvents>();
            if (events != null)
                events.InputRecognized += OnInputRecognized;
        }

        void OnDisable()
        {
            var events = GetComponent<VRParkourInputEvents>();
            if (events != null)
                events.InputRecognized -= OnInputRecognized;
        }

        void OnGUI()
        {
            if (!showOverlay)
                return;

            var reader = inputReader != null ? inputReader : VRInputReader.Instance;
            if (reader == null)
                return;

            var panelX = Mathf.Max(12f, Screen.width - panelSize.x - 12f);
            GUILayout.BeginArea(new Rect(panelX, 12f, panelSize.x, panelSize.y), GUI.skin.box);
            GUILayout.Label("VR Input Debug");
            DrawHand("Left", reader.LeftHand);
            DrawHand("Right", reader.RightHand);
            GUILayout.Label($"Turn: tracked={reader.PlayerTurn.isTracked} delta={reader.PlayerTurn.yawDeltaDegrees:0.0} dir={reader.PlayerTurn.direction}");
            GUILayout.Label($"Slide: {reader.IsSlideInput()}  Jump: {reader.IsLongJumpInput()}");
            GUILayout.Label($"Left Grapple 0.2s: {reader.IsGrappleHoldInput(VRHand.Left, 0.2f)}");
            GUILayout.Label($"Right Grapple 0.2s: {reader.IsGrappleHoldInput(VRHand.Right, 0.2f)}");
            if (hasLastInputEvent)
                GUILayout.Label($"Last Event: {lastInputEvent.actionType} {lastInputEvent.hand} {lastInputEvent.direction} {lastInputEvent.holdDuration:0.00}s");
            GUILayout.EndArea();

            DrawTriggeredEventMessage();
        }

        static void DrawHand(string label, VRHandInputFrame frame)
        {
            GUILayout.Label($"{label}: tracked={frame.isTracked} pressed={frame.isPressed} down={frame.wasPressedThisFrame} dir={frame.direction} hold={frame.holdDuration:0.00}s");
        }

        void OnInputRecognized(VRParkourInputEvent inputEvent)
        {
            lastInputEvent = inputEvent;
            hasLastInputEvent = true;
            lastInputEventText = FormatInputEvent(inputEvent);
            lastInputEventTime = Time.time;
        }

        void DrawTriggeredEventMessage()
        {
            if (!hasLastInputEvent || Time.time - lastInputEventTime > eventMessageSeconds)
                return;

            var previousColor = GUI.color;
            var previousFontSize = GUI.skin.label.fontSize;
            var alpha = 1f - Mathf.Clamp01((Time.time - lastInputEventTime) / eventMessageSeconds);

            GUI.color = new Color(0.25f, 1f, 0.95f, alpha);
            GUI.skin.label.fontSize = eventMessageFontSize;
            var messageWidth = Mathf.Min(900f, Screen.width - 24f);
            var messageX = Mathf.Max(12f, Screen.width - messageWidth - 12f);
            GUI.Label(new Rect(messageX, panelSize.y + 24f, messageWidth, 48f), $"Triggered: {lastInputEventText}");
            GUI.skin.label.fontSize = previousFontSize;
            GUI.color = previousColor;
        }

        static string FormatInputEvent(VRParkourInputEvent inputEvent)
        {
            switch (inputEvent.actionType)
            {
                case VRParkourActionType.Step:
                    return $"{inputEvent.hand} Step";
                case VRParkourActionType.SideGrab:
                    return $"{inputEvent.hand} Side Grab {inputEvent.direction}";
                case VRParkourActionType.Slide:
                    return "Both Hands Slide Down";
                case VRParkourActionType.LongJump:
                    return "Both Hands Long Jump Up";
                case VRParkourActionType.TurnLeft:
                    return "Player Turn Left";
                case VRParkourActionType.TurnRight:
                    return "Player Turn Right";
                case VRParkourActionType.GrappleHoldStarted:
                    return $"{inputEvent.hand} Grapple Started";
                case VRParkourActionType.GrappleHoldUpdated:
                    return $"{inputEvent.hand} Grapple Holding {inputEvent.holdDuration:0.00}s";
                case VRParkourActionType.GrappleHoldEnded:
                    return $"{inputEvent.hand} Grapple Ended";
                default:
                    return inputEvent.actionType.ToString();
            }
        }
    }
}
