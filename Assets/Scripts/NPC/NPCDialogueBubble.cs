using UnityEngine;
using TMPro;

public class NPCDialogueBubble : MonoBehaviour
{
    private const char UpperHardSign = '\u042A';
    private const char LowerHardSign = '\u044A';
    private const char UpperSoftSign = '\u042C';
    private const char LowerSoftSign = '\u044C';

    [SerializeField] private GameObject bubbleRoot;
    [SerializeField] private TMP_Text bubbleText;
    [SerializeField] private float hideDelay = 2f;

    private float hideTimer = -1f;
    private bool isFocused;
    private bool isVisible;
    private SpriteRenderer[] cachedSpriteRenderers;
    private Color[] cachedSpriteColors;
    private TMP_Text[] cachedTexts;
    private Color[] cachedTextColors;

    private void Awake()
    {
        ResolveReferences();
        CacheVisualComponents();
        SetVisible(false);
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        hideTimer = -1f;
        isFocused = false;
        isVisible = false;
        SetLocalVisualAlpha(0f);
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
        bubbleText.text = SanitizeForCurrentFont(message);
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
        bubbleText.text = SanitizeForCurrentFont(message);
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
        bubbleText.text = SanitizeForCurrentFont(message);
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
            SetLocalVisualAlpha(visible ? 1f : 0f);
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

        cachedSpriteRenderers = bubbleRoot.GetComponentsInChildren<SpriteRenderer>(true);
        cachedTexts = bubbleRoot.GetComponentsInChildren<TMP_Text>(true);

        if (cachedSpriteRenderers != null)
        {
            cachedSpriteColors = new Color[cachedSpriteRenderers.Length];
            for (int i = 0; i < cachedSpriteRenderers.Length; i++)
            {
                cachedSpriteColors[i] = cachedSpriteRenderers[i] != null ? cachedSpriteRenderers[i].color : Color.white;
            }
        }

        if (cachedTexts != null)
        {
            cachedTextColors = new Color[cachedTexts.Length];
            for (int i = 0; i < cachedTexts.Length; i++)
            {
                cachedTextColors[i] = cachedTexts[i] != null ? cachedTexts[i].color : Color.white;
            }
        }
    }

    private void ResolveReferences()
    {
        if (bubbleRoot == null)
        {
            bubbleRoot = gameObject;
        }

        if (bubbleText != null)
        {
            return;
        }

        bubbleText = bubbleRoot.GetComponentInChildren<TMP_Text>(true);
        if (bubbleText == null && bubbleRoot != gameObject)
        {
            bubbleText = GetComponentInChildren<TMP_Text>(true);
        }

        if (bubbleText == null)
        {
            Debug.LogWarning($"{nameof(NPCDialogueBubble)} on '{name}' could not find a {nameof(TMP_Text)} reference.", this);
        }
    }

    private void SetLocalVisualAlpha(float alpha)
    {
        if (cachedSpriteRenderers == null || cachedTexts == null)
        {
            CacheVisualComponents();
        }

        if (cachedSpriteRenderers != null)
        {
            for (int i = 0; i < cachedSpriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = cachedSpriteRenderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                Color color = i < cachedSpriteColors.Length ? cachedSpriteColors[i] : spriteRenderer.color;
                color.a *= alpha;
                spriteRenderer.color = color;
            }
        }

        if (cachedTexts != null)
        {
            for (int i = 0; i < cachedTexts.Length; i++)
            {
                TMP_Text text = cachedTexts[i];
                if (text == null)
                {
                    continue;
                }

                Color color = i < cachedTextColors.Length ? cachedTextColors[i] : text.color;
                color.a *= alpha;
                text.color = color;
            }
        }
    }

    private static string SanitizeForCurrentFont(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        // Legacy LiberationSans SDF in this project is missing hard-sign glyphs.
        return message
            .Replace(UpperHardSign, UpperSoftSign)
            .Replace(LowerHardSign, LowerSoftSign);
    }
}
