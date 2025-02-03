using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using FYPProject.Models;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add Entity Framework Core and configure it with a connection string
builder.Services.AddDbContext<FYPProject.Models.ApplicationDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login/";
        options.AccessDeniedPath = "/AccessDenied/Error/";

    });

// Add session services
builder.Services.AddDistributedMemoryCache(); // Required for session handling
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true; // Session cookies will only be accessible via HTTP, not JavaScript
    options.Cookie.IsEssential = true; // Ensure session cookies are sent even if consent isn't given
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout duration
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Add HSTS for production
}

// Serve default files (e.g., index.html) and enable static files
app.UseDefaultFiles();

// Serve static files from the "wwwroot/images" folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images")),
    RequestPath = "/images"
});

// Enable session, authentication, and routing
app.UseSession(); 
app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();

// Map controller routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
