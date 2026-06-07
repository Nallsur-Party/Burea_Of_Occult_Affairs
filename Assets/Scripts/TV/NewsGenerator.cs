using System;
using System.Collections.Generic;
using UnityEngine;

public static class NewsGenerator
{
    private const string PlaceholderName = "{NAME}";
    private const string PlaceholderTraceText = "{TRACE_TEXT}";

    public static string GenerateNews(NPC npc, NPCProblemDefinition activeProblem)
    {
        NewsDataLoader.EnsureInitialized();

        string problemName = ResolveProblemName(npc, activeProblem);
        if (string.IsNullOrWhiteSpace(problemName))
        {
            Debug.LogWarning("NewsGenerator: cannot generate news without a problem name.");
            return string.Empty;
        }

        if (!NewsDataLoader.TryGetNpcProblem(problemName, out _))
        {
            Debug.LogWarning($"NewsGenerator: canonical problem '{problemName}' is missing from NPCProblems.xml.");
            return string.Empty;
        }

        if (!NewsDataLoader.TryGetProblem(problemName, out ProblemData problemData))
        {
            Debug.LogWarning($"NewsGenerator: no ProblemData found for '{problemName}'.");
            return string.Empty;
        }

        if (!NewsDataLoader.TryGetMapping(problemName, out NewsProblemMapping mapping))
        {
            Debug.LogWarning($"NewsGenerator: no news mapping found for problem '{problemName}'.");
            return string.Empty;
        }

        if (!mapping.TryPickEventType(out EventType eventType))
        {
            Debug.LogWarning($"NewsGenerator: mapping for problem '{problemName}' has no valid event types.");
            return string.Empty;
        }

        if (!TryPickSeverity(mapping, out EventSeverity severity))
        {
            Debug.LogWarning($"NewsGenerator: mapping for problem '{problemName}' has no valid severity tier.");
            return string.Empty;
        }

        if (!TrySelectTraceText(problemData, npc, out string traceText))
        {
            Debug.LogWarning($"NewsGenerator: no trace text could be selected for problem '{problemName}'.");
            return string.Empty;
        }

        if (!TrySelectTemplate(eventType, severity, npc, out TemplateData template))
        {
            Debug.LogWarning($"NewsGenerator: no template found for {eventType}/{severity} and problem '{problemName}'.");
            return string.Empty;
        }

        string templateLine = SelectTemplateLine(template);
        if (string.IsNullOrWhiteSpace(templateLine))
        {
            Debug.LogWarning($"NewsGenerator: selected template for '{problemName}' contained no usable lines.");
            return string.Empty;
        }

        string npcName = ResolveNpcName(npc);
        return templateLine
            .Replace(PlaceholderName, npcName)
            .Replace(PlaceholderTraceText, traceText);
    }

    private static string ResolveProblemName(NPC npc, NPCProblemDefinition activeProblem)
    {
        if (activeProblem != null && !string.IsNullOrWhiteSpace(activeProblem.Name))
        {
            return NormalizeProblemName(activeProblem.Name);
        }

        if (npc != null && !string.IsNullOrWhiteSpace(npc.ProblemName))
        {
            return NormalizeProblemName(npc.ProblemName);
        }

        return string.Empty;
    }

    private static string ResolveNpcName(NPC npc)
    {
        if (npc == null || string.IsNullOrWhiteSpace(npc.Name))
        {
            return "Unknown";
        }

        return npc.Name.Trim();
    }

    private static bool TryPickSeverity(NewsProblemMapping mapping, out EventSeverity severity)
    {
        severity = EventSeverity.Minor;

        if (mapping == null || !mapping.TryGetSeverityPool(out List<EventSeverity> severityPool) || severityPool.Count == 0)
        {
            return false;
        }

        severity = severityPool[UnityEngine.Random.Range(0, severityPool.Count)];
        return true;
    }

    private static bool TrySelectTraceText(ProblemData problemData, NPC npc, out string traceText)
    {
        traceText = string.Empty;

        if (problemData == null || problemData.Texts == null || problemData.Texts.Count == 0)
        {
            return false;
        }

        string preferredGender = GetPreferredGender(npc);
        List<string> exactMatches = new List<string>();
        List<string> anyMatches = new List<string>();
        List<string> fallbackMatches = new List<string>();

        for (int i = 0; i < problemData.Texts.Count; i++)
        {
            ProblemTextData textData = problemData.Texts[i];
            if (textData == null || string.IsNullOrWhiteSpace(textData.Text))
            {
                continue;
            }

            string normalizedGender = NormalizeGender(textData.Gender);
            string text = textData.Text.Trim();
            fallbackMatches.Add(text);

            if (normalizedGender == preferredGender)
            {
                exactMatches.Add(text);
                continue;
            }

            if (normalizedGender == "any")
            {
                anyMatches.Add(text);
            }
        }

        if (exactMatches.Count > 0)
        {
            traceText = exactMatches[UnityEngine.Random.Range(0, exactMatches.Count)];
            return true;
        }

        if (anyMatches.Count > 0)
        {
            traceText = anyMatches[UnityEngine.Random.Range(0, anyMatches.Count)];
            return true;
        }

        if (fallbackMatches.Count > 0)
        {
            traceText = fallbackMatches[UnityEngine.Random.Range(0, fallbackMatches.Count)];
            return true;
        }

        return false;
    }

    private static bool TrySelectTemplate(EventType eventType, EventSeverity severity, NPC npc, out TemplateData template)
    {
        template = null;

        List<TemplateData> exactMatches = new List<TemplateData>();
        List<TemplateData> anyMatches = new List<TemplateData>();
        string preferredGender = GetPreferredGender(npc);

        IReadOnlyList<TemplateData> templates = NewsDataLoader.Templates;
        for (int i = 0; i < templates.Count; i++)
        {
            TemplateData candidate = templates[i];
            if (candidate == null || candidate.EventType != eventType || candidate.Severity != severity)
            {
                continue;
            }

            string normalizedGender = NormalizeGender(candidate.Gender);
            if (normalizedGender == preferredGender)
            {
                exactMatches.Add(candidate);
                continue;
            }

            if (normalizedGender == "any")
            {
                anyMatches.Add(candidate);
            }
        }

        if (exactMatches.Count > 0)
        {
            template = exactMatches[UnityEngine.Random.Range(0, exactMatches.Count)];
            return true;
        }

        if (anyMatches.Count > 0)
        {
            template = anyMatches[UnityEngine.Random.Range(0, anyMatches.Count)];
            return true;
        }

        return false;
    }

    private static string SelectTemplateLine(TemplateData template)
    {
        if (template == null || template.Lines == null || template.Lines.Count == 0)
        {
            return string.Empty;
        }

        List<string> validLines = new List<string>();
        for (int i = 0; i < template.Lines.Count; i++)
        {
            string line = template.Lines[i];
            if (!string.IsNullOrWhiteSpace(line))
            {
                validLines.Add(line.Trim());
            }
        }

        if (validLines.Count == 0)
        {
            return string.Empty;
        }

        return validLines[UnityEngine.Random.Range(0, validLines.Count)];
    }

    private static string GetPreferredGender(NPC npc)
    {
        if (npc == null)
        {
            return "any";
        }

        switch (npc.Gender)
        {
            case NPC.GenderType.Male:
                return "male";
            case NPC.GenderType.Female:
                return "female";
            default:
                return "any";
        }
    }

    private static string NormalizeGender(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "any";
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized == "male" || normalized == "female" || normalized == "any")
        {
            return normalized;
        }

        return "any";
    }

    private static string NormalizeProblemName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Trim();
        normalized = normalized.Trim('\"', '\'', '«', '»');
        return normalized.Trim();
    }
}
