using System;
using System.Collections.Generic;
using UnityEngine;

public static class NPCArchiveValidation
{
    private const int MinAge = 1;
    private const int MaxAge = 120;

    public static bool IsValid(NPCArchiveEntry entry, out string reason)
    {
        reason = null;

        if (entry == null)
        {
            reason = "snapshot is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.PersistentId))
        {
            reason = "persistent id is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            reason = $"npc '{entry.PersistentId}' has no name.";
            return false;
        }

        if (!Enum.IsDefined(typeof(NPC.GenderType), entry.Gender))
        {
            reason = $"npc '{entry.PersistentId}' has invalid gender value '{entry.Gender}'.";
            return false;
        }

        if (entry.Age < MinAge || entry.Age > MaxAge)
        {
            reason = $"npc '{entry.PersistentId}' has invalid age '{entry.Age}'.";
            return false;
        }

        if (!Enum.IsDefined(typeof(NPCTraitType), entry.Trait))
        {
            reason = $"npc '{entry.PersistentId}' has invalid trait value '{entry.Trait}'.";
            return false;
        }

        if (!Enum.IsDefined(typeof(NPCCaseType), entry.CaseType))
        {
            reason = $"npc '{entry.PersistentId}' has invalid case type '{entry.CaseType}'.";
            return false;
        }

        if (entry.CaseType == NPCCaseType.Paranormal && string.IsNullOrWhiteSpace(entry.ProblemName))
        {
            reason = $"npc '{entry.PersistentId}' is paranormal but has no problem name.";
            return false;
        }

        if (entry.CaseType == NPCCaseType.Paranormal)
        {
            if (!HasAnyValues(entry.SymptomIds))
            {
                reason = $"npc '{entry.PersistentId}' is paranormal but has no symptom ids.";
                return false;
            }

            if (!HasAnyValues(entry.Symptoms))
            {
                reason = $"npc '{entry.PersistentId}' is paranormal but has no symptoms.";
                return false;
            }
        }

        return true;
    }

    public static bool TryBuildNpc(NPCArchiveEntry entry, out NPC npc, out string reason)
    {
        npc = null;

        if (!IsValid(entry, out reason))
        {
            return false;
        }

        npc = NPC.FromSnapshot(entry);
        if (npc == null)
        {
            reason = $"npc '{entry.PersistentId}' could not be reconstructed from snapshot.";
            return false;
        }

        if (!IsValidRuntimeNpc(npc, out reason))
        {
            npc = null;
            return false;
        }

        return true;
    }

    private static bool IsValidRuntimeNpc(NPC npc, out string reason)
    {
        reason = null;

        if (npc == null)
        {
            reason = "npc is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(npc.Name))
        {
            reason = "npc name is missing.";
            return false;
        }

        if (!Enum.IsDefined(typeof(NPC.GenderType), npc.Gender))
        {
            reason = $"npc '{npc.Name}' has invalid gender value '{npc.Gender}'.";
            return false;
        }

        if (npc.Age < MinAge || npc.Age > MaxAge)
        {
            reason = $"npc '{npc.Name}' has invalid age '{npc.Age}'.";
            return false;
        }

        if (!Enum.IsDefined(typeof(NPCTraitType), npc.Trait))
        {
            reason = $"npc '{npc.Name}' has invalid trait value '{npc.Trait}'.";
            return false;
        }

        if (npc.IsParanormalCase && string.IsNullOrWhiteSpace(npc.ProblemName))
        {
            reason = $"npc '{npc.Name}' is paranormal but has no problem name.";
            return false;
        }

        if (npc.IsParanormalCase)
        {
            NewsDataLoader.EnsureInitialized();
            if (!NewsDataLoader.TryGetNpcProblem(npc.ProblemName, out _))
            {
                reason = $"npc '{npc.Name}' has a problem '{npc.ProblemName}' that is missing from NPCProblems.xml.";
                return false;
            }

            if (!NewsDataLoader.TryGetProblem(npc.ProblemName, out _))
            {
                reason = $"npc '{npc.Name}' has a problem '{npc.ProblemName}' that is missing from ProblemNews.xml.";
                return false;
            }
        }

        return true;
    }

    private static bool HasAnyValues(IReadOnlyList<string> values)
    {
        if (values == null || values.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
            {
                return true;
            }
        }

        return false;
    }
}
