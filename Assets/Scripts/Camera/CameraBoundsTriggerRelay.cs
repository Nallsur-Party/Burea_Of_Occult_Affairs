using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraBoundsTriggerRelay : MonoBehaviour
{
    private CameraBoundsZone owner;
    private string playerTag = "Player";

    public void Initialize(CameraBoundsZone newOwner, string newPlayerTag)
    {
        owner = newOwner;
        playerTag = newPlayerTag;
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        owner.ActivateBounds(other);
    }
}
