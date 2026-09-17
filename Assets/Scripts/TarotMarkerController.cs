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

    private static readonly Color Paper = new Color(0.93f, 0.90f, 0.83f, 0.98f);
    private static readonly Color Ink = new Color(0.11f, 0.10f, 0.09f, 1f);
    private static readonly Color Border = new Color(0.18f, 0.15f, 0.12f, 0.95f);
    private static readonly Color UprightAccent = new Color(0.29f, 0.57f, 0.76f, 1f);
    private static readonly Color ReversedAccent = new Color(0.67f, 0.30f, 0.22f, 1f);

    private sealed class PanelHandle
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public Vector3 NeutralPosition;
        public Vector3 FocusedPosition;
        public Vector3 FarPosition;
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
            if (trackedImage.trackingState == TrackingState.Tracking && trackedImage.referenceImage.name == markerName)
                return trackedImage;
        }
        return null;
    }

    private void EnsureContent(ARTrackedImage trackedImage)
    {
        if (contentRoot != null)
            return;

        contentRoot = new GameObject("Queen of Swords Perspective Carousel").transform;
        Vector3 toCamera = (arCamera.transform.position - trackedImage.transform.position).normalized;
        contentRoot.position = trackedImage.transform.position + toCamera * cameraLift + Vector3.up * worldLift;
        contentRoot.rotation = FaceCameraRotation(contentRoot.position);

        hero = CreateHeroCard();

        leftPanel = CreatePanel(
            "Reversed Plane",
            "R E V E R S E D",
            "COLDNESS",
            "Harsh Judgment\nIsolation",
            ReversedAccent,
            -1f);

        rightPanel = CreatePanel(
            "Upright Plane",
            "U P R I G H T",
            "CLARITY",
            "Independence\nTruth",
            UprightAccent,
            1f);

        ConfigurePanelGeometry(leftPanel, -1f);
        ConfigurePanelGeometry(rightPanel, 1f);
    }

    private HeroHandle CreateHeroCard()
    {
        Texture2D cardTexture = Resources.Load<Texture2D>(DisplayTextureResource);
        float aspect = cardTexture != null && cardTexture.width > 0 ? (float)cardTexture.height / cardTexture.width : 1.74f;

        const float width = 360f;
        float height = width * aspect;
        const float worldScale = 0.00036f;

        GameObject cardObject = new GameObject("Hero Queen of Swords", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
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

        GameObject auraObject = new GameObject("Aura", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        auraObject.transform.SetParent(cardObject.transform, false);
        RectTransform auraRect = auraObject.GetComponent<RectTransform>();
        auraRect.anchorMin = Vector2.zero;
        auraRect.anchorMax = Vector2.one;
        auraRect.offsetMin = new Vector2(-26f, -26f);
        auraRect.offsetMax = new Vector2(26f, 26f);
        Image aura = auraObject.GetComponent<Image>();
        aura.color = new Color(UprightAccent.r, UprightAccent.g, UprightAccent.b, 0.12f);
        aura.raycastTarget = false;

        GameObject shadowObject = new GameObject("Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shadowObject.transform.SetParent(cardObject.transform, false);
        RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = new Vector2(12f, -14f);
        shadowRect.offsetMax = new Vector2(12f, -14f);
        Image shadow = shadowObject.GetComponent<Image>();
        shadow.color = new Color(0.02f, 0.02f, 0.025f, 0.26f);
        shadow.raycastTarget = false;

        GameObject frameObject = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameObject.transform.SetParent(cardObject.transform, false);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        Stretch(frameRect);
        Image frame = frameObject.GetComponent<Image>();
        frame.color = new Color(0.95f, 0.92f, 0.84f, 1f);
        frame.raycastTarget = false;
        Outline outline = frameObject.AddComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(4f, -4f);

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
            Debug.LogWarning("QueenOfSwordsDisplay was not found in Assets/Resources. The editor helper should create it automatically from the marker image.");

        return new HeroHandle
        {
            Rect = rect,
            Group = group,
            Aura = aura,
            BaseScale = Vector3.one * worldScale
        };
    }

    private PanelHandle CreatePanel(string name, string headerText, string heroWord, string subWords, Color accentColor, float direction)
    {
        const float worldScale = 0.00030f;
        Vector2 canvasSize = new Vector2(360f, 470f);

        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
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
        group.alpha = 0.82f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject paperObject = new GameObject("Surface", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        paperObject.transform.SetParent(panelObject.transform, false);
        RectTransform paperRect = paperObject.GetComponent<RectTransform>();
        Stretch(paperRect);
        Image paper = paperObject.GetComponent<Image>();
        paper.color = Paper;
        paper.raycastTarget = false;
        Outline paperOutline = paperObject.AddComponent<Outline>();
        paperOutline.effectColor = Border;
        paperOutline.effectDistance = new Vector2(3f, -3f);

        CreateFrameLine(panelObject.transform, "Top", new Vector2(0.5f, 1f), new Vector2(canvasSize.x - 34f, 2f), new Vector2(0f, -18f));
        CreateFrameLine(panelObject.transform, "Bottom", new Vector2(0.5f, 0f), new Vector2(canvasSize.x - 34f, 2f), new Vector2(0f, 18f));

        Text header = CreateText(panelObject.transform, "Header", headerText, 24, FontStyle.Bold, accentColor, TextAnchor.MiddleCenter);
        header.rectTransform.anchorMin = new Vector2(0.08f, 0.76f);
        header.rectTransform.anchorMax = new Vector2(0.92f, 0.90f);
        header.rectTransform.offsetMin = Vector2.zero;
        header.rectTransform.offsetMax = Vector2.zero;

        GameObject ruleObject = new GameObject("Accent Rule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ruleObject.transform.SetParent(panelObject.transform, false);
        RectTransform ruleRect = ruleObject.GetComponent<RectTransform>();
        ruleRect.anchorMin = new Vector2(0.18f, 0.71f);
        ruleRect.anchorMax = new Vector2(0.82f, 0.71f);
        ruleRect.sizeDelta = new Vector2(0f, 4f);
        Image rule = ruleObject.GetComponent<Image>();
        rule.color = accentColor;
        rule.raycastTarget = false;

        Text heroWordText = CreateText(panelObject.transform, "Hero Word", heroWord, 40, FontStyle.Bold, Ink, TextAnchor.MiddleCenter);
        heroWordText.rectTransform.anchorMin = new Vector2(0.07f, 0.44f);
        heroWordText.rectTransform.anchorMax = new Vector2(0.93f, 0.66f);
        heroWordText.rectTransform.offsetMin = Vector2.zero;
        heroWordText.rectTransform.offsetMax = Vector2.zero;

        Text subText = CreateText(panelObject.transform, "Meanings", subWords, 29, FontStyle.Normal, Ink, TextAnchor.UpperCenter);
        subText.rectTransform.anchorMin = new Vector2(0.08f, 0.16f);
        subText.rectTransform.anchorMax = new Vector2(0.92f, 0.41f);
        subText.rectTransform.offsetMin = Vector2.zero;
        subText.rectTransform.offsetMax = Vector2.zero;
        subText.lineSpacing = 1.25f;

        return new PanelHandle
        {
            Rect = rect,
            Group = group,
            Direction = direction,
            CurrentFocus = 0f,
            FocusVelocity = 0f
        };
    }

    private static void ConfigurePanelGeometry(PanelHandle panel, float direction)
    {
        panel.NeutralPosition = new Vector3(0.145f * direction, 0f, -0.030f);
        panel.FocusedPosition = new Vector3(0.110f * direction, 0.008f, 0.062f);
        panel.FarPosition = new Vector3(0.185f * direction, -0.004f, -0.105f);
        panel.NeutralScale = 0.78f;
        panel.FocusedScale = 1.34f;
        panel.FarScale = 0.58f;
        panel.NeutralYaw = -74f * direction;
        panel.Rect.localPosition = panel.NeutralPosition;
        panel.Rect.localRotation = Quaternion.Euler(0f, panel.NeutralYaw, 0f);
        panel.Rect.localScale *= panel.NeutralScale;
    }

    private void UpdateFloatingPose(ARTrackedImage trackedImage)
    {
        Vector3 markerPosition = trackedImage.transform.position;
        Vector3 toCamera = arCamera.transform.position - markerPosition;
        if (toCamera.sqrMagnitude < 0.0001f)
            toCamera = -arCamera.transform.forward;

        Vector3 targetPosition = markerPosition + toCamera.normalized * cameraLift + Vector3.up * worldLift;
        contentRoot.position = Vector3.SmoothDamp(contentRoot.position, targetPosition, ref rootVelocity, 0.08f, Mathf.Infinity, Time.deltaTime);

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

        currentSide = Mathf.SmoothDamp(currentSide, normalizedSide, ref sideVelocity, 0.16f, Mathf.Infinity, Time.deltaTime);

        float rightFocus = SmoothFocus(currentSide);
        float leftFocus = SmoothFocus(-currentSide);

        UpdatePanel(leftPanel, leftFocus, rightFocus);
        UpdatePanel(rightPanel, rightFocus, leftFocus);
        UpdateHero(currentSide, leftFocus, rightFocus);
    }

    private void UpdateHero(float side, float leftFocus, float rightFocus)
    {
        float strongestFocus = Mathf.Max(leftFocus, rightFocus);
        Vector3 targetPosition = new Vector3(-0.050f * side, 0.004f * strongestFocus, -0.032f * strongestFocus);
        Quaternion targetRotation = Quaternion.Euler(0f, -10f * side, 0f);
        float scale = Mathf.Lerp(1.08f, 0.98f, strongestFocus);

        hero.Rect.localPosition = Vector3.Lerp(hero.Rect.localPosition, targetPosition, 1f - Mathf.Exp(-10f * Time.deltaTime));
        hero.Rect.localRotation = Quaternion.Slerp(hero.Rect.localRotation, targetRotation, 1f - Mathf.Exp(-10f * Time.deltaTime));
        hero.Rect.localScale = Vector3.Lerp(hero.Rect.localScale, hero.BaseScale * scale, 1f - Mathf.Exp(-10f * Time.deltaTime));
        hero.Group.alpha = Mathf.Lerp(hero.Group.alpha, Mathf.Lerp(1f, 0.90f, strongestFocus), 1f - Mathf.Exp(-10f * Time.deltaTime));

        Color targetAura = new Color(UprightAccent.r, UprightAccent.g, UprightAccent.b, 0.12f);
        if (leftFocus > rightFocus)
            targetAura = new Color(ReversedAccent.r, ReversedAccent.g, ReversedAccent.b, 0.20f);
        else if (rightFocus > leftFocus)
            targetAura = new Color(UprightAccent.r, UprightAccent.g, UprightAccent.b, 0.20f);

        hero.Aura.color = Color.Lerp(hero.Aura.color, targetAura, 1f - Mathf.Exp(-8f * Time.deltaTime));
    }

    private static void UpdatePanel(PanelHandle panel, float targetFocus, float oppositeFocus)
    {
        panel.CurrentFocus = Mathf.SmoothDamp(panel.CurrentFocus, targetFocus, ref panel.FocusVelocity, 0.14f, Mathf.Infinity, Time.deltaTime);
        float focus = panel.CurrentFocus;

        Vector3 targetPosition = Vector3.Lerp(panel.NeutralPosition, panel.FocusedPosition, focus);
        targetPosition = Vector3.Lerp(targetPosition, panel.FarPosition, oppositeFocus);

        float scale = Mathf.Lerp(panel.NeutralScale, panel.FocusedScale, focus);
        scale = Mathf.Lerp(scale, panel.FarScale, oppositeFocus);

        float farYaw = panel.NeutralYaw * 1.14f;
        float yaw = Mathf.Lerp(panel.NeutralYaw, 0f, focus);
        yaw = Mathf.Lerp(yaw, farYaw, oppositeFocus);

        float alpha = Mathf.Lerp(0.84f, 1f, focus) * Mathf.Lerp(1f, 0.40f, oppositeFocus);
        float t = 1f - Mathf.Exp(-11f * Time.deltaTime);

        panel.Rect.localPosition = Vector3.Lerp(panel.Rect.localPosition, targetPosition, t);
        panel.Rect.localRotation = Quaternion.Slerp(panel.Rect.localRotation, Quaternion.Euler(0f, yaw, 0f), t);
        panel.Rect.localScale = Vector3.Lerp(panel.Rect.localScale, Vector3.one * (0.00030f * scale), t);
        panel.Group.alpha = Mathf.Lerp(panel.Group.alpha, alpha, t);
    }

    private static float SmoothFocus(float sideValue)
    {
        float t = Mathf.InverseLerp(0.12f, 0.90f, sideValue);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(Transform parent, string name, string value, int fontSize, FontStyle style, Color color, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void CreateFrameLine(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 position)
    {
        Image line = CreateImage(parent, name, Border);
        RectTransform rect = line.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
