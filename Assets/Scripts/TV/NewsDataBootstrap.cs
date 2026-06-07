using UnityEngine;

[DisallowMultipleComponent]
public class NewsDataBootstrap : MonoBehaviour
{
    [Header("XML Sources")]
    [SerializeField] private TextAsset npcProblemsXml;
    [SerializeField] private TextAsset problemNewsXml;
    [SerializeField] private TextAsset newsTemplatesXml;
    [SerializeField] private TextAsset newsMappingsXml;

    [Header("Behaviour")]
    [SerializeField] private bool initializeOnAwake = true;

    private void Awake()
    {
        if (initializeOnAwake)
        {
            InitializeNewsData();
        }
    }

    [ContextMenu("Initialize News Data")]
    public void InitializeNewsData()
    {
        NewsDataLoader.Initialize(npcProblemsXml, problemNewsXml, newsTemplatesXml, newsMappingsXml);
    }
}
