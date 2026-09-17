using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DisallowMultipleComponent]
public sealed class TarotMarkerController : MonoBehaviour
{
    private const string DefaultMarkerName = "QueenOfSwords";

    [Header("Tracking")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private Camera arCamera;
    [SerializeField] private string markerName = DefaultMarkerName;

    [Header("Viewpoint interaction")]
    [SerializeField] private float sideActivationDistance = 0.08f;
    [SerializeField] private bool invertLeftRight;

    private ARTrackedImage activeImage;
    private Transform contentRoot;
    private PanelHandle leftPanel;
    private PanelHandle centerPanel;
    private PanelHandle rightPanel;

    private sealed class PanelHandle
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public Vector3 BasePosition;
        public Vector3 BaseScale;
        public float ClosedYaw;
        public float OutwardDirection;
    }

    public void Configure(ARTrackedImageManager manager, Camera camera)
    {
        trackedImageManager = manager;
        arCamera = camera;
    }

    private void Awake()
    {
        if (trackedImageManager == null)
            trackedImageManager = FindFirstObjectByType<ARTrackedImageManager>();

        if (arCamera == null)
            arCamera = Camera.main;
    }

    private void Update()
    {
        if (trackedImageManager == null || arCamera == null)
            return;

        activeImage = FindTrackedQueenOfSwords();

        if (activeImage == null)
        {
            if (contentRoot != null)
                contentRoot.gameObject.SetActive(false);
            return;
        }

        EnsureContent(activeImage);
        contentRoot.gameObject.SetActive(true);

        if (contentRoot.parent != activeImage.transform)
            contentRoot.SetParent(activeImage.transform, false);

        UpdateViewpoint(activeImage);
    }

    private ARTrackedImage FindTrackedQueenOfSwords()
    {
        foreach (ARTrackedImage trackedImage in trackedImageManager.trackables)
        {
            if (trackedImage.trackingState != TrackingState.Tracking)
                continue;

            if (trackedImage.referenceImage.name == markerName)
                return trackedImage;
        }

        return null;
    }

    private void EnsureContent(ARTrackedImage trackedImage)
    {
        if (contentRoot != null)
            return;

        GameObject root = new GameObject("Queen of Swords AR Content");
        contentRoot = root.transform;
        contentRoot.SetParent(trackedImage.transform, false);
        contentRoot.localPosition = Vector3.zero;
        contentRoot.localRotation = Quaternion.identity;
        contentRoot.localScale = Vector3.one;

        float markerWidth = Mathf.Clamp(trackedImage.size.x, 0.06f, 0.22f);
        float sideOffset = Mathf.Clamp(markerWidth * 0.82f, 0.075f, 0.16f);

        leftPanel = CreatePanel(
            "Reversed Panel",
            "REVERSED\n\nColdness\nHarsh Judgment\nIsolation",
            new Vector3(-sideOffset, 0f, 0f),
            28f,
            -1f,
            new Vector2(430f, 560f),
            0.00024f);

        centerPanel = CreatePanel(
            "Title Panel",
            "QUEEN OF\nSWORDS",
            Vector3.zero,
            0f,
            0f,
            new Vector2(360f, 220f),
            0.00022f);

        rightPanel = CreatePanel(
            "Upright Panel",
            "UPRIGHT\n\nClarity\nIndependence\nTruth",
            new Vector3(sideOffset, 0f, 0f),
            -28f,
            1f,
            new Vector2(430f, 560f),
            0.00024f);
    }

    private PanelHandle CreatePanel(
        string name,
        string text,
        Vector3 localPosition,
        float closedYaw,
        float outwardDirection,
        Vector2 canvasSize,
        float worldScale)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        panelObject.transform.SetParent(contentRoot, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.sizeDelta = canvasSize;
        rect.localPosition = localPosition;
        rect.localRotation = Quaternion.Euler(0f, closedYaw, 0f);
        rect.localScale = Vector3.one * worldScale;

        Canvas canvas = panelObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = arCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10;

        CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(panelObject.transform, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image background = backgroundObject.GetComponent<Image>();
        background.color = new Color(0.055f, 0.065f, 0.085f, 0.82f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(28f, 28f);
        textRect.offsetMax = new Vector2(-28f, -28f);

        Text label = textObject.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 42;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;

        return new PanelHandle
        {
            Rect = rect,
            Group = group,
            BasePosition = localPosition,
            BaseScale = Vector3.one * worldScale,
            ClosedYaw = closedYaw,
            OutwardDirection = outwardDirection
        };
    }

    private void UpdateViewpoint(ARTrackedImage trackedImage)
    {
        Vector3 localCameraPosition = trackedImage.transform.InverseTransformPoint(arCamera.transform.position);
        float normalizedSide = Mathf.Clamp(localCameraPosition.x / Mathf.Max(0.01f, sideActivationDistance), -1f, 1f);

        if (invertLeftRight)
            normalizedSide *= -1f;

        float rightFocus = SmoothFocus(normalizedSide);
        float leftFocus = SmoothFocus(-normalizedSide);

        UpdateSidePanel(rightPanel, rightFocus, leftFocus);
        UpdateSidePanel(leftPanel, leftFocus, rightFocus);

        float strongestFocus = Mathf.Max(leftFocus, rightFocus);
        centerPanel.Group.alpha = Mathf.Lerp(1f, 0.62f, strongestFocus);
        centerPanel.Rect.localScale = centerPanel.BaseScale * Mathf.Lerp(1f, 0.9f, strongestFocus);
        centerPanel.Rect.localPosition = centerPanel.BasePosition;
        centerPanel.Rect.localRotation = Quaternion.identity;
    }

    private static float SmoothFocus(float sideValue)
    {
        float t = Mathf.InverseLerp(0.12f, 0.9f, sideValue);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    private static void UpdateSidePanel(PanelHandle panel, float focus, float oppositeFocus)
    {
        float alpha = Mathf.Lerp(0.38f, 1f, focus) * Mathf.Lerp(1f, 0.42f, oppositeFocus);
        float scaleMultiplier = Mathf.Lerp(0.84f, 1.18f, focus) * Mathf.Lerp(1f, 0.9f, oppositeFocus);
        float yaw = Mathf.Lerp(panel.ClosedYaw, 0f, focus);
        float outwardShift = 0.018f * focus * panel.OutwardDirection;

        panel.Group.alpha = alpha;
        panel.Rect.localScale = panel.BaseScale * scaleMultiplier;
        panel.Rect.localRotation = Quaternion.Euler(0f, yaw, 0f);
        panel.Rect.localPosition = panel.BasePosition + new Vector3(outwardShift, 0f, -0.008f * focus);
    }
}
