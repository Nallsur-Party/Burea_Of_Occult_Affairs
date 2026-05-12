using System;
using System.Collections.Generic;

public class NPCTraitFallbackCatalog
{
    private readonly Dictionary<NPCTraitType, List<string>> linesByTrait;

    public NPCTraitFallbackCatalog(Dictionary<NPCTraitType, List<string>> loadedLines)
    {
        linesByTrait = loadedLines ?? new Dictionary<NPCTraitType, List<string>>();
    }

    public bool TryGetLines(NPCTraitType trait, out IReadOnlyList<string> lines)
    {
        if (!linesByTrait.TryGetValue(trait, out List<string> storedLines) || storedLines == null || storedLines.Count == 0)
        {
            lines = null;
            return false;
        }

        lines = storedLines;
        return true;
    }
}
