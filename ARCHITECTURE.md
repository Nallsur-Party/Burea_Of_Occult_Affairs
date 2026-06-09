# Architecture

## Structure Style
Feature slices first: group files by game feature and responsibility, not by technical layer.

## Core organization
If you add new code, place it in the proper feature folder:
- `Player/` — movement, input, player state.
- `NPC/` — generation, dialogue, queue, routing, archive.
- `Ritual/` — ritual / sanation logic, step validation, results.
- `UI/` — interface, panels, diary.
- `TV/` — news generation, mappings, presenters, data loading.
- `Camera/` — camera and bounds.
- `Shared/` — shared helper components.

## Expected folders and responsibilities

### Player/
- `Player.cs`: player runtime state.
- `PlayerController.cs`: movement, interaction, dialogue input, ritual / sanation input.
- `PlayerProfile.cs`: player constraints and parameters.

### Runtime/
- `WorkShiftTimeSystem.cs`: shift clock, action cost accounting, shift end notification.
- `WorkShiftClockPresenter.cs`: clock hand presentation bound to the shift clock.
- `RuntimeDebugHub.cs`: persistent debug UI, scene rebinding, debug actions.

### NPC/

#### NPC/Data/
- `NPC.cs`: NPC state model with identity, case type, paranormal problem, non-paranormal condition, symptoms, tokens, health.
- `NPCCaseType.cs`: NPC case classification.
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

### TV/
- `NewsData.cs`: data model for problem texts and templates.
- `NewsDataLoader.cs`: XML loading and normalization.
- `NewsGenerator.cs`: selects event type, severity, text, and template.
- `NewsTextMeshProPresenter.cs`: news display and pinned NPC presentation.
- `NewsMappings.xml`, `NewsTemplates.xml`, `ProblemNews.xml`: news content and mappings.

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
1. `WorkShiftTimeSystem` tracks the shift clock and blocks time-consuming actions after shift end.
2. `NPC/Generation` creates NPC with hidden case type and associated data.
3. `NPC/Queue` brings NPC to the counter and manages waiting order.
4. Player interaction triggers `NPC/Dialogue` responses and can spend time for questions.
5. Player infers the problem via diary UI, symptom logic, and optional `TV/` news presentation/debugging.
6. `Ritual/` validates ritual / sanation item+action sequence against the paranormal problem only and spends shift time when started.
7. `Paranormal` NPC can be cured; `None` and `NonParanormal` NPC leave without bureau treatment.

## Important invariants
- Ritual / Sanation lookup key is the NPC paranormal problem name.
- `NPC.CaseType` is the primary routing flag for archive, dialogue interpretation, and treatment eligibility.
- `None` means no case, `Paranormal` means a bureau patient, and `NonParanormal` means a false or non-bureau case.
- `WorkShiftTimeSystem` is persistent across scenes and owns the shift costs for inspection, questions, and rituals.
- If no ritual catalog asset is assigned, runtime default catalog is used.
- `NpcOrderVisitor` owns NPC scene lifecycle and exit behavior.
- Dialogue bubble ownership stays on NPC side, not player side.
- New NPC storage logic should remain a separate service, not mixed with queue/routing.
