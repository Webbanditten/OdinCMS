---
title: Dependency Injection Registration
impact: HIGH
impactDescription: Unregistered services cause runtime DI resolution failures
tags: [architecture, dependency-injection, startup]
---

# Dependency Injection Registration

All services must be registered in `Startup.cs` in the `ConfigureServices` method.

## Pattern

```csharp
// In Startup.cs ConfigureServices method:
services.AddScoped<IMyFeatureService, MyFeatureService>();
```

## Rules

1. ALWAYS register as `AddScoped` (per-request lifetime) — this matches EF Core's DbContext lifetime
2. Register the interface, not the implementation
3. Place registration near related services (alphabetical or grouped by feature)
4. Constructor injection is the only DI pattern used — never use service locator

## Incorrect

```csharp
// Singleton with DbContext dependency will cause threading issues
services.AddSingleton<IMyService, MyService>();

// No interface - can't mock for testing
services.AddScoped<MyService>();
```

## Correct

```csharp
// Scoped lifetime matching DbContext
services.AddScoped<IMyFeatureService, MyFeatureService>();
```

## Verification

If you get a runtime error like `Unable to resolve service for type 'IMyFeatureService'`, you forgot to register it in Startup.cs.
