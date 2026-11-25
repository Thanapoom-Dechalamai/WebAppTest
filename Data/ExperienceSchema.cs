using Microsoft.EntityFrameworkCore;
namespace Nontiwat_7362_L002.Data;
public static class ExperienceSchema
{
 public static async Task EnsureAsync(ApplicationDbContext db)
 {
  async Task Column(string table, string name, string type) {
   var connection=db.Database.GetDbConnection();
   await db.Database.OpenConnectionAsync();
   try {
    await using var command=connection.CreateCommand();
    command.CommandText="SELECT COUNT(*) FROM information_schema.columns WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@t AND COLUMN_NAME=@c";
    var t=command.CreateParameter(); t.ParameterName="@t"; t.Value=table; command.Parameters.Add(t);
    var c=command.CreateParameter(); c.ParameterName="@c"; c.Value=name; command.Parameters.Add(c);
    if(Convert.ToInt32(await command.ExecuteScalarAsync())==0)
    {
     // Identifiers and types are private compile-time constants above, never request input.
     var ddl=$"ALTER TABLE `{table}` ADD COLUMN `{name}` {type}";
     await db.Database.ExecuteSqlRawAsync(ddl);
    }
   } finally { await db.Database.CloseConnectionAsync(); }
  }
  await Column("promotions","Code","varchar(40) NULL UNIQUE");
  await Column("promotions","DiscountKind","varchar(10) NOT NULL DEFAULT 'Percent'");
  await Column("promotions","DiscountValue","decimal(12,2) NOT NULL DEFAULT 0");
  await Column("promotions","MinNights","int NOT NULL DEFAULT 1");
  await Column("promotions","MinSpend","decimal(12,2) NOT NULL DEFAULT 0");
  await Column("promotions","MaxDiscount","decimal(12,2) NULL");
  await Column("stay_orders","Subtotal","decimal(12,2) NULL");
  await Column("stay_orders","DiscountAmount","decimal(12,2) NOT NULL DEFAULT 0");
  await Column("stay_orders","PromotionCode","varchar(40) NULL");
  await Column("hotels","PhotoUrl","varchar(1000) NULL");
  await Column("hotels","PhotoSourceUrl","varchar(1000) NULL");
  await Column("hotels","PhotoCredit","varchar(200) NULL");
  await Column("stay_environments","SourceUrl","varchar(1000) NULL");
  await Column("stay_environments","VerifiedAtUtc","datetime(6) NULL");
  await db.Database.ExecuteSqlRawAsync("""
   CREATE TABLE IF NOT EXISTS promotion_redemptions (
    Id int AUTO_INCREMENT PRIMARY KEY, PromotionId int NOT NULL, UserId int NOT NULL,
    OrderId int NOT NULL, Code varchar(40) NOT NULL, Subtotal decimal(12,2) NOT NULL,
    DiscountAmount decimal(12,2) NOT NULL, CreatedAtUtc datetime(6) NOT NULL,
    UNIQUE KEY UX_promotion_user(PromotionId,UserId), UNIQUE KEY UX_promotion_order(OrderId),
    FOREIGN KEY (PromotionId) REFERENCES promotions(Id) ON DELETE RESTRICT,
    FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE RESTRICT,
    FOREIGN KEY (OrderId) REFERENCES stay_orders(Id) ON DELETE RESTRICT
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
   """);
 }
}
