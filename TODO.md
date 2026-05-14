# TODO

## Roadmap Direction
- Preserve and polish current office loop (intake -> diagnosis -> ritual).
- Expand into mixed visitor/task gameplay.
- Add external locations.
- Add occult creatures as progression tools.

## High Priority
- [ ] Fix text encoding/mojibake in Russian strings across scripts/data pipeline.
- [ ] Improve ritual UX feedback to reduce punitive trial-and-error feel.
- [ ] Validate diary clarity against real symptom/problem mapping complexity.

## Feature Track A: Orders / Requests
- [ ] Introduce visitors without occult illness who provide tasks/orders.
- [ ] Define order types (retrieve object, investigate anomaly, escort/protection, info contract).
- [ ] Add order progression parallel to treatment cases.
- [ ] Extend NPC/visitor model with intent type (`case`, `order`, `mixed`).

## Feature Track B: Locations
- [ ] Implement first external location playable from an order.
- [ ] Decide world-building strategy:
  - handcrafted,
  - procedural,
  - or hybrid (recommended initial hypothesis).
- [ ] Add location manager for order-driven transitions.

## Feature Track C: Occult Animals
- [ ] Add collectible/rescuable occult creature entities.
- [ ] Add curse/healing loop for creatures (ritual-compatible).
- [ ] Implement first creature utility ability:
  - interrogation assist, or
  - ritual assist, or
  - exploration trace detection.

## Suggested Delivery Order
1. [ ] Minimal order system in office scene using current queue/dialogue stack.
2. [ ] One handcrafted field location + one order that uses it.
3. [ ] One end-to-end occult creature vertical slice.
4. [ ] Re-evaluate need and scope of procedural generation after polished handcrafted baseline.

## Risks to Manage
- [ ] Scope explosion from developing office loop, field loop, and creatures in parallel.
- [ ] Narrative quality loss if procedural generation is adopted too early.
- [ ] UI overload risk if diary expansion is not information-architecture-first.
