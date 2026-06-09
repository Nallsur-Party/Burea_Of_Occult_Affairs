# AI Context

When modifying code and documentation:
- Preserve the current gameplay loop.
- Prefer extending existing systems instead of adding new frameworks.
- Avoid third-party frameworks, plugins, and large architectural rewrites.
- Keep the code simple and beginner-friendly for Unity developers.
- Prefer explicit solutions over deep abstraction.
- Treat the game as a bureaucratic procedure for the supernatural, not as fantasy magic.
- Use consistent terminology: `Ritual / Sanation`.
- Treat `WorkShiftTimeSystem` as part of core runtime flow when changes affect action costs, shift end behavior, or clock presentation.
- Treat `RuntimeDebugHub` and the TV/news pipeline as supported runtime systems, not throwaway test code.
- Use the NPC case model explicitly: `None`, `Paranormal`, `NonParanormal`.
- Treat only `Paranormal` NPC as bureau patients; `None` and `NonParanormal` must not enter the patient archive.
- `NonParanormal` means false or non-bureau requests such as ordinary illness, mental disorder, self-suggestion, fraud, paperwork error, or other non-paranormal complaints.
- Keep responsibility boundaries clear: `Player`, `NPC`, `Ritual`, `UI`, `Camera`, `TV`, `Shared`, and global runtime services.
- If you add new mechanics, integrate them through existing catalogs and data files.
- Review changes as if you are a bureaucratic engineer: fix the anomaly, fill the form, send the next client.
