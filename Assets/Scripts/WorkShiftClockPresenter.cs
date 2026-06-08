using UnityEngine;

[DisallowMultipleComponent]
public class WorkShiftClockPresenter : MonoBehaviour
{
    [Header("Scene Binding")]
    [SerializeField] private Transform hourHand;
    [SerializeField] private Transform minuteHand;
    [SerializeField, Min(0f)] private float smoothSpeed = 8f;

    private WorkShiftTimeSystem timeSystem;
    private float hourHandBaseZ;
    private float minuteHandBaseZ;
    private bool hasCachedBaseRotation;
    private float targetHourZ;
    private float targetMinuteZ;
    private bool hasTargetAngles;

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
        UpdateTargetAngles(currentMinutesFromMidnight);
    }

    private void HandleShiftEnded()
    {
        UpdateTargetAngles(timeSystem != null ? timeSystem.CurrentMinutesFromMidnight : WorkShiftTimeSystem.ShiftEndMinutes);
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
            hourHandBaseZ = hourHand.eulerAngles.z;
        }

        if (minuteHand != null)
        {
            minuteHandBaseZ = minuteHand.eulerAngles.z;
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

        UpdateTargetAngles(timeSystem.CurrentMinutesFromMidnight);
        ApplySmoothedRotation(true);
    }

    private void Update()
    {
        if (!hasTargetAngles)
        {
            return;
        }

        ApplySmoothedRotation(false);
    }

    private void UpdateTargetAngles(int minutesFromMidnight)
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

        targetMinuteZ = minuteHandBaseZ - minuteAngle;
        targetHourZ = hourHandBaseZ - hourAngle;
        hasTargetAngles = true;
    }

    private void ApplySmoothedRotation(bool snap)
    {
        if (hourHand == null || minuteHand == null)
        {
            return;
        }

        Vector3 minuteEuler = minuteHand.eulerAngles;
        Vector3 hourEuler = hourHand.eulerAngles;

        if (snap || smoothSpeed <= 0f)
        {
            minuteEuler.z = targetMinuteZ;
            hourEuler.z = targetHourZ;
        }
        else
        {
            float deltaTime = Time.deltaTime * smoothSpeed;
            minuteEuler.z = Mathf.LerpAngle(minuteEuler.z, targetMinuteZ, deltaTime);
            hourEuler.z = Mathf.LerpAngle(hourEuler.z, targetHourZ, deltaTime);
        }

        minuteHand.eulerAngles = minuteEuler;
        hourHand.eulerAngles = hourEuler;
    }
}
