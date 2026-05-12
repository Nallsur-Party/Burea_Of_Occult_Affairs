using System;
using System.Collections.Generic;

[Serializable]
public class NPCProblemDefinition
{
    private readonly List<string> symptomIds;
    private readonly List<string> symptoms;

    public string Name { get; }
    public IReadOnlyList<string> SymptomIds => symptomIds;
    public IReadOnlyList<string> Symptoms => symptoms;

    public NPCProblemDefinition(string name, IEnumerable<string> symptomIds, IEnumerable<string> symptoms)
    {
        Name = name;
        this.symptomIds = symptomIds != null ? new List<string>(symptomIds) : new List<string>();
        this.symptoms = symptoms != null ? new List<string>(symptoms) : new List<string>();
    }
}
