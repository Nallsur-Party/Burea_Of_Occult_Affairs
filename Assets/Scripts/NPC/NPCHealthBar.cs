using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NPCHealthBar : MonoBehaviour
{
    [SerializeField] private NpcOrderVisitor targetNpc;

    private Slider slider;
    private CanvasGroup canvasGroup;
    private bool isVisible = true;
    private int lastHealth = -1;
    private int lastMaxHealth = -1;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (targetNpc == null)
        {
            targetNpc = GetComponentInParent<NpcOrderVisitor>();
        }

        SetVisible(false);
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void SetTargetNpc(NpcOrderVisitor npc)
    {
        targetNpc = npc;
        lastHealth = -1;
        lastMaxHealth = -1;
        Refresh();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        SetCanvasVisible(visible);
    }

    public void Refresh()
    {
        if (slider == null || targetNpc == null || targetNpc.NpcData == null)
        {
            SetVisible(false);
            return;
        }

        int maxHealth = Mathf.Max(1, targetNpc.NpcData.MaxHealth);
        int health = Mathf.Clamp(targetNpc.NpcData.Health, 0, maxHealth);

        if (targetNpc.NpcData.MaxHealth <= 0)
        {
            SetVisible(false);
            return;
        }

        if (slider.maxValue != maxHealth)
        {
            slider.maxValue = maxHealth;
        }

        if (!Mathf.Approximately(slider.value, health))
        {
            slider.value = health;
        }

        if (health != lastHealth || maxHealth != lastMaxHealth)
        {
            lastHealth = health;
            lastMaxHealth = maxHealth;
        }

        SetCanvasVisible(isVisible);
    }

    private void SetCanvasVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
