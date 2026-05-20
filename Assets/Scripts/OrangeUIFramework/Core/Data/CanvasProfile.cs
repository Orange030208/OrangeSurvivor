using UnityEngine;

namespace Orange.UIFramework
{
    [CreateAssetMenu(menuName = "Orange/UI Framework/Canvas Profile", fileName = "CanvasProfile")]
    public sealed class CanvasProfile : ScriptableObject
    {
        [Header("渲染")]
        [SerializeField] private RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
        [SerializeField] private Camera uiCamera;
        [Min(0.01f)]
        [SerializeField] private float planeDistance = 100f;
        [SerializeField] private int rootSortingOrder;

        [Header("缩放")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [Range(0f, 1f)]
        [SerializeField] private float matchWidthOrHeight = 0.5f;

        public RenderMode RenderMode => renderMode;
        public Camera UICamera => uiCamera;
        public float PlaneDistance => planeDistance;
        public int RootSortingOrder => rootSortingOrder;
        public Vector2 ReferenceResolution => referenceResolution;
        public float MatchWidthOrHeight => matchWidthOrHeight;

        public ValidationReport Validate()
        {
            ValidationReport report = new ValidationReport();
            if (renderMode == RenderMode.WorldSpace)
            {
                report.AddError($"CanvasProfile '{name}' uses WorldSpace, but OrangeUIFramework only supports ScreenSpaceOverlay and ScreenSpaceCamera.");
            }

            if (planeDistance <= 0f)
            {
                report.AddError($"CanvasProfile '{name}' has invalid planeDistance '{planeDistance}'.");
            }

            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
            {
                report.AddError($"CanvasProfile '{name}' has invalid referenceResolution '{referenceResolution}'.");
            }

            return report;
        }

        [ContextMenu("Log Validation Report")]
        private void LogValidationReport()
        {
            ValidationReport report = Validate();
            if (report.HasErrors)
            {
                Debug.LogError(report.ToDisplayString(), this);
                return;
            }

            Debug.Log(report.ToDisplayString(), this);
        }

        private void OnValidate()
        {
            planeDistance = Mathf.Max(0.01f, planeDistance);
            referenceResolution.x = Mathf.Max(1f, referenceResolution.x);
            referenceResolution.y = Mathf.Max(1f, referenceResolution.y);
            matchWidthOrHeight = Mathf.Clamp01(matchWidthOrHeight);
        }
    }
}
