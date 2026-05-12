using System.Collections.Generic;
using UnityEngine;

public static class NPCDialogueUtility
{
    private const string MemoryGapSymptomId = "S1";
    private const string ConfusedDetailsSymptomId = "S4";
    private const string ContradictoryStorySymptomId = "S18";
    private const string MissingNpcLine = "Сначала нужно найти собеседника.";

    public static NPCTraitType GetRandomTrait()
    {
        int traitCount = System.Enum.GetValues(typeof(NPCTraitType)).Length;
        return (NPCTraitType)Random.Range(0, traitCount);
    }

    public static NPCTraitTokenProfile GetTraitTokenProfile(NPCTraitType trait)
    {
        switch (trait)
        {
            case NPCTraitType.Honest:
                return new NPCTraitTokenProfile(4, 0, 1, 3);

            case NPCTraitType.Liar:
                return new NPCTraitTokenProfile(2, 2, 3, 4);

            default:
                return new NPCTraitTokenProfile(3, 1, 2, 4);
        }
    }

    public static int CalculateTruthTokens(NPCTraitType trait)
    {
        return GetTraitTokenProfile(trait).TruthTokens;
    }

    public static int CalculateLieTokens(NPCTraitType trait)
    {
        return GetTraitTokenProfile(trait).LieTokens;
    }

    public static int CalculateDetectiveQuestionTokens(NPCTraitType trait)
    {
        NPCTraitTokenProfile profile = GetTraitTokenProfile(trait);
        return Random.Range(profile.MinDetectiveTokens, profile.MaxDetectiveTokens + 1);
    }

    public static string GetFallbackLine(NPC npc, NPCTraitFallbackCatalog fallbackCatalog)
    {
        if (npc != null && npc.PreparedFallbackLines.Count > 0)
        {
            return npc.PreparedFallbackLines[Random.Range(0, npc.PreparedFallbackLines.Count)];
        }

        if (npc != null
            && fallbackCatalog != null
            && fallbackCatalog.TryGetLines(npc.Trait, out IReadOnlyList<string> lines)
            && lines.Count > 0)
        {
            return lines[Random.Range(0, lines.Count)];
        }

        return "Мне больше нечего добавить.";
    }

    public static string GetQuestionLimitLine()
    {
        return "Я уже ответил на все, что готов сейчас рассказать.";
    }

    public static string GetDialogueLine(
        NPC npc,
        NPCSymptomLinesCatalog symptomLinesCatalog,
        NPCTraitFallbackCatalog fallbackCatalog
    )
    {
        if (npc == null)
        {
            return "Данные NPC еще не сгенерированы.";
        }

        if (!npc.HasProblem || npc.SymptomIds.Count == 0)
        {
            return "Ничего странного со мной вроде бы не происходит.";
        }

        if (npc.RemainingConversationTokens <= 0 || npc.PreparedConversationLines.Count == 0)
        {
            if (npc.TryGetRepeatedConversationLine(out string repeatedConversationLine))
            {
                return repeatedConversationLine;
            }

            return GetFallbackLine(npc, fallbackCatalog);
        }

        if (!TryGetPreparedConversationLine(npc, out string conversationLine))
        {
            return GetFallbackLine(npc, fallbackCatalog);
        }

        return conversationLine;
    }

    public static string GetQuestionResponse(
        NPC npc,
        NPCQuestionType questionType,
        PlayerProfile playerProfile,
        NPCSymptomLinesCatalog symptomLinesCatalog,
        NPCTraitFallbackCatalog fallbackCatalog
    )
    {
        if (npc == null)
        {
            return MissingNpcLine;
        }

        if (questionType == NPCQuestionType.AnotherStory)
        {
            return GetAnotherStoryQuestionResponse(npc, playerProfile, symptomLinesCatalog, fallbackCatalog);
        }

        if (npc.HasRememberedAnswer(questionType, out string rememberedAnswer))
        {
            return rememberedAnswer;
        }

        if (npc.RemainingDetectiveQuestionTokens <= 0)
        {
            return GetRepeatedQuestionAnswerOrLimit(npc, questionType);
        }

        npc.MarkQuestionAsked(questionType);
        npc.ConsumeDetectiveQuestionToken();

        string answer = BuildQuestionAnswer(npc, questionType);
        npc.RememberAnswer(questionType, answer);
        npc.RecordQuestionAnswer(questionType, answer);
        return answer;
    }

    private static string BuildQuestionAnswer(NPC npc, NPCQuestionType questionType)
    {
        switch (questionType)
        {
            case NPCQuestionType.Name:
                return $"Меня зовут {npc.Name}.";

            case NPCQuestionType.Gender:
                return npc.Gender == NPC.GenderType.Male
                    ? "Я мужчина."
                    : npc.Gender == NPC.GenderType.Female
                        ? "Я женщина."
                        : "Я не хочу уточнять пол.";

            case NPCQuestionType.Age:
                return $"Мне {npc.Age}.";

            default:
                return "Не понимаю, о чем именно вы спрашиваете.";
        }
    }

    private static string GetAnotherStoryQuestionResponse(
        NPC npc,
        PlayerProfile playerProfile,
        NPCSymptomLinesCatalog symptomLinesCatalog,
        NPCTraitFallbackCatalog fallbackCatalog
    )
    {
        if (npc == null)
        {
            return MissingNpcLine;
        }

        string repeatedConversationLine = TryGetMemoryGapConversationLine(npc);

        if (!string.IsNullOrWhiteSpace(repeatedConversationLine))
        {
            if (npc.RemainingDetectiveQuestionTokens > 0)
            {
                ConsumeAnotherStoryQuestionToken(npc);
            }

            npc.RecordQuestionAnswer(NPCQuestionType.AnotherStory, repeatedConversationLine);
            return repeatedConversationLine;
        }

        if (npc.HasSymptomId(MemoryGapSymptomId))
        {
            return GetRepeatedQuestionAnswerOrLimit(npc, NPCQuestionType.AnotherStory);
        }

        if (CanVaryFollowUpLine(npc)
            && symptomLinesCatalog != null
            && TryGetContradictoryFollowUpLine(npc, symptomLinesCatalog, out string unstableLine))
        {
            if (npc.RemainingDetectiveQuestionTokens <= 0)
            {
                return GetRepeatedQuestionAnswerOrLimit(npc, NPCQuestionType.AnotherStory);
            }

            ConsumeAnotherStoryQuestionToken(npc);
            npc.MarkFollowUpLineTold(unstableLine);
            npc.RecordQuestionAnswer(NPCQuestionType.AnotherStory, unstableLine);
            return unstableLine;
        }

        if (symptomLinesCatalog != null
            && TryGetFollowUpLine(npc, symptomLinesCatalog, out string followUpLine))
        {
            if (npc.RemainingDetectiveQuestionTokens <= 0)
            {
                return GetRepeatedQuestionAnswerOrLimit(npc, NPCQuestionType.AnotherStory);
            }

            ConsumeAnotherStoryQuestionToken(npc);
            npc.MarkFollowUpLineTold(followUpLine);
            npc.RecordQuestionAnswer(NPCQuestionType.AnotherStory, followUpLine);
            return followUpLine;
        }

        if (npc.TryGetRepeatedQuestionAnswer(NPCQuestionType.AnotherStory, out string repeatedAnotherStoryAnswer))
        {
            if (npc.RemainingDetectiveQuestionTokens > 0)
            {
                ConsumeAnotherStoryQuestionToken(npc);
            }

            return repeatedAnotherStoryAnswer;
        }

        return GetQuestionLimitLine();
    }

    private static string GetRepeatedQuestionAnswerOrLimit(NPC npc, NPCQuestionType questionType)
    {
        if (npc != null && npc.TryGetRepeatedQuestionAnswer(questionType, out string repeatedAnswer))
        {
            return repeatedAnswer;
        }

        return GetQuestionLimitLine();
    }

    private static void ConsumeAnotherStoryQuestionToken(NPC npc)
    {
        if (npc == null)
        {
            return;
        }

        npc.MarkQuestionAsked(NPCQuestionType.AnotherStory);
        npc.ConsumeDetectiveQuestionToken();
    }

    private static bool TryGetPreparedConversationLine(NPC npc, out string selectedLine)
    {
        selectedLine = null;

        if (npc == null || npc.PreparedConversationLines.Count == 0)
        {
            return false;
        }

        List<string> candidateLines = new List<string>();

        for (int i = 0; i < npc.PreparedConversationLines.Count; i++)
        {
            string line = npc.PreparedConversationLines[i];

            if (string.IsNullOrWhiteSpace(line) || npc.HasToldConversationLine(line))
            {
                continue;
            }

            candidateLines.Add(line);
        }

        if (candidateLines.Count == 0)
        {
            return false;
        }

        selectedLine = candidateLines[Random.Range(0, candidateLines.Count)];
        return MarkConversationLine(npc, selectedLine);
    }

    private static bool TryGetFollowUpLine(NPC npc, NPCSymptomLinesCatalog symptomLinesCatalog, out string selectedLine)
    {
        return TryGetCandidateLine(
            npc,
            symptomLinesCatalog,
            excludeConversationLines: true,
            excludeFollowUpLines: true,
            allowAnyKnownLines: false,
            out selectedLine
        );
    }

    private static bool TryGetCandidateLine(
        NPC npc,
        NPCSymptomLinesCatalog symptomLinesCatalog,
        bool excludeConversationLines,
        bool excludeFollowUpLines,
        bool allowAnyKnownLines,
        out string selectedLine)
    {
        selectedLine = null;

        if (npc == null || symptomLinesCatalog == null)
        {
            return false;
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

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                bool wasInConversation = npc.HasToldConversationLine(line);
                bool wasInFollowUp = npc.HasToldFollowUpLine(line);

                if (!allowAnyKnownLines)
                {
                    if (excludeConversationLines && wasInConversation)
                    {
                        continue;
                    }

                    if (excludeFollowUpLines && wasInFollowUp)
                    {
                        continue;
                    }
                }

                candidateLines.Add(line);
            }
        }

        if (candidateLines.Count == 0)
        {
            return false;
        }

        selectedLine = candidateLines[Random.Range(0, candidateLines.Count)];
        return true;
    }

    private static bool MarkConversationLine(NPC npc, string line)
    {
        if (npc == null || string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        if (npc.HasToldConversationLine(line))
        {
            return false;
        }

        npc.ConsumeConversationToken();
        npc.MarkConversationLineTold(line);
        return true;
    }

    private static string TryGetMemoryGapConversationLine(NPC npc)
    {
        if (npc == null || !npc.HasSymptomId(MemoryGapSymptomId))
        {
            return null;
        }

        if (npc.PreparedConversationLines.Count == 0)
        {
            return null;
        }

        return npc.PreparedConversationLines[Random.Range(0, npc.PreparedConversationLines.Count)];
    }

    private static bool CanVaryFollowUpLine(NPC npc)
    {
        return npc != null
            && (npc.HasSymptomId(ConfusedDetailsSymptomId) || npc.HasSymptomId(ContradictoryStorySymptomId));
    }

    private static bool TryGetContradictoryFollowUpLine(
        NPC npc,
        NPCSymptomLinesCatalog symptomLinesCatalog,
        out string selectedLine)
    {
        selectedLine = null;

        if (npc == null || symptomLinesCatalog == null || symptomLinesCatalog.SymptomIds.Count == 0)
        {
            return false;
        }

        List<string> contradictoryLines = new List<string>();

        for (int i = 0; i < symptomLinesCatalog.SymptomIds.Count; i++)
        {
            string symptomId = symptomLinesCatalog.SymptomIds[i];

            if (npc.HasSymptomId(symptomId))
            {
                continue;
            }

            if (!symptomLinesCatalog.TryGetLines(symptomId, out IReadOnlyList<string> symptomLines) || symptomLines.Count == 0)
            {
                continue;
            }

            for (int lineIndex = 0; lineIndex < symptomLines.Count; lineIndex++)
            {
                string line = symptomLines[lineIndex];

                if (string.IsNullOrWhiteSpace(line) || npc.HasToldAnyLine(line))
                {
                    continue;
                }

                contradictoryLines.Add(line);
            }
        }

        if (contradictoryLines.Count == 0)
        {
            return false;
        }

        selectedLine = contradictoryLines[Random.Range(0, contradictoryLines.Count)];
        return true;
    }
}
