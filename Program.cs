var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Simple in-memory store registered as a singleton so data
// persists for the lifetime of the running application.
builder.Services.AddSingleton<TodoListApp.Data.TodoStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Todo/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Todo}/{action=Index}/{id?}");

app.Run();
