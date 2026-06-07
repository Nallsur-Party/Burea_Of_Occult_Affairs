using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public static class NewsDataLoader
{
    private static readonly List<ProblemData> problemData = new List<ProblemData>();
    private static readonly List<TemplateData> templateData = new List<TemplateData>();
    private static readonly List<NewsProblemMapping> mappingData = new List<NewsProblemMapping>();
    private static readonly Dictionary<string, ProblemData> problemLookup = new Dictionary<string, ProblemData>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, NewsProblemMapping> mappingLookup = new Dictionary<string, NewsProblemMapping>(StringComparer.OrdinalIgnoreCase);

    private static NPCProblemCatalog npcProblemCatalog = new NPCProblemCatalog(Array.Empty<NPCProblemDefinition>());
    private static bool isInitialized;
    private static bool hasLoggedUninitializedWarning;

    public static bool IsInitialized => isInitialized;
    public static IReadOnlyList<ProblemData> Problems => problemData;
    public static IReadOnlyList<TemplateData> Templates => templateData;
    public static IReadOnlyList<NewsProblemMapping> Mappings => mappingData;
    public static NPCProblemCatalog NpcProblemCatalog => npcProblemCatalog;

    public static void Initialize(
        TextAsset npcProblemsXml,
        TextAsset problemNewsXml,
        TextAsset newsTemplatesXml,
        TextAsset newsMappingsXml)
    {
        hasLoggedUninitializedWarning = false;
        isInitialized = false;

        ClearState();

        npcProblemCatalog = NPCProblemsLoader.Load(npcProblemsXml);
        LoadProblems(problemNewsXml);
        LoadTemplates(newsTemplatesXml);
        LoadMappings(newsMappingsXml);

        isInitialized = true;
    }

    public static void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        if (!hasLoggedUninitializedWarning)
        {
            hasLoggedUninitializedWarning = true;
            Debug.LogWarning(
                "NewsDataLoader: not initialized. Add NewsDataBootstrap to the scene and assign the XML assets in the inspector.");
        }
    }

    public static bool TryGetNpcProblem(string problemName, out NPCProblemDefinition problem)
    {
        EnsureInitialized();

        problem = null;
        string normalizedName = NormalizeProblemName(problemName);
        if (string.IsNullOrWhiteSpace(normalizedName) || npcProblemCatalog == null)
        {
            return false;
        }

        return npcProblemCatalog.TryGetProblem(normalizedName, out problem);
    }

    public static bool TryGetProblem(string problemName, out ProblemData problem)
    {
        EnsureInitialized();

        problem = null;
        string normalizedName = NormalizeProblemName(problemName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return false;
        }

        return problemLookup.TryGetValue(normalizedName, out problem);
    }

    public static bool TryGetMapping(string problemName, out NewsProblemMapping mapping)
    {
        EnsureInitialized();

        mapping = null;
        string normalizedName = NormalizeProblemName(problemName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return false;
        }

        return mappingLookup.TryGetValue(normalizedName, out mapping);
    }

    private static void ClearState()
    {
        problemData.Clear();
        templateData.Clear();
        mappingData.Clear();
        problemLookup.Clear();
        mappingLookup.Clear();
        npcProblemCatalog = new NPCProblemCatalog(Array.Empty<NPCProblemDefinition>());
    }

    private static void LoadProblems(TextAsset problemNewsXml)
    {
        if (problemNewsXml == null)
        {
            Debug.LogWarning("NewsDataLoader: ProblemNews.xml is not assigned.");
            return;
        }

        if (string.IsNullOrWhiteSpace(problemNewsXml.text))
        {
            Debug.LogWarning("NewsDataLoader: ProblemNews.xml is empty.");
            return;
        }

        try
        {
            XDocument document = XDocument.Parse(problemNewsXml.text);
            XElement root = document.Root;
            if (root == null)
            {
                Debug.LogWarning("NewsDataLoader: ProblemNews.xml has no root element.");
                return;
            }

            foreach (XElement problemElement in root.Elements("Problem"))
            {
                string problemName = NormalizeProblemName(ReadProblemName(problemElement));
                if (string.IsNullOrWhiteSpace(problemName))
                {
                    continue;
                }

                List<ProblemTextData> texts = new List<ProblemTextData>();
                foreach (XElement textElement in problemElement.Elements("Text"))
                {
                    string text = textElement.Value != null ? textElement.Value.Trim() : string.Empty;
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    string gender = GetAttributeValue(textElement, "gender");
                    texts.Add(new ProblemTextData(gender, text));
                }

                if (texts.Count == 0)
                {
                    continue;
                }

                ProblemData entry = new ProblemData(problemName, texts);
                problemData.Add(entry);
                problemLookup[problemName] = entry;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"NewsDataLoader: failed to parse ProblemNews.xml: {exception.Message}");
        }
    }

    private static void LoadTemplates(TextAsset newsTemplatesXml)
    {
        if (newsTemplatesXml == null)
        {
            Debug.LogWarning("NewsDataLoader: NewsTemplates.xml is not assigned.");
            return;
        }

        if (string.IsNullOrWhiteSpace(newsTemplatesXml.text))
        {
            Debug.LogWarning("NewsDataLoader: NewsTemplates.xml is empty.");
            return;
        }

        try
        {
            XDocument document = XDocument.Parse(newsTemplatesXml.text);
            XElement root = document.Root;
            if (root == null)
            {
                Debug.LogWarning("NewsDataLoader: NewsTemplates.xml has no root element.");
                return;
            }

            foreach (XElement templateElement in root.Elements("Template"))
            {
                if (!TryReadTemplateHeader(templateElement, out EventType eventType, out EventSeverity severity, out string gender))
                {
                    continue;
                }

                List<string> lines = new List<string>();
                foreach (XElement lineElement in templateElement.Elements("Line"))
                {
                    string line = lineElement.Value != null ? lineElement.Value.Trim() : string.Empty;
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        lines.Add(line);
                    }
                }

                if (lines.Count == 0)
                {
                    continue;
                }

                templateData.Add(new TemplateData(eventType, severity, gender, lines));
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"NewsDataLoader: failed to parse NewsTemplates.xml: {exception.Message}");
        }
    }

    private static void LoadMappings(TextAsset newsMappingsXml)
    {
        if (newsMappingsXml == null)
        {
            Debug.LogWarning("NewsDataLoader: NewsMappings.xml is not assigned.");
            return;
        }

        if (string.IsNullOrWhiteSpace(newsMappingsXml.text))
        {
            Debug.LogWarning("NewsDataLoader: NewsMappings.xml is empty.");
            return;
        }

        try
        {
            XDocument document = XDocument.Parse(newsMappingsXml.text);
            XElement root = document.Root;
            if (root == null)
            {
                Debug.LogWarning("NewsDataLoader: NewsMappings.xml has no root element.");
                return;
            }

            foreach (XElement mappingElement in root.Elements("Mapping"))
            {
                string problemName = NormalizeProblemName(GetAttributeValue(mappingElement, "problem", "name"));
                if (string.IsNullOrWhiteSpace(problemName))
                {
                    continue;
                }

                if (!TryReadSeverityTier(mappingElement, out EventSeverity severityTier))
                {
                    Debug.LogWarning($"NewsDataLoader: invalid severity tier for mapping '{problemName}', defaulting to Minor.");
                    severityTier = EventSeverity.Minor;
                }

                List<EventType> eventTypes = new List<EventType>();
                foreach (XElement eventElement in mappingElement.Elements("Event"))
                {
                    string eventValue = GetAttributeValue(eventElement, "eventType", "type");
                    if (string.IsNullOrWhiteSpace(eventValue))
                    {
                        continue;
                    }

                    if (!Enum.TryParse(eventValue.Trim(), true, out EventType eventType))
                    {
                        Debug.LogWarning($"NewsDataLoader: invalid event type '{eventValue}' in mapping for '{problemName}'.");
                        continue;
                    }

                    if (!eventTypes.Contains(eventType))
                    {
                        eventTypes.Add(eventType);
                    }
                }

                if (eventTypes.Count == 0)
                {
                    Debug.LogWarning($"NewsDataLoader: mapping for '{problemName}' has no valid event types.");
                    continue;
                }

                NewsProblemMapping mapping = new NewsProblemMapping();
                mapping.SetProblemName(problemName);
                mapping.SetSeverityTier(severityTier);
                mapping.SetEventTypes(eventTypes);

                mappingData.Add(mapping);
                mappingLookup[problemName] = mapping;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"NewsDataLoader: failed to parse NewsMappings.xml: {exception.Message}");
        }
    }

    private static bool TryReadSeverityTier(XElement mappingElement, out EventSeverity severityTier)
    {
        severityTier = EventSeverity.Minor;

        string severityValue = GetAttributeValue(mappingElement, "tier", "severityTier", "severity");
        if (string.IsNullOrWhiteSpace(severityValue))
        {
            return true;
        }

        return Enum.TryParse(severityValue.Trim(), true, out severityTier);
    }

    private static bool TryReadTemplateHeader(XElement templateElement, out EventType eventType, out EventSeverity severity, out string gender)
    {
        eventType = default(EventType);
        severity = default(EventSeverity);
        gender = "any";

        if (templateElement == null)
        {
            return false;
        }

        XAttribute eventTypeAttribute = templateElement.Attribute("eventType");
        XAttribute severityAttribute = templateElement.Attribute("severity");
        XAttribute genderAttribute = templateElement.Attribute("gender");

        if (eventTypeAttribute == null || severityAttribute == null)
        {
            Debug.LogWarning("NewsDataLoader: template is missing eventType or severity attribute.");
            return false;
        }

        if (!Enum.TryParse(eventTypeAttribute.Value, true, out eventType))
        {
            Debug.LogWarning($"NewsDataLoader: invalid eventType '{eventTypeAttribute.Value}'.");
            return false;
        }

        if (!Enum.TryParse(severityAttribute.Value, true, out severity))
        {
            Debug.LogWarning($"NewsDataLoader: invalid severity '{severityAttribute.Value}'.");
            return false;
        }

        if (genderAttribute != null && !string.IsNullOrWhiteSpace(genderAttribute.Value))
        {
            gender = NormalizeGender(genderAttribute.Value);
        }

        return true;
    }

    private static string ReadProblemName(XElement problemElement)
    {
        if (problemElement == null)
        {
            return string.Empty;
        }

        return GetAttributeValue(problemElement, "problem", "name");
    }

    private static string GetAttributeValue(XElement element, params string[] attributeNames)
    {
        if (element == null || attributeNames == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < attributeNames.Length; i++)
        {
            XAttribute attribute = element.Attribute(attributeNames[i]);
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Value))
            {
                return attribute.Value.Trim();
            }
        }

        return string.Empty;
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
        normalized = normalized.Replace("Â«", string.Empty).Replace("Â»", string.Empty);
        return normalized.Trim();
    }
}
