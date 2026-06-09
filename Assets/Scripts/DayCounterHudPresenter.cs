using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class DayCounterHudPresenter : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private string prefix = "Day ";

    private DayCounterSystem dayCounterSystem;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        ResolveDayCounterSystem();

        if (dayCounterSystem != null)
        {
            dayCounterSystem.DayChanged += HandleDayChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (dayCounterSystem != null)
        {
            dayCounterSystem.DayChanged -= HandleDayChanged;
        }
    }

    [ContextMenu("Refresh Day HUD")]
    public void Refresh()
    {
        ResolveDayCounterSystem();
        if (targetText == null)
        {
            return;
        }

        int day = dayCounterSystem != null ? dayCounterSystem.CurrentDay : 1;
        targetText.text = $"{prefix}{day}";
    }

    private void HandleDayChanged(int currentDay)
    {
        if (targetText != null)
        {
            targetText.text = $"{prefix}{currentDay}";
        }
    }

    private void ResolveDayCounterSystem()
    {
        if (dayCounterSystem != null)
        {
            return;
        }

        dayCounterSystem = DayCounterSystem.Instance != null
            ? DayCounterSystem.Instance
            : Object.FindObjectOfType<DayCounterSystem>();
    }
}
