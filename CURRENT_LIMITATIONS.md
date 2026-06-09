# Current Limitations

- Ritual / Sanation sequences are still catalog-based but remain tightly coupled to the current validation flow and action costs.
- NPC memory is not persistent between sessions; client history is lost when the game restarts.
- NPC case taxonomy is still narrow and only distinguishes `None`, `Paranormal`, and `NonParanormal`; the non-paranormal branch is still mostly a filtering and flavor layer.
- Queue and NPC routing currently support only one active office/service flow.
- Time-based gameplay is present, but the shift clock is still a global runtime service and is not yet deeply integrated with every subsystem.
- `RuntimeDebugHub` is useful, but it is still a scene-bound debug layer rather than a full in-game operations console.
- `TV/` news generation is data-driven, but the news pipeline still depends on current mappings and manual content curation.
- NPC archive is implemented, but not yet a full save/export feature.

> These are the current limitations to keep in mind when improving the system.
