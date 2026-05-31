using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NPC
{
    private const int RepeatIndexStart = 0;
    private const int DefaultRitualMaxHealth = 3;

    public enum GenderType
    {
        Male,
        Female,
        Other
    }

    [SerializeField] private string persistentId;
    [SerializeField] private NPCCaseType caseType = NPCCaseType.None;
    [SerializeField] private string npcName;
    [SerializeField] private GenderType gender;
    [SerializeField] private int age;
    [SerializeField] private NPCTraitType trait;
    [SerializeField] private string problemName;
    [SerializeField] private string nonParanormalConditionName;
    [SerializeField] private List<string> nonParanormalSymptoms = new List<string>();
    [SerializeField] private List<string> symptomIds = new List<string>();
    [SerializeField] private List<string> symptoms = new List<string>();
    [SerializeField] private List<string> preparedConversationLines = new List<string>();
    [SerializeField] private List<string> preparedFallbackLines = new List<string>();
    [SerializeField] private int truthTokens;
    [SerializeField] private int lieTokens;
    [SerializeField] private int detectiveQuestionTokens;
    [SerializeField] private int spentDetectiveQuestionCount;
    [SerializeField] private int health;
    [SerializeField] private int maxHealth;
    [SerializeField] private bool isCured;
    [NonSerialized] private HashSet<NPCQuestionType> askedQuestionTypes = new HashSet<NPCQuestionType>();
    [NonSerialized] private Dictionary<NPCQuestionType, string> rememberedAnswers = new Dictionary<NPCQuestionType, string>();
    [NonSerialized] private HashSet<string> conversationLines = new HashSet<string>();
    [NonSerialized] private HashSet<string> followUpLines = new HashSet<string>();
    [NonSerialized] private List<string> conversationHistory = new List<string>();
    [NonSerialized] private List<string> followUpHistory = new List<string>();
    [NonSerialized] private Dictionary<NPCQuestionType, List<string>> questionAnswerHistoryByType = new Dictionary<NPCQuestionType, List<string>>();
    [NonSerialized] private int nextConversationRepeatIndex;
    [NonSerialized] private Dictionary<NPCQuestionType, int> nextQuestionRepeatIndexByType = new Dictionary<NPCQuestionType, int>();
    [NonSerialized] private bool shouldRepeatConversationLimitLine = true;
    [NonSerialized] private Dictionary<NPCQuestionType, bool> shouldRepeatQuestionLimitLineByType = new Dictionary<NPCQuestionType, bool>();

    public string PersistentId => persistentId;
    public NPCCaseType CaseType => caseType;
    public string Name => npcName;
    public GenderType Gender => gender;
    public int Age => age;
    public NPCTraitType Trait => trait;
    public string ProblemName => problemName;
    public string NonParanormalConditionName => nonParanormalConditionName;
    public IReadOnlyList<string> NonParanormalSymptoms => nonParanormalSymptoms;
    public IReadOnlyList<string> SymptomIds => symptomIds;
    public IReadOnlyList<string> Symptoms => symptoms;
    public IReadOnlyList<string> PreparedConversationLines => preparedConversationLines;
    public IReadOnlyList<string> PreparedFallbackLines => preparedFallbackLines;
    public int RemainingTruthTokens => truthTokens;
    public int RemainingLieTokens => lieTokens;
    public int RemainingConversationTokens => truthTokens + lieTokens;
    public int RemainingDetectiveQuestionTokens => detectiveQuestionTokens;
    public int SpentDetectiveQuestionCount => spentDetectiveQuestionCount;
    public bool HasProblem => !string.IsNullOrWhiteSpace(problemName);
    public bool IsParanormalCase => caseType == NPCCaseType.Paranormal;
    public bool IsNonParanormalCase => caseType == NPCCaseType.NonParanormal;
    public bool IsBureauPatient => caseType == NPCCaseType.Paranormal;
    public int Health => health;
    public int MaxHealth => maxHealth;
    public bool IsCured => isCured;
    public bool IsAlive => health > 0;

    public NPC(string npcName, GenderType gender, int age, NPCTraitType trait)
    {
        this.npcName = npcName;
        this.gender = gender;
        this.age = age;
        this.trait = trait;
        EnsurePersistentId();
    }

    public NPC(string npcName, GenderType gender, int age, NPCTraitType trait, string problemName, IEnumerable<string> symptomIds, IEnumerable<string> symptoms)
        : this(npcName, gender, age, trait)
    {
        SetProblem(problemName, symptomIds, symptoms);
    }

    public NPCArchiveEntry CreateSnapshot()
    {
        EnsurePersistentId();

        return new NPCArchiveEntry
        {
            PersistentId = persistentId,
            CaseType = caseType,
            Name = npcName,
            Gender = gender,
            Age = age,
            Trait = trait,
            ProblemName = problemName,
            NonParanormalConditionName = nonParanormalConditionName,
            NonParanormalSymptoms = nonParanormalSymptoms != null ? new List<string>(nonParanormalSymptoms) : new List<string>(),
            SymptomIds = symptomIds != null ? new List<string>(symptomIds) : new List<string>(),
            Symptoms = symptoms != null ? new List<string>(symptoms) : new List<string>(),
            PreparedConversationLines = preparedConversationLines != null ? new List<string>(preparedConversationLines) : new List<string>(),
            PreparedFallbackLines = preparedFallbackLines != null ? new List<string>(preparedFallbackLines) : new List<string>()
        };
    }

    public static NPC FromSnapshot(NPCArchiveEntry snapshot)
    {
        if (snapshot == null)
        {
            return null;
        }

        NPC npc = new NPC(snapshot.Name, snapshot.Gender, snapshot.Age, snapshot.Trait);
        npc.ApplySnapshot(snapshot);
        return npc;
    }

    public void ApplySnapshot(NPCArchiveEntry snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        persistentId = NormalizePersistentId(snapshot.PersistentId);
        caseType = snapshot.CaseType;
        npcName = snapshot.Name;
        gender = snapshot.Gender;
        age = snapshot.Age;
        trait = snapshot.Trait;

        ResetDialogueState();

        if (caseType == NPCCaseType.Paranormal && !string.IsNullOrWhiteSpace(snapshot.ProblemName))
        {
            SetProblem(
                snapshot.ProblemName,
                snapshot.SymptomIds,
                snapshot.Symptoms);
        }
        else if (caseType == NPCCaseType.NonParanormal)
        {
            SetNonParanormalCondition(
                snapshot.NonParanormalConditionName ?? snapshot.ProblemName,
                snapshot.NonParanormalSymptoms ?? snapshot.Symptoms);
        }
        else
        {
            ClearCase();
        }

        SetPreparedConversationLines(snapshot.PreparedConversationLines);
        SetPreparedFallbackLines(snapshot.PreparedFallbackLines);
    }

    public void SetParanormalProblem(string newProblemName, IEnumerable<string> newSymptomIds, IEnumerable<string> newSymptoms)
    {
        caseType = NPCCaseType.Paranormal;
        problemName = string.IsNullOrWhiteSpace(newProblemName) ? null : newProblemName.Trim();
        nonParanormalConditionName = null;
        nonParanormalSymptoms.Clear();
        symptomIds.Clear();
        symptoms.Clear();
        ResetDialogueState();

        if (!HasProblem || newSymptoms == null)
        {
            InitializeRitualState(0);
            return;
        }

        if (newSymptomIds != null)
        {
            foreach (string symptomId in newSymptomIds)
            {
                if (string.IsNullOrWhiteSpace(symptomId))
                {
                    continue;
                }

                symptomIds.Add(symptomId.Trim());
            }
        }

        foreach (string symptom in newSymptoms)
        {
            if (string.IsNullOrWhiteSpace(symptom))
            {
                continue;
            }

            symptoms.Add(symptom.Trim());
        }

        truthTokens = NPCDialogueUtility.CalculateTruthTokens(trait);
        lieTokens = NPCDialogueUtility.CalculateLieTokens(trait);
        detectiveQuestionTokens = NPCDialogueUtility.CalculateDetectiveQuestionTokens(trait);
        InitializeRitualState(DefaultRitualMaxHealth);
    }

    public void SetNonParanormalCondition(string conditionName, IEnumerable<string> conditionSymptoms)
    {
        caseType = NPCCaseType.NonParanormal;
        symptomIds.Clear();
        symptoms.Clear();
        problemName = null;
        nonParanormalConditionName = string.IsNullOrWhiteSpace(conditionName) ? null : conditionName.Trim();
        nonParanormalSymptoms.Clear();
        ResetDialogueState();

        if (conditionSymptoms != null)
        {
            foreach (string symptom in conditionSymptoms)
            {
                if (string.IsNullOrWhiteSpace(symptom))
                {
                    continue;
                }

                nonParanormalSymptoms.Add(symptom.Trim());
            }
        }

        InitializeRitualState(0);
    }

    public void SetProblem(string newProblemName, IEnumerable<string> newSymptomIds, IEnumerable<string> newSymptoms)
    {
        SetParanormalProblem(newProblemName, newSymptomIds, newSymptoms);
    }

    public void SetProblem(NPCProblemDefinition problem)
    {
        if (problem == null)
        {
            ClearCase();
            return;
        }

        SetParanormalProblem(problem.Name, problem.SymptomIds, problem.Symptoms);
    }

    public void SetPreparedConversationLines(IEnumerable<string> lines)
    {
        preparedConversationLines.Clear();

        if (lines == null)
        {
            return;
        }

        foreach (string line in lines)
        {
            AddNormalizedLine(preparedConversationLines, line);
        }
    }

    public void SetPreparedFallbackLines(IEnumerable<string> lines)
    {
        preparedFallbackLines.Clear();

        if (lines == null)
        {
            return;
        }

        foreach (string line in lines)
        {
            AddNormalizedLine(preparedFallbackLines, line);
        }
    }

    public void ClearProblem()
    {
        ClearCase();
    }

    public void ClearCase()
    {
        caseType = NPCCaseType.None;
        problemName = null;
        nonParanormalConditionName = null;
        nonParanormalSymptoms.Clear();
        symptomIds.Clear();
        symptoms.Clear();
        preparedConversationLines.Clear();
        preparedFallbackLines.Clear();
        InitializeRitualState(0);
        ResetDialogueState();
    }

    public void InitializeRitualState(int newMaxHealth)
    {
        maxHealth = Math.Max(0, newMaxHealth);
        health = maxHealth;
        isCured = false;
    }

    public void ApplyRitualDamage(int amount)
    {
        if (amount <= 0 || maxHealth <= 0)
        {
            return;
        }

        health = Math.Max(0, health - amount);
        isCured = false;
    }

    public void MarkCured()
    {
        isCured = true;
    }

    public void ConsumeTruthToken()
    {
        truthTokens = Math.Max(0, truthTokens - 1);
    }

    public void ConsumeLieToken()
    {
        lieTokens = Math.Max(0, lieTokens - 1);
    }

    public void ConsumeConversationToken()
    {
        int totalTokens = truthTokens + lieTokens;
        if (totalTokens <= 0)
        {
            return;
        }

        int selectedTokenIndex = UnityEngine.Random.Range(0, totalTokens);
        if (selectedTokenIndex < truthTokens)
        {
            ConsumeTruthToken();
            return;
        }

        ConsumeLieToken();
    }

    public bool HasSymptomId(string symptomId)
    {
        if (string.IsNullOrWhiteSpace(symptomId))
        {
            return false;
        }

        return symptomIds.Contains(symptomId.Trim());
    }

    public bool HasNonParanormalSymptom(string symptom)
    {
        if (string.IsNullOrWhiteSpace(symptom))
        {
            return false;
        }

        return nonParanormalSymptoms.Contains(symptom.Trim());
    }

    public bool HasRememberedAnswer(NPCQuestionType questionType, out string answer)
    {
        EnsureRuntimeState();
        return rememberedAnswers.TryGetValue(questionType, out answer);
    }

    public void RememberAnswer(NPCQuestionType questionType, string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return;
        }

        EnsureRuntimeState();
        rememberedAnswers[questionType] = answer.Trim();
    }

    public int GetRememberedQuestionCount()
    {
        return spentDetectiveQuestionCount;
    }

    public bool HasAskedQuestion(NPCQuestionType questionType)
    {
        EnsureRuntimeState();
        return askedQuestionTypes.Contains(questionType);
    }

    public void MarkQuestionAsked(NPCQuestionType questionType)
    {
        EnsureRuntimeState();
        askedQuestionTypes.Add(questionType);
    }

    public void ConsumeDetectiveQuestionToken()
    {
        detectiveQuestionTokens = Math.Max(0, detectiveQuestionTokens - 1);
        spentDetectiveQuestionCount++;
    }

    public bool HasToldConversationLine(string storyLine)
    {
        EnsureRuntimeState();
        return ContainsNormalizedLine(conversationLines, storyLine);
    }

    public bool HasToldFollowUpLine(string storyLine)
    {
        EnsureRuntimeState();
        return ContainsNormalizedLine(followUpLines, storyLine);
    }

    public bool HasToldAnyLine(string storyLine)
    {
        return HasToldConversationLine(storyLine) || HasToldFollowUpLine(storyLine);
    }

    public void MarkConversationLineTold(string storyLine)
    {
        EnsureRuntimeState();
        AddHistoryLine(conversationLines, conversationHistory, storyLine);
    }

    public void MarkFollowUpLineTold(string storyLine)
    {
        EnsureRuntimeState();
        AddHistoryLine(followUpLines, followUpHistory, storyLine);
    }

    public IReadOnlyList<string> GetConversationHistory()
    {
        EnsureRuntimeState();
        return conversationHistory;
    }

    public IReadOnlyList<string> GetFollowUpHistory()
    {
        EnsureRuntimeState();
        return followUpHistory;
    }

    public void RecordQuestionAnswer(NPCQuestionType questionType, string answer)
    {
        EnsureRuntimeState();
        AddNormalizedLine(GetOrCreateQuestionAnswerHistory(questionType), answer);
    }

    public bool TryGetRepeatedConversationLine(out string repeatedLine)
    {
        EnsureRuntimeState();
        repeatedLine = null;

        if (conversationHistory.Count == 0)
        {
            shouldRepeatConversationLimitLine = true;
            nextConversationRepeatIndex = RepeatIndexStart;
            return false;
        }

        if (shouldRepeatConversationLimitLine)
        {
            shouldRepeatConversationLimitLine = false;
            nextConversationRepeatIndex = RepeatIndexStart;
            return false;
        }

        if (nextConversationRepeatIndex >= conversationHistory.Count)
        {
            shouldRepeatConversationLimitLine = true;
            nextConversationRepeatIndex = RepeatIndexStart;
            return false;
        }

        repeatedLine = conversationHistory[nextConversationRepeatIndex];
        nextConversationRepeatIndex++;
        return !string.IsNullOrWhiteSpace(repeatedLine);
    }

    public bool TryGetRepeatedQuestionAnswer(NPCQuestionType questionType, out string repeatedAnswer)
    {
        EnsureRuntimeState();
        repeatedAnswer = null;

        if (!questionAnswerHistoryByType.TryGetValue(questionType, out List<string> answers) || answers.Count == 0)
        {
            shouldRepeatQuestionLimitLineByType[questionType] = true;
            nextQuestionRepeatIndexByType[questionType] = RepeatIndexStart;
            return false;
        }

        EnsureQuestionRepeatState(questionType);

        if (shouldRepeatQuestionLimitLineByType[questionType])
        {
            shouldRepeatQuestionLimitLineByType[questionType] = false;
            nextQuestionRepeatIndexByType[questionType] = RepeatIndexStart;
            return false;
        }

        if (nextQuestionRepeatIndexByType[questionType] >= answers.Count)
        {
            shouldRepeatQuestionLimitLineByType[questionType] = true;
            nextQuestionRepeatIndexByType[questionType] = RepeatIndexStart;
            return false;
        }

        repeatedAnswer = answers[nextQuestionRepeatIndexByType[questionType]];
        nextQuestionRepeatIndexByType[questionType]++;
        return !string.IsNullOrWhiteSpace(repeatedAnswer);
    }

    private void ResetDialogueState()
    {
        truthTokens = 0;
        lieTokens = 0;
        detectiveQuestionTokens = 0;
        spentDetectiveQuestionCount = 0;
        EnsureRuntimeState();
        askedQuestionTypes.Clear();
        rememberedAnswers.Clear();
        conversationLines.Clear();
        followUpLines.Clear();
        conversationHistory.Clear();
        followUpHistory.Clear();
        questionAnswerHistoryByType.Clear();
        nextConversationRepeatIndex = RepeatIndexStart;
        nextQuestionRepeatIndexByType.Clear();
        shouldRepeatConversationLimitLine = true;
        shouldRepeatQuestionLimitLineByType.Clear();
    }

    private void EnsurePersistentId()
    {
        persistentId = NormalizePersistentId(persistentId);
    }

    private static string NormalizePersistentId(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }

    private List<string> GetOrCreateQuestionAnswerHistory(NPCQuestionType questionType)
    {
        if (!questionAnswerHistoryByType.TryGetValue(questionType, out List<string> answers))
        {
            answers = new List<string>();
            questionAnswerHistoryByType[questionType] = answers;
        }

        return answers;
    }

    private void EnsureQuestionRepeatState(NPCQuestionType questionType)
    {
        if (!shouldRepeatQuestionLimitLineByType.ContainsKey(questionType))
        {
            shouldRepeatQuestionLimitLineByType[questionType] = true;
        }

        if (!nextQuestionRepeatIndexByType.ContainsKey(questionType))
        {
            nextQuestionRepeatIndexByType[questionType] = RepeatIndexStart;
        }
    }

    private static bool ContainsNormalizedLine(HashSet<string> lines, string line)
    {
        if (lines == null || string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return lines.Contains(line.Trim());
    }

    private static void AddHistoryLine(HashSet<string> lines, List<string> history, string line)
    {
        if (lines == null || history == null || string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        string normalizedLine = line.Trim();

        if (lines.Add(normalizedLine))
        {
            history.Add(normalizedLine);
        }
    }

    private static void AddNormalizedLine(List<string> lines, string line)
    {
        if (lines == null || string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lines.Add(line.Trim());
    }

    private void EnsureRuntimeState()
    {
        if (askedQuestionTypes == null)
        {
            askedQuestionTypes = new HashSet<NPCQuestionType>();
        }

        if (rememberedAnswers == null)
        {
            rememberedAnswers = new Dictionary<NPCQuestionType, string>();
        }

        if (conversationLines == null)
        {
            conversationLines = new HashSet<string>();
        }

        if (followUpLines == null)
        {
            followUpLines = new HashSet<string>();
        }

        if (conversationHistory == null)
        {
            conversationHistory = new List<string>();
        }

        if (followUpHistory == null)
        {
            followUpHistory = new List<string>();
        }

        if (questionAnswerHistoryByType == null)
        {
            questionAnswerHistoryByType = new Dictionary<NPCQuestionType, List<string>>();
        }

        if (nextQuestionRepeatIndexByType == null)
        {
            nextQuestionRepeatIndexByType = new Dictionary<NPCQuestionType, int>();
        }

        if (shouldRepeatQuestionLimitLineByType == null)
        {
            shouldRepeatQuestionLimitLineByType = new Dictionary<NPCQuestionType, bool>();
        }
    }
}
