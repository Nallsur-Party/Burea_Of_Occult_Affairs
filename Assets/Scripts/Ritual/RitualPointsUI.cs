using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class RitualPointsUI : MonoBehaviour
{
    [SerializeField] private int startingPoints = 0;

    private TMP_Text pointsText;
    private int currentPoints;

    public int CurrentPoints => currentPoints;

    private void Awake()
    {
        pointsText = GetComponent<TMP_Text>();
        currentPoints = ParseInitialPoints(pointsText != null ? pointsText.text : null, startingPoints);
        Refresh();
    }

    public void SetPoints(int points)
    {
        currentPoints = Mathf.Max(0, points);
        Refresh();
    }

    public void AddPoints(int points)
    {
        if (points == 0)
        {
            return;
        }

        currentPoints = Mathf.Max(0, currentPoints + points);
        Refresh();
    }

    private void Refresh()
    {
        if (pointsText != null)
        {
            pointsText.text = currentPoints.ToString();
        }
    }

    private static int ParseInitialPoints(string text, int fallback)
    {
        if (!string.IsNullOrWhiteSpace(text) && int.TryParse(text, out int parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
