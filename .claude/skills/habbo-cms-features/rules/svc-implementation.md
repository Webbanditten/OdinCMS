---
title: Service Implementation
impact: HIGH
impactDescription: Incorrect service implementations cause data access issues and poor performance
tags: [service, implementation, entity-framework, database]
---

# Service Implementation

Service implementations handle all database access via Entity Framework Core.

## Template

```csharp
// Services/Implementations/MyFeatureService.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.Implementations
{
    public class MyFeatureService : IMyFeatureService
    {
        private readonly DataContext _context;

        public MyFeatureService(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MyFeature>> GetAll()
        {
            return await _context.MyFeatures
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<MyFeature> GetById(int id)
        {
            return await _context.MyFeatures.FindAsync(id);
        }

        public async Task<MyFeature> Create(MyFeature model)
        {
            model.CreatedAt = DateTime.Now;
            _context.MyFeatures.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<MyFeature> Update(MyFeature model)
        {
            var existing = await _context.MyFeatures.FindAsync(model.Id);
            if (existing == null) return null;

            // Map fields explicitly (don't overwrite navigation properties)
            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task Remove(int id)
        {
            var item = await _context.MyFeatures.FindAsync(id);
            if (item != null)
            {
                _context.MyFeatures.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<MyFeature>> Search(string query, int take, int skip)
        {
            var queryable = _context.MyFeatures.AsQueryable();
            
            if (!string.IsNullOrEmpty(query))
            {
                queryable = queryable.Where(f => f.Name.Contains(query));
            }

            return await queryable
                .OrderByDescending(f => f.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}
```

## Rules

1. Only inject `DataContext` — never other services (use controller-level composition)
2. Always use `async`/`await` with `ToListAsync()`, `FindAsync()`, `SaveChangesAsync()`
3. For updates, fetch the existing entity first, then map properties explicitly
4. Never call `_context.Update(model)` with a detached entity — it overwrites all fields
5. Use `AsNoTracking()` for read-only queries to improve performance
6. Use `Skip()`/`Take()` for pagination
7. Return `null` for not-found scenarios (let the controller handle the 404)
