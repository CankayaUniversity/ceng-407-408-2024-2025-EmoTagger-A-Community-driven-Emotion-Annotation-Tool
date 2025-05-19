using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies; // Bu satýrý ekleyin
using EmoTagger.Models;
using EmoTagger.Services;
using EmoTagger.Data;

var builder = WebApplication.CreateBuilder(args);

// **PostgreSQL baðlantýsýný ekleyelim**
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<EmailService>();

// **Session ve Cache için Gerekli Servisleri Ekleyelim**
builder.Services.AddDistributedMemoryCache(); // **Session için Gerekli**
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // **30 Dakika Oturum Süresi**
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// **Authentication Servislerini Ekleyelim**
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Dashboard/Login"; // Giriþ sayfanýzýn yolu
        options.AccessDeniedPath = "/Dashboard/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Logging.AddConsole(); // Konsol loglamayý ekle
builder.Logging.AddDebug();   // Debug loglamayý ekle

// **Servisleri ekleyelim**
builder.Services.AddControllersWithViews();

var app = builder.Build();

// **Middleware yapýlandýrmasý**
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseDeveloperExceptionPage();
app.UseSession(); // **Session Middleware'ini Etkinleþtir**

// **Authentication middleware'ini UseAuthorization'dan ÖNCE ekliyoruz**
app.UseAuthentication(); // BU SATIRI EKLEYÝN
app.UseAuthorization();

// **Varsayýlan Route Yapýsý**
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
        // Eðer uploads/profiles klasöründeki bir dosyaysa önbelleði devre dýþý býrak
        if (ctx.File.PhysicalPath.Contains("uploads/profiles"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
    }
});
app.Run();