using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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
app.UseAuthorization();

// **Varsay�lan Route Yap�s�**
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});
app.MapControllerRoute(
    name: "forgotpassword",
    pattern: "dashboard/forgotpassword",
    defaults: new { controller = "Dashboard", action = "ForgotPassword" }
);

app.Run();
