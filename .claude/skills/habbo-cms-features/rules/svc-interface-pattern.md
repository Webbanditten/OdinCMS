---
title: Service Interface Pattern
impact: HIGH
impactDescription: Services without interfaces cannot be properly mocked or swapped
tags: [service, interface, abstraction]
---

# Service Interface Pattern

All business logic lives in services. Services are always defined as an interface + implementation pair.

## Interface Convention

```csharp
// Services/Interfaces/IMyFeatureService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface IMyFeatureService
    {
        Task<IEnumerable<MyFeature>> GetAll();
        Task<MyFeature> GetById(int id);
        Task<MyFeature> Create(MyFeature model);
        Task<MyFeature> Update(MyFeature model);
        Task Remove(int id);
        
        // Search/filter methods
        Task<IEnumerable<MyFeature>> Search(string query, int take, int skip);
        
        // Count methods for pagination
        Task<int> Count();
    }
}
```

## Rules

1. Interface goes in `Services/Interfaces/` with `I` prefix
2. Implementation goes in `Services/Implementations/`
3. ALL methods return `Task<T>` (async throughout)
4. Use `IEnumerable<T>` for collections (not `List<T>`) in the interface
5. Parameters use the entity model or primitives — never request/response objects
6. Name methods clearly: `GetAll`, `GetById`, `GetByUserId`, `Search`, `Create`, `Update`, `Remove`
7. For search results with pagination, return a DTO with both results and total count

## Pagination Pattern

```csharp
// In the interface:
Task<(IEnumerable<MyFeature> Items, int TotalCount)> Search(string query, int take, int skip);

// Or use a dedicated result class:
public class SearchResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalResults { get; set; }
}
```
