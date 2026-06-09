using UnityEngine;

[DisallowMultipleComponent]
public class SceneObjectActivator : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    private void Awake()
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }
    }

    public void Activate()
    {
        SetActive(true);
    }

    public void Deactivate()
    {
        SetActive(false);
    }

    public void Toggle()
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }

        SetActive(!targetObject.activeSelf);
    }

    public void SetActive(bool isActive)
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }

        if (targetObject.activeSelf == isActive)
        {
            return;
        }

        targetObject.SetActive(isActive);
    }
}
