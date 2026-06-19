using RhythmParkour;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoringRun.VRInput
{
    public sealed class VRMenuController : MonoBehaviour
    {
        [SerializeField] VRInputReader inputReader;
        [SerializeField] Transform leftRayOrigin;
        [SerializeField] Transform rightRayOrigin;
        [SerializeField] Camera menuCamera;
        [SerializeField] LineRenderer leftRay;
        [SerializeField] LineRenderer rightRay;
        [SerializeField] VRMenuButton[] buttons;
        [SerializeField] LayerMask buttonMask = ~0;
        [SerializeField, Min(1f)] float rayDistance = 12f;
        [SerializeField] bool useHeadGazeFallback = true;
        [SerializeField] bool useKeyboardFallback = true;
        [SerializeField] KeyCode nextKey = KeyCode.DownArrow;
        [SerializeField] KeyCode previousKey = KeyCode.UpArrow;
        [SerializeField] KeyCode confirmKey = KeyCode.Space;
        [SerializeField] KeyCode confirmAltKey = KeyCode.Return;
        [SerializeField] KeyCode backKey = KeyCode.Escape;
        [SerializeField] string backSceneName;
        [SerializeField, Min(0.05f)] float confirmCooldownSeconds = 0.18f;

        int selectedIndex;
        float lastConfirmAt = -100f;

        void Awake()
        {
            ResolveReferences();
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, buttons.Length - 1));
            ApplySelection(null);
        }

        void Update()
        {
            ResolveReferences();

            var hover = FindHoveredButton();
            if (hover != null)
                selectedIndex = IndexOf(hover);

            HandleKeyboardNavigation();
            ApplySelection(hover);
            UpdateRayVisual(leftRay, leftRayOrigin);
            UpdateRayVisual(rightRay, rightRayOrigin);

            if (HasConfirmInput())
                InvokeSelected();

            if (useKeyboardFallback && !string.IsNullOrEmpty(backSceneName) && NewInputKeyboard.WasPressedThisFrame(backKey))
                SceneManager.LoadScene(backSceneName);
        }

        public void SetButtons(VRMenuButton[] menuButtons)
        {
            buttons = menuButtons != null ? menuButtons : new VRMenuButton[0];
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, buttons.Length - 1));
            ApplySelection(null);
        }

        void ResolveReferences()
        {
            if (inputReader == null)
                inputReader = VRInputReader.Instance != null ? VRInputReader.Instance : FindObjectOfType<VRInputReader>();

            if (menuCamera == null)
                menuCamera = Camera.main;

            if (buttons == null || buttons.Length == 0)
                buttons = FindObjectsOfType<VRMenuButton>();
        }

        VRMenuButton FindHoveredButton()
        {
            if (TryRaycastButton(rightRayOrigin, out var rightButton))
                return rightButton;

            if (TryRaycastButton(leftRayOrigin, out var leftButton))
                return leftButton;

            if (useHeadGazeFallback && menuCamera != null && TryRaycastButton(menuCamera.transform, out var gazeButton))
                return gazeButton;

            return null;
        }

        bool TryRaycastButton(Transform origin, out VRMenuButton button)
        {
            button = null;
            if (origin == null)
                return false;

            if (!Physics.Raycast(origin.position, origin.forward, out var hit, rayDistance, buttonMask, QueryTriggerInteraction.Collide))
                return false;

            button = hit.collider.GetComponentInParent<VRMenuButton>();
            return button != null;
        }

        void HandleKeyboardNavigation()
        {
            if (!useKeyboardFallback || buttons == null || buttons.Length == 0)
                return;

            if (NewInputKeyboard.WasPressedThisFrame(nextKey))
                selectedIndex = (selectedIndex + 1) % buttons.Length;
            else if (NewInputKeyboard.WasPressedThisFrame(previousKey))
                selectedIndex = (selectedIndex - 1 + buttons.Length) % buttons.Length;
        }

        bool HasConfirmInput()
        {
            if (Time.unscaledTime - lastConfirmAt < confirmCooldownSeconds)
                return false;

            var keyboardConfirm = useKeyboardFallback &&
                (NewInputKeyboard.WasPressedThisFrame(confirmKey) || NewInputKeyboard.WasPressedThisFrame(confirmAltKey));
            var vrConfirm = inputReader != null && (inputReader.WasPressed(VRHand.Left) || inputReader.WasPressed(VRHand.Right));

            if (!keyboardConfirm && !vrConfirm)
                return false;

            lastConfirmAt = Time.unscaledTime;
            return true;
        }

        void InvokeSelected()
        {
            if (buttons == null || buttons.Length == 0)
                return;

            selectedIndex = Mathf.Clamp(selectedIndex, 0, buttons.Length - 1);
            var button = buttons[selectedIndex];
            if (button == null)
                return;

            switch (button.Action)
            {
                case VRMenuButtonAction.LoadScene:
                    if (!string.IsNullOrEmpty(button.SceneName))
                        SceneManager.LoadScene(button.SceneName);
                    break;
                case VRMenuButtonAction.Quit:
                    Application.Quit();
                    Debug.Log("[VRMenuController] Quit requested.");
                    break;
            }
        }

        void ApplySelection(VRMenuButton hovered)
        {
            if (buttons == null)
                return;

            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    continue;

                buttons[i].SetState(i == selectedIndex, buttons[i] == hovered);
            }
        }

        int IndexOf(VRMenuButton button)
        {
            if (buttons == null || button == null)
                return selectedIndex;

            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == button)
                    return i;
            }

            return selectedIndex;
        }

        void UpdateRayVisual(LineRenderer line, Transform origin)
        {
            if (line == null || origin == null)
                return;

            var end = origin.position + origin.forward * rayDistance;
            if (Physics.Raycast(origin.position, origin.forward, out var hit, rayDistance, buttonMask, QueryTriggerInteraction.Collide))
                end = hit.point;

            line.positionCount = 2;
            line.SetPosition(0, origin.position);
            line.SetPosition(1, end);
        }
    }
}
