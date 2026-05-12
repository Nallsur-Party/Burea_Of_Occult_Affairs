using UnityEngine;
using TMPro;

public class NPCDialogueBubble : MonoBehaviour
{
    [SerializeField] private GameObject bubbleRoot;
    [SerializeField] private TMP_Text bubbleText;
    [SerializeField] private float hideDelay = 2f;

    private float hideTimer = -1f;
    private bool isFocused;
    private bool isVisible;
    private Canvas[] cachedCanvases;
    private Renderer[] cachedRenderers;

    private void Awake()
    {
        CacheVisualComponents();
        SetVisible(false);
    }

    private void Update()
    {
        if (hideTimer < 0f)
        {
            return;
        }

        hideTimer -= Time.deltaTime;

        if (hideTimer <= 0f)
        {
            SetVisible(false);
            hideTimer = -1f;
        }
    }

    public void Show(string message)
    {
        if (bubbleRoot == null || bubbleText == null)
        {
            return;
        }

        SetVisible(true);
        bubbleText.text = message;
        bubbleText.ForceMeshUpdate();
        isFocused = false;
        hideTimer = hideDelay;
    }

    public void ShowPersistent(string message)
    {
        if (bubbleRoot == null || bubbleText == null)
        {
            return;
        }

        SetVisible(true);
        bubbleText.text = message;
        bubbleText.ForceMeshUpdate();
        isFocused = true;
        hideTimer = -1f;
    }

    public void Show(string message, float duration)
    {
        if (bubbleRoot == null || bubbleText == null)
        {
            return;
        }

        SetVisible(true);
        bubbleText.text = message;
        bubbleText.ForceMeshUpdate();
        isFocused = false;
        hideTimer = duration;
    }

    public void Hide()
    {
        SetVisible(false);
        isFocused = false;
        hideTimer = -1f;
    }

    public void SetFocus(bool focused)
    {
        if (bubbleRoot == null || bubbleText == null)
        {
            return;
        }

        isFocused = focused;

        if (focused)
        {
            if (isVisible)
            {
                hideTimer = -1f;
            }

            return;
        }

        if (isVisible && hideTimer < 0f)
        {
            hideTimer = hideDelay;
        }
    }

    public bool IsVisible
    {
        get
        {
            return isVisible;
        }
    }

    private void SetVisible(bool visible)
    {
        if (bubbleRoot == null)
        {
            return;
        }

        if (bubbleRoot == gameObject)
        {
            SetComponentsVisible(visible);
            isVisible = visible;
            return;
        }

        bubbleRoot.SetActive(visible);
        isVisible = visible;
    }

    private void CacheVisualComponents()
    {
        if (bubbleRoot == null)
        {
            return;
        }

        cachedCanvases = bubbleRoot.GetComponentsInChildren<Canvas>(true);
        cachedRenderers = bubbleRoot.GetComponentsInChildren<Renderer>(true);
    }

    private void SetComponentsVisible(bool visible)
    {
        if (cachedCanvases == null || cachedRenderers == null)
        {
            CacheVisualComponents();
        }

        if (cachedCanvases != null)
        {
            for (int i = 0; i < cachedCanvases.Length; i++)
            {
                if (cachedCanvases[i] != null)
                {
                    cachedCanvases[i].enabled = visible;
                }
            }
        }

        if (cachedRenderers != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].enabled = visible;
                }
            }
        }
    }
}
