using System;
using System.Collections.Generic;

[Serializable]
public class NPCArchiveEntry
{
    public string PersistentId;
    public NPCCaseType CaseType;
    public string Name;
    public NPC.GenderType Gender;
    public int Age;
    public NPCTraitType Trait;
    public string ProblemName;
    public string NonParanormalConditionName;
    public List<string> NonParanormalSymptoms = new List<string>();
    public List<string> SymptomIds = new List<string>();
    public List<string> Symptoms = new List<string>();
    public List<string> PreparedConversationLines = new List<string>();
    public List<string> PreparedFallbackLines = new List<string>();
}

[Serializable]
public class NPCArchiveFileData
{
    public List<NPCArchiveEntry> ArchivedNpcs = new List<NPCArchiveEntry>();
}
