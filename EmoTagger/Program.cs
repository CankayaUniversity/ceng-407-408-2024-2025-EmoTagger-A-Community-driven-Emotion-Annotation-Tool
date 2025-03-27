using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using EmoTagger.Models;

var builder = WebApplication.CreateBuilder(args);

// **PostgreSQL baðlantýsýný ekleyelim**
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// **Session ve Cache için Gerekli Servisleri Ekleyelim**
builder.Services.AddDistributedMemoryCache(); // **Session için Gerekli**
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // **30 Dakika Oturum Süresi**
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// **Servisleri ekleyelim**
builder.Services.AddControllersWithViews();

var app = builder.Build();

// **Middleware yapýlandýrmasý**
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseDeveloperExceptionPage();

app.UseSession(); // **Session Middleware'ini Etkinleþtir**
app.UseAuthorization();

// **Varsayýlan Route Yapýsý**
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.Run();
