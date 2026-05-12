using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public static class NPCTraitFallbackLoader
{
    public static NPCTraitFallbackCatalog Load(TextAsset xmlAsset)
    {
        if (xmlAsset == null)
        {
            throw new ArgumentNullException(nameof(xmlAsset));
        }

        return Load(xmlAsset.text);
    }

    public static NPCTraitFallbackCatalog Load(string xmlContent)
    {
        Dictionary<NPCTraitType, List<string>> linesByTrait = new Dictionary<NPCTraitType, List<string>>();

        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            return new NPCTraitFallbackCatalog(linesByTrait);
        }

        XDocument document = XDocument.Parse(xmlContent);
        XElement documentRoot = document.Root;

        if (documentRoot == null)
        {
            return new NPCTraitFallbackCatalog(linesByTrait);
        }

        Dictionary<string, string> phrasesById = LoadPhrases(documentRoot);
        XElement root = documentRoot.Name.LocalName == "TraitFallbackLines"
            ? documentRoot
            : documentRoot.Element("TraitFallbackLines");

        if (root == null)
        {
            return new NPCTraitFallbackCatalog(linesByTrait);
        }

        foreach (XElement traitElement in root.Elements("Trait"))
        {
            string traitValue = traitElement.Attribute("type")?.Value?.Trim();

            if (string.IsNullOrWhiteSpace(traitValue) || !Enum.TryParse(traitValue, true, out NPCTraitType trait))
            {
                continue;
            }

            List<string> lines = new List<string>();

            foreach (XElement sayElement in traitElement.Elements("Say"))
            {
                string line = ResolveLine(sayElement, phrasesById);

                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                }
            }

            if (lines.Count > 0)
            {
                linesByTrait[trait] = lines;
            }
        }

        return new NPCTraitFallbackCatalog(linesByTrait);
    }

    private static Dictionary<string, string> LoadPhrases(XElement documentRoot)
    {
        Dictionary<string, string> phrasesById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        XElement dictionaryElement = documentRoot.Name.LocalName == "PhrasesDictionary"
            ? documentRoot
            : documentRoot.Element("PhrasesDictionary");

        if (dictionaryElement == null)
        {
            return phrasesById;
        }

        foreach (XElement phraseElement in dictionaryElement.Elements("Phrase"))
        {
            string phraseId = phraseElement.Attribute("id")?.Value?.Trim();
            string phraseText = phraseElement.Value?.Trim();

            if (string.IsNullOrWhiteSpace(phraseId) || string.IsNullOrWhiteSpace(phraseText))
            {
                continue;
            }

            phrasesById[phraseId] = phraseText;
        }

        return phrasesById;
    }

    private static string ResolveLine(XElement sayElement, IReadOnlyDictionary<string, string> phrasesById)
    {
        if (sayElement == null)
        {
            return null;
        }

        string refId = sayElement.Attribute("ref")?.Value?.Trim();

        if (!string.IsNullOrWhiteSpace(refId)
            && phrasesById != null
            && phrasesById.TryGetValue(refId, out string referencedLine)
            && !string.IsNullOrWhiteSpace(referencedLine))
        {
            return referencedLine.Trim();
        }

        return sayElement.Value?.Trim();
    }
}
