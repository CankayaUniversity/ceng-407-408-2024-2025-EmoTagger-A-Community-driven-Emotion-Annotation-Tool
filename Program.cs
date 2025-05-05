using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies; // Bu sat�r� ekleyin
using EmoTagger.Models;
using EmoTagger.Services;
using EmoTagger.Data;

var builder = WebApplication.CreateBuilder(args);

// **PostgreSQL ba�lant�s�n� ekleyelim**
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<EmailService>();

// **Session ve Cache i�in Gerekli Servisleri Ekleyelim**
builder.Services.AddDistributedMemoryCache(); // **Session i�in Gerekli**
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // **30 Dakika Oturum S�resi**
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// **Authentication Servislerini Ekleyelim**
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Dashboard/Login"; // Giri� sayfan�z�n yolu
        options.AccessDeniedPath = "/Dashboard/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Logging.AddConsole(); // Konsol loglamay� ekle
builder.Logging.AddDebug();   // Debug loglamay� ekle

// **Servisleri ekleyelim**
builder.Services.AddControllersWithViews();

var app = builder.Build();

// **Middleware yap�land�rmas�**
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseDeveloperExceptionPage();
app.UseSession(); // **Session Middleware'ini Etkinle�tir**

// **Authentication middleware'ini UseAuthorization'dan �NCE ekliyoruz**
app.UseAuthentication(); // BU SATIRI EKLEY�N
app.UseAuthorization();

// **Varsay�lan Route Yap�s�**
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}");
});

app.MapControllerRoute(
    name: "forgotpassword",
    pattern: "dashboard/forgotpassword",
    defaults: new { controller = "Dashboard", action = "ForgotPassword" }
);
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // E�er uploads/profiles klas�r�ndeki bir dosyaysa �nbelle�i devre d��� b�rak
        if (ctx.File.PhysicalPath.Contains("uploads/profiles"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
    }
});
app.Run();