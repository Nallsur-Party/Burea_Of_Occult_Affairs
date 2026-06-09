# Tech Constraints

- Target: PC first
- Singleplayer only
- No multiplayer planned
- Prefer simple MonoBehaviour architecture
- Avoid ECS unless absolutely necessary
- Avoid heavy runtime allocations
- Pixel-art + 3D environment oriented presentation
- NPC logic should remain data-driven through `NPC` case type and catalogs rather than a separate AI framework.
- Global runtime services are acceptable when they are scene-persistent and deliberately narrow in scope, such as the shift clock and debug hub.
- TV/news generation should stay data-driven through XML catalogs and loaders instead of hardcoded story assembly.
- Scene binding should prefer explicit component references or controlled runtime discovery over a large service framework.
