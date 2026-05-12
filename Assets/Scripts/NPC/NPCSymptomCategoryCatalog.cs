using System.Collections.Generic;

public class NPCSymptomCategoryCatalog
{
    private readonly List<NPCSymptomCategoryDefinition> categories;

    public IReadOnlyList<NPCSymptomCategoryDefinition> Categories => categories;

    public NPCSymptomCategoryCatalog(IEnumerable<NPCSymptomCategoryDefinition> categories)
    {
        this.categories = categories != null
            ? new List<NPCSymptomCategoryDefinition>(categories)
            : new List<NPCSymptomCategoryDefinition>();
    }
}
