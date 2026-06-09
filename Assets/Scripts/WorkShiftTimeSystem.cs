using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class WorkShiftTimeSystem : MonoBehaviour
{
    public const int ShiftStartMinutes = 8 * 60;
    public const int ShiftEndMinutes = 18 * 60;
    public const int InspectionCostMinutes = 15;
    public const int ExtraNpcQuestionCostMinutes = 2;
    public const int RitualCostMinutes = 30;

    private static WorkShiftTimeSystem instance;
    private bool isReloadingScene;

    [SerializeField] private int currentMinutesFromMidnight = ShiftStartMinutes;
    [SerializeField] private bool resetToShiftStartOnAwake = true;
    [SerializeField] private bool logTimeChanges = false;

    private bool shiftEndedNotified;

    public static WorkShiftTimeSystem Instance => instance;

    public event Action<int> TimeChanged;
    public event Action ShiftEnded;

    public int CurrentMinutesFromMidnight => currentMinutesFromMidnight;
    public int CurrentHour => currentMinutesFromMidnight / 60;
    public int CurrentMinute => currentMinutesFromMidnight % 60;
    public bool IsShiftEnded => currentMinutesFromMidnight >= ShiftEndMinutes;
    public int RemainingMinutes => Mathf.Max(0, ShiftEndMinutes - currentMinutesFromMidnight);
    public string CurrentTimeText => FormatMinutes(currentMinutesFromMidnight);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject systemObject = new GameObject(nameof(WorkShiftTimeSystem));
        systemObject.AddComponent<WorkShiftTimeSystem>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            instance.ApplySceneConfiguration(this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (resetToShiftStartOnAwake || currentMinutesFromMidnight < ShiftStartMinutes || currentMinutesFromMidnight > ShiftEndMinutes)
        {
            currentMinutesFromMidnight = ShiftStartMinutes;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        NotifyTimeChanged();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (instance == this)
        {
            instance = null;
        }
    }

    public void ResetShift()
    {
        currentMinutesFromMidnight = ShiftStartMinutes;
        shiftEndedNotified = false;
        NotifyTimeChanged();
    }

    [ContextMenu("Log Current Time")]
    public void LogCurrentTime()
    {
        Debug.Log($"WorkShiftTimeSystem | current time: {CurrentTimeText} ({currentMinutesFromMidnight} min)", this);
    }

    public bool CanSpendTime()
    {
        return !IsShiftEnded;
    }

    public bool TrySpendMinutes(int minutes)
    {
        if (minutes <= 0)
        {
            return true;
        }

        if (IsShiftEnded)
        {
            return false;
        }

        int previousMinutes = currentMinutesFromMidnight;
        currentMinutesFromMidnight = Mathf.Min(ShiftEndMinutes, currentMinutesFromMidnight + minutes);

        if (currentMinutesFromMidnight != previousMinutes)
        {
            NotifyTimeChanged();
        }

        if (currentMinutesFromMidnight >= ShiftEndMinutes)
        {
            NotifyShiftEnded();
        }

        return true;
    }

    public static string FormatMinutes(int minutesFromMidnight)
    {
        int clampedMinutes = Mathf.Clamp(minutesFromMidnight, 0, 23 * 60 + 59);
        int hours = clampedMinutes / 60;
        int minutes = clampedMinutes % 60;
        return $"{hours:00}:{minutes:00}";
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        isReloadingScene = false;
        NotifyTimeChanged();
    }

    private void NotifyTimeChanged()
    {
        if (logTimeChanges)
        {
            Debug.Log($"WorkShiftTimeSystem | time changed to {CurrentTimeText}", this);
        }

        TimeChanged?.Invoke(currentMinutesFromMidnight);
    }

    private void ApplySceneConfiguration(WorkShiftTimeSystem source)
    {
        if (source == null)
        {
            return;
        }

        currentMinutesFromMidnight = source.currentMinutesFromMidnight;
        resetToShiftStartOnAwake = source.resetToShiftStartOnAwake;
        logTimeChanges = source.logTimeChanges;
        shiftEndedNotified = source.shiftEndedNotified;
    }

    private void NotifyShiftEnded()
    {
        if (shiftEndedNotified)
        {
            return;
        }

        shiftEndedNotified = true;
        Debug.Log("Work shift has ended.", this);
        ShiftEnded?.Invoke();
        AdvanceDayAndReloadCurrentScene();
    }

    private void AdvanceDayAndReloadCurrentScene()
    {
        if (isReloadingScene)
        {
            return;
        }

        isReloadingScene = true;

        if (DayCounterSystem.Instance != null)
        {
            DayCounterSystem.Instance.AdvanceDay();
        }

        ResetShift();

        Scene currentScene = SceneManager.GetActiveScene();
        if (!currentScene.IsValid() || !currentScene.isLoaded)
        {
            isReloadingScene = false;
            return;
        }

        SceneManager.LoadScene(currentScene.name);
    }
}
