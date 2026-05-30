# Game Rules

## Narrative Frame
- Eternal winter city, post-Soviet ruins, tired people.
- Player is a clerk-investigator in an ordinary bureau serving citizens.
- The goal is not to expel a demon, but to diagnose an anomaly and perform ritual / sanation.

## World and Tone
- The paranormal is treated as a civic problem.
- This is not fantasy religion or mystical horror.
- It is a "public service for the occult" with forms and old instructions.
- The mood is melancholy, fatigue, and mundane despair.

## Important terminology
- Anomaly = hidden problem trace.
- Ritual / Sanation = procedural treatment, disinfection, technical operation.
- NPC = bureau client with a problem.
- Problem is defined by symptoms and trace type.

## Main Loop
1. NPC appears at the queue or counter.
2. Player engages in dialogue and asks questions.
3. Symptoms and clues are gathered from answers.
4. Player chooses the appropriate ritual / sanation for the diagnosed problem.
5. A sanation step is performed as item + action.
6. Correct sequence cures the NPC; mistakes deal damage and reset progress.

## Dialogue Rules
- NPC may have a hidden problem or no problem at all.
- The problem defines symptom IDs from `NPCProblems.xml`.
- NPC lines come from `NPCSymptomeLines.xml`.
- NPC traits control answer reliability: truth, lie, evasive.
- When lines run out, repeats or fallback phrases are used.

## Ritual / Sanation Rules
- Ritual / Sanation is selected by problem name.
- A step consists of an item and an action.
- Correct step advances the procedure.
- A wrong step damages the NPC and resets progress.
- Completed ritual / sanation cures the NPC and closes the case.

## Controls
- `E`: interact with nearest NPC.
- `1..4`: ask a question (`Name`, `Gender`, `Age`, `AnotherStory`).
- `Q`: cycle ritual / sanation item.
- `Alt + 1..8`: select ritual / sanation action.
- `R`: perform the current ritual / sanation step.
- `H`: toggle the panel.

## Debug Controls
- `P`: spawn NPC.
- `Z`: send waiting NPC to route Z.
- `N`: send waiting NPC to route N.
