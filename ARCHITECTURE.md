# Architecture

## Structure Style
Feature slices first: group files by game feature and responsibility, not by technical layer.

## Proposed Feature Slices

### Player/
- `Player.cs`: lightweight runtime state (facing, grounded, sprinting, selected ritual item/action).
- `PlayerController.cs`: movement, interaction, dialogue input, ritual input.
- `PlayerProfile.cs`: investigator constraints.

### NPC/

#### NPC/Data/
- `NPC.cs`: core NPC state model (identity, problem, symptoms, tokens, memory, ritual health).
- `NPCProblemDefinition.cs`, `NPCProblemCatalog.cs`
- `NPCSymptomCategoryDefinition.cs`, `NPCSymptomCategoryCatalog.cs`
- `NPCSymptomLinesCatalog.cs`
- `NPCTraitFallbackCatalog.cs`
- `NPCTraitType.cs`, `NPCTraitTokenProfile.cs`, `NPCQuestionType.cs`

#### NPC/Dialogue/
- `NPCDialogueUtility.cs`: dialogue rules, token consumption, repeated/fallback lines, symptom-specific behavior.
- `NPCDialogueBubble.cs`: in-world dialogue UI behavior.

#### NPC/Queue/
- `NPCQueueManager.cs`: queue state, capacity and target assignment.
- `NpcOrderVisitor.cs`: world actor FSM (counter, queue, exits, hold-until-cured behavior).
- `NPCSpawner.cs`: NPC spawn flow entry.

#### NPC/Generation/
- `NPCGenerator.cs`: loads catalogs and generates NPC data.
- `NameList.xml`: name pools.

#### NPC/Loaders/
- `NPCProblemsLoader.cs`
- `NPCSymptomLinesLoader.cs`
- `NPCTraitFallbackLoader.cs`
- `NPCSymptomCategoriesLoader.cs`

#### NPC/Content/
- `NPCProblems.xml`: symptom pool + problem->symptom mapping.
- `NPCSymptomeLines.xml`: dialogue lines per symptom.
- `NPCTraitFallbackLines.xml`: fallback lines per trait.
- `NPCSymptomCategories.xml`: diary symptom grouping.

### Ritual/
- `RitualManager.cs`: per-NPC ritual progress, validation, result handling.
- `RitualSolutionCatalog.cs`: problem->sequence map (asset or runtime default).
- `RitualSolutionDefinition.cs`, `RitualStepDefinition.cs`
- `RitualItemType.cs`, `RitualActionType.cs`, `RitualActionTypeExtensions.cs`
- `RitualAttemptResult.cs`
- `RitualPointsUI.cs`

### UI/
- `UIDiaryPageController.cs`: page switching for diary.
- `UIDraggablePanel.cs`: draggable panel behavior.
- `UIDraggableTogglePanel.cs`: panel visibility toggle.

### Camera/
- `CameraFollow.cs`
- `CameraBoundsZone.cs`
- `CameraBoundsTriggerRelay.cs`

### Shared/
- `Billboard.cs`
- `NPCHealthBar.cs` (NPC-adjacent visual support)

## Runtime Flow (Case Processing)
1. `NPC/Generation` creates NPC with hidden problem and symptom IDs.
2. `NPC/Queue` brings NPC to counter and manages waiting order.
3. Player interaction triggers `NPC/Dialogue` responses.
4. Player infers problem via diary UI and symptom logic.
5. `Ritual/` validates item+action sequence against problem.
6. NPC is cured (success) or damaged/reset (failure), then leaves via queue/exit logic.

## Invariants
- Ritual lookup key is NPC problem name.
- If no ritual catalog asset is assigned, runtime default catalog is used.
- `NpcOrderVisitor` owns NPC scene lifecycle and exit behavior.
- Dialogue bubble ownership is NPC-side, not player-side.
