using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RitualSolutionCatalog", menuName = "Bureau Of Occult Affairs/Ritual Solution Catalog")]
public class RitualSolutionCatalog : ScriptableObject
{
    [SerializeField] private TextAsset ritualSolutionsXml;
    [SerializeField] private List<RitualSolutionDefinition> solutions = new List<RitualSolutionDefinition>();

    private Dictionary<string, RitualSolutionDefinition> solutionsByProblemName;
    private Dictionary<string, string> legacyAliasesByProblemName;

    public IReadOnlyList<RitualSolutionDefinition> Solutions => solutions;

    private void OnEnable()
    {
        if (solutions == null)
        {
            solutions = new List<RitualSolutionDefinition>();
        }

        if (TryLoadFromXml())
        {
            RebuildLookup();
            return;
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

        string normalizedProblemName = NormalizeProblemName(problemName);
        if (solutionsByProblemName.TryGetValue(normalizedProblemName, out solution))
        {
            return true;
        }

        if (legacyAliasesByProblemName != null
            && legacyAliasesByProblemName.TryGetValue(normalizedProblemName, out string canonicalProblemName)
            && !string.IsNullOrWhiteSpace(canonicalProblemName)
            && solutionsByProblemName.TryGetValue(canonicalProblemName, out solution))
        {
            return true;
        }

        if (TryGetSolutionByLooseName(normalizedProblemName, out solution))
        {
            return true;
        }

        solution = null;
        return false;
    }

    public static RitualSolutionCatalog CreateRuntimeDefault()
    {
        RitualSolutionCatalog catalog = CreateInstance<RitualSolutionCatalog>();
        catalog.hideFlags = HideFlags.HideAndDontSave;
        catalog.PopulateDefaults();
        catalog.RebuildLookup();
        return catalog;
    }

    public static RitualSolutionCatalog CreateRuntimeFromXml(TextAsset xmlAsset)
    {
        return RitualSolutionsLoader.Load(xmlAsset);
    }

    public void SetSolutions(List<RitualSolutionDefinition> newSolutions)
    {
        solutions = newSolutions != null ? newSolutions : new List<RitualSolutionDefinition>();
        RebuildLookup();
    }

    [ContextMenu("Populate Ritual Defaults")]
    public void PopulateDefaults()
    {
        solutions.Clear();

        Add(
            "Одержимость",
            Step(0, RitualItemType.SoapWithPlantain, RitualActionType.EquipOnNpc, "Надеть оберег", "Надеть защитный предмет на NPC"),
            Step(1, RitualItemType.LeadTablet, RitualActionType.ReadIncantation, "Прочитать формулу", "Запустить ритуал чтением текста")
        );

        Add(
            "Привязанный паразит",
            Step(0, RitualItemType.GlassWithPencil, RitualActionType.CircleAroundNpc, "Очертить контур", "Отделить паразита от носителя"),
            Step(1, RitualItemType.SoapWithPlantain, RitualActionType.PlaceNearby, "Оставить очищение", "Стабилизировать след")
        );

        Add(
            "Преследующая сущность",
            Step(0, RitualItemType.SoapWithPlantain, RitualActionType.PlaceNearby, "Оставить защиту", "Снять давление присутствия"),
            Step(1, RitualItemType.LeadTablet, RitualActionType.CircleAroundNpc, "Замкнуть круг", "Сдержать след в границе")
        );

        Add(
            "Подмена",
            Step(0, RitualItemType.LeadTablet, RitualActionType.TouchNpc, "Коснуться следа", "Проверить подменённую форму"),
            Step(1, RitualItemType.GildedIcon, RitualActionType.ReadIncantation, "Прочитать формулу", "Вернуть исходный образ")
        );

        Add(
            "Наблюдатель",
            Step(0, RitualItemType.SoapWithPlantain, RitualActionType.PlaceNearby, "Оставить защиту", "Снизить давление наблюдения"),
            Step(1, RitualItemType.LeadTablet, RitualActionType.MarkGround, "Нанести знак", "Зафиксировать точку наблюдения")
        );

        Add(
            "Осознанная сделка",
            Step(0, RitualItemType.GildedIcon, RitualActionType.ReadIncantation, "Проговорить формулу", "Разорвать согласие"),
            Step(1, RitualItemType.GlassWithPencil, RitualActionType.BreakItem, "Сломать носитель", "Уничтожить канал сделки")
        );

        Add(
            "Нарушенный контракт",
            Step(0, RitualItemType.GildedIcon, RitualActionType.HoldNearNpc, "Поднести символ", "Активировать договор"),
            Step(1, RitualItemType.LeadTablet, RitualActionType.MarkGround, "Нанести знак", "Закрепить пересмотр условий")
        );

        Add(
            "Неосознанный контракт",
            Step(0, RitualItemType.SoapWithPlantain, RitualActionType.EquipOnNpc, "Наложить защиту", "Снять автоматическую привязку"),
            Step(1, RitualItemType.GlassWithPencil, RitualActionType.BreakItem, "Сломать носитель", "Прервать скрытое условие")
        );

        Add(
            "Классическое проклятие",
            Step(0, RitualItemType.SoapWithPlantain, RitualActionType.EquipOnNpc, "Наложить защиту", "Защитить цель от воздействия"),
            Step(1, RitualItemType.LeadTablet, RitualActionType.MarkGround, "Нанести знак", "Закрыть проклятие в границе")
        );

        Add(
            "Наследственное проклятие",
            Step(0, RitualItemType.GlassWithPencil, RitualActionType.BreakItem, "Разорвать линию", "Сломать передачу"),
            Step(1, RitualItemType.SoapWithPlantain, RitualActionType.EquipOnNpc, "Наложить защиту", "Закрепить новый след")
        );

        Add(
            "Самонавязанное",
            Step(0, RitualItemType.SoapWithPlantain, RitualActionType.EquipOnNpc, "Наложить защиту", "Снять самоподдержку"),
            Step(1, RitualItemType.LeadTablet, RitualActionType.TouchNpc, "Коснуться следа", "Закрепить отмену")
        );

        Add(
            "Локальное проклятие",
            Step(0, RitualItemType.LeadTablet, RitualActionType.MarkGround, "Нанести знак", "Обозначить границу"),
            Step(1, RitualItemType.SoapWithPlantain, RitualActionType.PlaceNearby, "Оставить очищение", "Стабилизировать точку")
        );

        Add(
            "Предметное проклятие",
            Step(0, RitualItemType.LeadTablet, RitualActionType.HoldNearNpc, "Поднести знак", "Выявить источник"),
            Step(1, RitualItemType.LeadTablet, RitualActionType.BreakItem, "Сломать носитель", "Уничтожить предметный канал")
        );

        Add(
            "Незакрытый ритуал",
            Step(0, RitualItemType.GildedIcon, RitualActionType.ReadIncantation, "Прочитать формулу", "Завершить незакрытый цикл"),
            Step(1, RitualItemType.LeadTablet, RitualActionType.MarkGround, "Нанести знак", "Поставить финальную точку")
        );

        Add(
            "Ошибка ритуала",
            Step(0, RitualItemType.LeadTablet, RitualActionType.MarkGround, "Нанести знак", "Сбросить неверный след"),
            Step(1, RitualItemType.GildedIcon, RitualActionType.ReadIncantation, "Прочитать формулу", "Переписать последовательность")
        );

        Add(
            "Чужой ритуал",
            Step(0, RitualItemType.SoapWithPlantain, RitualActionType.HoldNearNpc, "Поднести защиту", "Перехватить чужое влияние"),
            Step(1, RitualItemType.GlassWithPencil, RitualActionType.BreakItem, "Разрушить носитель", "Снять чужой контур")
        );

        Add(
            "Искажение пространства",
            Step(0, RitualItemType.SoapWithPlantain, RitualActionType.PlaceNearby, "Оставить защиту", "Стабилизировать среду"),
            Step(1, RitualItemType.LeadTablet, RitualActionType.MarkGround, "Нанести знак", "Закрепить пространство")
        );

        Add(
            "Искажение времени",
            Step(0, RitualItemType.GlassWithPencil, RitualActionType.EquipOnNpc, "Наложить предмет", "Синхронизировать след"),
            Step(1, RitualItemType.LeadTablet, RitualActionType.MarkGround, "Нанести знак", "Выровнять временную петлю")
        );
    }

    private void Add(string problemName, params RitualStepDefinition[] steps)
    {
        solutions.Add(new RitualSolutionDefinition(problemName, steps));
    }

    private static RitualStepDefinition Step(
        int index,
        RitualItemType item,
        RitualActionType action,
        string title = null,
        string description = null)
    {
        return new RitualStepDefinition(index, item, action, title, description);
    }

    private bool TryLoadFromXml()
    {
        if (ritualSolutionsXml == null)
        {
            ritualSolutionsXml = Resources.Load<TextAsset>("Ritual/RitualSolutions");
        }

        if (ritualSolutionsXml == null || string.IsNullOrWhiteSpace(ritualSolutionsXml.text))
        {
            return false;
        }

        RitualSolutionCatalog parsed = RitualSolutionsLoader.Load(ritualSolutionsXml);
        if (parsed == null || parsed.Solutions == null || parsed.Solutions.Count == 0)
        {
            return false;
        }

        solutions = new List<RitualSolutionDefinition>(parsed.Solutions);
        return true;
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

            solutionsByProblemName[NormalizeProblemName(solution.ProblemName)] = solution;
        }

        EnsureLegacyAliases();
    }

    private void EnsureLegacyAliases()
    {
        if (legacyAliasesByProblemName == null)
        {
            legacyAliasesByProblemName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        if (legacyAliasesByProblemName.Count > 0)
        {
            return;
        }

        RegisterLegacyAlias("Повтор", "Эффект наложения");
        RegisterLegacyAlias("Потерянный маршрут", "Забытая комната");
        RegisterLegacyAlias("Незавершённый разговор", "Шумовой след");
        RegisterLegacyAlias("Забытая встреча", "Резонанс памяти");
        RegisterLegacyAlias("Пропавшее намерение", "Нарушенный контракт");
        RegisterLegacyAlias("Расслоение", "Пространственный двойник");
        RegisterLegacyAlias("Отпечаток", "Инородный отпечаток");
        RegisterLegacyAlias("Зеркальный", "Проекция");
        RegisterLegacyAlias("Смещение", "Пространственный сдвиг");
        RegisterLegacyAlias("Эхо", "Резонанс памяти");
        RegisterLegacyAlias("Привязка", "Привязанный паразит");
        RegisterLegacyAlias("Сбой личности", "Подмена");
        RegisterLegacyAlias("Зов", "Холодный зов");
        RegisterLegacyAlias("Петля", "Точка возврата");
        RegisterLegacyAlias("Десинхронизация", "Искажение времени");
    }

    private void RegisterLegacyAlias(string legacyName, string canonicalName)
    {
        string legacyKey = NormalizeProblemName(legacyName);
        string canonicalKey = NormalizeProblemName(canonicalName);

        if (string.IsNullOrWhiteSpace(legacyKey) || string.IsNullOrWhiteSpace(canonicalKey))
        {
            return;
        }

        legacyAliasesByProblemName[legacyKey] = canonicalKey;
    }

    private bool TryGetSolutionByLooseName(string problemName, out RitualSolutionDefinition solution)
    {
        solution = null;

        if (string.IsNullOrWhiteSpace(problemName) || solutionsByProblemName == null || solutionsByProblemName.Count == 0)
        {
            return false;
        }

        foreach (KeyValuePair<string, RitualSolutionDefinition> pair in solutionsByProblemName)
        {
            if (MatchesLoosely(problemName, pair.Key))
            {
                solution = pair.Value;
                return solution != null;
            }
        }

        if (legacyAliasesByProblemName != null)
        {
            foreach (KeyValuePair<string, string> pair in legacyAliasesByProblemName)
            {
                if (!MatchesLoosely(problemName, pair.Key))
                {
                    continue;
                }

                if (solutionsByProblemName.TryGetValue(pair.Value, out solution))
                {
                    return solution != null;
                }
            }
        }

        return false;
    }

    private static bool MatchesLoosely(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return left.IndexOf(right, StringComparison.OrdinalIgnoreCase) >= 0
            || right.IndexOf(left, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string NormalizeProblemName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Trim();
        normalized = normalized.Trim('\"', '\'', '«', '»', 'Â');
        normalized = normalized.Replace("Ã‚Â«", string.Empty).Replace("Ã‚Â»", string.Empty);
        return normalized.Trim();
    }
}
