using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DiarySymptomCategoryPanel : MonoBehaviour
{
    private static readonly Dictionary<TextAsset, NPCSymptomCategoryCatalog> CachedSymptomCategoryCatalogs = new Dictionary<TextAsset, NPCSymptomCategoryCatalog>();
    private static readonly Dictionary<TextAsset, NPCProblemCatalog> CachedProblemCatalogs = new Dictionary<TextAsset, NPCProblemCatalog>();
    private static readonly Dictionary<TextAsset, Dictionary<string, string>> CachedSymptomTextMaps = new Dictionary<TextAsset, Dictionary<string, string>>();

    [SerializeField] private TextAsset symptomCategoriesXml;
    [SerializeField] private TextAsset npcProblemsXml;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private TMP_Text observedSymptomsText;
    [SerializeField] private TMP_Text problemsOutputText;
    [SerializeField] private TMP_Text possibleSymptomsOutputText;
    [SerializeField] private string targetLabelName = "Categoria";
    [SerializeField] private string targetSymptomLabelName = "symptomes";
    [SerializeField] private bool loadOnAwake = true;

    private NPCSymptomCategoryCatalog catalog;
    private NPCProblemCatalog problemCatalog;
    private Dictionary<string, string> symptomTextById = new Dictionary<string, string>();
    private Dictionary<Button, UnityAction> symptomButtonHandlers = new Dictionary<Button, UnityAction>();
    private readonly List<ProblemRowEntry> problemRows = new List<ProblemRowEntry>();
    private string lastObservedSymptomsText = string.Empty;

    private sealed class CategoryEntry
    {
        public TMP_Text CategoryLabel;
        public Transform EntryRoot;
        public readonly List<TMP_Text> SymptomLabels = new List<TMP_Text>();
    }

    private sealed class ProblemRowEntry
    {
        public Transform RowRoot;
        public TMP_Text ProblemLabel;
        public TMP_Text KnownSymptomsLabel;
    }

    private void Awake()
    {
        if (loadOnAwake)
        {
            RefreshCategoryButtons();
        }
    }

    [ContextMenu("Refresh Category Buttons")]
    public void RefreshCategoryButtons()
    {
        if (symptomCategoriesXml == null)
        {
            Debug.LogWarning($"{nameof(DiarySymptomCategoryPanel)} on {name} has no categories XML assigned.", this);
            return;
        }

        if (buttonContainer == null)
        {
            buttonContainer = transform;
        }

        catalog = GetOrLoadSymptomCategoryCatalog(symptomCategoriesXml);
        symptomTextById = GetOrLoadSymptomTextMap(npcProblemsXml);

        if (npcProblemsXml != null)
        {
            problemCatalog = GetOrLoadProblemCatalog(npcProblemsXml);
        }
        else
        {
            problemCatalog = null;
        }

        ApplyCategoryTexts();
        RefreshProblemCandidates();
    }

    private void Update()
    {
        if (observedSymptomsText == null)
        {
            return;
        }

        string currentObservedSymptomsText = observedSymptomsText.text ?? string.Empty;
        if (string.Equals(currentObservedSymptomsText, lastObservedSymptomsText))
        {
            return;
        }

        RefreshProblemCandidates();
    }

    private void ApplyCategoryTexts()
    {
        if (catalog == null || buttonContainer == null)
        {
            return;
        }

        List<CategoryEntry> entries = new List<CategoryEntry>();
        CollectCategoryEntries(buttonContainer, entries);

        int entryCount = entries.Count;
        int categoryCount = catalog.Categories.Count;

        for (int i = 0; i < entryCount; i++)
        {
            CategoryEntry entry = entries[i];
            if (entry == null || entry.CategoryLabel == null)
            {
                continue;
            }

            bool hasCategory = i < categoryCount;
            entry.CategoryLabel.gameObject.SetActive(hasCategory);

            if (!hasCategory)
            {
                SetSymptomButtons(entry, new List<string>());
                continue;
            }

            NPCSymptomCategoryDefinition category = catalog.Categories[i];
            entry.CategoryLabel.text = category.DisplayName;
            SetSymptomButtons(entry, BuildSymptomTexts(category));
        }
    }

    [ContextMenu("Refresh Problem Candidates")]
    public void RefreshProblemCandidates()
    {
        if (problemsOutputText == null)
        {
            return;
        }

        if (problemCatalog == null)
        {
            if (npcProblemsXml == null)
            {
                problemsOutputText.text = string.Empty;
                if (possibleSymptomsOutputText != null)
                {
                    possibleSymptomsOutputText.text = string.Empty;
                }
                lastObservedSymptomsText = observedSymptomsText != null ? observedSymptomsText.text ?? string.Empty : string.Empty;
                return;
            }

            problemCatalog = GetOrLoadProblemCatalog(npcProblemsXml);
        }

        List<string> observedSymptoms = ParseObservedSymptoms();
        lastObservedSymptomsText = observedSymptomsText != null ? observedSymptomsText.text ?? string.Empty : string.Empty;
        if (TryRefreshProblemRows(observedSymptoms))
        {
            return;
        }

        problemsOutputText.text = BuildProblemsOutput(observedSymptoms);
        if (possibleSymptomsOutputText != null)
        {
            possibleSymptomsOutputText.text = BuildPossibleSymptomsOutput(observedSymptoms);
        }
    }

    private string BuildProblemsOutput(List<string> observedSymptoms)
    {
        if (problemCatalog == null || problemCatalog.Problems.Count == 0)
        {
            return string.Empty;
        }

        List<string> lines = new List<string>();

        for (int i = 0; i < problemCatalog.Problems.Count; i++)
        {
            NPCProblemDefinition problem = problemCatalog.Problems[i];
            if (problem == null || string.IsNullOrWhiteSpace(problem.Name))
            {
                continue;
            }

            bool isCompatible = IsProblemCompatible(problem, observedSymptoms);
            string line = problem.Name;

            if (!isCompatible)
            {
                line = $"<s>{line}</s>";
            }

            lines.Add(line);
        }

        return string.Join("\n", lines);
    }

    private string BuildPossibleSymptomsOutput(List<string> observedSymptoms)
    {
        if (problemCatalog == null || problemCatalog.Problems.Count == 0)
        {
            return string.Empty;
        }

        List<string> lines = new List<string>();

        for (int i = 0; i < problemCatalog.Problems.Count; i++)
        {
            NPCProblemDefinition problem = problemCatalog.Problems[i];
            if (problem == null)
            {
                continue;
            }

            bool isCompatible = IsProblemCompatible(problem, observedSymptoms);
            string symptomLine = problem.Symptoms != null && problem.Symptoms.Count > 0
                ? string.Join(", ", problem.Symptoms)
                : string.Empty;

            if (!isCompatible)
            {
                symptomLine = $"<s>{symptomLine}</s>";
            }

            lines.Add(symptomLine);
        }

        return string.Join("\n", lines);
    }

    private bool TryRefreshProblemRows(List<string> observedSymptoms)
    {
        ProblemRowEntry templateRow = GetProblemRowTemplate();
        if (templateRow == null || templateRow.RowRoot == null || templateRow.ProblemLabel == null || templateRow.KnownSymptomsLabel == null)
        {
            return false;
        }

        Transform rowsContainer = templateRow.RowRoot.parent;
        if (rowsContainer == null || problemCatalog == null)
        {
            return false;
        }

        EnsureProblemRowCount(rowsContainer, templateRow, problemCatalog.Problems.Count);

        for (int i = 0; i < problemRows.Count; i++)
        {
            ProblemRowEntry row = problemRows[i];
            if (row == null || row.RowRoot == null || row.ProblemLabel == null || row.KnownSymptomsLabel == null)
            {
                continue;
            }

            bool hasProblem = i < problemCatalog.Problems.Count;
            row.RowRoot.gameObject.SetActive(hasProblem);

            if (!hasProblem)
            {
                continue;
            }

            NPCProblemDefinition problem = problemCatalog.Problems[i];
            bool isCompatible = IsProblemCompatible(problem, observedSymptoms);
            string problemText = problem != null ? problem.Name : string.Empty;
            string symptomText = problem != null && problem.Symptoms != null
                ? string.Join(", ", problem.Symptoms)
                : string.Empty;

            row.ProblemLabel.text = isCompatible ? problemText : $"<s>{problemText}</s>";
            row.KnownSymptomsLabel.text = isCompatible ? symptomText : $"<s>{symptomText}</s>";
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rowsContainer as RectTransform);
        return true;
    }

    private ProblemRowEntry GetProblemRowTemplate()
    {
        if (problemRows.Count > 0 && problemRows[0] != null)
        {
            return problemRows[0];
        }

        if (problemsOutputText == null || possibleSymptomsOutputText == null)
        {
            return null;
        }

        Transform rowRoot = problemsOutputText.transform.parent;
        if (rowRoot == null || rowRoot != possibleSymptomsOutputText.transform.parent)
        {
            return null;
        }

        TMP_Text problemLabel = FindTextByName(rowRoot, "Problems") ?? problemsOutputText;
        TMP_Text knownSymptomsLabel = FindTextByName(rowRoot, "KnownSymptoms") ?? possibleSymptomsOutputText;

        ProblemRowEntry templateRow = new ProblemRowEntry
        {
            RowRoot = rowRoot,
            ProblemLabel = problemLabel,
            KnownSymptomsLabel = knownSymptomsLabel
        };

        problemRows.Add(templateRow);
        return templateRow;
    }

    private void EnsureProblemRowCount(Transform rowsContainer, ProblemRowEntry templateRow, int requiredCount)
    {
        if (rowsContainer == null || templateRow == null)
        {
            return;
        }

        while (problemRows.Count < requiredCount)
        {
            Transform clone = Instantiate(templateRow.RowRoot, rowsContainer);
            clone.SetAsLastSibling();
            clone.gameObject.SetActive(true);

            ProblemRowEntry clonedRow = new ProblemRowEntry
            {
                RowRoot = clone,
                ProblemLabel = FindTextByName(clone, "Problems"),
                KnownSymptomsLabel = FindTextByName(clone, "KnownSymptoms")
            };

            if (clonedRow.ProblemLabel == null || clonedRow.KnownSymptomsLabel == null)
            {
                Destroy(clone.gameObject);
                break;
            }

            problemRows.Add(clonedRow);
        }
    }

    private bool IsProblemCompatible(NPCProblemDefinition problem, List<string> observedSymptoms)
    {
        if (problem == null || observedSymptoms == null || observedSymptoms.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < observedSymptoms.Count; i++)
        {
            string observedSymptom = observedSymptoms[i];
            if (string.IsNullOrWhiteSpace(observedSymptom))
            {
                continue;
            }

            bool matchesObservedSymptom = false;

            for (int symptomIndex = 0; symptomIndex < problem.Symptoms.Count; symptomIndex++)
            {
                string problemSymptom = problem.Symptoms[symptomIndex];
                if (string.Equals(problemSymptom, observedSymptom, System.StringComparison.OrdinalIgnoreCase))
                {
                    matchesObservedSymptom = true;
                    break;
                }
            }

            if (!matchesObservedSymptom)
            {
                return false;
            }
        }

        return true;
    }

    private List<string> ParseObservedSymptoms()
    {
        List<string> observedSymptoms = new List<string>();

        if (observedSymptomsText == null || string.IsNullOrWhiteSpace(observedSymptomsText.text))
        {
            return observedSymptoms;
        }

        string[] rawEntries = observedSymptomsText.text.Split(',');
        for (int i = 0; i < rawEntries.Length; i++)
        {
            string normalizedEntry = rawEntries[i]?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedEntry) || observedSymptoms.Contains(normalizedEntry))
            {
                continue;
            }

            observedSymptoms.Add(normalizedEntry);
        }

        return observedSymptoms;
    }

    private List<string> BuildSymptomTexts(NPCSymptomCategoryDefinition category)
    {
        List<string> texts = new List<string>();

        if (category == null || category.SymptomIds == null)
        {
            return texts;
        }

        for (int i = 0; i < category.SymptomIds.Count; i++)
        {
            string symptomId = category.SymptomIds[i];
            if (string.IsNullOrWhiteSpace(symptomId))
            {
                continue;
            }

            if (symptomTextById.TryGetValue(symptomId, out string symptomText) && !string.IsNullOrWhiteSpace(symptomText))
            {
                texts.Add(symptomText);
            }
        }

        return texts;
    }

    private void SetSymptomButtons(CategoryEntry entry, List<string> symptomTexts)
    {
        if (entry == null || entry.EntryRoot == null)
        {
            return;
        }

        List<TMP_Text> symptomLabels = entry.SymptomLabels;
        if (symptomLabels.Count == 0)
        {
            CollectTargetTexts(entry.EntryRoot, targetSymptomLabelName, symptomLabels);
        }

        if (symptomLabels.Count == 0)
        {
            return;
        }

        EnsureSymptomButtonCount(entry.EntryRoot, symptomLabels, symptomTexts.Count);

        for (int i = 0; i < symptomLabels.Count; i++)
        {
            TMP_Text label = symptomLabels[i];
            if (label == null)
            {
                continue;
            }

            bool hasSymptom = i < symptomTexts.Count;
            if (hasSymptom)
            {
                label.text = symptomTexts[i];
            }

            BindSymptomButton(label, hasSymptom);
        }
    }

    private void BindSymptomButton(TMP_Text label, bool hasSymptom)
    {
        Transform buttonRoot = GetButtonRoot(label != null ? label.transform : null);
        Button button = buttonRoot != null ? buttonRoot.GetComponent<Button>() : null;

        if (button == null)
        {
            return;
        }

        if (symptomButtonHandlers.TryGetValue(button, out UnityAction existingHandler))
        {
            button.onClick.RemoveListener(existingHandler);
            symptomButtonHandlers.Remove(button);
        }

        if (!hasSymptom)
        {
            return;
        }

        UnityAction handler = () => AddObservedSymptom(label.text);
        symptomButtonHandlers[button] = handler;
        button.onClick.AddListener(handler);
    }

    public void AddObservedSymptom(string symptomText)
    {
        if (observedSymptomsText == null || string.IsNullOrWhiteSpace(symptomText))
        {
            return;
        }

        List<string> observedSymptoms = ParseObservedSymptoms();
        string normalizedSymptomText = symptomText.Trim();

        for (int i = 0; i < observedSymptoms.Count; i++)
        {
            if (string.Equals(observedSymptoms[i], normalizedSymptomText, System.StringComparison.OrdinalIgnoreCase))
            {
                observedSymptoms.RemoveAt(i);
                observedSymptomsText.text = string.Join(", ", observedSymptoms);
                RefreshProblemCandidates();
                return;
            }
        }

        observedSymptoms.Add(normalizedSymptomText);
        observedSymptomsText.text = string.Join(", ", observedSymptoms);
        RefreshProblemCandidates();
    }

    public void ClearObservedSymptoms()
    {
        if (observedSymptomsText == null)
        {
            return;
        }

        observedSymptomsText.text = string.Empty;
        RefreshProblemCandidates();
    }

    private void EnsureSymptomButtonCount(Transform entryRoot, List<TMP_Text> symptomLabels, int requiredCount)
    {
        if (entryRoot == null || symptomLabels == null || symptomLabels.Count == 0 || requiredCount <= symptomLabels.Count)
        {
            return;
        }

        TMP_Text templateLabel = GetTemplateSymptomLabel(symptomLabels);
        if (templateLabel == null)
        {
            return;
        }

        Transform templateButtonRoot = GetButtonRoot(templateLabel.transform);
        Transform parent = templateButtonRoot != null ? templateButtonRoot.parent : templateLabel.transform.parent;

        if (parent == null)
        {
            return;
        }

        while (symptomLabels.Count < requiredCount)
        {
            Transform source = templateButtonRoot != null ? templateButtonRoot : templateLabel.transform;
            Transform clone = Instantiate(source, parent);
            clone.SetAsLastSibling();
            clone.gameObject.SetActive(true);

            TMP_Text clonedLabel = FindTextByName(clone, targetSymptomLabelName);
            if (clonedLabel != null)
            {
                Transform clonedButtonRoot = GetButtonRoot(clonedLabel.transform);
                if (clonedButtonRoot != null)
                {
                    clonedButtonRoot.gameObject.SetActive(true);
                }

                symptomLabels.Add(clonedLabel);
            }
            else
            {
                break;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);
    }

    private TMP_Text GetTemplateSymptomLabel(List<TMP_Text> symptomLabels)
    {
        if (symptomLabels == null || symptomLabels.Count == 0)
        {
            return null;
        }

        for (int i = symptomLabels.Count - 1; i >= 0; i--)
        {
            TMP_Text label = symptomLabels[i];
            Transform buttonRoot = label != null ? GetButtonRoot(label.transform) : null;
            if (buttonRoot != null && buttonRoot.gameObject.activeSelf)
            {
                return label;
            }
        }

        return symptomLabels[symptomLabels.Count - 1];
    }

    private void CollectCategoryEntries(Transform root, List<CategoryEntry> results)
    {
        if (root == null || results == null)
        {
            return;
        }

        TMP_Text directLabel = root.gameObject.name == targetLabelName
            ? root.GetComponent<TMP_Text>()
            : null;

        if (directLabel != null)
        {
            results.Add(new CategoryEntry
            {
                CategoryLabel = directLabel,
                EntryRoot = root.parent
            });
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectCategoryEntries(root.GetChild(i), results);
        }
    }

    private void CollectTargetTexts(Transform root, string targetName, List<TMP_Text> results)
    {
        if (root == null || results == null)
        {
            return;
        }

        TMP_Text directLabel = root.gameObject.name == targetName
            ? root.GetComponent<TMP_Text>()
            : null;

        if (directLabel != null)
        {
            results.Add(directLabel);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectTargetTexts(root.GetChild(i), targetName, results);
        }
    }

    private TMP_Text FindTextByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        TMP_Text directLabel = root.gameObject.name == targetName
            ? root.GetComponent<TMP_Text>()
            : null;

        if (directLabel != null)
        {
            return directLabel;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            TMP_Text childResult = FindTextByName(root.GetChild(i), targetName);
            if (childResult != null)
            {
                return childResult;
            }
        }

        return null;
    }

    private Transform GetButtonRoot(Transform textTransform)
    {
        if (textTransform == null)
        {
            return null;
        }

        Button button = textTransform.GetComponentInParent<Button>(true);
        return button != null ? button.transform : textTransform.parent;
    }

    private Dictionary<string, string> LoadSymptomTexts(TextAsset xmlAsset)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();

        if (xmlAsset == null || string.IsNullOrWhiteSpace(xmlAsset.text))
        {
            return result;
        }

        XDocument document = XDocument.Parse(xmlAsset.text);
        XElement root = document.Element("NPCData");
        XElement symptomsPoolElement = root?.Element("SymptomsPool");

        if (symptomsPoolElement == null)
        {
            return result;
        }

        foreach (XElement symptomElement in symptomsPoolElement.Elements("Symptom"))
        {
            string id = symptomElement.Attribute("id")?.Value?.Trim();
            string text = symptomElement.Value?.Trim();

            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(text))
            {
                result[id] = text;
            }
        }

        return result;
    }

    private static NPCSymptomCategoryCatalog GetOrLoadSymptomCategoryCatalog(TextAsset xmlAsset)
    {
        if (xmlAsset == null)
        {
            return null;
        }

        if (!CachedSymptomCategoryCatalogs.TryGetValue(xmlAsset, out NPCSymptomCategoryCatalog loadedCatalog))
        {
            loadedCatalog = NPCSymptomCategoriesLoader.Load(xmlAsset);
            CachedSymptomCategoryCatalogs[xmlAsset] = loadedCatalog;
        }

        return loadedCatalog;
    }

    private static NPCProblemCatalog GetOrLoadProblemCatalog(TextAsset xmlAsset)
    {
        if (xmlAsset == null)
        {
            return null;
        }

        if (!CachedProblemCatalogs.TryGetValue(xmlAsset, out NPCProblemCatalog loadedCatalog))
        {
            loadedCatalog = NPCProblemsLoader.Load(xmlAsset);
            CachedProblemCatalogs[xmlAsset] = loadedCatalog;
        }

        return loadedCatalog;
    }

    private Dictionary<string, string> GetOrLoadSymptomTextMap(TextAsset xmlAsset)
    {
        if (xmlAsset == null)
        {
            return new Dictionary<string, string>();
        }

        if (!CachedSymptomTextMaps.TryGetValue(xmlAsset, out Dictionary<string, string> loadedMap))
        {
            loadedMap = LoadSymptomTexts(xmlAsset);
            CachedSymptomTextMaps[xmlAsset] = loadedMap;
        }

        return loadedMap;
    }
}
