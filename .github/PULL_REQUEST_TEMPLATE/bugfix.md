## 🩸 Bug Fix — The PR Inquisition

### Summary

<!-- What bug does this fix? Provide a clear description of the defect and root cause. -->

Fixes # (issue)

### 🔍 Root Cause Analysis

<!-- What was the root cause? Where in the layer stack did the breach occur? -->

- **Layer affected**: <!-- Presentation / Application / Domain / Infrastructure -->
- **Root cause**: <!-- e.g., "Missing ownership check allowed IDOR access to another player's character" -->

### ⚖️ Traceability Report

- **UI / Presentation Layer**: <!-- How the bug manifested visually -->
- **Domain Logic / Application Layer**: <!-- Where the logic failed -->
- **Data Persistence (The Blood)**: <!-- Any data corruption or incorrect persistence -->

### 🧪 Validation & Testing

<!-- How did you verify the fix? Include regression tests. -->

- [ ] Regression test added to prevent recurrence
- [ ] Existing tests still pass
- [ ] Manual reproduction confirmed the fix

### 📜 The Antigravity Pacts

- [ ] My code follows the "Explicit over implicit" rule — no hidden magic.
- [ ] I have provided the Traceability Report above.
- [ ] My changes pass the local Inquisition (`scripts/test-local.ps1`).
- [ ] I have run `dotnet format` and verified no style violations.
