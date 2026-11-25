using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.Services.Booking;
using System.Globalization;

// HTML date inputs and payment decimals require Gregorian ISO dates and a dot
// decimal separator even when Windows uses the Thai Buddhist calendar.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");

var createAdmin = args.Contains("--create-admin");
var builder = WebApplication.CreateBuilder(args.Where(x => x != "--create-admin").ToArray());
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Nontiwat_7362_L002.Security.RolePolicies.Customer, p => p.RequireRole("User", "Admin"));
    options.AddPolicy(Nontiwat_7362_L002.Security.RolePolicies.Staff, p => p.RequireRole("Staff", "Admin"));
    options.AddPolicy(Nontiwat_7362_L002.Security.RolePolicies.Owner, p => p.RequireRole("Owner", "Admin"));
    options.AddPolicy(Nontiwat_7362_L002.Security.RolePolicies.Manager, p => p.RequireRole("Manager", "Admin"));
    options.AddPolicy(Nontiwat_7362_L002.Security.RolePolicies.AC, p => p.RequireRole("AC", "Admin"));
    options.AddPolicy(Nontiwat_7362_L002.Security.RolePolicies.HotelOwner, p => p.RequireRole("HotelOwner", "Admin"));
});
var keyDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys");
Directory.CreateDirectory(keyDirectory);
builder.Services.AddDataProtection().SetApplicationName("Lodgo.A9")
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
builder.Services.AddHttpContextAccessor();
// One clock for the whole app: Thai date and time from .NET's TimeProvider (tests swap in a fake one).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IThaiClock, ThaiClock>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<HotelCatalogService>();
builder.Services.AddScoped<HotelRoomCatalog>();
builder.Services.AddScoped<HotelRoomAdminService>();
builder.Services.AddScoped<HotelPhotoService>();
builder.Services.AddDbContext<ApplicationDbContext>((services, options) => options.UseMySql(
    new MySqlConnector.MySqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing database connection")) { AllowUserVariables = true }.ConnectionString,
    new MySqlServerVersion(new Version(8, 0, 36))));
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.AddScoped<UserService>();
// Booking and money ports (Step 4b): the real MySQL/EF implementations behind the interfaces the workflows use.
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Booking.IBookingUnitOfWork, Nontiwat_7362_L002.Data.Booking.EfBookingUnitOfWork>();
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Booking.IBookingStore, Nontiwat_7362_L002.Data.Booking.EfBookingStore>();
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Booking.IWalletStore, Nontiwat_7362_L002.Data.Booking.EfWalletStore>();
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Booking.IHotelLedger, Nontiwat_7362_L002.Data.Booking.EfHotelLedger>();
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Booking.IBookingJournal, Nontiwat_7362_L002.Data.Booking.EfBookingJournal>();
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Booking.IActorDirectory, Nontiwat_7362_L002.Data.Booking.EfActorDirectory>();
builder.Services.AddScoped<BookingDecisions>();
builder.Services.AddScoped<BookingRefunds>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<TeamBookingEdits>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<PaymentReviewService>();
builder.Services.AddScoped<HotelDeskService>();
builder.Services.AddScoped<CancellationService>();
builder.Services.AddScoped<StayDemoService>();
builder.Services.AddHostedService<Nontiwat_7362_L002.Services.Booking.BookingHousekeeping>();
builder.Services.AddScoped<HotelOwnershipService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddScoped<IBookingAvailability>(services => services.GetRequiredService<AvailabilityService>());
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<TodayService>();
builder.Services.AddScoped<WishlistService>();
builder.Services.AddScoped<ReviewReplyService>();
builder.Services.AddScoped<Nontiwat_7362_L002.Services.IUnitOfWork>(services => services.GetRequiredService<Nontiwat_7362_L002.Services.Booking.IBookingUnitOfWork>());
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Support.ISupportTicketStore, Nontiwat_7362_L002.Data.Support.EfSupportTicketStore>();
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Support.SupportTicketService>();
builder.Services.AddScoped<Nontiwat_7362_L002.Data.Support.SupportInboxQuery>();
builder.Services.AddScoped<SupportService>();
builder.Services.AddScoped<PasswordResetRequests>();
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Support.ISupportChat>(services => services.GetRequiredService<SupportService>());
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Support.ISupportQuoteStore, Nontiwat_7362_L002.Data.Support.EfSupportQuoteStore>();
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Support.IQuoteCheckout, Nontiwat_7362_L002.Services.Support.CheckoutQuoteAdapter>();
builder.Services.AddScoped<Nontiwat_7362_L002.Services.Support.SupportQuoteService>();
builder.Services.AddScoped<HotelAccessService>();
builder.Services.AddScoped<MonthlyFigures>();
builder.Services.AddScoped<KpiService>();
builder.Services.AddScoped<OwnerInsights>();
builder.Services.AddScoped<DatabaseCookieEvents>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Lodgo.A9.Account";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.EventsType = typeof(DatabaseCookieEvents);
    });
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "Lodgo.A9.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    // Creates the project's new database and table without deleting existing rows.
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (builder.Configuration.GetValue<bool>("Database:RequireExistingDatabase") ||
        File.Exists(Path.Combine(builder.Environment.ContentRootPath, "ExistingDatabase.json")))
    {
        // A missing or incorrect legacy database must fail before any schema creation.
        await database.Database.OpenConnectionAsync();
        try
        {
            await using var check = database.Database.GetDbConnection().CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM users";
            await check.ExecuteScalarAsync();
        }
        finally { await database.Database.CloseConnectionAsync(); }
    }
    else { await database.Database.EnsureCreatedAsync(); }
    await CommerceSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await RoleSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await HotelOwnerSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await CartSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await ReviewSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await StayEnvironmentSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await CatalogSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await AuditSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await ExperienceSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    // Account suspension and the other role features (round 1). Must run before the seeds: they read and add users.
    await RoleFeaturesSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await UserDirectorySchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await ServiceSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await PaymentSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await VerifiedCatalogSeed.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), builder.Environment.ContentRootPath, scope.ServiceProvider.GetRequiredService<IThaiClock>().Today);
    await DiscoveryCatalogSeed.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), builder.Environment.ContentRootPath);
    // Demo HotelStaff accounts and hotel teams (adds only what is missing).
    await HotelStaffSeed.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    // Booking history, notifications and the room calendar (runs after room_types exists).
    await OperationsSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    await HotelDetailsSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    if (createAdmin)
    {
        await AdminSetup.RunAsync(scope.ServiceProvider.GetRequiredService<UserService>());
        return;
    }
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseSession();
app.UseAuthentication();
// MVC pages contain account-specific navigation, balances and bookings.
// Never reuse a previous account's HTML after signing out or switching users.
app.Use(async (context, next) =>
{
    if (context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>() is not null)
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
    await next();
});
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();
app.Run();
public partial class Program { }
