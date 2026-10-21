// LOCAL / OFFLINE SUPPORT
//
// This partial adapts the SQL-Server-scaffolded model so it also works on SQLite, which
// is used when running the app locally outside the corporate network.
//
// Two SQL-Server-isms break on SQLite and are handled here:
//   1. Guid primary keys use HasDefaultValueSql("(newid())"). SQLite has no newid(), so we
//      strip those server defaults and instead assign Guid.NewGuid() in SaveChanges for any
//      new entity whose Guid key is still empty.
//   2. Server-specific column type names (datetime/text/tinyint/smallint/decimal(x,y)) are
//      left as-is; EF's SQLite provider maps unknown type names to compatible affinities,
//      so EnsureCreated still produces a usable schema.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace WRKJRNL01.Data.Models;

public partial class WRKJRNL01Context
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        if (!Database.IsSqlite())
            return;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                // Drop SQL Server default-value SQL (e.g. newid()) that SQLite can't execute.
                if (property.GetDefaultValueSql() is not null)
                {
                    property.SetDefaultValueSql(null);

                    // Guid keys were relying on newid(); generate them client-side instead.
                    if (property.ClrType == typeof(Guid))
                    {
                        property.ValueGenerated = ValueGenerated.Never;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Assigns a new Guid to any added entity whose Guid key is still empty.
    /// Needed because SQLite (unlike SQL Server) does not generate key values for us.
    /// </summary>
    private void EnsureGuidKeys()
    {
        if (!Database.IsSqlite())
            return;

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key is null)
                continue;

            foreach (var keyProp in key.Properties.Where(p => p.ClrType == typeof(Guid)))
            {
                var member = entry.Property(keyProp.Name);
                if (member.CurrentValue is Guid g && g == Guid.Empty)
                {
                    member.CurrentValue = Guid.NewGuid();
                }
            }
        }
    }

    public override int SaveChanges()
    {
        EnsureGuidKeys();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureGuidKeys();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override System.Threading.Tasks.Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        System.Threading.CancellationToken cancellationToken = default)
    {
        EnsureGuidKeys();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override System.Threading.Tasks.Task<int> SaveChangesAsync(
        System.Threading.CancellationToken cancellationToken = default)
    {
        EnsureGuidKeys();
        return base.SaveChangesAsync(cancellationToken);
    }
}
