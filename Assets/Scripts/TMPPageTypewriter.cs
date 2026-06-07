using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TMPPageTypewriter : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField, Min(0f)] private float charactersPerSecond = 30f;
    [SerializeField, Min(0f)] private float pagePauseSeconds = 0.5f;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private bool forcePageOverflowMode = true;
    [SerializeField] private GameObject visualRoot;

    private TMP_Text textComponent;
    private string sourceText = string.Empty;
    private bool hasCapturedSourceText;
    private bool isPlaying;
    private bool isWaitingForPageAdvance;
    private float characterAccumulator;
    private float pagePauseTimer;
    private int currentPageIndex;
    private int currentPageStartVisibleCharacterCount;
    private int currentPageEndVisibleCharacterCount;
    private int currentVisibleCharacterCount;
    private int pageCount = 1;

    public bool IsPlaying => isPlaying;
    public bool IsComplete { get; private set; }

    private void Awake()
    {
        ResolveTextComponent();
        CaptureSourceText();
        ApplyIdleState();
    }

    private void OnValidate()
    {
        ResolveTextComponent();
        ResolveVisualRoot();
        charactersPerSecond = Mathf.Max(0f, charactersPerSecond);
        pagePauseSeconds = Mathf.Max(0f, pagePauseSeconds);
    }

    private void Update()
    {
        if (!isPlaying || textComponent == null)
        {
            return;
        }

        if (isWaitingForPageAdvance)
        {
            pagePauseTimer -= Time.deltaTime;
            if (pagePauseTimer <= 0f)
            {
                AdvanceToNextPageOrFinish();
            }

            return;
        }

        float speed = Mathf.Max(0f, charactersPerSecond);
        if (speed <= 0f)
        {
            return;
        }

        characterAccumulator += speed * Time.deltaTime;

        while (characterAccumulator >= 1f && isPlaying && !isWaitingForPageAdvance)
        {
            characterAccumulator -= 1f;
            RevealNextCharacter();
        }
    }

    [ContextMenu("Activate")]
    public void Activate()
    {
        ResolveTextComponent();
        ResolveVisualRoot();
        CaptureSourceText();

        StartPlaybackFromSourceText();
    }

    public void SetSourceText(string newSourceText, bool restartPlayback)
    {
        ResolveTextComponent();
        ResolveVisualRoot();

        sourceText = newSourceText ?? string.Empty;
        hasCapturedSourceText = true;

        if (restartPlayback)
        {
            StartPlaybackFromSourceText();
            return;
        }

        StopPlayback();
        ApplyIdleState();
    }

    public void SyncSourceTextFromCurrentText(bool restartPlayback)
    {
        ResolveTextComponent();
        ResolveVisualRoot();

        if (textComponent == null)
        {
            return;
        }

        SetSourceText(textComponent.text, restartPlayback);
    }

    public void RestartPlaybackFromCurrentText()
    {
        SyncSourceTextFromCurrentText(true);
    }

    [ContextMenu("Deactivate")]
    public void Deactivate()
    {
        StopPlayback();
        ApplyHiddenState();
    }

    [ContextMenu("Reset Playback")]
    public void ResetPlayback()
    {
        StopPlayback();
        ApplyIdleState();
    }

    private void StartPlaybackFromSourceText()
    {
        if (textComponent == null)
        {
            return;
        }

        StopPlayback();

        if (string.IsNullOrEmpty(sourceText))
        {
            ApplyHiddenState();
            return;
        }

        IsComplete = false;
        isPlaying = true;
        isWaitingForPageAdvance = false;
        characterAccumulator = 0f;
        pagePauseTimer = 0f;

        if (forcePageOverflowMode && textComponent.overflowMode != TextOverflowModes.Page)
        {
            textComponent.overflowMode = TextOverflowModes.Page;
        }

        textComponent.text = sourceText;
        textComponent.enabled = true;
        textComponent.pageToDisplay = 1;
        textComponent.maxVisibleCharacters = int.MaxValue;
        textComponent.ForceMeshUpdate();
        SetVisualRootActive(true);

        RefreshPageCache(0);
        ApplyCurrentPageState();
    }

    private void RevealNextCharacter()
    {
        currentVisibleCharacterCount = Mathf.Min(
            currentVisibleCharacterCount + 1,
            currentPageEndVisibleCharacterCount
        );

        ApplyCurrentCharacterCount();

        if (currentVisibleCharacterCount < currentPageEndVisibleCharacterCount)
        {
            return;
        }

        if (currentPageIndex >= pageCount - 1)
        {
            CompletePlayback();
            return;
        }

        if (pagePauseSeconds > 0f)
        {
            isWaitingForPageAdvance = true;
            pagePauseTimer = pagePauseSeconds;
            return;
        }

        AdvanceToNextPageOrFinish();
    }

    private void AdvanceToNextPageOrFinish()
    {
        isWaitingForPageAdvance = false;

        if (currentPageIndex >= pageCount - 1)
        {
            CompletePlayback();
            return;
        }

        currentPageIndex++;
        RefreshPageCache(currentPageIndex);
        ApplyCurrentPageState();
    }

    private void CompletePlayback()
    {
        isPlaying = false;
        isWaitingForPageAdvance = false;
        IsComplete = true;
        characterAccumulator = 0f;
        pagePauseTimer = 0f;

        ApplyHiddenState();
    }

    private void StopPlayback()
    {
        isPlaying = false;
        isWaitingForPageAdvance = false;
        IsComplete = false;
        characterAccumulator = 0f;
        pagePauseTimer = 0f;
        currentPageIndex = 0;
        currentPageStartVisibleCharacterCount = 0;
        currentPageEndVisibleCharacterCount = 0;
        currentVisibleCharacterCount = 0;
        pageCount = 1;
    }

    private void RefreshPageCache(int pageIndex)
    {
        if (textComponent == null)
        {
            pageCount = 1;
            currentPageStartVisibleCharacterCount = 0;
            currentPageEndVisibleCharacterCount = 0;
            currentVisibleCharacterCount = 0;
            return;
        }

        TMP_TextInfo textInfo = textComponent.textInfo;
        TMP_PageInfo[] pages = textInfo != null ? textInfo.pageInfo : null;

        if (pages == null || pages.Length == 0)
        {
            pageCount = 1;
            currentPageStartVisibleCharacterCount = 0;
            currentPageEndVisibleCharacterCount = Mathf.Max(0, textInfo != null ? textInfo.characterCount : 0);
            currentVisibleCharacterCount = currentPageStartVisibleCharacterCount;
            return;
        }

        pageCount = pages.Length;
        currentPageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);

        TMP_PageInfo pageInfo = pages[currentPageIndex];
        currentPageStartVisibleCharacterCount = Mathf.Max(0, pageInfo.firstCharacterIndex);
        currentPageEndVisibleCharacterCount = Mathf.Max(
            currentPageStartVisibleCharacterCount,
            pageInfo.lastCharacterIndex + 1
        );
        currentVisibleCharacterCount = currentPageStartVisibleCharacterCount;
    }

    private void ApplyCurrentPageState()
    {
        if (textComponent == null)
        {
            return;
        }

        textComponent.pageToDisplay = currentPageIndex + 1;
        textComponent.maxVisibleCharacters = currentVisibleCharacterCount;
        textComponent.ForceMeshUpdate();
    }

    private void ApplyCurrentCharacterCount()
    {
        if (textComponent == null)
        {
            return;
        }

        textComponent.maxVisibleCharacters = currentVisibleCharacterCount;
    }

    private void ApplyIdleState()
    {
        if (textComponent == null)
        {
            return;
        }

        textComponent.text = sourceText;
        textComponent.maxVisibleCharacters = int.MaxValue;
        textComponent.pageToDisplay = 1;
        textComponent.enabled = !startHidden;
        SetVisualRootActive(!startHidden);
    }

    private void ApplyHiddenState()
    {
        if (textComponent == null)
        {
            return;
        }

        textComponent.text = sourceText;
        textComponent.maxVisibleCharacters = 0;
        textComponent.pageToDisplay = 1;
        textComponent.enabled = false;
        SetVisualRootActive(false);
    }

    private void ResolveTextComponent()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
        }
    }

    private void ResolveVisualRoot()
    {
        if (visualRoot != null)
        {
            return;
        }

        if (transform.parent != null)
        {
            visualRoot = transform.parent.gameObject;
            return;
        }

        visualRoot = gameObject;
    }

    private void CaptureSourceText()
    {
        if (hasCapturedSourceText || textComponent == null)
        {
            return;
        }

        sourceText = textComponent.text ?? string.Empty;
        hasCapturedSourceText = true;
    }

    private void SetVisualRootActive(bool active)
    {
        if (visualRoot == null)
        {
            return;
        }

        if (visualRoot.activeSelf == active)
        {
            return;
        }

        visualRoot.SetActive(active);
    }
}
