using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Data;

public static class SchemaPatcher
{
    public static void EnsureBookingSchema(FilmDbContext db)
    {
        var provider = db.Database.ProviderName ?? string.Empty;

        if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            EnsureSqliteColumn(db, "Cinemas", "CapienzaTotale", "INTEGER NOT NULL DEFAULT 120");
            EnsureSqliteColumn(db, "Prenotazioni", "PostiSelezionati", "TEXT NULL");
            return;
        }

        if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        {
            EnsureMySqlColumn(db, "Cinemas", "CapienzaTotale", "INT NOT NULL DEFAULT 120");
            EnsureMySqlColumn(db, "Prenotazioni", "PostiSelezionati", "VARCHAR(2000) NULL");
        }
    }

    private static void EnsureSqliteColumn(FilmDbContext db, string tableName, string columnName, string definition)
    {
        try
        {
            if (SqliteColumnExists(db, tableName, columnName))
            {
                return;
            }

            var sql = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {definition};";
#pragma warning disable EF1002
            db.Database.ExecuteSqlRaw(sql);
#pragma warning restore EF1002
        }
        catch
        {
            // Ignore schema patch failures in runtime startup.
        }
    }

    private static bool SqliteColumnExists(FilmDbContext db, string tableName, string columnName)
    {
        using var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var current = reader[1]?.ToString();
            if (string.Equals(current, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureMySqlColumn(FilmDbContext db, string tableName, string columnName, string definition)
    {
        try
        {
            var sql = $"ALTER TABLE `{tableName}` ADD COLUMN `{columnName}` {definition};";
#pragma warning disable EF1002
            db.Database.ExecuteSqlRaw(sql);
#pragma warning restore EF1002
        }
        catch (DbException ex)
        {
            var msg = ex.Message ?? string.Empty;
            if (msg.Contains("Duplicate column", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }
        catch
        {
            // Ignore schema patch failures in runtime startup.
        }
    }
}
