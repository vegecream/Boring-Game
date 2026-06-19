using RhythmParkour;
using UnityEngine;

namespace BoringRun.VRInput
{
    public sealed class VRRunResultPanel : MonoBehaviour
    {
        [SerializeField] VRRhythmActionPrototype actionSource;
        [SerializeField] RhythmPortalSuccessTrigger successTrigger;
        [SerializeField] GameObject panelRoot;
        [SerializeField] VRMenuController menuController;
        [SerializeField] TextMesh titleText;
        [SerializeField] TextMesh statsText;
        [SerializeField] TextMesh hintText;
        [SerializeField] string successTitle = "RUN COMPLETE";
        [SerializeField] string failureTitle = "RUN FAILED";

        bool isShowing;

        void Awake()
        {
            ResolveReferences();
            SetVisible(false);
        }

        void OnEnable()
        {
            ResolveReferences();
        }

        void Update()
        {
            if (isShowing)
                return;

            ResolveReferences();
            if (successTrigger != null && successTrigger.IsCompleted)
            {
                ShowSuccess();
                return;
            }

            if (actionSource != null && actionSource.Session != null && actionSource.Session.IsFailed)
                ShowFailure();
        }

        public void ShowSuccess()
        {
            Show(successTitle, new Color(0.35f, 1f, 1f, 1f));
        }

        public void ShowFailure()
        {
            Show(failureTitle, new Color(1f, 0.16f, 0.12f, 1f));
        }

        void Show(string title, Color titleColor)
        {
            if (isShowing)
                return;

            isShowing = true;
            SetVisible(true);

            if (titleText != null)
            {
                titleText.text = title;
                titleText.color = titleColor;
            }

            if (statsText != null)
            {
                statsText.transform.localPosition = new Vector3(0f, 0.44f, -0.08f);
                statsText.transform.localScale = Vector3.one * 0.043f;
                statsText.text = BuildStatsText();
            }

            if (hintText != null)
                hintText.text = "Point and press trigger. Keyboard: Up/Down + Space.";
        }

        void SetVisible(bool visible)
        {
            if (panelRoot != null)
                panelRoot.SetActive(visible);

            if (menuController != null)
                menuController.enabled = visible;
        }

        string BuildStatsText()
        {
            var session = actionSource != null ? actionSource.Session : null;
            if (session == null)
                return "No run data.";

            var hits = session.SuccessCount;
            var misses = session.MissCount;
            var total = Mathf.Max(1, hits + misses);
            var accuracy = hits / (float)total;
            var tempo = session.TempoScale;

            var text =
                $"Hits: {hits}\n" +
                $"Misses: {misses}\n" +
                $"Accuracy: {accuracy * 100f:0}%\n" +
                $"Final Speed: {tempo:0.00}x\n" +
                $"Rank: {GetRank(accuracy, tempo, misses)}";

            return text;
        }

        static string GetRank(float accuracy, float tempoScale, int misses)
        {
            if (misses == 0 && tempoScale >= 0.99f)
                return "S";

            if (accuracy >= 0.9f && tempoScale >= 0.95f)
                return "A";

            if (accuracy >= 0.75f && tempoScale >= 0.85f)
                return "B";

            if (accuracy >= 0.55f)
                return "C";

            return "D";
        }

        void ResolveReferences()
        {
            if (actionSource == null)
                actionSource = FindObjectOfType<VRRhythmActionPrototype>();

            if (successTrigger == null)
                successTrigger = FindObjectOfType<RhythmPortalSuccessTrigger>();

            if (menuController == null)
                menuController = GetComponentInChildren<VRMenuController>(true);
        }
    }
}
