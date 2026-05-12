using UnityEngine;

public class CameraBoundsZone : MonoBehaviour
{
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private BoxCollider cameraBounds;
    [SerializeField] private Collider onEnterTrigger;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string cameraTag = "MainCamera";

    private CameraBoundsTriggerRelay triggerRelay;

    private void Reset()
    {
        if (cameraBounds == null)
        {
            cameraBounds = GetComponent<BoxCollider>();
        }

        if (onEnterTrigger == null)
        {
            onEnterTrigger = GetComponentInChildren<Collider>();
        }

        if (onEnterTrigger != null)
        {
            onEnterTrigger.isTrigger = true;
        }
    }

    private void Awake()
    {
        ResolveCameraFollow();

        if (cameraBounds == null)
        {
            cameraBounds = GetComponent<BoxCollider>();
        }

        if (onEnterTrigger == null)
        {
            onEnterTrigger = GetComponentInChildren<Collider>();
        }

        RegisterTriggerRelay();
    }

    private void OnValidate()
    {
        if (cameraBounds == null)
        {
            cameraBounds = GetComponent<BoxCollider>();
        }
    }

    private void ResolveCameraFollow()
    {
        if (cameraFollow != null)
        {
            return;
        }

        GameObject cameraObject = GameObject.FindGameObjectWithTag(cameraTag);

        if (cameraObject == null)
        {
            return;
        }

        cameraFollow = cameraObject.GetComponent<CameraFollow>();
    }

    private void RegisterTriggerRelay()
    {
        if (onEnterTrigger == null)
        {
            return;
        }

        onEnterTrigger.isTrigger = true;

        triggerRelay = onEnterTrigger.GetComponent<CameraBoundsTriggerRelay>();

        if (triggerRelay == null)
        {
            triggerRelay = onEnterTrigger.gameObject.AddComponent<CameraBoundsTriggerRelay>();
        }

        triggerRelay.Initialize(this, playerTag);
    }

    public void ActivateBounds(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (cameraFollow != null && cameraBounds != null)
        {
            cameraFollow.SetBounds(cameraBounds);
        }
    }
}
