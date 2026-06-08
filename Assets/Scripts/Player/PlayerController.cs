using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Player))]
[RequireComponent(typeof(PlayerProfile))]
public class PlayerController : MonoBehaviour
{
    [Serializable]
    private class RitualItemVisualBinding
    {
        public RitualItemType item;
        public Transform itemObject;

        [NonSerialized] public Vector3 InitialLocalPosition;
        [NonSerialized] public Quaternion InitialLocalRotation;
        [NonSerialized] public Vector3 InitialLocalScale;

        public void CacheInitialTransform()
        {
            if (itemObject == null)
            {
                return;
            }

            InitialLocalPosition = itemObject.localPosition;
            InitialLocalRotation = itemObject.localRotation;
            InitialLocalScale = itemObject.localScale;
        }
    }

    private static readonly int SpeedHash = Animator.StringToHash("speed");
    private static readonly int SpeedXHash = Animator.StringToHash("speedX");
    private static readonly int SpeedZHash = Animator.StringToHash("speedZ");
    private static readonly int IsMovingForwardHash = Animator.StringToHash("isMovingForward");
    private static readonly int IsMovingBackwardHash = Animator.StringToHash("isMovingBackward");
    private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("verticalSpeed");
    private static readonly int IsRunningHash = Animator.StringToHash("isRunning");

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField, Tooltip("Disable this in scenes where the player should not be able to jump.")]
    private bool canJump = true;
    [SerializeField] private bool useDepthMovement = false;
    [SerializeField] private float groundAcceleration = 35f;
    [SerializeField] private float airAcceleration = 20f;
    [SerializeField] private float wallCheckDistance = 0.15f;
    [SerializeField] private float sprintMultiplier = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;

    [Header("Held Item Visual")]
    [SerializeField, Tooltip("Mirrors only the held item's local X position when the player faces left.")]
    private bool mirrorHeldItemWhenFacingLeft = true;
    [SerializeField, Tooltip("Extra local rotation applied to 3D held items while the player faces left.")]
    private Vector3 heldItemLeftFacingEulerOffset = new Vector3(0f, 180f, 0f);
    [SerializeField, Tooltip("Scene object bindings for each selectable ritual item. Objects should be children of the player or hand anchor.")]
    private RitualItemVisualBinding[] heldItemObjects;

    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private LayerMask interactionMask = ~0;
    [SerializeField] private GameObject interactionPrompt;

    [Header("Debug Ritual")]
    [SerializeField] private RitualManager ritualManager;

    private Rigidbody rb;
    private Player player;
    private PlayerProfile playerProfile;
    private Collider bodyCollider;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private float depthInput;
    private bool jumpPressed;
    private NpcOrderVisitor currentInteractableNpc;
    private NpcOrderVisitor activeDialogueNpc;
    private WorkShiftTimeSystem workShiftTimeSystem;
    private RitualItemType[] ritualItems;
    private Transform activeHeldItem;
    private RitualItemType? activeHeldItemType;

    private void Awake()
    {
        if (!TryGetComponent(out rb))
        {
            Debug.LogError("PlayerController requires Rigidbody.", this);
            enabled = false;
            return;
        }

        if (!TryGetComponent(out player))
        {
            Debug.LogError("PlayerController requires Player.", this);
            enabled = false;
            return;
        }

        if (!TryGetComponent(out playerProfile))
        {
            Debug.LogError("PlayerController requires PlayerProfile.", this);
            enabled = false;
            return;
        }

        bodyCollider = GetComponent<Collider>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ritualManager = FindObjectOfType<RitualManager>();
        workShiftTimeSystem = ResolveWorkShiftTimeSystem();

        if (ritualManager == null)
        {
            GameObject ritualManagerObject = new GameObject("RitualManager");
            ritualManager = ritualManagerObject.AddComponent<RitualManager>();
        }

        ritualItems = (RitualItemType[])Enum.GetValues(typeof(RitualItemType));

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        SetInteractionPromptVisible(false);
        CacheHeldItemTransforms();
        UpdateHeldItemVisual();
    }

    private void Update()
    {
        ReadMovementInput();
        currentInteractableNpc = FindNearestInteractableNpc();

        if (canJump && Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
        }
        else if (!canJump)
        {
            jumpPressed = false;
        }

        HandleDialogueInput();
        HandleInspectionInput();
        HandleRitualInput();
        UpdateGroundedState();
        UpdateFacing();
        UpdateAnimator();
        SetInteractionPromptVisible(currentInteractableNpc != null);

        if (activeDialogueNpc != null)
        {
            bool isActiveNpcNearby = currentInteractableNpc == activeDialogueNpc;
            activeDialogueNpc.SetDialogueFocus(isActiveNpcNearby);
        }

        if (activeDialogueNpc != null && currentInteractableNpc != activeDialogueNpc)
        {
            if (ritualManager != null)
            {
                ritualManager.ClearProgress(activeDialogueNpc);
            }

            activeDialogueNpc = null;
        }
    }

    private void FixedUpdate()
    {
        Vector3 velocity = rb.velocity;
        Vector3 moveDirection = useDepthMovement
            ? new Vector3(moveInput, 0f, depthInput)
            : new Vector3(moveInput, 0f, 0f);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.Normalize();
        }

        Vector3 targetPlanarVelocity = moveDirection * (player.IsSprinting ? moveSpeed * sprintMultiplier : moveSpeed);
        Vector3 currentPlanarVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float acceleration = player.IsGrounded ? groundAcceleration : airAcceleration;

        currentPlanarVelocity = Vector3.MoveTowards(
            currentPlanarVelocity,
            targetPlanarVelocity,
            acceleration * Time.fixedDeltaTime
        );

        currentPlanarVelocity = ResolveWallCollision(currentPlanarVelocity);

        velocity.x = currentPlanarVelocity.x;
        velocity.z = currentPlanarVelocity.z;

        if (canJump && jumpPressed && player.IsGrounded)
        {
            velocity.y = jumpForce;
        }
        else if (!canJump && velocity.y > 0f)
        {
            velocity.y = 0f;
        }

        rb.velocity = velocity;
        jumpPressed = false;
    }

    private Vector3 ResolveWallCollision(Vector3 planarVelocity)
    {
        if (bodyCollider == null || planarVelocity.sqrMagnitude <= 0.0001f)
        {
            return planarVelocity;
        }

        Vector3 direction = planarVelocity.normalized;
        float sweepDistance = wallCheckDistance + planarVelocity.magnitude * Time.fixedDeltaTime;

        if (!rb.SweepTest(direction, out RaycastHit hit, sweepDistance, QueryTriggerInteraction.Ignore))
        {
            return planarVelocity;
        }

        Vector3 adjustedVelocity = Vector3.ProjectOnPlane(planarVelocity, hit.normal);

        if (!useDepthMovement)
        {
            adjustedVelocity.z = 0f;
        }

        return adjustedVelocity;
    }

    private void UpdateGroundedState()
    {
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position;

        player.IsGrounded = Physics.CheckSphere(checkPosition, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        if (!player.IsGrounded)
        {
            player.IsGrounded = Physics.Raycast(
                checkPosition,
                Vector3.down,
                groundCheckDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            );
        }
    }

    private void UpdateFacing()
    {
        if (moveInput > 0.01f)
        {
            SetFacingRight(true);
        }
        else if (moveInput < -0.01f)
        {
            SetFacingRight(false);
        }
    }

    private void ReadMovementInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        depthInput = useDepthMovement ? Input.GetAxisRaw("Vertical") : 0f;
        player.IsSprinting = Input.GetKey(KeyCode.LeftShift);
    }

    private void HandleDialogueInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentInteractableNpc != null)
        {
            StartNpcConversation(currentInteractableNpc);
        }

        if (activeDialogueNpc == null)
        {
            return;
        }

        if (!IsRitualActionModifierPressed() && Input.GetKeyDown(KeyCode.Alpha1))
        {
            AskNpcQuestion(NPCQuestionType.Name);
        }
        else if (!IsRitualActionModifierPressed() && Input.GetKeyDown(KeyCode.Alpha2))
        {
            AskNpcQuestion(NPCQuestionType.Gender);
        }
        else if (!IsRitualActionModifierPressed() && Input.GetKeyDown(KeyCode.Alpha3))
        {
            AskNpcQuestion(NPCQuestionType.Age);
        }
        else if (!IsRitualActionModifierPressed() && Input.GetKeyDown(KeyCode.Alpha4))
        {
            AskNpcQuestion(NPCQuestionType.AnotherStory);
        }
    }

    private void HandleRitualInput()
    {
        HandleRitualItemSelection();
        HandleRitualActionSelection();

        if (Input.GetKeyDown(KeyCode.R))
        {
            PerformRitualStep();
        }
    }

    private void StartNpcConversation(NpcOrderVisitor npc)
    {
        if (npc == null)
        {
            return;
        }

        if (activeDialogueNpc != null && activeDialogueNpc != npc && ritualManager != null)
        {
            ritualManager.ClearProgress(activeDialogueNpc);
        }

        activeDialogueNpc = npc;
        npc.ShowDialogue(npc.Interact());
        npc.SetDialogueFocus(true);
    }

    private void AskNpcQuestion(NPCQuestionType questionType)
    {
        if (activeDialogueNpc == null)
        {
            return;
        }

        NPC npcData = activeDialogueNpc.NpcData;
        if (!TrySpendQuestionTime(npcData, questionType))
        {
            return;
        }

        activeDialogueNpc.ShowDialogue(activeDialogueNpc.GetQuestionResponse(questionType, playerProfile));
        npcData?.RegisterQuestionAsked();
    }

    private void HandleInspectionInput()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            PerformInspectionStub();
        }
    }

    private void HandleRitualItemSelection()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CycleRitualItem();
        }
    }

    private void HandleRitualActionSelection()
    {
        if (!IsRitualActionModifierPressed())
        {
            return;
        }

        if (IsAnyActionHotkeyPressed(KeyCode.Alpha1, KeyCode.Keypad1))
        {
            SelectRitualAction(RitualActionType.EquipOnNpc);
        }
        else if (IsAnyActionHotkeyPressed(KeyCode.Alpha2, KeyCode.Keypad2))
        {
            SelectRitualAction(RitualActionType.HoldNearNpc);
        }
        else if (IsAnyActionHotkeyPressed(KeyCode.Alpha3, KeyCode.Keypad3))
        {
            SelectRitualAction(RitualActionType.ReadIncantation);
        }
        else if (IsAnyActionHotkeyPressed(KeyCode.Alpha4, KeyCode.Keypad4))
        {
            SelectRitualAction(RitualActionType.CircleAroundNpc);
        }
        else if (IsAnyActionHotkeyPressed(KeyCode.Alpha5, KeyCode.Keypad5))
        {
            SelectRitualAction(RitualActionType.PlaceNearby);
        }
        else if (IsAnyActionHotkeyPressed(KeyCode.Alpha6, KeyCode.Keypad6))
        {
            SelectRitualAction(RitualActionType.TouchNpc);
        }
        else if (IsAnyActionHotkeyPressed(KeyCode.Alpha7, KeyCode.Keypad7))
        {
            SelectRitualAction(RitualActionType.BreakItem);
        }
        else if (IsAnyActionHotkeyPressed(KeyCode.Alpha8, KeyCode.Keypad8))
        {
            SelectRitualAction(RitualActionType.MarkGround);
        }
    }

    private void PerformRitualStep()
    {
        if (ritualManager == null)
        {
            Debug.LogWarning("RitualManager is not available.");
            return;
        }

        if (activeDialogueNpc == null)
        {
            Debug.Log("Ritual Debug | No active NPC dialogue target.");
            return;
        }

        if (!ritualManager.HasActiveRitual(activeDialogueNpc))
        {
            if (!CanStartTimeConsumingAction("ритуал"))
            {
                return;
            }

            if (!ritualManager.CanStartRitual(activeDialogueNpc, out string blockedReason))
            {
                if (!string.IsNullOrWhiteSpace(blockedReason))
                {
                    activeDialogueNpc.ShowDialogue(blockedReason);
                }

                return;
            }

            if (!TrySpendTimeForAction(WorkShiftTimeSystem.RitualCostMinutes, "ритуал"))
            {
                return;
            }

            if (ritualManager.TryStartRitual(activeDialogueNpc))
            {
                activeDialogueNpc.ShowDialogue("Начинаем ритуал...");
            }

            return;
        }

        RitualAttemptResult result = ritualManager.TryPerformStep(activeDialogueNpc, player.SelectedRitualItem, player.SelectedRitualAction);
        if (result == RitualAttemptResult.NotStarted)
        {
            Debug.Log("Ritual Debug | Ritual step ignored because the ritual has not been started.");
        }
    }

    public void PerformInspectionStub()
    {
        if (!CanStartTimeConsumingAction("осмотр"))
        {
            return;
        }

        if (!TrySpendTimeForAction(WorkShiftTimeSystem.InspectionCostMinutes, "осмотр"))
        {
            return;
        }

        Debug.LogWarning("Inspection is not implemented yet. Time was consumed as a placeholder.", this);
    }

    private void SelectRitualItem(RitualItemType item)
    {
        player.SelectedRitualItem = item;
        UpdateHeldItemVisual();
        Debug.Log($"Ritual Debug | Cycled selected item to: {player.SelectedRitualItem}");
    }

    private void SelectRitualAction(RitualActionType action)
    {
        player.SelectedRitualAction = action;
        Debug.Log($"Ritual Debug | Selected action: {player.SelectedRitualAction.GetDisplayName()} | {player.SelectedRitualAction.GetDescription()}");
    }

    private static bool IsRitualActionModifierPressed()
    {
        return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    }

    private static bool IsAnyActionHotkeyPressed(KeyCode primaryKey, KeyCode secondaryKey)
    {
        return Input.GetKeyDown(primaryKey) || Input.GetKeyDown(secondaryKey);
    }

    private bool TrySpendQuestionTime(NPC npc, NPCQuestionType questionType)
    {
        if (!CanStartTimeConsumingAction("вопрос NPC"))
        {
            return false;
        }

        int questionCount = npc != null ? npc.AskedQuestionActionCount : 0;
        int costMinutes = questionCount <= 0 ? 0 : WorkShiftTimeSystem.ExtraNpcQuestionCostMinutes;

        if (!TrySpendTimeForAction(costMinutes, $"вопрос NPC {questionType}"))
        {
            return false;
        }

        return true;
    }

    private bool CanStartTimeConsumingAction(string actionLabel)
    {
        WorkShiftTimeSystem timeSystem = GetWorkShiftTimeSystem();
        if (timeSystem == null)
        {
            Debug.LogWarning($"Work shift time system is not available. {actionLabel} cannot be processed.", this);
            return false;
        }

        if (timeSystem.IsShiftEnded)
        {
            Debug.LogWarning($"Work shift has ended. {actionLabel} is blocked.", this);
            return false;
        }

        return true;
    }

    private bool TrySpendTimeForAction(int minutes, string actionLabel)
    {
        if (minutes <= 0)
        {
            return true;
        }

        WorkShiftTimeSystem timeSystem = GetWorkShiftTimeSystem();
        if (timeSystem == null)
        {
            Debug.LogWarning($"Work shift time system is not available. {actionLabel} cannot consume time.", this);
            return false;
        }

        if (!timeSystem.TrySpendMinutes(minutes))
        {
            Debug.LogWarning($"Work shift has ended. {actionLabel} cannot consume time.", this);
            return false;
        }

        return true;
    }

    private WorkShiftTimeSystem GetWorkShiftTimeSystem()
    {
        if (workShiftTimeSystem != null)
        {
            return workShiftTimeSystem;
        }

        workShiftTimeSystem = ResolveWorkShiftTimeSystem();
        return workShiftTimeSystem;
    }

    private static WorkShiftTimeSystem ResolveWorkShiftTimeSystem()
    {
        if (WorkShiftTimeSystem.Instance != null)
        {
            return WorkShiftTimeSystem.Instance;
        }

        return FindObjectOfType<WorkShiftTimeSystem>();
    }

    private void CycleRitualItem()
    {
        if (ritualItems == null || ritualItems.Length == 0)
        {
            ritualItems = (RitualItemType[])Enum.GetValues(typeof(RitualItemType));
        }

        int currentIndex = Array.IndexOf(ritualItems, player.SelectedRitualItem);
        int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % ritualItems.Length;
        SelectRitualItem(ritualItems[nextIndex]);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        Vector3 velocity = rb != null ? rb.velocity : Vector3.zero;
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        float planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        float speedX = Mathf.Abs(localVelocity.x);
        float speedZ = localVelocity.z;
        bool isMovingForward = speedZ >= speedX;
        bool isMovingBackward = speedZ <= -speedX;
        bool isRunning = planarSpeed > moveSpeed + 0.1f;
        animator.SetFloat(SpeedHash, planarSpeed);
        animator.SetFloat(SpeedXHash, speedX);
        animator.SetFloat(SpeedZHash, speedZ);
        animator.SetBool(IsMovingForwardHash, isMovingForward);
        animator.SetBool(IsMovingBackwardHash, isMovingBackward);
        animator.SetBool(IsGroundedHash, player.IsGrounded);
        animator.SetFloat(VerticalSpeedHash, velocity.y);
        animator.SetBool(IsRunningHash, isRunning);
    }

    private void SetFacingRight(bool facingRight)
    {
        if (player.IsFacingRight != facingRight)
        {
            player.IsFacingRight = facingRight;
            RefreshHeldItemTransform();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facingRight;
            return;
        }

        if (visualRoot == null)
        {
            return;
        }

        Vector3 scale = visualRoot.localScale;
        scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        visualRoot.localScale = scale;
    }

    private void UpdateHeldItemVisual()
    {
        RitualItemVisualBinding binding = GetHeldItemBinding(player.SelectedRitualItem);

        SetAllHeldItemsVisible(false);
        activeHeldItem = null;
        activeHeldItemType = null;

        if (binding == null || binding.itemObject == null)
        {
            return;
        }

        activeHeldItem = binding.itemObject;
        activeHeldItemType = player.SelectedRitualItem;
        activeHeldItem.gameObject.SetActive(true);
        RefreshHeldItemTransform();
    }

    private void CacheHeldItemTransforms()
    {
        if (heldItemObjects == null)
        {
            return;
        }

        for (int i = 0; i < heldItemObjects.Length; i++)
        {
            heldItemObjects[i]?.CacheInitialTransform();
        }
    }

    private void SetAllHeldItemsVisible(bool isVisible)
    {
        if (heldItemObjects == null)
        {
            return;
        }

        for (int i = 0; i < heldItemObjects.Length; i++)
        {
            Transform itemObject = heldItemObjects[i]?.itemObject;

            if (itemObject != null)
            {
                itemObject.gameObject.SetActive(isVisible);
            }
        }
    }

    private void RefreshHeldItemTransform()
    {
        if (activeHeldItem == null || !activeHeldItemType.HasValue)
        {
            return;
        }

        RitualItemVisualBinding binding = GetHeldItemBinding(activeHeldItemType.Value);
        if (binding == null)
        {
            return;
        }

        Vector3 position = binding.InitialLocalPosition;

        if (mirrorHeldItemWhenFacingLeft && !player.IsFacingRight)
        {
            position.x = -position.x;
        }

        Quaternion rotation = binding.InitialLocalRotation;
        if (!player.IsFacingRight)
        {
            rotation *= Quaternion.Euler(heldItemLeftFacingEulerOffset);
        }

        activeHeldItem.localPosition = position;
        activeHeldItem.localRotation = rotation;
        activeHeldItem.localScale = binding.InitialLocalScale;
    }

    private RitualItemVisualBinding GetHeldItemBinding(RitualItemType item)
    {
        if (heldItemObjects == null)
        {
            return null;
        }

        for (int i = 0; i < heldItemObjects.Length; i++)
        {
            RitualItemVisualBinding binding = heldItemObjects[i];

            if (binding != null && binding.item == item)
            {
                return binding;
            }
        }

        return null;
    }

    private NpcOrderVisitor FindNearestInteractableNpc()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position,
            interactionRadius,
            interactionMask,
            QueryTriggerInteraction.Collide
        );

        NpcOrderVisitor nearestNpc = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            NpcOrderVisitor npc = nearbyColliders[i].GetComponentInParent<NpcOrderVisitor>();

            if (npc == null)
            {
                continue;
            }

            float distance = (npc.transform.position - transform.position).sqrMagnitude;

            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestNpc = npc;
        }

        return nearestNpc;
    }

    private void SetInteractionPromptVisible(bool isVisible)
    {
        if (interactionPrompt == null || interactionPrompt.activeSelf == isVisible)
        {
            return;
        }

        interactionPrompt.SetActive(isVisible);
    }

    private void ShowDialogue(string message)
    {
        // Dialogue display is now handled by NPCDialogueBubble on the NPC pawn.
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
        Gizmos.DrawLine(checkPosition, checkPosition + Vector3.down * groundCheckDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
