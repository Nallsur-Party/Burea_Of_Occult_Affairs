using System;
using System.Collections.Generic;

[Serializable]
public class RitualSolutionDefinition
{
    public string ProblemName;
    public List<RitualStepDefinition> Steps = new List<RitualStepDefinition>();

    public RitualSolutionDefinition()
    {
    }

    public RitualSolutionDefinition(string problemName, params RitualStepDefinition[] steps)
    {
        ProblemName = problemName;

        if (steps == null)
        {
            return;
        }

        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] != null)
            {
                Steps.Add(steps[i]);
            }
        }
    }
}
