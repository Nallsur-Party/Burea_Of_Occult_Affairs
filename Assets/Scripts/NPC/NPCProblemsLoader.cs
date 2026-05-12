using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public static class NPCProblemsLoader
{
    public static NPCProblemCatalog Load(TextAsset xmlAsset)
    {
        if (xmlAsset == null)
        {
            throw new ArgumentNullException(nameof(xmlAsset));
        }

        return Load(xmlAsset.text);
    }

    public static NPCProblemCatalog Load(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            return new NPCProblemCatalog(Array.Empty<NPCProblemDefinition>());
        }

        XDocument document = XDocument.Parse(xmlContent);
        XElement root = document.Element("NPCData");

        if (root == null)
        {
            return new NPCProblemCatalog(Array.Empty<NPCProblemDefinition>());
        }

        Dictionary<string, string> symptomsPool = LoadSymptomsPool(root.Element("SymptomsPool"));
        List<NPCProblemDefinition> problems = LoadProblems(root.Element("Problems"), symptomsPool);
        return new NPCProblemCatalog(problems);
    }

    private static Dictionary<string, string> LoadSymptomsPool(XElement symptomsPoolElement)
    {
        Dictionary<string, string> symptomsPool = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (symptomsPoolElement == null)
        {
            return symptomsPool;
        }

        foreach (XElement symptomElement in symptomsPoolElement.Elements("Symptom"))
        {
            XAttribute idAttribute = symptomElement.Attribute("id");
            string id = idAttribute?.Value?.Trim();
            string symptomText = symptomElement.Value?.Trim();

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(symptomText))
            {
                continue;
            }

            symptomsPool[id] = symptomText;
        }

        return symptomsPool;
    }

    private static List<NPCProblemDefinition> LoadProblems(
        XElement problemsElement,
        IReadOnlyDictionary<string, string> symptomsPool
    )
    {
        List<NPCProblemDefinition> problems = new List<NPCProblemDefinition>();

        if (problemsElement == null)
        {
            return problems;
        }

        foreach (XElement problemElement in problemsElement.Elements("Problem"))
        {
            XAttribute nameAttribute = problemElement.Attribute("name");
            string problemName = nameAttribute?.Value?.Trim();

            if (string.IsNullOrWhiteSpace(problemName))
            {
                continue;
            }

            List<string> symptomIds = new List<string>();
            List<string> symptomTexts = new List<string>();

            foreach (XElement symptomRefElement in problemElement.Elements("SymptomRef"))
            {
                string symptomId = symptomRefElement.Value?.Trim();

                if (string.IsNullOrWhiteSpace(symptomId))
                {
                    continue;
                }

                symptomIds.Add(symptomId);

                if (symptomsPool.TryGetValue(symptomId, out string symptomText))
                {
                    symptomTexts.Add(symptomText);
                }
            }

            problems.Add(new NPCProblemDefinition(problemName, symptomIds, symptomTexts));
        }

        return problems;
    }
}
