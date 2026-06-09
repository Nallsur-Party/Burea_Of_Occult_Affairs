using UnityEngine;

[DisallowMultipleComponent]
public class WorkShiftClockPresenter : MonoBehaviour
{
    [Header("Scene Binding")]
    [SerializeField]
    private Transform hourHand;

    [SerializeField]
    private Transform minuteHand;

    [SerializeField, Min(0f)]
    private float minuteHandDegreesPerSecond = 360f;

    [SerializeField, Min(0f)]
    private float hourHandDegreesPerSecond = 90f;

    [SerializeField, Min(0f)]
    private float minuteHandMinTweenDuration = 0.2f;

    [SerializeField, Min(0f)]
    private float hourHandMinTweenDuration = 0.35f;

    [Header("Audio")]
    [SerializeField]
    private AudioSource clockAudioSource; // AudioSource, который будет проигрывать звук

    [SerializeField]
    private AudioClip shiftEndClip; // звук при окончании смены

    [SerializeField]
    private AudioClip shiftAlmostEndClip; // звук за N минут до конца (опционально)

    [SerializeField, Min(0)]
    private int warningMinutesBeforeEnd = 5; // за сколько минут предупредить
    private bool hasPlayedEndSound = false;
    private bool hasPlayedWarningSound = false;

    private WorkShiftTimeSystem timeSystem;
    private Quaternion hourHandBaseLocalRotation;
    private Quaternion minuteHandBaseLocalRotation;
    private bool hasCachedBaseRotation;
    private Quaternion targetHourLocalRotation;
    private Quaternion targetMinuteLocalRotation;
    private Quaternion hourHandTweenStartRotation;
    private Quaternion minuteHandTweenStartRotation;
    private float hourHandTweenDuration;
    private float minuteHandTweenDuration;
    private float hourHandTweenElapsed;
    private float minuteHandTweenElapsed;
    private bool isHourHandTweenActive;
    private bool isMinuteHandTweenActive;

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
        hasPlayedEndSound = false;
        hasPlayedWarningSound = false;
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
        RestartTweenFromCurrentVisualState();

        // Проверяем, не пора ли предупредить о скором окончании смены
        if (!hasPlayedWarningSound && shiftAlmostEndClip != null && clockAudioSource != null)
        {
            int minutesLeft = WorkShiftTimeSystem.ShiftEndMinutes - currentMinutesFromMidnight;
            if (minutesLeft > 0 && minutesLeft <= warningMinutesBeforeEnd)
            {
                clockAudioSource.PlayOneShot(shiftAlmostEndClip);
                hasPlayedWarningSound = true;
            }
        }
    }

    private void HandleShiftEnded()
    {
        UpdateTargetAngles(
            timeSystem != null
                ? timeSystem.CurrentMinutesFromMidnight
                : WorkShiftTimeSystem.ShiftEndMinutes
        );
        RestartTweenFromCurrentVisualState();

        // Воспроизвести звук окончания смены (только один раз)
        if (!hasPlayedEndSound && shiftEndClip != null && clockAudioSource != null)
        {
            clockAudioSource.PlayOneShot(shiftEndClip);
            hasPlayedEndSound = true;
        }
    }

    private void ResolveTimeSystem()
    {
        if (timeSystem != null)
        {
            return;
        }

        timeSystem =
            WorkShiftTimeSystem.Instance != null
                ? WorkShiftTimeSystem.Instance
                : Object.FindObjectOfType<WorkShiftTimeSystem>();
    }

    private void CacheBaseRotations()
    {
        if (hourHand != null)
        {
            hourHandBaseLocalRotation = hourHand.localRotation;
        }

        if (minuteHand != null)
        {
            minuteHandBaseLocalRotation = minuteHand.localRotation;
        }

        hasCachedBaseRotation = hourHand != null && minuteHand != null;
    }

    private void SyncHandsToGlobalTime()
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
            Debug.LogWarning(
                "WorkShiftClockPresenter needs hourHand and minuteHand assigned in the Inspector.",
                this
            );
            return;
        }

        if (!hasCachedBaseRotation)
        {
            CacheBaseRotations();
        }

        UpdateTargetAngles(timeSystem.CurrentMinutesFromMidnight);
        SnapToTarget();
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
            Debug.LogWarning(
                "WorkShiftClockPresenter needs hourHand and minuteHand assigned in the Inspector.",
                this
            );
            return;
        }

        if (!hasCachedBaseRotation)
        {
            CacheBaseRotations();
        }

        UpdateTargetAngles(timeSystem.CurrentMinutesFromMidnight);
        SnapToTarget();
    }

    private void Update()
    {
        if (!isHourHandTweenActive && !isMinuteHandTweenActive)
        {
            return;
        }

        AnimateTween(Time.deltaTime);
    }

    private void UpdateTargetAngles(int minutesFromMidnight)
    {
        if (hourHand == null || minuteHand == null)
        {
            return;
        }

        int clampedMinutes = Mathf.Clamp(
            minutesFromMidnight,
            WorkShiftTimeSystem.ShiftStartMinutes,
            WorkShiftTimeSystem.ShiftEndMinutes
        );
        int hour = clampedMinutes / 60;
        int minute = clampedMinutes % 60;

        float minuteAngle = minute * 6f;
        float hourAngle = ((hour % 12) + minute / 60f) * 30f;

        targetMinuteLocalRotation =
            minuteHandBaseLocalRotation * Quaternion.AngleAxis(-minuteAngle, Vector3.right);
        targetHourLocalRotation =
            hourHandBaseLocalRotation * Quaternion.AngleAxis(-hourAngle, Vector3.right);
    }

    private void SnapToTarget()
    {
        if (hourHand == null || minuteHand == null)
        {
            return;
        }

        minuteHand.localRotation = targetMinuteLocalRotation;
        hourHand.localRotation = targetHourLocalRotation;
        minuteHandTweenStartRotation = targetMinuteLocalRotation;
        hourHandTweenStartRotation = targetHourLocalRotation;
        minuteHandTweenElapsed = 0f;
        hourHandTweenElapsed = 0f;
        minuteHandTweenDuration = 0f;
        hourHandTweenDuration = 0f;
        isMinuteHandTweenActive = false;
        isHourHandTweenActive = false;
    }

    private void RestartTweenFromCurrentVisualState()
    {
        if (hourHand == null || minuteHand == null)
        {
            return;
        }

        minuteHandTweenStartRotation = minuteHand.localRotation;
        hourHandTweenStartRotation = hourHand.localRotation;

        minuteHandTweenElapsed = 0f;
        hourHandTweenElapsed = 0f;

        minuteHandTweenDuration = CalculateTweenDuration(
            minuteHandTweenStartRotation,
            targetMinuteLocalRotation,
            minuteHandDegreesPerSecond,
            minuteHandMinTweenDuration
        );
        hourHandTweenDuration = CalculateTweenDuration(
            hourHandTweenStartRotation,
            targetHourLocalRotation,
            hourHandDegreesPerSecond,
            hourHandMinTweenDuration
        );

        isMinuteHandTweenActive = minuteHandTweenDuration > 0f;
        isHourHandTweenActive = hourHandTweenDuration > 0f;

        if (!isMinuteHandTweenActive)
        {
            minuteHand.localRotation = targetMinuteLocalRotation;
        }

        if (!isHourHandTweenActive)
        {
            hourHand.localRotation = targetHourLocalRotation;
        }
    }

    private void AnimateTween(float deltaTime)
    {
        if (isMinuteHandTweenActive)
        {
            minuteHandTweenElapsed = Mathf.Min(
                minuteHandTweenElapsed + deltaTime,
                minuteHandTweenDuration
            );
            float minuteT =
                minuteHandTweenDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(minuteHandTweenElapsed / minuteHandTweenDuration);
            minuteT = EaseOutCubic(minuteT);
            minuteHand.localRotation = Quaternion.SlerpUnclamped(
                minuteHandTweenStartRotation,
                targetMinuteLocalRotation,
                minuteT
            );

            if (minuteHandTweenElapsed >= minuteHandTweenDuration)
            {
                minuteHand.localRotation = targetMinuteLocalRotation;
                isMinuteHandTweenActive = false;
            }
        }

        if (isHourHandTweenActive)
        {
            hourHandTweenElapsed = Mathf.Min(
                hourHandTweenElapsed + deltaTime,
                hourHandTweenDuration
            );
            float hourT =
                hourHandTweenDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(hourHandTweenElapsed / hourHandTweenDuration);
            hourT = EaseOutCubic(hourT);
            hourHand.localRotation = Quaternion.SlerpUnclamped(
                hourHandTweenStartRotation,
                targetHourLocalRotation,
                hourT
            );

            if (hourHandTweenElapsed >= hourHandTweenDuration)
            {
                hourHand.localRotation = targetHourLocalRotation;
                isHourHandTweenActive = false;
            }
        }
    }

    private float CalculateTweenDuration(
        Quaternion from,
        Quaternion to,
        float degreesPerSecond,
        float minDuration
    )
    {
        if (degreesPerSecond <= 0f)
        {
            return 0f;
        }

        float angle = Quaternion.Angle(from, to);
        if (angle <= Mathf.Epsilon)
        {
            return 0f;
        }

        return Mathf.Max(angle / degreesPerSecond, minDuration);
    }

    private static float EaseOutCubic(float t)
    {
        float oneMinusT = 1f - t;
        return 1f - (oneMinusT * oneMinusT * oneMinusT);
    }
}
