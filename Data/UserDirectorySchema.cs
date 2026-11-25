namespace Nontiwat_7362_L002.Data;

public static class UserDirectorySchema
{
    // Runs before the first seed (Program.cs): seeds read and write users, so its columns must exist first.
    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        await SchemaHelpers.AddColumnIfMissingAsync(db, "users", "LastLoginUtc", "datetime(6) NULL");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "users", "SecurityStamp", "varchar(36) NULL");
    }
}
