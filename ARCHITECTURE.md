# Architecture

## Scope
This file describes technical structure under `Assets/Scripts`.

## Top-Level Modules
- `Player/`: player state, controller, profile.
- `NPC/`: NPC data model, generation, queue actor, dialogue logic, XML loading.
- `Ritual/`: ritual catalog, step validation, progress and reward.
- UI scripts: diary page switching and draggable/toggle panels.
- Support: camera scripts and billboard behavior.

## Core Runtime Objects

### Player
- `Player/Player.cs`: selected ritual item/action and runtime flags.
- `Player/PlayerController.cs`: movement, interaction, dialogue input, ritual input.
- `Player/PlayerProfile.cs`: interrogation constraints.

### NPC Data Layer
- `NPC/NPC.cs`: identity, case data, token economy, dialogue memory, ritual health/cure state.
- `NPC/NPCProblemCatalog.cs`, `NPC/NPCProblemDefinition.cs`
- `NPC/NPCSymptomLinesCatalog.cs`
- `NPC/NPCTraitFallbackCatalog.cs`

### NPC Loaders
- `NPC/NPCProblemsLoader.cs`
- `NPC/NPCSymptomLinesLoader.cs`
- `NPC/NPCTraitFallbackLoader.cs`
- `NPC/NPCSymptomCategoriesLoader.cs`

### NPC Generator + World Actor
- `NPC/NPCGenerator.cs`: loads catalogs, builds NPCs, precomputes dialogue pools.
- `NPC/NpcOrderVisitor.cs`: movement/state machine, dialogue UI integration, exit logic.
- `NPC/NPCQueueManager.cs`: queue state and queue point placement.
- `NPC/NPCSpawner.cs`: spawns NPC visitors.

### Dialogue Logic
- `NPC/NPCDialogueUtility.cs`: token profiles, line selection, repetition, special symptom behavior.

### Ritual
- `Ritual/RitualManager.cs`: ritual start/progress/validation/reward.
- `Ritual/RitualSolutionCatalog.cs`: problem -> step sequence map.
- `Ritual/RitualSolutionDefinition.cs`
- `Ritual/RitualStepDefinition.cs`
- `Ritual/RitualItemType.cs`
- `Ritual/RitualActionType.cs`
- `Ritual/RitualPointsUI.cs`

### UI
- `UIDiaryPageController.cs`
- `UIDraggableTogglePanel.cs`
- `UIDraggablePanel.cs`

## Case Data Flow
1. Generator loads XML catalogs.
2. NPC is created with hidden problem and symptom IDs.
3. Player obtains clues via dialogue.
4. Player infers problem in diary.
5. RitualManager validates submitted sequence.
6. NPC is cured or damaged depending on correctness.

## Invariants
- Ritual lookup key is NPC problem name.
- Runtime default ritual catalog is used if no asset is assigned.
- NPC scene lifecycle is controlled by `NpcOrderVisitor`.
- Dialogue bubble ownership is NPC-side.
