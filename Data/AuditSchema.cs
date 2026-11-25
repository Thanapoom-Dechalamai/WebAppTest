using Microsoft.EntityFrameworkCore;
namespace Nontiwat_7362_L002.Data;
public static class AuditSchema
{
 public static async Task EnsureAsync(ApplicationDbContext db)
 {
  await db.Database.ExecuteSqlRawAsync("""
   CREATE TABLE IF NOT EXISTS audit_logs (
    Id bigint AUTO_INCREMENT PRIMARY KEY, ActorUserId int NULL, RequestId varchar(128) NULL,
    TableName varchar(64) NOT NULL, Action varchar(32) NOT NULL,
    RecordId varchar(160) NOT NULL, OldData json NULL, NewData json NULL,
    CreatedAtUtc datetime(6) NOT NULL, INDEX IX_audit_record (TableName,RecordId),
    INDEX IX_audit_time (CreatedAtUtc)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
   """);
  // A custom room allotment key contains both 32-character IDs and the date.
  var recordIdLength = await db.Database.SqlQueryRaw<long>("""
   SELECT CAST(CHARACTER_MAXIMUM_LENGTH AS SIGNED) AS Value
   FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'audit_logs' AND COLUMN_NAME = 'RecordId'
   """).SingleAsync();
  if (recordIdLength < 160)
   await db.Database.ExecuteSqlRawAsync("ALTER TABLE audit_logs MODIFY COLUMN RecordId varchar(160) NOT NULL");
 }
}
