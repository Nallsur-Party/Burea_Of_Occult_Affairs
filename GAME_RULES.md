# Game Rules

## Narrative Frame
- Setting: post-Soviet city covered by eternal winter after an old occult event.
- Role: employee of the Ministry of Occult Affairs.
- Job: process citizens, identify occult affliction by symptoms, perform corrective ritual.

## Main Loop
1. Wait for next NPC at counter/queue.
2. Interact and gather evidence through dialogue.
3. Ask targeted questions for additional facts.
4. Map observed symptoms to likely occult problem in diary.
5. Execute ritual sequence for selected problem.
6. Resolve NPC (cure or fail state) and continue.

## Dialogue and Evidence Rules
- Each NPC may have a hidden problem (or no problem).
- Problem defines a set of symptom IDs from `NPCProblems.xml`.
- Dialogue lines are sampled from `NPCSymptomeLines.xml` for those symptoms.
- NPC trait impacts reliability through token profile:
  - Truth tokens
  - Lie tokens
  - Detective question tokens
- After token exhaustion, NPC can repeat past lines or fallback lines.
- `AnotherStory` can produce unstable/contradictory content for specific symptom combinations.

## Deduction Rules
- Ground truth of diagnosis is symptom-set mapping in `NPCProblems.xml`.
- Diary is used to eliminate impossible occult traces and converge to one problem.
- Wrong diagnosis usually leads to wrong ritual sequence and health loss.

## Ritual Rules
- Ritual sequence is determined by problem name in `RitualSolutionCatalog`.
- Step format: `(RitualItemType, RitualActionType)`.
- Correct next step advances ritual.
- Wrong step:
  - deals ritual damage to NPC (`WrongStepDamage=1`)
  - clears current ritual progress
  - may force restart when health reaches zero.
- Completion:
  - marks NPC as cured
  - awards ritual points
  - routes NPC to exit logic.

## Player Inputs
- `E`: interact/start conversation with nearest interactable NPC.
- `1..4` (without Alt): ask question type (`Name`, `Gender`, `Age`, `AnotherStory`).
- `Q`: cycle ritual item.
- `Alt + 1..8`: select ritual action.
- `R`: perform ritual step.
- `H`: toggle draggable/toggle panel (UI script default).

## Debug Inputs
- `P`: spawn NPC.
- `Z`: send next waiting NPC to `Z` exit route logic.
- `N`: send next waiting NPC to `N` (hold-until-cured) route logic.

## Failure and Recovery Semantics
- If ritual progress is lost on mistake, player must restart ritual sequence.
- If NPC health is depleted, ritual may require re-initiation via renewed interaction flow.
- Strong feedback clarity is important because rule strictness is high.
