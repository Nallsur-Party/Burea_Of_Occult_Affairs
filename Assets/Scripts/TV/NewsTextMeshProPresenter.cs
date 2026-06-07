using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class NewsTextMeshProPresenter : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private string archivedNpcPersistentId;
    [SerializeField] private bool useLatestArchivedNpc = true;

    [Header("Output")]
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private string emptyStateText = string.Empty;

    [Header("Behaviour")]
    [SerializeField] private bool refreshOnStart = true;
    [SerializeField] private bool logWarnings = true;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }

    private void Start()
    {
        if (refreshOnStart)
        {
            Refresh();
        }
    }

    [ContextMenu("Refresh News Text")]
    public void Refresh()
    {
        NewsDataLoader.EnsureInitialized();

        if (targetText == null)
        {
            LogWarning("target TMP_Text is not assigned.");
            return;
        }

        if (NPCArchiveService.Instance == null)
        {
            LogWarning("NPCArchiveService instance is not available.");
            SetEmptyState();
            return;
        }

        if (!TryGetArchivedNpc(out NPC npc, out NPCArchiveEntry snapshot, out string reason))
        {
            LogWarning(reason);
            SetEmptyState();
            return;
        }

        if (!NPCArchiveValidation.TryBuildNpc(snapshot, out NPC validatedNpc, out string validationReason))
        {
            LogWarning(validationReason);
            SetEmptyState();
            return;
        }

        NPCProblemDefinition activeProblem = BuildProblemDefinition(snapshot);
        string newsText = NewsGenerator.GenerateNews(validatedNpc, activeProblem);

        if (string.IsNullOrWhiteSpace(newsText))
        {
            LogWarning($"news generation returned an empty result for npc '{validatedNpc.Name}'.");
            SetEmptyState();
            return;
        }

        targetText.text = newsText;
    }

    public bool PinLatestArchivedNpcToTv(out string reason)
    {
        reason = null;

        if (NPCArchiveService.Instance == null)
        {
            reason = "NPCArchiveService instance is not available.";
            return false;
        }

        if (!TryGetLatestArchivedNpc(out NPC npc, out NPCArchiveEntry snapshot, out reason))
        {
            return false;
        }

        if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.PersistentId))
        {
            reason = "latest archived NPC has no persistent id.";
            return false;
        }

        archivedNpcPersistentId = snapshot.PersistentId.Trim();
        useLatestArchivedNpc = false;

        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }

        Refresh();
        return true;
    }

    private bool TryGetArchivedNpc(out NPC npc, out NPCArchiveEntry snapshot, out string reason)
    {
        npc = null;
        snapshot = null;
        reason = null;

        NPCArchiveService archiveService = NPCArchiveService.Instance;
        if (archiveService == null)
        {
            reason = "NPCArchiveService instance is not available.";
            return false;
        }

        if (!useLatestArchivedNpc)
        {
            if (string.IsNullOrWhiteSpace(archivedNpcPersistentId))
            {
                reason = "archivedNpcPersistentId is not assigned.";
                return false;
            }

            if (!archiveService.TryGetArchivedSnapshot(archivedNpcPersistentId, out snapshot))
            {
                reason = $"no archived NPC found for persistent id '{archivedNpcPersistentId}'.";
                return false;
            }

            npc = NPC.FromSnapshot(snapshot);
            return npc != null;
        }

        return TryGetLatestArchivedNpc(out npc, out snapshot, out reason);
    }

    private bool TryGetLatestArchivedNpc(out NPC npc, out NPCArchiveEntry snapshot, out string reason)
    {
        npc = null;
        snapshot = null;
        reason = null;

        NPCArchiveService archiveService = NPCArchiveService.Instance;
        if (archiveService == null)
        {
            reason = "NPCArchiveService instance is not available.";
            return false;
        }

        IReadOnlyList<NPCArchiveEntry> archivedNpcs = archiveService.ArchivedNpcs;
        if (archivedNpcs == null || archivedNpcs.Count == 0)
        {
            reason = "NPC archive is empty.";
            return false;
        }

        for (int index = archivedNpcs.Count - 1; index >= 0; index--)
        {
            snapshot = archivedNpcs[index];
            if (snapshot == null)
            {
                continue;
            }

            string validationReason;
            if (!NPCArchiveValidation.TryBuildNpc(snapshot, out npc, out validationReason))
            {
                reason = string.IsNullOrWhiteSpace(validationReason)
                    ? $"archived NPC at index {index} could not be validated."
                    : validationReason;
                continue;
            }

            return true;
        }

        reason = "no valid archived NPC snapshot was found.";
        return false;
    }

    private static NPCProblemDefinition BuildProblemDefinition(NPCArchiveEntry snapshot)
    {
        if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.ProblemName))
        {
            return null;
        }

        return new NPCProblemDefinition(snapshot.ProblemName.Trim(), snapshot.SymptomIds, snapshot.Symptoms);
    }

    private void SetEmptyState()
    {
        if (targetText != null)
        {
            targetText.text = emptyStateText;
        }
    }

    private void LogWarning(string message)
    {
        if (!logWarnings || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Debug.LogWarning($"{nameof(NewsTextMeshProPresenter)} on '{name}': {message}", this);
    }
}
