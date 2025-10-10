# CI/CD Pipeline Fixes - Step-by-Step Guide

**Project:** YoutubeRag
**Date:** 2025-10-10
**Branch:** YRUS-0201_gestionar_modelos_whisper

---

## Table of Contents

1. [P0-1: Fix .NET Version Mismatch](#p0-1-fix-net-version-mismatch)
2. [P0-2: Add Database Migrations to CI](#p0-2-add-database-migrations-to-ci)
3. [P1-1: Add Sprint 2 Environment Variables](#p1-1-add-sprint-2-environment-variables)
4. [P1-2: Improve Code Coverage Configuration](#p1-2-improve-code-coverage-configuration)
5. [P1-4: Create Dependency Check Suppression File](#p1-4-create-dependency-check-suppression-file)
6. [P2-1: Enforce Build Warnings as Errors](#p2-1-enforce-build-warnings-as-errors)
7. [P2-2: Enforce Code Formatting](#p2-2-enforce-code-formatting)
8. [Verification Steps](#verification-steps)

---

## P0-1: Fix .NET Version Mismatch

### Problem
Test project targets .NET 9.0, but CI and all other projects use .NET 8.0 LTS.

### Solution
Change test project to target .NET 8.0.

### Steps

1. **Open the test project file:**
   ```bash
   # Path: YoutubeRag.Tests.Integration/YoutubeRag.Tests.Integration.csproj
   ```

2. **Change the TargetFramework:**

   **BEFORE:**
   ```xml
   <PropertyGroup>
     <TargetFramework>net9.0</TargetFramework>
   </PropertyGroup>
   ```

   **AFTER:**
   ```xml
   <PropertyGroup>
     <TargetFramework>net8.0</TargetFramework>
   </PropertyGroup>
   ```

3. **Downgrade incompatible packages:**

   **BEFORE:**
   ```xml
   <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.9" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.9" />
   ```

   **AFTER:**
   ```xml
   <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.11" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.11" />
   ```

4. **Clean and rebuild:**
   ```bash
   dotnet clean YoutubeRag.sln
   dotnet restore YoutubeRag.sln
   dotnet build YoutubeRag.sln --configuration Release
   ```

5. **Verify tests still pass locally:**
   ```bash
   dotnet test YoutubeRag.Tests.Integration --configuration Release
   ```

### Expected Result
- Solution builds without errors
- All 74 integration tests pass
- No .NET SDK version conflicts

### Files Modified
- `YoutubeRag.Tests.Integration/YoutubeRag.Tests.Integration.csproj`

---

## P0-2: Add Database Migrations to CI

### Problem
Integration tests fail because database schema doesn't exist in CI environment.

### Solution
Add a step to apply EF Core migrations before running tests.

### Steps

1. **Open CI workflow file:**
   ```
   .github/workflows/ci.yml
   ```

2. **Add migration step after "Build Solution" and before "Run Tests with Coverage":**

   **Insert this new step:**
   ```yaml
   # Step 5.5: Apply Database Migrations (NEW)
   - name: Apply Database Migrations
     env:
       ConnectionStrings__DefaultConnection: "Server=localhost;Port=3306;Database=test_db;User=root;Password=test_password;AllowPublicKeyRetrieval=True;"
     run: |
       # Install EF Core tools
       dotnet tool install --global dotnet-ef --version 8.0.0

       # Add tools to PATH
       export PATH="$PATH:$HOME/.dotnet/tools"

       # List available migrations (for debugging)
       echo "Available migrations:"
       dotnet ef migrations list \
         --project YoutubeRag.Infrastructure \
         --startup-project YoutubeRag.Api \
         --no-build || true

       # Apply all pending migrations
       echo "Applying database migrations..."
       dotnet ef database update \
         --project YoutubeRag.Infrastructure \
         --startup-project YoutubeRag.Api \
         --no-build \
         --verbose

       echo "✅ Database migrations applied successfully"
   ```

3. **Update step numbering:**
   - Old "Step 6" becomes "Step 7"
   - Old "Step 7" becomes "Step 8"
   - etc.

### Alternative: Use Migration Docker Stage

If you prefer using the migration stage from the Dockerfile:

```yaml
- name: Apply Database Migrations (Docker)
  env:
    ConnectionStrings__DefaultConnection: "Server=localhost;Port=3306;Database=test_db;User=root;Password=test_password;AllowPublicKeyRetrieval=True;"
  run: |
    # Build migration image
    docker build --target migration -t youtuberag-migration .

    # Run migrations
    docker run --rm \
      --network host \
      -e ConnectionStrings__DefaultConnection="$ConnectionStrings__DefaultConnection" \
      youtuberag-migration
```

### Expected Result
- Database schema created in test_db
- All 5 migrations applied successfully
- Tests can access database tables

### Files Modified
- `.github/workflows/ci.yml`

---

## P1-1: Add Sprint 2 Environment Variables

### Problem
Tests may fail or behave incorrectly due to missing Sprint 2 configuration.

### Solution
Add all required environment variables to the CI workflow.

### Steps

1. **Open CI workflow file:**
   ```
   .github/workflows/ci.yml
   ```

2. **Find the "Run Tests with Coverage" step (currently step 6):**

3. **Replace the env section with this comprehensive version:**

   **BEFORE:**
   ```yaml
   env:
     # Test environment variables
     ConnectionStrings__DefaultConnection: "Server=localhost;Port=3306;Database=test_db;User=root;Password=test_password;AllowPublicKeyRetrieval=True;"
     Redis__ConnectionString: "localhost:6379"
     JWT__Secret: "TestSecretKeyForJWTTokenGenerationMinimum256Bits!"
     JWT__Issuer: "TestIssuer"
     JWT__Audience: "TestAudience"
     ASPNETCORE_ENVIRONMENT: Testing
   ```

   **AFTER:**
   ```yaml
   env:
     # Database and Cache
     ConnectionStrings__DefaultConnection: "Server=localhost;Port=3306;Database=test_db;User=root;Password=test_password;AllowPublicKeyRetrieval=True;"
     ConnectionStrings__Redis: "localhost:6379"

     # JWT Authentication
     JwtSettings__SecretKey: "TestSecretKeyForJWTTokenGenerationMinimum256Bits!"
     JwtSettings__ExpirationInMinutes: "60"
     JwtSettings__RefreshTokenExpirationInDays: "7"

     # Application Settings
     ASPNETCORE_ENVIRONMENT: "Testing"
     AppSettings__Environment: "Testing"
     AppSettings__ProcessingMode: "Mock"
     AppSettings__StorageMode: "Database"
     AppSettings__EnableAuth: "true"
     AppSettings__EnableWebSockets: "true"
     AppSettings__EnableMetrics: "false"
     AppSettings__EnableRealProcessing: "false"
     AppSettings__EnableDocs: "true"
     AppSettings__EnableCors: "true"

     # YouTube Configuration
     YouTube__TimeoutSeconds: "30"
     YouTube__MaxRetries: "3"
     YouTube__MaxVideoDurationSeconds: "14400"

     # Processing Configuration (Sprint 2)
     Processing__TempFilePath: "/tmp/youtuberag"
     Processing__MaxVideoSizeMB: "2048"
     Processing__FFmpegPath: "ffmpeg"
     Processing__CleanupAfterHours: "1"
     Processing__MinDiskSpaceGB: "5"
     Processing__ProgressReportIntervalSeconds: "10"

     # Whisper Configuration (Sprint 2)
     Whisper__ModelsPath: "/tmp/whisper-models"
     Whisper__DefaultModel: "auto"
     Whisper__ForceModel: ""
     Whisper__ModelDownloadUrl: "https://openaipublic.azureedge.net/main/whisper/models/"
     Whisper__CleanupUnusedModelsDays: "30"
     Whisper__MinDiskSpaceGB: "10"
     Whisper__DownloadRetryAttempts: "3"
     Whisper__DownloadRetryDelaySeconds: "5"
     Whisper__ModelCacheDurationMinutes: "60"
     Whisper__TinyModelThresholdSeconds: "600"
     Whisper__BaseModelThresholdSeconds: "1800"

     # Cleanup Configuration (Sprint 2)
     Cleanup__JobRetentionDays: "1"
     Cleanup__NotificationRetentionDays: "1"
     Cleanup__NotificationDeduplicationMinutes: "5"

     # Rate Limiting
     RateLimiting__PermitLimit: "200"
     RateLimiting__WindowMinutes: "1"

     # Logging
     Logging__LogLevel__Default: "Information"
     Logging__LogLevel__Microsoft.AspNetCore: "Warning"
     Logging__LogLevel__Microsoft.EntityFrameworkCore: "Warning"
   ```

4. **Optional: Create temp directories before tests:**

   Add before "Run Tests with Coverage":
   ```yaml
   - name: Create Test Directories
     run: |
       mkdir -p /tmp/youtuberag
       mkdir -p /tmp/whisper-models
       chmod 777 /tmp/youtuberag
       chmod 777 /tmp/whisper-models
   ```

### Expected Result
- Tests have access to all required configuration
- No configuration-related test failures
- Tests can create temp files in /tmp/youtuberag

### Files Modified
- `.github/workflows/ci.yml`

---

## P1-2: Improve Code Coverage Configuration

### Problem
Coverage calculation doesn't exclude generated code and may report inaccurate results.

### Solution
Add coverlet configuration file and improve threshold enforcement.

### Steps

#### Step 1: Create coverlet.runsettings

1. **Create file at repository root:**
   ```
   coverlet.runsettings
   ```

2. **Add this content:**
   ```xml
   <?xml version="1.0" encoding="utf-8" ?>
   <RunSettings>
     <DataCollectionRunSettings>
       <DataCollectors>
         <DataCollector friendlyName="XPlat Code Coverage">
           <Configuration>
             <Format>cobertura</Format>

             <!-- Exclude generated code and infrastructure -->
             <Exclude>
               [*.Tests*]*,
               [*]*.Migrations.*,
               [*]*.Designer,
               [*]*Migration,
               [YoutubeRag.Infrastructure]YoutubeRag.Infrastructure.Migrations.*,
               [YoutubeRag.Infrastructure]YoutubeRag.Infrastructure.Data.Migrations.*,
               [YoutubeRag.Api]Program,
               [*]*Program*,
               [*]*Startup*
             </Exclude>

             <!-- Exclude specific files -->
             <ExcludeByFile>
               **/*Migration*.cs,
               **/*Designer.cs,
               **/Migrations/*.cs,
               **/Program.cs,
               **/Startup.cs
             </ExcludeByFile>

             <!-- Exclude attributes -->
             <ExcludeByAttribute>
               Obsolete,
               GeneratedCodeAttribute,
               CompilerGeneratedAttribute,
               ExcludeFromCodeCoverageAttribute
             </ExcludeByAttribute>

             <!-- Include all assemblies except test projects -->
             <Include>
               [YoutubeRag.Domain]*,
               [YoutubeRag.Application]*,
               [YoutubeRag.Infrastructure]*,
               [YoutubeRag.Api]*
             </Include>
           </Configuration>
         </DataCollector>
       </DataCollectors>
     </DataCollectionRunSettings>
   </RunSettings>
   ```

#### Step 2: Update CI to use runsettings

1. **Open `.github/workflows/ci.yml`**

2. **Find "Run Tests with Coverage" step**

3. **Update to use runsettings:**

   **BEFORE:**
   ```yaml
   run: |
     dotnet test YoutubeRag.sln \
       --configuration Release \
       --no-build \
       --collect:"XPlat Code Coverage" \
       -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
   ```

   **AFTER:**
   ```yaml
   run: |
     dotnet test YoutubeRag.sln \
       --configuration Release \
       --no-build \
       --verbosity normal \
       --logger "trx;LogFileName=test-results.trx" \
       --logger "console;verbosity=detailed" \
       --collect:"XPlat Code Coverage" \
       --results-directory ./TestResults \
       --settings coverlet.runsettings
   ```

#### Step 3: Make coverage threshold fail the build

1. **Find "Check Coverage Threshold" step**

2. **Change warning to error:**

   **BEFORE:**
   ```yaml
   if [ "$COVERAGE" -lt "$THRESHOLD" ]; then
     echo "::warning::Code coverage ${COVERAGE}% is below threshold"
   fi
   ```

   **AFTER:**
   ```yaml
   if [ "$COVERAGE" -lt "$THRESHOLD" ]; then
     echo "::error::Code coverage ${COVERAGE}% is below the required threshold of ${THRESHOLD}%"
     exit 1
   else
     echo "::notice::Code coverage ${COVERAGE}% meets the required threshold of ${THRESHOLD}%"
   fi
   ```

### Expected Result
- Accurate coverage calculation excluding migrations and generated code
- Build fails if coverage drops below 80%
- Coverage reports properly reflect testable code

### Files Created/Modified
- `coverlet.runsettings` (NEW)
- `.github/workflows/ci.yml` (MODIFIED)

---

## P1-4: Create Dependency Check Suppression File

### Problem
Security workflow references non-existent suppression file.

### Solution
Create basic suppression file for known false positives.

### Steps

1. **Create file at repository root:**
   ```
   .dependency-check-suppressions.xml
   ```

2. **Add this content:**
   ```xml
   <?xml version="1.0" encoding="UTF-8"?>
   <suppressions xmlns="https://jeremylong.github.io/DependencyCheck/dependency-suppression.1.3.xsd">

     <!--
       Suppress false positives and known issues
       Format:
       - filePath: Pattern to match dependency file
       - cve: Specific CVE to suppress
       - Optionally add notes explaining why suppressed
     -->

     <!-- Example: Suppress specific CVE for a package -->
     <!--
     <suppress>
       <notes>
         This vulnerability does not affect our usage of the package.
         We only use feature X, which is not vulnerable.
       </notes>
       <packageUrl regex="true">^pkg:nuget/PackageName@.*$</packageUrl>
       <cve>CVE-2023-12345</cve>
     </suppress>
     -->

     <!-- Suppress development-only dependencies -->
     <suppress base="true">
       <notes>
         Development and test dependencies are not deployed to production.
       </notes>
       <packageUrl regex="true">^pkg:nuget/(xunit|Moq|FluentAssertions|Bogus|Microsoft\.NET\.Test\.Sdk)@.*$</packageUrl>
     </suppress>

     <!-- Suppress coverlet (code coverage tool) -->
     <suppress base="true">
       <notes>
         Code coverage tool used only in development/CI, not in production.
       </notes>
       <packageUrl regex="true">^pkg:nuget/coverlet\..*@.*$</packageUrl>
     </suppress>

     <!-- Add project-specific suppressions below as needed -->

   </suppressions>
   ```

3. **Add to .gitignore to track future suppressions:**
   ```
   # Keep suppression file tracked
   !.dependency-check-suppressions.xml
   ```

### Expected Result
- Security workflow runs without file not found errors
- Known false positives are suppressed
- Real vulnerabilities still reported

### Files Created
- `.dependency-check-suppressions.xml` (NEW)

---

## P2-1: Enforce Build Warnings as Errors

### Problem
Build warnings are ignored, leading to code quality degradation.

### Solution
Treat warnings as errors in CI (but not locally for development).

### Steps

1. **Open CI workflow file:**
   ```
   .github/workflows/ci.yml
   ```

2. **Find "Build Solution" step**

3. **Add warning enforcement:**

   **BEFORE:**
   ```yaml
   - name: Build Solution
     run: dotnet build YoutubeRag.sln --configuration Release --no-restore
   ```

   **AFTER:**
   ```yaml
   - name: Build Solution
     run: |
       dotnet build YoutubeRag.sln \
         --configuration Release \
         --no-restore \
         /p:TreatWarningsAsErrors=true \
         /p:WarningLevel=4
   ```

4. **Optional: Selectively enable for specific warnings:**

   If there are too many existing warnings, enable incrementally:
   ```yaml
   run: |
     dotnet build YoutubeRag.sln \
       --configuration Release \
       --no-restore \
       /p:WarningsAsErrors="CS8600;CS8602;CS8603;CS8604;CS8625"
   ```

   Common warnings to enforce:
   - CS8600: Converting null literal or possible null value to non-nullable type
   - CS8602: Dereference of a possibly null reference
   - CS8603: Possible null reference return
   - CS8604: Possible null reference argument
   - CS8625: Cannot convert null literal to non-nullable reference type

### Expected Result
- Build fails on any warnings
- Forces developers to fix warnings before merge
- Maintains code quality standards

### Files Modified
- `.github/workflows/ci.yml`

### Rollback Plan
If this breaks the build due to existing warnings:
1. Fix warnings first in separate PR
2. Then enable warning enforcement
3. OR use selective enforcement (WarningsAsErrors)

---

## P2-2: Enforce Code Formatting

### Problem
Code formatting check is warning-only, allowing style inconsistencies.

### Solution
Make formatting check fail the build.

### Steps

1. **Open CI workflow file:**
   ```
   .github/workflows/ci.yml
   ```

2. **Find "Check Code Format" step in code-quality job**

3. **Remove the warning bypass:**

   **BEFORE:**
   ```yaml
   - name: Check Code Format
     run: |
       dotnet format YoutubeRag.sln --verify-no-changes --verbosity diagnostic || echo "::warning::Code formatting issues found"
   ```

   **AFTER:**
   ```yaml
   - name: Check Code Format
     run: |
       echo "Checking code formatting..."
       dotnet format YoutubeRag.sln --verify-no-changes --verbosity diagnostic

       if [ $? -ne 0 ]; then
         echo "::error::Code formatting issues detected. Run 'dotnet format' locally to fix."
         exit 1
       fi

       echo "✅ Code formatting check passed"
   ```

4. **Add helpful comment in PR template:**

   Create `.github/pull_request_template.md` if it doesn't exist:
   ```markdown
   ## Pre-merge Checklist

   - [ ] Code builds without errors or warnings
   - [ ] All tests pass locally
   - [ ] Code formatted with `dotnet format`
   - [ ] No merge conflicts

   ## Testing

   Describe how you tested these changes.

   ## Breaking Changes

   List any breaking changes or migrations required.
   ```

### Expected Result
- PRs with formatting issues fail CI
- Consistent code style across codebase
- Developers run `dotnet format` before committing

### Files Modified
- `.github/workflows/ci.yml`
- `.github/pull_request_template.md` (OPTIONAL)

---

## Verification Steps

### Local Verification (Before Pushing)

1. **Verify .NET version fix:**
   ```bash
   dotnet build YoutubeRag.sln --configuration Release
   # Should build successfully without SDK version errors
   ```

2. **Run tests locally:**
   ```bash
   dotnet test YoutubeRag.sln --configuration Release
   # All tests should pass
   ```

3. **Check code formatting:**
   ```bash
   dotnet format YoutubeRag.sln --verify-no-changes
   # Should report no formatting issues
   ```

4. **Validate Docker build:**
   ```bash
   docker build -t youtuberag:test .
   # Should build successfully
   ```

### CI Verification (After Pushing)

1. **Create test branch:**
   ```bash
   git checkout -b ci-fixes-test
   git add .
   git commit -m "Fix CI/CD pipeline for Sprint 2"
   git push origin ci-fixes-test
   ```

2. **Monitor GitHub Actions:**
   - Go to: https://github.com/[your-repo]/actions
   - Watch CI pipeline execution
   - Verify all jobs pass:
     - ✅ build-and-test
     - ✅ code-quality
     - ✅ security-scan

3. **Check artifacts:**
   - Test results uploaded
   - Coverage reports generated
   - Published app artifact created

4. **Verify coverage threshold:**
   - Coverage should be calculated correctly
   - Threshold check should pass or fail appropriately

5. **Review logs for issues:**
   - Database migrations applied successfully
   - All 74 integration tests executed
   - No timeout errors
   - No configuration errors

### Integration Test Verification

Run specific test categories to verify Sprint 2 features:

```bash
# Database tests
dotnet test --filter "Category=Database" --configuration Release

# API tests
dotnet test --filter "Category=API" --configuration Release

# Integration tests
dotnet test --filter "Category=Integration" --configuration Release
```

### Performance Check

Monitor CI execution times:

**Before fixes:**
- Build: ~5 minutes
- Tests: ~10 minutes
- Total: ~15 minutes

**After fixes (expected):**
- Build: ~5 minutes
- Migrations: ~30 seconds
- Tests: ~10-12 minutes
- Total: ~15-17 minutes

---

## Rollback Procedures

If fixes cause unexpected issues:

### Rollback P0-1 (.NET Version)

```bash
cd YoutubeRag.Tests.Integration
# Revert to .NET 9.0
sed -i 's/net8.0/net9.0/g' YoutubeRag.Tests.Integration.csproj
dotnet restore
```

### Rollback P0-2 (Migrations)

Remove the migration step from `.github/workflows/ci.yml`:
```bash
git checkout HEAD -- .github/workflows/ci.yml
```

### Rollback All Changes

```bash
# Discard all changes
git checkout -- .

# Or reset to previous commit
git reset --hard HEAD~1
```

---

## Post-Fix Validation Checklist

- [ ] ✅ Solution builds successfully locally
- [ ] ✅ All tests pass locally
- [ ] ✅ Docker image builds successfully
- [ ] ✅ CI pipeline passes on test branch
- [ ] ✅ Database migrations applied in CI
- [ ] ✅ Code coverage calculated correctly
- [ ] ✅ No breaking changes introduced
- [ ] ✅ All 74 integration tests pass in CI
- [ ] ✅ Coverage threshold met (≥80%)
- [ ] ✅ No security vulnerabilities introduced
- [ ] ✅ Build time acceptable (<20 minutes)
- [ ] ✅ Artifacts generated correctly

---

## Troubleshooting Common Issues

### Issue: EF Core tools not found

**Error:**
```
bash: dotnet-ef: command not found
```

**Fix:**
Ensure PATH includes .dotnet/tools:
```yaml
export PATH="$PATH:$HOME/.dotnet/tools"
```

### Issue: MySQL connection refused

**Error:**
```
MySqlException: Unable to connect to any of the specified MySQL hosts
```

**Fix:**
- Verify MySQL service is healthy before running migrations
- Use `localhost` not `127.0.0.1`
- Include `AllowPublicKeyRetrieval=True` in connection string

### Issue: Coverage file not found

**Error:**
```
No coverage file found
```

**Fix:**
- Verify tests actually ran
- Check TestResults directory exists
- Ensure coverlet.runsettings is in repository root

### Issue: Too many build warnings

**Error:**
```
Build FAILED. 92 Warning(s)
```

**Fix (temporary):**
Use selective warning enforcement:
```yaml
/p:WarningsAsErrors="CS8600;CS8602"  # Only critical warnings
```

Then fix warnings in separate PR.

### Issue: Test timeout in CI

**Error:**
```
Test timed out after 10 minutes
```

**Fix:**
Add timeout configuration:
```yaml
--  RunConfiguration.TestSessionTimeout=600000
```

Or increase in runsettings:
```xml
<RunConfiguration>
  <TestSessionTimeout>600000</TestSessionTimeout>
</RunConfiguration>
```

---

## Next Steps After Fixes Applied

1. **Merge to feature branch:**
   ```bash
   git checkout YRUS-0201_gestionar_modelos_whisper
   git merge ci-fixes-test
   git push origin YRUS-0201_gestionar_modelos_whisper
   ```

2. **Monitor CI on feature branch:**
   - Verify all checks pass
   - Review any new issues

3. **Create PR to develop:**
   - Include CI fixes in PR description
   - Reference this document
   - Request DevOps review

4. **Plan Sprint 3 improvements:**
   - Install FFmpeg in CI
   - Install Python + Whisper in CI
   - Create Helm charts
   - Implement E2E tests

---

## Summary of Changes

| Fix | Files Modified | Risk Level | Reversible |
|-----|----------------|------------|------------|
| P0-1 | YoutubeRag.Tests.Integration.csproj | Low | Yes |
| P0-2 | .github/workflows/ci.yml | Low | Yes |
| P1-1 | .github/workflows/ci.yml | Low | Yes |
| P1-2 | coverlet.runsettings (new), ci.yml | Medium | Yes |
| P1-4 | .dependency-check-suppressions.xml (new) | Low | Yes |
| P2-1 | .github/workflows/ci.yml | High | Yes |
| P2-2 | .github/workflows/ci.yml | Low | Yes |

**Total Files Modified:** 3
**Total Files Created:** 2
**Estimated Time:** 2-4 hours
**Risk Assessment:** Low-Medium

---

**End of CI/CD Fixes Document**
