using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NewsProblemMapping
{
    [SerializeField] private string problemName;
    [SerializeField] private EventSeverity severityTier = EventSeverity.Minor;
    [SerializeField] private List<EventType> eventTypes = new List<EventType>();

    public string ProblemName => problemName;
    public EventSeverity SeverityTier => severityTier;
    public IReadOnlyList<EventType> EventTypes => eventTypes;

    public void SetProblemName(string value)
    {
        problemName = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public void SetSeverityTier(EventSeverity value)
    {
        severityTier = value;
    }

    public void SetEventTypes(IEnumerable<EventType> values)
    {
        eventTypes.Clear();

        if (values == null)
        {
            return;
        }

        foreach (EventType value in values)
        {
            if (Enum.IsDefined(typeof(EventType), value) && !eventTypes.Contains(value))
            {
                eventTypes.Add(value);
            }
        }
    }

    public bool TryPickEventType(out EventType eventType)
    {
        eventType = default(EventType);

        if (eventTypes == null || eventTypes.Count == 0)
        {
            return false;
        }

        List<EventType> candidates = new List<EventType>();
        for (int i = 0; i < eventTypes.Count; i++)
        {
            EventType candidate = eventTypes[i];
            if (Enum.IsDefined(typeof(EventType), candidate))
            {
                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        eventType = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return true;
    }

    public bool TryGetSeverityPool(out List<EventSeverity> pool)
    {
        pool = new List<EventSeverity>();

        switch (severityTier)
        {
            case EventSeverity.Minor:
                pool.Add(EventSeverity.Minor);
                break;
            case EventSeverity.Moderate:
                pool.Add(EventSeverity.Minor);
                pool.Add(EventSeverity.Moderate);
                break;
            case EventSeverity.Major:
                pool.Add(EventSeverity.Minor);
                pool.Add(EventSeverity.Moderate);
                pool.Add(EventSeverity.Major);
                break;
            case EventSeverity.Catastrophic:
                pool.Add(EventSeverity.Minor);
                pool.Add(EventSeverity.Moderate);
                pool.Add(EventSeverity.Major);
                pool.Add(EventSeverity.Catastrophic);
                break;
            default:
                pool.Add(EventSeverity.Minor);
                break;
        }

        return pool.Count > 0;
    }
}
