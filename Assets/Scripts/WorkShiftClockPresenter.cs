using UnityEngine;

[DisallowMultipleComponent]
public class WorkShiftClockPresenter : MonoBehaviour
{
    [Header("Scene Binding")]
    [SerializeField] private Transform hourHand;
    [SerializeField] private Transform minuteHand;

    private WorkShiftTimeSystem timeSystem;
    private float hourHandBaseX;
    private float minuteHandBaseX;
    private bool hasCachedBaseRotation;

    private void Awake()
    {
        ResolveTimeSystem();
        CacheBaseRotations();
    }

    private void OnEnable()
    {
        if (timeSystem == null)
        {
            ResolveTimeSystem();
        }

        if (timeSystem != null)
        {
            timeSystem.TimeChanged += HandleTimeChanged;
            timeSystem.ShiftEnded += HandleShiftEnded;
        }

        RefreshHands();
    }

    private void Start()
    {
        RefreshHands();
    }

    private void OnDisable()
    {
        if (timeSystem != null)
        {
            timeSystem.TimeChanged -= HandleTimeChanged;
            timeSystem.ShiftEnded -= HandleShiftEnded;
        }
    }

    private void HandleTimeChanged(int currentMinutesFromMidnight)
    {
        RefreshHands();
    }

    private void HandleShiftEnded()
    {
        RefreshHands();
    }

    private void ResolveTimeSystem()
    {
        if (timeSystem != null)
        {
            return;
        }

        timeSystem = WorkShiftTimeSystem.Instance != null
            ? WorkShiftTimeSystem.Instance
            : Object.FindObjectOfType<WorkShiftTimeSystem>();
    }

    private void CacheBaseRotations()
    {
        if (hourHand != null)
        {
            hourHandBaseX = hourHand.eulerAngles.z;
        }

        if (minuteHand != null)
        {
            minuteHandBaseX = minuteHand.eulerAngles.z;
        }

        hasCachedBaseRotation = hourHand != null && minuteHand != null;
    }

    private void RefreshHands()
    {
        if (timeSystem == null)
        {
            ResolveTimeSystem();
        }

        if (timeSystem == null)
        {
            return;
        }

        if (hourHand == null || minuteHand == null)
        {
            Debug.LogWarning("WorkShiftClockPresenter needs hourHand and minuteHand assigned in the Inspector.", this);
            return;
        }

        if (!hasCachedBaseRotation)
        {
            CacheBaseRotations();
        }

        ApplyHandRotations(timeSystem.CurrentMinutesFromMidnight);
    }

    private void ApplyHandRotations(int minutesFromMidnight)
    {
        if (hourHand == null || minuteHand == null)
        {
            return;
        }

        int clampedMinutes = Mathf.Clamp(minutesFromMidnight, WorkShiftTimeSystem.ShiftStartMinutes, WorkShiftTimeSystem.ShiftEndMinutes);
        int hour = clampedMinutes / 60;
        int minute = clampedMinutes % 60;

        float minuteAngle = minute * 6f;
        float hourAngle = ((hour % 12) + minute / 60f) * 30f;

        Vector3 minuteEuler = minuteHand.eulerAngles;
        Vector3 hourEuler = hourHand.eulerAngles;

        minuteEuler.z = minuteHandBaseX - minuteAngle;
        hourEuler.z = hourHandBaseX - hourAngle;

        minuteHand.eulerAngles = minuteEuler;
        hourHand.eulerAngles = hourEuler;
    }
}
