using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("Prefab asset or hidden scene prototype to instantiate for spawned NPCs.")]
    private GameObject npcPawnPrefab;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private NPCGenerator npcGenerator;
    [SerializeField] private NPCQueueManager npcQueueManager;
    [SerializeField] private Transform routeRoot;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform[] exitPoints;
    [SerializeField] private Transform[] zExitRoutePoints;
    [SerializeField] private Transform[] nExitRoutePoints;
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
        if (npcPawnPrefab == null)
        {
            Debug.LogError("NPC Pawn Prefab is not assigned!", this);
            return false;
        }

        if (npcGenerator == null || !npcGenerator.IsCatalogLoaded)
        {
            Debug.LogError("NPCGenerator is not available or catalog not loaded!", this);
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

        NPC generatedNpc = GenerateUniqueNPC();

        npcOrderVisitor.SetNpcData(generatedNpc);
        npcOrderVisitor.ConfigureRoute(startPoint, counterPoint, exitPoints, true);
        npcOrderVisitor.SetSequentialExitRoutePoints(GetSequentialExitRoutePoints());
        npcOrderVisitor.SetHoldUntilCuredExitRoutePoints(GetHoldUntilCuredExitRoutePoints());

        npcQueueManager.EnqueueNPC(npcOrderVisitor);

        Debug.Log($"Spawned NPC: {generatedNpc?.Name}", spawnedNpcObject);
        return true;
    }

    private NPC GenerateUniqueNPC()
    {
        if (npcGenerator == null)
        {
            return null;
        }

        npcGenerator.GenerateNpc();
        return npcGenerator.GeneratedNpc;
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

        if (zExitRoutePoints == null || zExitRoutePoints.Length == 0)
        {
            zExitRoutePoints = new Transform[]
            {
                FindExitPoint("ExitPoint_Z0", "ExitPoint_Z"),
                FindExitPoint("ExitPoint_Z1"),
                FindExitPoint("ExitPoint_Z2")
            };
        }

        if (nExitRoutePoints == null || nExitRoutePoints.Length == 0)
        {
            nExitRoutePoints = new Transform[]
            {
                FindExitPoint("ExitPoint_NStay"),
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
        if (zExitRoutePoints != null && zExitRoutePoints.Length > 0)
        {
            return BuildRoute(zExitRoutePoints);
        }

        return BuildRoute(
            FindExitPoint("ExitPoint_Z0", "ExitPoint_Z"),
            FindExitPoint("ExitPoint_Z1"),
            FindExitPoint("ExitPoint_Z2")
        );
    }

    private Transform[] GetHoldUntilCuredExitRoutePoints()
    {
        if (nExitRoutePoints != null && nExitRoutePoints.Length > 0)
        {
            return BuildRoute(nExitRoutePoints);
        }

        return BuildRoute(
            FindExitPoint("ExitPoint_NStay"),
            FindExitPoint("ExitPoint_N")
        );
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
}
