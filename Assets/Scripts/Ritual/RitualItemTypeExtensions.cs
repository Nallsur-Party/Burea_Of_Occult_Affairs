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
}
