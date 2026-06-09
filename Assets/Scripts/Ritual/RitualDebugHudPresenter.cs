using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RitualDebugHudPresenter : MonoBehaviour
{
    private enum DebugPage
    {
        Quick = 0,
        Deduction = 1
    }

    private static readonly Vector2 QuickPanelSize = new Vector2(160f, 140f);
    private static readonly Vector2 DeductionPanelSize = new Vector2(380f, 320f);
    private static readonly Color PanelBackgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.92f);
    private static readonly Color AccentColor = new Color(0.9f, 0.78f, 0.35f, 1f);
    private static readonly Color MutedColor = new Color(0.8f, 0.8f, 0.84f, 0.9f);
    private static readonly Color ExcludedColor = new Color(0.6f, 0.6f, 0.65f, 0.65f);

    [Header("Binding")]
    [SerializeField] private Player player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Vector2 screenOffset = new Vector2(20f, 12f);

    [Header("Catalogs")]
    [SerializeField] private TextAsset npcProblemsXml;
    [SerializeField] private TextAsset ritualSolutionsXml;

    [Header("Behaviour")]
    [SerializeField] private bool visibleByDefault = true;
    [SerializeField] private bool showAllActions = true;
    [SerializeField] private bool showAllItems = true;

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset customFont;

    private Canvas canvas;
    private RectTransform quickPanel;
    private RectTransform deductionPanel;
    private TMP_Text quickTitleText;
    private TMP_Text quickSummaryText;
    private TMP_Text quickActionsText;
    private TMP_Text quickItemsText;
    private TMP_Text deductionTitleText;
    private TMP_Text deductionSummaryText;
    private TMP_Text candidateText;
    private TMP_Text ritualText;
    private ScrollRect deductionScrollRect;
    private RectTransform deductionScrollContent;
    private RectTransform symptomButtonsRoot;
    private Button clearButton;

    private Sprite whiteSprite;
    private DebugPage currentPage = DebugPage.Quick;
    private bool isPanelOpen = true;
    private NPCProblemCatalog problemCatalog;
    private RitualSolutionCatalog solutionCatalog;
    private readonly List<NPCProblemDefinition> candidateProblems = new List<NPCProblemDefinition>();
    private readonly List<SymptomEntry> symptomEntries = new List<SymptomEntry>();
    private readonly HashSet<string> excludedSymptoms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly List<RitualActionType> actionTypes = new List<RitualActionType>();
    private readonly List<RitualItemType> itemTypes = new List<RitualItemType>();

    private sealed class SymptomEntry
    {
        public string Symptom;
        public Button Button;
        public TMP_Text Label;
    }

    private void Awake()
    {
        ResolvePlayer();
        ResolveCamera();
        CacheEnums();
        LoadCatalogs();
        BuildUiIfNeeded();
        SetVisible(visibleByDefault);
        SetPage(currentPage == DebugPage.Quick ? 0 : 1);
        RefreshAll();
    }

    private void OnEnable()
    {
        RefreshAll();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            SetPanelOpen(!isPanelOpen);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SetPage(currentPage == DebugPage.Quick ? 1 : 0);
        }

        if (player == null)
        {
            ResolvePlayer();
        }

        if (targetCamera == null)
        {
            ResolveCamera();
        }

        if (canvas == null || player == null || targetCamera == null)
        {
            return;
        }

        Vector3 screenPosition = targetCamera.WorldToScreenPoint(player.transform.position + worldOffset);
        if (screenPosition.z < 0f)
        {
            canvas.enabled = false;
            return;
        }

        canvas.enabled = true;
        if (currentPage == DebugPage.Quick && quickPanel != null)
        {
            quickPanel.position = new Vector3(screenPosition.x + screenOffset.x, screenPosition.y + screenOffset.y, 0f);
        }
        else if (deductionPanel != null)
        {
            deductionPanel.position = new Vector3(screenPosition.x + screenOffset.x, screenPosition.y + screenOffset.y, 0f);
        }

        RefreshQuickPage();
    }

    public void SetVisible(bool visible)
    {
        isPanelOpen = visible;

        if (canvas != null)
        {
            canvas.enabled = visible;
        }

        if (quickPanel != null)
        {
            quickPanel.gameObject.SetActive(visible && currentPage == DebugPage.Quick);
        }

        if (deductionPanel != null)
        {
            deductionPanel.gameObject.SetActive(visible && currentPage == DebugPage.Deduction);
        }
    }

    public void SetPanelOpen(bool open)
    {
        SetVisible(open);
    }

    public void SetPage(int pageIndex)
    {
        currentPage = pageIndex <= 0 ? DebugPage.Quick : DebugPage.Deduction;
        if (quickPanel != null)
        {
            quickPanel.gameObject.SetActive(isPanelOpen && currentPage == DebugPage.Quick);
        }

        if (deductionPanel != null)
        {
            deductionPanel.gameObject.SetActive(isPanelOpen && currentPage == DebugPage.Deduction);
        }

        RefreshAll();
    }

    [ContextMenu("Refresh Ritual HUD")]
    public void RefreshAll()
    {
        RefreshQuickPage();
        RefreshDeductionPage();
    }

    private void RefreshQuickPage()
    {
        if (quickTitleText == null || quickSummaryText == null || quickActionsText == null || quickItemsText == null || player == null)
        {
            return;
        }

        quickTitleText.text = "Ritual Debug";
        quickSummaryText.text =
            $"Current item: <color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>{player.SelectedRitualItem.GetDisplayName()}</color>\n" +
            $"Current action: <color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>{player.SelectedRitualAction.GetDisplayName()}</color>\n" +
            $"Actions: <color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>T</color> | Page: <color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>Tab</color> | Toggle: <color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>H</color>";

        quickActionsText.text = showAllActions ? BuildActionListText(player.SelectedRitualAction) : string.Empty;
        quickItemsText.text = showAllItems ? BuildItemListText(player.SelectedRitualItem) : string.Empty;
    }

    private void RefreshDeductionPage()
    {
        if (deductionTitleText == null || deductionSummaryText == null || candidateText == null || ritualText == null)
        {
            return;
        }

        RecalculateCandidates();

        deductionTitleText.text = "Paranormal Trace Deduction";
        deductionSummaryText.text =
            $"Excluded: {excludedSymptoms.Count}\n" +
            $"Candidates: {candidateProblems.Count}\n" +
            "Click a trace to cross it out.";

        candidateText.text = BuildCandidateText();
        ritualText.text = BuildRitualText();
        UpdateSymptomButtons();
    }

    private void BuildUiIfNeeded()
    {
        if (canvas != null)
        {
            return;
        }

        whiteSprite = CreateWhiteSprite();

        GameObject canvasObject = new GameObject("RitualDebugCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 1;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        quickPanel = CreatePanel(canvasObject.transform, QuickPanelSize, true);
        deductionPanel = CreatePanel(canvasObject.transform, DeductionPanelSize, false);

        BuildQuickPage(quickPanel);
        BuildDeductionPage(deductionPanel);
    }

    private void BuildQuickPage(Transform parent)
    {
        quickTitleText = CreateTextElement(parent, string.Empty, 11f, TextAlignmentOptions.Center);
        quickTitleText.fontStyle = FontStyles.Bold;
        quickTitleText.color = AccentColor;

        quickSummaryText = CreateTextElement(parent, string.Empty, 8f, TextAlignmentOptions.TopLeft);
        quickSummaryText.color = MutedColor;

        quickActionsText = CreateTextElement(parent, string.Empty, 7f, TextAlignmentOptions.TopLeft);
        quickActionsText.color = Color.white;

        quickItemsText = CreateTextElement(parent, string.Empty, 7f, TextAlignmentOptions.TopLeft);
        quickItemsText.color = Color.white;
    }

    private void BuildDeductionPage(Transform parent)
    {
        deductionTitleText = CreateTextElement(parent, string.Empty, 11f, TextAlignmentOptions.Center);
        deductionTitleText.fontStyle = FontStyles.Bold;
        deductionTitleText.color = AccentColor;

        deductionSummaryText = CreateTextElement(parent, string.Empty, 8f, TextAlignmentOptions.TopLeft);
        deductionSummaryText.color = MutedColor;

        TMP_Text tracesLabel = CreateTextElement(parent, "Paranormal traces:", 7f, TextAlignmentOptions.TopLeft);
        tracesLabel.color = Color.white;

        GameObject scrollObject = new GameObject("DeductionScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
        scrollObject.transform.SetParent(parent, false);

        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.sizeDelta = new Vector2(0f, 0f);

        LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
        scrollLayout.preferredHeight = 190f;
        scrollLayout.flexibleHeight = 1f;

        Image scrollBackground = scrollObject.GetComponent<Image>();
        scrollBackground.sprite = whiteSprite;
        scrollBackground.type = Image.Type.Sliced;
        scrollBackground.color = new Color(0.12f, 0.12f, 0.14f, 0.9f);

        deductionScrollRect = scrollObject.GetComponent<ScrollRect>();
        deductionScrollRect.horizontal = false;
        deductionScrollRect.vertical = true;
        deductionScrollRect.movementType = ScrollRect.MovementType.Clamped;
        deductionScrollRect.scrollSensitivity = 14f;

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        Mask viewportMask = viewportObject.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);

        deductionScrollContent = contentObject.GetComponent<RectTransform>();
        deductionScrollContent.anchorMin = new Vector2(0f, 1f);
        deductionScrollContent.anchorMax = new Vector2(1f, 1f);
        deductionScrollContent.pivot = new Vector2(0.5f, 1f);
        deductionScrollContent.anchoredPosition = Vector2.zero;
        deductionScrollContent.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(2, 2, 2, 2);
        contentLayout.spacing = 2f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;

        ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        deductionScrollRect.viewport = viewportRect;
        deductionScrollRect.content = deductionScrollContent;

        GameObject symptomContainerObject = new GameObject("Symptoms", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        symptomContainerObject.transform.SetParent(deductionScrollContent, false);

        symptomButtonsRoot = symptomContainerObject.GetComponent<RectTransform>();
        VerticalLayoutGroup symptomLayout = symptomContainerObject.GetComponent<VerticalLayoutGroup>();
        symptomLayout.padding = new RectOffset(0, 0, 0, 0);
        symptomLayout.spacing = 2f;
        symptomLayout.childAlignment = TextAnchor.UpperLeft;
        symptomLayout.childControlHeight = true;
        symptomLayout.childControlWidth = true;
        symptomLayout.childForceExpandHeight = false;
        symptomLayout.childForceExpandWidth = true;

        LayoutElement symptomContainerLayout = symptomContainerObject.GetComponent<LayoutElement>();
        symptomContainerLayout.preferredHeight = -1f;
        symptomContainerLayout.flexibleHeight = -1f;

        clearButton = CreateButton(symptomButtonsRoot, "Clear Exclusions", ClearExclusions);

        TMP_Text candidateLabel = CreateTextElement(deductionScrollContent, "Remaining candidates:", 7f, TextAlignmentOptions.TopLeft);
        candidateLabel.color = Color.white;

        candidateText = CreateTextElement(deductionScrollContent, string.Empty, 7f, TextAlignmentOptions.TopLeft);
        candidateText.color = Color.white;

        ritualText = CreateTextElement(deductionScrollContent, string.Empty, 7f, TextAlignmentOptions.TopLeft);
        ritualText.color = Color.white;

        if (deductionScrollRect != null)
        {
            deductionScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private RectTransform CreatePanel(Transform parent, Vector2 size, bool fitToContent)
    {
        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.sizeDelta = size;

        Image background = panelObject.GetComponent<Image>();
        background.sprite = whiteSprite;
        background.type = Image.Type.Sliced;
        background.color = PanelBackgroundColor;

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        if (fitToContent)
        {
            ContentSizeFitter fitter = panelObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        return rectTransform;
    }

    private TMP_Text CreateTextElement(Transform parent, string value, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.font = ResolveFontAsset();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        LayoutElement layout = textObject.GetComponent<LayoutElement>();
        layout.minHeight = Mathf.Max(8f, fontSize + 1f);

        return text;
    }

    private Button CreateButton(Transform parent, string label, Action onClick)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0f, 18f);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = whiteSprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.16f, 0.16f, 0.18f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());

        TMP_Text text = CreateTextElement(buttonObject.transform, label, 6.5f, TextAlignmentOptions.Center);
        text.color = Color.white;

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.minHeight = 18f;
        layout.preferredHeight = 18f;

        return button;
    }

    private void LoadCatalogs()
    {
        if (npcProblemsXml != null)
        {
            problemCatalog = NPCProblemsLoader.Load(npcProblemsXml);
        }
        else
        {
            NPCGenerator generator = FindObjectOfType<NPCGenerator>();
            problemCatalog = generator != null ? generator.ProblemCatalog : null;
        }

        if (ritualSolutionsXml != null)
        {
            solutionCatalog = RitualSolutionCatalog.CreateRuntimeFromXml(ritualSolutionsXml);
        }
        else
        {
            solutionCatalog = RitualSolutionCatalog.CreateRuntimeDefault();
        }
    }

    private void RecalculateCandidates()
    {
        candidateProblems.Clear();
        if (problemCatalog == null || problemCatalog.Problems == null)
        {
            return;
        }

        for (int i = 0; i < problemCatalog.Problems.Count; i++)
        {
            NPCProblemDefinition problem = problemCatalog.Problems[i];
            if (problem == null || string.IsNullOrWhiteSpace(problem.Name))
            {
                continue;
            }

            if (MatchesCurrentExclusions(problem))
            {
                candidateProblems.Add(problem);
            }
        }
    }

    private bool MatchesCurrentExclusions(NPCProblemDefinition problem)
    {
        if (problem == null || problem.Symptoms == null)
        {
            return false;
        }

        foreach (string excluded in excludedSymptoms)
        {
            if (string.IsNullOrWhiteSpace(excluded))
            {
                continue;
            }

            if (ContainsSymptom(problem, excluded))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsSymptom(NPCProblemDefinition problem, string symptom)
    {
        if (problem == null || string.IsNullOrWhiteSpace(symptom) || problem.Symptoms == null)
        {
            return false;
        }

        for (int i = 0; i < problem.Symptoms.Count; i++)
        {
            if (string.Equals(problem.Symptoms[i], symptom, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private string BuildCandidateText()
    {
        if (candidateProblems.Count == 0)
        {
            return "No candidates left.";
        }

        List<string> lines = new List<string>();
        for (int i = 0; i < candidateProblems.Count; i++)
        {
            NPCProblemDefinition problem = candidateProblems[i];
            if (problem == null)
            {
                continue;
            }

            string symptoms = problem != null && problem.Symptoms != null && problem.Symptoms.Count > 0
                ? string.Join(", ", problem.Symptoms)
                : "No symptoms";

            lines.Add($"{i + 1}. {problem.Name}");
            lines.Add($"   Symptoms: {symptoms}");
        }

        return string.Join("\n", lines);
    }

    private string BuildRitualText()
    {
        if (candidateProblems.Count == 0)
        {
            return "No compatible paranormal traces remain.";
        }

        if (candidateProblems.Count != 1)
        {
            return "Keep crossing out traces until one candidate remains.";
        }

        NPCProblemDefinition candidate = candidateProblems[0];
        if (candidate == null || solutionCatalog == null || !solutionCatalog.TryGetSolution(candidate.Name, out RitualSolutionDefinition solution))
        {
            return $"No ritual found for: {candidate?.Name ?? "unknown"}\nMap an alias in RitualSolutionCatalog.";
        }

        List<string> lines = new List<string>
        {
            $"Ritual: {solution.DisplayName ?? solution.ProblemName}",
            "Steps:"
        };

        if (solution.Steps == null || solution.Steps.Count == 0)
        {
            lines.Add("- No steps defined.");
            return string.Join("\n", lines);
        }

        for (int i = 0; i < solution.Steps.Count; i++)
        {
            RitualStepDefinition step = solution.Steps[i];
            if (step == null)
            {
                continue;
            }

            string stepLabel = !string.IsNullOrWhiteSpace(step.Title)
                ? step.Title
                : $"{step.Item.GetDisplayName()} + {step.Action.GetDisplayName()}";

            string description = string.IsNullOrWhiteSpace(step.Description) ? string.Empty : $" - {step.Description}";
            lines.Add($"{step.Index + 1}. {stepLabel}{description}");
        }

        return string.Join("\n", lines);
    }

    private void UpdateSymptomButtons()
    {
        if (symptomButtonsRoot == null)
        {
            return;
        }

        EnsureSymptomButtons();

        for (int i = 0; i < symptomEntries.Count; i++)
        {
            SymptomEntry entry = symptomEntries[i];
            if (entry == null || entry.Button == null || entry.Label == null)
            {
                continue;
            }

            bool excluded = excludedSymptoms.Contains(entry.Symptom);
            entry.Label.text = excluded ? $"<s>{entry.Symptom}</s>" : entry.Symptom;
            entry.Label.color = excluded ? ExcludedColor : Color.white;
            entry.Button.targetGraphic.color = excluded
                ? new Color(0.18f, 0.18f, 0.2f, 0.9f)
                : new Color(0.16f, 0.16f, 0.18f, 1f);
        }

        if (deductionScrollRect != null)
        {
            deductionScrollRect.verticalNormalizedPosition = Mathf.Clamp01(deductionScrollRect.verticalNormalizedPosition);
        }
    }

    private void EnsureSymptomButtons()
    {
        List<string> allSymptoms = CollectAllSymptoms();

        while (symptomEntries.Count < allSymptoms.Count)
        {
            int index = symptomEntries.Count;
            string symptom = allSymptoms[index];
            Button button = CreateButton(symptomButtonsRoot, symptom, () => ToggleSymptom(symptom));
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            symptomEntries.Add(new SymptomEntry
            {
                Symptom = symptom,
                Button = button,
                Label = label
            });
        }

        for (int i = 0; i < symptomEntries.Count; i++)
        {
            bool active = i < allSymptoms.Count;
            symptomEntries[i].Button.gameObject.SetActive(active);
            if (active)
            {
                symptomEntries[i].Symptom = allSymptoms[i];
            }
        }

    }

    private List<string> CollectAllSymptoms()
    {
        List<string> symptoms = new List<string>();
        if (problemCatalog == null || problemCatalog.Problems == null)
        {
            return symptoms;
        }

        for (int i = 0; i < problemCatalog.Problems.Count; i++)
        {
            NPCProblemDefinition problem = problemCatalog.Problems[i];
            if (problem == null || problem.Symptoms == null)
            {
                continue;
            }

            for (int j = 0; j < problem.Symptoms.Count; j++)
            {
                string symptom = problem.Symptoms[j];
                if (string.IsNullOrWhiteSpace(symptom))
                {
                    continue;
                }

                symptom = symptom.Trim();
                bool alreadyPresent = false;
                for (int k = 0; k < symptoms.Count; k++)
                {
                    if (string.Equals(symptoms[k], symptom, StringComparison.OrdinalIgnoreCase))
                    {
                        alreadyPresent = true;
                        break;
                    }
                }

                if (!alreadyPresent)
                {
                    symptoms.Add(symptom);
                }
            }
        }

        return symptoms;
    }

    private void ToggleSymptom(string symptom)
    {
        if (string.IsNullOrWhiteSpace(symptom))
        {
            return;
        }

        symptom = symptom.Trim();
        if (!excludedSymptoms.Add(symptom))
        {
            excludedSymptoms.Remove(symptom);
        }

        RefreshDeductionPage();
    }

    private void ClearExclusions()
    {
        excludedSymptoms.Clear();
        RefreshDeductionPage();
    }

    private TMP_FontAsset ResolveFontAsset()
    {
        if (customFont != null)
        {
            return customFont;
        }

        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;
        if (fontAsset != null)
        {
            return fontAsset;
        }

        return Resources.Load<TMP_FontAsset>("Fonts/PixelCodes/PixelCode-Bold SDF");
    }

    private Sprite CreateWhiteSprite()
    {
        Texture2D texture = Texture2D.whiteTexture;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void ResolvePlayer()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        }
    }

    private void CacheEnums()
    {
        actionTypes.Clear();
        actionTypes.AddRange((RitualActionType[])Enum.GetValues(typeof(RitualActionType)));
        itemTypes.Clear();
        itemTypes.AddRange((RitualItemType[])Enum.GetValues(typeof(RitualItemType)));
    }

    private string BuildActionListText(RitualActionType currentAction)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine("Actions:");

        for (int i = 0; i < actionTypes.Count; i++)
        {
            RitualActionType action = actionTypes[i];
            builder.Append(action == currentAction ? "> " : "  ");
            builder.Append(action.GetDisplayName());
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private string BuildItemListText(RitualItemType currentItem)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine("Items:");

        for (int i = 0; i < itemTypes.Count; i++)
        {
            RitualItemType item = itemTypes[i];
            builder.Append(item == currentItem ? "> " : "  ");
            builder.Append(item.GetDisplayName());
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}
