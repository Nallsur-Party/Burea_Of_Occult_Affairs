using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class WorkShiftTimedEventManager : MonoBehaviour
{
    [System.Serializable]
    public class TimedEvent
    {
        [SerializeField, Range(0, 23)] private int hour = 8;
        [SerializeField, Range(0, 59)] private int minute = 15;
        [SerializeField] private string label = "Event";
        [SerializeField] private UnityEvent onTriggered;
        [SerializeField, HideInInspector] private bool hasTriggered;

        public int TriggerMinutes => (hour * 60) + minute;
        public string Label => label;

        public bool ShouldTrigger(int currentMinutesFromMidnight)
        {
            return !hasTriggered && currentMinutesFromMidnight >= TriggerMinutes;
        }

        public void Trigger()
        {
            hasTriggered = true;
            onTriggered?.Invoke();
        }

        public void ResetState()
        {
            hasTriggered = false;
        }
    }

    [Header("Source")]
    [SerializeField] private bool autoBindOnEnable = true;
    [SerializeField] private bool logEventFires = true;
    [SerializeField] private bool logBindingState = false;

    [Header("Schedule")]
    [SerializeField] private List<TimedEvent> timedEvents = new List<TimedEvent>();

    private WorkShiftTimeSystem timeSystem;
    private int lastObservedMinutes = -1;
    private bool isSubscribed;

    private void Awake()
    {
        ResolveTimeSystem();
    }

    private void OnEnable()
    {
        if (autoBindOnEnable)
        {
            TryBindToTimeSystem();
        }

        EvaluateCurrentTime();
    }

    private void Update()
    {
        if (!isSubscribed)
        {
            TryBindToTimeSystem();
        }
    }

    private void OnDisable()
    {
        UnbindFromTimeSystem();
    }

    [ContextMenu("Evaluate Current Time")]
    public void EvaluateCurrentTime()
    {
        if (timeSystem == null)
        {
            ResolveTimeSystem();
        }

        if (timeSystem == null)
        {
            if (logBindingState)
            {
                Debug.LogWarning("TimedEventManager | no WorkShiftTimeSystem found.", this);
            }

            return;
        }

        if (logBindingState)
        {
            Debug.Log(
                $"TimedEventManager | evaluating at {WorkShiftTimeSystem.FormatMinutes(timeSystem.CurrentMinutesFromMidnight)}",
                this
            );
            LogSchedule();
        }

        HandleTimeChanged(timeSystem.CurrentMinutesFromMidnight);
    }

    [ContextMenu("Reset Timed Events")]
    public void ResetTimedEvents()
    {
        ResetAllEventStates();
        lastObservedMinutes = timeSystem != null ? timeSystem.CurrentMinutesFromMidnight : -1;
        EvaluateCurrentTime();
    }

    private void HandleTimeChanged(int currentMinutesFromMidnight)
    {
        if (lastObservedMinutes >= 0 && currentMinutesFromMidnight < lastObservedMinutes)
        {
            ResetAllEventStates();
        }

        if (logBindingState)
        {
            Debug.Log(
                $"TimedEventManager | time update {WorkShiftTimeSystem.FormatMinutes(currentMinutesFromMidnight)}",
                this
            );
        }

        lastObservedMinutes = currentMinutesFromMidnight;

        if (timedEvents == null)
        {
            return;
        }

        for (int i = 0; i < timedEvents.Count; i++)
        {
            TimedEvent timedEvent = timedEvents[i];
            if (timedEvent == null || !timedEvent.ShouldTrigger(currentMinutesFromMidnight))
            {
                continue;
            }

            if (logEventFires)
            {
                Debug.Log(
                    $"TimedEventManager | fired '{timedEvent.Label}' at {WorkShiftTimeSystem.FormatMinutes(currentMinutesFromMidnight)}",
                    this
                );
            }
            timedEvent.Trigger();
        }
    }

    private void HandleShiftEnded()
    {
        EvaluateCurrentTime();
    }

    private void TryBindToTimeSystem()
    {
        if (isSubscribed)
        {
            return;
        }

        ResolveTimeSystem();
        if (timeSystem == null)
        {
            return;
        }

        timeSystem.TimeChanged += HandleTimeChanged;
        timeSystem.ShiftEnded += HandleShiftEnded;
        isSubscribed = true;
        lastObservedMinutes = timeSystem.CurrentMinutesFromMidnight;

        if (logBindingState)
        {
            Debug.Log(
                $"TimedEventManager | bound at {WorkShiftTimeSystem.FormatMinutes(lastObservedMinutes)}",
                this
            );
        }
    }

    private void UnbindFromTimeSystem()
    {
        if (timeSystem != null && isSubscribed)
        {
            timeSystem.TimeChanged -= HandleTimeChanged;
            timeSystem.ShiftEnded -= HandleShiftEnded;
        }

        isSubscribed = false;

        if (logBindingState)
        {
            Debug.Log("TimedEventManager | unbound from WorkShiftTimeSystem", this);
        }
    }

    private void ResolveTimeSystem()
    {
        timeSystem = WorkShiftTimeSystem.Instance;
    }

    private void ResetAllEventStates()
    {
        if (timedEvents == null)
        {
            return;
        }

        for (int i = 0; i < timedEvents.Count; i++)
        {
            timedEvents[i]?.ResetState();
        }
    }

    private void LogSchedule()
    {
        if (timedEvents == null || timedEvents.Count == 0)
        {
            Debug.Log("TimedEventManager | schedule is empty.", this);
            return;
        }

        for (int i = 0; i < timedEvents.Count; i++)
        {
            TimedEvent timedEvent = timedEvents[i];
            if (timedEvent == null)
            {
                Debug.Log($"TimedEventManager | event[{i}] is null.", this);
                continue;
            }

            Debug.Log(
                $"TimedEventManager | event[{i}] '{timedEvent.Label}' at {FormatTriggerMinutes(timedEvent.TriggerMinutes)}",
                this
            );
        }
    }

    private static string FormatTriggerMinutes(int minutesFromMidnight)
    {
        int clampedMinutes = Mathf.Clamp(minutesFromMidnight, 0, 23 * 60 + 59);
        int hours = clampedMinutes / 60;
        int minutes = clampedMinutes % 60;
        return $"{hours:00}:{minutes:00}";
    }
}
