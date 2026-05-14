# Game Rules

## Narrative Frame
- Eternal winter city, post-Soviet bureaucratic setting.
- Player is a ministry clerk-investigator.
- Goal: identify occult affliction and apply correct ritual.

## Main Loop
1. Receive NPC at counter.
2. Gather evidence through dialogue.
3. Ask targeted questions.
4. Infer problem from symptom clues.
5. Execute ritual sequence.
6. Resolve NPC and continue.

## Dialogue Rules
- NPC may have hidden problem (or no problem).
- Problem defines symptom IDs from `NPCProblems.xml`.
- Spoken lines come from `NPCSymptomeLines.xml`.
- Trait controls reliability via truth/lie/question tokens.
- After exhaustion, lines repeat or fallback.

## Ritual Rules
- Ritual sequence is selected by problem name.
- Step = item + action.
- Correct step advances.
- Wrong step damages NPC and resets ritual progress.
- Completed ritual cures NPC and awards points.

## Controls
- `E`: interact with nearest NPC.
- `1..4`: ask question (`Name`, `Gender`, `Age`, `AnotherStory`).
- `Q`: cycle ritual item.
- `Alt + 1..8`: select ritual action.
- `R`: perform ritual step.
- `H`: toggle panel (UI).

## Debug Controls
- `P`: spawn NPC.
- `Z`: send waiting NPC to Z route.
- `N`: send waiting NPC to N route.
