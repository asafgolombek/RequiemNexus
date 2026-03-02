# Security Policy

## Supported Versions

Requiem Nexus is currently in active development. As such, there are no official releases that receive long-term security updates. However, we take security seriously and are committed to addressing any vulnerabilities found in the main branch or active development branches.

| Version | Supported |
| ------- | ------------------ |
| `main` branch | :white_check_mark: |
| Pre-release versions | :x: |

## Reporting a Vulnerability

Security is intentional, explicit, and verifiable in Requiem Nexus. If you discover a security vulnerability, please do **not** open a public issue. 

Instead, please report it privately by emailing the repository owner or using GitHub's private vulnerability reporting feature (if enabled on the repository).

Please include the following in your report:
- A description of the vulnerability.
- Steps to reproduce the issue.
- Potential impact (e.g., data leak, unauthorized access).
- Any potential mitigation or fix you might suggest.

We will endeavor to respond to vulnerability reports within 48 hours and will keep you updated on the progress of the investigation and any subsequent fixes.

## Threat Modeling

As outlined in our Mission, every exposed endpoint in our application aims to document:
- Trust boundaries
- Expected attacker capabilities
- Failure impact

If you find a discrepancy between our documented threat model and the actual implementation, please feel free to report it as a vulnerability or submit a Pull Request to address the gap.
