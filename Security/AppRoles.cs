using System.Security.Claims;
namespace Nontiwat_7362_L002.Security;

public static class AppRoles
{
    // Keep the existing database value "User" to preserve current accounts.
    public const string Customer = "User";
    public const string Staff = "Staff";
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Admin = "Admin";
    public const string AC = "AC";
    // Retain the stored role value and assignments used by existing accounts.
    // This role is presented as HotelStaff (พนักงานโรงแรม) throughout the UI.
    public const string HotelOwner = "HotelOwner";
    public const string Pattern = "^(User|Staff|Owner|Manager|Admin|AC|HotelOwner)$";
    public static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[] { Customer, Staff, Owner, Manager, Admin, AC, HotelOwner });
    // Manager may move people between Customer and Lodgo support only.
    // Finance (AC) can read every payment, so only Admin grants it.
    public static readonly string[] TeamRoles = [Customer, Staff];
    public static bool CanBook(ClaimsPrincipal user) => user.IsInRole(Customer) || user.IsInRole(Admin);
    public static bool IsValid(string? role) => role is not null && All.Contains(role, StringComparer.Ordinal);
    public static string Label(string? role) => role switch {
        Customer => "Customer · ลูกค้า", Staff => "Staff · Lodgo support", Owner => "Owner · เจ้าของ",
        Manager => "Manager · ผู้จัดการ", Admin => "Admin · ผู้ดูแล", AC => "AC · บัญชี", HotelOwner => "HotelStaff · พนักงานโรงแรม", _ => "ไม่ทราบบทบาท"
    };
    public static string HomeController(string? role) => role switch {
        Staff => "Staff", Owner => "Owner", Manager => "Manager", Admin => "Admin", AC => "AC", HotelOwner => "HotelOwner", _ => "Customer"
    };
    public static string Current(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role) ?? Customer;
}
public static class RolePolicies
{
    public const string Customer = "CustomerPortal";
    public const string Staff = "BookingOperations";
    public const string Owner = "BusinessOverview";
    public const string Manager = "TeamManagement";
    public const string AC = "FinancialReports";
    public const string HotelOwner = "HotelOwnership";
}
