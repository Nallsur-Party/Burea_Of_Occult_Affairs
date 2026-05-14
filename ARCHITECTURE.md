# Architecture

## Scope
This file describes technical structure under `Assets/Scripts`.

## Top-Level Modules
- `Player/`: player state, controller, profile.
- `NPC/`: NPC data model, generation, queue actor, dialogue logic, XML loading.
- `Ritual/`: ritual catalog, step validation, progress and reward.
- Root UI scripts: diary page switching and draggable/toggle panels.
- `Camera/`, `Billboard.cs`: support systems.

## Core Runtime Objects and Responsibilities

### Player
- `Assets/Scripts/Player/Player.cs`
  - Holds selected ritual item/action and runtime flags (`IsFacingRight`, `IsGrounded`, `IsSprinting`).
- `Assets/Scripts/Player/PlayerController.cs`
  - Movement and interaction loop.
  - Owns player input mapping to dialogue and ritual systems.
  - Finds `NPCSpawner`, `NPCQueueManager`, `RitualManager` in scene.
  - Creates runtime `RitualManager` if absent.
- `Assets/Scripts/Player/PlayerProfile.cs`
  - Investigator constraints (currently interrogation limit setting).

### NPC Data Layer
- `Assets/Scripts/NPC/NPC.cs`
  - Stateful data object (not MonoBehaviour):
  - Identity: name, gender, age, trait.
  - Case data: problem name, symptom IDs and symptom texts.
  - Dialogue economy: truth/lie tokens + detective question tokens.
  - Dialogue memory: asked questions, remembered answers, history/repetition state.
  - Ritual state: health/maxHealth, cured/alive.
- `Assets/Scripts/NPC/NPCProblemDefinition.cs`, `NPCProblemCatalog.cs`
- `Assets/Scripts/NPC/NPCSymptomLinesCatalog.cs`
- `Assets/Scripts/NPC/NPCTraitFallbackCatalog.cs`

### NPC Data Loaders
- `Assets/Scripts/NPC/NPCProblemsLoader.cs`
  - Parses `NPCProblems.xml` into problem catalog.
- `Assets/Scripts/NPC/NPCSymptomLinesLoader.cs`
  - Parses `NPCSymptomeLines.xml` into symptom->lines map.
- `Assets/Scripts/NPC/NPCTraitFallbackLoader.cs`
  - Parses fallback trait lines.
- `Assets/Scripts/NPC/NPCSymptomCategoriesLoader.cs`
  - Parses diary category grouping.

### NPC Generator
- `Assets/Scripts/NPC/NPCGenerator.cs`
  - Loads catalogs and name pools.
  - Creates random NPCs with optional "no problem" chance.
  - Precomputes prepared conversation/fallback lines.

### NPC World Actor and Queue
- `Assets/Scripts/NPC/NpcOrderVisitor.cs`
  - In-scene NPC finite-state behavior:
  - Counter movement, queue wait, leave routes, hold-until-cured behavior.
  - Delegates dialogue text generation to generator/utility.
  - Hosts dialogue bubble + health bar visibility/state sync.
- `Assets/Scripts/NPC/NPCQueueManager.cs`
  - Queue container, queue capacity, queue point assignment, reorder refresh.
- `Assets/Scripts/NPC/NPCSpawner.cs`
  - Spawns NPC visitors into active flow.

### Dialogue Logic
- `Assets/Scripts/NPC/NPCDialogueUtility.cs`
  - Trait token profiles and random trait assignment.
  - Selection of prepared symptom lines, fallback lines, repeated lines.
  - Question answering logic by question type.
  - Special behavior for symptoms `S1`, `S4`, `S18`.

### Ritual
- `Assets/Scripts/Ritual/RitualManager.cs`
  - Per-NPC ritual session storage.
  - Start ritual from NPC `ProblemName`.
  - Validate each submitted step against expected sequence.
  - Wrong step: damage + clear progress.
  - Completion: cure NPC + award points + release NPC.
- `Assets/Scripts/Ritual/RitualSolutionCatalog.cs`
  - Problem -> sequence map. Uses assigned asset or runtime defaults.
- `Assets/Scripts/Ritual/RitualSolutionDefinition.cs`
- `Assets/Scripts/Ritual/RitualStepDefinition.cs`
- `Assets/Scripts/Ritual/RitualItemType.cs`
- `Assets/Scripts/Ritual/RitualActionType.cs`
- `Assets/Scripts/Ritual/RitualPointsUI.cs`

### UI
- `Assets/Scripts/UIDiaryPageController.cs`
  - Collects pages and activates one at a time.
- `Assets/Scripts/UIDraggableTogglePanel.cs`
  - Panel visibility toggle (`H` by default).
- `Assets/Scripts/UIDraggablePanel.cs`
  - Drag support.

## Data Flow (Case)
1. `NPCGenerator` loads XML catalogs.
2. NPC is generated with problem + symptom IDs (+ prepared lines).
3. `NpcOrderVisitor` exposes generated NPC to player interaction.
4. `PlayerController` requests dialogue/question responses.
5. Player infers problem and submits ritual steps to `RitualManager`.
6. `RitualManager` checks sequence via `RitualSolutionCatalog`, updates NPC state, and awards points.

## Invariants
- Ritual matching key is exact problem name string (case-insensitive lookup in catalog).
- If no explicit ritual catalog asset is provided, runtime default catalog is used.
- `NpcOrderVisitor` owns NPC scene lifecycle and disables object on final leave.
- Dialogue UI ownership is NPC-side (`NPCDialogueBubble`), not player-side.
