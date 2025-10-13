# Security Scan Configuration Guide

**Task**: SEC-010 - Configure Security Scans
**Sprint**: Sprint 7
**Date**: 2025-10-13
**Status**: COMPLETED

---

## Table of Contents

1. [Overview](#overview)
2. [Security Scan Jobs](#security-scan-jobs)
3. [Configuration Files](#configuration-files)
4. [Scan Status Summary](#scan-status-summary)
5. [Common False Positives](#common-false-positives)
6. [Adding Suppressions](#adding-suppressions)
7. [Required Secrets](#required-secrets)
8. [Troubleshooting](#troubleshooting)
9. [Maintenance Schedule](#maintenance-schedule)

---

## Overview

The YoutubeRag project implements comprehensive security scanning as part of the CI/CD pipeline. This document outlines the configuration, status, and maintenance procedures for all security scans.

### Goals

- **Early Detection**: Identify security vulnerabilities before production deployment
- **Compliance**: Meet industry security standards and best practices
- **Visibility**: Provide actionable security insights without blocking development
- **Progressive Improvement**: Incrementally enhance security posture over time

### Security Scanning Strategy

Our security scanning strategy follows a **defense-in-depth** approach:

1. **Static Analysis**: Detect vulnerabilities in source code (SAST)
2. **Dependency Analysis**: Identify vulnerable third-party packages
3. **Secret Detection**: Prevent credential leaks
4. **Container Security**: Scan Docker images for vulnerabilities
5. **IaC Security**: Validate infrastructure configuration
6. **License Compliance**: Track software license obligations

---

## Security Scan Jobs

The security pipeline (`.github/workflows/security.yml`) includes 8 jobs:

### 1. CodeQL Analysis ✅ STABLE

**Purpose**: Static analysis for C# and JavaScript code
**Languages**: C#, JavaScript
**Status**: **STABLE** - `continue-on-error` removed
**Execution**: On push, PR, daily schedule, manual trigger

**What it does**:
- Scans source code for security vulnerabilities
- Detects common patterns: SQL injection, XSS, insecure deserialization
- Uses GitHub's security-and-quality ruleset
- Uploads results to GitHub Security tab (SARIF format)

**Why stable**:
- Properly configured with .NET 8.0 SDK
- SARIF uploads are informational only (don't fail build)
- Mature tooling with low false positive rate

**Configuration**:
- No additional configuration files needed
- Uses default GitHub CodeQL queries
- Results visible in Security > Code scanning alerts

**Expected behavior**:
- ✅ Should pass for clean code
- ⚠️ May report findings that require code review
- 📊 Results uploaded to GitHub Security dashboard

---

### 2. Dependency Vulnerability Scanning ⚠️ INFORMATIONAL

**Purpose**: Identify vulnerable NuGet packages
**Tools**: NuGet vulnerability check, OWASP Dependency-Check, Snyk
**Status**: **INFORMATIONAL** - `continue-on-error: true` (Snyk requires token)
**Execution**: On push, PR, daily schedule

**What it does**:
- Scans NuGet packages for known CVEs
- Generates comprehensive vulnerability reports
- Uses National Vulnerability Database (NVD)
- Optional Snyk integration for enhanced scanning

**Components**:

#### 2.1 NuGet Vulnerability Check
- Built-in `dotnet list package --vulnerable`
- Checks direct and transitive dependencies
- Reports findings as warnings (informational only)

#### 2.2 OWASP Dependency-Check
- Industry-standard dependency scanner
- Generates HTML, JSON, and SARIF reports
- **Now configured with suppressions**: `.dependency-check-suppressions.xml`
- Uploads SARIF to GitHub Security tab

#### 2.3 Snyk Security Scan (Optional)
- Requires `SNYK_TOKEN` secret
- Enhanced vulnerability database
- Continues on error if token not configured

**Configuration Files**:
- `.dependency-check-suppressions.xml` - Suppress false positives

**Why informational**:
- Some vulnerabilities may not affect our usage
- Development dependencies are not deployed to production
- Allows time to evaluate and address findings

---

### 3. Container Security Scanning 🐳 STABLE

**Purpose**: Scan Docker images for vulnerabilities
**Tools**: Trivy, Grype, Docker Scout
**Status**: **STABLE** - Runs on push only (not PRs)
**Execution**: On push to master/develop, manual trigger

**What it does**:
- Builds Docker image from Dockerfile
- Scans for OS and application vulnerabilities
- Checks for known CVEs in base images
- Reports CRITICAL, HIGH, and MEDIUM severity issues

**Scanners**:

#### 3.1 Trivy
- Fast, comprehensive vulnerability scanner
- Scans OS packages, application dependencies
- Generates SARIF reports

#### 3.2 Grype
- Vulnerability scanner from Anchore
- Alternative scanner for cross-validation
- Severity cutoff: HIGH

#### 3.3 Docker Scout (Experimental)
- Requires Docker Hub account
- `continue-on-error: true` (optional)
- Enhanced CVE database

**Why stable**:
- Only runs on push (not every PR)
- Fail-build: false for most scanners
- Provides visibility without blocking

**Configuration**:
- No additional configuration files
- Severity thresholds set in workflow

---

### 4. Secret Scanning ✅ STABLE

**Purpose**: Prevent credential and secret leaks
**Tools**: GitLeaks, TruffleHog
**Status**: **STABLE** - `continue-on-error` removed
**Execution**: On push, PR, daily schedule

**What it does**:
- Scans entire git history for exposed secrets
- Detects API keys, passwords, tokens, certificates
- Uses pattern matching and entropy analysis
- Validates detected secrets when possible

**Scanners**:

#### 4.1 GitLeaks
- Fast secret scanner with customizable rules
- **Now configured**: `.gitleaks.toml`
- Supports allowlists and custom rules
- Detects 100+ secret types

#### 4.2 TruffleHog OSS
- Focuses on verified secrets only (`--only-verified`)
- Validates secrets against actual APIs
- Lower false positive rate

**Configuration Files**:
- `.gitleaks.toml` - Configure exclusions and custom rules

**Why stable**:
- Configured with allowlists to reduce false positives
- Excludes test files, documentation, examples
- Filters common non-secret patterns

**Common allowlisted patterns**:
- Test files (`*.Tests/`, `TestData/`)
- Documentation (`docs/*.md`, `README.md`)
- Example values (`example.com`, `your-api-key-here`)
- GitHub Actions placeholders (`${{ secrets.* }}`)

---

### 5. Infrastructure as Code (IaC) Scanning ✅ STABLE

**Purpose**: Validate Docker and Kubernetes security
**Tools**: Checkov, Terrascan
**Status**: **STABLE** - `continue-on-error` removed
**Execution**: On push, PR, daily schedule

**What it does**:
- Scans Dockerfile for security best practices
- Validates Kubernetes manifests
- Checks for misconfigurations
- Enforces security policies

**Scanners**:

#### 5.1 Checkov
- Primary IaC scanner
- Frameworks: Dockerfile, Kubernetes, Helm
- Generates SARIF reports
- **Configured with skip checks**: `CKV_DOCKER_2`, `CKV_DOCKER_3`

#### 5.2 Terrascan
- Secondary IaC scanner
- Policy-based validation
- `continue-on-error: true` (supplementary)

**Why stable**:
- Skip checks configured for non-critical rules
- SARIF uploads are informational
- Catches common Dockerfile misconfigurations

**Common checks**:
- ✅ Run as non-root user
- ✅ Use specific image tags (not `latest`)
- ✅ Health checks defined
- ✅ No hardcoded secrets
- ✅ Minimal base images

---

### 6. SAST (Static Application Security Testing) ⚠️ EXPERIMENTAL

**Purpose**: Deep security analysis of application code
**Tools**: Security Code Scan, Semgrep
**Status**: **EXPERIMENTAL** - `continue-on-error: true`
**Execution**: On push, PR, daily schedule

**What it does**:
- Advanced static analysis beyond CodeQL
- Detects complex security patterns
- Checks against OWASP Top 10
- Custom C# security rules

**Scanners**:

#### 6.1 Security Code Scan
- .NET-specific security analyzer
- May fail during installation (experimental)
- Generates SARIF reports
- `continue-on-error: true` at step level

#### 6.2 Semgrep
- Multi-language SAST tool
- Rulesets:
  - `p/security-audit`
  - `p/csharp`
  - `p/owasp-top-ten`
  - `p/r2c-security-audit`
- Uploads SARIF to GitHub Security

**Why experimental**:
- Security Code Scan tooling may be unstable
- Can have high false positive rate
- Results are supplementary to CodeQL

---

### 7. License Compliance Check ✅ STABLE

**Purpose**: Track software license obligations
**Tool**: dotnet-project-licenses
**Status**: **STABLE** - `continue-on-error` removed
**Execution**: On push, PR, daily schedule

**What it does**:
- Scans all NuGet packages for licenses
- Generates comprehensive license report
- Exports license texts for review
- Identifies licensing obligations

**Why stable**:
- Informational only (doesn't fail build)
- Helps maintain license compliance
- No configuration needed

**Output**:
- License report artifact (30-day retention)
- Summary in GitHub Actions step summary
- Useful for legal compliance reviews

**Usage**:
- Review license-report.txt in artifacts
- Ensure compatibility with project license
- Address GPL/AGPL dependencies if needed

---

### 8. Security Summary Report 📊 ALWAYS RUNS

**Purpose**: Aggregate results from all security scans
**Status**: ALWAYS RUNS - Depends on all previous jobs
**Execution**: After all security jobs complete

**What it does**:
- Collects results from all 7 security jobs
- Generates summary table with pass/fail status
- Sends Slack notification on failures (if configured)
- Provides single point of security status

**Configuration**:
- Requires `SLACK_WEBHOOK` secret for notifications
- Slack notification has `continue-on-error: true`

**Summary includes**:
| Check | Status |
|-------|--------|
| CodeQL Analysis | success/failure |
| Dependency Scanning | success/failure |
| Container Security | success/failure |
| Secret Scanning | success/failure |
| IaC Scanning | success/failure |
| SAST | success/failure |
| License Check | success/failure |

---

## Configuration Files

### `.gitleaks.toml`

**Location**: Repository root
**Purpose**: Configure GitLeaks secret scanning
**Created**: SEC-010 (2025-10-13)

**Key configurations**:

```toml
# Use default GitLeaks ruleset
[extend]
useDefault = true

# Allowlist patterns
[[allowlist]]
description = "Ignore test files and fixtures"
paths = [
    '''.*\.Tests/.*''',
    '''.*TestData/.*''',
    # ... more patterns
]

[[allowlist]]
description = "Ignore common false positives"
regexes = [
    '''example\.com''',
    '''your-api-key-here''',
    # ... more patterns
]

# Custom rules for project-specific secrets
[[rules]]
id = "youtube-api-key"
regex = '''AIza[0-9A-Za-z\-_]{35}'''
```

**When to update**:
- Adding new test directories
- Defining project-specific secret patterns
- After false positive detections
- Adding new documentation paths

---

### `.dependency-check-suppressions.xml`

**Location**: Repository root
**Purpose**: Suppress OWASP Dependency-Check false positives
**Created**: Enhanced in SEC-010 (2025-10-13)

**Structure**:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<suppressions xmlns="...">

  <!-- Development dependencies (not in production) -->
  <suppress base="true">
    <notes>
      Test dependencies not deployed to production.
      Reviewed: 2025-10-13
      Expires: 2026-04-13
    </notes>
    <packageUrl regex="true">^pkg:nuget/(xunit|Moq|...)@.*$</packageUrl>
  </suppress>

  <!-- Specific CVE suppressions -->
  <suppress>
    <notes>Detailed justification...</notes>
    <packageUrl regex="true">...</packageUrl>
    <cve>CVE-XXXX-XXXXX</cve>
  </suppress>

</suppressions>
```

**Current suppressions**:
- Test frameworks (xUnit, Moq, FluentAssertions, Bogus)
- Code coverage tools (Coverlet)
- Playwright (E2E testing)
- Build analyzers (StyleCop, SonarAnalyzer, Roslynator)

**Suppression categories**:

1. **Development Dependencies** (`base="true"`)
   - Not deployed to production
   - Used only in build/test pipeline
   - Review every 6 months

2. **False Positives** (Specific CVE)
   - Incorrect CPE matching
   - Vulnerability doesn't apply to usage
   - Review quarterly

3. **Accepted Risks** (Specific CVE + justification)
   - No patch available
   - Requires security team approval
   - Review monthly until resolved

**When to update**:
- After dependency scan identifies false positives
- When adding new test dependencies
- After security team reviews accepted risks
- At scheduled review dates (see expiration)

---

## Scan Status Summary

Based on SEC-010 configuration:

| Scan | Status | Continue-on-Error | Reason |
|------|--------|-------------------|--------|
| **CodeQL Analysis** | ✅ STABLE | ❌ Removed | Properly configured, SARIF upload only |
| **Dependency Scanning** | ⚠️ INFORMATIONAL | ✅ Kept | Snyk requires SNYK_TOKEN secret |
| **Container Security** | 🐳 STABLE | Varies | Docker Scout experimental, others stable |
| **Secret Scanning** | ✅ STABLE | ❌ Removed | Configured with .gitleaks.toml |
| **IaC Scanning** | ✅ STABLE | ❌ Removed | Checkov skip rules configured |
| **SAST** | ⚠️ EXPERIMENTAL | ✅ Kept | Security Code Scan may fail install |
| **License Check** | ✅ STABLE | ❌ Removed | Informational only, won't fail |
| **Security Summary** | 📊 ALWAYS | ❌ No | Aggregates all results |

**Legend**:
- ✅ STABLE: `continue-on-error` removed, scan should pass consistently
- ⚠️ INFORMATIONAL/EXPERIMENTAL: `continue-on-error: true`, provides insights without blocking
- 🐳 STABLE: Runs only on push, not on every PR
- ❌ Removed: `continue-on-error` flag removed in SEC-010
- ✅ Kept: `continue-on-error: true` retained for good reasons

---

## Common False Positives

### Secret Scanning

**Problem**: Test files flagged as containing secrets
**Solution**: Already excluded in `.gitleaks.toml`
```toml
paths = [
    '''.*\.Tests/.*''',
    '''.*TestData/.*''',
]
```

**Problem**: Documentation examples flagged
**Solution**: Already excluded in `.gitleaks.toml`
```toml
paths = [
    '''docs/.*\.md''',
    '''.*README\.md''',
]
```

**Problem**: GitHub Actions secret placeholders flagged
**Solution**: Already excluded in `.gitleaks.toml`
```toml
regexes = [
    '''\$\{\{\s*secrets\.\w+\s*\}\}''',
]
```

---

### Dependency Scanning

**Problem**: Test dependencies flagged as vulnerable
**Solution**: Already suppressed in `.dependency-check-suppressions.xml`
```xml
<suppress base="true">
  <packageUrl regex="true">^pkg:nuget/(xunit|Moq|FluentAssertions)@.*$</packageUrl>
</suppress>
```

**Problem**: False CPE match (different product, same name)
**Solution**: Add specific CVE suppression
```xml
<suppress>
  <notes>
    False positive - CPE incorrectly matches different product.
    Our package: MyPackage v2.0
    Vulnerable product: DifferentVendor/MyPackage v1.x
    Reviewed: 2025-10-13
  </notes>
  <cpe>cpe:/a:vendor:product</cpe>
  <packageUrl regex="true">^pkg:nuget/MyPackage@2\..*$</packageUrl>
</suppress>
```

**Problem**: Vulnerability doesn't apply to our usage
**Solution**: Add CVE suppression with justification
```xml
<suppress>
  <notes>
    CVE-2023-XXXXX affects JSON deserialization in untrusted scenarios.
    We only deserialize trusted, internal configuration files.
    Attack vector does not apply to our usage.
    Reviewed by: Security Team
    Date: 2025-10-13
    Expires: 2026-01-13
  </notes>
  <packageUrl regex="true">^pkg:nuget/PackageName@.*$</packageUrl>
  <cve>CVE-2023-XXXXX</cve>
</suppress>
```

---

### IaC Scanning (Checkov)

**Problem**: Health check not defined (CKV_DOCKER_2)
**Solution**: Already skipped in workflow
```yaml
skip_check: CKV_DOCKER_2,CKV_DOCKER_3
```

**Problem**: User is root (CKV_DOCKER_8)
**Solution**: Fix in Dockerfile
```dockerfile
# Add non-root user
RUN addgroup --system --gid 1000 appuser && \
    adduser --system --uid 1000 --ingroup appuser appuser
USER appuser
```

---

### CodeQL

**Problem**: False positive on sanitized input
**Solution**: Add CodeQL suppression comment
```csharp
// lgtm[cs/sql-injection]
var query = SanitizeInput(userInput);
```

**Problem**: Query pack version mismatch
**Solution**: Update CodeQL action version
```yaml
uses: github/codeql-action/analyze@v3  # Use latest version
```

---

## Adding Suppressions

### GitLeaks Suppressions

**Step 1**: Identify false positive in scan results

**Step 2**: Determine appropriate suppression type

**Step 3**: Add to `.gitleaks.toml`

**Example - Suppress by path**:
```toml
[[allowlist]]
description = "Ignore new test directory"
paths = [
    '''tests/new-feature/.*''',
]
```

**Example - Suppress by pattern**:
```toml
[[allowlist]]
description = "Ignore placeholder values"
regexes = [
    '''PLACEHOLDER_.*''',
]
```

**Example - Add custom secret detection**:
```toml
[[rules]]
id = "custom-api-key"
description = "Custom API Key Pattern"
regex = '''CUSTOM_[A-Z0-9]{32}'''
tags = ["key", "custom"]
```

**Step 4**: Test locally if possible
```bash
# Install GitLeaks
docker run -v $(pwd):/path zricethezav/gitleaks:latest detect --source="/path" -v

# Or with gitleaks binary
gitleaks detect --source . --config .gitleaks.toml
```

**Step 5**: Commit and validate in CI

---

### Dependency-Check Suppressions

**Step 1**: Review OWASP Dependency-Check report
- Download `owasp-dependency-check-report` artifact
- Open `dependency-check-report.html`
- Identify false positive CVEs

**Step 2**: Gather information
- Package name and version
- CVE ID
- Why it's a false positive or accepted risk
- Expiration date (for temporary suppressions)

**Step 3**: Add suppression to `.dependency-check-suppressions.xml`

**Example - Suppress test dependency**:
```xml
<suppress base="true">
  <notes>
    NewTestLibrary is a test-only dependency.
    Not deployed to production.
    Reviewed: 2025-10-13
    Expires: 2026-04-13
  </notes>
  <packageUrl regex="true">^pkg:nuget/NewTestLibrary@.*$</packageUrl>
</suppress>
```

**Example - Suppress specific CVE**:
```xml
<suppress>
  <notes>
    CVE-2024-XXXXX affects System.Text.Json when parsing untrusted JSON
    with specific configuration. We only parse trusted internal data.
    Mitigation: Input validation ensures only trusted sources.
    Reviewed by: Security Team
    Date: 2025-10-13
    Expires: 2026-01-13
  </notes>
  <packageUrl regex="true">^pkg:nuget/System\.Text\.Json@8\.0\..*$</packageUrl>
  <cve>CVE-2024-XXXXX</cve>
</suppress>
```

**Example - Suppress by file path** (less common for NuGet):
```xml
<suppress>
  <notes>
    Legacy library in isolated module, not used in production paths.
    Scheduled for replacement in Sprint 10.
    Reviewed: 2025-10-13
    Expires: 2025-12-31
  </notes>
  <filePath regex="true">.*LegacyModule.*\.dll</filePath>
</suppress>
```

**Step 4**: Validate suppression format
```bash
# Ensure XML is well-formed
xmllint --noout .dependency-check-suppressions.xml
```

**Step 5**: Review quarterly
- Check expiration dates in suppressions
- Remove suppressions for updated/removed packages
- Validate accepted risks are still necessary

---

### Checkov Skip Rules

**Step 1**: Review Checkov SARIF report in GitHub Security tab

**Step 2**: Determine if check should be skipped globally or fixed

**Step 3**: Add to workflow's `skip_check` parameter

**Example**:
```yaml
- name: Run Checkov
  uses: bridgecrewio/checkov-action@master
  with:
    skip_check: CKV_DOCKER_2,CKV_DOCKER_3,CKV_DOCKER_7
```

**Common skips**:
- `CKV_DOCKER_2`: HEALTHCHECK instruction missing
- `CKV_DOCKER_3`: User is root (if multi-stage build requires it)
- `CKV_DOCKER_7`: Using latest tag (if version is dynamic)

**Better approach**: Fix in Dockerfile when possible
```dockerfile
# Instead of skipping CKV_DOCKER_2, add health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
```

---

## Required Secrets

Configure these secrets in GitHub repository settings (`Settings > Secrets and variables > Actions`):

### Optional but Recommended

| Secret | Purpose | Required By | Impact if Missing |
|--------|---------|-------------|-------------------|
| `SNYK_TOKEN` | Enhanced dependency scanning | Snyk scan in dependency-scanning job | Snyk scan skipped (continues on error) |
| `NVD_API_KEY` | OWASP Dependency-Check NVD API | OWASP Dependency-Check | Scan works but may be rate-limited |
| `SLACK_WEBHOOK` | Security failure notifications | Security summary job | No notifications sent (continues on error) |

### How to Configure

**SNYK_TOKEN**:
1. Sign up at https://snyk.io
2. Go to Account Settings > API Token
3. Copy token
4. Add to GitHub Secrets as `SNYK_TOKEN`

**NVD_API_KEY**:
1. Request API key from https://nvd.nist.gov/developers/request-an-api-key
2. Receive key via email
3. Add to GitHub Secrets as `NVD_API_KEY`

**SLACK_WEBHOOK**:
1. Create Slack app: https://api.slack.com/apps
2. Enable Incoming Webhooks
3. Create webhook for desired channel
4. Add webhook URL to GitHub Secrets as `SLACK_WEBHOOK`

**Note**: All scans work without these secrets, but with reduced functionality or rate limiting.

---

## Troubleshooting

### CodeQL Analysis Failing

**Symptom**: CodeQL analysis fails with build errors
**Likely cause**: .NET SDK version mismatch

**Solution**:
```yaml
# Verify .NET version in workflow
env:
  DOTNET_VERSION: '8.0.x'  # Should match project target

# Ensure setup-dotnet step runs before CodeQL build
- name: Setup .NET
  if: matrix.language == 'csharp'
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: ${{ env.DOTNET_VERSION }}
```

**Symptom**: JavaScript CodeQL scan times out
**Likely cause**: Large JavaScript bundle or node_modules

**Solution**:
```yaml
# Add timeout to prevent hanging
- name: Perform CodeQL Analysis
  timeout-minutes: 30
  uses: github/codeql-action/analyze@v3
```

---

### GitLeaks Reporting False Positives

**Symptom**: GitLeaks flags non-secrets in test files

**Solution**: Add path exclusion to `.gitleaks.toml`
```toml
[[allowlist]]
description = "Ignore specific test directory"
paths = [
    '''path/to/test/directory/.*''',
]
```

**Symptom**: GitLeaks flags placeholder values in config examples

**Solution**: Add regex exclusion
```toml
[[allowlist]]
description = "Ignore placeholder patterns"
regexes = [
    '''REPLACE_WITH_ACTUAL_VALUE''',
    '''your-.*-here''',
]
```

---

### OWASP Dependency-Check Taking Too Long

**Symptom**: Dependency-Check job times out

**Solution**: Add caching for NVD database
```yaml
- name: Cache NVD Database
  uses: actions/cache@v3
  with:
    path: ~/.gradle/dependency-check-data
    key: ${{ runner.os }}-nvd-${{ hashFiles('**/packages.lock.json') }}
    restore-keys: |
      ${{ runner.os }}-nvd-
```

**Symptom**: Rate limited by NVD

**Solution**: Configure `NVD_API_KEY` secret (see Required Secrets)

---

### Container Scans Failing on Base Image Vulnerabilities

**Symptom**: Trivy/Grype report HIGH/CRITICAL vulnerabilities in base image

**Solution 1**: Update base image version
```dockerfile
# Update from older version
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

# Use specific digest for reproducibility
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine@sha256:abc123...
```

**Solution 2**: Switch to minimal base image
```dockerfile
# Use distroless or minimal variants
FROM gcr.io/distroless/dotnet:8
```

**Solution 3**: Accept and document risk (if no fix available)
- Document in security review
- Monitor for updates
- Consider compensating controls

---

### Semgrep Reporting Too Many Findings

**Symptom**: Semgrep generates excessive false positives

**Solution**: Create `.semgrepignore` file
```
# .semgrepignore
*.Tests/
tests/
TestData/
*.g.cs  # Generated files
Migrations/
```

**Solution**: Reduce ruleset scope
```yaml
# Use only critical security rules
config: >-
  p/security-audit
  p/csharp
```

---

### License Check Fails to Install Tool

**Symptom**: `dotnet-project-licenses` tool installation fails

**Solution**: Specify version explicitly
```yaml
- name: Install License Tool
  run: dotnet tool install --global dotnet-project-licenses --version 2.4.0
```

---

## Maintenance Schedule

### Daily (Automated via Cron)

✅ **Security scans run automatically**
- Scheduled: 2 AM UTC daily
- Detects newly disclosed vulnerabilities
- No manual intervention required

---

### Weekly

📅 **Review security scan results**
- Check GitHub Security tab for new findings
- Triage CodeQL alerts
- Review dependency vulnerabilities

**Action items**:
- Assign critical/high findings to developers
- Update dependencies with security patches
- Document false positives for suppression

---

### Monthly

📅 **Accepted risk review**
- Review all suppressed vulnerabilities
- Check if patches are now available
- Update risk assessments

**Process**:
1. List all suppressions in `.dependency-check-suppressions.xml`
2. Check for package updates that fix suppressed CVEs
3. Update or remove suppressions as needed
4. Document review in security meeting notes

---

### Quarterly (Every 3 Months)

📅 **Comprehensive configuration review**
- Review `.gitleaks.toml` for outdated exclusions
- Audit `.dependency-check-suppressions.xml` suppressions
- Check expiration dates on temporary suppressions
- Update security scan tool versions

**Checklist**:
- [ ] Review GitLeaks allowlist paths
- [ ] Validate all suppression expiration dates
- [ ] Check for new GitLeaks/Checkov rules to adopt
- [ ] Update security scan action versions
- [ ] Review GitHub Security tab for trends
- [ ] Update this documentation

**Next review dates**:
- 2026-01-13 (Q1 2026)
- 2026-04-13 (Q2 2026)
- 2026-07-13 (Q3 2026)
- 2026-10-13 (Q4 2026)

---

### Annually

📅 **Security posture assessment**
- Comprehensive review of all security controls
- Benchmark against industry standards
- Update security scanning strategy
- Security team audit

**Scope**:
- Evaluate effectiveness of security scans
- Compare with OWASP, NIST, CIS benchmarks
- Assess whether new scan types are needed
- Review and update security policies

**Next annual review**: 2026-10-13

---

## Best Practices

### 1. Don't Suppress Real Vulnerabilities

❌ **Bad**: Suppressing because it's easier than fixing
```xml
<suppress base="true">
  <notes>Too many findings, suppressing entire package</notes>
  <packageUrl regex="true">^pkg:nuget/VulnerablePackage@.*$</packageUrl>
</suppress>
```

✅ **Good**: Suppressing with proper justification and mitigation
```xml
<suppress>
  <notes>
    CVE-2024-XXXXX affects feature we don't use.
    Attack requires X condition which is not present in our usage.
    Mitigation: Input validation layer prevents attack vector.
    Monitoring: WAF rules detect exploitation attempts.
    Reviewed by: Security Team
    Date: 2025-10-13
    Expires: 2026-01-13
  </notes>
  <packageUrl regex="true">^pkg:nuget/VulnerablePackage@2\.1\..*$</packageUrl>
  <cve>CVE-2024-XXXXX</cve>
</suppress>
```

---

### 2. Use Expiration Dates

✅ **Good**: Time-bound suppressions force regular review
```xml
<suppress>
  <notes>
    ...
    Reviewed: 2025-10-13
    Expires: 2026-04-13  <!-- Review in 6 months -->
  </notes>
  ...
</suppress>
```

---

### 3. Keep Suppressions Specific

❌ **Bad**: Overly broad suppression
```xml
<suppress base="true">
  <filePath regex="true">.*</filePath>  <!-- Suppresses EVERYTHING -->
</suppress>
```

✅ **Good**: Specific suppression
```xml
<suppress base="true">
  <packageUrl regex="true">^pkg:nuget/SpecificTestPackage@1\.2\..*$</packageUrl>
</suppress>
```

---

### 4. Document Thoroughly

✅ **Good**: Comprehensive documentation
```xml
<suppress>
  <notes>
    CVE-2024-XXXXX - SQL Injection in ORM

    Affected package: MicroORM v1.2.3
    Vulnerability: SQL injection via dynamic query builder

    Why not applicable:
    - We only use parameterized queries
    - Dynamic query builder is disabled in configuration
    - Input validation prevents SQL metacharacters

    Mitigation:
    - WAF blocks SQL injection patterns
    - Database user has minimal privileges
    - Monitoring alerts on suspicious queries

    Upgrade path:
    - v1.3.0 fixes issue but has breaking changes
    - Scheduled for Sprint 10 (migration effort: 3 days)

    Reviewed by: Security Team (Alice, Bob)
    Date: 2025-10-13
    Expires: 2025-12-31 (Sprint 10 completion)
  </notes>
  <packageUrl regex="true">^pkg:nuget/MicroORM@1\.2\.3$</packageUrl>
  <cve>CVE-2024-XXXXX</cve>
</suppress>
```

---

### 5. Keep Configuration Files Updated

✅ **Process**:
1. Run security scans
2. Review new findings
3. Update suppressions as needed
4. Commit configuration changes with descriptive messages
5. Document in PR why suppression was added

**Example commit message**:
```
SEC-010: Suppress CVE-2024-XXXXX in Moq test dependency

Moq v4.18.4 has CVE-2024-XXXXX which affects mock object creation
in untrusted scenarios. Our usage is test-only and doesn't expose
this attack surface. Test dependencies are not deployed to production.

Added suppression to .dependency-check-suppressions.xml with
expiration date of 2026-04-13 for quarterly review.

Refs: #14
```

---

## Migration Notes (SEC-010)

### What Changed in SEC-010

**Files Created**:
- ✅ `.gitleaks.toml` - GitLeaks configuration with allowlists
- ✅ `.github/docs/SEC-010-SECURITY-SCAN-CONFIGURATION.md` - This document

**Files Modified**:
- ✅ `.dependency-check-suppressions.xml` - Enhanced with better documentation
- ✅ `.github/workflows/security.yml` - Selectively removed `continue-on-error`

**Workflow Changes**:

| Job | Before SEC-010 | After SEC-010 | Reason |
|-----|----------------|---------------|--------|
| CodeQL Analysis | `continue-on-error: true` | ❌ Removed | Stable with .NET 8.0 |
| Dependency Scanning | `continue-on-error: true` | ✅ Kept | Snyk requires token |
| Secret Scanning | `continue-on-error: true` | ❌ Removed | Configured with .gitleaks.toml |
| IaC Scanning | `continue-on-error: true` | ❌ Removed | Checkov skip rules work |
| SAST | `continue-on-error: true` | ✅ Kept | Experimental tooling |
| License Check | `continue-on-error: true` | ❌ Removed | Informational only |

**Configuration Additions**:
- OWASP Dependency-Check now uses `--suppression .dependency-check-suppressions.xml`

**Expected Impact**:
- ✅ Fewer false positives in secret scanning
- ✅ Cleaner dependency scan results
- ✅ More stable security pipeline
- ⚠️ Some scans may fail if new vulnerabilities are introduced
  - This is EXPECTED behavior - scans should catch real issues
  - Review findings and suppress only if false positives

---

## References

### Official Documentation

- [GitHub CodeQL Documentation](https://codeql.github.com/docs/)
- [GitLeaks Documentation](https://github.com/gitleaks/gitleaks)
- [OWASP Dependency-Check](https://jeremylong.github.io/DependencyCheck/)
- [Snyk Documentation](https://docs.snyk.io/)
- [Trivy Documentation](https://aquasecurity.github.io/trivy/)
- [Checkov Documentation](https://www.checkov.io/documentation.html)
- [Semgrep Documentation](https://semgrep.dev/docs/)

### Project Documentation

- `CI_CD_ISSUES_ANALYSIS.md` - CI/CD issues analysis
- `docs/TEST-027-CI-CD-SETUP.md` - Sprint 6 CI/CD setup
- `.github/workflows/security.yml` - Security workflow configuration
- `.github/workflows/ci.yml` - Main CI workflow

### Security Standards

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [NIST NVD](https://nvd.nist.gov/)

---

## Changelog

### 2025-10-13 - SEC-010 Initial Configuration

**Created**:
- Comprehensive security scan documentation
- GitLeaks configuration with sensible defaults
- Enhanced dependency-check suppressions

**Modified**:
- Security workflow optimized for stability
- Removed `continue-on-error` from 4 scans
- Added suppression file reference to OWASP scan

**Status**:
- ✅ 4 scans now stable (CodeQL, Secret, IaC, License)
- ⚠️ 2 scans informational (Dependency, SAST)
- 🐳 1 scan runs on push only (Container)
- 📊 1 summary job always runs

**Next Steps**:
- Configure optional secrets (SNYK_TOKEN, NVD_API_KEY, SLACK_WEBHOOK)
- Monitor scan results in first few PRs
- Adjust suppressions as needed based on findings
- Schedule quarterly review (2026-01-13)

---

**Document version**: 1.0
**Last updated**: 2025-10-13
**Maintained by**: DevOps Team
**Next review**: 2026-01-13
