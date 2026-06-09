public static class RitualItemExtensions
{
    public static string GetDisplayName(this RitualItemType item)
    {
        switch (item)
        {
            case RitualItemType.СтаканСКарандашом:
                return "Стакан с карандашом";
            case RitualItemType.МылоСПодорожником:
                return "Мыло с подорожником";
            case RitualItemType.ЗолоченаяИкона:
                return "Золоченая икона";
            case RitualItemType.СвинцовыйПланшет:
                return "Свинцовый планшет";
            default:
                return item.ToString();
        }
    }

    public static string GetDescription(this RitualItemType item)
    {
        switch (item)
        {
            case RitualItemType.СтаканСКарандашом:
                return "Поднести к рту, постучать карандашом, держать перед лицом.";
            case RitualItemType.МылоСПодорожником:
                return "Круговые движения мышью для намыливания, приложить к точке.";
            case RitualItemType.ЗолоченаяИкона:
                return "Удерживать перед собой и вращать, как фонарик, чтобы просветить комнату.";
            case RitualItemType.СвинцовыйПланшет:
                return "Поднести к пациенту и следить за прогресс-баром.";
            default:
                return item.ToString();
        }
    }
}

