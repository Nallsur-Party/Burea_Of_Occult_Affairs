public static class RitualActionTypeExtensions
{
    public static string GetDisplayName(this RitualActionType action)
    {
        switch (action)
        {
            case RitualActionType.EquipOnNpc:
                return "Надеть или дать NPC";
            case RitualActionType.HoldNearNpc:
                return "Держать у цели";
            case RitualActionType.ReadIncantation:
                return "Проговорить формулу";
            case RitualActionType.CircleAroundNpc:
                return "Обвести вокруг цели";
            case RitualActionType.PlaceNearby:
                return "Оставить в нужной точке";
            case RitualActionType.TouchNpc:
                return "Коснуться цели";
            case RitualActionType.BreakItem:
                return "Разрушить или разорвать";
            case RitualActionType.MarkGround:
                return "Отметить или начертить знак";
            default:
                return action.ToString();
        }
    }

    public static string GetDescription(this RitualActionType action)
    {
        switch (action)
        {
            case RitualActionType.EquipOnNpc:
                return "Надеть предмет на NPC или дать ему принять действие предмета на себя.";
            case RitualActionType.HoldNearNpc:
                return "Удерживать предмет рядом с NPC или другой целью, направляя его воздействие.";
            case RitualActionType.ReadIncantation:
                return "Проговорить или прочитать нужную формулу, удерживая предмет как фокус ритуала.";
            case RitualActionType.CircleAroundNpc:
                return "Обвести предметом вокруг NPC или очертить им круг, разрыв или границу.";
            case RitualActionType.PlaceNearby:
                return "Оставить предмет рядом, в центре аномалии или в другой важной точке.";
            case RitualActionType.TouchNpc:
                return "Коснуться предметом NPC, подозрительного объекта или точки воздействия.";
            case RitualActionType.BreakItem:
                return "Разорвать, сломать или символически уничтожить предмет, связь или носитель эффекта.";
            case RitualActionType.MarkGround:
                return "Начертить, отметить, закрепить или закрыть знак на месте действия.";
            default:
                return action.ToString();
        }
    }
}
