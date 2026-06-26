using Microsoft.EntityFrameworkCore;
using Orca.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<OrcaDbContext>(options =>
    options.UseSqlServer("Server=DESKTOP-63TV43B\\SQLEXPRESS02;Database=OrcaDB;Trusted_Connection=True;TrustServerCertificate=True;"));

builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();