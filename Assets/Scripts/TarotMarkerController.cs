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
    private PanelHandle rightPanel;

    private static readonly Color Paper = new Color(0.91f, 0.875f, 0.79f, 0.97f);
    private static readonly Color Ink = new Color(0.12f, 0.105f, 0.09f, 1f);
    private static readonly Color Border = new Color(0.19f, 0.16f, 0.12f, 0.95f);
    private static readonly Color UprightAccent = new Color(0.35f, 0.66f, 0.73f, 1f);
    private static readonly Color ReversedAccent = new Color(0.62f, 0.24f, 0.15f, 1f);

    private sealed class PanelHandle
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public Vector3 BasePosition;
        public Vector3 BaseScale;
        public float ClosedYaw;
        public float OutwardDirection;
        public float CurrentFocus;
        public float FocusVelocity;
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

        activeImage = FindTrackedMarker();

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

    private ARTrackedImage FindTrackedMarker()
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
        float hingeOffset = markerWidth * 0.51f;

        // The panels are hinged directly to the left and right edges of the tracked card.
        // Their pivots sit on the inner edge, so rotation feels like opening a book page
        // rather than rotating a floating UI rectangle around its centre.
        leftPanel = CreatePanel(
            "Reversed Panel",
            "REVERSED",
            "Coldness\nHarsh Judgment\nIsolation",
            new Vector3(-hingeOffset, 0f, -0.003f),
            68f,
            -1f,
            ReversedAccent,
            new Vector2(390f, 480f),
            0.00025f);

        rightPanel = CreatePanel(
            "Upright Panel",
            "UPRIGHT",
            "Clarity\nIndependence\nTruth",
            new Vector3(hingeOffset, 0f, -0.003f),
            -68f,
            1f,
            UprightAccent,
            new Vector2(390f, 480f),
            0.00025f);
    }

    private PanelHandle CreatePanel(
        string name,
        string headerText,
        string bodyText,
        Vector3 localPosition,
        float closedYaw,
        float outwardDirection,
        Color accentColor,
        Vector2 canvasSize,
        float worldScale)
    {
        GameObject panelObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        panelObject.transform.SetParent(contentRoot, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.sizeDelta = canvasSize;
        rect.pivot = outwardDirection < 0f ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        rect.localPosition = localPosition;
        rect.localRotation = Quaternion.Euler(0f, closedYaw, 0f);
        rect.localScale = Vector3.one * worldScale;

        Canvas canvas = panelObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = arCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10;

        CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
        group.alpha = 0.72f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject backgroundObject = new GameObject("Paper", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(panelObject.transform, false);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        Stretch(backgroundRect, Vector2.zero, Vector2.zero);

        Image background = backgroundObject.GetComponent<Image>();
        background.color = Paper;
        background.raycastTarget = false;

        Outline outline = backgroundObject.AddComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = true;

        // Thin inner frame. Four simple lines are more reliable than a textured asset
        // and visually echo the printed border of the Rider-Waite card.
        CreateFrameLine(panelObject.transform, "Frame Top", new Vector2(0.5f, 1f), new Vector2(canvasSize.x - 34f, 2.5f), new Vector2(0f, -17f));
        CreateFrameLine(panelObject.transform, "Frame Bottom", new Vector2(0.5f, 0f), new Vector2(canvasSize.x - 34f, 2.5f), new Vector2(0f, 17f));
        CreateFrameLine(panelObject.transform, "Frame Left", new Vector2(0f, 0.5f), new Vector2(2.5f, canvasSize.y - 34f), new Vector2(17f, 0f));
        CreateFrameLine(panelObject.transform, "Frame Right", new Vector2(1f, 0.5f), new Vector2(2.5f, canvasSize.y - 34f), new Vector2(-17f, 0f));

        GameObject headerObject = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        headerObject.transform.SetParent(panelObject.transform, false);
        RectTransform headerRect = headerObject.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0.08f, 0.72f);
        headerRect.anchorMax = new Vector2(0.92f, 0.91f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        Text header = headerObject.GetComponent<Text>();
        header.text = headerText;
        header.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        header.fontSize = 30;
        header.fontStyle = FontStyle.Bold;
        header.alignment = TextAnchor.MiddleCenter;
        header.color = accentColor;
        header.raycastTarget = false;

        GameObject accentObject = new GameObject("Accent Rule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accentObject.transform.SetParent(panelObject.transform, false);
        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.18f, 0.70f);
        accentRect.anchorMax = new Vector2(0.82f, 0.70f);
        accentRect.sizeDelta = new Vector2(0f, 4f);
        accentRect.anchoredPosition = Vector2.zero;
        Image accent = accentObject.GetComponent<Image>();
        accent.color = accentColor;
        accent.raycastTarget = false;

        GameObject bodyObject = new GameObject("Meanings", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        bodyObject.transform.SetParent(panelObject.transform, false);
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.09f, 0.13f);
        bodyRect.anchorMax = new Vector2(0.91f, 0.65f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        Text body = bodyObject.GetComponent<Text>();
        body.text = bodyText;
        body.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        body.fontSize = 36;
        body.lineSpacing = 1.35f;
        body.alignment = TextAnchor.MiddleCenter;
        body.color = Ink;
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.raycastTarget = false;

        return new PanelHandle
        {
            Rect = rect,
            Group = group,
            BasePosition = localPosition,
            BaseScale = Vector3.one * worldScale,
            ClosedYaw = closedYaw,
            OutwardDirection = outwardDirection,
            CurrentFocus = 0f,
            FocusVelocity = 0f
        };
    }

    private static void CreateFrameLine(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(parent, false);

        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = lineObject.GetComponent<Image>();
        image.color = Border;
        image.raycastTarget = false;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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
    }

    private static float SmoothFocus(float sideValue)
    {
        float t = Mathf.InverseLerp(0.08f, 0.92f, sideValue);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    private static void UpdateSidePanel(PanelHandle panel, float targetFocus, float oppositeFocus)
    {
        panel.CurrentFocus = Mathf.SmoothDamp(
            panel.CurrentFocus,
            targetFocus,
            ref panel.FocusVelocity,
            0.13f,
            Mathf.Infinity,
            Time.deltaTime);

        float focus = panel.CurrentFocus;

        // Stronger near/far separation: the selected page opens toward the viewer,
        // grows and moves forward; the opposite page folds almost shut and recedes.
        float alpha = Mathf.Lerp(0.70f, 1f, focus) * Mathf.Lerp(1f, 0.40f, oppositeFocus);
        float scaleMultiplier = Mathf.Lerp(0.78f, 1.22f, focus) * Mathf.Lerp(1f, 0.82f, oppositeFocus);

        float extraClose = 10f * oppositeFocus * Mathf.Sign(panel.ClosedYaw);
        float yaw = Mathf.Lerp(panel.ClosedYaw + extraClose, 4f * panel.OutwardDirection, focus);

        float outwardShift = 0.018f * focus * panel.OutwardDirection;
        float depthShift = Mathf.Lerp(0.010f, -0.038f, focus) + (0.012f * oppositeFocus);
        float verticalLift = 0.006f * focus;

        panel.Group.alpha = alpha;
        panel.Rect.localScale = panel.BaseScale * scaleMultiplier;
        panel.Rect.localRotation = Quaternion.Euler(0f, yaw, 0f);
        panel.Rect.localPosition = panel.BasePosition + new Vector3(outwardShift, verticalLift, depthShift);
    }
}
