using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public static class NPCSymptomLinesLoader
{
    public static NPCSymptomLinesCatalog Load(TextAsset xmlAsset)
    {
        if (xmlAsset == null)
        {
            throw new ArgumentNullException(nameof(xmlAsset));
        }

        return Load(xmlAsset.text);
    }

    public static NPCSymptomLinesCatalog Load(string xmlContent)
    {
        Dictionary<string, List<string>> linesBySymptomId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            return new NPCSymptomLinesCatalog(linesBySymptomId);
        }

        XDocument document = XDocument.Parse(xmlContent);
        XElement root = document.Element("SymptomsLines");

        if (root == null)
        {
            return new NPCSymptomLinesCatalog(linesBySymptomId);
        }

        foreach (XElement symptomElement in root.Elements("Symptom"))
        {
            string symptomId = symptomElement.Attribute("id")?.Value?.Trim();

            if (string.IsNullOrWhiteSpace(symptomId))
            {
                continue;
            }

            List<string> symptomLines = new List<string>();

            foreach (XElement sayElement in symptomElement.Elements("Say"))
            {
                string line = sayElement.Value?.Trim();

                if (!string.IsNullOrWhiteSpace(line))
                {
                    symptomLines.Add(line);
                }
            }

            if (symptomLines.Count > 0)
            {
                linesBySymptomId[symptomId] = symptomLines;
            }
        }

        return new NPCSymptomLinesCatalog(linesBySymptomId);
    }
}
