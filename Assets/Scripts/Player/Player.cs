using UnityEngine;

[DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    [Header("Ritual State")]
    [SerializeField] private RitualItemType selectedRitualItem = RitualItemType.GlassWithPencil;
    [SerializeField] private RitualActionType selectedRitualAction = RitualActionType.EquipOnNpc;

    [Header("Runtime State")]
    [SerializeField] private bool isFacingRight = true;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isSprinting;

    public RitualItemType SelectedRitualItem
    {
        get => selectedRitualItem;
        set => selectedRitualItem = value;
    }

    public RitualActionType SelectedRitualAction
    {
        get => selectedRitualAction;
        set => selectedRitualAction = value;
    }

    public bool IsFacingRight
    {
        get => isFacingRight;
        set => isFacingRight = value;
    }

    public bool IsGrounded
    {
        get => isGrounded;
        set => isGrounded = value;
    }

    public bool IsSprinting
    {
        get => isSprinting;
        set => isSprinting = value;
    }
}
