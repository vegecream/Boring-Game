using System;
using UnityEngine;
using UnityEngine.Events;

namespace BoringRun.VRInput
{
    [Serializable]
    public sealed class VRParkourInputUnityEvent : UnityEvent<VRParkourInputEvent>
    {
    }

    public sealed class VRParkourInputEvents : MonoBehaviour
    {
        [SerializeField] VRInputReader inputReader;
        [SerializeField, Min(0f)] float grappleHoldThreshold = 0.2f;
        [SerializeField] bool requireSimultaneousTwoHandPress;

        [Header("Events")]
        [SerializeField] VRParkourInputUnityEvent inputRecognized = new VRParkourInputUnityEvent();
        [SerializeField] VRParkourInputUnityEvent stepRecognized = new VRParkourInputUnityEvent();
        [SerializeField] VRParkourInputUnityEvent sideGrabRecognized = new VRParkourInputUnityEvent();
        [SerializeField] VRParkourInputUnityEvent slideRecognized = new VRParkourInputUnityEvent();
        [SerializeField] VRParkourInputUnityEvent longJumpRecognized = new VRParkourInputUnityEvent();
        [SerializeField] VRParkourInputUnityEvent turnRecognized = new VRParkourInputUnityEvent();
        [SerializeField] VRParkourInputUnityEvent grappleRecognized = new VRParkourInputUnityEvent();

        bool slideOrJumpChordConsumed;
        bool leftGrappleActive;
        bool rightGrappleActive;

        public event Action<VRParkourInputEvent> InputRecognized;

        public VRParkourInputUnityEvent InputRecognizedUnityEvent => inputRecognized;
        public VRParkourInputUnityEvent StepRecognized => stepRecognized;
        public VRParkourInputUnityEvent SideGrabRecognized => sideGrabRecognized;
        public VRParkourInputUnityEvent SlideRecognized => slideRecognized;
        public VRParkourInputUnityEvent LongJumpRecognized => longJumpRecognized;
        public VRParkourInputUnityEvent TurnRecognized => turnRecognized;
        public VRParkourInputUnityEvent GrappleRecognized => grappleRecognized;

        void Reset()
        {
            inputReader = GetComponent<VRInputReader>();
        }

        void Update()
        {
            var reader = inputReader != null ? inputReader : VRInputReader.Instance;
            if (reader == null)
                return;

            RecognizePlayerTurn(reader);

            var twoHandConsumed = RecognizeTwoHandInputs(reader);
            if (!twoHandConsumed)
            {
                RecognizePressInputs(VRHand.Left, reader.LeftHand);
                RecognizePressInputs(VRHand.Right, reader.RightHand);
            }

            RecognizeGrapple(reader, VRHand.Left, reader.LeftHand, ref leftGrappleActive);
            RecognizeGrapple(reader, VRHand.Right, reader.RightHand, ref rightGrappleActive);
        }

        void RecognizePlayerTurn(VRInputReader reader)
        {
            var turn = reader.PlayerTurn;
            if (!turn.wasTurnedThisFrame)
                return;

            if (turn.direction == VRHandDirection.Left)
                Emit(new VRParkourInputEvent(VRParkourActionType.TurnLeft, VRHand.Both, VRHandDirection.Left, 0f));
            else if (turn.direction == VRHandDirection.Right)
                Emit(new VRParkourInputEvent(VRParkourActionType.TurnRight, VRHand.Both, VRHandDirection.Right, 0f));
        }

        void RecognizePressInputs(VRHand hand, VRHandInputFrame frame)
        {
            if (!frame.wasPressedThisFrame)
                return;

            if (frame.direction == VRHandDirection.Left || frame.direction == VRHandDirection.Right)
            {
                Emit(new VRParkourInputEvent(VRParkourActionType.SideGrab, hand, frame.direction, 0f));
                return;
            }

            Emit(new VRParkourInputEvent(VRParkourActionType.Step, hand, frame.direction, 0f));
        }

        bool RecognizeTwoHandInputs(VRInputReader reader)
        {
            if (!reader.IsPressed(VRHand.Left) || !reader.IsPressed(VRHand.Right))
            {
                slideOrJumpChordConsumed = false;
                return false;
            }

            if (slideOrJumpChordConsumed)
                return true;

            if (requireSimultaneousTwoHandPress && !reader.AreBothHandsPressedTogether())
                return false;

            if (reader.IsSlideInput())
            {
                Emit(new VRParkourInputEvent(VRParkourActionType.Slide, VRHand.Both, VRHandDirection.Down, 0f));
                slideOrJumpChordConsumed = true;
                return true;
            }

            if (reader.IsLongJumpInput())
            {
                Emit(new VRParkourInputEvent(VRParkourActionType.LongJump, VRHand.Both, VRHandDirection.Up, 0f));
                slideOrJumpChordConsumed = true;
                return true;
            }

            return false;
        }

        void RecognizeGrapple(VRInputReader reader, VRHand hand, VRHandInputFrame frame, ref bool grappleActive)
        {
            var isValidHold = frame.isPressed
                && frame.direction == VRHandDirection.Up
                && frame.holdDuration >= grappleHoldThreshold;

            if (isValidHold && !grappleActive)
            {
                grappleActive = true;
                Emit(new VRParkourInputEvent(VRParkourActionType.GrappleHoldStarted, hand, VRHandDirection.Up, frame.holdDuration));
            }

            if (isValidHold)
                Emit(new VRParkourInputEvent(VRParkourActionType.GrappleHoldUpdated, hand, VRHandDirection.Up, frame.holdDuration));

            if (!isValidHold && grappleActive)
            {
                grappleActive = false;
                Emit(new VRParkourInputEvent(VRParkourActionType.GrappleHoldEnded, hand, frame.direction, frame.holdDuration));
            }
        }

        void Emit(VRParkourInputEvent inputEvent)
        {
            InputRecognized?.Invoke(inputEvent);
            inputRecognized.Invoke(inputEvent);

            switch (inputEvent.actionType)
            {
                case VRParkourActionType.Step:
                    stepRecognized.Invoke(inputEvent);
                    break;
                case VRParkourActionType.SideGrab:
                    sideGrabRecognized.Invoke(inputEvent);
                    break;
                case VRParkourActionType.Slide:
                    slideRecognized.Invoke(inputEvent);
                    break;
                case VRParkourActionType.LongJump:
                    longJumpRecognized.Invoke(inputEvent);
                    break;
                case VRParkourActionType.TurnLeft:
                case VRParkourActionType.TurnRight:
                    turnRecognized.Invoke(inputEvent);
                    break;
                case VRParkourActionType.GrappleHoldStarted:
                case VRParkourActionType.GrappleHoldUpdated:
                case VRParkourActionType.GrappleHoldEnded:
                    grappleRecognized.Invoke(inputEvent);
                    break;
            }
        }
    }
}
