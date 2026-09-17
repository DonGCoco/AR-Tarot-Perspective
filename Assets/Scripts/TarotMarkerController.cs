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
    [SerializeField] private float sideActivationDistance = 0.10f;
    [SerializeField] private bool invertLeftRight;

    [Header("Floating presentation")]
    [SerializeField] private float cameraLift = 0.055f;
    [SerializeField] private float worldLift = 0.050f;

    private ARTrackedImage activeImage;
    private Transform contentRoot;
    private HeroHandle hero;
    private PanelHandle leftPanel;
    private PanelHandle rightPanel;

    private Vector3 rootVelocity;
    private float currentSide;
    private float sideVelocity;

    private bool wasTracking;
    private float entranceStartTime;

    private static readonly Color HeroPaper = new Color(0.95f, 0.92f, 0.84f, 1f);
    private static readonly Color Ink = new Color(0.11f, 0.10f, 0.09f, 1f);
    private static readonly Color SideSurface = new Color(0.065f, 0.060f, 0.078f, 0.97f);
    private static readonly Color SideText = new Color(0.94f, 0.91f, 0.84f, 1f);
    private static readonly Color MutedText = new Color(0.78f, 0.75f, 0.70f, 1f);
    private static readonly Color Border = new Color(0.23f, 0.20f, 0.17f, 0.94f);
    private static readonly Color UprightAccent = new Color(0.29f, 0.57f, 0.76f, 1f);
    private static readonly Color ReversedAccent = new Color(0.67f, 0.30f, 0.22f, 1f);

    private sealed class PanelHandle
    {
        public RectTransform Rect;
        public Canvas Canvas;
        public CanvasGroup Group;
        public CanvasGroup InterpretationGroup;
        public CanvasGroup QuestionGroup;
        public RectTransform InterpretationRect;
        public RectTransform QuestionRect;

        public Vector3 NeutralPosition;
        public Vector3 FocusedPosition;
        public Vector3 FarPosition;

        public Vector3 BaseScale;
        public float NeutralScale;
        public float FocusedScale;
        public float FarScale;
        public float NeutralYaw;
        public float Direction;

        public float CurrentFocus;
        public float FocusVelocity;
    }

    private sealed class HeroHandle
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public Image Aura;
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

            wasTracking = false;
            return;
        }

        EnsureContent(activeImage);

        if (!wasTracking)
        {
            ResetEntrance();
            wasTracking = true;
        }

        contentRoot.gameObject.SetActive(true);

        UpdateFloatingPose(activeImage);
        UpdateViewpoint(activeImage);
    }

    private ARTrackedImage FindTrackedMarker()
    {
        foreach (ARTrackedImage trackedImage in trackedImageManager.trackables)
        {
            if (trackedImage.trackingState == TrackingState.Tracking &&
                trackedImage.referenceImage.name == markerName)
            {
                return trackedImage;
            }
        }

        return null;
    }

    private void EnsureContent(ARTrackedImage trackedImage)
    {
        if (contentRoot != null)
            return;

        contentRoot = new GameObject("Queen of Swords Perspective Carousel").transform;

        Vector3 toCamera = arCamera.transform.position - trackedImage.transform.position;
        if (toCamera.sqrMagnitude < 0.0001f)
            toCamera = -arCamera.transform.forward;

        contentRoot.position =
            trackedImage.transform.position +
            toCamera.normalized * cameraLift +
            Vector3.up * worldLift;

        contentRoot.rotation = FaceCameraRotation(contentRoot.position);

        hero = CreateHeroCard();

        leftPanel = CreatePanel(
            "Reversed Plane",
            "R E V E R S E D",
            "Coldness · Harsh Judgment · Isolation",
            "Clarity hardens into distance;\ndiscernment becomes criticism;\nindependence turns inward.",
            "Where might you be judging too quickly?",
            ReversedAccent,
            -1f);

        rightPanel = CreatePanel(
            "Upright Plane",
            "U P R I G H T",
            "Clarity · Independence · Truth",
            "A clear mind, an honest voice,\nand the courage to stand by\nyour own judgment.",
            "What truth are you ready to face?",
            UprightAccent,
            1f);

        ConfigurePanelGeometry(leftPanel, -1f);
        ConfigurePanelGeometry(rightPanel, 1f);
    }

    private HeroHandle CreateHeroCard()
    {
        Texture2D cardTexture = Resources.Load<Texture2D>(DisplayTextureResource);
        float aspect =
            cardTexture != null && cardTexture.width > 0
                ? (float)cardTexture.height / cardTexture.width
                : 1.74f;

        const float width = 360f;
        float height = width * aspect;
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
        rect.sizeDelta = new Vector2(width, height);
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * worldScale;

        Canvas canvas = cardObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = arCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30;

        CanvasGroup group = cardObject.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject auraObject = new GameObject(
            "Aura",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        auraObject.transform.SetParent(cardObject.transform, false);

        RectTransform auraRect = auraObject.GetComponent<RectTransform>();
        auraRect.anchorMin = Vector2.zero;
        auraRect.anchorMax = Vector2.one;
        auraRect.offsetMin = new Vector2(-28f, -28f);
        auraRect.offsetMax = new Vector2(28f, 28f);

        Image aura = auraObject.GetComponent<Image>();
        aura.color = new Color(UprightAccent.r, UprightAccent.g, UprightAccent.b, 0.10f);
        aura.raycastTarget = false;

        GameObject shadowObject = new GameObject(
            "Shadow",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        shadowObject.transform.SetParent(cardObject.transform, false);

        RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = new Vector2(12f, -14f);
        shadowRect.offsetMax = new Vector2(12f, -14f);

        Image shadow = shadowObject.GetComponent<Image>();
        shadow.color = new Color(0.02f, 0.02f, 0.025f, 0.28f);
        shadow.raycastTarget = false;

        GameObject frameObject = new GameObject(
            "Frame",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        frameObject.transform.SetParent(cardObject.transform, false);

        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        Stretch(frameRect);

        Image frame = frameObject.GetComponent<Image>();
        frame.color = HeroPaper;
        frame.raycastTarget = false;

        Outline outline = frameObject.AddComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(4f, -4f);
        outline.useGraphicAlpha = true;

        GameObject imageObject = new GameObject(
            "Card Image",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

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
        {
            Debug.LogWarning(
                "QueenOfSwordsDisplay was not found in Assets/Resources. " +
                "The editor helper should create it automatically from the marker image.");
        }

        return new HeroHandle
        {
            Rect = rect,
            Group = group,
            Aura = aura,
            BaseScale = Vector3.one * worldScale
        };
    }

    private PanelHandle CreatePanel(
        string name,
        string headerText,
        string keywordText,
        string interpretationText,
        string questionText,
        Color accentColor,
        float direction)
    {
        const float worldScale = 0.00029f;
        Vector2 canvasSize = new Vector2(420f, 560f);

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
        rect.localScale = Vector3.one * worldScale;

        Canvas canvas = panelObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = arCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 18;

        CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject shadowObject = new GameObject(
            "Depth Shadow",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        shadowObject.transform.SetParent(panelObject.transform, false);

        RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
        Stretch(shadowRect);
        shadowRect.offsetMin = new Vector2(10f, -12f);
        shadowRect.offsetMax = new Vector2(10f, -12f);

        Image shadow = shadowObject.GetComponent<Image>();
        shadow.color = new Color(0f, 0f, 0f, 0.30f);
        shadow.raycastTarget = false;

        GameObject surfaceObject = new GameObject(
            "Surface",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        surfaceObject.transform.SetParent(panelObject.transform, false);

        RectTransform surfaceRect = surfaceObject.GetComponent<RectTransform>();
        Stretch(surfaceRect);

        Image surface = surfaceObject.GetComponent<Image>();
        surface.color = SideSurface;
        surface.raycastTarget = false;

        Outline surfaceOutline = surfaceObject.AddComponent<Outline>();
        surfaceOutline.effectColor = new Color(
            accentColor.r,
            accentColor.g,
            accentColor.b,
            0.55f);
        surfaceOutline.effectDistance = new Vector2(2f, -2f);
        surfaceOutline.useGraphicAlpha = true;

        CreateFrameLine(
            panelObject.transform,
            "Top",
            new Vector2(0.5f, 1f),
            new Vector2(canvasSize.x - 38f, 2f),
            new Vector2(0f, -19f),
            MutedText);

        CreateFrameLine(
            panelObject.transform,
            "Bottom",
            new Vector2(0.5f, 0f),
            new Vector2(canvasSize.x - 38f, 2f),
            new Vector2(0f, 19f),
            MutedText);

        GameObject edgeObject = new GameObject(
            "Accent Edge",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        edgeObject.transform.SetParent(panelObject.transform, false);

        RectTransform edgeRect = edgeObject.GetComponent<RectTransform>();
        float edgeX = direction < 0f ? 1f : 0f;
        edgeRect.anchorMin = new Vector2(edgeX, 0.08f);
        edgeRect.anchorMax = new Vector2(edgeX, 0.92f);
        edgeRect.pivot = new Vector2(edgeX, 0.5f);
        edgeRect.sizeDelta = new Vector2(6f, 0f);
        edgeRect.anchoredPosition = Vector2.zero;

        Image edge = edgeObject.GetComponent<Image>();
        edge.color = accentColor;
        edge.raycastTarget = false;

        Text header = CreateText(
            panelObject.transform,
            "Header",
            headerText,
            22,
            FontStyle.Bold,
            accentColor,
            TextAnchor.MiddleCenter);

        SetRegion(header.rectTransform, 0.08f, 0.80f, 0.92f, 0.92f);

        GameObject ruleObject = new GameObject(
            "Accent Rule",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        ruleObject.transform.SetParent(panelObject.transform, false);

        RectTransform ruleRect = ruleObject.GetComponent<RectTransform>();
        ruleRect.anchorMin = new Vector2(0.18f, 0.765f);
        ruleRect.anchorMax = new Vector2(0.82f, 0.765f);
        ruleRect.sizeDelta = new Vector2(0f, 3f);

        Image rule = ruleObject.GetComponent<Image>();
        rule.color = accentColor;
        rule.raycastTarget = false;

        Text keywords = CreateText(
            panelObject.transform,
            "Keywords",
            keywordText,
            25,
            FontStyle.Bold,
            SideText,
            TextAnchor.MiddleCenter);

        SetRegion(keywords.rectTransform, 0.08f, 0.57f, 0.92f, 0.73f);
        keywords.lineSpacing = 1.05f;

        Text interpretation = CreateText(
            panelObject.transform,
            "Interpretation",
            interpretationText,
            21,
            FontStyle.Normal,
            MutedText,
            TextAnchor.MiddleCenter);

        SetRegion(interpretation.rectTransform, 0.09f, 0.30f, 0.91f, 0.54f);
        interpretation.lineSpacing = 1.16f;

        CanvasGroup interpretationGroup =
            interpretation.gameObject.AddComponent<CanvasGroup>();
        interpretationGroup.alpha = 0f;
        interpretationGroup.interactable = false;
        interpretationGroup.blocksRaycasts = false;

        Text question = CreateText(
            panelObject.transform,
            "Reflection Question",
            questionText,
            19,
            FontStyle.Italic,
            accentColor,
            TextAnchor.MiddleCenter);

        SetRegion(question.rectTransform, 0.10f, 0.095f, 0.90f, 0.255f);
        question.lineSpacing = 1.10f;

        CanvasGroup questionGroup =
            question.gameObject.AddComponent<CanvasGroup>();
        questionGroup.alpha = 0f;
        questionGroup.interactable = false;
        questionGroup.blocksRaycasts = false;

        return new PanelHandle
        {
            Rect = rect,
            Canvas = canvas,
            Group = group,
            InterpretationGroup = interpretationGroup,
            QuestionGroup = questionGroup,
            InterpretationRect = interpretation.rectTransform,
            QuestionRect = question.rectTransform,
            Direction = direction,
            BaseScale = Vector3.one * worldScale,
            CurrentFocus = 0f,
            FocusVelocity = 0f
        };
    }

    private static void ConfigurePanelGeometry(PanelHandle panel, float direction)
    {
        panel.NeutralPosition = new Vector3(
            0.150f * direction,
            0f,
            0.025f);

        panel.FocusedPosition = new Vector3(
            0.075f * direction,
            0.010f,
            -0.060f);

        panel.FarPosition = new Vector3(
            0.195f * direction,
            -0.006f,
            0.105f);

        panel.NeutralScale = 0.76f;
        panel.FocusedScale = 1.42f;
        panel.FarScale = 0.52f;
        panel.NeutralYaw = -76f * direction;

        panel.Rect.localPosition = panel.NeutralPosition;
        panel.Rect.localRotation =
            Quaternion.Euler(0f, panel.NeutralYaw, 0f);
        panel.Rect.localScale =
            panel.BaseScale * panel.NeutralScale;
    }

    private void ResetEntrance()
    {
        entranceStartTime = Time.time;

        currentSide = 0f;
        sideVelocity = 0f;

        leftPanel.CurrentFocus = 0f;
        leftPanel.FocusVelocity = 0f;

        rightPanel.CurrentFocus = 0f;
        rightPanel.FocusVelocity = 0f;

        hero.Group.alpha = 0f;
        hero.Rect.localPosition = new Vector3(0f, -0.025f, 0.015f);
        hero.Rect.localRotation = Quaternion.identity;
        hero.Rect.localScale = hero.BaseScale * 0.78f;

        ResetPanelForEntrance(leftPanel);
        ResetPanelForEntrance(rightPanel);
    }

    private static void ResetPanelForEntrance(PanelHandle panel)
    {
        panel.Group.alpha = 0f;
        panel.InterpretationGroup.alpha = 0f;
        panel.QuestionGroup.alpha = 0f;

        panel.Rect.localPosition = new Vector3(
            0.055f * panel.Direction,
            -0.012f,
            0.120f);

        panel.Rect.localRotation = Quaternion.Euler(
            0f,
            panel.NeutralYaw * 1.18f,
            0f);

        panel.Rect.localScale =
            panel.BaseScale * 0.46f;

        panel.Canvas.sortingOrder = 12;
    }

    private void UpdateFloatingPose(ARTrackedImage trackedImage)
    {
        Vector3 markerPosition = trackedImage.transform.position;
        Vector3 toCamera = arCamera.transform.position - markerPosition;

        if (toCamera.sqrMagnitude < 0.0001f)
            toCamera = -arCamera.transform.forward;

        Vector3 targetPosition =
            markerPosition +
            toCamera.normalized * cameraLift +
            Vector3.up * worldLift;

        contentRoot.position = Vector3.SmoothDamp(
            contentRoot.position,
            targetPosition,
            ref rootVelocity,
            0.08f,
            Mathf.Infinity,
            Time.deltaTime);

        Quaternion targetRotation =
            FaceCameraRotation(contentRoot.position);

        contentRoot.rotation = Quaternion.Slerp(
            contentRoot.rotation,
            targetRotation,
            1f - Mathf.Exp(-10f * Time.deltaTime));
    }

    private Quaternion FaceCameraRotation(Vector3 worldPosition)
    {
        Vector3 awayFromCamera =
            worldPosition - arCamera.transform.position;

        if (awayFromCamera.sqrMagnitude < 0.0001f)
            awayFromCamera = arCamera.transform.forward;

        return Quaternion.LookRotation(
            awayFromCamera.normalized,
            Vector3.up);
    }

    private void UpdateViewpoint(ARTrackedImage trackedImage)
    {
        Vector3 localCameraPosition =
            trackedImage.transform.InverseTransformPoint(
                arCamera.transform.position);

        float normalizedSide = Mathf.Clamp(
            localCameraPosition.x /
            Mathf.Max(0.01f, sideActivationDistance),
            -1f,
            1f);

        if (invertLeftRight)
            normalizedSide *= -1f;

        float elapsed = Time.time - entranceStartTime;
        float interactionGate = Smooth01(
            Mathf.InverseLerp(0.42f, 0.90f, elapsed));

        normalizedSide *= interactionGate;

        currentSide = Mathf.SmoothDamp(
            currentSide,
            normalizedSide,
            ref sideVelocity,
            0.16f,
            Mathf.Infinity,
            Time.deltaTime);

        float rightFocus = SmoothFocus(currentSide);
        float leftFocus = SmoothFocus(-currentSide);

        UpdatePanel(leftPanel, leftFocus, rightFocus);
        UpdatePanel(rightPanel, rightFocus, leftFocus);
        UpdateHero(currentSide, leftFocus, rightFocus);
    }

    private void UpdateHero(
        float side,
        float leftFocus,
        float rightFocus)
    {
        float elapsed = Time.time - entranceStartTime;
        float entrance = Smooth01(
            Mathf.InverseLerp(0f, 0.58f, elapsed));

        float strongestFocus =
            Mathf.Max(leftFocus, rightFocus);

        float neutralFloat =
            Mathf.Sin(Time.time * 1.35f) *
            0.0024f *
            (1f - strongestFocus);

        Vector3 enteredPosition = new Vector3(
            -0.045f * side,
            neutralFloat + 0.004f * strongestFocus,
            0.046f * strongestFocus);

        Vector3 targetPosition = Vector3.Lerp(
            new Vector3(0f, -0.025f, 0.015f),
            enteredPosition,
            entrance);

        Quaternion targetRotation = Quaternion.Euler(
            -2f * (1f - entrance),
            -10f * side,
            0f);

        float focusScale =
            Mathf.Lerp(1.03f, 0.76f, strongestFocus);

        float entranceScale =
            Mathf.Lerp(0.78f, 1f, entrance);

        Vector3 targetScale =
            hero.BaseScale *
            focusScale *
            entranceScale;

        float t = 1f - Mathf.Exp(-10f * Time.deltaTime);

        hero.Rect.localPosition = Vector3.Lerp(
            hero.Rect.localPosition,
            targetPosition,
            t);

        hero.Rect.localRotation = Quaternion.Slerp(
            hero.Rect.localRotation,
            targetRotation,
            t);

        hero.Rect.localScale = Vector3.Lerp(
            hero.Rect.localScale,
            targetScale,
            t);

        float targetAlpha =
            entrance *
            Mathf.Lerp(1f, 0.84f, strongestFocus);

        hero.Group.alpha = Mathf.Lerp(
            hero.Group.alpha,
            targetAlpha,
            t);

        Color targetAura =
            new Color(
                UprightAccent.r,
                UprightAccent.g,
                UprightAccent.b,
                Mathf.Lerp(0.08f, 0.17f, strongestFocus));

        if (leftFocus > rightFocus)
        {
            targetAura = new Color(
                ReversedAccent.r,
                ReversedAccent.g,
                ReversedAccent.b,
                Mathf.Lerp(0.08f, 0.18f, leftFocus));
        }

        hero.Aura.color = Color.Lerp(
            hero.Aura.color,
            targetAura,
            1f - Mathf.Exp(-8f * Time.deltaTime));
    }

    private void UpdatePanel(
        PanelHandle panel,
        float targetFocus,
        float oppositeFocus)
    {
        float elapsed = Time.time - entranceStartTime;

        float panelEntrance = Smooth01(
            Mathf.InverseLerp(0.28f, 0.88f, elapsed));

        panel.CurrentFocus = Mathf.SmoothDamp(
            panel.CurrentFocus,
            targetFocus,
            ref panel.FocusVelocity,
            0.14f,
            Mathf.Infinity,
            Time.deltaTime);

        float focus = panel.CurrentFocus;

        Vector3 desiredPosition =
            Vector3.Lerp(
                panel.NeutralPosition,
                panel.FocusedPosition,
                focus);

        desiredPosition = Vector3.Lerp(
            desiredPosition,
            panel.FarPosition,
            oppositeFocus);

        Vector3 entrancePosition = new Vector3(
            0.055f * panel.Direction,
            -0.012f,
            0.120f);

        Vector3 targetPosition = Vector3.Lerp(
            entrancePosition,
            desiredPosition,
            panelEntrance);

        float desiredScale =
            Mathf.Lerp(
                panel.NeutralScale,
                panel.FocusedScale,
                focus);

        desiredScale = Mathf.Lerp(
            desiredScale,
            panel.FarScale,
            oppositeFocus);

        float targetScale =
            Mathf.Lerp(0.46f, desiredScale, panelEntrance);

        float farYaw =
            panel.NeutralYaw * 1.18f;

        float desiredYaw =
            Mathf.Lerp(
                panel.NeutralYaw,
                0f,
                focus);

        desiredYaw = Mathf.Lerp(
            desiredYaw,
            farYaw,
            oppositeFocus);

        float startYaw =
            panel.NeutralYaw * 1.18f;

        float targetYaw =
            Mathf.Lerp(
                startYaw,
                desiredYaw,
                panelEntrance);

        float visibleAlpha =
            Mathf.Lerp(0.80f, 1f, focus) *
            Mathf.Lerp(1f, 0.34f, oppositeFocus);

        float targetAlpha =
            panelEntrance * visibleAlpha;

        float t =
            1f - Mathf.Exp(-11f * Time.deltaTime);

        panel.Rect.localPosition = Vector3.Lerp(
            panel.Rect.localPosition,
            targetPosition,
            t);

        panel.Rect.localRotation = Quaternion.Slerp(
            panel.Rect.localRotation,
            Quaternion.Euler(0f, targetYaw, 0f),
            t);

        panel.Rect.localScale = Vector3.Lerp(
            panel.Rect.localScale,
            panel.BaseScale * targetScale,
            t);

        panel.Group.alpha = Mathf.Lerp(
            panel.Group.alpha,
            targetAlpha,
            t);

        float interpretationReveal = Smooth01(
            Mathf.InverseLerp(0.28f, 0.66f, focus));

        float questionReveal = Smooth01(
            Mathf.InverseLerp(0.70f, 0.96f, focus));

        interpretationReveal *= panelEntrance;
        questionReveal *= panelEntrance;

        panel.InterpretationGroup.alpha = Mathf.Lerp(
            panel.InterpretationGroup.alpha,
            interpretationReveal,
            t);

        panel.QuestionGroup.alpha = Mathf.Lerp(
            panel.QuestionGroup.alpha,
            questionReveal,
            t);

        panel.InterpretationRect.anchoredPosition =
            Vector2.Lerp(
                new Vector2(0f, -10f),
                Vector2.zero,
                interpretationReveal);

        panel.QuestionRect.anchoredPosition =
            Vector2.Lerp(
                new Vector2(0f, -8f),
                Vector2.zero,
                questionReveal);

        if (focus > 0.42f)
            panel.Canvas.sortingOrder = 42;
        else if (oppositeFocus > 0.42f)
            panel.Canvas.sortingOrder = 10;
        else
            panel.Canvas.sortingOrder = 18;
    }

    private static float SmoothFocus(float sideValue)
    {
        float t =
            Mathf.InverseLerp(
                0.12f,
                0.90f,
                sideValue);

        return Mathf.SmoothStep(0f, 1f, t);
    }

    private static float Smooth01(float value)
    {
        return Mathf.SmoothStep(
            0f,
            1f,
            Mathf.Clamp01(value));
    }

    private static void SetRegion(
        RectTransform rect,
        float minX,
        float minY,
        float maxX,
        float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Text CreateText(
        Transform parent,
        string name,
        string value,
        int fontSize,
        FontStyle style,
        Color color,
        TextAnchor alignment)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));

        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.text = value;
        text.font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");

        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow =
            HorizontalWrapMode.Wrap;
        text.verticalOverflow =
            VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        return text;
    }

    private static void CreateFrameLine(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 size,
        Vector2 position,
        Color color)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        go.transform.SetParent(parent, false);

        RectTransform rect =
            go.GetComponent<RectTransform>();

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image line = go.GetComponent<Image>();
        line.color = color;
        line.raycastTarget = false;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
