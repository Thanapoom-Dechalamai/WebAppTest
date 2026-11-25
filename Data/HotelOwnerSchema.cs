using Microsoft.EntityFrameworkCore;
namespace Nontiwat_7362_L002.Data;
public static class HotelOwnerSchema
{
    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS hotel_assignments (
                HotelId varchar(32) NOT NULL, OwnerUserId int NOT NULL,
                UpdatedAtUtc datetime(6) NOT NULL,
                PRIMARY KEY (HotelId, OwnerUserId),
                KEY IX_hotel_assignments_owner (OwnerUserId),
                FOREIGN KEY (OwnerUserId) REFERENCES users(Id) ON DELETE RESTRICT
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
        // Older databases used HotelId alone as the key (one staff account per hotel).
        // Widen it so a hotel can have a team; existing rows are kept as they are.
        if (await PrimaryKeyColumnCountAsync(db, "hotel_assignments") == 1)
        {
            // InnoDB already keeps an index on OwnerUserId for its foreign key, so only the key changes.
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE hotel_assignments DROP PRIMARY KEY, ADD PRIMARY KEY (HotelId, OwnerUserId)");
        }
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS hotel_owner_entries (
                Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, OwnerUserId int NOT NULL, OrderId int NOT NULL,
                HotelId varchar(32) NOT NULL, HotelName varchar(120) NOT NULL, Kind varchar(20) NOT NULL,
                Amount decimal(12,2) NOT NULL, CreatedAtUtc datetime(6) NOT NULL,
                UNIQUE KEY UX_hotel_owner_order_kind (OrderId, Kind),
                FOREIGN KEY (OwnerUserId) REFERENCES users(Id) ON DELETE RESTRICT,
                FOREIGN KEY (OrderId) REFERENCES stay_orders(Id) ON DELETE RESTRICT
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS hotel_payouts (
                Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, HotelId varchar(32) NOT NULL,
                HotelName varchar(120) NOT NULL, Amount decimal(12,2) NOT NULL, Reference varchar(120) NOT NULL,
                Method varchar(20) NOT NULL DEFAULT 'BankTransfer', Note varchar(255) NULL,
                PaidByUserId int NOT NULL, CreatedAtUtc datetime(6) NOT NULL,
                KEY IX_hotel_payouts_hotel (HotelId),
                FOREIGN KEY (PaidByUserId) REFERENCES users(Id) ON DELETE RESTRICT
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
        // hotel_payouts created before Method/Note existed: add the columns, keep the rows.
        if (await ColumnCountAsync(db, "hotel_payouts", "Method") == 0)
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE hotel_payouts ADD COLUMN Method varchar(20) NOT NULL DEFAULT 'BankTransfer' AFTER Reference, ADD COLUMN Note varchar(255) NULL AFTER Method");
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS hotel_decisions (
                OrderId int NOT NULL PRIMARY KEY, ActorUserId int NOT NULL,
                Action varchar(20) NOT NULL, Note varchar(255) NOT NULL, UpdatedAtUtc datetime(6) NOT NULL,
                FOREIGN KEY (OrderId) REFERENCES stay_orders(Id) ON DELETE RESTRICT
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
    }

    private static async Task<long> ColumnCountAsync(ApplicationDbContext db, string table, string column)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @column";
            foreach (var (name, value) in new[] { ("@table", table), ("@column", column) })
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    private static async Task<long> PrimaryKeyColumnCountAsync(ApplicationDbContext db, string table)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND CONSTRAINT_NAME = 'PRIMARY'";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@table";
            parameter.Value = table;
            command.Parameters.Add(parameter);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
