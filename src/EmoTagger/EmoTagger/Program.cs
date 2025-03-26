//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Configuration;
//using EmoTagger.Models;

//var builder = WebApplication.CreateBuilder(args);

//// **PostgreSQL ba�lant�s�n� ekleyelim**
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//// **Session ve Cache i�in Gerekli Servisleri Ekleyelim**
//builder.Services.AddDistributedMemoryCache(); // **Session i�in Gerekli**
//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(30); // **30 Dakika Oturum S�resi**
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

//// **Servisleri ekleyelim**
//builder.Services.AddControllersWithViews();

//var app = builder.Build();

//// **Middleware yap�land�rmas�**
//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();
//app.UseDeveloperExceptionPage();

//app.UseSession(); // **Session Middleware'ini Etkinle�tir**
//app.UseAuthorization();

//// **Varsay�lan Route Yap�s�**
//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllerRoute(
//        name: "default",
//        pattern: "{controller=Home}/{action=Index}/{id?}");
//});

//app.Run();
///
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using EmoTagger.Models;
using DotNetEnv; // Load environment variables from .env

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file (if exists)
DotNetEnv.Env.Load();

// Build the PostgreSQL connection string dynamically from environment variables
var connectionString = $"Host={Environment.GetEnvironmentVariable("DB_HOST")};" +
                       $"Port={Environment.GetEnvironmentVariable("DB_PORT")};" +
                       $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                       $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
                       $"Password={Environment.GetEnvironmentVariable("DB_PASS")}";

// Add PostgreSQL DbContext with the dynamically built connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// **Session and Cache Configuration**
builder.Services.AddDistributedMemoryCache(); // Required for Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 30-minute session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// **Add Controllers with Views**
builder.Services.AddControllersWithViews();

var app = builder.Build();

// **Middleware Configuration**
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseDeveloperExceptionPage();

app.UseSession(); // Enable session middleware
app.UseAuthorization();

// **Configure Default Route**
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.Run();

