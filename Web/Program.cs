using Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Web.Models;
using Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<ColetaService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<ComandoService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PersistentLogService>();
builder.Services.AddScoped<ManutencaoService>();
builder.Services.AddScoped<SmartphoneService>();
builder.Services.AddHostedService<PingService>();

// Configuração do Kestrel para escutar em todas as interfaces de rede
builder.WebHost.ConfigureKestrel(serverOptions =>
{
serverOptions.ListenAnyIP(80); // HTTP
serverOptions.ListenAnyIP(443, listenOptions => listenOptions.UseHttps()); // HTTPS
});

var app = builder.Build();

// Configure o pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    // app.UseExceptionHandler("/Home/Error"); // Removido pois HomeController não existe mais
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ChatHub>("/chatHub");

app.Run();