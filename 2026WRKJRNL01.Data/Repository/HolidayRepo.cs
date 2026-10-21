using Microsoft.EntityFrameworkCore;
using SDPBC.DataFramework.Repositories;
using WRKJRNL01.Data.Models;

namespace WRKJRNL01.Data.Repository
{
    public class HolidayRepo : DataRepositoryBase<WRKJRNL01Context>
    {
        private readonly IDbContextFactory<WRKJRNL01Context> _contextFactory;

        public HolidayRepo(IDbContextFactory<WRKJRNL01Context> contextFactory) : base(contextFactory)
        {
            _contextFactory = contextFactory;
        }

        //return true if the date is a holiday, false otherwise
        public async Task<bool> Get(DateTime date)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Holidays.AnyAsync(d => d.Date == date);
        }
    }

}