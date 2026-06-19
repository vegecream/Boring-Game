using UnityEngine;

namespace BoringRun.VRInput
{
    public enum VRMenuButtonAction
    {
        None,
        LoadScene,
        Quit
    }

    public sealed class VRMenuButton : MonoBehaviour
    {
        [SerializeField] VRMenuButtonAction action = VRMenuButtonAction.None;
        [SerializeField] string sceneName;
        [SerializeField] TextMesh labelText;
        [SerializeField] TextMesh detailText;
        [SerializeField] Renderer[] targetRenderers;
        [SerializeField] Material normalMaterial;
        [SerializeField] Material hoverMaterial;
        [SerializeField] Material selectedMaterial;
        [SerializeField] Color normalTextColor = new Color(0.75f, 0.9f, 1f, 1f);
        [SerializeField] Color selectedTextColor = Color.white;
        [SerializeField] float selectedScale = 1.06f;
        [SerializeField] float pulseAmount = 0.025f;
        [SerializeField] float pulseRate = 5.5f;

        Vector3 baseScale;
        bool isSelected;
        bool isHovered;

        public VRMenuButtonAction Action => action;

        public string SceneName => sceneName;

        public string Label => labelText != null ? labelText.text : name;

        void Awake()
        {
            baseScale = transform.localScale;
            ApplyVisualState();
        }

        void Update()
        {
            if (!isSelected && !isHovered)
                return;

            var pulse = 1f + Mathf.Sin(Time.time * pulseRate) * pulseAmount;
            transform.localScale = baseScale * (isSelected ? selectedScale * pulse : pulse);
        }

        public void Configure(VRMenuButtonAction buttonAction, string targetSceneName, TextMesh label, TextMesh detail, Renderer[] renderers, Material normal, Material hover, Material selected)
        {
            action = buttonAction;
            sceneName = targetSceneName;
            labelText = label;
            detailText = detail;
            targetRenderers = renderers;
            normalMaterial = normal;
            hoverMaterial = hover;
            selectedMaterial = selected;
            baseScale = transform.localScale;
            ApplyVisualState();
        }

        public void SetState(bool selected, bool hovered)
        {
            if (isSelected == selected && isHovered == hovered)
                return;

            isSelected = selected;
            isHovered = hovered;
            ApplyVisualState();
        }

        void ApplyVisualState()
        {
            var material = isSelected
                ? selectedMaterial != null ? selectedMaterial : hoverMaterial
                : isHovered
                    ? hoverMaterial
                    : normalMaterial;

            if (targetRenderers != null && material != null)
            {
                for (var i = 0; i < targetRenderers.Length; i++)
                {
                    if (targetRenderers[i] != null)
                        targetRenderers[i].sharedMaterial = material;
                }
            }

            var textColor = isSelected || isHovered ? selectedTextColor : normalTextColor;
            if (labelText != null)
                labelText.color = textColor;

            if (detailText != null)
                detailText.color = new Color(textColor.r, textColor.g, textColor.b, 0.72f);

            if (!isSelected && !isHovered && baseScale != Vector3.zero)
                transform.localScale = baseScale;
        }
    }
}
