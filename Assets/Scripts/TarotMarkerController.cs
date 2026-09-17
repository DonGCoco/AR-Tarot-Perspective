using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DisallowMultipleComponent]
public sealed class TarotMarkerController : MonoBehaviour
{
    private const string DefaultMarkerName = "QueenOfSwords";
    private const string DisplayTextureResource = "QueenOfSwordsDisplay";

    [Header("Tracking")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private Camera arCamera;
    [SerializeField] private string markerName = DefaultMarkerName;

    [Header("Viewpoint interaction")]
    [SerializeField] private float sideActivationDistance = 0.08f;
    [SerializeField] private bool invertLeftRight;

    [Header("Floating presentation")]
    [SerializeField] private float cameraLift = 0.055f;
    [SerializeField] private float worldLift = 0.035f;

    private ARTrackedImage activeImage;
    private Transform contentRoot;
    private PanelHandle leftPanel;
    private PanelHandle rightPanel;
    private HeroHandle hero;
    private Vector3 rootVelocity;
    private float currentSide;
    private float sideVelocity;

    private static readonly Color Paper = new Color(0.925f, 0.895f, 0.82f, 0.98f);
    private static readonly Color Ink = new Color(0.105f, 0.095f, 0.08f, 1f);
    private static readonly Color Border = new Color(0.19f, 0.155f, 0.115f, 0.96f);
    private static readonly Color UprightAccent = new Color(0.31f, 0.64f, 0.73f, 1f);
    private static readonly Color ReversedAccent = new Color(0.67f, 0.26f, 0.16f, 1f);

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

    private sealed class HeroHandle
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public Image Glow;
        public Vector3 BaseScale;
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

        UpdateFloatingPose(activeImage);
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

        GameObject root = new GameObject("Queen of Swords Floating Presentation");
        contentRoot = root.transform;

        Vector3 toCamera = (arCamera.transform.position - trackedImage.transform.position).normalized;
        contentRoot.position = trackedImage.transform.position + toCamera * cameraLift + Vector3.up * worldLift;
        contentRoot.rotation = FaceCameraRotation(contentRoot.position);
        contentRoot.localScale = Vector3.one;

        hero = CreateHeroCard();

        leftPanel = CreatePanel(
            "Reversed Wing",
            "R E V E R S E D",
            new[] { "COLDNESS", "HARSH JUDGMENT", "ISOLATION" },
            new Vector3(-0.064f, 0f, 0.018f),
            76f,
            -1f,
            ReversedAccent);

        rightPanel = CreatePanel(
            "Upright Wing",
            "U P R I G H T",
            new[] { "CLARITY", "INDEPENDENCE", "TRUTH" },
            new Vector3(0.064f, 0f, 0.018f),
            -76f,
            1f,
            UprightAccent);
    }

    private HeroHandle CreateHeroCard()
    {
        Texture2D cardTexture = Resources.Load<Texture2D>(DisplayTextureResource);

        float aspect = cardTexture != null && cardTexture.width > 0
            ? (float)cardTexture.height / cardTexture.width
            : 1.74f;

        const float canvasWidth = 360f;
        float canvasHeight = canvasWidth * aspect;
        const float worldScale = 0.00036f;

        GameObject cardObject = new GameObject(
            "Hero Queen of Swords",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        cardObject.transform.SetParent(contentRoot, false);

        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(canvasWidth, canvasHeight);
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.Euler(-4f, 0f, 0f);
        rect.localScale = Vector3.one * worldScale;

        Canvas canvas = cardObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = arCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;

        CanvasGroup group = cardObject.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject glowObject = new GameObject("Aura", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glowObject.transform.SetParent(cardObject.transform, false);
        RectTransform glowRect = glowObject.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-28f, -28f);
        glowRect.offsetMax = new Vector2(28f, 28f);
        glowRect.localPosition = new Vector3(0f, 0f, 10f);

        Image glow = glowObject.GetComponent<Image>();
        glow.color = new Color(UprightAccent.r, UprightAccent.g, UprightAccent.b, 0.20f);
        glow.raycastTarget = false;

        GameObject shadowObject = new GameObject("Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shadowObject.transform.SetParent(cardObject.transform, false);
        RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = new Vector2(10f, -12f);
        shadowRect.offsetMax = new Vector2(10f, -12f);
        shadowRect.localPosition = new Vector3(0f, 0f, 5f);

        Image shadow = shadowObject.GetComponent<Image>();
        shadow.color = new Color(0.03f, 0.025f, 0.02f, 0.33f);
        shadow.raycastTarget = false;

        GameObject frameObject = new GameObject("Card Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameObject.transform.SetParent(cardObject.transform, false);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        Stretch(frameRect, Vector2.zero, Vector2.zero);
        frameRect.localPosition = Vector3.zero;

        Image frame = frameObject.GetComponent<Image>();
        frame.color = new Color(0.94f, 0.90f, 0.80f, 1f);
        frame.raycastTarget = false;

        Outline frameOutline = frameObject.AddComponent<Outline>();
        frameOutline.effectColor = Border;
        frameOutline.effectDistance = new Vector2(4f, -4f);
        frameOutline.useGraphicAlpha = true;

        GameObject imageObject = new GameObject("Card Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        imageObject.transform.SetParent(cardObject.transform, false);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = new Vector2(8f, 8f);
        imageRect.offsetMax = new Vector2(-8f, -8f);

        RawImage rawImage = imageObject.GetComponent<RawImage>();
        rawImage.texture = cardTexture;
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;

        if (cardTexture == null)
            Debug.LogWarning("QueenOfSwordsDisplay texture was not found in Resources. The hero card will appear blank until the resource is present.");

        return new HeroHandle
        {
            Rect = rect,
            Group = group,
            Glow = glow,
            BaseScale = Vector3.one * worldScale
        };
    }

    private PanelHandle CreatePanel(
        string name,
        string headerText,
        string[] meanings,
        Vector3 basePosition,
        float closedYaw,
        float outwardDirection,
        Color accentColor)
    {
        const float worldScale = 0.00031f;
        Vector2 canvasSize = new Vector2(340f, 470f);

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
        rect.localPosition = basePosition;
        rect.localRotation = Quaternion.Euler(0f, closedYaw, 0f);
        rect.localScale = Vector3.one * worldScale;

        Canvas canvas = panelObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = arCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 12;

        CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
        group.alpha = 0.72f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject paperObject = new GameObject("Paper", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        paperObject.transform.SetParent(panelObject.transform, false);
        RectTransform paperRect = paperObject.GetComponent<RectTransform>();
        Stretch(paperRect, Vector2.zero, Vector2.zero);

        Image paper = paperObject.GetComponent<Image>();
        paper.color = Paper;
        paper.raycastTarget = false;

        Outline outline = paperObject.AddComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = true;

        GameObject spineObject = new GameObject("Spine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        spineObject.transform.SetParent(panelObject.transform, false);
        RectTransform spineRect = spineObject.GetComponent<RectTransform>();
        float spineX = outwardDirection < 0f ? 1f : 0f;
        spineRect.anchorMin = new Vector2(spineX, 0.08f);
        spineRect.anchorMax = new Vector2(spineX, 0.92f);
        spineRect.pivot = new Vector2(spineX, 0.5f);
        spineRect.sizeDelta = new Vector2(7f, 0f);
        spineRect.anchoredPosition = Vector2.zero;

        Image spine = spineObject.GetComponent<Image>();
        spine.color = accentColor;
        spine.raycastTarget = false;

        CreateFrameLine(panelObject.transform, "Frame Top", new Vector2(0.5f, 1f), new Vector2(canvasSize.x - 34f, 2f), new Vector2(0f, -17f));
        CreateFrameLine(panelObject.transform, "Frame Bottom", new Vector2(0.5f, 0f), new Vector2(canvasSize.x - 34f, 2f), new Vector2(0f, 17f));

        GameObject headerObject = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        headerObject.transform.SetParent(panelObject.transform, false);
        RectTransform headerRect = headerObject.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0.08f, 0.73f);
        headerRect.anchorMax = new Vector2(0.92f, 0.90f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        Text header = headerObject.GetComponent<Text>();
        header.text = headerText;
        header.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        header.fontSize = 25;
        header.fontStyle = FontStyle.Bold;
        header.alignment = TextAnchor.MiddleCenter;
        header.color = accentColor;
        header.raycastTarget = false;

        GameObject accentObject = new GameObject("Accent Rule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accentObject.transform.SetParent(panelObject.transform, false);
        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.17f, 0.70f);
        accentRect.anchorMax = new Vector2(0.83f, 0.70f);
        accentRect.sizeDelta = new Vector2(0f, 3f);
        accentRect.anchoredPosition = Vector2.zero;

        Image accent = accentObject.GetComponent<Image>();
        accent.color = accentColor;
        accent.raycastTarget = false;

        CreateMeaning(panelObject.transform, meanings[0], 0.53f, 39, FontStyle.Bold, Ink, 0f);
        CreateMeaning(panelObject.transform, meanings[1], 0.37f, 29, FontStyle.Normal, Ink, 2.5f);
        CreateMeaning(panelObject.transform, meanings[2], 0.22f, 29, FontStyle.Normal, Ink, 5f);

        return new PanelHandle
        {
            Rect = rect,
            Group = group,
            BasePosition = basePosition,
            BaseScale = Vector3.one * worldScale,
            ClosedYaw = closedYaw,
            OutwardDirection = outwardDirection,
            CurrentFocus = 0f,
            FocusVelocity = 0f
        };
    }

    private static void CreateMeaning(
        Transform parent,
        string value,
        float anchorY,
        int fontSize,
        FontStyle style,
        Color color,
        float localDepth)
    {
        GameObject textObject = new GameObject(value, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, anchorY - 0.065f);
        rect.anchorMax = new Vector2(0.92f, anchorY + 0.065f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localPosition += new Vector3(0f, 0f, localDepth);

        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
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

    private void UpdateFloatingPose(ARTrackedImage trackedImage)
    {
        Vector3 markerPosition = trackedImage.transform.position;
        Vector3 toCamera = arCamera.transform.position - markerPosition;
        if (toCamera.sqrMagnitude < 0.0001f)
            toCamera = -arCamera.transform.forward;

        toCamera.Normalize();
        Vector3 targetPosition = markerPosition + toCamera * cameraLift + Vector3.up * worldLift;

        contentRoot.position = Vector3.SmoothDamp(
            contentRoot.position,
            targetPosition,
            ref rootVelocity,
            0.08f,
            Mathf.Infinity,
            Time.deltaTime);

        Quaternion targetRotation = FaceCameraRotation(contentRoot.position);
        contentRoot.rotation = Quaternion.Slerp(contentRoot.rotation, targetRotation, 1f - Mathf.Exp(-10f * Time.deltaTime));
    }

    private Quaternion FaceCameraRotation(Vector3 worldPosition)
    {
        Vector3 awayFromCamera = worldPosition - arCamera.transform.position;
        if (awayFromCamera.sqrMagnitude < 0.0001f)
            awayFromCamera = arCamera.transform.forward;

        return Quaternion.LookRotation(awayFromCamera.normalized, Vector3.up);
    }

    private void UpdateViewpoint(ARTrackedImage trackedImage)
    {
        Vector3 localCameraPosition = trackedImage.transform.InverseTransformPoint(arCamera.transform.position);
        float normalizedSide = Mathf.Clamp(localCameraPosition.x / Mathf.Max(0.01f, sideActivationDistance), -1f, 1f);

        if (invertLeftRight)
            normalizedSide *= -1f;

        currentSide = Mathf.SmoothDamp(currentSide, normalizedSide, ref sideVelocity, 0.11f);

        float rightFocus = SmoothFocus(currentSide);
        float leftFocus = SmoothFocus(-currentSide);

        UpdateSidePanel(rightPanel, rightFocus, leftFocus);
        UpdateSidePanel(leftPanel, leftFocus, rightFocus);
        UpdateHero(currentSide, Mathf.Max(leftFocus, rightFocus));
    }

    private void UpdateHero(float side, float strongestFocus)
    {
        float x = -side * 0.027f;
        float z = 0.012f * Mathf.Abs(side);
        float y = 0.004f * strongestFocus;
        float yaw = -side * 11f;
        float roll = side * 2.5f;
        float scale = Mathf.Lerp(1.04f, 0.92f, strongestFocus);

        hero.Rect.localPosition = new Vector3(x, y, z);
        hero.Rect.localRotation = Quaternion.Euler(-4f, yaw, roll);
        hero.Rect.localScale = hero.BaseScale * scale;
        hero.Group.alpha = Mathf.Lerp(1f, 0.92f, strongestFocus);

        float colourT = Mathf.InverseLerp(-1f, 1f, side);
        Color glowColour = Color.Lerp(ReversedAccent, UprightAccent, colourT);
        glowColour.a = Mathf.Lerp(0.15f, 0.28f, strongestFocus);
        hero.Glow.color = glowColour;
    }

    private static float SmoothFocus(float sideValue)
    {
        float t = Mathf.InverseLerp(0.08f, 0.90f, sideValue);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    private static void UpdateSidePanel(PanelHandle panel, float targetFocus, float oppositeFocus)
    {
        panel.CurrentFocus = Mathf.SmoothDamp(
            panel.CurrentFocus,
            targetFocus,
            ref panel.FocusVelocity,
            0.11f,
            Mathf.Infinity,
            Time.deltaTime);

        float focus = panel.CurrentFocus;

        float alpha = Mathf.Lerp(0.66f, 1f, focus) * Mathf.Lerp(1f, 0.25f, oppositeFocus);
        float scaleMultiplier = Mathf.Lerp(0.64f, 1.38f, focus) * Mathf.Lerp(1f, 0.82f, oppositeFocus);

        float foldedYaw = panel.ClosedYaw + Mathf.Sign(panel.ClosedYaw) * 9f * oppositeFocus;
        float openYaw = 5f * panel.OutwardDirection;
        float yaw = Mathf.Lerp(foldedYaw, openYaw, focus);

        float outwardShift = 0.020f * focus * panel.OutwardDirection;
        float depthShift = Mathf.Lerp(0.045f, -0.060f, focus) + 0.018f * oppositeFocus;
        float verticalLift = 0.010f * focus;

        panel.Group.alpha = alpha;
        panel.Rect.localScale = panel.BaseScale * scaleMultiplier;
        panel.Rect.localRotation = Quaternion.Euler(0f, yaw, 0f);
        panel.Rect.localPosition = panel.BasePosition + new Vector3(outwardShift, verticalLift, depthShift);
    }
}
