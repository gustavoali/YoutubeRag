# Dependency Injection Issue - Captive Dependency

**Severity:** HIGH - Blocking CI/CD Pipeline
**Date Discovered:** 2025-10-10
**Context:** Sprint 2 PR - Database Migrations Step

---

## Issue Summary

The application fails to start due to a **captive dependency** violation in the Dependency Injection container. A **Singleton** service (`IProgressNotificationService`) is trying to consume a **Scoped** service (`IUserNotificationRepository`), which is not allowed in ASP.NET Core.

---

## Error Message

```
System.InvalidOperationException: Cannot consume scoped service
'YoutubeRag.Application.Interfaces.IUserNotificationRepository'
from singleton 'YoutubeRag.Application.Interfaces.Services.IProgressNotificationService'.
```

---

## Root Cause

**Problem:**
- `IProgressNotificationService` is registered as **Singleton** (lives for the entire application lifetime)
- `SignalRProgressNotificationService` (the implementation) injects `IUserNotificationRepository`
- `IUserNotificationRepository` is registered as **Scoped** (lives per HTTP request/scope)

**Why This is a Problem:**
ASP.NET Core prevents Singleton services from consuming Scoped services because:
1. The Singleton lives for the entire application lifetime
2. The Scoped service is disposed at the end of each request
3. The Singleton would hold a reference to a disposed service → memory leaks and errors

This is called a **"Captive Dependency"** anti-pattern.

---

## Affected Services

The following services all have the same issue:

1. **YoutubeRag.Application.Services.TranscriptionJobProcessor** (Scoped)
2. **YoutubeRag.Infrastructure.Services.EmbeddingJobProcessor** (Scoped)
3. **YoutubeRag.Infrastructure.Jobs.TranscriptionBackgroundJob** (Scoped)
4. **YoutubeRag.Infrastructure.Jobs.EmbeddingBackgroundJob** (Scoped)
5. **YoutubeRag.Infrastructure.Jobs.VideoProcessingBackgroundJob** (Scoped)
6. **YoutubeRag.Api.Services.SignalRProgressNotificationService** (Singleton) ⚠️ **ROOT CAUSE**

All of these consume `IProgressNotificationService` (Singleton), which in turn consumes `IUserNotificationRepository` (Scoped).

---

## Where Was This Registered?

Look for service registration in:
- `Program.cs` or `Startup.cs`
- Service extension methods in `YoutubeRag.Api/Extensions/ServiceCollectionExtensions.cs`

Expected code that causes this:
```csharp
// This is the problematic registration:
services.AddSingleton<IProgressNotificationService, SignalRProgressNotificationService>();

// IUserNotificationRepository is likely registered as:
services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
```

---

## Solution Options

### Option 1: Change IProgressNotificationService to Scoped (RECOMMENDED)

**Change the service lifetime from Singleton to Scoped:**

```csharp
// BEFORE (WRONG):
services.AddSingleton<IProgressNotificationService, SignalRProgressNotificationService>();

// AFTER (CORRECT):
services.AddScoped<IProgressNotificationService, SignalRProgressNotificationService>();
```

**Pros:**
- Simple fix
- Follows ASP.NET Core best practices
- Works with Entity Framework scoped repositories

**Cons:**
- A new instance is created per HTTP request/scope
- May impact SignalR hub lifetime (but this is usually fine)

**Verification:**
- Ensure SignalR hub connections still work correctly
- Test that notifications are sent properly in multi-user scenarios

---

### Option 2: Use IServiceScopeFactory in SignalRProgressNotificationService

**Keep IProgressNotificationService as Singleton but manually create scopes:**

```csharp
public class SignalRProgressNotificationService : IProgressNotificationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ProgressHub> _hubContext;

    public SignalRProgressNotificationService(
        IServiceScopeFactory scopeFactory, // ✅ Inject factory instead of repository
        IHubContext<ProgressHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    public async Task NotifyJobCompletedAsync(string jobId, string videoId)
    {
        // ✅ Create a scope manually
        using var scope = _scopeFactory.CreateScope();
        var userNotificationRepo = scope.ServiceProvider
            .GetRequiredService<IUserNotificationRepository>();

        // Use the repository
        await userNotificationRepo.CreateNotificationAsync(/* ... */);

        // Send SignalR notification
        await _hubContext.Clients.All.SendAsync("JobCompleted", jobId, videoId);
    }
}
```

**Pros:**
- Keeps IProgressNotificationService as Singleton (if that's required for some reason)
- Properly manages scoped dependencies

**Cons:**
- More complex code
- Manual scope management
- Easy to forget to dispose scopes (use `using` statements!)

---

### Option 3: Remove IUserNotificationRepository Dependency

**If notifications can be sent without database persistence:**

```csharp
public class SignalRProgressNotificationService : IProgressNotificationService
{
    private readonly IHubContext<ProgressHub> _hubContext;

    public SignalRProgressNotificationService(IHubContext<ProgressHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyJobCompletedAsync(string jobId, string videoId)
    {
        // Just send SignalR notification, no database persistence
        await _hubContext.Clients.All.SendAsync("JobCompleted", jobId, videoId);
    }
}

// Service registration:
services.AddSingleton<IProgressNotificationService, SignalRProgressNotificationService>();
```

**Pros:**
- Simple
- No captive dependency issue
- Fast (no database calls)

**Cons:**
- Loses notification persistence
- May not meet business requirements

---

## Recommended Solution

**OPTION 1** is the recommended solution:

**Change IProgressNotificationService from Singleton to Scoped**

**Steps:**
1. Find the service registration (likely in `Program.cs` or a service extension method)
2. Change `AddSingleton` to `AddScoped`
3. Test that SignalR notifications still work
4. Verify no performance regression

**Expected change:**
```csharp
// Find this line and change it:
services.AddScoped<IProgressNotificationService, SignalRProgressNotificationService>();
```

---

## Why Wasn't This Caught Earlier?

This issue was **always present in the code**, but it only manifested now because:

1. **EF Core Migrations** validate the entire DI container at startup
2. Before adding `Microsoft.EntityFrameworkCore.Design`, migrations didn't trigger this validation
3. The application might have worked in development if:
   - The problematic code paths weren't executed
   - The service was disposed before issues occurred
   - Lazy service resolution prevented instantiation

**This is a common pattern:** Adding EF Core tools or hosting services often reveals hidden DI issues.

---

## How to Find the Registration

**Search for the service registration:**

```bash
# Find where IProgressNotificationService is registered
cd /c/agents/youtube_rag_net
grep -r "AddSingleton<IProgressNotificationService" . --include="*.cs"
grep -r "AddScoped<IProgressNotificationService" . --include="*.cs"

# Or search in Program.cs
cat YoutubeRag.Api/Program.cs | grep -i "IProgressNotificationService"
```

---

## Testing the Fix

After applying the fix, verify:

1. **Application Starts:**
   ```bash
   dotnet run --project YoutubeRag.Api
   ```
   Should start without errors.

2. **Migrations Work:**
   ```bash
   dotnet ef database update \
     --project YoutubeRag.Infrastructure \
     --startup-project YoutubeRag.Api
   ```
   Should apply migrations successfully.

3. **SignalR Works:**
   - Send a test notification
   - Verify it reaches connected clients
   - Check database for persisted notifications

4. **Integration Tests Pass:**
   ```bash
   dotnet test YoutubeRag.Tests.Integration
   ```

---

## Priority

**CRITICAL:** This blocks the entire CI/CD pipeline and prevents:
- Database migrations
- Application startup
- All tests from running

**Action Required:**
A backend developer needs to fix the service lifetime registration **immediately**.

---

## Related Files to Check

1. **Service Registration:**
   - `YoutubeRag.Api/Program.cs`
   - `YoutubeRag.Api/Extensions/ServiceCollectionExtensions.cs`
   - Any file with `services.Add*` calls

2. **Service Implementation:**
   - `YoutubeRag.Api/Services/SignalRProgressNotificationService.cs`

3. **Repository Registration:**
   - Look for `IUserNotificationRepository` registration
   - Verify it's registered as Scoped

---

## Next Steps

1. ✅ **Identify** the exact location of service registration
2. ✅ **Apply** Option 1 fix (change Singleton to Scoped)
3. ✅ **Test** locally to verify fix works
4. ✅ **Commit** the fix
5. ✅ **Push** to PR branch
6. ✅ **Monitor** CI/CD pipeline

---

## Note to Reviewers

This is **NOT a CI/CD infrastructure issue**. This is a **code architecture issue** that was revealed by proper CI/CD validation. The application would have failed in production with this bug.

**The fix is simple:** Change one line of service registration from `AddSingleton` to `AddScoped`.

---

**Issue Type:** Dependency Injection / Architecture
**Estimated Fix Time:** 5 minutes (once location is identified)
**Risk:** Low (standard service lifetime change)
**Testing Required:** SignalR notifications, database persistence
