using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class NpcOrderVisitor : MonoBehaviour
{
    private static NpcOrderVisitor nStayOccupant;

    private enum VisitorState
    {
        Idle,
        GoingToCounter,
        WaitingInQueue,
        WaitingAtCounter,
        WaitingForProblemResolution,
        PushingAway,
        Leaving
    }

    private static readonly int SpeedHash = Animator.StringToHash("speed");
    private static readonly int SpeedXHash = Animator.StringToHash("speedX");
    private static readonly int SpeedZHash = Animator.StringToHash("speedZ");
    private static readonly int IsMovingForwardHash = Animator.StringToHash("isMovingForward");
    private static readonly int IsMovingBackwardHash = Animator.StringToHash("isMovingBackward");
    private static readonly int IsLookingDownHash = Animator.StringToHash("isLookingDown");
    private static readonly int IsLookingHorizontalHash = Animator.StringToHash("isLookingHorizontal");
    private static readonly int IsPlayerNearHash = Animator.StringToHash("isPlayerNear");

    [Header("Route")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform[] exitPoints;
    [SerializeField] private Transform[] sequentialExitRoutePoints;
    [SerializeField] private Transform ritualStayPoint;
    [SerializeField] private Transform[] ritualApproachRoutePoints;
    [SerializeField] private Transform[] ritualExitRoutePoints;
    [SerializeField, HideInInspector, FormerlySerializedAs("holdUntilCuredExitRoutePoints")] private Transform[] legacyRitualRoutePoints;
    [SerializeField] private bool snapToStartPointOnAwake = true;
    [SerializeField] private bool beginRouteOnAwake = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stoppingDistance = 0.05f;
    [SerializeField] private bool keepCurrentY = true;
    [SerializeField] private float collisionSkin = 0.02f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool invertFlipX;
    [SerializeField] private bool facesRightByDefault = true;
    [SerializeField] private Animator animator;
    [SerializeField] private float lookAtPlayerRadius = 2f;
    [SerializeField] private NPCDialogueBubble dialogueBubble;
    [SerializeField] private NPCHealthBar healthBar;

    [Header("NPC Data")]
    [SerializeField] private NPCGenerator npcGenerator;
    [SerializeField] private bool generateNpcDataOnAwake = true;
    [SerializeField] private NPC npcData;
    [SerializeField] private bool renameGameObjectToNpcName = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onReachedCounter;
    [SerializeField] private UnityEvent onLeftScene;

    [Header("Push")]
    [SerializeField] private float pushDistance = 1f;

    private VisitorState currentState = VisitorState.Idle;
    private Transform currentTarget;
    private bool useCustomTarget;
    private Vector3 customTargetPosition;
    private Vector3 lastFrameVelocity = Vector3.zero;
    private Rigidbody rb;
    private CapsuleCollider bodyCollider;
    private PlayerController playerController;
    private NPCQueueManager npcQueueManager;
    private VisitorState previousState;
    private bool isPushed = false;
    private Vector3 queuePosition;
    private Transform interruptedTarget;
    private bool interruptedUseCustomTarget;
    private Vector3 interruptedCustomTargetPosition;
    private bool isSequentialExitActive;
    private int sequentialExitIndex = -1;
    private bool isRitualRouteActive;
    private RitualRoutePhase ritualRoutePhase = RitualRoutePhase.None;
    private int ritualApproachIndex = -1;
    private int ritualExitIndex = -1;

    private enum RitualRoutePhase
    {
        None,
        ApproachingRoute,
        ApproachingStayPoint,
        WaitingAtStayPoint,
        Exiting
    }

    public bool IsWaitingAtCounter => currentState == VisitorState.WaitingAtCounter;
    public bool IsInQueue => currentState == VisitorState.WaitingInQueue;
    public NPC NpcData => npcData;
    public static bool IsNStayOccupied => nStayOccupant != null && nStayOccupant.isActiveAndEnabled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<CapsuleCollider>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        if (npcGenerator == null)
        {
            npcGenerator = FindObjectOfType<NPCGenerator>();
        }

        if (dialogueBubble == null)
        {
            dialogueBubble = GetComponentInChildren<NPCDialogueBubble>();
        }

        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<NPCHealthBar>(true);
        }

        if (npcQueueManager == null)
        {
            npcQueueManager = FindObjectOfType<NPCQueueManager>();
        }

        if (generateNpcDataOnAwake)
        {
            GenerateNpcData();
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            if (keepCurrentY)
            {
                rb.constraints |= RigidbodyConstraints.FreezePositionY;
            }

            rb.useGravity = !keepCurrentY;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        if (snapToStartPointOnAwake && startPoint != null)
        {
            Vector3 startPosition = GetTargetPosition(startPoint);
            if (rb != null)
            {
                rb.position = startPosition;
            }
            else
            {
                transform.position = startPosition;
            }
        }

        if (beginRouteOnAwake)
        {
            SendToCounter();
        }
    }

    private void OnDisable()
    {
        ReleaseNStayOccupancy();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsNpcCollision(collision))
        {
            return;
        }

        if (collision.gameObject.CompareTag("Player")
            && !IsNpcMoving()
            && currentState != VisitorState.PushingAway
            && currentState != VisitorState.WaitingAtCounter)
        {
            PushAway();
        }
    }

    private bool IsNpcMoving()
    {
        return currentState == VisitorState.GoingToCounter
            || currentState == VisitorState.Leaving
            || currentState == VisitorState.PushingAway;
    }

    private bool IsNpcCollision(Collision collision)
    {
        if (collision == null)
        {
            return false;
        }

        NpcOrderVisitor otherNpc = collision.collider.GetComponentInParent<NpcOrderVisitor>();
        return otherNpc != null && otherNpc != this;
    }

    private void PushAway()
    {
        // Сохранить предыдущее состояние
        previousState = currentState;
        interruptedTarget = currentTarget;
        interruptedUseCustomTarget = useCustomTarget;
        interruptedCustomTargetPosition = customTargetPosition;

        // Выбрать случайное направление
        Vector3 direction = Random.insideUnitSphere;
        direction.y = 0; // Поскольку keepCurrentY
        direction = direction.normalized;

        // Проверить препятствия с помощью Raycast
        if (Physics.Raycast(transform.position, direction, pushDistance))
        {
            // Если препятствие, попробовать другое направление до 10 раз
            for (int i = 0; i < 10; i++)
            {
                direction = Random.insideUnitSphere;
                direction.y = 0;
                direction = direction.normalized;
                if (!Physics.Raycast(transform.position, direction, pushDistance))
                {
                    break;
                }
            }
        }

        // Установить цель
        Vector3 targetPos = transform.position + direction * pushDistance;
        SetTargetPosition(targetPos);
        currentState = VisitorState.PushingAway;
        isPushed = true;
    }

    public void GenerateNpcData()
    {
        TryGenerateAndSetNpcData();
    }

    public bool TryGenerateAndSetNpcData(NPCGenerator generatorOverride = null)
    {
        NPCGenerator generator = generatorOverride != null ? generatorOverride : npcGenerator;
        if (generator == null)
        {
            Debug.LogWarning($"{nameof(NpcOrderVisitor)} on {name} could not find {nameof(NPCGenerator)}.", this);
            return false;
        }

        generator.GenerateNpc();
        npcData = generator.GeneratedNpc;
        npcGenerator = generator;

        if (renameGameObjectToNpcName && npcData != null && !string.IsNullOrWhiteSpace(npcData.Name))
        {
            gameObject.name = $"NPC - {npcData.Name}";
        }

        return npcData != null;
    }

    public string Interact()
    {
        if (npcData == null)
        {
            Debug.Log($"NPC {name}: data has not been generated yet.", this);
            return "NPC data has not been generated yet.";
        }

        string interactionText = GetInteractionText();
        DebugLogNpcState("Interact", interactionText);
        return interactionText;
    }

    public void ShowDialogue(string message)
    {
        if (dialogueBubble == null)
        {
            return;
        }

        dialogueBubble.Show(message);
        if (healthBar != null)
        {
            healthBar.SetVisible(true);
        }
    }

    public void ShowPersistentDialogue(string message)
    {
        if (dialogueBubble == null)
        {
            return;
        }

        dialogueBubble.ShowPersistent(message);
        if (healthBar != null)
        {
            healthBar.SetVisible(true);
        }
    }

    public void HideDialogue()
    {
        if (dialogueBubble == null)
        {
            return;
        }

        dialogueBubble.Hide();
        if (healthBar != null)
        {
            healthBar.SetVisible(false);
        }
    }

    public void SetDialogueFocus(bool focused)
    {
        if (dialogueBubble == null)
        {
            return;
        }

        dialogueBubble.SetFocus(focused);
    }

    public void SyncHealthBarVisibility(bool visible)
    {
        if (healthBar == null)
        {
            return;
        }

        healthBar.SetVisible(visible);
    }

    public void RefreshHealthBar()
    {
        if (healthBar == null)
        {
            return;
        }

        healthBar.Refresh();
    }

    public bool IsDialogueVisible
    {
        get
        {
            return dialogueBubble != null && dialogueBubble.IsVisible;
        }
    }

    public void SetNpcData(NPC npc)
    {
        if (npc == null)
        {
            return;
        }

        npcData = npc;

        if (renameGameObjectToNpcName && !string.IsNullOrWhiteSpace(npcData.Name))
        {
            gameObject.name = $"NPC - {npcData.Name}";
        }
    }

    public void ConfigureRoute(Transform startPoint, Transform counterPoint, Transform[] exitPoints, bool snapToStart = false)
    {
        this.startPoint = startPoint;
        this.counterPoint = counterPoint;
        this.exitPoints = exitPoints;

        if (snapToStart && startPoint != null)
        {
            transform.position = GetTargetPosition(startPoint);
        }
    }

    public string GetInteractionText()
    {
        if (npcData == null)
        {
            return "NPC data has not been generated yet.";
        }

        string dialogueLine = npcGenerator != null
            ? npcGenerator.GetDialogueLine(npcData)
            : null;

        if (string.IsNullOrWhiteSpace(dialogueLine))
        {
            dialogueLine = "Мне нечего сказать.";
        }

        return dialogueLine;
    }

    public string GetQuestionResponse(NPCQuestionType questionType, PlayerProfile playerProfile)
    {
        if (npcData == null)
        {
            return "NPC data has not been generated yet.";
        }

        if (npcGenerator == null)
        {
            return "Говорить пока не о чем.";
        }

        string answer = npcGenerator.GetQuestionResponse(npcData, questionType, playerProfile);
        DebugLogNpcState($"Question {questionType}", answer);

        return answer;
    }

    private void DebugLogNpcState(string actionLabel, string responseText)
    {
        if (npcData == null)
        {
            return;
        }

        string symptomsText = npcData.Symptoms.Count > 0
            ? BuildSymptomsDebugText()
            : "No symptoms";
        string problemText = npcData.HasProblem ? npcData.ProblemName : "No problem";
        string safeResponseText = string.IsNullOrWhiteSpace(responseText) ? "No response" : responseText;

        Debug.Log(
            $"NPC Debug | Action: {actionLabel} | Response: {safeResponseText} | NPC: {npcData.Name} | Gender: {npcData.Gender} | Age: {npcData.Age} | Trait: {npcData.Trait} | Problem: {problemText} | Symptoms: {symptomsText} | TruthTokens: {npcData.RemainingTruthTokens} | LieTokens: {npcData.RemainingLieTokens} | QuestionTokens: {npcData.RemainingDetectiveQuestionTokens} | SpentQuestions: {npcData.SpentDetectiveQuestionCount}",
            this
        );
    }

    private string BuildSymptomsDebugText()
    {
        if (npcData == null || npcData.SymptomIds.Count == 0 || npcData.Symptoms.Count == 0)
        {
            return "No symptoms";
        }

        int symptomCount = Mathf.Min(npcData.SymptomIds.Count, npcData.Symptoms.Count);
        string[] symptomEntries = new string[symptomCount];

        for (int i = 0; i < symptomCount; i++)
        {
            symptomEntries[i] = $"{npcData.SymptomIds[i]}: {npcData.Symptoms[i]}";
        }

        return string.Join(", ", symptomEntries);
    }

    private void Update()
    {
        if (currentState == VisitorState.WaitingForProblemResolution && npcData != null && npcData.IsCured)
        {
            ContinueRitualExitRoute();
        }
        else if (currentState == VisitorState.WaitingForProblemResolution)
        {
            EnsureActiveRouteTarget();
        }

        if (currentTarget == null && !useCustomTarget)
        {
            lastFrameVelocity = Vector3.zero;
            UpdateAnimator();
            return;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (currentTarget == null && !useCustomTarget)
        {
            return;
        }

        Vector3 targetPosition = GetTargetPosition();
        Vector3 currentPosition = rb != null ? rb.position : transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.fixedDeltaTime);
        Vector3 delta = nextPosition - currentPosition;
        delta = ResolveMovementCollisions(delta);
        nextPosition = currentPosition + delta;

        if (rb != null)
        {
            rb.MovePosition(nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }

        lastFrameVelocity = Time.fixedDeltaTime > 0f ? delta / Time.fixedDeltaTime : Vector3.zero;
        UpdateFacing(delta);

        if (Vector3.Distance(nextPosition, targetPosition) <= stoppingDistance)
        {
            ArriveAtTarget();
        }
    }

    public void SendToCounter()
    {
        if (counterPoint == null)
        {
            return;
        }

        SetTargetTransform(counterPoint);
        currentState = VisitorState.GoingToCounter;
    }

    public void SendToQueuePosition(Vector3 queuePosition)
    {
        this.queuePosition = queuePosition;
        SetTargetPosition(queuePosition);
        currentState = VisitorState.WaitingInQueue;
    }

    public void LeaveRandomExit()
    {
        if (exitPoints == null || exitPoints.Length == 0)
        {
            ResetLeavingState();
            return;
        }

        int index = Random.Range(0, exitPoints.Length);
        LeaveThroughExit(index);
    }

    public void LeaveThroughExit(int exitIndex)
    {
        if (exitPoints == null || exitIndex < 0 || exitIndex >= exitPoints.Length || exitPoints[exitIndex] == null)
        {
            return;
        }

        if (exitPoints[exitIndex].name.Contains("Z"))
        {
            StartSequentialExitRoute();
            return;
        }

        if (exitPoints[exitIndex].name.Contains("N"))
        {
            StartRitualRoute();
            return;
        }

        if (npcQueueManager == null)
        {
            npcQueueManager = FindObjectOfType<NPCQueueManager>();
        }

        if (npcQueueManager != null)
        {
            npcQueueManager.DequeueNPC(this);
        }

        ResetLeavingState();
        SetTargetTransform(exitPoints[exitIndex]);
        currentState = VisitorState.Leaving;
    }

    public void LeaveThroughExitByName(string exitName)
    {
        if (exitPoints == null || string.IsNullOrWhiteSpace(exitName))
        {
            return;
        }

        if (string.Equals(exitName, "Z", System.StringComparison.OrdinalIgnoreCase))
        {
            StartSequentialExitRoute();
            return;
        }

        if (string.Equals(exitName, "N", System.StringComparison.OrdinalIgnoreCase))
        {
            StartRitualRoute();
            return;
        }

        for (int i = 0; i < exitPoints.Length; i++)
        {
            if (exitPoints[i] != null && exitPoints[i].name.Contains(exitName))
            {
                LeaveThroughExit(i);
                return;
            }
        }
    }

    public void SetSequentialExitRoutePoints(Transform[] routePoints)
    {
        sequentialExitRoutePoints = BuildRoute(routePoints);
        sequentialExitIndex = -1;
        isSequentialExitActive = false;
    }

    public void SetRitualStayPoint(Transform stayPoint)
    {
        ritualStayPoint = stayPoint;
    }

    public void SetRitualApproachRoutePoints(Transform[] routePoints)
    {
        ritualApproachRoutePoints = BuildRoute(routePoints);
    }

    public void SetRitualExitRoutePoints(Transform[] routePoints)
    {
        ritualExitRoutePoints = BuildRoute(routePoints);
    }

    public void SetRitualRoutePoints(Transform stayPoint, Transform[] approachRoutePoints, Transform[] exitRoutePoints)
    {
        ritualStayPoint = stayPoint;
        ritualApproachRoutePoints = BuildRoute(approachRoutePoints);
        ritualExitRoutePoints = BuildRoute(exitRoutePoints);
    }

    public void SetHoldUntilCuredExitRoutePoints(Transform[] routePoints)
    {
        legacyRitualRoutePoints = BuildRoute(routePoints);
        ritualApproachRoutePoints = null;
        ritualExitRoutePoints = null;
        ritualRoutePhase = RitualRoutePhase.None;
        ritualApproachIndex = -1;
        ritualExitIndex = -1;
        isRitualRouteActive = false;
    }

    private void StartSequentialExitRoute()
    {
        if (sequentialExitRoutePoints == null || sequentialExitRoutePoints.Length == 0)
        {
            return;
        }

        if (npcQueueManager == null)
        {
            npcQueueManager = FindObjectOfType<NPCQueueManager>();
        }

        if (npcQueueManager != null)
        {
            npcQueueManager.DequeueNPC(this);
        }

        ResetLeavingState();
        isSequentialExitActive = true;
        sequentialExitIndex = 0;
        SetTargetTransform(sequentialExitRoutePoints[0]);
        currentState = VisitorState.Leaving;
    }

    private void StartRitualRoute()
    {
        EnsureRitualRouteConfiguration();

        if (IsNStayOccupiedByAnother(this))
        {
            return;
        }

        if (npcQueueManager == null)
        {
            npcQueueManager = FindObjectOfType<NPCQueueManager>();
        }

        if (npcQueueManager != null)
        {
            npcQueueManager.DequeueNPC(this);
        }

        ResetLeavingState();
        if (!TryClaimNStayOccupancy())
        {
            isRitualRouteActive = false;
            ritualRoutePhase = RitualRoutePhase.None;
            return;
        }

        isRitualRouteActive = true;

        if (ritualApproachRoutePoints != null && ritualApproachRoutePoints.Length > 0)
        {
            ritualRoutePhase = RitualRoutePhase.ApproachingRoute;
            ritualApproachIndex = 0;
            SetTargetTransform(ritualApproachRoutePoints[0]);
        }
        else if (ritualStayPoint != null)
        {
            ritualRoutePhase = RitualRoutePhase.ApproachingStayPoint;
            SetTargetTransform(ritualStayPoint);
        }
        else if (ritualExitRoutePoints != null && ritualExitRoutePoints.Length > 0)
        {
            StartRitualExitRoute();
            return;
        }
        else
        {
            isRitualRouteActive = false;
            ritualRoutePhase = RitualRoutePhase.None;
            ReleaseNStayOccupancy();
            return;
        }

        currentState = VisitorState.Leaving;
    }

    public bool TryContinueResolvedExitRoute()
    {
        if (!isRitualRouteActive || currentState != VisitorState.WaitingForProblemResolution)
        {
            return false;
        }

        ContinueRitualExitRoute();
        return true;
    }

    private void ContinueRitualExitRoute()
    {
        if (!isRitualRouteActive)
        {
            return;
        }

        if (ritualExitRoutePoints != null && ritualExitRoutePoints.Length > 0)
        {
            StartRitualExitRoute();
            return;
        }

        FinishLeavingScene();
    }

    private void StartRitualExitRoute()
    {
        if (ritualExitRoutePoints == null || ritualExitRoutePoints.Length == 0)
        {
            FinishLeavingScene();
            return;
        }

        ritualRoutePhase = RitualRoutePhase.Exiting;
        ritualExitIndex = 0;
        SetTargetTransform(ritualExitRoutePoints[0]);
        currentState = VisitorState.Leaving;
    }

    private void EnsureRitualRouteConfiguration()
    {
        if (legacyRitualRoutePoints == null || legacyRitualRoutePoints.Length == 0)
        {
            return;
        }

        Transform resolvedStayPoint = ritualStayPoint ?? FindStayPointInLegacyRoute(legacyRitualRoutePoints);
        if (resolvedStayPoint != null && ritualStayPoint == null)
        {
            ritualStayPoint = resolvedStayPoint;
        }

        if (resolvedStayPoint != null)
        {
            if (ritualApproachRoutePoints == null || ritualApproachRoutePoints.Length == 0)
            {
                ritualApproachRoutePoints = BuildRouteBeforeStayPoint(legacyRitualRoutePoints, resolvedStayPoint);
            }

            if (ritualExitRoutePoints == null || ritualExitRoutePoints.Length == 0)
            {
                ritualExitRoutePoints = BuildRouteAfterStayPoint(legacyRitualRoutePoints, resolvedStayPoint);
            }
        }
        else
        {
            if (ritualApproachRoutePoints == null || ritualApproachRoutePoints.Length == 0)
            {
                ritualApproachRoutePoints = BuildRoute(legacyRitualRoutePoints);
            }
        }
    }

    private static Transform FindStayPointInLegacyRoute(IReadOnlyList<Transform> routePoints)
    {
        if (routePoints == null)
        {
            return null;
        }

        for (int i = 0; i < routePoints.Count; i++)
        {
            Transform waypoint = routePoints[i];
            if (waypoint == null)
            {
                continue;
            }

            string waypointName = waypoint.name;
            if (!string.IsNullOrWhiteSpace(waypointName)
                && (waypointName.Contains("NStay") || waypointName.Contains("Stay")))
            {
                return waypoint;
            }
        }

        return null;
    }

    private static Transform[] BuildRoute(params Transform[] points)
    {
        List<Transform> route = new List<Transform>();
        if (points == null)
        {
            return route.ToArray();
        }

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
            {
                route.Add(points[i]);
            }
        }

        return route.ToArray();
    }

    private static Transform[] BuildRouteBeforeStayPoint(IReadOnlyList<Transform> sourcePoints, Transform stayPoint)
    {
        List<Transform> route = new List<Transform>();
        if (sourcePoints == null)
        {
            return route.ToArray();
        }

        for (int i = 0; i < sourcePoints.Count; i++)
        {
            Transform waypoint = sourcePoints[i];
            if (waypoint == null)
            {
                continue;
            }

            if (waypoint == stayPoint)
            {
                break;
            }

            route.Add(waypoint);
        }

        return route.ToArray();
    }

    private static Transform[] BuildRouteAfterStayPoint(IReadOnlyList<Transform> sourcePoints, Transform stayPoint)
    {
        List<Transform> route = new List<Transform>();
        if (sourcePoints == null)
        {
            return route.ToArray();
        }

        bool hasPassedStayPoint = false;
        for (int i = 0; i < sourcePoints.Count; i++)
        {
            Transform waypoint = sourcePoints[i];
            if (waypoint == null)
            {
                continue;
            }

            if (!hasPassedStayPoint)
            {
                if (waypoint == stayPoint)
                {
                    hasPassedStayPoint = true;
                }

                continue;
            }

            route.Add(waypoint);
        }

        return route.ToArray();
    }

    private static bool IsNStayOccupiedByAnother(NpcOrderVisitor requester)
    {
        if (nStayOccupant != null && !nStayOccupant.isActiveAndEnabled)
        {
            nStayOccupant = null;
        }

        return nStayOccupant != null && nStayOccupant != requester;
    }

    private bool HandleRitualRouteArrival()
    {
        switch (ritualRoutePhase)
        {
            case RitualRoutePhase.ApproachingRoute:
                if (ritualStayPoint != null && currentTarget == ritualStayPoint)
                {
                    if (npcData != null && npcData.IsCured)
                    {
                        StartRitualExitRoute();
                        return true;
                    }
                    ritualRoutePhase = RitualRoutePhase.WaitingAtStayPoint;
                    currentState = VisitorState.WaitingForProblemResolution;
                    EnsureRitualStayTarget();
                    return true;
                }

                if (ritualApproachRoutePoints != null && ritualApproachIndex + 1 < ritualApproachRoutePoints.Length)
                {
                    ritualApproachIndex++;
                    Transform nextApproachWaypoint = ritualApproachRoutePoints[ritualApproachIndex];
                    if (nextApproachWaypoint != null)
                    {
                        SetTargetTransform(nextApproachWaypoint);
                        currentState = VisitorState.Leaving;
                        return true;
                    }
                }

                if (ritualStayPoint != null)
                {
                    ritualRoutePhase = RitualRoutePhase.ApproachingStayPoint;
                    SetTargetTransform(ritualStayPoint);
                    currentState = VisitorState.Leaving;
                    return true;
                }

                if (ritualExitRoutePoints != null && ritualExitRoutePoints.Length > 0)
                {
                    StartRitualExitRoute();
                    return true;
                }

                FinishLeavingScene();
                return true;

            case RitualRoutePhase.ApproachingStayPoint:
                if (npcData != null && npcData.IsCured)
                {
                    StartRitualExitRoute();
                    return true;
                }
                ritualRoutePhase = RitualRoutePhase.WaitingAtStayPoint;
                currentState = VisitorState.WaitingForProblemResolution;
                EnsureRitualStayTarget();
                return true;

            case RitualRoutePhase.WaitingAtStayPoint:
                return true;

            case RitualRoutePhase.Exiting:
                if (ritualExitRoutePoints != null && ritualExitIndex + 1 < ritualExitRoutePoints.Length)
                {
                    ritualExitIndex++;
                    Transform nextExitWaypoint = ritualExitRoutePoints[ritualExitIndex];
                    if (nextExitWaypoint != null)
                    {
                        SetTargetTransform(nextExitWaypoint);
                        currentState = VisitorState.Leaving;
                        return true;
                    }
                }

                FinishLeavingScene();
                return true;
        }

        return false;
    }

    private bool TryClaimNStayOccupancy()
    {
        if (IsNStayOccupiedByAnother(this))
        {
            return false;
        }

        nStayOccupant = this;
        return true;
    }

    private void EnsureActiveRouteTarget()
    {
        switch (currentState)
        {
            case VisitorState.WaitingInQueue:
                EnsureQueueTarget();
                break;

            case VisitorState.GoingToCounter:
            case VisitorState.WaitingAtCounter:
                EnsureCounterTarget();
                break;

            case VisitorState.WaitingForProblemResolution:
                EnsureRitualStayTarget();
                break;

            case VisitorState.Leaving:
                EnsureLeavingRouteTarget();
                break;
        }
    }

    private void EnsureQueueTarget()
    {
        if (npcQueueManager != null)
        {
            npcQueueManager.RefreshQueuePosition(this);
            return;
        }

        if (currentTarget != null && useCustomTarget)
        {
            return;
        }

        SetTargetPosition(queuePosition);
    }

    private void EnsureCounterTarget()
    {
        if (counterPoint == null)
        {
            return;
        }

        if (currentTarget != counterPoint || useCustomTarget)
        {
            SetTargetTransform(counterPoint);
        }
    }

    private void EnsureLeavingRouteTarget()
    {
        if (isRitualRouteActive)
        {
            EnsureRitualRouteTarget();
            return;
        }

        if (!isSequentialExitActive || sequentialExitRoutePoints == null || sequentialExitRoutePoints.Length == 0)
        {
            return;
        }

        EnsureRouteTarget(sequentialExitRoutePoints, sequentialExitIndex);
    }

    private void EnsureRitualRouteTarget()
    {
        switch (ritualRoutePhase)
        {
            case RitualRoutePhase.WaitingAtStayPoint:
                EnsureRitualStayTarget();
                return;

            case RitualRoutePhase.ApproachingStayPoint:
                EnsureTargetTransform(ritualStayPoint);
                return;

            case RitualRoutePhase.ApproachingRoute:
                EnsureRouteTarget(ritualApproachRoutePoints, ritualApproachIndex);
                return;

            case RitualRoutePhase.Exiting:
                EnsureRouteTarget(ritualExitRoutePoints, ritualExitIndex);
                return;
        }
    }

    private void EnsureRitualStayTarget()
    {
        EnsureTargetTransform(ritualStayPoint);
    }

    private void ReleaseNStayOccupancy()
    {
        if (nStayOccupant == this)
        {
            nStayOccupant = null;
        }
    }

    private void ArriveAtTarget()
    {
        switch (currentState)
        {
            case VisitorState.GoingToCounter:
                currentState = VisitorState.WaitingAtCounter;
                ClearTarget();
                onReachedCounter.Invoke();
                break;

            case VisitorState.WaitingInQueue:
                currentState = VisitorState.WaitingInQueue;
                ClearTarget();
                break;

            case VisitorState.WaitingForProblemResolution:
                EnsureRitualStayTarget();
                break;

            case VisitorState.PushingAway:
                currentState = previousState;
                isPushed = false;
                RestoreInterruptedTarget();
                EnsureActiveRouteTarget();
                break;

            case VisitorState.Leaving:
                if (isRitualRouteActive && HandleRitualRouteArrival())
                {
                    break;
                }

                if (isSequentialExitActive && sequentialExitRoutePoints != null && sequentialExitIndex + 1 < sequentialExitRoutePoints.Length)
                {
                    sequentialExitIndex++;
                    Transform nextWaypoint = sequentialExitRoutePoints[sequentialExitIndex];
                    if (nextWaypoint != null)
                    {
                        SetTargetTransform(nextWaypoint);
                    }
                    currentState = VisitorState.Leaving;
                    break;
                }

                FinishLeavingScene();
                break;
        }
    }

    private void FinishLeavingScene()
    {
        ReleaseNStayOccupancy();
        ResetLeavingState();
        currentState = VisitorState.Idle;
        ClearTarget();
        onLeftScene.Invoke();
        HideDialogue();
        gameObject.SetActive(false);
    }

    private Vector3 GetTargetPosition(Transform targetPoint)
    {
        if (targetPoint == null)
        {
            return GetTargetPosition();
        }

        Vector3 targetPosition = targetPoint.position;

        if (keepCurrentY)
        {
            targetPosition.y = transform.position.y;
        }

        return targetPosition;
    }

    private Vector3 ResolveMovementCollisions(Vector3 delta)
    {
        if (bodyCollider == null || delta.sqrMagnitude <= 0.0001f)
        {
            return delta;
        }

        if (!TryCapsuleCast(delta, out RaycastHit hit))
        {
            return delta;
        }

        float moveDistance = Mathf.Max(0f, hit.distance - collisionSkin);
        Vector3 moveToWall = delta.normalized * Mathf.Min(delta.magnitude, moveDistance);
        Vector3 remainingDelta = delta - moveToWall;
        Vector3 slideDelta = Vector3.ProjectOnPlane(remainingDelta, hit.normal);

        if (keepCurrentY)
        {
            slideDelta.y = 0f;
        }

        if (slideDelta.sqrMagnitude <= 0.0001f)
        {
            return moveToWall;
        }

        if (TryCapsuleCast(slideDelta, out RaycastHit slideHit))
        {
            float slideDistance = Mathf.Max(0f, slideHit.distance - collisionSkin);
            slideDelta = slideDelta.normalized * Mathf.Min(slideDelta.magnitude, slideDistance);
        }

        return moveToWall + slideDelta;
    }

    private void GetCapsuleWorldPoints(CapsuleCollider capsule, out Vector3 point1, out Vector3 point2, out float radius)
    {
        Transform capsuleTransform = capsule.transform;
        Vector3 center = capsuleTransform.TransformPoint(capsule.center);
        Vector3 lossyScale = capsuleTransform.lossyScale;

        float scaleX = Mathf.Abs(lossyScale.x);
        float scaleY = Mathf.Abs(lossyScale.y);
        float scaleZ = Mathf.Abs(lossyScale.z);

        switch (capsule.direction)
        {
            case 0:
            {
                radius = capsule.radius * Mathf.Max(scaleY, scaleZ);
                float halfHeight = Mathf.Max(capsule.height * scaleX * 0.5f, radius);
                float offset = halfHeight - radius;
                Vector3 axis = capsuleTransform.right * offset;
                point1 = center + axis;
                point2 = center - axis;
                break;
            }
            case 2:
            {
                radius = capsule.radius * Mathf.Max(scaleX, scaleY);
                float halfHeight = Mathf.Max(capsule.height * scaleZ * 0.5f, radius);
                float offset = halfHeight - radius;
                Vector3 axis = capsuleTransform.forward * offset;
                point1 = center + axis;
                point2 = center - axis;
                break;
            }
            default:
            {
                radius = capsule.radius * Mathf.Max(scaleX, scaleZ);
                float halfHeight = Mathf.Max(capsule.height * scaleY * 0.5f, radius);
                float offset = halfHeight - radius;
                Vector3 axis = capsuleTransform.up * offset;
                point1 = center + axis;
                point2 = center - axis;
                break;
            }
        }
    }

    private bool TryCapsuleCast(Vector3 delta, out RaycastHit hit)
    {
        hit = default;

        if (bodyCollider == null || delta.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector3 point1;
        Vector3 point2;
        float radius;
        GetCapsuleWorldPoints(bodyCollider, out point1, out point2, out radius);

        Vector3 direction = delta.normalized;
        float distance = delta.magnitude + collisionSkin;
        return Physics.CapsuleCast(point1, point2, radius, direction, out hit, distance, ~0, QueryTriggerInteraction.Ignore);
    }

    private Vector3 GetTargetPosition()
    {
        Vector3 targetPosition;

        if (useCustomTarget)
        {
            targetPosition = customTargetPosition;
        }
        else
        {
            targetPosition = currentTarget != null ? currentTarget.position : transform.position;
        }

        if (keepCurrentY)
        {
            targetPosition.y = transform.position.y;
        }

        return targetPosition;
    }

    private void SetTargetTransform(Transform target)
    {
        currentTarget = target;
        useCustomTarget = false;
    }

    private void SetTargetPosition(Vector3 position)
    {
        customTargetPosition = position;
        useCustomTarget = true;
        currentTarget = null;
    }

    private void ClearTarget()
    {
        currentTarget = null;
        useCustomTarget = false;
    }

    private void RestoreInterruptedTarget()
    {
        if (interruptedTarget == null && !interruptedUseCustomTarget)
        {
            return;
        }

        currentTarget = interruptedTarget;
        useCustomTarget = interruptedUseCustomTarget;
        customTargetPosition = interruptedCustomTargetPosition;
    }

    private void UpdateFacing(Vector3 delta)
    {
        if (spriteRenderer == null || Mathf.Abs(delta.x) <= 0.001f)
        {
            return;
        }

        ApplySpriteFacing(delta.x > 0f);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(lastFrameVelocity);
        float planarSpeed = new Vector2(lastFrameVelocity.x, lastFrameVelocity.z).magnitude;
        float speedX = Mathf.Abs(localVelocity.x);
        float speedZ = localVelocity.z;
        bool isMovingForward = speedZ >= speedX;
        bool isMovingBackward = speedZ <= -speedX;
        bool isMoving = planarSpeed > 0.001f;

        bool isLookingDown = false;
        bool isLookingHorizontal = false;
        bool isPlayerNear = false;
        bool canFacePlayer = false;
        if (playerController != null)
        {
            Vector3 playerPosition = playerController.transform.position;
            float distanceToPlayer = Vector3.Distance(transform.position, playerPosition);
            isPlayerNear = distanceToPlayer <= lookAtPlayerRadius;
            canFacePlayer = isPlayerNear
                && !isMoving
                && CanFacePlayerInCurrentState();
            if (canFacePlayer)
            {
                Vector3 localDirection = transform.InverseTransformDirection((playerPosition - transform.position).normalized);
                float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

                isLookingHorizontal = Mathf.Abs(angle) >= 45f && Mathf.Abs(angle) <= 135f;
                isLookingDown = playerPosition.z < transform.position.z && !isLookingHorizontal;
                ApplySpriteFacing(playerPosition.x >= transform.position.x);
            }
        }

        animator.SetFloat(SpeedHash, planarSpeed);
        animator.SetFloat(SpeedXHash, speedX);
        animator.SetFloat(SpeedZHash, speedZ);
        animator.SetBool(IsMovingForwardHash, isMovingForward);
        animator.SetBool(IsMovingBackwardHash, isMovingBackward);
        animator.SetBool(IsLookingDownHash, isLookingDown);
        animator.SetBool(IsLookingHorizontalHash, isLookingHorizontal);
        animator.SetBool(IsPlayerNearHash, canFacePlayer);
    }

    private bool CanFacePlayerInCurrentState()
    {
        return currentState == VisitorState.WaitingAtCounter
            || currentState == VisitorState.WaitingInQueue
            || currentState == VisitorState.WaitingForProblemResolution;
    }

    private void ApplySpriteFacing(bool faceRight)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        bool shouldFaceRight = invertFlipX ? !faceRight : faceRight;
        spriteRenderer.flipX = facesRightByDefault ? !shouldFaceRight : shouldFaceRight;
    }

    private void EnsureTargetTransform(Transform target)
    {
        if (target == null)
        {
            return;
        }

        if (currentTarget != target || useCustomTarget)
        {
            SetTargetTransform(target);
        }
    }

    private void EnsureRouteTarget(IReadOnlyList<Transform> routePoints, int routeIndex)
    {
        if (routePoints == null || routePoints.Count == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(routeIndex, 0, routePoints.Count - 1);
        EnsureTargetTransform(routePoints[clampedIndex]);
    }

    private void ResetLeavingState()
    {
        isSequentialExitActive = false;
        sequentialExitIndex = -1;
        isRitualRouteActive = false;
        ritualRoutePhase = RitualRoutePhase.None;
        ritualApproachIndex = -1;
        ritualExitIndex = -1;
    }

    private void OnDrawGizmosSelected()
    {
        if (startPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPoint.position, 0.15f);
        }

        if (counterPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(counterPoint.position, 0.15f);
        }

        if (exitPoints == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        for (int i = 0; i < exitPoints.Length; i++)
        {
            if (exitPoints[i] != null)
            {
                Gizmos.DrawWireSphere(exitPoints[i].position, 0.15f);
            }
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, lookAtPlayerRadius);
    }
}
