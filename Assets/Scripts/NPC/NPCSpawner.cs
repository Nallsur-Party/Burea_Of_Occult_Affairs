using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class NPCSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("Prefab asset or hidden scene prototype to instantiate for spawned NPCs.")]
    private GameObject npcPawnPrefab;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private NPCGenerator npcGenerator;
    [SerializeField] private NPCArchiveService npcArchiveService;
    [SerializeField] private NPCQueueManager npcQueueManager;
    [SerializeField] private Transform routeRoot;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform[] exitPoints;
    [SerializeField, InspectorName("Usual Exit Route Points"), FormerlySerializedAs("zExitRoutePoints")] private Transform[] usualExitRoutePoints;
    [SerializeField, InspectorName("Ritual Stay Point")] private Transform ritualStayPoint;
    [SerializeField, InspectorName("Ritual Approach Route Points")] private Transform[] ritualApproachRoutePoints;
    [SerializeField, InspectorName("Ritual Exit Route Points")] private Transform[] ritualExitRoutePoints;
    [SerializeField, HideInInspector, FormerlySerializedAs("ritualRoutePoints"), FormerlySerializedAs("nExitRoutePoints")] private Transform[] legacyRitualRoutePoints;
    [SerializeField] private float autoSpawnInterval = 2f;
    [SerializeField] private bool autoSpawnEnabledByDefault = true;

    private Coroutine autoSpawnCoroutine;
    private bool isAutoSpawnEnabled;

    private void Awake()
    {
        isAutoSpawnEnabled = autoSpawnEnabledByDefault;

        if (npcGenerator == null)
        {
            npcGenerator = FindObjectOfType<NPCGenerator>();
        }

        if (npcArchiveService == null)
        {
            npcArchiveService = NPCArchiveService.Instance != null
                ? NPCArchiveService.Instance
                : FindObjectOfType<NPCArchiveService>();
        }

        if (npcQueueManager == null)
        {
            npcQueueManager = FindObjectOfType<NPCQueueManager>();
        }

        ResolveRouteReferences();

        if (npcGenerator == null)
        {
            Debug.LogError("NPCGenerator not found in scene!", this);
        }

        if (npcQueueManager == null)
        {
            Debug.LogError("NPCQueueManager not found in scene!", this);
        }
    }

    private void Start()
    {
        if (isAutoSpawnEnabled)
        {
            StartAutoSpawn();
        }
    }

    public void SpawnNPC()
    {
        TrySpawnNPC();
    }

    public void SpawnArchivedNPC()
    {
        TrySpawnArchivedNPC();
    }

    public void SpawnArchivedNPC(string persistentId)
    {
        TrySpawnArchivedNPC(persistentId);
    }

    public void StartAutoSpawn()
    {
        isAutoSpawnEnabled = true;

        if (autoSpawnCoroutine != null)
        {
            return;
        }

        autoSpawnCoroutine = StartCoroutine(AutoSpawnRoutine());
    }

    public void StopAutoSpawn()
    {
        isAutoSpawnEnabled = false;

        if (autoSpawnCoroutine == null)
        {
            return;
        }

        StopCoroutine(autoSpawnCoroutine);
        autoSpawnCoroutine = null;
    }

    public void ToggleAutoSpawn()
    {
        if (isAutoSpawnEnabled)
        {
            StopAutoSpawn();
            Debug.Log("NPC auto spawn disabled.", this);
        }
        else
        {
            StartAutoSpawn();
            Debug.Log("NPC auto spawn enabled.", this);
        }
    }

    private IEnumerator AutoSpawnRoutine()
    {
        while (isAutoSpawnEnabled)
        {
            if (npcQueueManager == null)
            {
                Debug.LogError("NPCQueueManager not found in scene!", this);
                break;
            }

            if (npcQueueManager.HasFreeSlot)
            {
                TrySpawnNPC();
            }

            yield return new WaitForSeconds(autoSpawnInterval);
        }

        autoSpawnCoroutine = null;
    }

    private bool TrySpawnNPC()
    {
        if (npcGenerator == null || !npcGenerator.IsCatalogLoaded)
        {
            Debug.LogError("NPCGenerator is not available or catalog not loaded!", this);
            return false;
        }

        if (npcPawnPrefab == null)
        {
            Debug.LogError("NPC Pawn Prefab is not assigned!", this);
            return false;
        }

        if (npcQueueManager == null)
        {
            Debug.LogError("NPCQueueManager not found in scene!", this);
            return false;
        }

        if (!npcQueueManager.HasFreeSlot)
        {
            Debug.Log("NPC spawn skipped because queue is full.", this);
            return false;
        }

        NPC npcData = npcGenerator != null ? CreateGeneratedNpc() : null;
        if (npcData == null)
        {
            Debug.LogError("Failed to generate NPC data for spawned visitor.", this);
            return false;
        }

        return TrySpawnNpcInstance(npcData, "generated");
    }

    public bool TrySpawnArchivedNPC(string persistentId = null)
    {
        if (npcPawnPrefab == null)
        {
            Debug.LogError("NPC Pawn Prefab is not assigned!", this);
            return false;
        }

        if (npcArchiveService == null)
        {
            npcArchiveService = NPCArchiveService.Instance != null
                ? NPCArchiveService.Instance
                : FindObjectOfType<NPCArchiveService>();
        }

        if (npcArchiveService == null)
        {
            Debug.LogError("NPCArchiveService not found in scene!", this);
            return false;
        }

        if (npcQueueManager == null)
        {
            Debug.LogError("NPCQueueManager not found in scene!", this);
            return false;
        }

        if (!npcQueueManager.HasFreeSlot)
        {
            Debug.Log("NPC spawn skipped because queue is full.", this);
            return false;
        }

        NPC npcData;
        bool takenFromArchive = !string.IsNullOrWhiteSpace(persistentId)
            ? npcArchiveService.TryTakeArchivedNpc(persistentId, out npcData)
            : npcArchiveService.TryTakeArchivedNpc(out npcData);

        if (!takenFromArchive || npcData == null)
        {
            Debug.Log("No archived NPC was available to spawn.", this);
            return false;
        }

        if (!TrySpawnNpcInstance(npcData, "archived"))
        {
            npcArchiveService.ArchiveNpc(npcData);
            return false;
        }

        return true;
    }

    private NPC CreateGeneratedNpc()
    {
        if (npcGenerator == null)
        {
            return null;
        }

        npcGenerator.GenerateNpc();
        return npcGenerator.GeneratedNpc;
    }

    private bool TrySpawnNpcInstance(NPC npcData, string spawnSourceLabel)
    {
        if (npcData == null)
        {
            return false;
        }

        if (npcQueueManager == null)
        {
            Debug.LogError("NPCQueueManager not found in scene!", this);
            return false;
        }

        if (!npcQueueManager.HasFreeSlot)
        {
            Debug.Log("NPC spawn skipped because queue is full.", this);
            return false;
        }

        GameObject spawnedNpcObject = Instantiate(npcPawnPrefab, spawnParent);

        NpcOrderVisitor npcOrderVisitor = spawnedNpcObject.GetComponent<NpcOrderVisitor>();
        if (npcOrderVisitor == null)
        {
            Debug.LogError("Spawned NPC Pawn does not have NpcOrderVisitor component!", this);
            Destroy(spawnedNpcObject);
            return false;
        }

        npcOrderVisitor.SetNpcData(npcData);
        npcOrderVisitor.ConfigureRoute(startPoint, counterPoint, exitPoints, true);
        npcOrderVisitor.SetSequentialExitRoutePoints(GetSequentialExitRoutePoints());
        npcOrderVisitor.SetRitualStayPoint(GetRitualStayPoint());
        npcOrderVisitor.SetRitualApproachRoutePoints(GetRitualApproachRoutePoints());
        npcOrderVisitor.SetRitualExitRoutePoints(GetRitualExitRoutePoints());

        npcQueueManager.EnqueueNPC(npcOrderVisitor);

        Debug.Log($"Spawned {spawnSourceLabel} NPC: {npcOrderVisitor.NpcData?.Name}", spawnedNpcObject);
        return true;
    }

    public void SpawnMultipleNPCs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnNPC();
        }
    }

    private void ResolveRouteReferences()
    {
        if (routeRoot == null)
        {
            GameObject routeRootObject = GameObject.Find("WayPoints");
            if (routeRootObject != null)
            {
                routeRoot = routeRootObject.transform;
            }
        }

        if (routeRoot == null)
        {
            return;
        }

        if (startPoint == null)
        {
            startPoint = routeRoot.Find("StartPoint");
        }

        if (counterPoint == null)
        {
            counterPoint = routeRoot.Find("CounterPoint");
        }

        if (exitPoints == null || exitPoints.Length == 0)
        {
            exitPoints = new Transform[]
            {
                FindExitPoint("ExitPoint_Z0", "ExitPoint_Z"),
                FindExitPoint("ExitPoint_Z1"),
                FindExitPoint("ExitPoint_Z2"),
                routeRoot.Find("ExitPoint_N")
            };
        }

        if (usualExitRoutePoints == null || usualExitRoutePoints.Length == 0)
        {
            usualExitRoutePoints = new Transform[]
            {
                FindExitPoint("ExitPoint_Z0", "ExitPoint_Z"),
                FindExitPoint("ExitPoint_Z1"),
                FindExitPoint("ExitPoint_Z2")
            };
        }

        if (ritualStayPoint == null)
        {
            ritualStayPoint = FindExitPoint("ExitPoint_NStay", "ExitPoint_Nstay");
        }

        ApplyLegacyRitualRouteFallback();

        if (ritualExitRoutePoints == null || ritualExitRoutePoints.Length == 0)
        {
            ritualExitRoutePoints = new Transform[]
            {
                FindExitPoint("ExitPoint_N")
            };
        }
    }

    private Transform FindExitPoint(params string[] names)
    {
        if (routeRoot == null || names == null)
        {
            return null;
        }

        foreach (string name in names)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            Transform exitPoint = routeRoot.Find(name);
            if (exitPoint != null)
            {
                return exitPoint;
            }
        }

        return null;
    }

    private Transform[] GetSequentialExitRoutePoints()
    {
        if (usualExitRoutePoints != null && usualExitRoutePoints.Length > 0)
        {
            return BuildRoute(usualExitRoutePoints);
        }

        return BuildRoute(
            FindExitPoint("ExitPoint_Z0", "ExitPoint_Z"),
            FindExitPoint("ExitPoint_Z1"),
            FindExitPoint("ExitPoint_Z2")
        );
    }

    private Transform GetRitualStayPoint()
    {
        return ritualStayPoint;
    }

    private Transform[] GetRitualApproachRoutePoints()
    {
        if (ritualApproachRoutePoints != null && ritualApproachRoutePoints.Length > 0)
        {
            return BuildRoute(ritualApproachRoutePoints);
        }

        if (legacyRitualRoutePoints != null && legacyRitualRoutePoints.Length > 0)
        {
            Transform resolvedStayPoint = GetResolvedRitualStayPoint();
            if (resolvedStayPoint != null)
            {
                return BuildRouteBeforeStayPoint(legacyRitualRoutePoints, resolvedStayPoint);
            }

            return BuildRoute(legacyRitualRoutePoints);
        }

        return BuildRoute();
    }

    private Transform[] GetRitualExitRoutePoints()
    {
        if (ritualExitRoutePoints != null && ritualExitRoutePoints.Length > 0)
        {
            return BuildRoute(ritualExitRoutePoints);
        }

        if (legacyRitualRoutePoints != null && legacyRitualRoutePoints.Length > 0)
        {
            Transform resolvedStayPoint = GetResolvedRitualStayPoint();
            if (resolvedStayPoint != null)
            {
                Transform[] exitRoute = BuildRouteAfterStayPoint(legacyRitualRoutePoints, resolvedStayPoint);
                if (exitRoute.Length > 0)
                {
                    return exitRoute;
                }
            }
        }

        return BuildRoute(FindExitPoint("ExitPoint_N"));
    }

    private static Transform[] BuildRoute(params Transform[] points)
    {
        List<Transform> route = new List<Transform>();
        if (points == null)
        {
            return route.ToArray();
        }

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
            {
                route.Add(points[i]);
            }
        }

        return route.ToArray();
    }

    private void ApplyLegacyRitualRouteFallback()
    {
        if (legacyRitualRoutePoints == null || legacyRitualRoutePoints.Length == 0)
        {
            return;
        }

        Transform resolvedStayPoint = GetResolvedRitualStayPoint();
        if (resolvedStayPoint == null)
        {
            return;
        }

        if (ritualApproachRoutePoints == null || ritualApproachRoutePoints.Length == 0)
        {
            ritualApproachRoutePoints = BuildRouteBeforeStayPoint(legacyRitualRoutePoints, resolvedStayPoint);
        }

        if (ritualExitRoutePoints == null || ritualExitRoutePoints.Length == 0)
        {
            Transform[] exitRoute = BuildRouteAfterStayPoint(legacyRitualRoutePoints, resolvedStayPoint);
            ritualExitRoutePoints = exitRoute.Length > 0
                ? exitRoute
                : BuildRoute(FindExitPoint("ExitPoint_N"));
        }
    }

    private Transform GetResolvedRitualStayPoint()
    {
        if (ritualStayPoint != null)
        {
            return ritualStayPoint;
        }

        ritualStayPoint = FindExitPoint("ExitPoint_NStay", "ExitPoint_Nstay");
        return ritualStayPoint;
    }

    private static Transform[] BuildRouteBeforeStayPoint(IReadOnlyList<Transform> sourcePoints, Transform stayPoint)
    {
        List<Transform> route = new List<Transform>();
        if (sourcePoints == null || sourcePoints.Count == 0)
        {
            return route.ToArray();
        }

        for (int i = 0; i < sourcePoints.Count; i++)
        {
            Transform waypoint = sourcePoints[i];
            if (waypoint == null)
            {
                continue;
            }

            if (waypoint == stayPoint)
            {
                break;
            }

            route.Add(waypoint);
        }

        return route.ToArray();
    }

    private static Transform[] BuildRouteAfterStayPoint(IReadOnlyList<Transform> sourcePoints, Transform stayPoint)
    {
        List<Transform> route = new List<Transform>();
        if (sourcePoints == null || sourcePoints.Count == 0)
        {
            return route.ToArray();
        }

        bool hasPassedStayPoint = stayPoint == null;
        for (int i = 0; i < sourcePoints.Count; i++)
        {
            Transform waypoint = sourcePoints[i];
            if (waypoint == null)
            {
                continue;
            }

            if (!hasPassedStayPoint)
            {
                if (waypoint == stayPoint)
                {
                    hasPassedStayPoint = true;
                }

                continue;
            }

            if (waypoint != stayPoint)
            {
                route.Add(waypoint);
            }
        }

        return route.ToArray();
    }
}
