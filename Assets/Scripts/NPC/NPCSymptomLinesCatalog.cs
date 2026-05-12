using System;
using System.Collections.Generic;

public class NPCSymptomLinesCatalog
{
    private readonly Dictionary<string, List<string>> linesBySymptomId;
    private readonly List<string> symptomIds;

    public NPCSymptomLinesCatalog(Dictionary<string, List<string>> loadedLines)
    {
        linesBySymptomId = loadedLines ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        symptomIds = new List<string>(linesBySymptomId.Keys);
    }

    public IReadOnlyList<string> SymptomIds => symptomIds;

    public bool TryGetLines(string symptomId, out IReadOnlyList<string> lines)
    {
        if (string.IsNullOrWhiteSpace(symptomId) || !linesBySymptomId.TryGetValue(symptomId.Trim(), out List<string> storedLines))
        {
            lines = null;
            return false;
        }

        lines = storedLines;
        return true;
    }
}
