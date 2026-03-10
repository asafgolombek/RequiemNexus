# 🛡️ The Masquerade (Security Policy)

Security in Requiem Nexus is intentional, explicit, and verifiable. **The Masquerade** must be upheld at every perimeter.

## Supported Versions

Requiem Nexus is currently in active development. We address any vulnerabilities found in the main branch or active development branches to preserve the integrity of The Blood of the System.

| Version | Supported |
| ------- | ------------------ |
| `main` branch | ✅ |
| Pre-release versions | ❌ |

## Security Automation (Phase 6 Roadmap)

Requiem Nexus treats security automation as a first-class architectural boundary. As part of the Phase 6 roadmap, the CI/CD pipeline is expected to add:

- **CodeQL** scanning for C#
- **Dependabot** updates (NuGet + GitHub Actions)
- **Secret scanning** (and push protection where supported)
- **Container image** vulnerability scanning for release artifacts
- **SBOM** generation for releases
- **Artifact signing / provenance** (prefer keyless where possible)

This section documents the intent and expected controls. The authoritative implementation lives in GitHub Actions workflows and branch protection rules.

## Cloud Credentials (AWS)

If/when AWS deployments are enabled (Phase 5+), GitHub Actions must authenticate to AWS via **OIDC assume-role**.

- Do not use long-lived AWS access keys in GitHub Secrets.
- Do not store cloud credentials in the repository.

## Formal Inquisition (Reporting a Vulnerability)

If you discover a breach in The Masquerade, do **not** open a public issue. Exposing the flaw publicly invites corruption.

Instead, please report it privately via one of these channels:
- **GitHub**: Use [GitHub's private vulnerability reporting](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability) on this repository.
- **Email**: Contact the repository owner directly via their GitHub profile.

### What to Include

Please provide a formalized audit:
- A clear description of the vulnerability.
- Traceable steps to reproduce the breach.
- Expected impact (e.g., unauthorized access, data compromise, privilege escalation).
- Any recommendations to reinforce the boundary.

### Response Timeline

| Action | Timeframe |
|--------|-----------|
| Acknowledgment of report | Within **48 hours** |
| Initial assessment & severity classification | Within **7 days** |
| Patch release for critical vulnerabilities | Within **14 days** |
| Patch release for non-critical vulnerabilities | Within **30 days** |

We will keep the reporter updated on progress throughout remediation.

## Scope

### In Scope

- Authentication and authorization flaws (bypasses, privilege escalation)
- BOLA / IDOR vulnerabilities (accessing another player's character or chronicle data)
- Injection attacks (SQL, XSS, CSRF)
- Session management weaknesses
- Data exposure or privacy violations
- Cryptographic weaknesses

### Out of Scope

- Vulnerabilities in third-party dependencies (report these upstream; we will track via Dependabot)
- Local development environment issues
- Denial of service via excessive load (unless it reveals an architectural flaw)
- Social engineering attacks

## Safe Harbor

We are committed to working with security researchers in good faith. If you report a vulnerability responsibly:
- We **will not** pursue legal action against you.
- We **will not** publicly disclose your identity without your consent.
- We will credit you in the security advisory (unless you prefer anonymity).

We ask that researchers:
- Make a good-faith effort to avoid privacy violations, data destruction, or disruption of service.
- Only interact with accounts you own or with explicit permission.
- Provide sufficient detail for us to reproduce and validate the issue.

## Threat Modeling & Trust Boundaries

Our security relies on a Zero-Trust identity protocol. Every exposed endpoint serves as a fortified gate and documents:
- Trust boundaries (The Masquerade)
- Expected attacker capabilities
- Failure impact

If you find an undocumented variance between our threat model and the implemented code, it is your duty to report it as a vulnerability or submit a Pull Request to seal the breach.

---

> _"The Masquerade endures only through vigilance."_
