# Architecture

## Structure Style
Feature slices first: group files by game feature and responsibility, not by technical layer.

## Core organization
If you add new code, place it in the proper feature folder:
- `Player/` — movement, input, player state.
- `NPC/` — generation, dialogue, queue, routing, archive.
- `Ritual/` — ritual / sanation logic, step validation, results.
- `UI/` — interface, panels, diary.
- `Camera/` — camera and bounds.
- `Shared/` — shared helper components.

## Expected folders and responsibilities

### Player/
- `Player.cs`: player runtime state.
- `PlayerController.cs`: movement, interaction, dialogue input, ritual / sanation input.
- `PlayerProfile.cs`: player constraints and parameters.

### NPC/

#### NPC/Data/
- `NPC.cs`: NPC state model with identity, problem, symptoms, tokens, health.
- `NPCProblemDefinition.cs`, `NPCProblemCatalog.cs`
- `NPCSymptomCategoryDefinition.cs`, `NPCSymptomCategoryCatalog.cs`
- `NPCSymptomLinesCatalog.cs`
- `NPCTraitFallbackCatalog.cs`
- `NPCTraitType.cs`, `NPCTraitTokenProfile.cs`, `NPCQuestionType.cs`

#### NPC/Dialogue/
- `NPCDialogueUtility.cs`: dialogue rules, token consumption, repeats, symptom-specific behavior.
- `NPCDialogueBubble.cs`: NPC speech display.

#### NPC/Queue/
- `NPCQueueManager.cs`: queue state, capacity, target assignment.
- `NpcOrderVisitor.cs`: NPC state machine for counter approach, routing, exit.
- `NPCSpawner.cs`: NPC spawn flow.

#### NPC/Archive/
- `NPCArchiveService.cs`: NPC save/archive logic.
- `NPCArchiveEntry.cs`: archive entry.

#### NPC/Generation/
- `NPCGenerator.cs`: load catalogs and generate NPC data.
- `NameList.xml`: name pools.

#### NPC/Loaders/
- `NPCProblemsLoader.cs`
- `NPCSymptomLinesLoader.cs`
- `NPCTraitFallbackLoader.cs`
- `NPCSymptomCategoriesLoader.cs`

#### NPC/Content/
- `NPCProblems.xml`
- `NPCSymptomeLines.xml`
- `NPCTraitFallbackLines.xml`
- `NPCSymptomCategories.xml`

### Ritual/
- `RitualManager.cs`: ritual / sanation progress, validation, result handling.
- `RitualSolutionCatalog.cs`: problem-to-sequence map.
- `RitualSolutionDefinition.cs`, `RitualStepDefinition.cs`
- `RitualItemType.cs`, `RitualActionType.cs`, `RitualActionTypeExtensions.cs`
- `RitualAttemptResult.cs`
- `RitualPointsUI.cs`

### UI/
- `UIDiaryPageController.cs`
- `UIDraggablePanel.cs`
- `UIDraggableTogglePanel.cs`

### Camera/
- `CameraFollow.cs`
- `CameraBoundsZone.cs`
- `CameraBoundsTriggerRelay.cs`

### Shared/
- `Billboard.cs`
- `NPCHealthBar.cs`

## Runtime Flow
1. `NPC/Generation` creates NPC with hidden problem and symptom IDs.
2. `NPC/Queue` brings NPC to the counter and manages waiting order.
3. Player interaction triggers `NPC/Dialogue` responses.
4. Player infers the problem via diary UI and symptom logic.
5. `Ritual/` validates ritual / sanation item+action sequence against the problem.
6. NPC is cured (success) or damaged/reset (failure), then leaves via route.

## Important invariants
- Ritual / Sanation lookup key is the NPC problem name.
- If no ritual catalog asset is assigned, runtime default catalog is used.
- `NpcOrderVisitor` owns NPC scene lifecycle and exit behavior.
- Dialogue bubble ownership stays on NPC side, not player side.
- New NPC storage logic should remain a separate service, not mixed with queue/routing.
