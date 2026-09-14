using Microsoft.EntityFrameworkCore;
using TMS.DAL;

var builder = WebApplication.CreateBuilder(args);

// ── MVC
builder.Services.AddControllersWithViews();

// ── EF Core with SQL Server
builder.Services.AddDbContext<TuitionProContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TuitionProDB")));

// ── Session (needed for login state)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ── Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();       // must be before MapControllerRoute
app.UseAuthorization();

// ── Default route → Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
