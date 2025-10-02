using Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Initialize Firebase Admin SDK
var firebaseSettingsPath = builder.Configuration["Firebase:GoogleCredentialsJson"];
if (string.IsNullOrEmpty(firebaseSettingsPath))
{
    Console.WriteLine("Firebase credentials path not set in appsettings.json. Skipping Firebase initialization for now.");
}
else
{
    if (File.Exists(firebaseSettingsPath))
    {
        FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile(firebaseSettingsPath),
        });
    }
    else
    {
        Console.WriteLine($"Firebase credentials file not found at: {firebaseSettingsPath}");
    }
}


// Add Firestore DB
builder.Services.AddSingleton<FirestoreDb>(provider =>
{
    var projectId = builder.Configuration["Firebase:ProjectId"];
    if (string.IsNullOrEmpty(projectId))
    {
        // Throw an exception or handle it as per your application's needs
        throw new InvalidOperationException("Firebase ProjectId is not set in appsettings.json");
    }
    return FirestoreDb.Create(projectId);
});


// Custom Firebase Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        // Later, we will add a custom cookie event to validate the session against Firebase
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    // Other roles will be managed via Firebase custom claims
});


// Register other services
builder.Services.AddScoped<ColetaService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<ComandoService>();
// UserService, ManutencaoService, etc., will be re-implemented using Firestore
// builder.Services.AddScoped<UserService>();
// builder.Services.AddScoped<PersistentLogService>();
// builder.Services.AddScoped<ManutencaoService>();
builder.Services.AddHostedService<PingService>();
builder.Services.AddHttpClient();


// Configuração do Kestrel para escutar em todas as interfaces de rede
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80); // HTTP
    serverOptions.ListenAnyIP(443, listenOptions => listenOptions.UseHttps()); // HTTPS
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Dashboard/Error");
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

app.Run();