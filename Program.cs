using ApSafeFuzz;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ApSafeFuzz.Data;
using ApSafeFuzz.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
builder.Services.Configure<UploadFileSettingsModel>(
    builder.Configuration.GetSection(UploadFileSettingsModel.UploadFile));

builder.Services.AddSwaggerGen();

var app = builder.Build();

LogHelper.LoggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var startupChecks = new StartupChecks(LogHelper.CreateLogger<StartupChecks>());
// startupChecks.IsAnsibleInstalled(); Ansible is not working as a control node on Windows

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
