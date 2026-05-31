using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public class NPCGenerator : MonoBehaviour
{
    private const int PreparedFallbackLineCount = 3;

    [Serializable]
    private class NamePool
    {
        private const string UnnamedNpc = "Unnamed NPC";

        [SerializeField] private List<string> maleNames = new List<string>();
        [SerializeField] private List<string> femaleNames = new List<string>();
        [SerializeField] private List<string> otherNames = new List<string>();
        [SerializeField] private List<string> surnames = new List<string>();

        public string GetRandomName(NPC.GenderType gender)
        {
            List<string> selectedPool = GetPool(gender);

            if (!TryGetRandomValue(selectedPool, out string firstName))
            {
                return UnnamedNpc;
            }

            if (!TryGetRandomValue(surnames, out string surname))
            {
                return firstName;
            }

            return $"{firstName} {surname}";
        }

        private List<string> GetPool(NPC.GenderType gender)
        {
            switch (gender)
            {
                case NPC.GenderType.Male:
                    return maleNames;

                case NPC.GenderType.Female:
                    return femaleNames;

                default:
                    return otherNames.Count > 0 ? otherNames : femaleNames.Count > 0 ? femaleNames : maleNames;
            }
        }

        public void LoadFromXml(TextAsset xmlAsset)
        {
            if (xmlAsset == null || string.IsNullOrWhiteSpace(xmlAsset.text))
            {
                return;
            }

            XDocument document = XDocument.Parse(xmlAsset.text);
            XElement root = document.Element("names");

            if (root == null)
            {
                return;
            }

            maleNames = ReadValues(root.Element("male_names"), "name");
            femaleNames = ReadValues(root.Element("female_names"), "name");
            otherNames = ReadValues(root.Element("other_names"), "name");
            surnames = ReadValues(root.Element("surnames"), "surname");
        }

        private static List<string> ReadValues(XElement parentElement, string childName)
        {
            List<string> values = new List<string>();

            if (parentElement == null)
            {
                return values;
            }

            foreach (XElement element in parentElement.Elements(childName))
            {
                string value = element.Value?.Trim();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }

            return values;
        }

        private static bool TryGetRandomValue(List<string> values, out string selectedValue)
        {
            selectedValue = null;

            if (values == null || values.Count == 0)
            {
                return false;
            }

            selectedValue = values[UnityEngine.Random.Range(0, values.Count)];
            return !string.IsNullOrWhiteSpace(selectedValue);
        }
    }

    [Header("Data")]
    [SerializeField] private TextAsset npcProblemsXml;
    [SerializeField] private TextAsset npcSymptomeLinesXml;
    [SerializeField] private TextAsset npcTraitFallbackLinesXml;
    [SerializeField] private TextAsset npcNameListXml;

    [Header("NPC Settings")]
    [SerializeField] private int minAge = 18;
    [SerializeField] private int maxAge = 65;
    [SerializeField, Range(0f, 1f)] private float noProblemChance = 0.15f;
    [SerializeField, Range(0f, 1f)] private float nonParanormalChance = 0.35f;
    [SerializeField] private NamePool namePool = new NamePool();

    [Header("Non-Paranormal Cases")]
    [SerializeField] private List<string> nonParanormalConditions = new List<string> { "Cough", "Cold", "Placebo" };
    [SerializeField] private List<string> nonParanormalSymptoms = new List<string> { "cough", "sneezing", "sore throat" };

    [Header("Debug")]
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private NPC generatedNpc;

    private NPCProblemCatalog problemCatalog;
    private NPCSymptomLinesCatalog symptomLinesCatalog;
    private NPCTraitFallbackCatalog traitFallbackCatalog;

    public NPC GeneratedNpc => generatedNpc;
    public NPCProblemCatalog ProblemCatalog => problemCatalog;
    public NPCSymptomLinesCatalog SymptomLinesCatalog => symptomLinesCatalog;
    public NPCTraitFallbackCatalog TraitFallbackCatalog => traitFallbackCatalog;
    public bool IsCatalogLoaded => problemCatalog != null;

    private void Awake()
    {
        if (loadOnAwake)
        {
            LoadCatalog();
        }
    }

    [ContextMenu("Load NPC Catalog")]
    public void LoadCatalog()
    {
        symptomLinesCatalog = null;
        traitFallbackCatalog = null;

        if (npcProblemsXml == null)
        {
            Debug.LogWarning($"{nameof(NPCGenerator)} on {name} has no XML assigned.", this);
            problemCatalog = null;
            return;
        }

        problemCatalog = NPCProblemsLoader.Load(npcProblemsXml);

        if (npcSymptomeLinesXml != null)
        {
            symptomLinesCatalog = NPCSymptomLinesLoader.Load(npcSymptomeLinesXml);
        }

        if (npcTraitFallbackLinesXml != null)
        {
            traitFallbackCatalog = NPCTraitFallbackLoader.Load(npcTraitFallbackLinesXml);
        }

        if (npcNameListXml != null)
        {
            namePool.LoadFromXml(npcNameListXml);
        }
    }

    [ContextMenu("Generate NPC")]
    public void GenerateNpc()
    {
        EnsureCatalogLoaded();

        NPC.GenderType gender = GetRandomGender();
        string npcName = namePool.GetRandomName(gender);
        int age = UnityEngine.Random.Range(Mathf.Min(minAge, maxAge), Mathf.Max(minAge, maxAge) + 1);
        NPCTraitType trait = NPCDialogueUtility.GetRandomTrait();
        List<string> preparedFallbackLines = null;

        generatedNpc = new NPC(npcName, gender, age, trait);

        float roll = UnityEngine.Random.value;
        if (roll <= noProblemChance)
        {
            generatedNpc.ClearCase();
            preparedFallbackLines = BuildPreparedFallbackLines(generatedNpc);
            generatedNpc.SetPreparedFallbackLines(preparedFallbackLines);
            return;
        }

        if (roll <= noProblemChance + nonParanormalChance || problemCatalog == null || problemCatalog.Problems.Count == 0)
        {
            generatedNpc.SetNonParanormalCondition(GetRandomNonParanormalCondition(), GetRandomNonParanormalSymptoms());
            preparedFallbackLines = BuildPreparedFallbackLines(generatedNpc);
            generatedNpc.SetPreparedFallbackLines(preparedFallbackLines);
            return;
        }

        NPCProblemDefinition problem = problemCatalog.Problems[UnityEngine.Random.Range(0, problemCatalog.Problems.Count)];
        generatedNpc.SetProblem(problem);
        preparedFallbackLines = BuildPreparedFallbackLines(generatedNpc);
        generatedNpc.SetPreparedFallbackLines(preparedFallbackLines);
        generatedNpc.SetPreparedConversationLines(BuildPreparedConversationLines(generatedNpc));
    }

    public NPC CreateNpc(string npcName, NPC.GenderType gender, int age, string problemName = null)
    {
        EnsureCatalogLoaded();

        NPC npc = new NPC(npcName, gender, age, NPCDialogueUtility.GetRandomTrait());
        List<string> preparedFallbackLines;

        if (string.IsNullOrWhiteSpace(problemName))
        {
            preparedFallbackLines = BuildPreparedFallbackLines(npc);
            npc.SetPreparedFallbackLines(preparedFallbackLines);
            return npc;
        }

        if (problemCatalog != null && problemCatalog.TryGetProblem(problemName, out NPCProblemDefinition problem))
        {
            npc.SetProblem(problem);
            preparedFallbackLines = BuildPreparedFallbackLines(npc);
            npc.SetPreparedFallbackLines(preparedFallbackLines);
            npc.SetPreparedConversationLines(BuildPreparedConversationLines(npc));
        }
        else
        {
            preparedFallbackLines = BuildPreparedFallbackLines(npc);
            npc.SetPreparedFallbackLines(preparedFallbackLines);
        }

        return npc;
    }

    public bool TryGetProblem(string problemName, out NPCProblemDefinition problem)
    {
        EnsureCatalogLoaded();

        if (problemCatalog == null)
        {
            problem = null;
            return false;
        }

        return problemCatalog.TryGetProblem(problemName, out problem);
    }

    public string GetDialogueLine(NPC npc)
    {
        EnsureCatalogLoaded();
        return NPCDialogueUtility.GetDialogueLine(npc, symptomLinesCatalog, traitFallbackCatalog);
    }

    public string GetQuestionResponse(NPC npc, NPCQuestionType questionType, PlayerProfile playerProfile)
    {
        EnsureCatalogLoaded();
        return NPCDialogueUtility.GetQuestionResponse(
            npc,
            questionType,
            playerProfile,
            symptomLinesCatalog,
            traitFallbackCatalog
        );
    }

    private void EnsureCatalogLoaded()
    {
        if (problemCatalog == null)
        {
            LoadCatalog();
        }
    }

    private List<string> BuildPreparedConversationLines(NPC npc)
    {
        List<string> preparedLines = new List<string>();

        if (npc == null || symptomLinesCatalog == null || npc.SymptomIds.Count == 0)
        {
            return preparedLines;
        }

        List<string> candidateLines = new List<string>();

        for (int i = 0; i < npc.SymptomIds.Count; i++)
        {
            if (!symptomLinesCatalog.TryGetLines(npc.SymptomIds[i], out IReadOnlyList<string> symptomLines) || symptomLines.Count == 0)
            {
                continue;
            }

            for (int lineIndex = 0; lineIndex < symptomLines.Count; lineIndex++)
            {
                string line = symptomLines[lineIndex];

                if (string.IsNullOrWhiteSpace(line) || candidateLines.Contains(line))
                {
                    continue;
                }

                candidateLines.Add(line);
            }
        }

        int preparedCount = Mathf.Min(npc.RemainingConversationTokens, candidateLines.Count);

        for (int i = 0; i < preparedCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, candidateLines.Count);
            preparedLines.Add(candidateLines[selectedIndex]);
            candidateLines.RemoveAt(selectedIndex);
        }

        return preparedLines;
    }

    private List<string> BuildPreparedFallbackLines(NPC npc)
    {
        List<string> preparedLines = new List<string>();

        if (npc == null || traitFallbackCatalog == null)
        {
            return preparedLines;
        }

        if (!traitFallbackCatalog.TryGetLines(npc.Trait, out IReadOnlyList<string> fallbackLines) || fallbackLines.Count == 0)
        {
            return preparedLines;
        }

        List<string> candidateLines = new List<string>();

        for (int i = 0; i < fallbackLines.Count; i++)
        {
            string line = fallbackLines[i];

            if (string.IsNullOrWhiteSpace(line) || candidateLines.Contains(line))
            {
                continue;
            }

            candidateLines.Add(line);
        }

        int preparedCount = Mathf.Min(PreparedFallbackLineCount, candidateLines.Count);

        for (int i = 0; i < preparedCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, candidateLines.Count);
            preparedLines.Add(candidateLines[selectedIndex]);
            candidateLines.RemoveAt(selectedIndex);
        }

        return preparedLines;
    }

    private static NPC.GenderType GetRandomGender()
    {
        Array values = Enum.GetValues(typeof(NPC.GenderType));
        return (NPC.GenderType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
    }

    private string GetRandomNonParanormalCondition()
    {
        return GetRandomListEntry(nonParanormalConditions, "Unknown condition");
    }

    private List<string> GetRandomNonParanormalSymptoms()
    {
        List<string> symptoms = new List<string>();

        if (nonParanormalSymptoms == null || nonParanormalSymptoms.Count == 0)
        {
            return symptoms;
        }

        int symptomCount = Mathf.Clamp(UnityEngine.Random.Range(1, 3), 1, nonParanormalSymptoms.Count);
        List<string> pool = new List<string>(nonParanormalSymptoms);

        for (int i = 0; i < symptomCount && pool.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            symptoms.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return symptoms;
    }

    private static string GetRandomListEntry(IReadOnlyList<string> values, string fallbackValue)
    {
        if (values == null || values.Count == 0)
        {
            return fallbackValue;
        }

        List<string> candidates = new List<string>();
        for (int i = 0; i < values.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
            {
                candidates.Add(values[i].Trim());
            }
        }

        if (candidates.Count == 0)
        {
            return fallbackValue;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }
}
