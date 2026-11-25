namespace Nontiwat_7362_L002.Data;

// Columns and tables for the role features (design 2026-11-21). Safe to run on every start.
public static class RoleFeaturesSchema
{
    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        // Admin can suspend an account: it cannot sign in, and a session already open is ended.
        await SchemaHelpers.AddColumnIfMissingAsync(db, "users", "IsSuspended", "tinyint(1) NOT NULL DEFAULT 0");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "users", "SuspendedReason", "varchar(200) NULL");
    }
}
