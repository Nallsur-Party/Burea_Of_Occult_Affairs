using System.Collections.Generic;
using UnityEngine;

public class NPCQueueManager : MonoBehaviour
{
    [SerializeField] private Transform[] queuePoints;

    private Queue<NpcOrderVisitor> npcQueue = new Queue<NpcOrderVisitor>();

    public int QueueSize => npcQueue.Count;

    public int QueueCapacity
    {
        get
        {
            int waitingSlots = queuePoints != null ? queuePoints.Length : 0;
            return waitingSlots + 1;
        }
    }

    public bool HasFreeSlot => QueueSize < QueueCapacity;

    public void EnqueueNPC(NpcOrderVisitor npc)
    {
        if (npc != null && !npcQueue.Contains(npc))
        {
            npcQueue.Enqueue(npc);
            Debug.Log($"NPC {npc.gameObject.name} added to queue. Queue size: {npcQueue.Count}");
            UpdateQueueTargets();
        }
    }

    public void DequeueNPC(NpcOrderVisitor npc)
    {
        if (npc != null && npcQueue.Contains(npc))
        {
            Queue<NpcOrderVisitor> tempQueue = new Queue<NpcOrderVisitor>();
            while (npcQueue.Count > 0)
            {
                NpcOrderVisitor current = npcQueue.Dequeue();
                if (current != npc)
                {
                    tempQueue.Enqueue(current);
                }
            }
            npcQueue = tempQueue;
            Debug.Log($"NPC {npc.gameObject.name} removed from queue. Queue size: {npcQueue.Count}");
            UpdateQueueTargets();
        }
    }

    public NpcOrderVisitor GetNextWaitingNPC()
    {
        foreach (NpcOrderVisitor npc in npcQueue)
        {
            if (npc.IsWaitingAtCounter)
            {
                return npc;
            }
        }
        return null;
    }

    public void RefreshQueuePosition(NpcOrderVisitor npc)
    {
        if (npc == null || !npcQueue.Contains(npc))
        {
            return;
        }

        int index = 0;
        foreach (NpcOrderVisitor current in npcQueue)
        {
            if (current == npc)
            {
                break;
            }
            index++;
        }

        if (index == 0)
        {
            npc.SendToCounter();
            return;
        }

        if (queuePoints != null && queuePoints.Length > 0)
        {
            Transform queuePoint = index - 1 < queuePoints.Length ? queuePoints[index - 1] : queuePoints[queuePoints.Length - 1];
            if (queuePoint != null)
            {
                Vector3 queuePosition = GetRandomPositionInQueuePoint(queuePoint);
                npc.SendToQueuePosition(queuePosition);
            }
        }
    }

    private void UpdateQueueTargets()
    {
        if (npcQueue.Count == 0)
        {
            return;
        }

        int index = 0;
        foreach (NpcOrderVisitor npc in npcQueue)
        {
            if (npc == null)
            {
                index++;
                continue;
            }

            if (index == 0)
            {
                npc.SendToCounter();
            }
            else if (queuePoints != null && queuePoints.Length > 0)
            {
                Transform queuePoint = index - 1 < queuePoints.Length ? queuePoints[index - 1] : queuePoints[queuePoints.Length - 1];
                if (queuePoint != null)
                {
                    Vector3 queuePosition = GetRandomPositionInQueuePoint(queuePoint);
                    npc.SendToQueuePosition(queuePosition);
                }
            }
            else
            {
                npc.SendToCounter();
            }

            index++;
        }
    }

    private Vector3 GetRandomPositionInQueuePoint(Transform queuePoint)
    {
        Collider queueCollider = queuePoint.GetComponent<Collider>();
        if (queueCollider != null && queueCollider.isTrigger)
        {
            Bounds bounds = queueCollider.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            return new Vector3(randomX, queuePoint.position.y, randomZ);
        }

        return queuePoint.position;
    }

}
