# Pre-Commit Hooks Configuration

## Overview

This project uses **Husky.NET** to enforce code quality standards through Git hooks. These hooks run automatically before commits and pushes to catch issues early.

## What are Pre-Commit Hooks?

Git hooks are scripts that run automatically at specific points in the Git workflow:
- **pre-commit**: Runs before a commit is created
- **pre-push**: Runs before changes are pushed to remote

## Installed Hooks

### Pre-Commit Hook

Runs **before every commit** and performs:

1. **Code Formatting Check** (`dotnet format --verify-no-changes`)
   - Validates code follows .editorconfig standards
   - Ensures consistent formatting across the codebase
   - Prevents commits with formatting violations

2. **Build Verification** (`dotnet build --no-restore --configuration Release`)
   - Ensures the solution builds successfully
   - Catches compilation errors before commit
   - Uses Release configuration for stricter checks

**Location:** `.husky/pre-commit`

### Pre-Push Hook

Runs **before pushing to remote** and performs:

1. **Unit Tests** (`dotnet test --no-build --configuration Release --filter Category=Unit`)
   - Runs all unit tests (Category=Unit)
   - Prevents pushing broken code
   - Uses already-built binaries from pre-commit build

**Location:** `.husky/pre-push`

## Setup

### Automatic Setup

Hooks are automatically installed when you run:
```bash
# Linux/macOS
./scripts/dev-setup.sh

# Windows
.\scripts\dev-setup.ps1

# Or manually
dotnet husky install
```

### Manual Installation

If hooks aren't working:

```bash
# 1. Restore .NET tools (includes Husky)
dotnet tool restore

# 2. Install hooks
dotnet husky install

# 3. Verify installation
ls -la .git/hooks/
```

## Hook Workflow

### Typical Commit Flow

```bash
# 1. Make code changes
vim Program.cs

# 2. Stage changes
git add Program.cs

# 3. Attempt commit
git commit -m "Add new feature"

# Hook runs automatically:
# ✅ Code formatting check... PASSED
# ✅ Build verification... PASSED
# ✅ Commit created successfully

# 4. Push to remote
git push origin my-branch

# Hook runs automatically:
# ✅ Running unit tests... PASSED (422 tests)
# ✅ Push completed successfully
```

### Failed Hook Example

```bash
# Scenario: Code has formatting issues
git commit -m "Quick fix"

# Output:
# ❌ Code formatting check... FAILED
# Error: 3 files need formatting:
#   - Program.cs
#   - Services/VideoService.cs
#   - Controllers/VideoController.cs
#
# Fix by running: dotnet format
# Commit aborted.

# Fix the issue
dotnet format

# Try again
git commit -m "Quick fix"
# ✅ All hooks passed
# ✅ Commit created successfully
```

## Bypassing Hooks (Not Recommended)

In **emergency situations only**, you can bypass hooks:

```bash
# Skip pre-commit hooks
git commit --no-verify -m "Emergency hotfix"

# Skip pre-push hooks
git push --no-verify
```

⚠️ **Warning:** Bypassing hooks can introduce bugs and formatting issues. Only use in emergencies and fix issues immediately after.

## Customizing Hooks

### Adding More Checks

Edit `.husky/pre-commit` or `.husky/pre-push`:

```bash
# Add security scan to pre-commit
dotnet husky add pre-commit -c "dotnet list package --vulnerable"

# Add integration tests to pre-push
dotnet husky add pre-push -c "dotnet test --filter Category=Integration"
```

### Disabling Hooks

```bash
# Temporarily disable (not recommended)
rm .git/hooks/pre-commit
rm .git/hooks/pre-push

# Re-enable
dotnet husky install
```

## Performance Tips

### Hooks Taking Too Long?

If hooks are slowing you down:

1. **Pre-Commit Build**
   - Uses `--no-restore` (packages already restored)
   - Consider: `--no-incremental` if caching causes issues

2. **Pre-Push Tests**
   - Only runs unit tests (fast)
   - Integration tests run in CI only
   - Uses `--no-build` (already built in pre-commit)

### Typical Execution Times

- **Pre-Commit:** 10-30 seconds
  - Format check: 2-5s
  - Build: 8-25s

- **Pre-Push:** 5-15 seconds
  - Unit tests: 5-15s (422 tests)

## CI/CD Integration

Hooks mirror CI pipeline checks:

| Hook | CI Equivalent | Purpose |
|------|---------------|---------|
| Pre-commit format | `dotnet format --verify-no-changes` | Formatting |
| Pre-commit build | `dotnet build` | Compilation |
| Pre-push unit tests | `dotnet test` (Unit) | Unit testing |
| CI only | `dotnet test` (Integration) | Integration tests |
| CI only | Code coverage | Coverage analysis |
| CI only | Security scan | Vulnerability check |

**Philosophy:** Fast checks locally, comprehensive checks in CI.

## Troubleshooting

### Hook Not Running

```bash
# Check if hooks are installed
ls -la .git/hooks/

# Should show:
# pre-commit -> ../.husky/pre-commit
# pre-push -> ../.husky/pre-push

# If missing, reinstall
dotnet husky install
```

### "dotnet: command not found"

Ensure .NET SDK is installed and in PATH:
```bash
dotnet --version
# Should show: 8.0.x
```

### "Husky not found"

Restore .NET tools:
```bash
dotnet tool restore
```

### Hooks Pass Locally but Fail in CI

This usually means:
1. Different .NET SDK version (check CI uses .NET 8.0)
2. Missing dependencies (restore in CI)
3. Platform-specific code (test on Linux if CI uses Linux)

### Permission Denied (Linux/macOS)

```bash
# Make hooks executable
chmod +x .husky/pre-commit
chmod +x .husky/pre-push
```

## Best Practices

1. **Keep Hooks Fast**
   - Only run quick checks (< 30 seconds)
   - Save slow tests for CI

2. **Never Commit with --no-verify**
   - Fix issues instead of bypassing
   - Bypassing creates tech debt

3. **Update Hooks Regularly**
   - Keep aligned with CI pipeline
   - Document changes in this file

4. **Test Hook Changes**
   - Make a test commit after modifying hooks
   - Ensure they work on all platforms

## Further Reading

- [Husky.NET Documentation](https://alirezanet.github.io/Husky.Net/)
- [Git Hooks Documentation](https://git-scm.com/book/en/v2/Customizing-Git-Git-Hooks)
- [Project .editorconfig](.editorconfig)
- [CI/CD Pipeline](.github/workflows/)

## Maintenance

**Last Updated:** 2025-10-10
**Husky Version:** 0.7.1
**Maintained By:** DevOps Team

---

**Questions?** Check the [Quick Reference](QUICK_REFERENCE.md) or ask in #dev-help.
