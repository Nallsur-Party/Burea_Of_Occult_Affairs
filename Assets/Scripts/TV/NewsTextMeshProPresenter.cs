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

    [Header("Playback Sync")]
    [SerializeField] private TMPPageTypewriter typewriter;

    [Header("Behaviour")]
    [SerializeField] private bool refreshOnStart = true;
    [SerializeField] private bool logWarnings = true;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }

        if (typewriter == null)
        {
            TryResolveTypewriter();
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
        SyncTypewriterSource(false);
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
        RestartTypewriterPlayback();
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

        SyncTypewriterSource(false);
    }

    private void SyncTypewriterSource(bool restartPlayback)
    {
        if (typewriter == null && !TryResolveTypewriter())
        {
            return;
        }

        if (typewriter == null)
        {
            return;
        }

        typewriter.SetSourceText(targetText != null ? targetText.text : string.Empty, restartPlayback);
    }

    private void RestartTypewriterPlayback()
    {
        if (typewriter == null && !TryResolveTypewriter())
        {
            return;
        }

        if (typewriter == null)
        {
            return;
        }

        typewriter.RestartPlaybackFromCurrentText();
    }

    private bool TryResolveTypewriter()
    {
        if (typewriter != null)
        {
            return true;
        }

        typewriter = FindSceneComponentIncludingInactive<TMPPageTypewriter>();
        return typewriter != null;
    }

    private static T FindSceneComponentIncludingInactive<T>() where T : Component
    {
        T[] candidates = Resources.FindObjectsOfTypeAll<T>();
        T firstLoadedCandidate = null;
        T firstCandidate = null;

        for (int i = 0; i < candidates.Length; i++)
        {
            T candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            GameObject candidateObject = candidate.gameObject;
            if (candidateObject == null || !candidateObject.scene.IsValid() || !candidateObject.scene.isLoaded)
            {
                continue;
            }

            if (firstCandidate == null)
            {
                firstCandidate = candidate;
            }

            Behaviour behaviour = candidate as Behaviour;
            if (behaviour != null && behaviour.isActiveAndEnabled)
            {
                return candidate;
            }

            if (firstLoadedCandidate == null)
            {
                firstLoadedCandidate = candidate;
            }
        }

        return firstLoadedCandidate != null ? firstLoadedCandidate : firstCandidate;
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
