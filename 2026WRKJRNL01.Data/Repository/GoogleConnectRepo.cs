using Microsoft.EntityFrameworkCore;
using SDPBC.DataFramework.Repositories;
using WRKJRNL01.Data.Models;

namespace WRKJRNL01.Data.Repository
{

    public class GoogleConnectRepo : DataRepositoryBase<WRKJRNL01Context>
    {
        private readonly IDbContextFactory<WRKJRNL01Context> contextFactory;

        //constructor do not touch
        public GoogleConnectRepo(IDbContextFactory<WRKJRNL01Context> contextFactory): base(contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<GoogleCalendarConnection?> GetByEmailAsync(string email)
        {
            await using var context =
                await contextFactory.CreateDbContextAsync();

            return await context.GoogleCalendarConnections
                .FirstOrDefaultAsync(x =>
                    x.UserEmail == email);
        }

        public async Task<bool> IsConnectedAsync(string email)
        {
            await using var context =
                await contextFactory.CreateDbContextAsync();

            return await context.GoogleCalendarConnections
                .AnyAsync(x =>
                    x.UserEmail == email &&
                    x.GoogleConnected);
        }

        public async Task SaveConnectionAsync(string email, string encryptedRefreshToken)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var connection = await context.GoogleCalendarConnections
                .FirstOrDefaultAsync(x => x.UserEmail == email);

            if (connection == null)
            {
                connection = new GoogleCalendarConnection
                {
                    PkId = Guid.NewGuid(),
                    UserEmail = email,
                    CreatedDateUtc = DateTime.UtcNow,
                    GoogleRefreshToken = encryptedRefreshToken,
                    GoogleConnected = true,
                    UpdatedDateUtc = DateTime.UtcNow
                };

                context.GoogleCalendarConnections.Add(connection);
            }
            else
            {
                connection.GoogleRefreshToken = encryptedRefreshToken;
                connection.GoogleConnected = true;
                connection.UpdatedDateUtc = DateTime.UtcNow;
                context.GoogleCalendarConnections.Update(connection);
            }

            await context.SaveChangesAsync();
        }

        public async Task SetAutoImportEvents(string email, bool value)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var connection = await context.GoogleCalendarConnections
                .FirstOrDefaultAsync(x => x.UserEmail == email);

            if (connection == null)
            {
                //this should not happen
                return;
            }
            else
            {
                connection.AutoImportEvents = value;
                connection.UpdatedDateUtc = DateTime.UtcNow;
                context.GoogleCalendarConnections.Update(connection);
            }

            await context.SaveChangesAsync();
        }

        public async Task<bool> DisconnectAsync(string email)
        {
            Console.WriteLine($"DisconnectAsync {email}");

            await using var context =
                await contextFactory.CreateDbContextAsync();

            var connection =
                await context.GoogleCalendarConnections
                    .FirstOrDefaultAsync(x =>
                        x.UserEmail == email);

            if (connection == null)
                return false;

            connection.GoogleRefreshToken = string.Empty;
            connection.GoogleConnected = false;
            connection.UpdatedDateUtc = DateTime.UtcNow;

            // Ensure EF tracks the modification even if values appear unchanged
            context.GoogleCalendarConnections.Update(connection);

            var result = await context.SaveChangesAsync();

            return result > 0;
        }
    }
}