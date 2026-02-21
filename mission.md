# 🩸 Project: Requiem Nexus

## 🌌 The Mission

To build the definitive, high-performance digital ecosystem for **Vampire: The Requiem (Chronicles of Darkness)**.

**Requiem Nexus** is designed to eliminate the friction of character management. By leveraging the reactive power of **.NET 10** and the Antigravity philosophy, we provide a "Beyond"-style experience that is as fast as a digital app but as deep as a leather-bound sourcebook.

---

## 🛠️ Architecture & Scaling Strategy

We build for the future. While we start as a **Modular Monolith** for development speed, our boundaries are defined to scale into **Microservices** as the coterie grows.

- **Service Orchestration:** Utilize **.NET Aspire** to manage local resources (Postgres, Redis) and service discovery.
- **Modular Boundaries:** Separate logic into distinct modules: `Identity.Nexus`, `Sheet.Nexus`, and `Roll.Nexus`.
- **Focus on Scaling Up:**
  - **State Management:** Use distributed caching (Redis) to ensure SignalR sessions can persist across multiple server instances (Horizontal Scaling).
  - **Stateless Logic:** Keep the Rules Engine stateless; calculate derived stats on-demand rather than storing redundant data.
  - **Load Balancing:** Architect for **YARP (Yet Another Reverse Proxy)** to handle rate-limiting and traffic distribution.

---

## 🔐 Security Protocols (The Masquerade)

Data integrity is our highest priority. We treat player data with the same secrecy as a Prince's haven.

- **Zero-Trust Identity:** Implement **OpenID Connect (OIDC)** with short-lived JWTs. Every internal service call requires identity verification.
- **BOLA/IDOR Prevention:** Strict ownership checks on every request. A user can only access a character ID if they are the owner or a designated Storyteller.
- **Input Sanitization:** "Trust no one." All inputs are validated via strict C# Type-checking and parameterized queries to prevent SQL Injection.
- **Encryption at Rest:** Sensitive player and chronicle data is encrypted within the PostgreSQL JSONB columns.

---

## 🎯 Key Objectives

### 1. The Living Sheet

- **Auto-Calculations:** Instant updates for Health, Willpower, Defense, and Speed using .NET 10 performance optimizations.
- **tactile UI:** A bone-white and crimson interface optimized for mobile and desktop.

### 2. The XP Ledger

- **Immutable Logs:** Audit every dot increased or merit purchased.
- **Rule Enforcement:** Backend validation prevents "illegal" character builds.

### 3. The Dice Nexus

- **High-Throughput:** A specialized rolling service supporting 10-again, rote actions, and chance rolls.

---

## 📅 Phase 1: The Neonate (.NET 10)

- [ ] Setup **.NET Aspire** orchestrator for local development.
- [ ] Initialize **EF Core** migrations for the `Character` and `Clan` schemas.
- [ ] Build the `DiceService.cs` using .NET 10's improved Random number generation.
- [ ] Implement basic JWT-based authentication.

---

> _"The blood is the life... but the data is the legacy."_
