using UnityEngine;

[DisallowMultipleComponent]
public class DayCounterSystem : MonoBehaviour
{
    private static DayCounterSystem instance;

    [SerializeField, Min(1)] private int currentDay = 1;

    public static DayCounterSystem Instance => instance;
    public int CurrentDay => Mathf.Max(1, currentDay);

    public event System.Action<int> DayChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject systemObject = new GameObject(nameof(DayCounterSystem));
        systemObject.AddComponent<DayCounterSystem>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        currentDay = Mathf.Max(1, currentDay);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public int AdvanceDay()
    {
        currentDay = Mathf.Max(1, currentDay) + 1;
        DayChanged?.Invoke(CurrentDay);
        return CurrentDay;
    }

    public void ResetToDayOne()
    {
        currentDay = 1;
        DayChanged?.Invoke(CurrentDay);
    }
}
