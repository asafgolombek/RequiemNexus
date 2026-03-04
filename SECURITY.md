# 🛡️ The Masquerade (Security Policy)

Security in Requiem Nexus is intentional, explicit, and verifiable. **The Masquerade** must be upheld at every perimeter.

## Supported Versions

Requiem Nexus is currently in active development. We address any vulnerabilities found in the main branch or active development branches to preserve the integrity of The Blood of the System.

| Version | Supported |
| ------- | ------------------ |
| `main` branch | ✅ |
| Pre-release versions | ❌ |

## Formal Inquisition (Reporting a Vulnerability)

If you discover a breach in The Masquerade, do **not** open a public issue. Exposing the flaw publicly invites corruption.

Instead, please report it privately by emailing the repository owner or using GitHub's private vulnerability reporting feature.

Please provide a formalized audit:
- A bone-white and crimson clear description of the vulnerability.
- Traceable steps to reproduce the breach.
- Expected impact (e.g., unauthorized access, data compromise).
- Any recommendations to reinforce the boundary.

We will acknowledge your report within 48 hours and coordinate the remediation.

## Threat Modeling & Trust Boundaries

Our security relies on a Zero-Trust identity protocol. Every exposed endpoint serves as a fortified gate and documents:
- Trust boundaries (The Masquerade)
- Expected attacker capabilities
- Failure impact

If you find an undocumented variance between our threat model and the implemented code, it is your duty to report it as a vulnerability or submit a Pull Request to seal the breach.
