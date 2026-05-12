using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RitualSolutionCatalog", menuName = "Bureau Of Occult Affairs/Ritual Solution Catalog")]
public class RitualSolutionCatalog : ScriptableObject
{
    [SerializeField] private List<RitualSolutionDefinition> solutions = new List<RitualSolutionDefinition>();

    private Dictionary<string, RitualSolutionDefinition> solutionsByProblemName;

    public IReadOnlyList<RitualSolutionDefinition> Solutions => solutions;

    private void OnEnable()
    {
        if (solutions == null)
        {
            solutions = new List<RitualSolutionDefinition>();
        }

        if (solutions.Count == 0)
        {
            PopulateDefaults();
        }

        RebuildLookup();
    }

    public bool TryGetSolution(string problemName, out RitualSolutionDefinition solution)
    {
        if (string.IsNullOrWhiteSpace(problemName))
        {
            solution = null;
            return false;
        }

        if (solutionsByProblemName == null)
        {
            RebuildLookup();
        }

        return solutionsByProblemName.TryGetValue(problemName.Trim(), out solution);
    }

    public static RitualSolutionCatalog CreateRuntimeDefault()
    {
        RitualSolutionCatalog catalog = CreateInstance<RitualSolutionCatalog>();
        catalog.hideFlags = HideFlags.HideAndDontSave;
        catalog.PopulateDefaults();
        catalog.RebuildLookup();
        return catalog;
    }

    [ContextMenu("Populate Ritual Defaults")]
    public void PopulateDefaults()
    {
        solutions.Clear();

        Add("Одержимость",
            Step(RitualItemType.Amulet, RitualActionType.EquipOnNpc),
            Step(RitualItemType.Cross, RitualActionType.ReadIncantation));

        Add("Привязанный паразит",
            Step(RitualItemType.Necklace, RitualActionType.CircleAroundNpc),
            Step(RitualItemType.Amulet, RitualActionType.PlaceNearby));

        Add("Преследующая сущность",
            Step(RitualItemType.Amulet, RitualActionType.PlaceNearby),
            Step(RitualItemType.Wand, RitualActionType.CircleAroundNpc));

        Add("Подмена",
            Step(RitualItemType.Cross, RitualActionType.TouchNpc),
            Step(RitualItemType.Grimoire, RitualActionType.ReadIncantation));

        Add("Наблюдатель",
            Step(RitualItemType.Amulet, RitualActionType.PlaceNearby),
            Step(RitualItemType.Wand, RitualActionType.MarkGround));

        Add("Осознанная сделка",
            Step(RitualItemType.Grimoire, RitualActionType.ReadIncantation),
            Step(RitualItemType.Necklace, RitualActionType.BreakItem));

        Add("Нарушенный контракт",
            Step(RitualItemType.Grimoire, RitualActionType.HoldNearNpc),
            Step(RitualItemType.Cross, RitualActionType.MarkGround));

        Add("Неосознанный контракт",
            Step(RitualItemType.Amulet, RitualActionType.EquipOnNpc),
            Step(RitualItemType.Necklace, RitualActionType.BreakItem));

        Add("Классическое проклятие",
            Step(RitualItemType.Amulet, RitualActionType.EquipOnNpc),
            Step(RitualItemType.Cross, RitualActionType.MarkGround));

        Add("Наследственное проклятие",
            Step(RitualItemType.Necklace, RitualActionType.BreakItem),
            Step(RitualItemType.Amulet, RitualActionType.EquipOnNpc));

        Add("Самонавязанное",
            Step(RitualItemType.Amulet, RitualActionType.EquipOnNpc),
            Step(RitualItemType.Wand, RitualActionType.TouchNpc));

        Add("Локальное проклятие",
            Step(RitualItemType.Cross, RitualActionType.MarkGround),
            Step(RitualItemType.Amulet, RitualActionType.PlaceNearby));

        Add("Предметное проклятие",
            Step(RitualItemType.Cross, RitualActionType.HoldNearNpc),
            Step(RitualItemType.Wand, RitualActionType.BreakItem));

        Add("Незакрытый ритуал",
            Step(RitualItemType.Grimoire, RitualActionType.ReadIncantation),
            Step(RitualItemType.Wand, RitualActionType.MarkGround));

        Add("Ошибка ритуала",
            Step(RitualItemType.Cross, RitualActionType.MarkGround),
            Step(RitualItemType.Grimoire, RitualActionType.ReadIncantation));

        Add("Чужой ритуал",
            Step(RitualItemType.Amulet, RitualActionType.HoldNearNpc),
            Step(RitualItemType.Necklace, RitualActionType.BreakItem));

        Add("Искажение пространства",
            Step(RitualItemType.Amulet, RitualActionType.PlaceNearby),
            Step(RitualItemType.Cross, RitualActionType.MarkGround));

        Add("Искажение времени",
            Step(RitualItemType.Necklace, RitualActionType.EquipOnNpc),
            Step(RitualItemType.Wand, RitualActionType.MarkGround));
    }

    private void Add(string problemName, params RitualStepDefinition[] steps)
    {
        solutions.Add(new RitualSolutionDefinition(problemName, steps));
    }

    private static RitualStepDefinition Step(RitualItemType item, RitualActionType action)
    {
        return new RitualStepDefinition(item, action);
    }

    private void RebuildLookup()
    {
        solutionsByProblemName = new Dictionary<string, RitualSolutionDefinition>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < solutions.Count; i++)
        {
            RitualSolutionDefinition solution = solutions[i];
            if (solution == null || string.IsNullOrWhiteSpace(solution.ProblemName))
            {
                continue;
            }

            solutionsByProblemName[solution.ProblemName.Trim()] = solution;
        }
    }
}
