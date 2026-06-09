public static class RitualItemTypeExtensions
{
    public static string GetDisplayName(this RitualItemType item)
    {
        switch (item)
        {
            case RitualItemType.GlassWithPencil:
                return "Стакан с карандашом";
            case RitualItemType.SoapWithPlantain:
                return "Мыло с подорожником";
            case RitualItemType.GildedIcon:
                return "Золоченая икона";
            case RitualItemType.LeadTablet:
                return "Свинцовый планшет";
            default:
                return item.ToString();
        }
    }

    public static string GetDescription(this RitualItemType item)
    {
        switch (item)
        {
            case RitualItemType.GlassWithPencil:
                return "Поднести к рту, постучать карандашом, держать перед лицом.";
            case RitualItemType.SoapWithPlantain:
                return "Круговые движения мылом для намыливания, приложить к точке.";
            case RitualItemType.GildedIcon:
                return "Удерживать перед собой и вращать, как фонарик, чтобы просветить комнату.";
            case RitualItemType.LeadTablet:
                return "Поднести к пациенту и следить за прогресс-баром.";
            default:
                return item.ToString();
        }
    }
}
