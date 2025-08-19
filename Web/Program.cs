using Web.Services;

var builder = WebApplication.CreateBuilder(args);
// Adiciona os serviços ao contêiner
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ColetaService>();
builder.Services.AddScoped<LogService>();

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

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
name: "default",
pattern: "{controller=Computadores}/{action=Index}/{id?}");

app.Run();