using Microsoft.EntityFrameworkCore;
using SDPBC.DataFramework.Repositories;
using WRKJRNL01.Data.Models;

namespace WRKJRNL01.Data.Repository
{
    public class UserRepo : DataRepositoryBase<WRKJRNL01Context>
    {
        private readonly IDbContextFactory<WRKJRNL01Context> _contextFactory;

        public UserRepo(
            IDbContextFactory<WRKJRNL01Context> contextFactory)
            : base(contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .ToListAsync();
        }
    }
}