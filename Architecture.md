# 📐 Requiem Nexus Architecture

## 🪐 Antigravity Architecture

Requiem Nexus follows the **Antigravity Philosophy**:

> Systems must reduce cognitive weight, not add to it.

This document defines the **architectural laws** of the system.  
Breaking these rules requires explicit justification and documentation.

---

## 🧠 Antigravity Rules of Thumb

These rules apply to **all layers**: UI, application logic, domain logic, and infrastructure.

1. **If it’s implicit, it’s a bug waiting to happen**  
   All state transitions must be explicit and traceable.

2. **State must be visible or eliminable**  
   Hidden state is forbidden. Cached state must be invalidatable.

3. **Magic is debt**  
   Framework conveniences are acceptable only when fully understood and documented.

4. **Traceability beats cleverness**  
   Code should be readable by a tired developer at 2 a.m.

5. **One reason to change per module**  
   Violations of SRP are architectural defects.

6. **No silent failure—ever**  
   Fail fast, log clearly, surface safely.

7. **Teach the system by reading the code**  
   Code is documentation. Comments explain _why_, not _what_.

8. **If debugging is hard, the design is wrong**  
   Debuggability is a first-class requirement.

9. **Performance is a feature, not an optimization**  
   Efficiency must be designed, not retrofitted.

10. **Every shortcut must be temporary—and documented**  
    Technical debt must have a due date.

---

## 🧱 Architectural Layers

The system is structured into **explicit layers** with strict boundaries.

### 1. Presentation Layer (`Web`)

- UI components and reactive state
- No business rules
- No database access
- All inputs validated before passing inward
- **Real-Time boundaries**: SignalR/WebSockets reside here, pushing state updates to clients without holding authoritative game state.

**Allowed dependencies:** Application layer only

---

### 2. Application Layer

- Orchestrates use cases
- Coordinates domain operations
- Handles authorization and validation flows

**Must not:**

- Contain persistence logic
- Encode game rules directly

---

### 3. Domain Layer

- Game rules and invariants
- Derived stat calculations
- Dice mechanics logic

This layer is:

- Stateless
- Deterministic
- Fully unit-testable

---

### 4. Infrastructure Layer (`Data`, external services)

- EF Core mappings
- Database migrations
- External integrations (Redis, Identity, etc.)
- **Open API / Extensibility**: Any external entry points (REST/gRPC) must reside here, secured via the same zero-trust principles as the primary UI.

Infrastructure **serves** the domain, never the reverse.

---

## 🧬 Domain Boundaries

Each domain owns:

- Its own models
- Its own invariants
- Its own persistence mappings

Cross-domain interaction is only allowed via **explicit contracts**.

🚫 Shared “Common” or “Utils” projects are forbidden.

---

## 🔁 State Management Rules

- All mutable state changes must be:
  - Intentional
  - Logged
  - Observable
- **Event Sourcing (Audit Trails)**: Critical domain transitions (e.g., spending XP, suffering Aggravated damage) must be recorded as explicit historical events, rather than just mutating the current value.
- Derived state must never be stored unless proven necessary.

---

## 🎲 Dice Nexus Architecture

- Dice rolls are:
  - Stateless
  - Deterministic when seeded
  - Auditable

- No UI component performs probability logic directly.

---

## 🧭 Observability as Architecture

Every major action emits:

- Logs (who, what, when)
- Metrics (frequency, latency)
- Correlation IDs

If it cannot be observed, it is architecturally incomplete.

---

## 🚫 Architectural Non-Goals

- Generic VTT behavior
- Implicit “magic” pipelines
- Framework-driven design
- Shared mutable state across domains

---

> _Architecture is frozen intent.  
> Make it intentional._
