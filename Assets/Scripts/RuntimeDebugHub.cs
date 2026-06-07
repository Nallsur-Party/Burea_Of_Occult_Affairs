using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RuntimeDebugHub : MonoBehaviour
{
    private const string DebugEnabledPrefsKey = "RuntimeDebugHub.DebugEnabled";
    private static readonly Vector2 PanelSize = new Vector2(320f, 320f);
    private static readonly Color PanelBackgroundColor = new Color(0.08f, 0.08f, 0.09f, 0.92f);
    private static readonly Color ButtonColor = new Color(0.18f, 0.18f, 0.2f, 1f);
    private static readonly Color ButtonDisabledColor = new Color(0.11f, 0.11f, 0.12f, 0.85f);

    private static RuntimeDebugHub instance;

    [SerializeField] private bool debugEnabled = true;
    [SerializeField] private bool startPanelVisible = false;

    private TMPPageTypewriter typewriter;
    private NPCSpawner npcSpawner;
    private NPCQueueManager npcQueueManager;
    private NewsTextMeshProPresenter newsPresenter;

    private Canvas debugCanvas;
    private RectTransform panelRoot;
    private bool isPanelVisible;
    private Button debugToggleButton;
    private TMP_Text debugToggleLabel;
    private TMP_Text statusLabel;
    private Button typewriterActivateButton;
    private Button typewriterDeactivateButton;
    private Button spawnNpcButton;
    private Button pinLatestNpcToTvButton;
    private Button exitZButton;
    private Button exitNButton;
    private readonly List<Button> actionButtons = new List<Button>();
    private Sprite whiteSprite;

    public static RuntimeDebugHub Instance => instance;
    public bool IsDebugEnabled => debugEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject hubObject = new GameObject(nameof(RuntimeDebugHub));
        hubObject.AddComponent<RuntimeDebugHub>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        debugEnabled = LoadDebugEnabled();

        BuildUiIfNeeded();
        BindSceneTargets();
        SetPanelVisible(startPanelVisible);
        RefreshUiState();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        BindSceneTargets();
        RefreshUiState();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private bool LoadDebugEnabled()
    {
        if (!PlayerPrefs.HasKey(DebugEnabledPrefsKey))
        {
            return debugEnabled;
        }

        return PlayerPrefs.GetInt(DebugEnabledPrefsKey, debugEnabled ? 1 : 0) != 0;
    }

    private void SaveDebugEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(DebugEnabledPrefsKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void Update()
    {
        if (!debugEnabled)
        {
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                TogglePanelVisibility();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Insert))
        {
            TogglePanelVisibility();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            SpawnNpc();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            SendNpcToExit("Z");
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            SendNpcToExit("N");
        }
    }

    public void ToggleDebugEnabled()
    {
        SetDebugEnabled(!debugEnabled);
    }

    public void TogglePanelVisibility()
    {
        SetPanelVisible(!isPanelVisible);
    }

    public void SetDebugEnabled(bool enabled)
    {
        if (debugEnabled == enabled)
        {
            RefreshUiState();
            return;
        }

        debugEnabled = enabled;
        SaveDebugEnabled(enabled);
        RefreshUiState();
    }

    public void SetPanelVisible(bool visible)
    {
        isPanelVisible = visible;

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(visible);
        }
    }

    public void ActivateTypewriter()
    {
        if (!CanUseTypewriter())
        {
            return;
        }

        typewriter.Activate();
    }

    public void DeactivateTypewriter()
    {
        if (!CanUseTypewriter())
        {
            return;
        }

        typewriter.Deactivate();
    }

    public void SpawnNpc()
    {
        if (!CanUseNpcDebug())
        {
            return;
        }

        npcSpawner.SpawnNPC();
    }

    public void PinLatestArchivedNpcToTv()
    {
        if (!CanUseNewsDebug())
        {
            return;
        }

        string reason;
        if (!newsPresenter.PinLatestArchivedNpcToTv(out reason))
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                Debug.LogWarning($"RuntimeDebugHub could not pin the latest archived NPC to TV: {reason}", this);
            }

            RefreshUiState();
            return;
        }

        Debug.Log("RuntimeDebugHub | Pinned the latest archived NPC to the TV.", this);
        RefreshUiState();
    }

    public void SendNpcToExit(string exitName)
    {
        if (!CanUseNpcDebug())
        {
            return;
        }

        if (npcQueueManager == null)
        {
            Debug.LogWarning("RuntimeDebugHub could not find an NPCQueueManager in the current scene.", this);
            return;
        }

        NpcOrderVisitor npcToSend = npcQueueManager.GetNextWaitingNPC();
        if (npcToSend == null)
        {
            Debug.Log($"RuntimeDebugHub | No NPC waiting at counter to send to exit {exitName}.", this);
            return;
        }

        npcToSend.LeaveThroughExitByName(exitName);
        Debug.Log($"RuntimeDebugHub | Sending NPC {npcToSend.gameObject.name} to exit {exitName}.", this);
    }

    public void RebindSceneTargets()
    {
        BindSceneTargets();
        RefreshUiState();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        BindSceneTargets();
        RefreshUiState();
    }

    private void BuildUiIfNeeded()
    {
        if (debugCanvas != null)
        {
            return;
        }

        whiteSprite = CreateWhiteSprite();

        GameObject canvasObject = new GameObject("RuntimeDebugCanvas");
        canvasObject.transform.SetParent(transform, false);

        debugCanvas = canvasObject.AddComponent<Canvas>();
        debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        debugCanvas.sortingOrder = short.MaxValue;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        panelRoot = CreatePanel(canvasObject.transform);
        CreateTitle(panelRoot, "Runtime Debug");
        statusLabel = CreateStatusLabel(panelRoot);

        debugToggleButton = CreateButton(
            panelRoot,
            "Debug: ON",
            ToggleDebugEnabled
        );
        debugToggleLabel = debugToggleButton.GetComponentInChildren<TMP_Text>(true);

        typewriterActivateButton = CreateButton(panelRoot, "Typewriter: Activate", ActivateTypewriter);
        typewriterDeactivateButton = CreateButton(panelRoot, "Typewriter: Deactivate", DeactivateTypewriter);
        spawnNpcButton = CreateButton(panelRoot, "NPC: Spawn", SpawnNpc);
        pinLatestNpcToTvButton = CreateButton(panelRoot, "TV: Pin Latest NPC", PinLatestArchivedNpcToTv);
        exitZButton = CreateButton(panelRoot, "NPC: Exit Z", () => SendNpcToExit("Z"));
        exitNButton = CreateButton(panelRoot, "NPC: Exit N", () => SendNpcToExit("N"));

        actionButtons.Add(typewriterActivateButton);
        actionButtons.Add(typewriterDeactivateButton);
        actionButtons.Add(spawnNpcButton);
        actionButtons.Add(pinLatestNpcToTvButton);
        actionButtons.Add(exitZButton);
        actionButtons.Add(exitNButton);

        SetPanelVisible(isPanelVisible);
    }

    private RectTransform CreatePanel(Transform parent)
    {
        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(16f, -16f);
        rectTransform.sizeDelta = PanelSize;

        Image background = panelObject.GetComponent<Image>();
        background.sprite = whiteSprite;
        background.type = Image.Type.Sliced;
        background.color = PanelBackgroundColor;

        VerticalLayoutGroup layoutGroup = panelObject.GetComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.spacing = 6f;
        layoutGroup.padding = new RectOffset(12, 12, 12, 12);

        ContentSizeFitter fitter = panelObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rectTransform;
    }

    private void CreateTitle(Transform parent, string titleText)
    {
        TMP_Text title = CreateTextElement(parent, titleText, 22f, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.95f, 0.92f, 0.75f, 1f);

        LayoutElement layoutElement = title.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 28f;
    }

    private TMP_Text CreateStatusLabel(Transform parent)
    {
        TMP_Text label = CreateTextElement(parent, string.Empty, 15f, TextAlignmentOptions.TopLeft);
        label.color = new Color(0.82f, 0.82f, 0.84f, 1f);

        LayoutElement layoutElement = label.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 42f;

        return label;
    }

    private Button CreateButton(Transform parent, string label, UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0f, 36f);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = whiteSprite;
        image.type = Image.Type.Sliced;
        image.color = ButtonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        TMP_Text text = CreateTextElement(buttonObject.transform, label, 16f, TextAlignmentOptions.Center);
        text.color = Color.white;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.minHeight = 36f;
        layoutElement.preferredHeight = 36f;

        return button;
    }

    private TMP_Text CreateTextElement(Transform parent, string textValue, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = textValue;
        text.font = ResolveFontAsset();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        return text;
    }

    private TMP_FontAsset ResolveFontAsset()
    {
        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;
        if (fontAsset != null)
        {
            return fontAsset;
        }

        fontAsset = Resources.Load<TMP_FontAsset>("Fonts/PixelCodes/PixelCode-Bold SDF");
        if (fontAsset != null)
        {
            return fontAsset;
        }

        Debug.LogWarning("RuntimeDebugHub could not resolve a TMP font asset for the debug UI.", this);
        return null;
    }

    private void BindSceneTargets()
    {
        typewriter = FindSceneComponentIncludingInactive<TMPPageTypewriter>();
        npcSpawner = FindSceneComponentIncludingInactive<NPCSpawner>();
        npcQueueManager = FindSceneComponentIncludingInactive<NPCQueueManager>();
        newsPresenter = FindSceneComponentIncludingInactive<NewsTextMeshProPresenter>();
    }

    private static T FindSceneComponentIncludingInactive<T>() where T : Component
    {
        T[] candidates = Resources.FindObjectsOfTypeAll<T>();
        T firstLoadedCandidate = null;
        T firstCandidate = null;

        for (int i = 0; i < candidates.Length; i++)
        {
            T candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            GameObject candidateObject = candidate.gameObject;
            if (candidateObject == null || !candidateObject.scene.IsValid() || !candidateObject.scene.isLoaded)
            {
                continue;
            }

            if (firstCandidate == null)
            {
                firstCandidate = candidate;
            }

            Behaviour behaviour = candidate as Behaviour;
            if (behaviour != null && behaviour.isActiveAndEnabled)
            {
                return candidate;
            }

            if (firstLoadedCandidate == null)
            {
                firstLoadedCandidate = candidate;
            }
        }

        return firstLoadedCandidate != null ? firstLoadedCandidate : firstCandidate;
    }

    private void RefreshUiState()
    {
        if (debugToggleLabel != null)
        {
            debugToggleLabel.text = debugEnabled ? "Debug: ON" : "Debug: OFF";
        }

        if (debugToggleButton != null)
        {
            debugToggleButton.interactable = true;
            SetButtonVisual(debugToggleButton, debugEnabled);
        }

        bool typewriterAvailable = typewriter != null;
        bool npcDebugAvailable = npcSpawner != null && npcQueueManager != null;
        bool newsDebugAvailable = newsPresenter != null;

        SetActionButtonState(typewriterActivateButton, typewriterAvailable);
        SetActionButtonState(typewriterDeactivateButton, typewriterAvailable);
        SetActionButtonState(spawnNpcButton, npcDebugAvailable);
        SetActionButtonState(pinLatestNpcToTvButton, newsDebugAvailable);
        SetActionButtonState(exitZButton, npcDebugAvailable);
        SetActionButtonState(exitNButton, npcDebugAvailable);

        if (statusLabel != null)
        {
            statusLabel.text = BuildStatusText(typewriterAvailable, npcDebugAvailable, newsDebugAvailable);
        }
    }

    private string BuildStatusText(bool typewriterAvailable, bool npcDebugAvailable, bool newsDebugAvailable)
    {
        string debugState = debugEnabled ? "ON" : "OFF";
        string typewriterState = typewriterAvailable ? "bound" : "missing";
        string npcState = npcDebugAvailable ? "ready" : "missing";
        string newsState = newsDebugAvailable ? "ready" : "missing";
        return $"State: {debugState}\nTypewriter: {typewriterState}\nNPC debug: {npcState}\nTV debug: {newsState}";
    }

    private void SetActionButtonState(Button button, bool available)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = debugEnabled && available;
        SetButtonVisual(button, debugEnabled && available);
    }

    private void SetButtonVisual(Button button, bool enabled)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
        }

        if (image == null)
        {
            return;
        }

        image.color = enabled ? ButtonColor : ButtonDisabledColor;
    }

    private bool CanUseTypewriter()
    {
        if (!debugEnabled)
        {
            return false;
        }

        if (typewriter == null)
        {
            Debug.LogWarning("RuntimeDebugHub could not find a TMPPageTypewriter in the current scene.", this);
            RefreshUiState();
            return false;
        }

        return true;
    }

    private bool CanUseNpcDebug()
    {
        if (!debugEnabled)
        {
            return false;
        }

        if (npcSpawner == null || npcQueueManager == null)
        {
            Debug.LogWarning("RuntimeDebugHub could not find NPC debug targets in the current scene.", this);
            RefreshUiState();
            return false;
        }

        return true;
    }

    private bool CanUseNewsDebug()
    {
        if (!debugEnabled)
        {
            return false;
        }

        if (newsPresenter == null)
        {
            Debug.LogWarning("RuntimeDebugHub could not find a NewsTextMeshProPresenter in the current scene.", this);
            RefreshUiState();
            return false;
        }

        return true;
    }

    private Sprite CreateWhiteSprite()
    {
        Texture2D texture = Texture2D.whiteTexture;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
