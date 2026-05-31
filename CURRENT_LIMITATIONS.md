# Current Limitations

- Ritual / Sanation sequences are still hardcoded and not easy to extend.
- NPC memory is not persistent between sessions; client history is lost when the game restarts.
- NPC case taxonomy is still narrow and only distinguishes `None`, `Paranormal`, and `NonParanormal`; the non-paranormal branch is only a filtering layer for now.
- Queue and NPC routing currently support only one active service flow.
- NPC movement into the ritual / sanation zone remains fragile and can still suffer orientation and collision issues.
- NPC archive is implemented, but not yet a full save/export feature.

> These are the current limitations to keep in mind when improving the system.
