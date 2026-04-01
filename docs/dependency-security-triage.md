# Dependency and static-analysis security triage

## CodeQL (`.github/workflows/codeql.yml`)

1. Open the repository **Security** tab → **Code scanning** alerts.
2. For each open alert: classify **True positive** (fix or track issue), **False positive** (dismiss with reason), or **Won’t fix** (document risk acceptance).
3. Prefer fixes in **Application** / **Domain** over suppressions; suppress only with a code comment or CodeQL configuration justified in the PR.
4. Re-run analysis on the branch after fixes before merge.

## Dependabot

1. Review **Dependabot** pull requests and the **Dependency graph** for known vulnerabilities.
2. Apply **patch** and **minor** updates per project policy; **major** bumps require explicit review (see `.github/CODEOWNERS` and `AGENTS.md`).
3. If a CVE has no fixed version yet, document mitigation (e.g. feature flags, input validation, network boundaries) in the tracking issue.

## Container images (Trivy)

- Release and `container-scan` workflows run Trivy with `exit-code: 1` for HIGH/CRITICAL.
- Triage failures by updating base images or dependencies; do not lower severity without owner approval.

## Local baseline

- Run `dotnet build` and `.\scripts\test-local.ps1` before declaring a security-related change complete.
- Performance smoke: `.\scripts\run-performance.ps1` requires a **running** app at `TARGET_URL` (see script header).
