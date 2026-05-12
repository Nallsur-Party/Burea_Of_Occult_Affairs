using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public static class NPCSymptomCategoriesLoader
{
    public static NPCSymptomCategoryCatalog Load(TextAsset xmlAsset)
    {
        if (xmlAsset == null)
        {
            throw new ArgumentNullException(nameof(xmlAsset));
        }

        return Load(xmlAsset.text);
    }

    public static NPCSymptomCategoryCatalog Load(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            return new NPCSymptomCategoryCatalog(Array.Empty<NPCSymptomCategoryDefinition>());
        }

        XDocument document = XDocument.Parse(xmlContent);
        XElement root = document.Element("SymptomCategories");

        if (root == null)
        {
            return new NPCSymptomCategoryCatalog(Array.Empty<NPCSymptomCategoryDefinition>());
        }

        List<NPCSymptomCategoryDefinition> categories = new List<NPCSymptomCategoryDefinition>();

        foreach (XElement categoryElement in root.Elements("Category"))
        {
            string id = categoryElement.Attribute("id")?.Value?.Trim();
            string displayName = categoryElement.Attribute("displayName")?.Value?.Trim();

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            List<string> symptomIds = new List<string>();
            foreach (XElement symptomRefElement in categoryElement.Elements("SymptomRef"))
            {
                string symptomId = symptomRefElement.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(symptomId))
                {
                    symptomIds.Add(symptomId);
                }
            }

            categories.Add(new NPCSymptomCategoryDefinition(id, displayName, symptomIds));
        }

        return new NPCSymptomCategoryCatalog(categories);
    }
}
