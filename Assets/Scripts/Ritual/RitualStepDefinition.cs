using System;

[Serializable]
public class RitualStepDefinition
{
    public int Index;
    public RitualItemType Item;
    public RitualActionType Action;
    public string Title;
    public string Description;

    public RitualStepDefinition()
    {
    }

    public RitualStepDefinition(
        int index,
        RitualItemType item,
        RitualActionType action,
        string title = null,
        string description = null)
    {
        Index = index;
        Item = item;
        Action = action;
        Title = title;
        Description = description;
    }
}
