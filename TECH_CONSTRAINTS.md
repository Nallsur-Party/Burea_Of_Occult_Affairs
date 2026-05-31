# Tech Constraints

- Target: PC first
- Singleplayer only
- No multiplayer planned
- Prefer simple MonoBehaviour architecture
- Avoid ECS unless absolutely necessary
- Avoid heavy runtime allocations
- Pixel-art + 3D environment oriented presentation
- NPC logic should remain data-driven through `NPC` case type and catalogs rather than a separate AI framework.
