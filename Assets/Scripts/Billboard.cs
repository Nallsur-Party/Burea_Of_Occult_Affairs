using UnityEngine;

[ExecuteInEditMode]
public class Billboard : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool fullBillboard = true;
    [SerializeField] private bool freezeY = true;
    [SerializeField] private bool useMainCamera = true;

    private void Awake()
    {
        ResolveCamera();
    }

    private void OnEnable()
    {
        ApplyRotation();
    }

    private void LateUpdate()
    {
        ApplyRotation();
    }

    private void OnValidate()
    {
        ResolveCamera();
    }

    private void ApplyRotation()
    {
        ResolveCamera();

        if (targetCamera == null)
        {
            return;
        }

        if (fullBillboard)
        {
            transform.rotation = targetCamera.transform.rotation;
            return;
        }

        Vector3 cameraDirection = targetCamera.transform.forward;

        if (freezeY)
        {
            cameraDirection.y = 0f;
        }

        if (cameraDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(cameraDirection.normalized);
    }

    private void ResolveCamera()
    {
        if (useMainCamera && targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }
}
