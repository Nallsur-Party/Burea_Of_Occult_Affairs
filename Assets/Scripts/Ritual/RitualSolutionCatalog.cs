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
            Step(RitualItemType.SoapWithPlantain, RitualActionType.EquipOnNpc),
            Step(RitualItemType.LeadTablet, RitualActionType.ReadIncantation));

        Add("Привязанный паразит",
            Step(RitualItemType.GlassWithPencil, RitualActionType.CircleAroundNpc),
            Step(RitualItemType.SoapWithPlantain, RitualActionType.PlaceNearby));

        Add("Преследующая сущность",
            Step(RitualItemType.SoapWithPlantain, RitualActionType.PlaceNearby),
            Step(RitualItemType.LeadTablet, RitualActionType.CircleAroundNpc));

        Add("Подмена",
            Step(RitualItemType.LeadTablet, RitualActionType.TouchNpc),
            Step(RitualItemType.GildedIcon, RitualActionType.ReadIncantation));

        Add("Наблюдатель",
            Step(RitualItemType.SoapWithPlantain, RitualActionType.PlaceNearby),
            Step(RitualItemType.LeadTablet, RitualActionType.MarkGround));

        Add("Осознанная сделка",
            Step(RitualItemType.GildedIcon, RitualActionType.ReadIncantation),
            Step(RitualItemType.GlassWithPencil, RitualActionType.BreakItem));

        Add("Нарушенный контракт",
            Step(RitualItemType.GildedIcon, RitualActionType.HoldNearNpc),
            Step(RitualItemType.LeadTablet, RitualActionType.MarkGround));

        Add("Неосознанный контракт",
            Step(RitualItemType.SoapWithPlantain, RitualActionType.EquipOnNpc),
            Step(RitualItemType.GlassWithPencil, RitualActionType.BreakItem));

        Add("Классическое проклятие",
            Step(RitualItemType.SoapWithPlantain, RitualActionType.EquipOnNpc),
            Step(RitualItemType.LeadTablet, RitualActionType.MarkGround));

        Add("Наследственное проклятие",
            Step(RitualItemType.GlassWithPencil, RitualActionType.BreakItem),
            Step(RitualItemType.SoapWithPlantain, RitualActionType.EquipOnNpc));

        Add("Самонавязанное",
            Step(RitualItemType.SoapWithPlantain, RitualActionType.EquipOnNpc),
            Step(RitualItemType.LeadTablet, RitualActionType.TouchNpc));

        Add("Локальное проклятие",
            Step(RitualItemType.LeadTablet, RitualActionType.MarkGround),
            Step(RitualItemType.SoapWithPlantain, RitualActionType.PlaceNearby));

        Add("Предметное проклятие",
            Step(RitualItemType.LeadTablet, RitualActionType.HoldNearNpc),
            Step(RitualItemType.LeadTablet, RitualActionType.BreakItem));

        Add("Незакрытый ритуал",
            Step(RitualItemType.GildedIcon, RitualActionType.ReadIncantation),
            Step(RitualItemType.LeadTablet, RitualActionType.MarkGround));

        Add("Ошибка ритуала",
            Step(RitualItemType.LeadTablet, RitualActionType.MarkGround),
            Step(RitualItemType.GildedIcon, RitualActionType.ReadIncantation));

        Add("Чужой ритуал",
            Step(RitualItemType.SoapWithPlantain, RitualActionType.HoldNearNpc),
            Step(RitualItemType.GlassWithPencil, RitualActionType.BreakItem));

        Add("Искажение пространства",
            Step(RitualItemType.SoapWithPlantain, RitualActionType.PlaceNearby),
            Step(RitualItemType.LeadTablet, RitualActionType.MarkGround));

        Add("Искажение времени",
            Step(RitualItemType.GlassWithPencil, RitualActionType.EquipOnNpc),
            Step(RitualItemType.LeadTablet, RitualActionType.MarkGround));
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
