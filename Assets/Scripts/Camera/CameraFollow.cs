using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);

    [Header("Follow Axes")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;
    [SerializeField] private bool followZ = false;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private float maxSpeed = 20f;

    [Header("Bounds")]
    [SerializeField] private BoxCollider boundsCollider;
    [SerializeField] private bool useBounds = true;

    private Vector3 currentVelocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        Vector3 currentPosition = transform.position;

        if (!followX)
        {
            desiredPosition.x = currentPosition.x;
        }

        if (!followY)
        {
            desiredPosition.y = currentPosition.y;
        }

        if (!followZ)
        {
            desiredPosition.z = currentPosition.z;
        }

        desiredPosition = ClampToBounds(desiredPosition);

        transform.position = Vector3.SmoothDamp(
            currentPosition,
            desiredPosition,
            ref currentVelocity,
            smoothTime,
            maxSpeed
        );
    }

    public void SetBounds(BoxCollider newBounds)
    {
        boundsCollider = newBounds;
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        if (!useBounds || boundsCollider == null)
        {
            return position;
        }

        Bounds bounds = boundsCollider.bounds;
        position.x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
        position.y = Mathf.Clamp(position.y, bounds.min.y, bounds.max.y);
        position.z = Mathf.Clamp(position.z, bounds.min.z, bounds.max.z);
        return position;
    }

    private void OnDrawGizmosSelected()
    {
        if (boundsCollider == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(boundsCollider.bounds.center, boundsCollider.bounds.size);
    }
}
