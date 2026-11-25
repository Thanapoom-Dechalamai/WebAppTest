namespace Nontiwat_7362_L002.Models;
public class AuditLog
{
 public long Id {get;set;}
 public int? ActorUserId {get;set;}
 public string? RequestId {get;set;}
 public string TableName {get;set;} = "";
 public string Action {get;set;} = "";
 public string RecordId {get;set;} = "";
 public string? OldData {get;set;}
 public string? NewData {get;set;}
 public DateTime CreatedAtUtc {get;set;} = DateTime.UtcNow;
}
