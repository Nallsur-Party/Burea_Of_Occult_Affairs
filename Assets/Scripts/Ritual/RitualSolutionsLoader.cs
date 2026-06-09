using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public static class RitualSolutionsLoader
{
    public static RitualSolutionCatalog Load(TextAsset xmlAsset)
    {
        if (xmlAsset == null)
        {
            Debug.LogWarning("RitualSolutionsLoader: XML asset is not assigned.");
            RitualSolutionCatalog emptyCatalog = ScriptableObject.CreateInstance<RitualSolutionCatalog>();
            emptyCatalog.hideFlags = HideFlags.HideAndDontSave;
            emptyCatalog.PopulateDefaults();
            return emptyCatalog;
        }

        return Load(xmlAsset.text);
    }

    public static RitualSolutionCatalog Load(string xmlContent)
    {
        RitualSolutionCatalog catalog = ScriptableObject.CreateInstance<RitualSolutionCatalog>();
        catalog.hideFlags = HideFlags.HideAndDontSave;

        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            catalog.PopulateDefaults();
            return catalog;
        }

        try
        {
            XDocument document = XDocument.Parse(xmlContent);
            XElement root = document.Element("RitualData");

            if (root == null)
            {
                catalog.PopulateDefaults();
                return catalog;
            }

            List<RitualSolutionDefinition> solutions = new List<RitualSolutionDefinition>();
            foreach (XElement problemElement in root.Elements("Problem"))
            {
                string problemName = ReadAttribute(problemElement, "name");
                if (string.IsNullOrWhiteSpace(problemName))
                {
                    continue;
                }

                List<RitualStepDefinition> steps = new List<RitualStepDefinition>();
                foreach (XElement stepElement in problemElement.Elements("Step"))
                {
                    if (!TryReadStep(stepElement, out RitualStepDefinition step))
                    {
                        continue;
                    }

                    steps.Add(step);
                }

                steps.Sort((left, right) => left.Index.CompareTo(right.Index));
                solutions.Add(new RitualSolutionDefinition(problemName, steps.ToArray()));
            }

            catalog.SetSolutions(solutions);
            if (catalog.Solutions.Count == 0)
            {
                catalog.PopulateDefaults();
            }

            return catalog;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"RitualSolutionsLoader: failed to parse RitualSolutions XML: {exception.Message}");
            catalog.PopulateDefaults();
            return catalog;
        }
    }

    private static bool TryReadStep(XElement stepElement, out RitualStepDefinition step)
    {
        step = null;
        if (stepElement == null)
        {
            return false;
        }

        string itemValue = ReadAttribute(stepElement, "item");
        string actionValue = ReadAttribute(stepElement, "action");

        if (!Enum.TryParse(itemValue, true, out RitualItemType item))
        {
            return false;
        }

        if (!Enum.TryParse(actionValue, true, out RitualActionType action))
        {
            return false;
        }

        int index = 0;
        string indexValue = ReadAttribute(stepElement, "index");
        if (!string.IsNullOrWhiteSpace(indexValue))
        {
            int.TryParse(indexValue, out index);
        }

        step = new RitualStepDefinition(
            index,
            item,
            action,
            ReadAttribute(stepElement, "title"),
            ReadAttribute(stepElement, "description"));
        return true;
    }

    private static string ReadAttribute(XElement element, string attributeName)
    {
        if (element == null || string.IsNullOrWhiteSpace(attributeName))
        {
            return string.Empty;
        }

        XAttribute attribute = element.Attribute(attributeName);
        return attribute != null ? attribute.Value.Trim() : string.Empty;
    }
}
