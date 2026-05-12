using System;

[Serializable]
public class RitualStepDefinition
{
    public RitualItemType Item;
    public RitualActionType Action;

    public RitualStepDefinition()
    {
    }

    public RitualStepDefinition(RitualItemType item, RitualActionType action)
    {
        Item = item;
        Action = action;
    }
}
