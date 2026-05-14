using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform dragTarget;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private bool disableRaycasterWhileDragging = true;
    [SerializeField, Range(0f, 1f)] private float visiblePortion = 0.5f;

    private Vector2 dragOffset;
    private RectTransform referenceRect;
    private GraphicRaycaster canvasRaycaster;
    private bool restoreRaycasterAfterDrag;

    private void Awake()
    {
        if (dragTarget == null)
        {
            dragTarget = GetComponent<RectTransform>();
        }

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        referenceRect = dragTarget != null && dragTarget.parent is RectTransform rectTransform
            ? rectTransform
            : parentCanvas != null
                ? parentCanvas.transform as RectTransform
                : null;

        if (parentCanvas != null)
        {
            canvasRaycaster = parentCanvas.GetComponent<GraphicRaycaster>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag() || !TryGetLocalPointerPosition(eventData, out Vector2 localPointerPosition))
        {
            return;
        }

        dragTarget.SetAsLastSibling();
        dragOffset = dragTarget.anchoredPosition - localPointerPosition;

        if (disableRaycasterWhileDragging && canvasRaycaster != null && canvasRaycaster.enabled)
        {
            canvasRaycaster.enabled = false;
            restoreRaycasterAfterDrag = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanDrag() || !TryGetLocalPointerPosition(eventData, out Vector2 localPointerPosition))
        {
            return;
        }

        Vector2 nextPosition = ClampToBounds(localPointerPosition + dragOffset);
        if ((dragTarget.anchoredPosition - nextPosition).sqrMagnitude <= 0.0001f)
        {
            return;
        }

        dragTarget.anchoredPosition = nextPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        RestoreRaycaster();
    }

    private void OnDisable()
    {
        RestoreRaycaster();
    }

    private bool CanDrag()
    {
        return isActiveAndEnabled
            && dragTarget != null
            && parentCanvas != null
            && gameObject.activeInHierarchy;
    }

    private bool TryGetLocalPointerPosition(PointerEventData eventData, out Vector2 localPointerPosition)
    {
        localPointerPosition = default;

        if (dragTarget == null || parentCanvas == null)
        {
            return false;
        }

        Camera eventCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        return referenceRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            referenceRect,
            eventData.position,
            eventCamera,
            out localPointerPosition
        );
    }

    private Vector2 ClampToBounds(Vector2 anchoredPosition)
    {
        if (referenceRect == null)
        {
            return anchoredPosition;
        }

        Vector2 targetSize = dragTarget.rect.size;
        Vector2 targetPivot = dragTarget.pivot;
        Vector2 referenceSize = referenceRect.rect.size;
        float hiddenPortion = 1f - Mathf.Clamp01(visiblePortion);

        float minX = -referenceSize.x * referenceRect.pivot.x + targetSize.x * (targetPivot.x - hiddenPortion);
        float maxX = referenceSize.x * (1f - referenceRect.pivot.x) - targetSize.x * ((1f - targetPivot.x) - hiddenPortion);
        float minY = -referenceSize.y * referenceRect.pivot.y + targetSize.y * (targetPivot.y - hiddenPortion);
        float maxY = referenceSize.y * (1f - referenceRect.pivot.y) - targetSize.y * ((1f - targetPivot.y) - hiddenPortion);

        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
        anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, minY, maxY);
        return anchoredPosition;
    }

    private void RestoreRaycaster()
    {
        if (!restoreRaycasterAfterDrag || canvasRaycaster == null)
        {
            return;
        }

        canvasRaycaster.enabled = true;
        restoreRaycasterAfterDrag = false;
    }
}
