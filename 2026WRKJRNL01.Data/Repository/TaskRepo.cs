using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SDPBC.DataFramework.Repositories;
using System.Data.SqlTypes;
using System.Net.NetworkInformation;
using WRKJRNL01.Data.Models;

namespace WRKJRNL01.Data.Repository
{
    public class TaskRepo : DataRepositoryBase<WRKJRNL01Context>
    {
        private readonly IDbContextFactory<WRKJRNL01Context> _contextFactory;

        protected CatRepo CatRepo { get; }

        //contructor do not touch
        public TaskRepo(IDbContextFactory<WRKJRNL01Context> contextFactory, CatRepo catRepo) : base(contextFactory)
        {
            _contextFactory = contextFactory;
            CatRepo = catRepo;
        }

        public async Task<List<Tasque>> GetAllTasksAsync(bool includeDeleted)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Tasques.Where(t => includeDeleted || !t.IsDeleted).ToListAsync();
        }

        public async Task<List<Tasque>> GetAllTasksAsync(string? empId, bool includeDeleted)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tasques.Where(t => t.EmplId == empId && (includeDeleted || !t.IsDeleted)).ToListAsync();
        }

        public async Task<List<DateTime>> GetTaskDates(string? empId, bool includeDeleted)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tasques.Where(t => t.EmplId == empId && (includeDeleted || !t.IsDeleted)).Select(t => t.Date).ToListAsync();
        }

        public async Task<List<DateTime>> GetTaskDates(string? empId, int month, bool includeDeleted)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tasques.Where(t => t.EmplId == empId && t.Date.Month == month && (includeDeleted || !t.IsDeleted)).Select(t => t.Date).ToListAsync();
        }

        public async Task<List<Tasque>> GetAllTasksAsync(string? empId, DateTime startDate, DateTime endDate, bool includeDeleted)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var TaskList = string.IsNullOrEmpty(empId) ? 
                await context.Tasques
                .Where(t => 
                t.Date >= startDate &&
                t.Date <= endDate &&
                (includeDeleted || !t.IsDeleted))
                .ToListAsync() :

                await context.Tasques
                .Where(t => 
                t.EmplId == empId &&
                t.Date >= startDate &&
                t.Date <= endDate &&
                (includeDeleted || !t.IsDeleted))
                .ToListAsync();

            foreach (var task in TaskList)
            {
                task.TaskCategory = await CatRepo.Get(task.CatId);
            }

            return TaskList;
        }

        public async Task<List<Tasque>> GetAllTasksForDeptAsync(string deptId, DateTime startDate, DateTime endDate)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var TaskList = 
                await context.Tasques
                .Where(t =>
                t.DeptId == deptId &&
                t.Date.Date >= startDate.Date &&
                t.Date.Date <= endDate.Date)
                .ToListAsync();

            foreach (var task in TaskList)
            {
                task.TaskCategory = await CatRepo.Get(task.CatId);
            }

            return TaskList;
        }

        public async Task<List<Tasque>> GetAllTasksAsync(string empId, DateTime date, bool includeDeleted)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var TaskList = await context.Tasques
                .Where(t =>
                t.EmplId == empId &&
                t.Date.Date == date.Date &&
                (includeDeleted || !t.IsDeleted))
                .ToListAsync();

            foreach (var task in TaskList)
            {
                task.TaskCategory = await CatRepo.Get(task.CatId);
            }

            return TaskList;
        }

        public async Task<List<Tasque>> GetDeletedTasksAsync(string empId, DateTime date)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var TaskList = await context.Tasques
                .Where(t =>
                t.EmplId == empId &&
                t.Date.Date == date.Date &&
                t.IsDeleted == true)
                .ToListAsync();

            foreach (var task in TaskList)
            {
                task.TaskCategory = await CatRepo.Get(task.CatId);
            }

            return TaskList;
        }

        public async Task<List<Tasque>> GetDeletedTasksAsync(string? empId, DateTime startDate, DateTime endDate)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var TaskList = string.IsNullOrEmpty(empId) ?
                await context.Tasques
                .Where(t =>
                t.Date >= startDate &&
                t.Date <= endDate &&
                t.IsDeleted == true)
                .ToListAsync() :

                await context.Tasques
                .Where(t =>
                t.EmplId == empId &&
                t.Date >= startDate &&
                t.Date <= endDate &&
                t.IsDeleted == true)
                .ToListAsync();

            foreach (var task in TaskList)
            {
                task.TaskCategory = await CatRepo.Get(task.CatId);
            }

            return TaskList;
        }

        public async Task<Tasque> Upsert(Tasque taq)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // Check for duplicate Google Event (this will not trigger custom made events)
            if (!string.IsNullOrWhiteSpace(taq.EventId) && taq.PkId == Guid.Empty)
            {
                var existing = await context.Tasques
                    .FirstOrDefaultAsync(t => t.EventId == taq.EventId);

                if (existing != null)
                {
                    return existing;
                }
            }

            if (taq.PkId == Guid.Empty)
            {
                context.Tasques.Add(taq);
            }
            else
            {
                context.Tasques.Update(taq);
            }

            await context.SaveChangesAsync();

            return taq;
        }

        public async Task<IEnumerable<Tasque>> Upsert(IEnumerable<Tasque> taqs)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            foreach (var taq in taqs)
            {
                if (taq.Date < SqlDateTime.MinValue.Value)
                {
                    Console.WriteLine($"Bad Date: {taq.Date}");
                    continue;
                }

                Tasque? existing = null;

                if (!string.IsNullOrWhiteSpace(taq.EventId))
                {
                    existing = await context.Tasques
                        .FirstOrDefaultAsync(t => t.EventId == taq.EventId || t.PkId == taq.PkId);
                }

                if (existing is not null)
                {
                    context.Entry(existing).CurrentValues.SetValues(taq);
                }
                else
                {
                    context.Tasques.Add(taq);
                }
            }

            await context.SaveChangesAsync();

            return taqs;
        }

        public async Task<Tasque> Get(Tasque taq)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Tasques.FindAsync(taq);
        }

        public async Task<Tasque> Get(Guid Id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Tasques.Where(c => c.PkId == Id).FirstOrDefaultAsync();
        }

        public bool Remove(Tasque task, bool deleteFromDbase)
        {
            using var context = _contextFactory.CreateDbContext();

            var existingTask = context.Tasques.Find(task.PkId);

            if (existingTask == null) 
            {
                return false;
            }

            if (deleteFromDbase)
            {
                //remove item
                context.Tasques.Remove(existingTask);
            } else
            {
                //flag item as deleted
                existingTask.IsDeleted = true;
                context.Tasques.Update(existingTask);
            }

            context.SaveChanges();

            return true;
        }

        public async Task<bool> Restore(Tasque task)
        {
            using var context = _contextFactory.CreateDbContext();

            var existingTask = context.Tasques.Find(task.PkId);

            if (existingTask == null)
            {
                return false;
            }

                //flag item as deleted
                existingTask.IsDeleted = false;
                context.Tasques.Update(existingTask);

            context.SaveChanges();

            return true;
        }

        public async Task<bool> HasJournalEntryAsync(string empId,DateTime date)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            return await context.Tasques.AnyAsync(t =>
                t.EmplId == empId &&
                t.Date >= startDate.Date &&
                t.Date <= endDate.Date);
        }

        public async Task<HashSet<string>> GetEmployeesWithJournalEntriesAsync(DateTime date)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var employeeIds = await context.Tasques
                .Where(t =>
                    t.Date >= startDate.Date &&
                    t.Date <= endDate.Date)
                .Select(t => t.EmplId)
                .Distinct()
                .ToListAsync();

            return employeeIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

    }
}
