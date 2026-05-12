using System;
using System.Collections.Generic;

[Serializable]
public class NPCSymptomCategoryDefinition
{
    private readonly string id;
    private readonly string displayName;
    private readonly List<string> symptomIds;

    public string Id => id;
    public string DisplayName => displayName;
    public IReadOnlyList<string> SymptomIds => symptomIds;

    public NPCSymptomCategoryDefinition(string id, string displayName, IEnumerable<string> symptomIds)
    {
        this.id = id;
        this.displayName = displayName;
        this.symptomIds = symptomIds != null ? new List<string>(symptomIds) : new List<string>();
    }
}
