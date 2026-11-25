namespace Nontiwat_7362_L002.Models;
public class PromotionRedemption {
 public int Id {get;set;}
 public int PromotionId {get;set;}
 public int UserId {get;set;}
 public int OrderId {get;set;}
 public string Code {get;set;} = "";
 public decimal Subtotal {get;set;}
 public decimal DiscountAmount {get;set;}
 public DateTime CreatedAtUtc {get;set;}=DateTime.UtcNow;
}
