using System;
using System.Collections.Generic;
using UnityEngine;

public enum EventSeverity
{
    Minor,
    Moderate,
    Major,
    Catastrophic
}

public enum EventType
{
    Complaint,
    Conflict,
    Disorientation,
    Missing,
    Hospitalization,
    ApartmentIncident,
    DistrictIncident,
    BPDResponse,
    MediaReport,
    Death
}

[Serializable]
public class ProblemTextData
{
    [SerializeField] private string gender;
    [SerializeField] private string text;

    public string Gender => gender;
    public string Text => text;

    public ProblemTextData(string gender, string text)
    {
        this.gender = NormalizeGender(gender);
        this.text = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
    }

    private static string NormalizeGender(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "any";
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized == "male" || normalized == "female" || normalized == "any")
        {
            return normalized;
        }

        return "any";
    }
}

[Serializable]
public class ProblemData
{
    [SerializeField] private string name;
    [SerializeField] private List<ProblemTextData> texts = new List<ProblemTextData>();

    public string Name => name;
    public IReadOnlyList<ProblemTextData> Texts => texts;

    public ProblemData(string name, IEnumerable<ProblemTextData> texts)
    {
        this.name = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        this.texts = texts != null ? new List<ProblemTextData>(texts) : new List<ProblemTextData>();
    }
}

[Serializable]
public class TemplateData
{
    [SerializeField] private EventType eventType;
    [SerializeField] private EventSeverity severity;
    [SerializeField] private string gender;
    [SerializeField] private List<string> lines = new List<string>();

    public EventType EventType => eventType;
    public EventSeverity Severity => severity;
    public string Gender => gender;
    public IReadOnlyList<string> Lines => lines;

    public TemplateData(EventType eventType, EventSeverity severity, string gender, IEnumerable<string> lines)
    {
        this.eventType = eventType;
        this.severity = severity;
        this.gender = NormalizeGender(gender);
        this.lines = lines != null ? new List<string>(lines) : new List<string>();
    }

    private static string NormalizeGender(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "any";
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized == "male" || normalized == "female" || normalized == "any")
        {
            return normalized;
        }

        return "any";
    }
}

