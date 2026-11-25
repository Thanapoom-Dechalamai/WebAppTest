using Microsoft.EntityFrameworkCore;
namespace Nontiwat_7362_L002.Data;

// Small helpers for the startup schema scripts (CREATE/ALTER run every start, so they must be idempotent).
// Table, column and constraint names are identifiers (SQL parameters cannot hold them) and always come from
// constants in the *Schema.cs files, never from user input, so the raw ALTER statements below are safe.
#pragma warning disable EF1002
public static class SchemaHelpers
{
    public static Task<long> ColumnCountAsync(ApplicationDbContext db, string table, string column) =>
        CountAsync(db, "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @name", table, column);

    // Adds a column only when it is missing. 'definition' is the SQL after the column name.
    public static async Task AddColumnIfMissingAsync(ApplicationDbContext db, string table, string column, string definition)
    {
        if (await ColumnCountAsync(db, table, column) == 0)
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition}");
    }

    // Adds a named constraint (e.g. a foreign key) only when the table has no constraint with that name.
    // 'definition' is the SQL after CONSTRAINT <name>, e.g. "FOREIGN KEY (X) REFERENCES users(Id) ON DELETE SET NULL".
    public static async Task AddConstraintIfMissingAsync(ApplicationDbContext db, string table, string constraint, string definition)
    {
        var count = await CountAsync(db, "SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND CONSTRAINT_NAME = @name", table, constraint);
        if (count == 0)
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE `{table}` ADD CONSTRAINT `{constraint}` {definition}");
    }

    private static async Task<long> CountAsync(ApplicationDbContext db, string sql, string table, string name)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            foreach (var (parameterName, value) in new[] { ("@table", table), ("@name", name) })
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = parameterName;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
#pragma warning restore EF1002
