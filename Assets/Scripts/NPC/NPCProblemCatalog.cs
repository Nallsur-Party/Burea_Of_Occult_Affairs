using System;
using System.Collections.Generic;

public class NPCProblemCatalog
{
    private readonly Dictionary<string, NPCProblemDefinition> problemsByName;
    private readonly List<NPCProblemDefinition> problems;

    public IReadOnlyList<NPCProblemDefinition> Problems => problems;

    public NPCProblemCatalog(IEnumerable<NPCProblemDefinition> loadedProblems)
    {
        problems = loadedProblems != null ? new List<NPCProblemDefinition>(loadedProblems) : new List<NPCProblemDefinition>();
        problemsByName = new Dictionary<string, NPCProblemDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (NPCProblemDefinition problem in problems)
        {
            if (problem == null || string.IsNullOrWhiteSpace(problem.Name))
            {
                continue;
            }

            problemsByName[problem.Name] = problem;
        }
    }

    public bool TryGetProblem(string problemName, out NPCProblemDefinition problem)
    {
        if (string.IsNullOrWhiteSpace(problemName))
        {
            problem = null;
            return false;
        }

        return problemsByName.TryGetValue(problemName.Trim(), out problem);
    }
}
