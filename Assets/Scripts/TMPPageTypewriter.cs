using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TMPPageTypewriter : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField, Min(0f)]
    private float charactersPerSecond = 30f;

    [SerializeField, Min(0f)]
    private float pagePauseSeconds = 0.5f;

    [SerializeField]
    private bool startHidden = true;

    [SerializeField]
    private bool forcePageOverflowMode = true;

    [SerializeField]
    private GameObject visualRoot;

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip typeSound;

    [SerializeField, Range(0f, 1f)]
    private float volume = 0.5f;

    [SerializeField, Range(0f, 2f)]
    private float pitchRandomness = 0.1f;

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

        // Если AudioSource не назначен, пытаемся найти на объекте
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Если всё ещё null, добавляем новый компонент (чтобы не зависеть от внешнего назначения)
        if (audioSource == null && typeSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
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

        // Проигрываем звук для первого символа
        PlayFirstSound();
    }

    private void RevealNextCharacter()
    {
        int charIndex = currentVisibleCharacterCount;
        char currentChar = ' ';
        if (charIndex < sourceText.Length)
            currentChar = sourceText[charIndex];

        if (char.IsWhiteSpace(currentChar) || char.IsPunctuation(currentChar))
        {
            currentVisibleCharacterCount = Mathf.Min(
                currentVisibleCharacterCount + 1,
                currentPageEndVisibleCharacterCount
            );
            ApplyCurrentCharacterCount();
            return;
        }

        currentVisibleCharacterCount = Mathf.Min(
            currentVisibleCharacterCount + 1,
            currentPageEndVisibleCharacterCount
        );

        PlayTypeSound(); // Воспроизведение звука

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

    private void PlayTypeSound()
    {
        if (IsComplete)
            return;
        if (audioSource == null || typeSound == null)
            return;
        if (audioSource.isPlaying)
            return;

        audioSource.pitch = Random.Range(1f - pitchRandomness, 1f + pitchRandomness);
        audioSource.PlayOneShot(typeSound, volume);
    }

    private void PlayFirstSound()
    {
        Debug.Log($"PlayFirstSound called, sourceText length={sourceText.Length}");
        if (string.IsNullOrEmpty(sourceText))
            return;
        if (audioSource == null || typeSound == null)
            return;

        char firstChar = sourceText[0];
        if (char.IsWhiteSpace(firstChar) || char.IsPunctuation(firstChar))
            return;

        if (audioSource.isPlaying)
            audioSource.Stop();

        float originalPitch = audioSource.pitch;
        audioSource.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness);
        audioSource.PlayOneShot(typeSound, volume);
        audioSource.pitch = originalPitch;
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

        // Остановка звука после завершения текста
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

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

        // Остановка звука при принудительной остановке воспроизведения
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
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
            currentPageEndVisibleCharacterCount = Mathf.Max(
                0,
                textInfo != null ? textInfo.characterCount : 0
            );
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
            return;

        textComponent.text = sourceText;
        textComponent.maxVisibleCharacters = 0;
        textComponent.pageToDisplay = 1;
        textComponent.enabled = false;
        SetVisualRootActive(false);

        // Остановка звука при скрытии
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
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
