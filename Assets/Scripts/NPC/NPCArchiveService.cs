using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NPCArchiveService : MonoBehaviour
{
    private const string DefaultArchiveFileName = "npc_archive.json";

    private static NPCArchiveService instance;

    [SerializeField] private string archiveFileName = DefaultArchiveFileName;
    [SerializeField] private bool loadArchiveOnAwake = true;
    [SerializeField] private bool savePrettyPrinted = true;

    [SerializeField] private List<NPCArchiveEntry> archivedNpcs = new List<NPCArchiveEntry>();

    public static NPCArchiveService Instance => instance;
    public IReadOnlyList<NPCArchiveEntry> ArchivedNpcs => archivedNpcs;
    public int ArchivedNpcCount => archivedNpcs != null ? archivedNpcs.Count : 0;
    public string ArchiveFilePath => Path.Combine(Application.persistentDataPath, GetSafeArchiveFileName());

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (archivedNpcs == null)
        {
            archivedNpcs = new List<NPCArchiveEntry>();
        }

        if (loadArchiveOnAwake)
        {
            LoadArchive();
        }
    }

    private void OnApplicationQuit()
    {
        SaveArchive();
    }

    private void OnDisable()
    {
        if (instance == this)
        {
            SaveArchive();
        }
    }

    public void LoadArchive()
    {
        if (archivedNpcs == null)
        {
            archivedNpcs = new List<NPCArchiveEntry>();
        }

        if (!File.Exists(ArchiveFilePath))
        {
            archivedNpcs.Clear();
            return;
        }

        try
        {
            string json = File.ReadAllText(ArchiveFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                archivedNpcs.Clear();
                return;
            }

            NPCArchiveFileData fileData = JsonUtility.FromJson<NPCArchiveFileData>(json);
            archivedNpcs = fileData != null && fileData.ArchivedNpcs != null
                ? fileData.ArchivedNpcs
                : new List<NPCArchiveEntry>();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load NPC archive from '{ArchiveFilePath}': {exception.Message}", this);
            archivedNpcs = new List<NPCArchiveEntry>();
        }
    }

    public void SaveArchive()
    {
        if (archivedNpcs == null)
        {
            archivedNpcs = new List<NPCArchiveEntry>();
        }

        try
        {
            string directoryPath = Path.GetDirectoryName(ArchiveFilePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            NPCArchiveFileData fileData = new NPCArchiveFileData
            {
                ArchivedNpcs = archivedNpcs
            };

            string json = JsonUtility.ToJson(fileData, savePrettyPrinted);
            File.WriteAllText(ArchiveFilePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to save NPC archive to '{ArchiveFilePath}': {exception.Message}", this);
        }
    }

    public bool ArchiveNpc(NPC npc)
    {
        if (npc == null)
        {
            return false;
        }

        NPCArchiveEntry snapshot = npc.CreateSnapshot();
        if (snapshot == null)
        {
            return false;
        }

        UpsertSnapshot(snapshot);
        SaveArchive();
        return true;
    }

    public bool TryTakeArchivedNpc(out NPC npc)
    {
        npc = null;

        if (archivedNpcs == null || archivedNpcs.Count == 0)
        {
            return false;
        }

        for (int index = archivedNpcs.Count - 1; index >= 0; index--)
        {
            NPCArchiveEntry snapshot = archivedNpcs[index];
            if (snapshot == null)
            {
                archivedNpcs.RemoveAt(index);
                continue;
            }

            archivedNpcs.RemoveAt(index);
            SaveArchive();

            npc = NPC.FromSnapshot(snapshot);
            return npc != null;
        }

        SaveArchive();
        return false;
    }

    public bool TryTakeArchivedNpc(string persistentId, out NPC npc)
    {
        npc = null;

        if (string.IsNullOrWhiteSpace(persistentId) || archivedNpcs == null || archivedNpcs.Count == 0)
        {
            return false;
        }

        int index = FindSnapshotIndex(persistentId);
        if (index < 0)
        {
            return false;
        }

        NPCArchiveEntry snapshot = archivedNpcs[index];
        if (snapshot == null)
        {
            archivedNpcs.RemoveAt(index);
            SaveArchive();
            return false;
        }

        archivedNpcs.RemoveAt(index);
        SaveArchive();

        npc = NPC.FromSnapshot(snapshot);
        return npc != null;
    }

    public bool TryGetArchivedSnapshot(string persistentId, out NPCArchiveEntry snapshot)
    {
        snapshot = null;

        int index = FindSnapshotIndex(persistentId);
        if (index < 0)
        {
            return false;
        }

        snapshot = archivedNpcs[index];
        return snapshot != null;
    }

    public bool RemoveArchivedNpc(string persistentId)
    {
        int index = FindSnapshotIndex(persistentId);
        if (index < 0)
        {
            return false;
        }

        archivedNpcs.RemoveAt(index);
        SaveArchive();
        return true;
    }

    public void ClearArchive()
    {
        if (archivedNpcs == null)
        {
            archivedNpcs = new List<NPCArchiveEntry>();
        }

        archivedNpcs.Clear();
        SaveArchive();
    }

    private void UpsertSnapshot(NPCArchiveEntry snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        if (archivedNpcs == null)
        {
            archivedNpcs = new List<NPCArchiveEntry>();
        }

        int index = FindSnapshotIndex(snapshot.PersistentId);
        if (index >= 0)
        {
            archivedNpcs[index] = snapshot;
            return;
        }

        archivedNpcs.Add(snapshot);
    }

    private int FindSnapshotIndex(string persistentId)
    {
        if (string.IsNullOrWhiteSpace(persistentId) || archivedNpcs == null)
        {
            return -1;
        }

        for (int i = 0; i < archivedNpcs.Count; i++)
        {
            NPCArchiveEntry snapshot = archivedNpcs[i];
            if (snapshot != null && string.Equals(snapshot.PersistentId, persistentId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private string GetSafeArchiveFileName()
    {
        if (string.IsNullOrWhiteSpace(archiveFileName))
        {
            return DefaultArchiveFileName;
        }

        return archiveFileName.Trim();
    }
}
