using _2026WRKJRNL01.Shared.Services;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.NativeInterop;
using SDPBC.DataFramework.Repositories;
using WRKJRNL01.Data.Models;

namespace WRKJRNL01.Data.Repository
{
    public class CatRepo : DataRepositoryBase<WRKJRNL01Context>
    {
        private readonly IDbContextFactory<WRKJRNL01Context> contextFactory;

            private readonly ILogger<CatRepo> logger;

            public CatRepo(IDbContextFactory<WRKJRNL01Context> contextFactory, ILogger<CatRepo> logger): base(contextFactory)
            {
                this.contextFactory = contextFactory;
                this.logger = logger;
            }

        /// <summary>
        /// Gets the categories for a department, optionally including employee-specific custom categories.
        /// </summary>
        public Task<List<Category>> GetAllCategoriesAsync(string? deptId, bool includeCustom, CancellationToken cancellationToken = default) =>
            QueryCategoriesAsync(deptId, emplId: null, includeCustom, cancellationToken);

        /// <summary>
        /// Gets the shared categories for a department plus the custom categories owned by the given employee.
        /// </summary>
        public Task<List<Category>> GetAllCategoriesAsync(string? deptId, string? empId, CancellationToken cancellationToken = default) =>
            QueryCategoriesAsync(deptId, empId, includeCustom: true, cancellationToken);

        private async Task<List<Category>> QueryCategoriesAsync(string? deptId, string? emplId, bool includeCustom, CancellationToken cancellationToken)
        {
            
           // logger.LogDebug($"Loading categories. DeptId={deptId ?? "null"}, EmployeeId={emplId ?? "null"}, IncludeCustom={includeCustom}");
            
            logger.LogDebug("Loading categories. DeptId={deptId}, EmployeeId={emplId}, IncludeCustom={includeCustom}", deptId, emplId, includeCustom);

            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            IQueryable<Category> query = context.Categories.AsNoTracking();

            // no department means the global (unscoped) category set
            query = string.IsNullOrEmpty(deptId)
                ? query.Where(c => string.IsNullOrEmpty(c.DeptId))
                : query.Where(c => c.DeptId == deptId);

            if (!includeCustom)
            {
                query = query.Where(c => string.IsNullOrEmpty(c.EmplId));
            }
            else if (!string.IsNullOrEmpty(emplId))
            {
                // shared categories plus only this employee's custom ones
                query = query.Where(c => string.IsNullOrEmpty(c.EmplId) || c.EmplId == emplId);
            }

            // shared categories first, then custom
            var results = await query
                .OrderBy(c => string.IsNullOrEmpty(c.EmplId) ? 0 : 1)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);

           // logger.Debug($"Loaded {results.Count} categories. DeptId={deptId ?? "null"}, EmployeeId={emplId ?? "null"}");

            return results;
        }

        /// <summary>
        /// Deletes a category. Returns false when the category no longer exists.
        /// </summary>
        public async Task<bool> RemoveAsync(Category cat, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(cat);

            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var existingCategory = await context.Categories.FindAsync([cat.PkId], cancellationToken);
            if (existingCategory is null)
            {
                return false;
            }

            context.Categories.Remove(existingCategory);
            await context.SaveChangesAsync(cancellationToken);

            return true;
        }

        /// <summary>
        /// Inserts a new category or updates an existing one.
        /// Returns the category with <see cref="Category.Error"/> populated when the name is already taken.
        /// </summary>
        public async Task<Category> Upsert(Category cat, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(cat);

            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            //prevent adding duplicate categories by name
            var duplicateExists = await context.Categories
                .AnyAsync(c => c.Name.ToLower() == cat.Name.ToLower() && c.PkId != cat.PkId, cancellationToken);

            if (duplicateExists)
            {
                cat.Error = "Category already exists!";
                return cat;
            }

            // an empty key means this is a new category
            if (cat.PkId == Guid.Empty)
            {
                context.Categories.Add(cat);
            }
            else
            {
                context.Categories.Update(cat);
            }

            await context.SaveChangesAsync(cancellationToken);

            return cat;
        }

        /// <summary>
        /// Gets a single category by its primary key, or null when not found.
        /// </summary>
        public async Task<Category?> Get(Guid id, CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.PkId == id, cancellationToken);
        }
    }
}